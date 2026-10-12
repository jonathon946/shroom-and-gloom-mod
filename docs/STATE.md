> **Status:** Evergreen (rolling single source of truth; edited in place at every close). **Last updated:** 2026-10-12, Session 01 (part 14). Fifth playthrough reviewed (7 marks; the maintainer reached a shop). Built and waiting for a listen: unasked lines are gathered for 0.7 seconds and spoken once in a fixed order; dealing is silent until it ends, then "Drew N cards" and the focused card; cards that need two presses say so; first wording for the shop (prices on cards, the forget service, the bowl). The save is at fresh-install state; a copy of the run that reached the shop is in `backups/2026-10-12-run-with-shop-SaveData`. Next: the shop properly, the map, then the open items below.

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
- **Stuck foresight screen: likely cause found 2026-10-12.** It happened again (playthrough of 2026-10-12, mark 4). In both cases the game's log shows "Active instance set to: ForesightChoiceUI" followed at once by "Lost focus" and time frozen, and no cards were ever dealt. A dev replay of the same spot with no focus change dealt the cards normally. Reading: a chest takes some seconds to rise and deal, nothing is focusable meanwhile, the player concludes it is dead and pauses or switches window, and freezing time at that moment breaks the deal for good (a game bug). The mod now says "Wait: the cards are still being dealt. Stay in the game window until they are read." and reads the choice when it arrives. Recovery if it happens: Escape, Main Menu, Continue. Earlier notes follow.
- **Stuck foresight screen, earlier notes.** At mark 2 of the 2026-10-11 playthrough the game opened ForesightChoiceUI ("A Gift From Below. Choose a Foresight") with no cards; the focus mode stayed Suppressed and nothing responded. The game's log shows the screen becoming active and then the window losing focus, with no "Spawning reward choice" lines. Earlier in the same run the game logged "StartInteraction called on ForesightChoiceUI ... already been started! ignoring!". Possibilities, none tested: a game bug on losing focus at that moment; Enter reaching the game twice (the game's Confirm and the UI module's Submit both fire on Enter); something the mod's hooks disturb. Quitting and choosing Continue put the run back in the world just after the fight. Copies of the logs and marks are in `reference/playthrough-2026-10-11/`.
- **From the same playthrough, still to do:** read class details on the class screen (the buffer now lists every text on the focused control; unverified there); say what breaking a chest did and why a fight began (the prompt hook and screen text may cover it; unverified); the hand is hidden after a fight until the game draws it again, and Down now says "No cards in hand right now"; the maintainer asked for a way to replay the tutorial, which the game does not offer once it is done (restoring the fresh-install save would do it, and is the reset already promised for hand-over).
- **Alt text:** 71 descriptions in `mod/ShroomAccess/data/alt-text.txt` as of 2026-10-12: 9 written in the main session from pictures, 62 by helper agents from the automatic pictures of two playthroughs. Only a handful have been spot-checked by a second reader and none have been heard by the maintainer. Not written for want of a clear picture: Lazarus, the Foresight chest, The Encroaching Gloom, Paper Boy, Bash, Spring Into Action. Flagged as uncertain by the helpers: Pet Rat, Chest and Class card chest (described from an identical chest), Dark Crystal. The helpers skip pictures of menu controls; about half of all automatic pictures are of those, so the picture-taking should be limited to cards, creatures and world objects.
- **Saves are disposable until the mod is finished** (maintainer, 2026-10-11): overwrite freely; keep a copy only where it helps test later parts of the game. `tools/Reset-Save.ps1` restores the fresh-install save (tutorial available again) after copying the current one to `backups/`. The save was reset on 2026-10-11 after the first full playthrough; a dev replay that touches the tutorial must be followed by another reset. Whether Steam Cloud then asks which copy to keep has not been seen yet.
- **Pictures for alt text are taken automatically** on this machine (dev flag): the first time an undescribed thing is focused, a screenshot goes to `UserData\ShroomAccesslt-shots\<kind>-<name>.png`. The maintainer does not need to mark for pictures; marks are for things that seem wrong.
- **Silent text, surveyed so far** (priority set by the maintainer 2026-10-11). Covered, in dev tests: loading tip ("Loading. ..."), "Press Space Bar" on the loading screen (watched, not yet seen firing), `GeneralPromptText.Say` (hooked, not yet seen firing), headings and questions when a screen opens, text under the HUD's "Meta Root" (unlock and reward banners, death text; not yet seen firing). **Not yet covered or not yet checked:** tutorial hint text on its own objects, damage numbers and floating combat text, enemy speech or flavour lines if any exist, the map, the deck view, the class selection screen's panels, encounter dialogue screens (`GeneralEncounterUI`), the end-of-run summary, quest popups. Method: play each in a dev replay with `-Tree` and look for text that is on screen and not in the log as `[say]`.
- **Menus, checked in dev tests:** title menu (Settings, Credits, Quit), settings tabs Sound, Graphics, Game, Controls, the credits, the end-previous-run question, the pause menu. **Not checked:** opening a dropdown and choosing from its list, the VSync row (skipped by focus when windowed), rebinding a controller button, the Reset and Delete Save buttons (not pressed), settings reached from the pause menu rather than the title.
- **A dev replay changed a real setting** (master volume, and toggled chromatic aberration) and put them back. Settings are the maintainer's; replays that walk settings must restore what they touch.
- **From the 23 marks (playthrough b, 2026-10-11), still open:**
  - Marks 1 and 4: whether the game shows any text on first landing in the world, or after a door opens. Not yet checked against the screenshots. The mod now says "The door is open. You can walk on."
  - Mark 12: a foresight door opened and nothing was read. Not investigated.
  - Mark 13: why a fight starts. The mod says "Combat."; whether the game shows anything more is unknown.
  - Mark 17: what warmth and threat mean. The game's only text is "Rest for the night. Beware of creeping danger." (now read with Enter on that status). The rule itself still has to be learned by play and then written into help.
  - Marks 20 and 21: the card-change screen (Strange Geode, Practice Dummy). It works like a cast: Enter on a card in hand, then Enter on the panel. The wording now says so; "No thanks" is still a separate button below. The maintainer wants the message first, then the cards, then "No thanks" last; not built.
  - Mark 22: Foraged Snack on Pet Rat. Feeding is a card cast onto another card in hand; the "can't be played" line now covers a failure, but the card-on-card flow is untested.
  - Mark 23: Camp Fire's target read "5". Plots are now named ("Empty plot, front left", or the hazard standing on it with its threat). Untested on that screen.
  - Mark 3: fast double Enter and message timing. Not addressed.
  - The stuck empty foresight screen from playthrough a has not recurred and is not explained.
