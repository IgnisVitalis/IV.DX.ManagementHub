#!/usr/bin/env pwsh
param(
    [string]$SolutionPath = "src/IV.DX.ManagementHub/IV.DX.ManagementHub.sln",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$DxVersion,
    [string]$DxPresentationVersion,
    [string]$DxWebApiVersion,
    [string]$DxWebApiAuthVersion,
    [string]$DxWebApiManagementVersion,
    [switch]$SkipDxSync,
    [switch]$ErrorsOnly
)

$ErrorActionPreference = "Stop"

$repoRoot  = Resolve-Path (Join-Path $PSScriptRoot "..")
$LocalFeed = Join-Path $HOME ".nuget" "local-feed"
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

function Get-LocalPackageVersions {
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

    $sourcePaths += $LocalFeed
    $sourcePaths += (Join-Path $HOME ".nuget" "packages" $packageFolderName)

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

    return $versions | Select-Object -Unique
}

function Get-LatestLocalFamilyVersion {
    param([Parameter(Mandatory = $true)][string[]]$PackageIds)

    # Only versions published for every package in the family qualify — packages that
    # ship in lockstep are only valid against the exact version they were built with.
    $commonVersions = $null
    foreach ($packageId in $PackageIds) {
        $available = @(Get-LocalPackageVersions -PackageId $packageId)

        if ($null -eq $commonVersions) {
            $commonVersions = $available
        }
        else {
            $commonVersions = @($commonVersions | Where-Object { $available -contains $_ })
        }
    }

    return Get-HighestSemVer -Versions $commonVersions
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

        $projectRelativeToRepo = $projectFullPath.Substring($repoRoot.ToString().Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $versionInfos += [PSCustomObject]@{
            PackageId    = $PackageId
            RelativePath = $projectRelativeToRepo
            Version      = $match.Groups[1].Value
        }
    }

    return $versionInfos
}

function Sync-PackageVersion {
    param(
        [Parameter(Mandatory = $true)][string]$SolutionPath,
        [Parameter(Mandatory = $true)][string[]]$PackageIds,
        [string]$RequestedVersion
    )

    $familyName = $PackageIds -join ', '

    $versionInfos = @()
    foreach ($packageId in $PackageIds) {
        $versionInfos += @(Get-PackageVersionInfos -SolutionPath $SolutionPath -PackageId $packageId)
    }

    if ($versionInfos.Count -eq 0) {
        Write-Host "No $familyName PackageReference entries found for sync."
        return
    }

    $currentProjectVersion = Get-HighestSemVer -Versions ($versionInfos | ForEach-Object { $_.Version })
    $latestLocalVersion = Get-LatestLocalFamilyVersion -PackageIds $PackageIds

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
        throw "Unable to detect target $familyName version."
    }

    Write-Host "Syncing $familyName version to '$targetVersion'..."

    foreach ($info in $versionInfos) {
        if ($info.Version -eq $targetVersion) {
            Write-Host "$($info.PackageId) already up to date in $($info.RelativePath)"
            continue
        }

        $escapedId = [regex]::Escape($info.PackageId)
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
        Write-Host "Updated $($info.PackageId) in $($info.RelativePath): $($info.Version) -> $targetVersion"
    }
}

if (-not $SkipDxSync) {
    # IV.DX and its database provider packages ship in lockstep: the provider SPI is
    # internal, so a provider is only valid against the exact core version it was
    # built with. They are synced together to one version.
    Sync-PackageVersion -SolutionPath $SolutionPath -PackageIds @("IV.DX", "IV.DX.PostgreSQL") -RequestedVersion $DxVersion
    Sync-PackageVersion -SolutionPath $SolutionPath -PackageIds "IV.DX.Presentation" -RequestedVersion $DxPresentationVersion
    Sync-PackageVersion -SolutionPath $SolutionPath -PackageIds "IV.DX.WebApi" -RequestedVersion $DxWebApiVersion
    Sync-PackageVersion -SolutionPath $SolutionPath -PackageIds "IV.DX.WebApi.Auth" -RequestedVersion $DxWebApiAuthVersion
    Sync-PackageVersion -SolutionPath $SolutionPath -PackageIds "IV.DX.WebApi.Management" -RequestedVersion $DxWebApiManagementVersion
}

Write-Host "Building solution: $SolutionPath"
Write-Host "Configuration: $Configuration"

if (Test-Path $LocalFeed) {
    $registeredSources = dotnet nuget list source 2>$null
    if (-not ($registeredSources | Select-String -SimpleMatch $LocalFeed -Quiet)) {
        Write-Host "Registering local NuGet feed: $LocalFeed"
        dotnet nuget add source $LocalFeed --name "local-feed"
    }
}

Invoke-DotNet -Args @("restore", $SolutionPath) -ErrorContext "Restore failed."

if ($ErrorsOnly) {
    Write-Host "Mode: errors only"
    Invoke-DotNet -Args @("build", $SolutionPath, "-c", $Configuration, "-v:q", "-p:WarningLevel=0") -ErrorContext "Build failed."
}
else {
    Write-Host "Mode: full output"
    Invoke-DotNet -Args @("build", $SolutionPath, "-c", $Configuration) -ErrorContext "Build failed."
}
