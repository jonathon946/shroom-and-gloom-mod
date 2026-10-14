# Shroom Access installer. Run by "Install Shroom Access.cmd" in the same folder.
# Finds Shroom and Gloom through Steam, installs the .NET 6 runtime if it is missing (MelonLoader
# needs it), and copies MelonLoader, the mod and its speech library into the game folder.
# Everything it says is plain text, for a screen reader to read in the console window.
# PowerShell 5.1-safe.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$appId = '3271280'
$exeName = 'Shroom and Gloom.exe'

function Say([string]$text) { Write-Host $text }

function Finish([int]$code) {
    Say ''
    Say 'Press Enter to close this window.'
    [void](Read-Host)
    exit $code
}

# --- Find the game ---
function Find-Game {
    $roots = New-Object System.Collections.Generic.List[string]
    foreach ($key in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam') {
        try {
            $item = Get-ItemProperty -Path $key -ErrorAction Stop
            foreach ($name in 'SteamPath', 'InstallPath') {
                $value = $item.$name
                if ($value) { $roots.Add(($value -replace '/', '\')) }
            }
        } catch { }
    }
    $roots.Add('C:\Program Files (x86)\Steam')
    $libraries = New-Object System.Collections.Generic.List[string]
    foreach ($root in $roots) {
        $libraries.Add($root)
        $vdf = Join-Path $root 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $libraries.Add(($m.Groups[1].Value -replace '\\\\', '\'))
            }
        }
    }
    foreach ($lib in ($libraries | Select-Object -Unique)) {
        $manifest = Join-Path $lib "steamapps\appmanifest_$appId.acf"
        $dir = Join-Path $lib 'steamapps\common\Shroom and Gloom'
        if (Test-Path $manifest) {
            $m = [regex]::Match((Get-Content $manifest -Raw), '"installdir"\s+"([^"]+)"')
            if ($m.Success) { $dir = Join-Path $lib ('steamapps\common\' + $m.Groups[1].Value) }
        }
        if (Test-Path (Join-Path $dir $exeName)) { return $dir }
    }
    return $null
}

Say 'Shroom Access installer.'
Say ''

$game = Find-Game
if (-not $game) {
    Say 'I could not find Shroom and Gloom through Steam.'
    Say 'To find the game folder: in Steam, open the game''s Properties, Installed Files, Browse.'
    Say 'Copy the address of that folder from the Explorer address bar (Alt D, then Control C).'
    $typed = Read-Host 'Paste the folder address here and press Enter, or press Enter alone to stop'
    if (-not $typed) { Say 'Nothing was installed.'; Finish 1 }
    $game = $typed.Trim().Trim('"')
    if (-not (Test-Path (Join-Path $game $exeName))) { Say "There is no $exeName in that folder. Nothing was installed."; Finish 1 }
}
Say "Found the game in: $game"

if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Say 'The game is running. Close it, then press Enter to go on.'
    [void](Read-Host)
    if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) { Say 'The game is still running. Nothing was installed.'; Finish 1 }
}

# --- .NET 6 runtime ---
function Has-Net6 {
    foreach ($base in "$env:ProgramFiles\dotnet\shared\Microsoft.NETCore.App", "${env:ProgramFiles(x86)}\dotnet\shared\Microsoft.NETCore.App") {
        if ((Test-Path $base) -and (Get-ChildItem $base -Directory | Where-Object { $_.Name -like '6.*' })) { return $true }
    }
    return $false
}

if (Has-Net6) {
    Say 'The .NET 6 runtime is already installed.'
} else {
    Say 'MelonLoader needs the Microsoft .NET 6 runtime, which is not installed. Downloading it from Microsoft, about 28 megabytes.'
    $url = 'https://builds.dotnet.microsoft.com/dotnet/Runtime/6.0.36/dotnet-runtime-6.0.36-win-x64.exe'
    $sha512 = 'a8e493587d741dfc5ab3aeb548e8abae1bc180dfa28cc0aa4ddaf159bdd990644a97d5e987a17e25def1a41947938b0fabcfe35cf9d81df29b2619b54ec3a86c'
    $setup = Join-Path $env:TEMP 'dotnet-runtime-6.0.36-win-x64.exe'
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $url -OutFile $setup -UseBasicParsing
    } catch {
        Say "The download failed: $($_.Exception.Message)"
        Say 'Install the .NET 6 Desktop Runtime yourself from https://dotnet.microsoft.com/download/dotnet/6.0 and run this installer again.'
        Finish 1
    }
    if ((Get-FileHash $setup -Algorithm SHA512).Hash.ToLowerInvariant() -ne $sha512) {
        Remove-Item $setup -Force
        Say 'The downloaded file did not match Microsoft''s published checksum, so it was not run. Nothing was installed.'
        Finish 1
    }
    Say 'Installing .NET 6. Windows will ask for permission: choose Yes.'
    $p = Start-Process -FilePath $setup -ArgumentList '/install', '/quiet', '/norestart' -Verb RunAs -Wait -PassThru
    Remove-Item $setup -Force -ErrorAction SilentlyContinue
    if (-not (Has-Net6)) {
        Say "The .NET 6 install did not finish (code $($p.ExitCode)). Nothing else was installed. Run this installer again to retry."
        Finish 1
    }
    Say '.NET 6 is installed.'
}

# --- Copy the files ---
Say 'Copying MelonLoader, Shroom Access and its speech library into the game folder.'
try {
    foreach ($item in 'version.dll', 'MelonLoader', 'Mods', 'UserLibs') {
        $source = Join-Path $here $item
        if (-not (Test-Path $source)) { throw "the installer is missing $item; download the zip again" }
        Copy-Item -Path $source -Destination $game -Recurse -Force
    }
    # Files from the internet carry a download mark; clear it on what was copied.
    Get-ChildItem (Join-Path $game 'MelonLoader'), (Join-Path $game 'Mods'), (Join-Path $game 'UserLibs') -Recurse -File | Unblock-File -ErrorAction SilentlyContinue
    Unblock-File (Join-Path $game 'version.dll') -ErrorAction SilentlyContinue
} catch [System.UnauthorizedAccessException] {
    Say 'Windows did not allow writing to the game folder. Right-click "Install Shroom Access.cmd" and choose Run as administrator.'
    Finish 1
} catch {
    Say "Copying failed: $($_.Exception.Message)"
    Finish 1
}

Say ''
Say 'Shroom Access is installed.'
Say 'The first start takes a few minutes and is silent: MelonLoader prepares itself and needs the internet for that. Later starts are quick.'
Say 'You will hear "Shroom Access loaded" on the title screen. The keys are in README.txt and in the Help buffer in the game (Control Right until Help).'
Say ''
$answer = Read-Host 'Start the game now? Type Y and press Enter for yes, or just press Enter for no'
if ($answer -match '^\s*y') { Start-Process "steam://rungameid/$appId" }
exit 0
