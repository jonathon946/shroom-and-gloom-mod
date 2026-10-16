# Shroom and Gloom accessibility - listening checklist for the Session 04 roadmap build (2026-10-14; item O added 2026-10-16, Session 05).
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
#   J. (new 2026-10-14, after Leanne's report of Enter landing on the wrong card) In a fight, Left
#      and Right through the hand as fast as you can: each card should cut off the one before, with
#      no trail of old cards still being read, including straight after "Drew 4 cards" or a hit.
#      The hand no longer wraps: past the last card is the edge sound.
#
#   K. After playing a card, press Enter again without arrowing: the mod reads the card now under
#      the cursor instead of playing it ("Toasty, cost 0 ..."); the next Enter plays it. Another
#      card of the same name (Toasty after Toasty) plays at once. Right on a one-card hand: the
#      edge sound and that card read.
#
#   L. With two enemies of the same name, targets say their place: "Gill Beast ..., 2 of 3".
#
#   M. When the attacks shown add up to your health or more: "Warning: the enemies will deal 30
#      damage next turn, and you have 11 health." The Megashroom's explosion now counts.
#
#   N. Is the game smoother in fights? The mod's own work per frame dropped from about 22 to 2
#      milliseconds there.
#
#   O. (new 2026-10-16, after Leanne's report of lag creeping in late in a run) Play a long run,
#      past a camp or two and some reward screens. Do arrows in a camp and on reward choices stay
#      as quick near the end as at the start? Built and tested here; not yet released.
#
#   For Leanne, if lag still creeps in: before quitting the game, note the time; then send the
#   newest file from the game folder's MelonLoader\Logs folder (or MelonLoader\Latest.log while
#   the game is still closed and not yet restarted). Every 30 seconds it records the mod's and the
#   game's frame times and memory, which shows whether anything still piles up.
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
