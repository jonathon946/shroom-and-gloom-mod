> **Status:** Evergreen (project schematic; decision log is append-only). **Last reviewed:** 2026-10-11 (Session 01). This lays out *what the project is and the route through it*; the standing rules of engagement live in [`docs/instructions.md`](docs/instructions.md); live status lives in [`docs/STATE.md`](docs/STATE.md).

# Shroom and Gloom accessibility — project schematic

## Status

Spikes 1 and 2 passed on 2026-10-11 (Session 01). The mod runs in the game and the tutorial opening is playable in dev tests. Waiting on the first ear test; then feature work in the roadmap's order.

## Problem

Shroom and Gloom is a first-person deckbuilder with no screen-reader output. Cards, enemies, intents, the map and every menu are visual only, so a blind player cannot play it.

## Goal

A mod that makes the whole game playable by ear and keyboard with a screen reader:

- Every menu and settings screen spoken and operable.
- Both decks: the hand, card text and costs, piles, and card choices.
- Combat: enemies, health, statuses and intents on demand; events spoken in a useful order; targeting without a mouse.
- Exploration: what is ahead, what can be interacted with, doors and path choices.
- The map, rewards, shops, camp and unlock screens.
- The same rules sighted players play by.

The player is assumed to know nothing about the game: the mod must make the rules learnable by ear (tutorial text, keyword explanations, what each screen is for).

It will be released publicly to the audio games community, so it must work with screen readers other than NVDA (JAWS in particular) through Prism.

Out of scope until asked for: other platforms, controller-specific work, translating our spoken strings beyond English (the string table is built for it from the start).

## Working directory

