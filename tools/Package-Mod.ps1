# Builds a release folder and zip of the mod: only what a player needs, laid out as it sits in the
# game folder. Nothing from this machine's testing goes in (no dev flag, marks, pictures or logs).
# The player still needs MelonLoader 0.7.x installed in the game; the README in the zip says how.
param(
    # Defaults to the version the mod reports (UpdateCheck.Version), so the two cannot drift apart.
    [string]$Version = ''
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
if (-not $Version) {
    $source = Get-Content (Join-Path $root 'mod\ShroomAccess\UpdateCheck.cs') -Raw
    if ($source -match 'const string Version = "([0-9.]+)"') { $Version = $Matches[1] } else { Write-Host 'Version not found in UpdateCheck.cs'; exit 2 }
}
$dll = Join-Path $root 'mod\ShroomAccess\bin\ShroomAccess.dll'
$prism = Join-Path $root 'deps\prism-0.18.3'
if (-not (Test-Path $dll)) { Write-Host 'Build first: tools\Build-Mod.ps1'; exit 2 }

$out = Join-Path $root "dist\ShroomAccess-$Version"
if (Test-Path $out) { Remove-Item $out -Recurse -Force -Confirm:$false }
New-Item -ItemType Directory -Force (Join-Path $out 'Mods\ShroomAccess'), (Join-Path $out 'UserLibs') | Out-Null

Copy-Item $dll (Join-Path $out 'Mods')
Copy-Item (Join-Path $root 'mod\ShroomAccess\data\*') (Join-Path $out 'Mods\ShroomAccess')
Copy-Item (Join-Path $prism 'prism.dll') (Join-Path $out 'UserLibs')
Copy-Item (Join-Path $prism 'prism-licenses') (Join-Path $out 'UserLibs') -Recurse
Copy-Item (Join-Path $root 'mod\README.txt') $out

$zip = "$out.zip"
if (Test-Path $zip) { Remove-Item $zip -Confirm:$false }
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
Get-ChildItem $out -Recurse -File | ForEach-Object { $_.FullName.Substring($out.Length + 1) }
Write-Host "PACKAGED $zip"
