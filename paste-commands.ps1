# Shroom and Gloom accessibility - listening checklist for the Session 04 part 2 build (2026-10-14).
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
# CHECKLIST (Session 04 part 2 build, 2026-10-14) - report by letter, as far as you get.
# Your save is as you left it: in the shop in tunnel 2, after buying Assassinate.
#
#   A. Quieter: after playing a card, after a draw, at a new turn and after paying, nothing should
#      be read about the card you land on. The next arrow reads as usual. Too quiet anywhere?
#
#   B. Fights open with "Enemies: 2 Terrys, 1 Touchy"; while walking, the names should now be
#      clearly separate.
#
#   C. The edge sound should come sooner. In the backpack's pile it should play only at the ends.
#
#   D. Ctrl+Up at the end of the map (or any buffer): the edge sound, no line repeated.
#
#   E. A practice dummy, grimoire or geode: "placed", what it does, "Choose one of your cards",
#      and then quiet until you move.
#
#   F. Shop: Ctrl+Right to the Player buffer starts with "Food to spend". Choosing a card says
#      "Costs 2 food..." once. Paying: "Paid 1 of 2" only. Pause and unpause mid-purchase: no
#      "Combat started" or "Combat over", and the shop's opening is not read again.
#
#   G. The Paper Boy page: the quote, how many starting cards are found, where the foresight is,
#      and the Coming soon row, in words.
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
