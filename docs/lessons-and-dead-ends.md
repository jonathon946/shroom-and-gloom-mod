> **Status:** Living reference (deduplicated index of what's been tried and settled; checked before retrying anything). **Last updated:** 2026-10-11, Session 01 (part 3).

# Lessons & dead ends — Shroom and Gloom accessibility

Findings from inspection, from running the mod in the game, and lessons carried in from sibling projects because they apply directly here. Each carried lesson names its source.

## Inspecting the game [Session 01]

- **TECHNIQUE — The IL2CPP metadata keeps the developers' names.** `global-metadata.dat` holds every class, method and field name and even the source file names. Splitting its identifier string table on null bytes gives the names in definition order, so a type's members sit next to its name. No dumper tool is needed for a first look. Script: `tools/Dump-MetadataNames.ps1`.
- **GOTCHA — The localisation bundles are LZ4-compressed.** Scanning them for text returns fragments only. Read text through the game at run time.
- **GOTCHA — The game uses a typewriter text effect (Febucci Text Animator).** Visible text is partial while it animates. Read the source string.
- **CHECK — "No screen-reader DLL" does not mean "no accessibility", and a stock Unity module name does not mean support.** `UnityEngine.AccessibilityModule` is in every recent Unity build.

## Loader [Session 01]

- **KEY FINDING — MelonLoader 0.7.3 starts under Unity 2022.3.62f3 IL2CPP** (2026-10-11). First start took about two minutes to download Unity support files and generate 156 interop assemblies; later starts are quick. The warning below did not apply to this build.
- **POINTER — Unity 2022.3.62 IL2CPP is a known trouble spot.** MelonLoader issue 1063 (crash at start on 2022.3.62f2, closed stale, no fix named); the Class of '09 project's `docs/known-issues.md` says BepInEx 6 Bleeding Edge crashed as well. Unconfirmed for 2022.3.62f3. Do not write mod code before Spike 1 passes.
- **POINTER — BepInEx 6 builds before about be.753 reject metadata version 31.** Use be.755 or later if BepInEx is tried.

## Carried from sibling projects

- **LESSON — One bad attribute patch can take every patch down** (Caves of Qud, lessons line 18). Patch by name, one try/catch each.
- **LESSON — Check the installed game build at every session start** (Dwarf Fortress, lessons line 298): Steam updated that game silently while the project was paused.
- **LESSON — Never dedupe player-initiated feedback; dedupe only unsolicited chatter** (Caves of Qud, lessons line 48).
- **LESSON — Interrupting speech cancels lines the player never heard** (Caves of Qud, lessons line 49): after a screen or popup title, queue the selection line.
- **LESSON — Trigger announcements on a change in game state, never on a change in rendered text** (Dwarf Fortress, lessons line 301).
- **DEAD END — Alt-based hotkeys** (Caves of Qud, lessons line 25). Ctrl+letter is the layer.
- **GOTCHA — NVDA takes the whole numpad when Num Lock is off** (Caves of Qud, lessons line 51).
- **LESSON — Write logs to a user-writable folder, never under Program Files** (Class of '09, lessons line 33).
- **LESSON — Speech is confined to the game's foreground window; compare the foreground window's process to our own** (Dwarf Fortress, lessons lines 126 and 127).
- **POINTER — Prism specifics** (Dwarf Fortress, lessons lines 379 and 380): one context, best backend, initialise, then speak and stop under one lock; it rejects invalid UTF-8; with NVDA 2026.2 its NVDA backend cannot report "still speaking".

## Building and tooling [Session 01]

- **GOTCHA — `dotnet build` cannot restore here.** No NuGet source is configured, so the .NET 6 reference pack is unavailable (error NU1100). Compile with `tools/Build-Mod.ps1`, which calls `csc.dll` with `-nostdlib` and references the installed .NET 6.0 runtime's managed DLLs directly.
- **GOTCHA — An MSBuild `-p:` property cannot carry a path with a comma** ("No, I'm not a Human"). Moot now, but the same applies to any tool that splits on commas.
- **GOTCHA — Windows PowerShell 5.1 reads `0xFAB11BAF` as a negative Int32.** Compare large hex constants as text.
- **KEY FINDING — Prism 0.18.3 works from C# on the .NET 6 runtime by plain P/Invoke** (verified 2026-10-11 outside the game): `prism_config_init` returns its struct by value without trouble, the best backend was NVDA, features `0xB5` (speak, braille, output, stop; no "is speaking"). Whether the line was audible is unconfirmed.
- **KEY FINDING — Claude's session will not download third-party code and unpack it into the game folder.** That step goes in `paste-commands.ps1` for the maintainer, with the checksum pinned in the script.

## How the game takes input [Session 01]

- **KEY FINDING — The game's focus navigation is switched off unless a gamepad is "driving".** The focus service reports `blockedBy=IsUsingGamepad` in keyboard-and-mouse mode. Calling `BasicInputSource.SetInputContext(Gamepad, ...)` wakes it at once. Any real keyboard or mouse activity flips the context back, so the mod prefixes that method and forces Gamepad.
- **KEY FINDING — Focus and confirm are bound to gamepad controls only.** Focus Prev/Next/Up/Down, Confirm, Cancel, Pause, Inspect, deck view, paging and sort hand have no keyboard binding. The game reads them through `S_InputManager.GetActionDown / GetAction / GetActionUp / GetAxis`; postfixing those four methods and answering from the keyboard is enough to drive every screen tried so far. No virtual gamepad device is needed.
- **GOTCHA — Polling a key's "pressed this frame" misses a tap.** A key pressed and released inside one frame reads as never pressed. Use an Input System action per key (`WasPressedThisFrame` on an action is event-driven).
- **GOTCHA — The game has developer cheat keys on Ctrl combinations and their input map is enabled in the release build.** List in `docs/controls-reference.md`. Whether they act is unverified. Keep mod commands off those keys.
- **KEY FINDING — Some world objects can only be focused after being looked at.** `EncounterStartWorldObject` carries a `ProximityFocusGate` that passes only once the camera has dwelt on the object. Setting the gate's `_seen` to true when the object reports `PlayerInRange` and `CanInteract` puts it in the focus candidates. This is what stalled the tutorial at the shovel for a long time: the symptom was "movement blocked, hand hidden, nothing to do".
- **PATTERN — Walking stops at authored points.** A `PauseMovement` trigger halts the player (the focus service then reports `stickWalks=False` and pushing the stick steps focus instead). When walking stops, look for what is in range, not for a wall.
- **PATTERN — The flow for using a card on something:** focus the card in hand, Confirm (mode becomes Target and the game focuses a valid target by itself), Confirm again. Card offers ("loot") open a list scope with the card and a Take button; Confirm on the card takes it.

## Describing things [Session 01]

- **KEY FINDING — Read models, not the screen.** Cards in the world are 3D objects, not UI text. A card's name is `IV_CardInstance.IModel.CardName`, its rules text `IModel.IData.SingleDescription`, its cost `GameplayCardData.EnergyUse`.
- **GOTCHA — Card text is TextMeshPro rich text** with colour, `nobr` and `link` tags. The link ids name keyword tooltips (`Card_TOOLTIP_WORD_CONSUME`), which is the route to keyword explanations.
- **GOTCHA — `CampStructureSpellReceiver.GetName()` returns an internal collider name.** Use the parent encounter's object name instead ("PT Dig Site Encounter(Clone)" becomes "Dig Site").
- **KEY FINDING — What a world object needs is in `GetPromptsAndInteractions()`**: a list of intents (Dig, Unlock, Break, Feed, Fix, Ignite, Reveal), each with a ready-made sentence in `EnvironmentPromptTooltip` ("Looks diggable."). Those strings contain a literal backslash-n.

## Working without eyes or focus [Session 01]

- **DEAD END — Sending keystrokes at the desktop to test.** The maintainer was working in another window; the keys never reached the game and some went to that window. Never use SendKeys or similar. Tests inject actions inside the game through the dev bridge.
- **DEAD END — Desktop screenshots.** They come out black (a screen curtain, most likely). The mod saves the game's own frame with `ScreenCapture.CaptureScreenshot`, which works with the window unfocused.
- **TOOL — The dev bridge** (`DevBridge.cs`, on only when `UserData\ShroomAccess\dev.flag` exists): commands dropped in a text file give screenshots, a UI text dump, a focus and player state dump, an object subtree dump, simulated presses and held walking. `tools/Test-Run.ps1` wraps it. The game keeps running and loading scenes while unfocused.
- **LESSON — Speech is gated on `Application.isFocused`,** so development does not talk over whatever else the maintainer is doing; the same line is always written to the log as `[say]`.
- **GOTCHA — A bash here-document holding C# with some quote and backslash mixes fails in this shell.** Write the helper script to a file and run the file.
- **GOTCHA — Called with `-File`, a PowerShell array parameter given as `a,b,c` arrives as one string.** Split it in the script.

## Speech while a key is held [Session 01]

- **KEY FINDING — A screen reader cancels its speech on every key press, and a held key repeats.** Nothing spoken through NVDA survives while W is held. Confirmed by the maintainer's second ear test (lines arrived only after releasing the key). NVDA's "Speech interrupt for typed characters" setting turns this off for character keys, but that is a per-user, NVDA-only setting.
- **KEY FINDING — Prism can run two backends at once.** The screen reader backend and a OneCore or SAPI backend created with `prism_registry_create` both speak; the second is not affected by the screen reader's cancel. Their rate is 0 to 1 with 0.5 as the voice's normal speed (`prism_backend_set_rate`); the screen reader backends report no rate at all.
- **GOTCHA — The player's screen reader rate is only readable for NVDA,** from `%APPDATA%\nvda\nvda.ini` (`[speech]`, `synth = X`, then `[[X]]`, `rate = N` out of 100). The number is specific to the synthesizer.
- **PATTERN — Generated sounds.** `AudioClip.Create` plus `SetData` with an `Il2CppStructArray<float>`, played with `PlayOneShot` on an `AudioSource` on a `DontDestroyOnLoad` object, works under IL2CPP with no asset files.

## Keys the game also listens to [Session 01, part 7]

- **GOTCHA — The game's Move action is bound to the arrow keys as well as W A S D, and ignores Ctrl.** Up and Down between rows walked the character out of reach of the shovel, and "Nearby: Shovel" was heard on entering the status row because Up had walked forward. The `GetAxis` hook now discards the game's answer for Move and returns W A S D (without Ctrl) or a real gamepad stick.
- **GOTCHA — The engine's UI input module navigates on arrows and W A S D by itself,** alongside the game's focus navigation and regardless of Ctrl. Setting `InputSystemUIInputModule.move = null` stops it; menus still work because the game's own focus actions drive them.
- **DEAD END — `EventSystem.sendNavigationEvents = false`.** It also kills submit and cancel. A simulated Pause action did not open the pause menu afterwards, which suggests Escape pauses through the UI cancel path. Left alone.
- **LESSON — A dev test that injects actions cannot find a bug that needs the real keyboard.** All three of these came from the maintainer's hands, not from replays.

## Menus and passing text [Session 01, part 11]

- **PATTERN — A settings row is an object with a `Focus...Section` component, a label as a direct child, and the control beside it.** The control's own text is its value ("ON", the dropdown's caption). Name the control by the row's label.
- **PATTERN — The game fades text with canvas groups and leaves the objects active.** "Is this text showing" means multiplying `CanvasGroup.alpha` up the parents, not checking `activeInHierarchy`. The loading screen is always active and usually transparent.
- **GOTCHA — The game pauses and freezes time when its window loses focus** (`MinimizeResumeHandler` in its log). Dev replays run with the window unfocused, so anything timed can behave differently from real play.
- **GOTCHA — A simulated Pause action does not open the pause menu; the real Escape key does.** Escape arrives through the UI module's cancel action.
- **LESSON — A replay that walks a settings screen changes the maintainer's settings.** Left and Right on a slider adjust it. Restore, or avoid.