`C:\Users\Sam\Documents\projects\shroom and gloom mod\`. Its own Git repository; remote [jonathon946/shroom-and-gloom-mod](https://github.com/jonathon946/shroom-and-gloom-mod).

- `docs/`: state, sessions, lessons, questions, reconnaissance.
- `tools/`: scripts that read the game and regenerate `reference/`.
- `reference/` (gitignored): game-derived name lists and, later, interop assemblies.
- `deps/` (gitignored): fetched third-party binaries.
- `mod/ShroomAccess/`: the mod source (C#). Built with `tools/Build-Mod.ps1`, copied into the game with `tools/Deploy-Mod.ps1`.
- `backups/` (gitignored): the fresh-install copy of the save folder.

## Reconnaissance findings (2026-10-11, game 0.6.41, buildid 25442225)

Full record: [docs/recon-2026-10-11.md](docs/recon-2026-10-11.md). In short:

- Unity 2022.3.62f3, IL2CPP, metadata version 31. No anti-cheat, no existing mod.
- uGUI with TextMeshPro, the new Input System, Unity Localization.
- Clean model/view split (`M_`/`V_` classes) and real class names preserved in the metadata.
- The game has its own controller focus-navigation system across menus, hand, enemies and world objects, plus a combat event log and inspect/tooltip text for cards, enemies, intents and keywords.
- Prism is called from C# by direct P/Invoke; four installed mods show how.

## Architecture (as built after Spikes 1 and 2)

As built: MelonLoader 0.7.3 mod, `mod/ShroomAccess/`. `InputBridge` keeps the game in gamepad mode and answers its action queries from the keyboard. `FocusProbe` watches the game's focus service and speaks a description of whatever is focused. `WorldAssist` removes the need to aim the camera. `Speaker` is the only code that touches Prism. `DevBridge` is the test harness. The bullets below were the direction before the spikes and still hold.

- **Loader:** whichever of MelonLoader 0.7.x or BepInEx 6 Bleeding Edge starts under this Unity build. Both are C# with HarmonyX and Il2CppInterop, so the mod's design is the same either way and only the entry class and project references differ.
- **Speech:** one `Speech` singleton over Prism (`prism_backend_output` / `prism_backend_stop`), confined to the game's foreground window, with a short spoken-history buffer.
- **Ride the game's focus system.** Hook focus changes in `ControllerNav` and describe the focused target from its model, rather than building a parallel cursor. If the keyboard does not drive that system, feed it from keyboard keys.
- **Describers per kind of thing** (card, enemy, hazard, door, menu control, map node), each producing a short line and a longer detail list.
- **Combat events** from the game's own event log, batched by urgency: what happened to the player first, then the player's results, then the rest.
- **Query keys** on Ctrl+letter for each state dimension (health, block, energy, hand, piles, intents).
- **Failure isolation:** each hook resolved by name in its own try/catch, with a log line naming what broke.

Design principles to apply when design starts (from `../learning-repo/PRINCIPLES.md`): P-CARD-01 to 05, P-INFO-03, P-INFO-04, P-INFO-05, P-REVIEW-01, P-REVIEW-02, P-REVIEW-06, P-UI-07, P-UI-15, P-MAP-10, P-FAIR-01, P-SR-01, P-ARCH-06.

## Constraints

- The game is in Early Access and will change under us. Hooks must fail soft and names must be re-checked after every update.
- IL2CPP: no managed game assemblies; everything goes through generated interop types.
- Claude cannot hear the product; every audible result needs the maintainer.
- No game content in the repository.
- Hotkeys: Ctrl+letter only; no Alt chords, no function row; never override a game key.

## Tech direction

C# targeting the loader's runtime (`net6.0` for both candidates), built with the installed .NET SDK 10. HarmonyX and Il2CppInterop as supplied by the loader. Prism v0.18.3 or later, fetched from its release into `deps/`, shipped beside the mod with its licence files.

## Risks

- **No loader starts under Unity 2022.3.62f3 IL2CPP.** Likelihood medium, impact high. A crash on 2022.3.62f2 was reported against both loaders and never confirmed fixed. Mitigation: Spike 1 before anything else; try current MelonLoader, then MelonLoader nightly, then BepInEx 6 be.755 or later; if all fail, record the crash evidence and assess a native proxy DLL against the raw IL2CPP API before investing further.
- **A game update renames or removes hooked members.** Likelihood high, impact medium. Mitigation: per-hook isolation, a start-of-session build check, and a script that lists which hooked names no longer exist.
- **The focus system does not cover everything, or is gamepad-only.** Likelihood medium, impact medium. Mitigation: Spike 2 measures coverage before any feature work.
- **Prism latency or a backend gap.** Likelihood low, impact medium. Mitigation: listen for lag in the first ear test; the `Speech` layer is the only place that would change.
- **Exploration turns out to need free aiming or timing.** Likelihood low to medium, impact high for that part of the game. Mitigation: find out in Spike 2; design "pick from what is ahead" rather than aiming (P-NAV-08).
- **The developers add their own accessibility or controller changes.** Likelihood low, impact positive but disruptive. Mitigation: read patch notes at each build change.

## Roadmap

Each spike ends at a breakpoint: stop, report, and confirm before the next.

1. **Spike 1 (passed 2026-10-11): a loader starts and Prism speaks.** Install a loader, let it generate interop assemblies, load a do-nothing mod that says one line through Prism at the title screen. Gate: needs the maintainer's go-ahead before installing; ends with the maintainer hearing the line.
2. **Spike 2 (passed 2026-10-11): read the focus.** Log every focus change with the target's type and any text found. Find out whether the keyboard drives focus, and which screens and phases it covers. Ends with a coverage list.
3. **Menus:** title, settings, character and run setup, by ear.
4. **Combat:** hand, enemies, intents, events, end turn.
5. **Exploration, the map, encounters and rewards.**
6. **Polish:** help, settings for verbosity, controls reference, packaging and uninstall.

Steps 3 to 6 are a sketch; their order and content are set after Spike 2.

## Decision log

- **2026-10-11 (Session 01, maintainer):** **Prism is the speech library from the start.** No Tolk path. Reason: it is the maintained successor, already adopted by the Dwarf Fortress mod, and covers more screen readers.
- **2026-10-11 (Session 01):** **Game-derived material stays out of the repository** (`reference/` is gitignored and regenerated by script). Reason: copyright and clean distribution.
- **2026-10-11 (Session 01):** **Hooks are resolved by name at run time, one try/catch each; no attribute patches.** Reason: the installed IL2CPP mods show attribute patches breaking on game updates, and the Qud project lost every patch to one bad target.

- **2026-10-11 (Session 01, maintainer):** **Free hand to build and test; reset to a fresh player at the end.** Testing may change saves and settings. Before hand-over, the game's save data and settings are restored so the maintainer starts as a new player. A fresh-install copy of the save folder was taken before any testing.
- **2026-10-11 (Session 01, maintainer):** **The mod is for public release to the audio games community.** Prism stays the speech path for every reader, NVDA included, unless a measured problem forces an exception. JAWS support is a goal. Consequences: no machine paths or names in the build, licence files shipped, an install and uninstall story, a written controls reference.
- **2026-10-11 (Session 01):** **MelonLoader 0.7.3 is tried first**, BepInEx 6 Bleeding Edge second. Reason: it is the only IL2CPP loader proven on this machine, and 0.7.3 is two releases newer than the 0.7.1 crash report.
- **2026-10-11 (Session 01):** **The mod is compiled by driving the SDK's compiler directly** (`tools/Build-Mod.ps1`), not by `dotnet build`. Reason: no NuGet source is configured on this machine, so the .NET 6 reference pack cannot be restored. Same finding as the Caves of Qud project.
- **2026-10-11 (Session 01):** **`prism.dll` lives in the loader's `UserLibs` folder** and is loaded by full path.

- **2026-10-11 (Session 01):** **MelonLoader 0.7.3 is the loader.** It starts under this Unity build; BepInEx was not needed.
- **2026-10-11 (Session 01):** **Ride the game's gamepad navigation; do not build a parallel cursor.** The mod forces gamepad mode and feeds the game's own actions from the keyboard. Reason: the developers already made every screen tried so far pad-operable, including target selection, so the mod inherits their fixes and new screens.
- **2026-10-11 (Session 01):** **No aiming.** Anything the game gates behind looking at it is made focusable when in range (P-NAV-08, P-FAIR-01).
- **2026-10-11 (Session 01):** **Speech and keys only while the game window is focused.**
- **2026-10-11 (Session 01):** **Mod commands stay off the game's cheat key combinations** until those are proven inactive.
- **2026-10-11 (Session 01):** **Dev tests never type at the desktop.** They inject inside the game.

## Open items / questions

- Whether the game's cheat keys are live in release builds.
- How combat, the map and the deck view behave under the same approach.
- Questions for the maintainer are in [docs/open-questions.md](docs/open-questions.md).

## See also

- [docs/recon-2026-10-11.md](docs/recon-2026-10-11.md), [docs/STATE.md](docs/STATE.md), [docs/lessons-and-dead-ends.md](docs/lessons-and-dead-ends.md).
- [The field guide](../accessibility-modding-field-guide.md) and [shared principles](../learning-repo/PRINCIPLES.md).
