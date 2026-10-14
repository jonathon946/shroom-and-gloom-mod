using System;
using System.Linq;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppControllerNav;
using UnityEngine;

namespace ShroomAccess
{
    // Lays play out as rows the player can hold in their head:
    //   Status        health, energy, gunk, piles      (the mod's own row; nothing in the game is focused)
    //   Nearby        things in the world              (the game's world focus)
    //   Your hand     cards                            (the game's hand focus)
    // Up and Down move between rows. Left and Right move along a row and never leave it.
    // The game's own stepping is spatial (Up from one object can land on another), so on these
    // screens the navigator takes the arrow keys and moves the game's focus itself.
    //
    // Ctrl+arrows read buffers, in the manner of Say the Spire: Ctrl+Up/Down move through the lines
    // about the focused thing, Ctrl+Left/Right switch buffer.
    internal static class Navigator
    {
        internal static Action<string> Log = _ => { };

        private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<StatusEffect> _allStatuses;
        private static bool _inStatus;
        private static int _statusIndex;


        internal static bool InStatusRow => _inStatus;

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                FocusMode mode = focus.Mode;
                // A screen that offers nothing to focus (the map held up, a chest still rising) leaves
                // the player with no row at all unless the status row stays reachable.
                bool idleScreen = focus.Scope != null && mode == FocusMode.Suppressed;
                bool rows = (focus.Scope == null && (mode == FocusMode.Free || IsWorld(mode) || IsHand(mode))) || idleScreen;
                InputBridge.NavigatorOwnsArrows = rows;
                if (!rows) _inStatus = false;

                if (InputBridge.CtrlHeld)
                {
                    int dr = (InputBridge.Key(InputActionId.PagePrev) ? 1 : 0) - (InputBridge.Key(InputActionId.PageNext) ? 1 : 0);
                    if (dr != 0) Settings.NudgeDirectRate(dr);
                }

                if (_inStatus && rows && !InputBridge.CtrlHeld && InputBridge.Key(InputActionId.Confirm))
                {
                    _buffer = 0;
                    List<string> detail = Lines(focus);
                    FocusProbe.Speak(detail.Count > 1 ? string.Join(". ", detail.GetRange(1, detail.Count - 1)) : Loc.T("status.nothing_more", "The game says nothing more about this."));
                }

                int dx = (InputBridge.Key(InputActionId.FocusNext) ? 1 : 0) - (InputBridge.Key(InputActionId.FocusPrev) ? 1 : 0);
                int dy = (InputBridge.Key(InputActionId.FocusDown) ? 1 : 0) - (InputBridge.Key(InputActionId.FocusUp) ? 1 : 0);
                if (dx == 0 && dy == 0) return;

                if (InputBridge.CtrlHeld) { Buffers(focus, dx, dy); return; }

                // A pile of cards offered all together (a backpack): the game shows them fanned out
                // but lets a gamepad focus only the pile and the Take button. Left and Right walk
                // the pile here, so each card can be read and inspected before taking.
                if (dx != 0 && focus.Scope != null && focus.Scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name.Contains("LootAll"))
                {
                    Il2CppSystem.Object onNow = focus.CurrentFocus;
                    if (onNow != null && onNow.TryCast<IV_CardInstance>() != null) { PileStep(focus, dx); return; }
                }
                if (!rows) return;

                int world = focus.WorldFocusCandidates != null ? focus.WorldFocusCandidates.Count : 0;
                int hand = HeldCount(focus);

                if (idleScreen && !_inStatus) { EnterStatus(focus); return; }

                if (_inStatus)
                {
                    if (dx != 0) StatusStep(focus, dx);
                    else if (dy > 0)
                    {
                        if (world > 0) EnterNearby(focus, mode);
                        else if (hand > 0) EnterHand(focus);
                        else Edge(focus);
                    }
                    else Edge(focus);
                    return;
                }

