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
#   Ctrl+Down, Ctrl+Up      read the next / previous line about the focused thing
#   Ctrl+Right, Ctrl+Left   switch buffer: Item, Player
#   Ctrl+Page Up / Down     walking voice faster / slower
#
# CHECKLIST (listen through the first fight) - report what you heard, by letter, as far as you get.
#
#   A. Play the opening as before: shovel, dig, key, door. Cards now read their use, for example
#      "Rusty Shovel, cost 1. Dig. Consume." The door reads "Door. This is a door! Looks unlockable..."
#
#   B. Walk on. Expect "Nearby: Torch, Door." Keep walking until the game stops you and gives you a
#      Matchstick. Press Enter on it, Enter on the Torch. Expect, a moment later,
#      "Warmth: 3. Threat: 0. Safe for 3." and a Rusted Hammer in your hand.
#
#   C. Up twice to the Status row and Right along it. Expect Health, Energy, Gunk, the warmth line,
#      and the piles. Ctrl+Down on Health or Energy reads the game's own description of it.
#
#   D. The hammer costs 1 and you have 0 energy. Press Space to rest for the night.
#      Expect "New turn." and, a few seconds later, "Energy 3 of 3."
#
#   E. Enter on the hammer, Enter on the Door. Walk on (W). Expect new things nearby, a Backpack
#      among them. Open it, take what it offers, and carry on as you see fit.
#
#   F. When a fight starts expect "Combat." Up goes to the "Enemies" row; each enemy reads its name,
#      health, what it intends, and any status. Ctrl+Down gives the detail.
#      Play a card on an enemy: expect "<name> takes 3, 2 left." or "<name> dies."
#      Space ends your turn: expect "Enemy turn.", any health you lose, then "Your turn."
#
#   G. The questions: is the fight too chatty? Is anything said in the wrong order, or cut off?
#      Is there anything you could not find out that you needed?
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
