# Creates a "Heimdall" shortcut on the Desktop that launches the desktop app (Heimdall.Desktop.exe).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

# Ensure the icon exists.
& (Join-Path $PSScriptRoot "make-icon.ps1")
$icoPath = Join-Path $root "assets\heimdall.ico"
$exePath = Join-Path $root "dist\desktop\Heimdall.Desktop.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "[Heimdall] Desktop exe not found - run scripts\build-heimdall.ps1 first." -ForegroundColor Yellow
}

$desktop = [Environment]::GetFolderPath("Desktop")
$lnkPath = Join-Path $desktop "Heimdall.lnk"
$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($lnkPath)
$lnk.TargetPath = $exePath
$lnk.WorkingDirectory = Join-Path $root "dist\desktop"
$lnk.IconLocation = "$icoPath,0"
$lnk.Description = "Heimdall - server and project monitoring"
$lnk.WindowStyle = 1
$lnk.Save()
Write-Host "[Heimdall] Desktop shortcut created: $lnkPath" -ForegroundColor Green
