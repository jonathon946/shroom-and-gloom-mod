> **Status:** Historical record (rolling changelog; append-only, never edited after the fact). Newest first.

# Changelog — Shroom and Gloom accessibility

## 2026-10-12 — Session 02 — tooling, then the backlog

- Tools: `Snapshot.ps1` (named save copies); dev bridge commands `addcard`, `energy`, `heal`, `killall`, `list`, `focus TEXT`; `Test-Run.ps1` tokens `dev:<command>` and `until:<regex>`.
- Card-change screens (grimoire, geode, practice dummy): message first, then the cards; held-back focus lines are spoken after the message; the changed card is read with "Enter takes it. Left is No thanks."
- Player buffer lists the statuses on the player with the game's explanations.
- Class screen: locked characters say "locked"; a card is "Your hand" only in hand mode; the settings heading rule is confined to settings.
- Help: a fifth buffer with the keys, rows and buffers; the title greeting points to it.
- A new game's wordless "push forward" note is spoken once: "A hand holds up a note with a picture: push forward. Hold W to walk."
- Enemy intents read "Will attack for 5", "Will block 3", "Will use Psychic Scream 2".
- A use the game shows as unknown reads "Also takes something unknown." after the known uses.
- `mod/README.txt` for players and `tools/Package-Mod.ps1`, which builds `dist/ShroomAccess-<version>.zip` (mod, data, Prism and its licences, README; nothing from testing).

## 2026-10-12 — Session 01, part 17 — the map; session close

- `MapReader`: with the map open, the first line gives the number of areas, how many are explored and where the player is; Ctrl+Up reads each area (what it holds, fight won or ahead, unexplored).
- Automatic pictures are taken only of cards, creatures and things in the world, no longer of menu controls.
- Dev: `click <object>` presses a UI button (`Test-Run.ps1 -Press click:Map`).
- Session closed with the backlog recorded in STATE in the agreed order.

## 2026-10-12 — Session 01, part 16 — sixth playthrough: fights, shop, statuses

- In a fight the Enemies row holds only what the game will focus; room furniture no longer appears in it and bounces the focus back.
- Arrivals are grouped: "Nearby: 2 Doors, Class card chest, Foresight chest, Chest."
- Enemy statuses in the buffer carry the game's own explanation ("Vulnerable 3: WILL RECEIVE 150% MORE DAMAGE").
- Shop: "For sale" with row and place ("top row, 2 of 4"); the paying hand is "Your food"; the shopkeeper's changing lines are read; no false "Combat."; the entry line is said once.
- Text on an open screen that changes while it is open is read.
- A screen's heading is not re-read on returning to it within 30 seconds.
- 33 more appearance descriptions (103 in all); lookups ignore punctuation.

## 2026-10-12 — Session 01, part 15 — reading a pile of offered cards

- In an offer of several cards taken together (a backpack), Left and Right step through the cards with their place in the pile, and the buffers read the card stepped to. The game itself lets a gamepad focus only the top card and the Take button.
- The Take button no longer reads every card's text (a settings-page heading rule had been applied outside settings).

## 2026-10-12 — Session 01, part 14 — fifth playthrough: density, dealing, the shop

- Unasked lines are gathered for 0.7 seconds and spoken as one run in a fixed order; several "Nearby" lines become one list; "New turn" is dropped when "Your turn" says the same.
- While cards are being dealt the hand is not read card by card; afterwards "Drew 4 cards." and the focused card.
- A card that is lifted by the first Enter and played by the second says "... is ready. Enter again to play it" (it used to say it could not be played).
- Shop, first wording: "For sale: 2 food" on cards, "Forget a card, COST 1 food", the bowl, and an instruction line on entry.
- A focused control is never read as a wall of text.

## 2026-10-12 — Session 01, part 13 — fourth playthrough

- "Can't walk right now" is spoken in the walking voice with the edge sound, so holding W does not cut it.
- "The door is open. You can walk on." now fires when the opened door has left the nearby row.
- On a screen with nothing to focus, the arrows reach the status row.
- Offers wait for their cards (up to 25 seconds) and say "Wait: the cards are still being dealt."
- Things gated behind a camera look that are not encounter objects (a rising chest) are made focusable within ten units.
- "The map is open. Reading the map is not built yet. Press M to put it away."
- One-time description of the logo and the three doorways on the title screen.

## 2026-10-11 — Session 01, part 12 — fixes from 23 marks

