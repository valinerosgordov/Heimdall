# Builds the deployable Heimdall: publishes API + Agent, static-exports the dashboard into the API's
# wwwroot, and publishes the WebView2 desktop app. Run once, and again after code changes.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    Write-Host "[Heimdall] Publishing API + Agent (Release)..." -ForegroundColor Cyan
    dotnet publish src/Heimdall.Api   -c Release -o dist/api
    dotnet publish src/Heimdall.Agent -c Release -o dist/agent

    Write-Host "[Heimdall] Building the dashboard (static export)..." -ForegroundColor Cyan
    Push-Location src/Heimdall.Web
    try {
        if (-not (Test-Path node_modules)) { npm install }
        npm run build
    } finally { Pop-Location }

    Write-Host "[Heimdall] Embedding the dashboard into the API (wwwroot)..." -ForegroundColor Cyan
    $wwwroot = Join-Path $root "dist/api/wwwroot"
    if (Test-Path $wwwroot) { Remove-Item -Recurse -Force $wwwroot }
    New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
    Copy-Item -Recurse -Force (Join-Path $root "src/Heimdall.Web/out/*") $wwwroot

    Write-Host "[Heimdall] Ensuring app icon..." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "make-icon.ps1")

    Write-Host "[Heimdall] Publishing the desktop app..." -ForegroundColor Cyan
    dotnet publish src/Heimdall.Desktop -c Release -o dist/desktop

    Write-Host "[Heimdall] Build complete. Run scripts\create-shortcut.ps1 once, then use the Desktop 'Heimdall' icon." -ForegroundColor Green
} finally { Pop-Location }
