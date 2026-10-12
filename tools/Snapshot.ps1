# Named copies of the game's save, for returning to a situation on demand.
#   Snapshot.ps1 -Save camp-with-grimoire     copy the current save to backups\snapshots\camp-with-grimoire
#   Snapshot.ps1 -Load camp-with-grimoire     put that copy back (the current save is replaced)
#   Snapshot.ps1                              list the snapshots
# The game must be closed. The game saves at checkpoints (entering a tunnel, after a fight), so a
# snapshot returns to the last checkpoint before it was taken, not to the exact moment.
param(
    [string]$Save,
    [string]$Load
)
$ErrorActionPreference = 'Stop'

$live = Join-Path $env:USERPROFILE 'AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\SaveData'
$store = Join-Path (Split-Path $PSScriptRoot -Parent) 'backups\snapshots'
New-Item -ItemType Directory -Force $store | Out-Null

if (-not $Save -and -not $Load) {
    Get-ChildItem $store -Directory | ForEach-Object { '{0}  ({1:yyyy-MM-dd HH:mm})' -f $_.Name, $_.LastWriteTime }
    exit 0
}
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) { Write-Host 'Close Shroom and Gloom first.'; exit 2 }

if ($Save) {
    $to = Join-Path $store $Save
    if (Test-Path $to) { Remove-Item $to -Recurse -Force -Confirm:$false }
    Copy-Item $live $to -Recurse
    Write-Host "SAVED $Save"
} else {
    $from = Join-Path $store $Load
    if (-not (Test-Path $from)) { Write-Host "No snapshot named $Load"; exit 2 }
    Get-ChildItem $live -Force | Remove-Item -Recurse -Force -Confirm:$false
    Copy-Item (Join-Path $from '*') $live -Recurse -Force
    Write-Host "LOADED $Load"
}
