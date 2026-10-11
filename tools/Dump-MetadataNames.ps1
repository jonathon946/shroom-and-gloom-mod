# Regenerates reference\ (gitignored) from the installed game's IL2CPP metadata.
# Read-only on the game folder. Names only: no code or assets are extracted.
#   reference\metadata-ordered.txt  every identifier, in definition order (a type's members sit together)
#   reference\script-files.txt      the game's .cs source file names, sorted
#   reference\build.txt             the build these lists were taken from
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
)
$ErrorActionPreference = 'Stop'

$meta = Join-Path $GameDir 'Shroom and Gloom_Data\il2cpp_data\Metadata\global-metadata.dat'
$out = Join-Path (Split-Path $PSScriptRoot -Parent) 'reference'
New-Item -ItemType Directory -Force $out | Out-Null

$bytes = [IO.File]::ReadAllBytes($meta)
$magic = [BitConverter]::ToUInt32($bytes, 0)
# Compared as text: PowerShell 5.1 reads the literal 0xFAB11BAF as a negative Int32.
if ($magic.ToString('X8') -ne 'FAB11BAF') { throw ('Not an IL2CPP metadata file (magic 0x{0:X8}).' -f $magic) }
$version = [BitConverter]::ToInt32($bytes, 4)

# Header: magic, version, then (offset, size) pairs; the identifier string table is the third pair.
$offset = [BitConverter]::ToInt32($bytes, 24)
$size = [BitConverter]::ToInt32($bytes, 28)
$ordered = [Text.Encoding]::UTF8.GetString($bytes, $offset, $size).Split([char]0)
[IO.File]::WriteAllLines((Join-Path $out 'metadata-ordered.txt'), $ordered)

# Source file names are stored outside the identifier table, so scan the whole file for them.
$all = [Text.Encoding]::GetEncoding(28591).GetString($bytes)
$scripts = [regex]::Matches($all, '(?<![A-Za-z0-9_])[A-Za-z0-9_]+\.cs(?=\x00)') |
    ForEach-Object { $_.Value } | Sort-Object -Unique
[IO.File]::WriteAllLines((Join-Path $out 'script-files.txt'), [string[]]$scripts)

$manifest = Join-Path (Split-Path (Split-Path $GameDir -Parent) -Parent) 'appmanifest_3271280.acf'
$buildId = 'unknown'
if (Test-Path $manifest) {
    $line = Get-Content $manifest | Where-Object { $_ -match '"buildid"' } | Select-Object -First 1
    if ($line -match '"buildid"\s+"(\d+)"') { $buildId = $Matches[1] }
}
$unity = (Get-Item (Join-Path $GameDir 'UnityPlayer.dll')).VersionInfo.ProductVersion
@(
    "Dumped:           $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    "Steam buildid:    $buildId"
    "Unity:            $unity"
    "Metadata version: $version"
    "Identifiers:      $($ordered.Count)"
    "Script files:     $($scripts.Count)"
) | Set-Content (Join-Path $out 'build.txt') -Encoding utf8

Get-Content (Join-Path $out 'build.txt')
