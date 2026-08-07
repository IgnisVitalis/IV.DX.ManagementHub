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

. "$PSScriptRoot/nuget-feed.ps1"

$repoRoot  = Resolve-Path (Join-Path $PSScriptRoot "..")
$LocalFeed = $LocalFeedPath
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

    # GitHub Packages is the primary source; local packages count only while a
    # version has not been published there yet.
    $latestFeedVersion = Get-LatestFeedVersion -PackageIds $PackageIds

    $targetVersion = $RequestedVersion
    if ([string]::IsNullOrWhiteSpace($targetVersion)) {
        $targetVersion = $currentProjectVersion
        if (-not [string]::IsNullOrWhiteSpace($latestFeedVersion)) {
            $currentParsed = $null
            $feedParsed = $null

            $currentParsedOk = [version]::TryParse($currentProjectVersion, [ref]$currentParsed)
            $feedParsedOk = [version]::TryParse($latestFeedVersion, [ref]$feedParsed)

            # Never downgrade a reference automatically: the code may already
            # depend on API added after the published version.
            if ($feedParsedOk -and (($currentParsedOk -and $feedParsed -gt $currentParsed) -or -not $currentParsedOk)) {
                $targetVersion = $latestFeedVersion
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($targetVersion)) {
        throw "Unable to detect target $familyName version."
    }

    if ((Get-LastFeedVersionSource) -eq "github" -and $targetVersion -ne $latestFeedVersion) {
        Write-Warning "$familyName $targetVersion is not published on GitHub Packages (newest published: $latestFeedVersion) - CI can only restore published versions."
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

Register-GitHubPackagesSource | Out-Null

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
