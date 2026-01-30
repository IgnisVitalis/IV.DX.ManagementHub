#!/usr/bin/env pwsh
param(
    [string]$SolutionPath = "src/IV.ManagementHub/IV.ManagementHub.sln",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$ErrorsOnly
)

$ErrorActionPreference = "Stop"

Write-Host "Building solution: $SolutionPath"
Write-Host "Configuration: $Configuration"

dotnet restore $SolutionPath

if ($ErrorsOnly) {
    Write-Host "Mode: errors only"
    dotnet build $SolutionPath `
        -c $Configuration `
        -v:q `
        -p:WarningLevel=0
}
else {
    Write-Host "Mode: full output"
    dotnet build $SolutionPath `
        -c $Configuration
}
