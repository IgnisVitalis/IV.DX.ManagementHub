#!/usr/bin/env pwsh
param(
    [string]$SolutionPath = "src/IV.ManagementHub/IV.ManagementHub.sln",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$DxVersion,
    [string]$DxPresentationVersion,
    [switch]$SkipDxSync,
    [switch]$ErrorsOnly
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Args,
        [string]$ErrorContext = "dotnet command failed."
    )

    & dotnet @Args
    if ($LASTEXITCODE -ne 0) {
        throw "$ErrorContext Exit code: $LASTEXITCODE"
    }
}

function Get-HighestSemVer {
    param([string[]]$Versions)

    $parsedVersions = @()
    foreach ($candidate in $Versions) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        $version = $null
        if ([version]::TryParse($candidate, [ref]$version)) {
            $parsedVersions += [PSCustomObject]@{
                Raw    = $candidate
                Parsed = $version
            }
        }
    }

    if ($parsedVersions.Count -eq 0) {
        return $null
    }

    return ($parsedVersions | Sort-Object Parsed -Descending | Select-Object -First 1).Raw
}

function Get-LatestLocalPackageVersion {
    param([Parameter(Mandatory = $true)][string]$PackageId)

    $packageFolderName = $PackageId.ToLowerInvariant()
    $nupkgPattern = '^' + [regex]::Escape($PackageId) + '\.(?<Version>\d+\.\d+\.\d+)\.nupkg$'

    $sourcePaths = @()
    $sourceLines = dotnet nuget list source 2>$null

    for ($index = 0; $index -lt $sourceLines.Count; $index++) {
        $line = $sourceLines[$index].Trim()
        if ($line -match '^\d+\.\s+.+\[(Enabled|Disabled)\]$' -and ($index + 1) -lt $sourceLines.Count) {
            $value = $sourceLines[$index + 1].Trim()
            if ($value -and -not ($value -match '^(https?|ftp)://')) {
                $sourcePaths += $value
            }
        }
    }

    if ($env:USERPROFILE) {
        $sourcePaths += (Join-Path $env:USERPROFILE ".nuget\packages\$packageFolderName")
    }

    $sourcePaths = $sourcePaths |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique

    $versions = @()
    foreach ($path in $sourcePaths) {
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $nupkgFiles = Get-ChildItem -Path $path -Recurse -File -Filter "$PackageId*.nupkg" -ErrorAction SilentlyContinue
        foreach ($file in $nupkgFiles) {
            if ($file.Name -match $nupkgPattern) {
                $versions += $Matches.Version
            }
        }

        if ((Split-Path -Leaf $path).ToLowerInvariant() -eq $packageFolderName) {
            $versionFolders = Get-ChildItem -LiteralPath $path -Directory -ErrorAction SilentlyContinue
            foreach ($folder in $versionFolders) {
                if ($folder.Name -match '^\d+\.\d+\.\d+$') {
                    $versions += $folder.Name
                }
            }
        }
    }

    return Get-HighestSemVer -Versions $versions
}

function Get-PackageVersionInfos {
    param(
        [Parameter(Mandatory = $true)][string]$SolutionPath,
        [Parameter(Mandatory = $true)][string]$PackageId
    )

    $solutionFullPath = if ([IO.Path]::IsPathRooted($SolutionPath)) {
        (Resolve-Path $SolutionPath).Path
    }
    else {
        (Resolve-Path (Join-Path $repoRoot $SolutionPath)).Path
    }

    $solutionDir = Split-Path -Parent $solutionFullPath
    $escapedId = [regex]::Escape($PackageId)
    $pattern = "<PackageReference\s+Include=""$escapedId""\s+Version=""([^""]+)""\s*/>"

    $versionInfos = @()
    $projects = dotnet sln $solutionFullPath list |
        Where-Object { $_ -match '\.csproj$' }

    foreach ($projectLine in $projects) {
        $relativePath = $projectLine.Trim()
        $projectFullPath = (Resolve-Path (Join-Path $solutionDir $relativePath)).Path
        $content = Get-Content -Raw -LiteralPath $projectFullPath
        $match = [regex]::Match($content, $pattern)
        if (-not $match.Success) {
            continue
        }

        $projectRelativeToRepo = [IO.Path]::GetRelativePath($repoRoot, $projectFullPath)
        $versionInfos += [PSCustomObject]@{
            RelativePath = $projectRelativeToRepo
            Version      = $match.Groups[1].Value
        }
    }

    return $versionInfos
}

