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
# CHECKLIST (after your 23 marks) - report by letter, as far as you get.
# The save is fresh again: New Run starts the tutorial.
#
#   A. Walking. W and S should only ever walk. Where the game will not let you walk, pressing W
#      says "Can't walk right now" and why. Holding W against a dead end gives a low sound and
#      "Can't go further this way". Does W ever move you between rows now?
#
#   B. Walking voice: several things coming into reach close together should be read one after
#      another, none cut off.
#
#   C. Rows: each item ends with its place, "2 of 3". At the end of a row you should hear a low
#      sound and nothing else. Is the sound clear enough? Too loud or too quiet?
#
#   D. At the first camp (after the matchstick and torch): Enter on the Rusted Hammer with no energy
#      should say "Not enough energy... Space ends the turn." After Space, "New turn." then
#      "Energy 3 of 3."
#
#   E. Casting a card should end with "Played <card>." A card that cannot be used says so.
#
#   F. The backpack: expect one line naming all the cards on offer, then the card you are on.
#
#   G. Up to Status, then Enter on Health, Energy or the Warmth line: the game's own description.
#      Ctrl+Right to Player: your foresights are listed at the end.
#
#   H. In a camp room, the Nearby row should name plots ("Empty plot, back left") and chests
#      ("Foresight chest"). When you choose a Camp card, the targets should be those plots.
#
#   I. The card-change screen (Strange Geode): after its message, "Choose a card to change".
#      Enter on a card, then Enter again on the panel to apply. Tell me if that works.
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
