# Copies the built mod and Prism into the game's loader folders, or removes them with -Remove.
# The game must be closed: Windows will not overwrite a DLL the game has loaded.
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom',
    [switch]$Remove
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$mods = Join-Path $GameDir 'Mods'
$libs = Join-Path $GameDir 'UserLibs'
$targets = @(
    (Join-Path $mods 'ShroomAccess.dll'),
    (Join-Path $libs 'prism.dll'),
    (Join-Path $libs 'prism-licenses')
)

if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) { Write-Host 'Close Shroom and Gloom first.'; exit 2 }

if ($Remove) {
    foreach ($t in $targets) { if (Test-Path $t) { Remove-Item $t -Recurse -Force -Confirm:$false; Write-Host "Removed $t" } }
    Write-Host 'REMOVE OK'
    exit 0
}

if (-not (Test-Path (Join-Path $GameDir 'MelonLoader'))) { Write-Host 'The loader is not installed in the game folder.'; exit 2 }
$dll = Join-Path $root 'mod\ShroomAccess\bin\ShroomAccess.dll'
$prism = Join-Path $root 'deps\prism-0.18.3'
if (-not (Test-Path $dll)) { Write-Host 'Build first: tools\Build-Mod.ps1'; exit 2 }
if (-not (Test-Path (Join-Path $prism 'prism.dll'))) { Write-Host "prism.dll is missing from $prism"; exit 2 }

New-Item -ItemType Directory -Force $mods, $libs | Out-Null
Copy-Item $dll $targets[0] -Force
Copy-Item (Join-Path $prism 'prism.dll') $targets[1] -Force
if (Test-Path $targets[2]) { Remove-Item $targets[2] -Recurse -Force -Confirm:$false }
Copy-Item (Join-Path $prism 'prism-licenses') $targets[2] -Recurse
Write-Host 'DEPLOY OK'
