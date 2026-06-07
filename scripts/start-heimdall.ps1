# Alternative "browser mode": starts the backend (TimescaleDB + API + Agent) and opens the dashboard
# in your default browser. The normal way to run Heimdall is the desktop app (Heimdall.Desktop.exe).
$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $PSScriptRoot
$env:ASPNETCORE_ENVIRONMENT = "Development"

if (-not (Test-Path "$root\dist\api\Heimdall.Api.exe") -or -not (Test-Path "$root\dist\api\wwwroot\index.html")) {
    Write-Host "[Heimdall] Artifacts missing. Run scripts\build-heimdall.ps1 first." -ForegroundColor Yellow
    Read-Host "Press ENTER to exit"; exit 1
}

if (-not (docker info 2>$null)) {
    $dd = "$env:ProgramFiles\Docker\Docker\Docker Desktop.exe"
    if (Test-Path $dd) { Start-Process $dd }
    Write-Host "[Heimdall] Waiting for the Docker engine..." -ForegroundColor Cyan
    for ($i = 0; $i -lt 90; $i++) { if (docker info 2>$null) { break }; Start-Sleep 3 }
}
Push-Location $root
docker compose up -d | Out-Null
for ($i = 0; $i -lt 30; $i++) {
    if ((docker inspect --format '{{.State.Health.Status}}' heimdall-timescaledb 2>$null) -eq 'healthy') { break }
    Start-Sleep 2
}

Write-Host "[Heimdall] Starting API + Agent (API also serves the dashboard)..." -ForegroundColor Cyan
$api   = Start-Process -PassThru -WindowStyle Minimized -WorkingDirectory "$root\dist\api"   -FilePath "$root\dist\api\Heimdall.Api.exe"
$agent = Start-Process -PassThru -WindowStyle Minimized -WorkingDirectory "$root\dist\agent" -FilePath "$root\dist\agent\Heimdall.Agent.exe"

Write-Host "[Heimdall] Waiting for the dashboard..." -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 60; $i++) {
    try { if ((Invoke-WebRequest "http://localhost:5087/health" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { $ready = $true; break } } catch {}
    Start-Sleep 2
}
if ($ready) {
    Start-Process "http://localhost:5087"
    Write-Host "[Heimdall] Running at http://localhost:5087   (login: admin / heimdall)" -ForegroundColor Green
} else {
    Write-Host "[Heimdall] API did not respond in time; check the minimized windows." -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Press ENTER to STOP Heimdall"

function Stop-ByPort([int]$port) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
}
Stop-ByPort 5087
if ($api)   { Stop-Process -Id $api.Id   -Force -ErrorAction SilentlyContinue }
if ($agent) { Stop-Process -Id $agent.Id -Force -ErrorAction SilentlyContinue }
docker compose stop | Out-Null
Pop-Location
Write-Host "[Heimdall] Stopped (TimescaleDB data is preserved)." -ForegroundColor Green