                if (IsWorld(mode))
                {
                    if (dx != 0) NearbyStep(focus, dx);
                    else if (dy < 0) EnterStatus(focus);
                    else if (hand > 0) EnterHand(focus);
                    else FocusProbe.Speak(Loc.T("hand.none_now", "No cards in hand right now."));
                }
                else if (IsHand(mode))
                {
                    if (dx != 0) HandStep(focus, dx);
                    else if (dy < 0) { if (world > 0) EnterNearby(focus, mode); else EnterStatus(focus); }
                    else Edge(focus);
                }
                else // nothing focused yet: any arrow goes to the most useful row
                {
                    if (dy < 0) { if (world > 0) EnterNearby(focus, mode); else EnterStatus(focus); }
                    else if (dy > 0) { if (hand > 0) EnterHand(focus); else if (world > 0) EnterNearby(focus, mode); else EnterStatus(focus); }
                    else if (world > 0) EnterNearby(focus, mode);
                    else if (hand > 0) EnterHand(focus);
                    else EnterStatus(focus);
                }
            }
            catch (Exception ex)
            {
                Log("[nav] " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // FocusMode has more members at run time than the generated enum lists by name.
        private static bool IsWorld(FocusMode mode) => mode.ToString() == "World";
        private static bool IsHand(FocusMode mode) => mode.ToString() == "Hand";

        private static int HeldCount(S_ControllerFocus focus)
        {
            try
            {
                IM_Hand hand = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
                return hand != null && hand.HeldCards != null ? hand.HeldCards.Count : 0;
            }
            catch { return 0; }
        }

        // ---- Nearby row ----

        // Left to right as the player would see them.
        internal static List<IControllerFocusTarget> NearbyInOrder(S_ControllerFocus focus)
        {
            var list = new List<IControllerFocusTarget>();
            WorldFocusCandidates world = focus.WorldFocusCandidates;
            Camera cam = focus.FocusCamera;
            var keyed = new List<KeyValuePair<float, IControllerFocusTarget>>();
            for (int i = 0; world != null && i < world.Count; i++)
            {
                IControllerFocusTarget t = world.At(i);
                if (t == null) continue;
                // In a fight the game keeps the room's furniture in its list but will not focus it.
                if (focus.InCombat && !WorldFocusCandidates.InCombatScope(t)) continue;
                float x = i;
                try { if (cam != null) x = cam.WorldToViewportPoint(t.FocusWorldPosition).x; } catch { }
                keyed.Add(new KeyValuePair<float, IControllerFocusTarget>(x, t));
            }
            keyed.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var pair in keyed) list.Add(pair.Value);
            return list;
        }

        private static void EnterNearby(S_ControllerFocus focus, FocusMode mode)
        {
            _inStatus = false;
            if (IsWorld(mode)) { FocusProbe.Reannounce(); return; }
            // The game's own step up is what moves from the hand, or from nothing, to the world.
            InputBridge.Forward(InputActionId.FocusUp);
        }

        private static void NearbyStep(S_ControllerFocus focus, int dx)
        {
            List<IControllerFocusTarget> list = NearbyInOrder(focus);
            if (list.Count == 0) { Edge(focus); return; }
            IControllerFocusTarget current = focus.WorldResolver != null ? focus.WorldResolver.Focused : null;
            int index = -1;
            for (int i = 0; i < list.Count; i++)
                if (current != null && list[i].Pointer == current.Pointer) { index = i; break; }
            int next = index < 0 ? 0 : index + dx;
            if (next < 0 || next >= list.Count) { Edge(focus); return; }
            focus.WorldResolver.SetFocus(list[next], "ShroomAccess row step");
        }

        // ---- Hand row ----

        private static void EnterHand(S_ControllerFocus focus)
        {
            _inStatus = false;
            if (!focus.TryPushHand()) InputBridge.Forward(InputActionId.FocusDown);
        }

        // Left and Right move the game's hand cursor here, in the same frame as the key: handing
        // the arrow to the game costs a frame, and in a fight a frame can be a tenth of a second,
        // long enough for a quick Enter to land on the card before the one just read. The game's
        // own step also wraps from the last card to the first; this one stops at the ends.
        // For a moment after a play the game's hand still lists the card on its way out; the
        // cursor can land on it, and the player would hear nothing they could choose. Only cards
        // still held are stepped to.
        private static void HandStep(S_ControllerFocus focus, int dx)
        {
            HandFocusResolver hand = focus.HandResolver;
            List<int> slots = HeldSlots(focus, out IntPtr[] views);
            if (hand == null || slots.Count == 0)
            {
                InputBridge.Forward(dx > 0 ? InputActionId.FocusNext : InputActionId.FocusPrev);
                return;
            }
            // Where the cursor is among the held cards; on a card leaving, or on none, the step
            // goes to the nearest held card in the direction pressed.
            Il2CppSystem.Object current = focus.CurrentFocus;
            IntPtr at = current != null ? current.Pointer : IntPtr.Zero;
            int place = slots.FindIndex(s => views[s] == at);
            int toPlace;
            if (place >= 0) toPlace = place + dx;
            else
            {
                int raw = hand.HasFocus ? hand.FocusedIndex : (dx > 0 ? -1 : int.MaxValue);
                toPlace = dx > 0 ? slots.FindIndex(s => s > raw) : slots.FindLastIndex(s => s < raw);
            }
            if (toPlace < 0 || toPlace >= slots.Count)
            {
                Edge(focus);
                // The game may have moved the cursor by itself (a card played, a new hand dealt)
                // to a card that was never read: an arrow at the end reads where it is.
                FocusProbe.ReadIfUnheard();
                return;
            }
            int to = slots[toPlace];
            hand.SetFocus(to);
            if (hand.FocusedIndex != to)
            {
                Log("[hand] step to " + to + " did not take (at " + hand.FocusedIndex + "); handing the arrow to the game");
                InputBridge.Forward(dx > 0 ? InputActionId.FocusNext : InputActionId.FocusPrev);
            }
        }

        // The cursor is on a card that has left the hand: put it on the nearest one still held.
        internal static void SettleHand(S_ControllerFocus focus)
        {
            HandFocusResolver hand = focus.HandResolver;
            List<int> slots = HeldSlots(focus, out _);
            if (hand == null || slots.Count == 0) return;
            int raw = hand.HasFocus ? hand.FocusedIndex : 0;
            int best = slots[0];
            foreach (int s in slots) if (Math.Abs(s - raw) < Math.Abs(best - raw)) best = s;
            hand.SetFocus(best);
            Log("[hand] settled on " + best + " from " + raw);
        }

        // The cursor positions (the game's index) of the cards still held, left to right, and the
        // card object at every position.
        private static List<int> HeldSlots(S_ControllerFocus focus, out IntPtr[] views)
        {
            var slots = new List<int>();
            views = new IntPtr[0];
            try
            {
                HandFocusResolver resolver = focus.HandResolver;
                IControllerFocusHand hand = resolver != null ? resolver.FocusedHand : null;
                if (hand == null) hand = resolver != null ? resolver.ResolveHand() : null;
                if (hand == null) return slots;
                var held = new HashSet<IntPtr>();
                IM_Hand model = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
                var cards = model != null ? model.HeldCards : null;
                for (int i = 0; cards != null && i < cards.Count; i++) if (cards[i] != null) held.Add(cards[i].Pointer);
                int n = hand.FocusableCardCount;
                views = new IntPtr[n];
                for (int i = 0; i < n; i++)
                {
                    IV_CardInstance v = hand.GetFocusableCard(i);
                    if (v == null) continue;
                    views[i] = v.Pointer;
                    if (v.IModel != null && held.Contains(v.IModel.Pointer)) slots.Add(i);
                }
            }
            catch (Exception ex) { Log("[hand] " + ex.GetType().Name + ": " + ex.Message); }
            return slots;
        }

        // ---- Status row ----

        private static List<string> StatusItems(S_ControllerFocus focus)
        {
            var items = new List<string>();
            Il2CppBackend.M_Player p = focus.PlayerModel;
            if (p == null) { items.Add(Loc.T("status.no_character", "No character yet")); return items; }
            PlayerHealthData h = p.HealthData;
            string funds = ShopFunds(focus);
            if (funds.Length > 0) items.Add(funds);
            items.Add(Loc.T("health.of", "Health {0} of {1}", h.CurrentHealth, h.MaxHealth) + (h.Armour > 0 ? ", " + Loc.T("health.armour", "armour {0}", h.Armour) : ""));
            if (Hud.InFight(focus)) items.Add(FocusProbe.IncomingDamage());
            items.Add(Loc.T("status.energy", "Energy {0} of {1}", p.Energy, p.EnergySoftCap));
            items.Add(Loc.T("status.gunk", "Gunk {0}", p.StoredGunk));
            IM_Hand hand = p.GetActiveHand();
            string threat = Hud.Threat();
            if (threat.Length > 0) items.Add(threat);
            if (hand != null) items.Add(Loc.T("status.piles", "Draw pile {0}, discard pile {1}", hand.CardsInDeckCount, hand.CardsInDiscardCount));
            return items;
        }

        // In a shop, the food the player has to spend, as the shop's own counter shows it.
        internal static string ShopFunds(S_ControllerFocus focus)
        {
            try
            {
                Component shop = focus.Scope != null ? focus.Scope.Cast<Il2CppSystem.Object>().TryCast<BowlShopEncounterUI>() : null;
                if (shop == null) return "";
                foreach (Il2CppTMPro.TMP_Text t in shop.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(false))
                {
                    string text = t != null ? FocusProbe.Clean(t.text) : "";
                    // "Funds: 2 FOOD", once the colour tags are out.
                    if (text.StartsWith("Funds", StringComparison.OrdinalIgnoreCase))
                        return Loc.T("status.food_to_spend", "Food to spend: {0}", new string(text.Where(char.IsDigit).ToArray()));
                }
            }
            catch { }
            return "";
        }

        private static void EnterStatus(S_ControllerFocus focus)
        {
            List<string> items = StatusItems(focus);
            _inStatus = true;
            if (_statusIndex >= items.Count) _statusIndex = 0;
            FocusProbe.LeaveRow();
            FocusChanged();
            FocusProbe.Announce(Loc.T("status.row", "Status.") + " " + items[_statusIndex] + ", " + Loc.T("pos.of", "{0} of {1}", _statusIndex + 1, items.Count));
        }

        private static void StatusStep(S_ControllerFocus focus, int dx)
        {
            List<string> items = StatusItems(focus);
            int next = _statusIndex + dx;
            if (next < 0 || next >= items.Count) { Cue.Play(Cue.Edge); return; }
            _statusIndex = next;
            FocusChanged();
            FocusProbe.Announce(items[_statusIndex] + ", " + Loc.T("pos.of", "{0} of {1}", _statusIndex + 1, items.Count));
        }

        // At the end of a row: a short dull sound. The player has not moved, so nothing is re-read.
        private static void Edge(S_ControllerFocus focus)
        {
            Cue.Play(Cue.Edge);
            Log("[edge]");
        }

        // Where the focused thing sits in its row, for "2 of 3". 0 when it is not in a row.
        internal static void RowPosition(S_ControllerFocus focus, Il2CppSystem.Object current, out int index, out int count)
        {
            index = 0; count = 0;
            try
            {
                if (current == null) return;
                if (current.TryCast<IV_CardInstance>() != null)
                {
                    // Counted among the cards still held, as Left and Right step through them.
                    List<int> slots = HeldSlots(focus, out IntPtr[] views);
                    int place = slots.FindIndex(s => views[s] == current.Pointer);
                    if (place >= 0) { index = place + 1; count = slots.Count; return; }
                    HandFocusResolver hand = focus.HandResolver;
                    count = HeldCount(focus);
                    index = hand != null ? hand.FocusedIndex + 1 : 0;
                    if (index < 1 || index > count) { index = 0; count = 0; }
                    return;
                }
                List<IControllerFocusTarget> list = NearbyInOrder(focus);
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Pointer == current.Pointer) { index = i + 1; count = list.Count; return; }
            }
            catch { }
        }

