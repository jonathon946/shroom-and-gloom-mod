> **Status:** Evergreen (rolling single source of truth; edited in place at every close). **Last updated:** 2026-10-11, Session 01 (part 8). Four ear tests done; rows, buffers, walking voice, Escape all confirmed by ear. Built since and waiting for a listen: full card text, hazards and doors by their real names and signs, the warmth and threat readout, turn, energy and health announcements, and a first pass at combat (enemy names, health, intents, statuses, hits and deaths, whose turn). Dev replays have played from the title through the first fight. Next: listen, then the surroundings buffer, the map, deck view, rewards after a fight.

# Current state — Shroom and Gloom accessibility

## Where we are

Build: Shroom Access 0.0.1 on game 0.6.41 (Steam buildid 25442225), MelonLoader 0.7.3, Prism 0.18.3.

What works in dev tests (simulated input inside the game, log output read by Claude):

- **The loader starts** under Unity 2022.3.62f3 IL2CPP and generates interop assemblies. The crash reported for 2022.3.62f2 does not happen here.
- **Speech:** one line per focused item through Prism, only while the game window is focused.
- **Keyboard drive:** the mod keeps the game in gamepad mode and answers the game's action queries from the keyboard, so the game's own focus navigation runs. Arrows step focus, Enter confirms, Backspace cancels, Escape pauses, W and S walk.
- **Title screen:** the three paths, New Run and the menu button are reachable and spoken.
- **Pause menu:** Unpause, Settings, Credits, Main Menu, Quit are reachable and spoken.
- **Tutorial opening, start to finish:** walk to the shovel, pick it (card offer, "Take it"), cast Rusty Shovel on the dig site, take the Key, cast the Key on the door, walk through into the next area.
- **Spoken descriptions:** cards (name, cost, rules text), world objects (name and what the game says they need, for example "Dig Site. Something is hidden here. Looks diggable."), doors, buttons, title paths.
- **Looking is not needed:** objects the game only lets a gamepad focus after the camera has been pointed at them are marked as seen when they are in range.

Not built yet: combat, the map, deck view, inspect and keyword explanations, health and energy queries, settings screens, a review buffer, any help. See the roadmap in the plan.

## Resume point

1. **Maintainer's listen through the first fight** (run sheet in `paste-commands.ps1`).
2. Then, in order: health/energy/hand query keys; keyword explanations from the game's tooltip registry; the hazard ("Torch Hazard Off") just past the tutorial door; first combat.
3. Before any of that, if the game build has changed: regenerate `reference/` and re-check the hooks listed in the log at start ("Hooked ..." lines).

## Verification status

- **Confirmed by ear** (maintainer, NVDA, real keyboard, 2026-10-11): the greeting; title screen navigation with arrows; taking the shovel; digging; taking the key; unlocking the door. Third test: the walking voice spoke while W was held without talking over NVDA, and the arrival sound fired.
- **Verified by data** (mod log, game state dumps, in-game screenshots): everything under "What works" above.
- **Unverified:**
  - Part 5 (rows, status row, buffers, held speech, added-to-hand): checked in the log, not yet heard. Part 4's wording was heard and judged fine: nothing cut off, verbosity fine.
  - Whether keys held or tapped quickly behave (the mod uses event-driven actions so a fast tap should count).
  - JAWS, Narrator, and running with no screen reader.
  - Whether the game's cheat keys (Ctrl+H heal, Ctrl+K kill all and others) are live in this release build. The Cheats input map reports enabled. The mod ignores game actions while Ctrl is held, but does not block the game's own cheat handling.

## Open items

- **Reset to a fresh player before hand-over** (maintainer's condition). Dev testing has started runs and taken tutorial cards, so the save is no longer fresh. The fresh-install copy is in `backups/2026-10-11-fresh-install-SaveData/`. The reset must also deal with Steam Cloud (the save folder syncs) and any registry settings (not yet looked at).
- **Dev bridge must be off in a release:** it is enabled only by `UserData\ShroomAccess\dev.flag`, which exists on this machine. Do not ship that file.
- **Distribution** (maintainer asked 2026-10-11): the game has no Steam Workshop support, so the route is a public download plus an update check inside the mod. Design and build that at the packaging stage; the repository is private today.
- **Walking and speech** (decided with the maintainer, part 6): lines that arrive while a walk key is held are spoken at once through a direct Windows voice (OneCore, else SAPI) that the screen reader cannot cancel, with a two-note sound on arrival. The voice's rate starts from NVDA's configured rate and is adjusted with Ctrl+Page Up / Ctrl+Page Down. The rate match is approximate: this machine's NVDA uses the Eloquence synthesizer, whose rate number does not mean the same speed in a Windows voice. A walk toggle was considered and dropped as the default.
- **What the game is, as learned so far** (the maintainer knows nothing of it; keep this current): you walk a path forward and back. Things on the path take cards from your explore hand: a shovel digs, a key unlocks, a match lights or reveals, a hammer fixes or breaks. Lighting a torch makes camp: warmth against threat ("Safe for 3"), and ending the turn there (Space) rests for the night and refills energy. Doors carry a sign saying what lies behind. A backpack gives the explore deck. Fights use a separate combat deck; enemies stand in lanes, show what they intend, and you spend energy on cards, then end the turn.
- **Enemy intent wording** uses the intent's internal name ("Deal Damage 3"); the game's own tooltip sentence is used in the buffer. Statuses are named but not yet explained.
- **Parity:** the maintainer wants the mod to match what a sighted player can know; unopened things are not peeked into.
- **Escape and the pause menu:** a simulated Pause action does not open the pause menu, so Escape reaches it by another route (probably the UI module's cancel action). That route is left untouched; find out what it is before changing anything near it.
- **Surroundings buffer** (maintainer asked for a look or scan command): planned as one more Ctrl+Left/Right buffer; needs a check of what distance and direction the game exposes.
- The torch is read as "Torch Hazard Off"; hazards need a proper description and, on Enter, a result line.
- A stray focus line is sometimes spoken just before an offer opens (the game moves focus first).
- The title screen's centre path is spoken only as "New Run"; it should say which path it is.
- "Hamburger Button" should be spoken as "Menu".
- Focus changes interrupt speech; a context line followed by a selection can clip (lesson carried from Caves of Qud). Revisit after the ear test.
- The field guide's catalog (section 6) is out of date for this machine; noted, not changed.

## Running state and key locations

- **Verified against:** game 0.6.41, Steam buildid 25442225, Unity 2022.3.62f3.
- **Game install:** `C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom\`. **Modified:** MelonLoader 0.7.3 (`version.dll`, `MelonLoader\`), `Mods\ShroomAccess.dll`, `UserLibs\prism.dll` and `prism-licenses\`, `UserData\ShroomAccess\dev.flag`. The list of what the loader added is in `deps\game-root-added-by-loader.txt`.
- **Mod log:** `MelonLoader\Latest.log` in the game folder. Lines from the mod are tagged `[Shroom_Access]`.
- **Game log and saves:** `%USERPROFILE%\AppData\LocalLow\Team Lazerbeam\Shroom and Gloom\`.
- **Build and deploy:** `powershell -File tools\Build-Mod.ps1`, then `powershell -File tools\Deploy-Mod.ps1` (game closed). Remove with `Deploy-Mod.ps1 -Remove`.
- **Dev testing without window focus:** `tools\Test-Run.ps1` (see its header). It never types at the desktop.
- **Type lookups:** `tools/show-type.sh 'Il2Cpp.SomeType'` prints a game type's members from the generated assemblies; results cache in `reference/decomp/`.
- **The game is closed** as of this update.
