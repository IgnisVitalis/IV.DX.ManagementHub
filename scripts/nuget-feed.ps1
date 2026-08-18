#!/usr/bin/env pwsh
# Shared NuGet feed helpers. Dot-sourced by build.ps1 and pack.ps1, not run directly.
#
# Package versions are discovered from both GitHub Packages and the local sources
# (local feed, global package cache), and the newest wins wherever it lives - a local
# build is usually ahead of the feed precisely because it is the one being worked on.
# Resolving to a local-only version warns, because CI can restore only what has been
# published, but it does not change the choice.

$GitHubPackagesOwner = if ($env:IV_DX_GITHUB_OWNER) { $env:IV_DX_GITHUB_OWNER } else { "IgnisVitalis" }
$GitHubPackagesSourceName = "github"
$GitHubPackagesSourceUrl = "https://nuget.pkg.github.com/$($GitHubPackagesOwner)/index.json"
$LocalFeedPath = Join-Path $HOME ".nuget" "local-feed"

# Environment variables are checked in order; the first non-empty one wins.
$GitHubPackagesTokenVariables = @(
    "GITHUB_PACKAGES_TOKEN",
    "IV_DX_GITHUB_TOKEN",
    "GH_TOKEN",
    "GITHUB_TOKEN"
)

$script:GitHubPackagesTokenReported = $false

# Source the last Get-LatestFeedVersion result came from: "github", "local" or "none".
$script:LastFeedVersionSource = "none"

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
                Raw = $candidate
                Parsed = $version
            }
        }
    }

    if ($parsedVersions.Count -eq 0) {
        return $null
    }

    return ($parsedVersions | Sort-Object Parsed -Descending | Select-Object -First 1).Raw
}

function Get-StoredGitHubPackagesToken {
    # Credentials stored by setup-github-feed.ps1 (or `dotnet nuget add source`)
    # live in the user-level config, never in the repo.
    $configPath = Join-Path $HOME ".nuget" "NuGet" "NuGet.Config"
    if (-not (Test-Path -LiteralPath $configPath)) {
        return $null
    }

    try {
        [xml]$config = Get-Content -Raw -LiteralPath $configPath
    }
    catch {
        return $null
    }

    $credentials = $config.configuration.packageSourceCredentials
    if (-not $credentials) {
        return $null
    }

    foreach ($sourceNode in $credentials.ChildNodes) {
        if ($sourceNode.NodeType -ne [System.Xml.XmlNodeType]::Element) {
            continue
        }

        if ($sourceNode.Name -ne $GitHubPackagesSourceName) {
            continue
        }

        foreach ($entry in @($sourceNode.add)) {
            if ($entry.key -eq "ClearTextPassword" -and -not [string]::IsNullOrWhiteSpace($entry.value)) {
                return $entry.value
            }
        }
    }

    return $null
}

function Get-GitHubPackagesToken {
    foreach ($variableName in $GitHubPackagesTokenVariables) {
        $value = [Environment]::GetEnvironmentVariable($variableName)
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }

    return Get-StoredGitHubPackagesToken
}

function Get-GitHubPackageVersions {
    param([Parameter(Mandatory = $true)][string]$PackageId)

    $token = Get-GitHubPackagesToken
    if ([string]::IsNullOrWhiteSpace($token)) {
        if (-not $script:GitHubPackagesTokenReported) {
            Write-Warning "No GitHub Packages token found - using local packages only. Run scripts/setup-github-feed.ps1 or set GITHUB_PACKAGES_TOKEN."
            $script:GitHubPackagesTokenReported = $true
        }

        return @()
    }

    $uri = "https://nuget.pkg.github.com/$($GitHubPackagesOwner)/download/$($PackageId.ToLowerInvariant())/index.json"
    $credentialBytes = [Text.Encoding]::ASCII.GetBytes("$($GitHubPackagesOwner):$($token)")
    $headers = @{ Authorization = "Basic " + [Convert]::ToBase64String($credentialBytes) }

    try {
        $response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get -TimeoutSec 30 -ErrorAction Stop
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        # 404 simply means the package has never been published to the feed.
        if ($statusCode -ne 404) {
            Write-Warning "GitHub Packages lookup for $PackageId failed ($($_.Exception.Message)) - falling back to local packages."
        }

        return @()
    }

    if (-not $response.versions) {
        return @()
    }

    return @($response.versions | Where-Object { $_ -match '^\d+\.\d+\.\d+$' })
}

function Get-LocalPackageSourcePaths {
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

    $sourcePaths += $LocalFeedPath

    return $sourcePaths |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique
}

