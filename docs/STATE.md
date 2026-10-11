> **Status:** Evergreen (rolling single source of truth; edited in place at every close). **Last updated:** 2026-10-11, Session 01 close. Project scaffolded and the game inspected on disk. Nothing built, nothing installed into the game. Next: Spike 1 (a loader starts and Prism speaks), which waits for the maintainer's go-ahead.

# Current state — Shroom and Gloom accessibility

## Where we are

- The project folder, documents and repository exist and follow the tree-wide standards.
- The game has been inspected without launching or changing it: Unity 2022.3.62f3, IL2CPP, version 0.6.41, Steam buildid 25442225. Findings are in [recon-2026-10-11.md](recon-2026-10-11.md).
- The game looks like a good candidate: it already has a controller focus-navigation system, a combat event log, and text descriptions for cards, enemies, intents and keywords.
- One thing could stop the project: no mod loader is yet confirmed to start under this exact Unity build. A crash was reported on the neighbouring build 2022.3.62f2 and never confirmed fixed.
- Prism is chosen for speech. How to call it from C# is recorded in the reconnaissance document.

## Resume point

**Spike 1: a loader starts and Prism speaks one line at the title screen.** It installs third-party loader files into the Steam game folder, so it needs the maintainer's go-ahead first (see Q-01 in [open-questions.md](open-questions.md)).

When approved:

1. Check the game build against "Verified against" below.
2. Record the list of files the loader adds to the game folder so they can be removed. No game file is replaced, so no backup is needed.
3. Try MelonLoader (current release) first. If the game crashes at start, keep the log and try the MelonLoader nightly, then BepInEx 6 Bleeding Edge be.755 or later.
4. With a loader running, confirm interop assemblies were generated, copy them to `reference/`, and build a do-nothing mod that loads Prism v0.18.3 and says one line.
5. Maintainer confirms the line by ear. Stop and report.

## Verification status

- **Confirmed by ear:** nothing.
- **Verified by data:** the engine, version, libraries and class names in the reconnaissance document (read from the installed files on 2026-10-11); `tools/Dump-MetadataNames.ps1` runs and reproduces the name lists.
- **Unverified:** everything about run-time behaviour, including whether any loader starts, whether the keyboard drives the game's focus system, and how exploration plays.

## Open items

- Q-01: go-ahead to install a loader into the game folder.
- Q-02: who will play-test, and with which screen reader.
- The field guide's catalog (section 6) is out of date for this machine: it lacks Yu-Gi-Oh! Master Duel, No I'm not a Human, Date Everything, Darkest Dungeon II and Songs of Conquest. That is a hub document, noted here and not changed in this session.

## Running state and key locations

- **Verified against:** game 0.6.41, Steam buildid 25442225, Unity 2022.3.62f3.
- **Game install:** `C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom\`. Unmodified.
- **Logs and saves:** `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\`.
- **Name lists:** `reference\` in this folder (gitignored); regenerate with `powershell -File tools\Dump-MetadataNames.ps1`.
- **Nothing is running** and nothing is installed for this project.
