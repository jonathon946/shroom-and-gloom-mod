# Compiles mod\ShroomAccess into mod\ShroomAccess\bin\ShroomAccess.dll.
# Drives the .NET SDK's C# compiler directly against the installed .NET 6 runtime and the loader's
# assemblies. No NuGet restore: this machine has no package source configured.
# Prints BUILD OK or the compiler errors. Exit code 0 on success.
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom',
    # The loader's managed folder. Override to compile before the loader is installed in this game.
    [string]$MelonDir,
    # Extra source folders or reference DLLs, for test builds.
    [string[]]$ExtraReferences = @()
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root 'mod\ShroomAccess'
$out = Join-Path $src 'bin'
if (-not $MelonDir) { $MelonDir = Join-Path $GameDir 'MelonLoader\net6' }
if (-not (Test-Path (Join-Path $MelonDir 'MelonLoader.dll'))) { Write-Host "MelonLoader.dll not found in $MelonDir"; exit 2 }

$dotnet = 'C:\Program Files\dotnet'
$csc = Get-ChildItem (Join-Path $dotnet 'sdk') -Directory | Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'Roslyn\bincore\csc.dll' } | Where-Object { Test-Path $_ } | Select-Object -First 1
$runtime = Get-ChildItem (Join-Path $dotnet 'shared\Microsoft.NETCore.App') -Directory |
    Where-Object Name -like '6.*' | Sort-Object Name -Descending | Select-Object -First 1
if (-not $csc -or -not $runtime) { Write-Host 'Need a .NET SDK and the .NET 6 runtime.'; exit 2 }

function Test-Managed($path) {
    try { [void][Reflection.AssemblyName]::GetAssemblyName($path); $true } catch { $false }
}

$refs = @(Get-ChildItem $runtime.FullName -Filter *.dll | Where-Object { Test-Managed $_.FullName } | ForEach-Object FullName)
$refs += Join-Path $MelonDir 'MelonLoader.dll'
foreach ($name in '0Harmony.dll', 'Il2CppInterop.Runtime.dll', 'Il2CppInterop.Common.dll') {
    $p = Join-Path $MelonDir $name
    if (Test-Path $p) { $refs += $p }
}
# Generated game and Unity types; present once the loader has run the game once.
$interop = Join-Path $GameDir 'MelonLoader\Il2CppAssemblies'
if (Test-Path $interop) { $refs += Get-ChildItem $interop -Filter *.dll | ForEach-Object FullName }
$refs += $ExtraReferences

New-Item -ItemType Directory -Force $out | Out-Null
$sources = Get-ChildItem $src -Recurse -Filter *.cs | Where-Object { $_.FullName -notlike "$out*" } | ForEach-Object FullName

$rsp = Join-Path $out 'build.rsp'
$lines = @(
    '-nologo', '-nostdlib', '-target:library', '-optimize+', '-debug-', '-deterministic',
    '-langversion:latest', '-unsafe', '-nowarn:CS1701,CS1702',
    "-pathmap:`"$root`"=.",
    "-out:`"$(Join-Path $out 'ShroomAccess.dll')`""
)
$lines += $refs | ForEach-Object { "-r:`"$_`"" }
$lines += $sources | ForEach-Object { "`"$_`"" }
Set-Content $rsp $lines -Encoding utf8

& (Join-Path $dotnet 'dotnet.exe') $csc -noconfig "@$rsp"
if ($LASTEXITCODE -eq 0) { Write-Host 'BUILD OK'; exit 0 } else { Write-Host 'BUILD FAILED'; exit 1 }
