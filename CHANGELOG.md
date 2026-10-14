> **Status:** Historical record (rolling changelog; append-only, never edited after the fact). Newest first.

# Changelog — Shroom and Gloom accessibility

## 2026-10-14 — Session 04, part 2 — ninth playthrough: less speech

- After a play, a draw, a new turn, a fight opening, a payment or a screen's introduction, the card the focus lands on is not read; the next arrow is.
- "Played X" is no longer said.
- Fights open with counted names ("2 Terrys, 1 Touchy"), separated by full stops in the walking voice.
- The edge sound comes sooner (0.12 seconds); the backpack pile no longer plays it on every step.
- Buffers and the map: at either end the edge sound plays and nothing is re-read.
- Card-change screens (practice dummy): the effect, then "Choose one of your cards"; no heading, card list or draw count.
- Shop: "Food to spend" in the Player buffer; choosing a card is one line ("Costs 2 food..."); paying is "Paid 1 of 2" and back to the hand silently; no false combat when pausing mid-purchase.
- Returning from the pause menu does not re-read the screen's introduction.
- The character page's labels are said in words (starting cards found, where the foresight is, a Coming soon row).

## 2026-10-14 — Session 04 — eighth playthrough: false failures, the shop, the soup pot

- Recovered and committed the cut-off session's work from 2026-10-12 (map forks and boss, walking voice hand-over, quieter card plays, camp card placement, threat outcome).
- Sounds survive scene loads; they were switched off for good after a death.
- Card plays are counted by the game: Persistent cards say "Played"; Unplayable cards say they work by themselves in hand; a refused play says so with the game's reason or "The game gives no reason".
- Shop: no false "Combat started" or "You are in a fight" while paying; "Paid 1 of 3."; "Bought Hot Poker."; the passing "Nevermind" is not read.
- Armour: "3 blocked by armour" on a hit; "gains 3 armour" on the enemy turn.
- Death: "You have died." at zero health, with nothing from the fight after it.
- Turn start: "Your turn. Energy 3 of 3. Drew 4 cards." then the card; the hand is quiet while the enemies act; no "New turn" in fights.
- An emptied hand is said once: "Your hand is empty. Space ends the turn."
- Food and other self-aimed cards: "Aimed at you..."; the player as a target is "You."
- Soup pot: the soups with their costs and open or locked state, the soup value as it rises, "Made Spicy Soup"; the game's lost first Up is pressed again.
- Nearby is not repeated after a screen, a target choice or a door opening.
- Names: "Soup Pot" (was "Soup Pot Encounter With UI").
- Help: three lines on food targets, the shop and the soup pot.

## 2026-10-12 — Session 03 — seventh playthrough: voices take turns, fights and camps in one line, death screens

- The two voices never speak together: a line asked for with a key is said at once and the walking voice repeats its own afterwards; other lines wait for the walking voice to finish.
- While walking, things coming into reach make the sound only. They are listed when the walk stops: "You have stopped at a camp. Seen nearby: ...", or "You have stopped. Seen nearby: ...".
- A fight opens with one line: "Combat started. Enemies: ... Also here: ... Drew N cards." What was seen on the way in is read with "Combat over."
- Later arrivals in a fight: "Joined the fight: ...".
- An arrow key is never silenced by a hold; an arrow that moves nothing plays the edge sound.
- Card-change screens: no eight-second silence after the pause menu; the result says "Enter takes it."; "Up is No thanks".
- Enemy intents in the buffer: "Will deal 3 damage" (was "Will DEALS 3 DAMAGE").
- Death: "You have died."; "Space continues."; the page of numbers read in full and held in the Item buffer; a sealed card pack opens with Enter and its cards are read.
- Pausing, or choosing a target, no longer repeats the whole Nearby list.
- The first-landing note speaks only in the tutorial forest.
- Help: two more lines (arrival sounds; death and the card pack).
- Dev: `die`, `roundup`, `pack`, `polled`.

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
