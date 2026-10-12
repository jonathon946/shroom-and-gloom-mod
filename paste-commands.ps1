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
# CHECKLIST (after the shop playthrough) - report by letter, as far as you get.
# The save is fresh again: New Run starts the tutorial.
#
#   A. Density. When several things happen at once you should now hear one run of short sentences
#      in a steady order: the fight or camp first, then what is nearby, then whose turn, then
#      "Drew 4 cards", then energy. Better, worse, or still too much? Is anything missing that
#      you used to hear?
#
#   B. Dealing. When cards are dealt you should hear only "Drew N cards." and then the card you
#      are on, not every card.
#
#   C. Toasty (or any card with no target): the first Enter should say
#      "Toasty is ready. Enter again to play it, Backspace to put it back."
#
#   D. The shop, if you reach one. I have not been able to run it myself, so everything there is a
#      first guess: an instruction line on entry, "For sale: n food" on each card, "Forget a card",
#      and the bowl. Please mark (Ctrl+B) each step you try: choosing a card, paying, leaving.
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