- `Walk`: W and S never step the focus; "Can't walk right now" with the reason when the game blocks walking; an edge sound and "Can't go further this way" when a held walk key moves nothing.
- The walking voice queues its lines and no longer cuts itself off.
- Rows say position ("2 of 3"); the end of a row plays a low sound, with no repeat of the line.
- `CardPlay`: "Not enough energy for X: it costs 1, you have 0. Space ends the turn."; "X can't be played right now."; "Played X."
- A hand of several cards arriving is "Drew 4 cards."; a multi-card offer is introduced once ("You are offered all of these, 5 cards: ..." or "Choose one of 3: ...") and its dealing-out is not read card by card.
- "You have stopped at a camp. Cards are dealt; Space ends the turn."; "The door is open. You can walk on."
- Camp plots, chests (foresight, class card) and the card-change panel have proper descriptions.
- Player buffer lists foresights. Enter on a status reads the game's description of it.
- Things in the world with nothing more to read say so.

## 2026-10-11 — Session 01, part 11 — menus and silent text

- Controls are named by the label of their settings row, with kind and value: "MASTER VOLUME, slider, 55 percent", "WINDOW MODE, dropdown, Windowed", "AMBIENT OCCLUSION, ON". A change of value is spoken as it happens.
- Headings inside a settings page are read on reaching the rows under them. Controller Support carries a warning to leave it on. Controls-page rows say they rebind a controller button.
- Unlabelled buttons get plain names ("Menu", "Close").
- `TextWatch`: text that newly becomes visible on the loading screen, the HUD's banners and the prompt line is spoken ("Loading. Burritos are wraps...").
- The buffer steps through long text line by line (the credits).
- `tools/Reset-Save.ps1`; automatic pictures of undescribed things.

## 2026-10-11 — Session 01, part 10 — first full playthrough reviewed

- Buffers rebuilt: Item, Player, Surroundings, Messages; Ctrl+Up next line, Ctrl+Down previous; reset and return to Item when focus moves.
- Message history (newest first) in the Messages buffer.
- `Messages`: headings and questions read when a screen opens ("END PREVIOUS RUN? A new run ends your previous run. Are you made like that?"); the game's flashed prompts read as they appear; "This card acts on you. Enter to play it" for cards with no target.
- Unasked lines are no longer cut by the next focus line (2.5 seconds of protection).
- "No cards in hand right now." when Down has no hand to go to.
- Buffer for buttons and panels lists every text on them.
- Alt text: Terry, Touchy, Stab, Roast added from the maintainer's mark.

## 2026-10-11 — Session 01, part 9 — playthrough review and alt text plumbing

- Every key the mod acts on is logged. Ctrl+B saves a numbered mark (log line, screenshot, state dump).
- `AltText`: authored appearance descriptions from `data/alt-text.txt`, read as "Looks: ..." in the buffer; missing ones are logged. First five written (Shovel, Dig Site, Door, Torch, Rusted Hammer).

## 2026-10-11 — Session 01, part 8 — hazards, doors, the camp readout, first combat

- Fourth ear test: rows, buffers and Escape confirmed. Buffer on the status row made consistent; unopened objects say "Not opened yet. Enter opens it".
- Cards read their face text ("Fix OR Break", "Reveal OR Ignite") and type.
- Hazards use the game's display name ("Torch"); doors read "Door", whether open, and their sign.
- `Hud`: warmth and threat line from the game's own hover text, in the status row and announced when it changes; health changes; energy refills; new turn.
- Combat: "Combat.", "Your turn.", "Enemy turn."; enemies by name with health, intent and statuses; "takes 3, 2 left", "dies"; the nearby row is called Enemies in a fight.
- Space ends the turn.

## 2026-10-11 — Session 01, part 7 — third ear test; arrows no longer walk

- Confirmed by ear: the walking voice and the arrival sound.
- Fixed: the arrow keys also walked (the game binds them to movement), so moving between rows moved the character; walking is now rebuilt from W A S D only, and not while Ctrl is held.
- Fixed: Ctrl+arrows in an offer moved the cursor as well as reading the buffer; the engine UI module's own arrow navigation is now switched off (its submit and cancel are kept).

## 2026-10-11 — Session 01, part 6 — live announcements while walking

- A direct Windows voice (OneCore, else SAPI) through Prism, used for unasked lines while a walk key is held; the screen reader cannot cancel it.
- A generated two-note sound when something comes into reach.
- Settings in the loader's preferences file: walking voice rate (starts from NVDA's rate), cue volume. Ctrl+Page Up / Ctrl+Page Down change the rate and say the new value.

## 2026-10-11 — Session 01, part 5 — second ear test; rows, buffers, held speech

- `Navigator`: strict rows (Status, Nearby, Your hand); Left/Right within a row, Up/Down between rows; edges repeat the current item.
- Buffers: Ctrl+Up/Down through the focused thing's lines, Ctrl+Left/Right between Item and Player. Keyword explanations come from the game ("CONSUME: This card will be destroyed upon being played.").
- Unasked lines (arrivals, cards added) are held while a walk key is down and spoken on release; several arrivals read as one list.
- "Added to hand: ..." when a card arrives other than as the focused card.
- Dev: simulated presses now go through the same path as real keys, with `ctrl+` supported.

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
