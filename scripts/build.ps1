#!/usr/bin/env pwsh
param(
    [string]$SolutionPath = "src/IV.ManagementHub/IV.ManagementHub.sln",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$DxVersion,
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

function Get-LatestLocalDxVersion {
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
        $sourcePaths += (Join-Path $env:USERPROFILE ".nuget\packages\iv.dx")
    }

    $sourcePaths = $sourcePaths |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique

    $versions = @()
    foreach ($path in $sourcePaths) {
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $nupkgFiles = Get-ChildItem -Path $path -Recurse -File -Filter "IV.DX*.nupkg" -ErrorAction SilentlyContinue
        foreach ($file in $nupkgFiles) {
            if ($file.Name -match '^IV\.DX\.(?<Version>\d+\.\d+\.\d+)\.nupkg$') {
                $versions += $Matches.Version
            }
        }

        if ((Split-Path -Leaf $path).ToLowerInvariant() -eq "iv.dx") {
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

function Get-DxProjectVersionInfos {
    param([string]$ResolvedSolutionPath)

    $solutionFullPath = if ([IO.Path]::IsPathRooted($ResolvedSolutionPath)) {
        (Resolve-Path $ResolvedSolutionPath).Path
    }
    else {
        (Resolve-Path (Join-Path $repoRoot $ResolvedSolutionPath)).Path
    }

    $solutionDir = Split-Path -Parent $solutionFullPath
    $dxPattern = '<PackageReference\s+Include="IV\.DX"\s+Version="([^"]+)"\s*/>'

    $versionInfos = @()
    $projects = dotnet sln $solutionFullPath list |
        Where-Object { $_ -match '\.csproj$' }

    foreach ($projectLine in $projects) {
        $relativePath = $projectLine.Trim()
        $projectFullPath = (Resolve-Path (Join-Path $solutionDir $relativePath)).Path
        $content = Get-Content -Raw -LiteralPath $projectFullPath
        $match = [regex]::Match($content, $dxPattern)
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

if (-not $SkipDxSync) {
    $versionInfos = @(Get-DxProjectVersionInfos -ResolvedSolutionPath $SolutionPath)
    if ($versionInfos.Count -eq 0) {
        Write-Host "No IV.DX PackageReference entries found for sync."
    }
    else {
        $currentProjectVersion = Get-HighestSemVer -Versions ($versionInfos | ForEach-Object { $_.Version })
        $latestLocalVersion = Get-LatestLocalDxVersion

        $targetDxVersion = $DxVersion
        if ([string]::IsNullOrWhiteSpace($targetDxVersion)) {
            $targetDxVersion = $currentProjectVersion
            if (-not [string]::IsNullOrWhiteSpace($latestLocalVersion)) {
                $currentParsed = $null
                $localParsed = $null

                $currentParsedOk = [version]::TryParse($currentProjectVersion, [ref]$currentParsed)
                $localParsedOk = [version]::TryParse($latestLocalVersion, [ref]$localParsed)

                if ($localParsedOk -and (($currentParsedOk -and $localParsed -gt $currentParsed) -or -not $currentParsedOk)) {
                    $targetDxVersion = $latestLocalVersion
                }
            }
        }

        if ([string]::IsNullOrWhiteSpace($targetDxVersion)) {
            throw "Unable to detect target IV.DX version."
        }

        Write-Host "Syncing IV.DX package version to '$targetDxVersion'..."

        foreach ($info in $versionInfos) {
            if ($info.Version -eq $targetDxVersion) {
                Write-Host "IV.DX already up to date in $($info.RelativePath)"
                continue
            }

            $projectFullPath = Join-Path $repoRoot $info.RelativePath
            $content = Get-Content -Raw -LiteralPath $projectFullPath
            $updatedContent = [regex]::Replace($content, '(<PackageReference\s+Include="IV\.DX"\s+Version=")[^"]+("\s*/>)',
                {
                    param($match)
                    return $match.Groups[1].Value + $targetDxVersion + $match.Groups[2].Value
                })

            Set-Content -LiteralPath $projectFullPath -Value $updatedContent -NoNewline
            Write-Host "Updated IV.DX version in $($info.RelativePath): $($info.Version) -> $targetDxVersion"
        }
    }
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