        // ---- A pile of offered cards ----

        private static int _pileIndex = -1;
        private static IntPtr _pileScope;

        // The card the player is reading in a pile, when that is not the game's own focus.
        internal static Il2CppSystem.Object VirtualSubject;

        // When an arrow last stepped the pile here: the game's own focus did not move, and that
        // is not the end of a row.
        internal static int PileArrowFrame = -100;

        private static void PileStep(S_ControllerFocus focus, int dx)
        {
            Component root = focus.Scope.Cast<Il2CppSystem.Object>().TryCast<Component>();
            if (root == null) return;
            var cards = root.GetComponentsInChildren<V_GameplayCardInstance>(false);
            int count = cards.Length;
            if (count == 0) return;

            if (_pileScope != focus.Scope.Pointer || _pileIndex < 0 || _pileIndex >= count)
            {
                _pileScope = focus.Scope.Pointer;
                // Start from the card the game has on top.
                _pileIndex = 0;
                Il2CppSystem.Object top = focus.CurrentFocus;
                for (int i = 0; i < count; i++)
                    if (top != null && cards[i].Pointer == top.Pointer) { _pileIndex = i; break; }
            }

            int next = _pileIndex + dx;
            PileArrowFrame = Time.frameCount;
            if (next < 0 || next >= count) { Edge(focus); return; }
            _pileIndex = next;
            Il2CppSystem.Object card = cards[next].Cast<Il2CppSystem.Object>();
            FocusChanged();
            VirtualSubject = card;
            FocusProbe.Announce(FocusProbe.Describe(card, out _).TrimEnd('.', ' ') + ", " + Loc.T("pos.of", "{0} of {1}", next + 1, count));
        }

