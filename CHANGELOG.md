> **Status:** Historical record (rolling changelog; append-only, never edited after the fact). Newest first.

# Changelog — Shroom and Gloom accessibility

## 2026-10-11 — Session 01 — project set up, game inspected on disk

- Created the project: pickup entry, agent rules, docs map, plan, state, instructions (adopted by reference), lessons, open questions, this changelog.
- Added `tools/Dump-MetadataNames.ps1`, which writes the game's class, member and source-file names to the gitignored `reference/` folder.
- Recorded the reconnaissance: Unity 2022.3.62f3 IL2CPP, game 0.6.41, the game's own controller focus system, combat event log and inspect text; the IL2CPP and card-game mods already on this machine; how Prism is called from C#.
- Recorded the first decisions: Prism from the start; no game content in the repository; hooks resolved by name with per-hook failure isolation.
- No mod code. Nothing installed into the game. The game was not launched.
