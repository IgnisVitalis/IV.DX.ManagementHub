#!/usr/bin/env pwsh
# Registers the GitHub Packages feed so restores and the build/pack scripts can see
# IV.DX packages published by CI.
#
# The token is stored in the user-level NuGet config (~/.nuget/NuGet/NuGet.Config),
# never in the repository. It needs the read:packages scope; write:packages is only
# required to push a package by hand.
#
#   ./scripts/setup-github-feed.ps1 -Token ghp_xxx
#   GITHUB_PACKAGES_TOKEN=ghp_xxx ./scripts/setup-github-feed.ps1
param(
    [string]$Token,
    [string]$Owner = "IgnisVitalis",
    [string]$Name = "github"
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot/nuget-feed.ps1"

$GitHubPackagesOwner = $Owner
$GitHubPackagesSourceUrl = "https://nuget.pkg.github.com/$Owner/index.json"

if ([string]::IsNullOrWhiteSpace($Token)) {
    $Token = Get-GitHubPackagesToken
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "No token supplied. Pass -Token or set GITHUB_PACKAGES_TOKEN."
}

if (-not (Test-GitHubPackagesToken -Token $Token)) {
    throw "GitHub Packages rejected the token. It must be a personal access token with the read:packages scope."
}

# Re-adding is the only reliable way to replace credentials for an existing source.
dotnet nuget remove source $Name 2>$null | Out-Null

dotnet nuget add source $GitHubPackagesSourceUrl `
    --name $Name `
    --username $Owner `
    --password $Token `
    --store-password-in-clear-text

if ($LASTEXITCODE -ne 0) {
    throw "Failed to register the GitHub Packages source. Exit code: $LASTEXITCODE"
}

Write-Host "Registered '$Name' -> $GitHubPackagesSourceUrl"

$env:GITHUB_PACKAGES_TOKEN = $Token
$probe = @(Get-GitHubPackageVersions -PackageId "IV.DX")
if ($probe.Count -eq 0) {
    Write-Warning "The feed works, but no IV.DX versions came back - the package has not been published under '$Owner' yet."
}
else {
    Write-Host "Latest IV.DX on GitHub Packages: $(Get-HighestSemVer -Versions $probe)"
}
