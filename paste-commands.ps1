# Shroom and Gloom accessibility - listening checklist for the Session 02 build (2026-10-12).
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
# CHECKLIST (Session 02 build) - report by letter, as far as you get.
# Your far run is back: Continue resumes it at a camp with a Grimoire.
#
#   A. Help. Ctrl+Right four times reaches the Help buffer; Ctrl+Up reads on. Is anything
#      missing, wrong, or too long?
#
#   B. The Grimoire (or a Strange Geode or Practice Dummy). You should hear the message first,
#      then your cards. Enter on a card, Enter on the panel. Then: "The card is now: ...
#      Enter takes it. Left is No thanks."
#
#   C. In a fight, an enemy should say "Will attack for 5" or "Will block 3" in place of
#      "Intends Deal Damage 5".
#
#   D. When you have a status on yourself (Burning Sensation or any other): Ctrl+Right to the
#      Player buffer, Ctrl+Up through it. The status should be listed with what it does.
#
#   E. Still unheard from last time: in a fight, the Enemies row holds only enemies; arriving
#      somewhere busy gives one grouped "Nearby:" line; an enemy status ends with what it does.
#
#   F. A shop: each card says "For sale", its price, and its place ("top row, 2 of 4").
#      Enter on a card; the mole says how much food. Your hand is then "Your food".
#      Enter on a food card, Enter on the bowl. Repeat until paid. Mark each unclear step.
#
#   G. The map (M). The first line gives the number of areas and where you are; Ctrl+Up reads
#      each area. Does M close it again?
#
#   H. Only if you start a new run: the class screen should say "locked" on the two locked
#      characters. (A brand-new game also says "A hand holds up a note..." at the very start.)
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
