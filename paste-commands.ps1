# Shroom and Gloom accessibility - listening checklist for the Session 04 build (2026-10-14).
#
# There is nothing to run here except starting the game. Either start it from Steam as usual,
# or run this file, which starts it for you:
#
#   powershell -ExecutionPolicy Bypass -File "C:\Users\Sam\Documents\projects\shroom and gloom mod\paste-commands.ps1"
#
# Keep NVDA running and keep the game window focused. The mod is silent while another window has
# focus, and the game pauses itself when you switch away.
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
#   Ctrl+Right, Ctrl+Left   switch buffer: Item, Player, Surroundings, Messages, Help
#   Ctrl+Page Up / Down     walking voice faster / slower
#
# CHECKLIST (Session 04 build) - report by letter, as far as you get.
# Your save is as you left it after your last death: the title offers New Run.
#
#   A. The two voices (still open from last time). Do they ever talk over each other? Does the
#      walking voice wait too long after NVDA, or start too soon?
#
#   B. A new turn in a fight should sound like: "Enemy turn", what happened to you, then
#      "Your turn. Energy 3 of 3. Drew 4 cards. Your hand, 4 cards." and the card. Better?
#
#   C. Hitting an enemy that has armour: "3 blocked by armour". In their turn: "gains 3 armour".
#
#   D. Persistent cards (Toasty Kebab, soups) should say "Played", not "can't be played".
#      Unplayable cards (Sponge Shield, Pet Rat) say they work by themselves. If any card says
#      "was not played", press Ctrl+B there.
#
#   E. A shop: no "Combat started" while paying; "Paid 1 of 3" for each food; "Bought ..." at the end.
#
#   F. A soup pot (build one from a Soup Pot card at a camp): the opening line lists the soups with
#      what each needs and whether it is open; "Soup value 1. Now open: ..." as food goes in;
#      Up goes to the open soups; "Made ... It is in your hand." Is it clear how it works?
#
#   G. Feeding: Enter on a food card says "Aimed at you"; Left and Right reach hungry creatures,
#      and you are said as "You".
#
#   H. Sounds (the arrival notes and the low edge note) should keep working after a death and back
#      at the title. Last time they stopped.
#
#   Keep marking with Ctrl+B anywhere something seems wrong or silent.
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
