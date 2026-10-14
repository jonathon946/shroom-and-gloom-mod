Shroom Access
A screen reader mod for Shroom and Gloom.

This is an unfinished test build. It speaks through NVDA, JAWS or another screen reader if one is
running, and through a Windows voice if none is.


WHAT YOU NEED

- Shroom and Gloom on Steam (Windows).
- MelonLoader 0.7.3 or later installed into the game. Get the installer from
  https://github.com/LavaGang/MelonLoader/releases and point it at the game. Start the game once
  after installing it and let it finish its first-time setup, which takes a few minutes.


INSTALLING

Copy the Mods and UserLibs folders from this zip into the game's folder, next to
"Shroom and Gloom.exe". To find that folder in Steam: right-click the game, Manage, Browse local
files.

Start the game. After the logo you should hear "Shroom Access loaded".

In the game's settings, on the Game tab, leave Controller Support on. The mod steers the game
through it.


KEYS

Up and Down arrows     move between rows: Status, Nearby (or Enemies), Your hand
Left and Right arrows  move along a row
Enter                  choose, play, confirm
                       opens a sealed pack of cards (a reward after a run)
Backspace              cancel, go back
Escape                 pause menu
W and S (hold)         walk forward and back
Space                  end turn; at a camp, rest for the night
                       after a death, move through the words on screen
M                      map
Tab                    your draw pile; again, the discard pile; again, closes it
Control Up and Down    read the next and previous line of the current buffer
Control Left and Right switch buffer: Item, Player, Surroundings, Messages, Help
Control Page Up, Down  walking voice faster, slower
Control B              mark this moment (for bug reports)
Control O              after a new version is announced: open its download page
Control N              after a new version is announced: download and install it (press twice)

The Help buffer in the game has the same list.

Avoid other Control combinations in the game: some are developer cheat keys.


UPDATES

Once each time the game starts, the mod asks GitHub whether a newer version of Shroom Access has
been released. If one has, the title screen says so. Control O opens its download page in your
browser. Control N, pressed twice, downloads it, checks it against the checksum published with the
release, and puts the new files in place; restart the game to use it. If anything goes wrong,
nothing is changed. Only the mod's own files are written. To turn the check off, close the game,
open UserData\MelonPreferences.cfg in the game folder and set CheckForUpdates = false under
[ShroomAccess].


CHANGING KEYS

Close the game and open UserData\MelonPreferences.cfg in the game folder. Under
[ShroomAccessKeys] each action has its keys, separated by commas, for example
    Confirm = "enter, numpadEnter"
Key names are those of Unity's Input System: letters (a to z), digits, space, enter, tab,
backspace, escape, upArrow, downArrow, leftArrow, rightArrow, pageUp, pageDown, numpad1 and so on.
The entries starting CtrlPlus are the letters of the mod's own Control commands. A name the mod
does not recognise is ignored and the usual key kept. The Help buffer still names the usual keys.


TRANSLATING

Everything the mod says in its own words is listed in Mods\ShroomAccess\strings-template.txt.
Copy it to strings.txt in the same folder and change the text after " = " on each line, keeping
{0}, {1} and so on where the values go. Lines left out are said in English. Names and texts that
come from the game itself are already in the game's language.


REMOVING

Delete Mods\ShroomAccess.dll, the Mods\ShroomAccess folder, UserLibs\prism.dll and
UserLibs\prism-licenses from the game folder.


CREDITS AND LICENCES

Speech uses Prism by Ethin Probst (https://github.com/ethindp/prism), MPL-2.0. Its licence files
are in UserLibs\prism-licenses.
Shroom and Gloom is by Team Lazerbeam, published by Devolver Digital. This mod is not affiliated
with either and contains none of the game's files.