        // ---- Buffers ----
        // Modelled on Say the Spire: Ctrl+Left and Ctrl+Right choose a buffer, Ctrl+Up reads the
        // next line of it and Ctrl+Down the one before. Every buffer starts at its first line.
        // Moving focus returns to the Item buffer at its first line, because the thing just
        // focused is what the player will want to read about.

        private static string[] BufferNames => new[] { Loc.T("buffer.item", "Item"), Loc.T("buffer.player", "Player"), Loc.T("buffer.surroundings", "Surroundings"), Loc.T("buffer.messages", "Messages"), Loc.T("buffer.help", "Help") };

        private static string[] HelpLines => new[]
        {
            Loc.T("help.ctrl_up", "Control Up reads on, Control Down goes back"),
            Loc.T("help.rows", "Up and Down arrows move between rows: Status at the top, then Nearby or Enemies, then Your hand"),
            Loc.T("help.along_row", "Left and Right arrows move along a row. A low sound means the end of the row"),
            Loc.T("help.enter", "Enter chooses. On a card in your hand it picks the card, then Enter on a target plays it"),
            Loc.T("help.no_target", "A card with no target is played by pressing Enter twice"),
            Loc.T("help.backspace", "Backspace cancels or goes back. Escape opens the pause menu"),
            Loc.T("help.walk", "Hold W to walk forward and S to walk back. A second voice speaks while you walk"),
            Loc.T("help.arrivals", "While you walk, a sound marks each thing that comes into reach. They are listed when you stop at a camp, and after a fight"),
            Loc.T("help.death", "When you die, Space moves through the words on screen. A sealed pack of cards is opened with Enter"),
            Loc.T("help.space", "Space ends your turn. At a camp, Space rests for the night and refills your energy"),
            Loc.T("help.food", "A food card can be aimed at you or at something hungry: after the first Enter, Left and Right choose, and you are said as You"),
            Loc.T("help.shop", "In a shop, Enter on a card, then pay one food at a time: Enter on a food card, then Enter on the bowl"),
            Loc.T("help.soup", "At a soup pot, put food in with Enter on a food card, then Enter on the pot. The soup value rises and opens dearer soups. Up goes to the open soups"),
            Loc.T("help.map", "M opens and closes the map. With the map open, Control Up reads each area"),
            Loc.T("help.deck", "Tab opens your draw pile; Tab again shows the discard pile, and again closes it. Up from the cards reaches buttons for the full deck and the combat, explore and foresight decks"),
            Loc.T("help.inspect", "I inspects. O sorts your hand"),
            Loc.T("help.buffers", "Control Left and Right switch buffer: Item, Player, Surroundings, Messages, Help"),
            Loc.T("help.item_buffer", "The Item buffer describes what you are on, ending with what it looks like"),
            Loc.T("help.player_buffer", "The Player buffer has your health, energy, piles, statuses and foresights"),
            Loc.T("help.messages_buffer", "The Messages buffer repeats what was said, newest first"),
            Loc.T("help.status_enter", "On the Status row, Enter reads the game's own explanation of a status"),
            Loc.T("help.incoming", "In a fight the Status row has the total damage the enemies intend this turn"),
            Loc.T("help.card_label", "A card's name is followed by the label under its title when it has one: uses left, battery, evolve"),
            Loc.T("help.voice_rate", "Control Page Up and Control Page Down make the walking voice faster or slower"),
            Loc.T("help.bug_mark", "Control B marks the moment for a bug report"),
            Loc.T("help.update", "When the title screen says a new version is out, Control O opens its download page and Control N, pressed twice, installs it"),
            Loc.T("help.keys", "Every key can be changed in the preferences file; the README says how"),
        };
        private static int _buffer;
        private static int _line;

