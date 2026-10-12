# Shroom and Gloom accessibility - listen through the first fight (Session 01, part 8).
#
# There is nothing to run here except starting the game. Either start it from Steam as usual,
# or run this file, which starts it for you:
#
#   powershell -ExecutionPolicy Bypass -File "C:\Users\Sam\Documents\projects\shroom and gloom mod\paste-commands.ps1"
#
# Keep NVDA running and keep the game window focused. The mod is silent while another window has focus.
#
# KEYS
#   Up, Down                move between rows: Status (top), Nearby, Your hand (bottom)
#   Left, Right             move along the current row
#   Enter                   choose / confirm          Backspace   cancel / back
#   Escape                  pause menu
#   W (hold)                walk forward              S (hold)    walk back
#   Space                   end turn (in camp: rest for the night)
#   Ctrl+B                  mark this moment for review (it says "Mark 1", "Mark 2"...)
#   Ctrl+Up, Ctrl+Down      read the next / previous line of the buffer
#   Ctrl+Right, Ctrl+Left   switch buffer: Item, Player, Surroundings, Messages
#   Ctrl+Page Up / Down     walking voice faster / slower
#
# CHECKLIST (menus and silent text) - report by letter, as far as you get.
#
#   A. Title screen. Up to "Menu", Enter. Expect Settings, Credits, Quit with Up and Down.
#
#   B. Settings. Enter on Settings lands on the "SOUND, tab". Left and Right move between the tabs
#      Sound, Graphics, Game, Controls. Down goes into the tab.
#      Each row should say its name, what kind of control it is, and its value, for example
#      "MASTER VOLUME, slider, 55 percent". Left and Right change a slider and say the new value.
#      Enter on an ON / OFF row flips it and says the new state.
#      Enter on a dropdown opens its list: tell me what you hear there, I have not tested it.
#
#   C. Game tab: after Language you should hear the game's own heading
#      "ACCESSIBILITY. EXPERIMENTAL/WIP. Probably wont work with big hands..." before Head Bob.
#      Controller Support carries a warning: leave it on, the mod depends on it.
#
#   D. Close settings (Up to "Close", Enter, or Backspace). Credits: Enter on Credits, then
#      Ctrl+Up steps through the credits a line at a time. Backspace or "Skip" leaves.
#
#   E. New Run. While it loads expect "Loading." and a tip. If the game waits, "Press Space Bar".
#
#   F. In play: any moment where sound happens and nothing is said, press Ctrl+B.
#      That is now the main thing I need from you.
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
