> **Status:** Historical record (rolling changelog; append-only, never edited after the fact). Newest first.

# Changelog — Shroom and Gloom accessibility

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
