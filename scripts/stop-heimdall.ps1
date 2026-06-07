# Stops a running Heimdall stack (use if you closed the launcher window without pressing ENTER).
$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $PSScriptRoot
function Stop-ByPort([int]$port) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
}
Write-Host "[Heimdall] Stopping API (5087) + desktop + agent..." -ForegroundColor Cyan
Stop-ByPort 5087
Get-Process Heimdall.Desktop -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
# Stop the agent (no listening port): match the published agent process.
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*Heimdall.Agent.dll*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Push-Location $root
docker compose stop | Out-Null
Pop-Location
Write-Host "[Heimdall] Stopped (TimescaleDB data is preserved)." -ForegroundColor Green
