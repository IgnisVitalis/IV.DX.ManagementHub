#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$SolutionPath = "src/IV.ManagementHub/IV.ManagementHub.sln"

dotnet restore $SolutionPath
dotnet build  $SolutionPath -c Release