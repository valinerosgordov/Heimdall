# Packages a self-contained, shareable Heimdall release as a single .zip.
# The recipient does NOT need the .NET SDK/runtime (publish is self-contained),
# but DOES need Docker Desktop (for TimescaleDB) and the WebView2 Runtime
# (preinstalled on Windows 11). Output: release/Heimdall-<timestamp>.zip
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$rid = "win-x64"
Push-Location $root
try {
    $stamp = Get-Date -Format "yyyyMMdd-HHmm"
    $stage = Join-Path $root "release/Heimdall"
    if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
    $distApi = Join-Path $stage "dist/api"
    $distAgent = Join-Path $stage "dist/agent"
    $distDesktop = Join-Path $stage "dist/desktop"

    Write-Host "[package] Publishing self-contained API + Agent + Desktop ($rid)..." -ForegroundColor Cyan
    dotnet publish src/Heimdall.Api     -c Release -r $rid --self-contained true -o $distApi
    dotnet publish src/Heimdall.Agent   -c Release -r $rid --self-contained true -o $distAgent
    dotnet publish src/Heimdall.Desktop -c Release -r $rid --self-contained true -o $distDesktop

    Write-Host "[package] Building the dashboard (static export)..." -ForegroundColor Cyan
    Push-Location src/Heimdall.Web
    try {
        if (-not (Test-Path node_modules)) { npm install }
        npm run build
    } finally { Pop-Location }

    Write-Host "[package] Embedding the dashboard into the API (wwwroot)..." -ForegroundColor Cyan
    $wwwroot = Join-Path $distApi "wwwroot"
    if (Test-Path $wwwroot) { Remove-Item -Recurse -Force $wwwroot }
    New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
    Copy-Item -Recurse -Force (Join-Path $root "src/Heimdall.Web/out/*") $wwwroot

    Write-Host "[package] Staging compose file + launcher..." -ForegroundColor Cyan
    # Desktop resolves docker-compose.yml at <exeDir>/../.. so it must sit at the package root.
    Copy-Item -Force (Join-Path $root "docker-compose.yml") (Join-Path $stage "docker-compose.yml")

    $launcher = @'
@echo off
REM Heimdall launcher. Requires Docker Desktop running.
start "" "%~dp0dist\desktop\Heimdall.Desktop.exe"
'@
    Set-Content -Path (Join-Path $stage "Run Heimdall.bat") -Value $launcher -Encoding ascii

    $readme = @'
HEIMDALL - self-contained desktop build
=======================================

Requirements on the target machine:
  1. Docker Desktop (running) - hosts the TimescaleDB database.
  2. Windows 10/11. WebView2 Runtime is preinstalled on Windows 11;
     on Windows 10 install it from https://developer.microsoft.com/microsoft-edge/webview2/
  (No .NET install needed - this build is self-contained.)

To run:
  Double-click "Run Heimdall.bat" (or dist\desktop\Heimdall.Desktop.exe).
  The app starts TimescaleDB in Docker, boots the backend, and opens the
  dashboard in its own window. Default login: admin / heimdall.
  Closing the window stops everything (your data is preserved in Docker).

SECURITY: this build ships throwaway dev credentials (admin / heimdall) and a
default signing secret. Change them before exposing Heimdall to any network.
'@
    Set-Content -Path (Join-Path $stage "READ ME FIRST.txt") -Value $readme -Encoding ascii

    Write-Host "[package] Zipping..." -ForegroundColor Cyan
    $zip = Join-Path $root "release/Heimdall-$stamp.zip"
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip

    $size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
    Write-Host "[package] Done: $zip ($size MB)" -ForegroundColor Green
    Write-Host "[package] Recipient needs Docker Desktop; no .NET install required." -ForegroundColor Green
} finally { Pop-Location }
