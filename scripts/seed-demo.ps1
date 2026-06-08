# Seeds a realistic DEMO dataset (fictional servers / monitors / topology / alert rule) into a
# Heimdall instance for README and marketing screenshots.
#
# RUN ONLY against a FRESH / empty Heimdall instance -- this is demo content, not your real data.
# (Tip: start a throwaway DB, or `docker compose down -v` first, then launch the API.)
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\seed-demo.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\seed-demo.ps1 -BaseUrl http://localhost:5087 -Username demo -Password demodemo123
param(
    [string]$BaseUrl = "http://localhost:5087",
    [string]$Username = "demo",
    [string]$Password = "demodemo123"
)
$ErrorActionPreference = "Stop"

function Get-Token {
    $status = Invoke-RestMethod -Uri "$BaseUrl/api/auth/status"
    $body = @{ username = $Username; password = $Password } | ConvertTo-Json
    if (-not $status.configured) {
        Write-Host "[seed-demo] First run: creating operator '$Username'." -ForegroundColor Cyan
        return (Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/setup" -ContentType application/json -Body $body).accessToken
    }
    return (Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType application/json -Body $body).accessToken
}

$headers = @{ Authorization = "Bearer $(Get-Token)" }
$today = Get-Date
function DueIn([int]$days) { return $today.AddDays($days).ToString("yyyy-MM-dd") }

$monitors = @(
    @{ name = "api";      kind = "Http"; target = "https://api.example.com/health"; intervalSeconds = 30 },
    @{ name = "website";  kind = "Http"; target = "https://example.com";            intervalSeconds = 30 },
    @{ name = "postgres"; kind = "Tcp";  target = "db.example.com:5432";            intervalSeconds = 30 }
)
$monIds = @{}
foreach ($m in $monitors) {
    $r = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/healthchecks" -Headers $headers -ContentType application/json -Body ($m | ConvertTo-Json)
    $monIds[$m.name] = $r.id
}

$servers = @(
    @{ name = "lb-01";       provider = "Hetzner"; ipAddress = "10.0.0.10"; role = "Load balancer (nginx)";   cpuCores = 2; ramGb = 4;  diskGb = 80;  location = "FSN1"; monthlyCost = 8;  currency = "EUR"; paidUntil = (DueIn 5);  monitor = "website" },
    @{ name = "web-prod-01"; provider = "Hetzner"; ipAddress = "10.0.0.11"; role = "App server (.NET API)";   cpuCores = 4; ramGb = 8;  diskGb = 160; location = "FSN1"; monthlyCost = 24; currency = "EUR"; paidUntil = (DueIn 19); userCount = 1280; monitor = "api" },
    @{ name = "db-primary";  provider = "Hetzner"; ipAddress = "10.0.0.12"; role = "PostgreSQL 17 primary";   cpuCores = 8; ramGb = 32; diskGb = 512; location = "FSN1"; monthlyCost = 96; currency = "EUR"; paidUntil = (DueIn 2);  monitor = "postgres" },
    @{ name = "cache-01";    provider = "Hetzner"; ipAddress = "10.0.0.13"; role = "Redis cache";             cpuCores = 2; ramGb = 8;  diskGb = 40;  location = "FSN1"; monthlyCost = 12; currency = "EUR"; paidUntil = (DueIn 33) },
    @{ name = "vpn-gw";      provider = "Aeza";    ipAddress = "10.0.0.14"; role = "WireGuard VPN gateway";   cpuCores = 1; ramGb = 2;  diskGb = 20;  location = "AMS";  monthlyCost = 5;  currency = "USD"; paidUntil = (DueIn 11); userCount = 42 }
)
$srvIds = @{}
foreach ($s in $servers) {
    $b = @{}
    foreach ($kv in $s.GetEnumerator()) { if ($kv.Key -ne "monitor") { $b[$kv.Key] = $kv.Value } }
    if ($s.ContainsKey("monitor") -and $monIds.ContainsKey($s.monitor)) { $b["linkedHealthCheckId"] = $monIds[$s.monitor] }
    $r = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/servers" -Headers $headers -ContentType application/json -Body ($b | ConvertTo-Json)
    $srvIds[$s.name] = $r.id
}

$links = @(
    @{ from = "lb-01";       to = "web-prod-01"; kind = "proxy" },
    @{ from = "web-prod-01"; to = "db-primary";  kind = "database" },
    @{ from = "web-prod-01"; to = "cache-01";    kind = "cache" }
)
foreach ($l in $links) {
    $b = @{ fromServerId = $srvIds[$l.from]; toServerId = $srvIds[$l.to]; kind = $l.kind } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/servers/links" -Headers $headers -ContentType application/json -Body $b | Out-Null
}

$rule = @{ name = "High CPU"; metric = "cpu.usage"; operator = "gt"; threshold = 90; durationSeconds = 60; severity = "warning" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/alerts/rules" -Headers $headers -ContentType application/json -Body $rule | Out-Null

Write-Host "[seed-demo] Done: $($servers.Count) servers, $($monitors.Count) monitors, $($links.Count) links, 1 alert rule." -ForegroundColor Green