function Sync-PackageVersion {
    param(
        [Parameter(Mandatory = $true)][string]$SolutionPath,
        [Parameter(Mandatory = $true)][string]$PackageId,
        [string]$RequestedVersion
    )

    $versionInfos = @(Get-PackageVersionInfos -SolutionPath $SolutionPath -PackageId $PackageId)
    if ($versionInfos.Count -eq 0) {
        Write-Host "No $PackageId PackageReference entries found for sync."
        return
    }

    $currentProjectVersion = Get-HighestSemVer -Versions ($versionInfos | ForEach-Object { $_.Version })
    $latestLocalVersion = Get-LatestLocalPackageVersion -PackageId $PackageId

    $targetVersion = $RequestedVersion
    if ([string]::IsNullOrWhiteSpace($targetVersion)) {
        $targetVersion = $currentProjectVersion
        if (-not [string]::IsNullOrWhiteSpace($latestLocalVersion)) {
            $currentParsed = $null
            $localParsed = $null

            $currentParsedOk = [version]::TryParse($currentProjectVersion, [ref]$currentParsed)
            $localParsedOk = [version]::TryParse($latestLocalVersion, [ref]$localParsed)

            if ($localParsedOk -and (($currentParsedOk -and $localParsed -gt $currentParsed) -or -not $currentParsedOk)) {
                $targetVersion = $latestLocalVersion
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($targetVersion)) {
        throw "Unable to detect target $PackageId version."
    }

    Write-Host "Syncing $PackageId version to '$targetVersion'..."

    $escapedId = [regex]::Escape($PackageId)
    foreach ($info in $versionInfos) {
        if ($info.Version -eq $targetVersion) {
            Write-Host "$PackageId already up to date in $($info.RelativePath)"
            continue
        }

        $projectFullPath = Join-Path $repoRoot $info.RelativePath
        $content = Get-Content -Raw -LiteralPath $projectFullPath
        $updatedContent = [regex]::Replace(
            $content,
            "(<PackageReference\s+Include=""$escapedId""\s+Version="")[^""]+(""\s*/>)",
            {
                param($m)
                return $m.Groups[1].Value + $targetVersion + $m.Groups[2].Value
            })

        Set-Content -LiteralPath $projectFullPath -Value $updatedContent -NoNewline
        Write-Host "Updated $PackageId in $($info.RelativePath): $($info.Version) -> $targetVersion"
    }
}

if (-not $SkipDxSync) {
    Sync-PackageVersion -SolutionPath $SolutionPath -PackageId "IV.DX" -RequestedVersion $DxVersion
    Sync-PackageVersion -SolutionPath $SolutionPath -PackageId "IV.DX.Presentation" -RequestedVersion $DxPresentationVersion
}

Write-Host "Building solution: $SolutionPath"
Write-Host "Configuration: $Configuration"

Invoke-DotNet -Args @("restore", $SolutionPath) -ErrorContext "Restore failed."

if ($ErrorsOnly) {
    Write-Host "Mode: errors only"
    Invoke-DotNet -Args @("build", $SolutionPath, "-c", $Configuration, "-v:q", "-p:WarningLevel=0") -ErrorContext "Build failed."
}
else {
    Write-Host "Mode: full output"
    Invoke-DotNet -Args @("build", $SolutionPath, "-c", $Configuration) -ErrorContext "Build failed."
}
