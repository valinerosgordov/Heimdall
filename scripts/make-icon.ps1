# Generates assets\heimdall.ico (dark chassis + crimson border + "//" mark) used by the desktop exe + shortcut.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing

$assets = Join-Path $root "assets"
New-Item -ItemType Directory -Force -Path $assets | Out-Null
$icoPath = Join-Path $assets "heimdall.ico"

$bmp = New-Object System.Drawing.Bitmap 64, 64
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
$g.Clear([System.Drawing.ColorTranslator]::FromHtml("#0A0A0F"))
$crimson = [System.Drawing.ColorTranslator]::FromHtml("#FF1E2D")
$pen = New-Object System.Drawing.Pen($crimson, 3)
$g.DrawRectangle($pen, 5, 5, 53, 53)
$font = New-Object System.Drawing.Font("Consolas", 28, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush($crimson)
$g.DrawString("//", $font, $brush, 6, 10)
$g.Dispose()

$hicon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hicon)
$fs = [System.IO.File]::Create($icoPath)
$icon.Save($fs)
$fs.Close()
$icon.Dispose()
$bmp.Dispose()
Write-Host "[Heimdall] Icon: $icoPath" -ForegroundColor Green
