#!/usr/bin/env pwsh
param(
    [string]$ProjectPath = "src/IV.ManagementHub/IV.ManagementHub.AppHost/IV.ManagementHub.AppHost.csproj",
    [string]$SolutionPath = "src/IV.ManagementHub/IV.ManagementHub.sln",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$DxVersion,
    [switch]$SkipDxSync,
    [switch]$RemoveBootstrapSettings,
    [switch]$NoRestore,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiServiceRoot = Join-Path $repoRoot "src/IV.ManagementHub/IV.ManagementHub.ApiService"

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

if ($RemoveBootstrapSettings) {
    Write-Host "Removing bootstrap settings files..."

    $bootstrapFiles = Get-ChildItem `
        -Path $apiServiceRoot `
        -Recurse `
        -Filter "bootstrap.settings.json" `
        -File `
        -ErrorAction SilentlyContinue

    if ($bootstrapFiles.Count -eq 0) {
        Write-Host "No bootstrap settings files found."
    }
    else {
        foreach ($file in $bootstrapFiles) {
            Remove-Item -LiteralPath $file.FullName -Force
            Write-Host "Removed: $($file.FullName)"
        }
    }
}

if (-not $NoBuild) {
    $buildParams = @{
        SolutionPath   = $SolutionPath
        Configuration  = $Configuration
    }

    if ($DxVersion)  { $buildParams.DxVersion  = $DxVersion }
    if ($SkipDxSync) { $buildParams.SkipDxSync = $true }

    & "$PSScriptRoot/build.ps1" @buildParams
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed. Exit code: $LASTEXITCODE"
    }
}

$dotnetArgs = @(
    "run",
    "--project", $ProjectPath,
    "-c", $Configuration,
    "--no-build"
)

if ($NoRestore -or -not $NoBuild) {
    $dotnetArgs += "--no-restore"
}

Write-Host "Starting application..."
Write-Host "dotnet $($dotnetArgs -join ' ')"

Invoke-DotNet -Args $dotnetArgs -ErrorContext "Application run failed."
