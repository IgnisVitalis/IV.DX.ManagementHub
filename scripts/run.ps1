#!/usr/bin/env pwsh
param(
    [string]$WebPath      = "src/IV.DX.ManagementHub/IV.DX.ManagementHub.ApiService/IV.DX.ManagementHub.ApiService.csproj",
    [string]$WebAppPath   = "src/IV.DX.ManagementHub/IV.DX.ManagementHub.WebApp",
    [string]$SolutionPath = "src/IV.DX.ManagementHub/IV.DX.ManagementHub.sln",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    # Profile from Properties/launchSettings.json. "https" binds both 7097 (https)
    # and 5286 (http); the Angular dev proxy targets the https one.
    [string]$LaunchProfile = "https",

    [int]$AngularPort = 4200,
    [int]$WebReadyTimeoutSeconds = 120,

    [string]$DxVersion,
    [switch]$SkipDxSync,
    [switch]$RemoveBootstrapSettings,
    [switch]$NoRestore,
    [switch]$NoBuild,

    # Run only one side.
    [switch]$SkipAngular,
    [switch]$SkipWeb,

    [switch]$NoInstall,
    [switch]$Open
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$webRoot = Join-Path $repoRoot "src/IV.DX.ManagementHub/IV.DX.ManagementHub.ApiService"

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

# Reads applicationUrl of the selected launch profile so the readiness probe and
# the proxy check use the real ports instead of hardcoded ones.
function Get-LaunchProfileUrls {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectDir,
        [Parameter(Mandatory = $true)][string]$ProfileName
    )

    $launchSettings = Join-Path $ProjectDir "Properties/launchSettings.json"
    if (-not (Test-Path $launchSettings)) { return @() }

    $profiles = (Get-Content -Raw $launchSettings | ConvertFrom-Json).profiles
    $selected = $profiles.PSObject.Properties |
        Where-Object { $_.Name -eq $ProfileName } |
        Select-Object -First 1

    if (-not $selected -or -not $selected.Value.applicationUrl) { return @() }

    return $selected.Value.applicationUrl -split ';' |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ }
}