        internal static void FocusChanged()
        {
            _buffer = 0;
            _line = 0;
        }

        private static void Buffers(S_ControllerFocus focus, int dx, int dy)
        {
            if (dx != 0)
            {
                int next = Mathf.Clamp(_buffer + dx, 0, BufferNames.Length - 1);
                // At the first or last buffer: the edge sound, and nothing is read again.
                if (next == _buffer) { Edge(focus); return; }
                _buffer = next; _line = 0;
                List<string> opened = Lines(focus);
                FocusProbe.Speak(BufferNames[_buffer] + ". " + (opened.Count > 0 ? opened[Mathf.Clamp(_line, 0, opened.Count - 1)] : Loc.T("buffer.nothing", "Nothing here.")));
                return;
            }

            List<string> lines = Lines(focus);
            if (lines.Count == 0) { FocusProbe.Speak(Loc.T("buffer.nothing", "Nothing here.")); return; }
            // Ctrl+Up is "next line"; dy is -1 for Up.
            int wanted = Mathf.Min(_line, lines.Count - 1) - dy;
            // Past either end: the edge sound only, so the last line does not sound like a wrap.
            if (wanted < 0 || wanted >= lines.Count) { Edge(focus); return; }
            _line = wanted;
            FocusProbe.Speak(lines[_line]);
        }

