> **Status:** Historical record (rolling changelog; append-only, never edited after the fact). Newest first.

# Changelog — Shroom and Gloom accessibility

## 2026-10-11 — Session 01, part 4 — first ear test passed; its three requests built

- The maintainer played the tutorial opening by ear with the real keyboard.
- New: "Nearby: ..." when something comes into reach; row names with counts when focus moves between hand, nearby things and targets; card offers read as "Offered: ... Enter takes it."
- "Shovel Choice" is now "Shovel".

## 2026-10-11 — Session 01, part 3 — the mod runs in the game; tutorial opening playable in tests

- MelonLoader 0.7.3 installed (by the maintainer) and starts under Unity 2022.3.62f3.
- `InputBridge`: forces gamepad mode and feeds the game's actions from the keyboard (arrows, Enter, Backspace, Escape, W/S and others).
- `FocusProbe`: speaks the focused item; descriptions for cards, world objects, doors, buttons, title paths; "Choose a target."
- `WorldAssist`: objects that need a camera look are focusable when in range.
- `DevBridge` and `tools/Test-Run.ps1`: in-game screenshots, UI and state dumps, simulated input, all without window focus. `tools/show-type.sh` for type lookups.
- Speech and keys only while the game window is focused.
- New: `docs/controls-reference.md`; `paste-commands.ps1` is now the first ear-test checklist.

## 2026-10-11 — Session 01, part 2 — first mod and speech layer built

- Added `mod/ShroomAccess/`: Prism binding, speech singleton, loader entry that speaks one line at the title screen.
- Added `tools/Build-Mod.ps1` (direct compiler build, no NuGet) and `tools/Deploy-Mod.ps1` (copy into the game, or remove).
- Added `paste-commands.ps1`: installs MelonLoader 0.7.3 into the game folder with a pinned checksum. Not yet run.
- Verified outside the game that Prism 0.18.3 starts from C# on .NET 6 and selects NVDA.
- Recorded the maintainer's answers and decisions. Nothing installed into the game.

## 2026-10-11 — Session 01 — project set up, game inspected on disk

- Created the project: pickup entry, agent rules, docs map, plan, state, instructions (adopted by reference), lessons, open questions, this changelog.
- Added `tools/Dump-MetadataNames.ps1`, which writes the game's class, member and source-file names to the gitignored `reference/` folder.
- Recorded the reconnaissance: Unity 2022.3.62f3 IL2CPP, game 0.6.41, the game's own controller focus system, combat event log and inspect text; the IL2CPP and card-game mods already on this machine; how Prism is called from C#.
- Recorded the first decisions: Prism from the start; no game content in the repository; hooks resolved by name with per-hook failure isolation.
- No mod code. Nothing installed into the game. The game was not launched.
