> **Status:** Evergreen (docs map). **Last reviewed:** 2026-10-11.

# Shroom and Gloom accessibility

Make Shroom and Gloom (Team Lazerbeam / Devolver Digital; Unity 2022.3.62f3, IL2CPP; Steam Early Access) fully playable by a blind screen-reader user: every menu, both decks, combat, exploration, the map, rewards and shops spoken through Prism and operable from the keyboard.

Nothing is built yet. The project is at reconnaissance, with one gate ahead: proving that a mod loader starts under this Unity version.

## Reading order (start of every session)

0. [AGENTS.md](AGENTS.md) and [CLAUDE.md](CLAUDE.md): the pickup route and the hard technical rules.
1. This file.
2. [docs/STATE.md](docs/STATE.md): where we are and the single resume point.
3. The newest file in [docs/sessions/](docs/sessions/), by recorded session number.
4. [docs/lessons-and-dead-ends.md](docs/lessons-and-dead-ends.md): check before retrying anything.
5. [docs/open-questions.md](docs/open-questions.md): questions waiting on the maintainer.
6. [PROJECT_PLAN.md](PROJECT_PLAN.md): problem, goal, architecture direction, risks, roadmap, decision log.
7. [docs/instructions.md](docs/instructions.md): the standing contract and this project's adaptations.

## Other living documents

- [docs/recon-2026-10-11.md](docs/recon-2026-10-11.md): what the game is made of, the precedents on this machine, and how Prism is called from C#.
- [tools/Dump-MetadataNames.ps1](tools/Dump-MetadataNames.ps1): regenerates the game's class and member name lists into `reference/`.

## Historical records (never edited after the fact)

- [CHANGELOG.md](CHANGELOG.md): rolling changelog, newest first.
- [docs/sessions/](docs/sessions/): one close-out per session.

## Companion references (live outside this project)

- [The accessibility modding field guide](../accessibility-modding-field-guide.md): engines, loaders and speech bridges.
- [The shared design principles](../learning-repo/PRINCIPLES.md), with case studies for Slay the Spire 2, Yu-Gi-Oh! Master Duel, MTG Arena and Songs of Conquest.
- [The Dwarf Fortress mod's Prism bridge](../dwarf-fortress-accessibility/dfhack/bridge/README.md) and its vendored `deps/prism.h`.

## Key external locations

- Game install: `C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom\` (Steam app 3271280).
- Logs and saves: `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\` (`Player.log`, `LoadTimings.log`, `SaveData\`).
- Repository: [jonathon946/shroom-and-gloom-mod](https://github.com/jonathon946/shroom-and-gloom-mod) (private).