# Any HTTP answer means Kestrel is listening; status code is irrelevant here
# (the app redirects to /login and may return 302/401).
function Wait-ForUrl {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds,
        [System.Diagnostics.Process]$Process
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        if ($Process -and $Process.HasExited) {
            throw "Web exited before becoming ready. Exit code: $($Process.ExitCode)"
        }

        try {
            Invoke-WebRequest -Uri $Url `
                -SkipCertificateCheck `
                -SkipHttpErrorCheck `
                -MaximumRedirection 0 `
                -TimeoutSec 5 | Out-Null
            return $true
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    return $false
}

# Without this the script happily attaches to whatever already answers on the
# port — another dev-server, or a stale instance from a previous run — and the
# freshly started one silently fails to bind. Everything then appears to work
# while serving from the wrong process.
function Assert-PortFree {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$What
    )

    $port = ([Uri]$Url).Port

    # TcpClient rather than Get-NetTCPConnection: the latter is Windows-only.
    # A refused connection faults the task, which is exactly the free-port case.
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connect = $client.ConnectAsync('127.0.0.1', $port)
        $inUse = $connect.Wait(500) -and -not $connect.IsFaulted
    }
    catch {
        $inUse = $false
    }
    finally {
        $client.Dispose()
    }

    if ($inUse) {
        throw "Port $port is already in use, so $What cannot start there. Stop the other instance (or pass -SkipWeb / -AngularPort) and try again."
    }
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    if (-not $Process -or $Process.HasExited) { return }

    Write-Host "Stopping Web (PID $($Process.Id))..."
    try {
        # $true => kill children too; "dotnet run" spawns the actual app process.
        $Process.Kill($true)
        $Process.WaitForExit(10000) | Out-Null
    }
    catch {
        Write-Warning "Failed to stop Web: $($_.Exception.Message)"
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

# --- Build (.NET) ---

if (-not $NoBuild -and -not $SkipWeb) {
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

# --- Build (Angular dependencies) ---

$webAppRoot = Join-Path $repoRoot $WebAppPath

if (-not $SkipAngular) {
    if (-not (Test-Path $webAppRoot)) {
        throw "Angular project not found: $webAppRoot"
    }

    if (-not $NoInstall -and -not (Test-Path (Join-Path $webAppRoot "node_modules"))) {
        Write-Host "Installing Angular dependencies (npm install)..."
        Push-Location $webAppRoot
        try {
            & npm install
            if ($LASTEXITCODE -ne 0) {
                throw "npm install failed. Exit code: $LASTEXITCODE"
            }
        }
        finally {
            Pop-Location
        }
    }
}

# --- Run ---

$webArgs = @("run", "--project", $WebPath, "-c", $Configuration, "--launch-profile", $LaunchProfile, "--no-build")
if ($NoRestore -or (-not $NoBuild)) { $webArgs += "--no-restore" }

$webProjectDir = Split-Path -Parent (Join-Path $repoRoot $WebPath)
$urls = Get-LaunchProfileUrls -ProjectDir $webProjectDir -ProfileName $LaunchProfile
$httpsUrl = $urls | Where-Object { $_ -like "https://*" } | Select-Object -First 1
$probeUrl = if ($httpsUrl) { $httpsUrl } else { $urls | Select-Object -First 1 }

if (-not $SkipWeb) {
    if (-not $probeUrl) {
        throw "Could not resolve applicationUrl for launch profile '$LaunchProfile'."
    }

    Assert-PortFree -Url $probeUrl -What 'Web'
}

# Angular skipped => keep the original behaviour: Web in the foreground.
if ($SkipAngular) {
    Write-Host "Starting Web..."
    Write-Host "dotnet $($webArgs -join ' ')"
    Invoke-DotNet -Args $webArgs -ErrorContext "Web run failed."
    return
}

$webProcess = $null

try {
    if (-not $SkipWeb) {
        Write-Host "Starting Web (profile '$LaunchProfile')..."
        Write-Host "dotnet $($webArgs -join ' ')"

        $webProcess = Start-Process -FilePath "dotnet" `
            -ArgumentList $webArgs `
            -WorkingDirectory $repoRoot `
            -NoNewWindow `
            -PassThru

        Write-Host "Waiting for $probeUrl (timeout ${WebReadyTimeoutSeconds}s)..."
        if (-not (Wait-ForUrl -Url $probeUrl -TimeoutSeconds $WebReadyTimeoutSeconds -Process $webProcess)) {
            throw "Web did not become ready within $WebReadyTimeoutSeconds seconds."
        }

        Write-Host ""
        Write-Host "API ready: $probeUrl" -ForegroundColor Green

        # Hand the resolved address to the dev-server proxy so the two cannot
        # disagree about where the API lives (see proxy.conf.mjs).
        $env:MH_API_URL = $probeUrl
    }

    Assert-PortFree -Url "http://localhost:$AngularPort" -What 'the Angular dev server'

    $ngArgs = @("start", "--", "--port", $AngularPort)
    if ($Open) { $ngArgs += "--open" }

    # The Angular CLI blocks on consent prompts when stdout is a terminal, which
    # would hang the script. Analytics is already off in angular.json; the
    # autocompletion prompt is global-config-only, so it has to be silenced here.
    $env:NG_CLI_ANALYTICS = "false"
    $env:NG_FORCE_AUTOCOMPLETE = "false"

    Write-Host "Starting Angular dev server on http://localhost:$AngularPort ..." -ForegroundColor Green
    Write-Host "Press Ctrl+C to stop both."
    Write-Host ""

    Push-Location $webAppRoot
    try {
        & npm @ngArgs
    }
    finally {
        Pop-Location
    }
}
finally {
    Stop-ProcessTree -Process $webProcess
}
