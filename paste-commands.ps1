# Shroom and Gloom accessibility - third listen (Session 01, part 5).
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
#   Ctrl+Down, Ctrl+Up      read the next / previous line about the focused thing
#   Ctrl+Right, Ctrl+Left   switch buffer: Item, Player
#   Ctrl+Page Up / Down     walking voice faster / slower
#
# CHECKLIST (third listen) - please report what you heard at each letter.
#
#   A. New Run, then hold W and KEEP HOLDING. Expect, while still walking: a short two-note sound
#      and a different voice saying "Nearby: Dig Site." and later "Nearby: Shovel."
#      That second voice is a Windows voice, used only while a walk key is down.
#      Ctrl+Page Up makes it faster, Ctrl+Page Down slower; it says the new rate. Set it to taste.
#      Tell me: could you hear it over the footsteps? Was the starting speed close to your own?
#
#   B. Press Right. Expect "Nearby, 2 things." and one of them. Press Left and Right: they should
#      only ever move between the dig site and the shovel, and repeat the item at each end.
#
#   C. Press Up. Expect "Status, 3 items. Health 25 of 25". Right: "Energy 1 of 3". Right: "Gunk 10".
#      Press Down to return to the Nearby row.
#
#   D. Take the shovel (Enter on Shovel, Enter on the offer). With the card focused in your hand,
#      press Ctrl+Down several times. Expect one line each: the name, "Cost 1 energy",
#      "Explore deck", "CONSUME", then "CONSUME: This card will be destroyed upon being played."
#      Ctrl+Up goes back. Ctrl+Right: "Player. Health 25 of 25". Ctrl+Left: back to "Item".
#
#   E. From the hand, press Up: Nearby row. Press Down: back to your hand.
#
#   F. Dig, take the key, open the door, hold W through it, let go. Expect "Nearby: ..." naming the torch.
#      Go to the torch and press Enter. Tell me what you hear, if anything, and whether
#      "Added to hand: ..." is spoken.
#
#   G. Does Ctrl+arrow behave with NVDA (Ctrl on its own stops speech)? Anything spoken twice?
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
