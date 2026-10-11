# Shroom and Gloom accessibility - first ear test (Session 01).
#
# There is nothing to run here except starting the game. Either start it from Steam as usual,
# or run this file, which starts it for you:
#
#   powershell -ExecutionPolicy Bypass -File "C:\Users\Sam\Documents\projects\shroom and gloom mod\paste-commands.ps1"
#
# Keep NVDA running and keep the game window focused. The mod is silent while another window has focus.
#
# KEYS
#   Left, Right, Up, Down   move between things
#   Enter                   choose / confirm
#   Backspace               cancel / back
#   Escape                  pause menu
#   W (hold)                walk forward        S (hold)  walk back
#
# CHECKLIST - please report what you heard at each letter.
#
#   A. Start the game and wait about 20 seconds. Expect: "Shroom Access loaded."
#      Note: the very first screen may say "Press Space Bar" (not spoken yet). If nothing happens
#      after 30 seconds, press Space once.
#
#   B. Press Right, then Left, a few times. Expect names such as "New Run",
#      "Reach the Gloom at the end of the Forgotten Path, undiscovered", "Path 03, coming soon".
#      Press Up. Expect "Hamburger Button" (that is the menu button; the name will be fixed).
#
#   C. Move to "New Run" and press Enter. Wait about 10 seconds for the forest to load. Nothing is
#      spoken on arrival yet.
#
#   D. Hold W for about five seconds, then let go. The game stops you near a shovel.
#      Press Down. Expect "Shovel Choice". (Up may say "Dig Site. Something is hidden here. Looks diggable.")
#
#   E. With "Shovel Choice" spoken, press Enter. Expect "Rusty Shovel, cost 1. Consume."
#      Press Enter again to take it. Expect the card to be read again: it is now in your hand.
#
#   F. Press Enter on the shovel card. Expect "Choose a target." then "Dig Site. ..."
#      Press Enter. Expect, after a few seconds, "Key, cost 0. Consume. Handy."
#      Press Enter to take the key.
#
#   G. Press Enter on the Key card. Expect "Choose a target." then "Catacomb Tutorial Door. Looks unlockable. ..."
#      Press Enter. The door opens (not spoken yet).
#
#   H. Press Escape. Expect "Unpause". Press Down for the other items (Settings, Credits, Main Menu, Quit).
#      Press Enter on Unpause to go back. You can stop here: quit from the pause menu, or Alt+F4.
#
# Also worth telling me: was anything cut off, too slow, too long, or spoken twice? Did any key do
# nothing, or do something twice?
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
