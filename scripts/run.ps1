#!/usr/bin/env pwsh
param(
    [string]$WebPath      = "src/IV.DX.ManagementHub/IV.DX.ManagementHub.Web/IV.DX.ManagementHub.Web.csproj",
    [string]$SolutionPath = "src/IV.DX.ManagementHub/IV.DX.ManagementHub.sln",

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
$webRoot = Join-Path $repoRoot "src/IV.DX.ManagementHub/IV.DX.ManagementHub.Web"

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
        -Path $webRoot `
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
        SolutionPath  = $SolutionPath
        Configuration = $Configuration
    }

    if ($DxVersion)  { $buildParams.DxVersion  = $DxVersion }
    if ($SkipDxSync) { $buildParams.SkipDxSync = $true }

    & "$PSScriptRoot/build.ps1" @buildParams
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed. Exit code: $LASTEXITCODE"
    }
}

$noRestoreFlag = $NoRestore -or (-not $NoBuild)

$webArgs = @("run", "--project", $WebPath, "-c", $Configuration, "--no-build")
if ($noRestoreFlag) { $webArgs += "--no-restore" }

Write-Host "Starting Web..."
Write-Host "dotnet $($webArgs -join ' ')"
Invoke-DotNet -Args $webArgs -ErrorContext "Web run failed."
