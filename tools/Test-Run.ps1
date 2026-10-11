# Dev test: start the game if needed, wait for the title screen, simulate game actions inside the
# game, and print what the mod logged. Nothing is typed at the desktop and the game need not be
# focused: actions go through the mod's dev bridge (DevBridge.cs; needs UserData\ShroomAccess\dev.flag).
#   -Press FocusNext,Confirm,wait2,Cancel     game action names, or waitN to pause N seconds
#   -Screenshot <png>  -Tree <txt>            after the presses, save the frame and/or the UI hierarchy
param(
    [string[]]$Press = @(),
    [int]$PressDelayMs = 900,
    [int]$SettleSeconds = 9,
    [switch]$Close,
    [switch]$Restart,
    [string]$Screenshot,
    [string]$Tree,
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
)
$ErrorActionPreference = 'Stop'
# Called with -File, a comma list arrives as one string.
$Press = @($Press | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$log = Join-Path $GameDir 'MelonLoader\Latest.log'
$devDir = Join-Path $GameDir 'UserData\ShroomAccess'

function Stop-Game {
    Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue | ForEach-Object { [void]$_.CloseMainWindow() }
    $until = (Get-Date).AddSeconds(8)
    while ((Get-Date) -lt $until -and (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 300 }
    Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue | Stop-Process -Force
}

function Send-Dev([string]$command, [string]$resultFile) {
    if ($resultFile -and (Test-Path $resultFile)) { Remove-Item $resultFile -Confirm:$false }
    $file = Join-Path $devDir 'dev-command.txt'
    $until = (Get-Date).AddSeconds(5)
    while ((Get-Date) -lt $until -and (Test-Path $file)) { Start-Sleep -Milliseconds 100 }
    Set-Content $file $command -Encoding ascii
    if ($resultFile) {
        $until = (Get-Date).AddSeconds(8)
        while ((Get-Date) -lt $until -and -not (Test-Path $resultFile)) { Start-Sleep -Milliseconds 200 }
        Start-Sleep -Milliseconds 400
    }
}

if ($Restart) { Stop-Game }

$proc = Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue | Select-Object -First 1
$startLine = 0
if (-not $proc) {
    Start-Process -FilePath (Join-Path $GameDir 'Shroom and Gloom.exe') -WorkingDirectory $GameDir
    $deadline = (Get-Date).AddMinutes(4)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 3
        $proc = Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $proc) { Write-Host 'Game exited during start.'; break }
        if ((Test-Path $log) -and ((Get-Content $log -Raw) -match 'Scene initialised: \d+ TitleScreen')) { break }
    }
    Start-Sleep -Seconds $SettleSeconds
} else {
    $startLine = (Get-Content $log).Count
}

if ($proc) {
    foreach ($p in $Press) {
        if ($p -match '^wait(\d+)$') { Start-Sleep -Seconds ([int]$Matches[1]); continue }
        Send-Dev "press $p" $null
        Start-Sleep -Milliseconds $PressDelayMs
    }
    if ($Press.Count -gt 0) { Start-Sleep -Seconds 1 }
    if ($Screenshot) { Send-Dev "shot $Screenshot" $Screenshot }
    if ($Tree) { Send-Dev "tree $Tree" $Tree }
}

if (Test-Path $log) {
    Get-Content $log | Select-Object -Skip $startLine | Where-Object { $_ -match 'Shroom_Access|ERROR|Exception' } |
        ForEach-Object { $_ -replace '^\[[\d:.]+\] \[Shroom_Access\] ', '' } |
        Where-Object { $_ -notmatch '^\[(asset|binding)\]|ArgumentNull' }
}

if ($Close) { Stop-Game }
