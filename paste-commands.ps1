# Shroom and Gloom accessibility - listening checklist for the Session 03 build (2026-10-12).
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
#   Ctrl+Right, Ctrl+Left   switch buffer: Item, Player, Surroundings, Messages, Help
#   Ctrl+Page Up / Down     walking voice faster / slower
#
# CHECKLIST (Session 03 build) - report by letter, as far as you get.
# Your save is as you left it: no run in progress, so the title offers New Run.
#
#   A. The two voices. Do they ever talk over each other now? If they do, mark it (Ctrl+B) and
#      tell me which one started second. If the walking voice waits too long after NVDA has
#      finished, or starts before NVDA has finished, tell me which.
#
#   B. Walking. Things coming into reach should make the sound and nothing else. When you stop
#      at a camp: "You have stopped at a camp. Seen nearby: ..." Is that enough, or do you miss
#      hearing things as you pass?
#
#   C. A fight starting: one line, "Combat started. Enemies: ... Drew 4 cards", then the card
#      you are on. After the fight: "Combat over. Seen nearby: ..."
#
#   D. A Grimoire, Strange Geode or Practice Dummy: message, then your cards. Enter on a card
#      says "Choose a target ... Enter applies it". Enter again: "The card is now: ... Enter
#      takes it." No silent step. Arrows on that last screen make the edge sound.
#
#   E. Ctrl+Up on an enemy: "Will deal 3 damage" (not "will deals").
#
#   F. Dying. You should hear "You have died", then words and "Space continues". Space until
#      the page of numbers is read. If a quest finished: "A sealed pack of cards. Enter opens
#      it", then the cards, then Enter on "Woo". Then Enter on Continue. Tell me where that
#      leaves you, and whether Enter also works where I say Space.
#
#   G. Still unheard: a shop ("For sale", price, "top row, 2 of 4"; Enter on a card, then pay
#      with food cards on the bowl), and the map (M; Ctrl+Up reads each area; does M close it?).
#
#   H. On the class screen the two locked characters should say "locked".
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
