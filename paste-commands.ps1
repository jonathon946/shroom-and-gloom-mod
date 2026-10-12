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
# CHECKLIST (after the sixth playthrough) - report by letter, as far as you get.
# Your far run is back: Continue resumes it.
#
#   A. In a fight, Left and Right along the Enemies row should give only enemies, no torches.
#
#   B. Ctrl+Up on an enemy with a status (Seasoning, Vulnerable, Timid): the status line should now
#      end with what it does. Tell me any status that has no explanation.
#
#   C. Arriving somewhere busy: "Nearby: 2 Doors, 3 Chests..." in one grouped line.
#
#   D. A shop: each card says "For sale", its price, and its place ("top row, 2 of 4").
#      Enter on a card; the mole says how much food. Your hand is then "Your food".
#      Enter on a food card, Enter on the bowl. Repeat until paid. Mark each step that is unclear.
#
#   E. Ctrl+Up to the end on any card or creature for its "Looks:" line. 103 are written.
#
#   Keep marking with Ctrl+B anywhere sound happens with no speech.
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
