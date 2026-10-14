> **Status:** Living reference (the keys as built; updated whenever a key changes). **Last updated:** 2026-10-11, Session 01. Build 0.0.1.

# Controls reference — Shroom Access

The mod does not invent its own navigation. The game already has a full set of gamepad actions; the mod lets the keyboard press them. So each key below is "the keyboard stand-in for a game action", and the game decides what that action does on each screen.

## Keys

- **Up / Down arrows:** in play, move between rows: Status (top), Nearby, Your hand (bottom). In menus, offers and target selection they are the game's own Focus Up and Down.
- **Left / Right arrows:** move along the current row; in menus, the game's Focus Previous and Next.
- **Ctrl+Up / Ctrl+Down:** next / previous line of the current buffer. Each buffer starts at its first line.
- **Ctrl+Right / Ctrl+Left:** next / previous buffer: Item, Player, Surroundings, Messages, Help. Moving focus returns to Item. The Help buffer lists these keys in the game; `mod/README.txt` carries the same list for players and must be kept in step.
- **Ctrl+Page Up / Ctrl+Page Down:** walking voice faster / slower.
- **Ctrl+B:** drop a mark for later review (says "Mark 1", and so on).
- **Ctrl+N:** after an announced release, pressed twice: download, verify against the release's .sha256 and install (`Installer.cs`); the new version runs after a restart.
- **Ctrl+O:** after the title screen has announced a newer release, open its GitHub page in the browser (`UpdateCheck.cs`). Ctrl+U was avoided: it is one of the game's cheat keys.
- **Space:** end turn; in camp, rest for the night. After a death, Space moves through the words on screen.
- **Enter on a sealed card pack** (a quest reward after a run): opens it. The game itself opens it only on a mouse click.
- **Enter** (main or numpad): confirm. On a card in hand it starts a cast; on a target it casts; on a button it presses it.
- **Backspace:** cancel or back.
- **Escape:** pause menu.
- **W / S (hold):** walk forward / back. A and D are passed as left and right on the walk stick.
- **M:** map (the game's own key).
- **I:** inspect the focused thing.
- **Tab:** deck view. In a run the game opens it only from its mouse-only pile buttons, so the mod opens it: the draw pile of the deck in play; Tab again, the discard pile; Tab again, closed. Inside, Up reaches buttons for the full deck and the combat, explore and foresight decks.
- **Page Up / Page Down:** previous / next page.
- **O:** sort hand.
- **C:** recentre the view.

The last six are mapped but have not been tried on a screen where they do something.

## Rules the keys follow

- While **Ctrl** is held, the mod passes none of these to the game. Ctrl combinations are reserved for mod commands; Ctrl+arrows are the buffers.
- At either end of a row the current item is spoken again; rows do not wrap.
- Keys only count while the game window is focused.

## Keys the mod must never take

The game has its own Ctrl combinations for developer cheats, and their input map is enabled in this build. Until they are proven dead, no mod command may use: Ctrl+D, Ctrl+E, Ctrl+G, Ctrl+H, Ctrl+I, Ctrl+K, Ctrl+R, Ctrl+U, Ctrl+8, Ctrl+9, Ctrl+0, Ctrl+Shift+J, Ctrl+Alt+H.

## The game's own bindings (from its input asset, 2026-10-11)

- Keyboard and mouse: move W A S D or arrows; run Shift; look with the mouse; interact left click; inspect right click; end turn Space; map M; UI submit Enter; UI cancel Escape.
- Gamepad: move left stick; look right stick; focus d-pad; confirm A; cancel B; inspect X; end turn Y; pause Start; deck view Select; page with the triggers; map left shoulder; sort hand right shoulder; recentre right stick press.

## Rebinding (Session 04, 2026-10-14)

Every key above can be changed by the player in `UserData\MelonPreferences.cfg`, section `[ShroomAccessKeys]`: one entry per game action the mod answers (`FocusPrev`, `Confirm`, ...), the walk keys (`WalkForward`, ...) and the letters of the mod's Ctrl commands (`CtrlPlusMark`, `CtrlPlusOpenUpdatePage`, `CtrlPlusInstallUpdate`). Values are comma-separated Input System key names; an unknown name is logged and the default kept. The cheat-key list above still applies when choosing letters for Ctrl commands.