        private static List<string> Lines(S_ControllerFocus focus)
        {
            switch (_buffer)
            {
                case 1:
                {
                    List<string> lines = StatusItems(focus);
                    try
                    {
                        IM_Hand hand = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
                        if (hand != null && hand.HeldCards != null)
                        {
                            var names = new List<string>();
                            for (int i = 0; i < hand.HeldCards.Count; i++) names.Add(hand.HeldCards[i].CardName);
                            lines.Add(names.Count == 0 ? Loc.T("player.hand_empty", "Hand empty") : Loc.T("player.hand", "Hand: {0}", string.Join(", ", names)));
                        }
                        // Statuses on the player. The model answers "do you have this one" but keeps
                        // no list, so every status the game defines is asked about.
                        if (_allStatuses == null) _allStatuses = Resources.FindObjectsOfTypeAll<StatusEffect>();
                        Il2CppBackend.M_Player me = focus.PlayerModel;
                        foreach (StatusEffect effect in _allStatuses)
                        {
                            if (effect == null || me == null || !me.HasStatusEffect(effect)) continue;
                            int amount = me.GetStatusEffectAmount(effect);
                            string status = FocusProbe.Humanize(effect.name) + (amount != 0 ? " " + amount : "");
                            lines.Add(Loc.T("player.status", "Status: {0}", status + FocusProbe.StatusTip(status)));
                        }
                        V_ForesightHand foresight = UnityEngine.Object.FindObjectOfType<V_ForesightHand>();
                        M_ForesightHand fh = foresight != null && foresight.Model != null ? foresight.Model.TryCast<M_ForesightHand>() : null;
                        var held = fh != null ? fh.Foresights : null;
                        for (int i = 0; held != null && i < held.Count; i++)
                        {
                            M_ForesightCardInstance f = held[i];
                            if (f == null) continue;
                            IM_CardInstance im = f.Cast<Il2CppSystem.Object>().TryCast<IM_CardInstance>();
                            string name = im != null ? im.CardName : Loc.T("player.foresight_unnamed", "Foresight");
                            string text = im != null && im.IData != null ? im.IData.SingleDescription : "";
                            lines.Add(Loc.T("player.foresight", "Foresight: {0}", FocusProbe.Clean(name + ". " + text)));
                        }
                    }
                    catch { }
                    return lines;
                }
                case 2:
                {
                    var lines = new List<string>();
                    foreach (IControllerFocusTarget t in NearbyInOrder(focus)) lines.Add(FocusProbe.Describe(t, out _));
                    if (lines.Count == 0) lines.Add(Loc.T("surroundings.empty", "Nothing in reach"));
                    return lines;
                }
                case 3:
                    return FocusProbe.History();
                case 4:
                    return new List<string>(HelpLines);
                default:
                {
                    if (_inStatus)
                    {
                        // On the status row the focused "item" is the one status the player is on.
                        List<string> items = StatusItems(focus);
                        string item = items[Mathf.Clamp(_statusIndex, 0, items.Count - 1)];
                        var detail = new List<string> { item };
                        if (item.StartsWith("Warmth")) detail.AddRange(Hud.Tip("End Turn Button"));
                        if (item.StartsWith("Health")) detail.AddRange(Hud.Tip("Health"));
                        else if (item.StartsWith("Energy")) detail.AddRange(Hud.Tip("Energy"));
                        return detail;
                    }
                    // With the map held up, the thing being looked at is the map.
                    if (focus.Scope != null && focus.Scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name == "S_MapReminder") return MapReader.Lines();
                    List<string> stats = Rewards.StatLines(focus.Scope);
                    if (stats != null) return stats;
                    Il2CppSystem.Object subject = VirtualSubject ?? focus.CurrentFocus;
                    if (subject == null) return new List<string> { Loc.T("item.nothing_focused", "Nothing focused") };
                    return FocusProbe.DetailLines(subject);
                }
            }
        }
    }
}
