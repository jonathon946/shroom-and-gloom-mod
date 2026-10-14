# Shroom and Gloom accessibility - listening checklist for the Session 04 roadmap build (2026-10-14).
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
# CHECKLIST (Session 04, roadmap build, 2026-10-14) - report by letter, as far as you get.
# Your save has no run in progress: the title offers New Run.
#
#   A. Deck view: in a run, Tab opens your draw pile with a list of its cards; Tab again the
#      discard pile; Tab again closes it. Up from the cards reaches buttons for the full deck and
#      the combat, explore and foresight decks. Is the list useful? Too long?
#
#   B. Cards say their small label after the name: "Flash, Battery 10/10". A card refused for a
#      dead battery should say so.
#
#   C. In a fight, the Status row (Up from Enemies) has "Incoming: N damage from M attacks".
#
#   D. Enemy hits are named: "Brain Baby hits you for 6". Thorns: "Thorns on Lekker Limbs: lost 1".
#
#   E. At a camp, Up to Nearby, then the Encroaching Gloom and creatures: the threat they add and
#      what they do ("If you rest, take 5 damage").
#
#   F. Timing: "Your turn" with the draw sound; lines generally close to what happens.
#
#   G. (new 2026-10-14, after the Megashroom run) A card played on another card in a fight
#      (Vile Oil, Deadly Mould): "Choose one of your cards. Stab, cost 1. Deal 4 Damage, 1 of 5",
#      and each arrow reads the card and its place.
#
#   H. A card-change screen (Grimoire, Practice Dummy): after picking a card, "Stab gains: ADD 5
#      DAMAGE. Enter applies it". An extra Enter while the result is coming is ignored, so it can
#      no longer land on No thanks. If the change makes a card, the opening line should also say
#      what that card does ("Repressed Rage does: ...").
#
#   I. Several enemies summoned at once are said in one line: "Joined the fight: 4 Mega Babies."
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
