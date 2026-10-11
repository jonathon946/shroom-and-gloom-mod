> **Status:** Evergreen (project-local agent rules). **Last reviewed:** 2026-10-11.

# Shroom and Gloom accessibility — agent rules

Start with [AGENTS.md](AGENTS.md): it lists the shared contract, the working requirements and this project's reading order. Read them explicitly. The rules below are the hard technical constraints for this project. Evidence is in [docs/recon-2026-10-11.md](docs/recon-2026-10-11.md) and [docs/lessons-and-dead-ends.md](docs/lessons-and-dead-ends.md).

## Before any work

- The game is in Early Access and Steam updates it silently. Compare `buildid` in `C:\Program Files (x86)\Steam\steamapps\appmanifest_3271280.acf` and the version line in `LoadTimings.log` against the build `docs/STATE.md` records as verified. On a change, regenerate `reference/` and re-check every hooked name before anything else.
- Run Git inside this folder only; it is its own repository.

## Game files and copyright

- The Steam install is never the source of truth and is never edited by hand. Anything placed in it goes there through a deploy script that can also remove it.
- Never commit game-derived material: metadata dumps, decompiler or interop output, extracted string tables, assets. They live in `reference/` (gitignored) and are regenerated with `tools/`.
- Third-party binaries (`prism.dll`, loader archives) live in `deps/` (gitignored). Record source URL, version, licence and SHA256 in the docs when one is introduced.
- Do not write the Steam user ID that appears in the save path into any tracked file.

## Code (once the loader is chosen)

- Never guess a game class, method or field name. Look it up in `reference/` first.
- Resolve every Harmony patch target by name at run time inside its own try/catch, and log and disable only that feature on failure. Attribute patches and `PatchAll` are not used: one renamed method after a game update must not silence the whole mod.
- Speech goes through one `Speech` singleton over Prism. Nothing else calls a screen-reader library.
- All spoken strings go through one localisation table from day one.
- Mod hotkeys are Ctrl+letter. No Alt chords and no function-row keys. Check the game's own bindings before taking a key.
- Menus are cursors: on entry speak only the focused item. Edges play a sound and do not wrap. Player-initiated feedback is never deduplicated.
- Speech and sounds are confined to the game's foreground window.

## Speech and the maintainer

- Claude cannot hear the product. Mark audible behaviour unverified until the maintainer confirms it by ear.
- Commands for the maintainer go in `paste-commands.ps1` (PowerShell 5.1-safe), not just the chat.
