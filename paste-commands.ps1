# Shroom and Gloom accessibility - second listen (Session 01, part 4).
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
# CHECKLIST (second listen) - please report what you heard at each letter.
#
#   A. Start the game, wait for "Shroom Access loaded", press Enter on "New Run", wait ten seconds.
#
#   B. Hold W. Expect "Nearby: Dig Site." early on, then, when the game stops you, "Nearby: Shovel."
#
#   C. Press Up. Expect "Nearby, 2 things. Dig Site. Something is hidden here. Looks diggable."
#      Press Down. Expect "Shovel". (If Up or Down does nothing the first time, tell me which.)
#
#   D. Press Enter on Shovel. Expect "Offered: Rusty Shovel, cost 1. Consume. Enter takes it."
#      Press Enter. Expect "Your hand, 1 card. Rusty Shovel, cost 1. Consume."
#
#   E. Press Enter on the card. Expect "Choose a target. Dig Site. ..." in one go. Press Enter.
#      Expect "Offered: Key, cost 0. Consume. Handy. Enter takes it." Press Enter.
#
#   F. Was anything cut off by the line after it? Is any line too long now?
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
