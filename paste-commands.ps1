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
# CHECKLIST (after the first full playthrough) - report by letter, as far as you get.
#
#   A. At the title, "Continue" resumes your run just after the fight; it is no longer stuck.
#      "New Run" now reads its question: "END PREVIOUS RUN? A new run ends your previous run..."
#
#   B. Buffers. Focus a card. Ctrl+Up reads the next line each time, Ctrl+Down goes back.
#      Move to another card and press Ctrl+Up: it should start from the top of the new card.
#      Ctrl+Right steps through Player, Surroundings, Messages; Ctrl+Left goes back.
#      After moving focus you should be back in the Item buffer without doing anything.
#
#   C. Messages buffer: Ctrl+Right to "Messages" reads the newest line; Ctrl+Up goes back in time.
#
#   D. A card that acts on you (Toasty): Enter should now say
#      "This card acts on you. Enter to play it, Backspace to cancel."
#
#   E. Screens that open (a chest, a foresight choice, the class choice) should read their heading.
#      Tell me any that stay silent, and press Ctrl+B there.
#
#   F. Ctrl+Up on Terry, Touchy, Stab or Roast, to the end: a "Looks:" line.
#
# IF THERE IS NO SPEECH AT ALL: tell me, and I will read the log. Nothing else is needed from you.

$game = 'C:\Program Files (x86)\Steam\steamapps\common\Shroom and Gloom'
if (Get-Process 'Shroom and Gloom' -ErrorAction SilentlyContinue) {
    Write-Host 'The game is already running. Switch to its window.'
} else {
    Start-Process -FilePath (Join-Path $game 'Shroom and Gloom.exe') -WorkingDirectory $game
    Write-Host 'Game starting. Switch to its window if it does not take focus, and wait for "Shroom Access loaded".'
}