function Get-LocalPackageVersions {
    param([Parameter(Mandatory = $true)][string]$PackageId)

    $globalCache = Join-Path $HOME ".nuget" "packages" $PackageId.ToLowerInvariant()

    $sourcePaths = @(Get-LocalPackageSourcePaths)
    $sourcePaths += $globalCache
    $sourcePaths = $sourcePaths | Select-Object -Unique

    $versionPattern = '^' + [regex]::Escape($PackageId) + '\.(?<Version>\d+\.\d+\.\d+)\.nupkg$'

    $versions = @()
    foreach ($path in $sourcePaths) {
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $nupkgFiles = Get-ChildItem -Path $path -Recurse -File -Filter "$PackageId.*.nupkg" -ErrorAction SilentlyContinue
        foreach ($file in $nupkgFiles) {
            if ($file.Name -match $versionPattern) {
                $versions += $Matches.Version
            }
        }

        if ((Split-Path -Leaf $path).ToLowerInvariant() -eq $PackageId.ToLowerInvariant()) {
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

function Get-CommonPackageVersion {
    param(
        [Parameter(Mandatory = $true)][string[]]$PackageIds,
        [Parameter(Mandatory = $true)][scriptblock]$VersionResolver
    )

    # Only versions available for every package in the family qualify - packages
    # that ship in lockstep are valid only against the exact version they were
    # built with.
    $commonVersions = $null
    foreach ($packageId in $PackageIds) {
        $available = @(& $VersionResolver $packageId)

        if ($null -eq $commonVersions) {
            $commonVersions = $available
        }
        else {
            $commonVersions = @($commonVersions | Where-Object { $available -contains $_ })
        }
    }

    return Get-HighestSemVer -Versions $commonVersions
}

function Get-LatestFeedVersion {
    param([Parameter(Mandatory = $true)][string[]]$PackageIds)

    $familyName = $PackageIds -join ', '

    $gitHubVersion = Get-CommonPackageVersion -PackageIds $PackageIds -VersionResolver {
        param($packageId)
        Get-GitHubPackageVersions -PackageId $packageId
    }

    $localVersion = Get-CommonPackageVersion -PackageIds $PackageIds -VersionResolver {
        param($packageId)
        Get-LocalPackageVersions -PackageId $packageId
    }

    $gitHubParsed = $null
    $localParsed = $null
    $gitHubOk = [version]::TryParse($gitHubVersion, [ref]$gitHubParsed)
    $localOk = [version]::TryParse($localVersion, [ref]$localParsed)

    # The newest version wins wherever it lives. A local build is normally ahead of the
    # feed precisely because it is the one being worked on, so preferring the published
    # version would pin every consumer to the previous release.
    if ($localOk -and (-not $gitHubOk -or $localParsed -gt $gitHubParsed)) {
        if ($gitHubOk) {
            Write-Warning "$familyName $localVersion is local only - newer than the published $gitHubVersion, so CI cannot restore it until it is published."
        }
        else {
            Write-Warning "$familyName $localVersion is local only - CI cannot restore it until it is published."
        }

        Write-Host "Resolved $familyName $localVersion from local packages."
        $script:LastFeedVersionSource = "local"
        return $localVersion
    }

    if ($gitHubOk) {
        Write-Host "Resolved $familyName $gitHubVersion from GitHub Packages."
        $script:LastFeedVersionSource = "github"
        return $gitHubVersion
    }

    $script:LastFeedVersionSource = "none"
    return $null
}

function Get-LastFeedVersionSource {
    return $script:LastFeedVersionSource
}

function Get-HighestKnownVersion {
    param([Parameter(Mandatory = $true)][string[]]$PackageIds)

    # Union across sources, unlike Get-LatestFeedVersion: a version to be produced
    # must not collide with anything that already exists locally or on the feed.
    $versions = @()
    foreach ($packageId in $PackageIds) {
        $versions += @(Get-GitHubPackageVersions -PackageId $packageId)
        $versions += @(Get-LocalPackageVersions -PackageId $packageId)
    }

    return Get-HighestSemVer -Versions $versions
}

function Test-GitHubPackagesToken {
    param([Parameter(Mandatory = $true)][string]$Token)

    # The service index is readable without credentials, so the token has to be
    # checked against a package endpoint, which is not. A 404 there still proves
    # the token is accepted - it only means that package was never published.
    $uri = "https://nuget.pkg.github.com/$($GitHubPackagesOwner)/download/iv.dx/index.json"
    $credentialBytes = [Text.Encoding]::ASCII.GetBytes("$($GitHubPackagesOwner):$($Token)")
    $headers = @{ Authorization = "Basic " + [Convert]::ToBase64String($credentialBytes) }

    try {
        Invoke-RestMethod -Uri $uri -Headers $headers -Method Get -TimeoutSec 30 -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($statusCode -eq 404) {
            return $true
        }

        Write-Warning "GitHub Packages rejected the token ($($_.Exception.Message)) - the feed was not registered. Check that it has the read:packages scope."
        return $false
    }
}

function Register-GitHubPackagesSource {
    # Makes the GitHub feed usable by `dotnet restore`. The token is written to the
    # user-level NuGet config only, so it never reaches the repository.
    $token = Get-GitHubPackagesToken
    if ([string]::IsNullOrWhiteSpace($token)) {
        return $false
    }

    $registeredSources = dotnet nuget list source 2>$null
    if ($registeredSources | Select-String -SimpleMatch $GitHubPackagesSourceUrl -Quiet) {
        return $true
    }

    # A source with an unusable token would break every restore in the repo, so the
    # token is checked before it is written to the config.
    if (-not (Test-GitHubPackagesToken -Token $token)) {
        return $false
    }

    Write-Host "Registering GitHub Packages feed: $GitHubPackagesSourceUrl"
    dotnet nuget add source $GitHubPackagesSourceUrl `
        --name $GitHubPackagesSourceName `
        --username $GitHubPackagesOwner `
        --password $token `
        --store-password-in-clear-text | Out-Null

    return $LASTEXITCODE -eq 0
}
