# Puts the game's save back to how it was after the very first launch (no runs, tutorial not done).
# The current save is copied to backups\<timestamp>-SaveData first, so nothing is lost.
# The game must be closed. Steam Cloud syncs this folder: on the next launch Steam may ask which
# copy to keep; choose the local files.
param(
    [string]$Fresh = (Join-Path (Split-Path $PSScriptRoot -Parent) 'backups\2026-10-11-fresh-install-SaveData')
)
$ErrorActionPreference = 'Stop'

$save = Join-Path $env:USERPROFILE 'AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) { Write-Host 'Close Shroom and Gloom first.'; exit 2 }
if (-not (Test-Path $Fresh)) { Write-Host "Fresh save copy not found: $Fresh"; exit 2 }
if (-not (Test-Path $save)) { Write-Host "Save folder not found: $save"; exit 2 }

$keep = Join-Path (Split-Path $PSScriptRoot -Parent) ('backups\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss') + '-SaveData')
Copy-Item $save $keep -Recurse
Write-Host "Current save copied to $keep"

Get-ChildItem $save -Force | Remove-Item -Recurse -Force -Confirm:$false
Copy-Item (Join-Path $Fresh '*') $save -Recurse -Force
Write-Host 'RESET OK'