- **Camp rooms, as understood from mark 19:** a room has building plots (shown as hammer outlines) where cards marked Camp are built, hazards carrying a threat number, torches giving warmth, chests, and doors onward. The HUD shows total threat against warmth.
- **The shop** (`BowlShopEncounterUI`, seen in playthrough 2026-10-12b marks 4 to 7; picture in that folder's mark-4): a mole behind a counter, a speech bubble ("WEAPONS FOR FOOD?"), "Funds: 3 FOOD" at top right, eight cards in two rows each with a "COST: n" tag, a "NO THANKS" button, and a creature at the left that is the forget service ("COST: 1"). Paying is done by casting food cards from the hand onto the bowl. What the mod says there now is a first guess from the marks and has never been run: cards read "For sale: n food", the forget service and the bowl have names, and an instruction line is spoken on entry. **Not done:** the mole's speech lines, the funds readout, a clean row order (the game's stepping is spatial and Left repeated items), how to leave after buying (mark 7: the maintainer could not find the way on), and whether the instruction line is even right.
- **Message density** (maintainer, 2026-10-12): information must not be lost, but bursts were too dense. Unasked lines now batch for 0.7 seconds and are ordered: fight or camp, then what is nearby, then whose turn, then draws, then energy, then everything else in the order it happened. Tune by ear.
- **The map** (`S_MapReminder`, a focus scope in Suppressed mode with `MapUI`): opens by itself after some choices (seen after choosing Soup Pot) and on M. Nothing in it is read. The mod says so and that M puts it away. Building the map reader is the next feature.
- **Start-up:** the boot screen is the logo on black for about four seconds; no text. The title screen is described once at start. 
- **Reviewing a playthrough** (maintainer asked 2026-10-11): the mod logs every key it acts on (`[key]`) beside every line spoken (`[say]`, `[say-direct]`) in `MelonLoader\Latest.log`; the loader keeps earlier sessions in `MelonLoader\Logs\`. Ctrl+B drops a numbered mark: a log line, a screenshot and a state dump under `UserData\ShroomAccess\marks\<session>\`. The mark key has not been pressed by anyone yet.
- **Alt text** (maintainer asked 2026-10-11): things have hand-drawn art and no descriptive text of their own. Descriptions are authored by us in `mod/ShroomAccess/data/alt-text.txt` and read as a "Looks:" line at the end of the item buffer. Five are written. Anything looked up and not found is logged as `[alt-missing]`, which is the worklist. Writing them needs a picture of each thing: use marks and dev screenshots, and build a sprite capture command when the list gets long.
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
