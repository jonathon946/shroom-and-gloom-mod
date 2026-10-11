> **Status:** Evergreen (rolling single source of truth; edited in place at every close). **Last updated:** 2026-10-11, Session 01 (part 2). The first test mod is built and Prism is proven to work from C#. Waiting on the maintainer to run `paste-commands.ps1`, which installs the loader. Then: start the game and see whether the loader survives.

# Current state — Shroom and Gloom accessibility

## Where we are

- The game has been inspected on disk: Unity 2022.3.62f3, IL2CPP, version 0.6.41, Steam buildid 25442225. Findings are in [recon-2026-10-11.md](recon-2026-10-11.md).
- The maintainer has given a free hand to build and test (see the answered questions), will test by ear with NVDA, knows nothing about the game, and plans a public release to the audio games community.
- A first mod exists in `mod/ShroomAccess/`: it starts Prism and says one line when the title screen loads. It compiles. It has never run in the game.
- The speech layer was run on its own against Prism 0.18.3: it started, chose NVDA and reported no error.
- **Nothing is installed in the game folder yet.** Claude's session is not allowed to download and unpack the loader there, so that step is in `paste-commands.ps1`.
- Still unproven, and able to stop the project: that a loader starts under this Unity build.

## Resume point

**Spike 1, continued.**

1. Maintainer runs `paste-commands.ps1` (installs MelonLoader 0.7.3, checksum-verified) and reports "loader installed".
2. Start the game once. The loader's first run downloads Unity support files and generates interop assemblies; this can take several minutes. Read `MelonLoader\Latest.log` in the game folder.
3. If the game crashes at start: keep the log, then try a MelonLoader nightly build, then BepInEx 6 Bleeding Edge be.755 or later. Each needs its own paste-commands step.
4. If it starts: run `tools\Build-Mod.ps1` (now against the game's own loader folder), `tools\Deploy-Mod.ps1`, start the game, and check the log for "Greeting spoken".
5. Maintainer confirms by ear that "Shroom Access loaded" is spoken at the title screen. Then Spike 2 (log every focus change).

## Verification status

- **Confirmed by ear:** nothing.
- **Verified by data:** the reconnaissance facts; `tools/Dump-MetadataNames.ps1` and `tools/Build-Mod.ps1` run; the speech layer starts Prism 0.18.3 on .NET 6 with backend NVDA and no error.
- **Unverified:** whether the standalone Prism test line was heard; `tools/Deploy-Mod.ps1` (never run); everything about run-time behaviour in the game.

## Open items

- **Reset to a fresh player before hand-over** (maintainer's condition). The fresh-install save copy is in `backups/2026-10-11-fresh-install-SaveData/`. The game uses Steam Cloud for its save folder (`steam_autocloud.vdf`), so the reset must account for the cloud copy, and for any settings the game keeps in the registry (not yet looked at).
- The field guide's catalog (section 6) is out of date for this machine; noted, not changed.

## Running state and key locations

- **Verified against:** game 0.6.41, Steam buildid 25442225, Unity 2022.3.62f3.
- **Game install:** `C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom\`. Unmodified as of this update.
- **Logs and saves:** `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\`.
- **Third-party binaries:** `deps\prism-0.18.3\` (copied from the Dwarf Fortress project; SHA256 `7C7D09C8C7306E8E1A0C46D603CCB2A391C194C77843400C11B746E7DD56A467`, the v0.18.3 release copy recorded there). The loader archive will land in `deps\melonloader-0.7.3\`.
- **Name lists:** `reference\`; regenerate with `powershell -File tools\Dump-MetadataNames.ps1`.
- **Nothing is running.**
