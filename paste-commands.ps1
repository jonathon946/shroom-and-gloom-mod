# Shroom and Gloom accessibility - Spike 1, step 1: install the MelonLoader mod loader.
#
# Run the whole file from a normal (not administrator) PowerShell window:
#
#   powershell -ExecutionPolicy Bypass -File "C:\Users\Sam\Documents\projects\shroom and gloom mod\paste-commands.ps1"
#
# What it does, in order:
#   1. Downloads MelonLoader 0.7.3 (x64) from its official GitHub release into this project's deps folder.
#   2. Checks the download against the SHA256 that GitHub publishes for that release file. Stops if it differs.
#   3. Writes down what was in the game folder beforehand, so the loader can be removed cleanly.
#   4. Unpacks the loader into the game folder. No game file is replaced.
#
# It does NOT start the game. When it prints INSTALL OK, tell Claude "loader installed".
#
# To undo by hand at any time: close the game, then delete from the game folder
#   version.dll, dobby.dll (if present) and the folders MelonLoader, Mods, Plugins, UserData, UserLibs.
# Or in Steam: Properties, Installed Files, Verify integrity of game files.

$ErrorActionPreference = 'Stop'

$project = 'C:\Users\Sam\Documents\projects\shroom and gloom mod'
$game    = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
$url     = 'https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip'
$sha256  = '5b2b2f3d1cd42b59ec886c5bdc2663edae87a0097a4f4a8f58c0965a99dda416'
$deps    = Join-Path $project 'deps\melonloader-0.7.3'
$zip     = Join-Path $deps 'MelonLoader.x64.zip'

if (-not (Test-Path (Join-Path $game 'GameAssembly.dll'))) { throw "Game not found at $game" }
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) { throw 'Close Shroom and Gloom first, then run this again.' }

New-Item -ItemType Directory -Force $deps | Out-Null

if (-not (Test-Path $zip)) {
    Write-Host 'Downloading MelonLoader 0.7.3 (about 20 MB)...'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
}

$actual = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
if ($actual -ne $sha256) {
    Remove-Item $zip -Confirm:$false
    throw "Checksum mismatch. Expected $sha256 but got $actual. The download was deleted; nothing was installed."
}
Write-Host 'Checksum matches the published release.'

$before = Join-Path $project 'deps\game-root-before-loader.txt'
if (-not (Test-Path $before)) {
    Get-ChildItem $game -Force | Select-Object -ExpandProperty Name | Set-Content $before -Encoding utf8
}

Write-Host 'Unpacking into the game folder...'
Expand-Archive -Path $zip -DestinationPath $game -Force

$added = Get-ChildItem $game -Force | Select-Object -ExpandProperty Name | Where-Object { (Get-Content $before) -notcontains $_ }
$added | Set-Content (Join-Path $project 'deps\game-root-added-by-loader.txt') -Encoding utf8

if (Test-Path (Join-Path $game 'version.dll')) {
    Write-Host ''
    Write-Host ('Added to the game folder: ' + ($added -join ', '))
    Write-Host 'INSTALL OK. Tell Claude: loader installed.'
} else {
    throw 'version.dll is not in the game folder after unpacking. Tell Claude what this window printed.'
}
