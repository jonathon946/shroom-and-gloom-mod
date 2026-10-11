> **Status:** Living reference (deduplicated index of what's been tried and settled; checked before retrying anything). **Last updated:** 2026-10-11, Session 01.

# Lessons & dead ends — Shroom and Gloom accessibility

Nothing has been tried in the running game yet. The entries below are findings from inspection and lessons carried in from sibling projects because they apply directly here. Each carried lesson names its source.

## Inspecting the game [Session 01]

- **TECHNIQUE — The IL2CPP metadata keeps the developers' names.** `global-metadata.dat` holds every class, method and field name and even the source file names. Splitting its identifier string table on null bytes gives the names in definition order, so a type's members sit next to its name. No dumper tool is needed for a first look. Script: `tools/Dump-MetadataNames.ps1`.
- **GOTCHA — The localisation bundles are LZ4-compressed.** Scanning them for text returns fragments only. Read text through the game at run time.
- **GOTCHA — The game uses a typewriter text effect (Febucci Text Animator).** Visible text is partial while it animates. Read the source string.
- **CHECK — "No screen-reader DLL" does not mean "no accessibility", and a stock Unity module name does not mean support.** `UnityEngine.AccessibilityModule` is in every recent Unity build.

## Loader [Session 01]

- **POINTER — Unity 2022.3.62 IL2CPP is a known trouble spot.** MelonLoader issue 1063 (crash at start on 2022.3.62f2, closed stale, no fix named); the Class of '09 project's `docs/known-issues.md` says BepInEx 6 Bleeding Edge crashed as well. Unconfirmed for 2022.3.62f3. Do not write mod code before Spike 1 passes.
- **POINTER — BepInEx 6 builds before about be.753 reject metadata version 31.** Use be.755 or later if BepInEx is tried.

## Carried from sibling projects

- **LESSON — One bad attribute patch can take every patch down** (Caves of Qud, lessons line 18). Patch by name, one try/catch each.
- **LESSON — Check the installed game build at every session start** (Dwarf Fortress, lessons line 298): Steam updated that game silently while the project was paused.
- **LESSON — Never dedupe player-initiated feedback; dedupe only unsolicited chatter** (Caves of Qud, lessons line 48).
- **LESSON — Interrupting speech cancels lines the player never heard** (Caves of Qud, lessons line 49): after a screen or popup title, queue the selection line.
- **LESSON — Trigger announcements on a change in game state, never on a change in rendered text** (Dwarf Fortress, lessons line 301).
- **DEAD END — Alt-based hotkeys** (Caves of Qud, lessons line 25). Ctrl+letter is the layer.
- **GOTCHA — NVDA takes the whole numpad when Num Lock is off** (Caves of Qud, lessons line 51).
- **LESSON — Write logs to a user-writable folder, never under Program Files** (Class of '09, lessons line 33).
- **LESSON — Speech is confined to the game's foreground window; compare the foreground window's process to our own** (Dwarf Fortress, lessons lines 126 and 127).
- **POINTER — Prism specifics** (Dwarf Fortress, lessons lines 379 and 380): one context, best backend, initialise, then speak and stop under one lock; it rejects invalid UTF-8; with NVDA 2026.2 its NVDA backend cannot report "still speaking".

## Building and tooling [Session 01]

- **GOTCHA — `dotnet build` cannot restore here.** No NuGet source is configured, so the .NET 6 reference pack is unavailable (error NU1100). Compile with `tools/Build-Mod.ps1`, which calls `csc.dll` with `-nostdlib` and references the installed .NET 6.0 runtime's managed DLLs directly.
- **GOTCHA — An MSBuild `-p:` property cannot carry a path with a comma** ("No, I'm not a Human"). Moot now, but the same applies to any tool that splits on commas.
- **GOTCHA — Windows PowerShell 5.1 reads `0xFAB11BAF` as a negative Int32.** Compare large hex constants as text.
- **KEY FINDING — Prism 0.18.3 works from C# on the .NET 6 runtime by plain P/Invoke** (verified 2026-10-11 outside the game): `prism_config_init` returns its struct by value without trouble, the best backend was NVDA, features `0xB5` (speak, braille, output, stop; no "is speaking"). Whether the line was audible is unconfirmed.
- **KEY FINDING — Claude's session will not download third-party code and unpack it into the game folder.** That step goes in `paste-commands.ps1` for the maintainer, with the checksum pinned in the script.
