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
                    FocusProbe.Speak(detail.Count > 1 ? string.Join(". ", detail.GetRange(1, detail.Count - 1)) : "The game says nothing more about this.");
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
                    else FocusProbe.Speak("No cards in hand right now.");
                }
                else if (IsHand(mode))
                {
                    if (dx != 0) InputBridge.Forward(dx > 0 ? InputActionId.FocusNext : InputActionId.FocusPrev);
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

        // ---- Status row ----

        private static List<string> StatusItems(S_ControllerFocus focus)
        {
            var items = new List<string>();
            Il2CppBackend.M_Player p = focus.PlayerModel;
            if (p == null) { items.Add("No character yet"); return items; }
            PlayerHealthData h = p.HealthData;
            string funds = ShopFunds(focus);
            if (funds.Length > 0) items.Add(funds);
            items.Add("Health " + h.CurrentHealth + " of " + h.MaxHealth + (h.Armour > 0 ? ", armour " + h.Armour : ""));
            items.Add("Energy " + p.Energy + " of " + p.EnergySoftCap);
            items.Add("Gunk " + p.StoredGunk);
            IM_Hand hand = p.GetActiveHand();
            string threat = Hud.Threat();
            if (threat.Length > 0) items.Add(threat);
            if (hand != null) items.Add("Draw pile " + hand.CardsInDeckCount + ", discard pile " + hand.CardsInDiscardCount);
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
                        return "Food to spend: " + new string(text.Where(char.IsDigit).ToArray());
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
            FocusProbe.Announce("Status. " + items[_statusIndex] + ", " + (_statusIndex + 1) + " of " + items.Count);
        }

        private static void StatusStep(S_ControllerFocus focus, int dx)
        {
            List<string> items = StatusItems(focus);
            int next = _statusIndex + dx;
            if (next < 0 || next >= items.Count) { Cue.Play(Cue.Edge); return; }
            _statusIndex = next;
            FocusChanged();
            FocusProbe.Announce(items[_statusIndex] + ", " + (_statusIndex + 1) + " of " + items.Count);
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
        internal static float PileArrowAt = -100f;

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
            PileArrowAt = Time.realtimeSinceStartup;
            if (next < 0 || next >= count) { Edge(focus); return; }
            _pileIndex = next;
            Il2CppSystem.Object card = cards[next].Cast<Il2CppSystem.Object>();
            FocusChanged();
            VirtualSubject = card;
            FocusProbe.Announce(FocusProbe.Describe(card, out _).TrimEnd('.', ' ') + ", " + (next + 1) + " of " + count);
        }

        // ---- Buffers ----
        // Modelled on Say the Spire: Ctrl+Left and Ctrl+Right choose a buffer, Ctrl+Up reads the
        // next line of it and Ctrl+Down the one before. Every buffer starts at its first line.
        // Moving focus returns to the Item buffer at its first line, because the thing just
        // focused is what the player will want to read about.

        private static readonly string[] BufferNames = { "Item", "Player", "Surroundings", "Messages", "Help" };

        private static readonly string[] HelpLines =
        {
            "Control Up reads on, Control Down goes back",
            "Up and Down arrows move between rows: Status at the top, then Nearby or Enemies, then Your hand",
            "Left and Right arrows move along a row. A low sound means the end of the row",
            "Enter chooses. On a card in your hand it picks the card, then Enter on a target plays it",
            "A card with no target is played by pressing Enter twice",
            "Backspace cancels or goes back. Escape opens the pause menu",
            "Hold W to walk forward and S to walk back. A second voice speaks while you walk",
            "While you walk, a sound marks each thing that comes into reach. They are listed when you stop at a camp, and after a fight",
            "When you die, Space moves through the words on screen. A sealed pack of cards is opened with Enter",
            "Space ends your turn. At a camp, Space rests for the night and refills your energy",
            "A food card can be aimed at you or at something hungry: after the first Enter, Left and Right choose, and you are said as You",
            "In a shop, Enter on a card, then pay one food at a time: Enter on a food card, then Enter on the bowl",
            "At a soup pot, put food in with Enter on a food card, then Enter on the pot. The soup value rises and opens dearer soups. Up goes to the open soups",
            "M opens and closes the map. With the map open, Control Up reads each area",
            "Tab opens your deck. I inspects. O sorts your hand",
            "Control Left and Right switch buffer: Item, Player, Surroundings, Messages, Help",
            "The Item buffer describes what you are on, ending with what it looks like",
            "The Player buffer has your health, energy, piles, statuses and foresights",
            "The Messages buffer repeats what was said, newest first",
            "On the Status row, Enter reads the game's own explanation of a status",
            "Control Page Up and Control Page Down make the walking voice faster or slower",
            "Control B marks the moment for a bug report",
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
                FocusProbe.Speak(BufferNames[_buffer] + ". " + (opened.Count > 0 ? opened[Mathf.Clamp(_line, 0, opened.Count - 1)] : "Nothing here."));
                return;
            }

            List<string> lines = Lines(focus);
            if (lines.Count == 0) { FocusProbe.Speak("Nothing here."); return; }
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
                            lines.Add(names.Count == 0 ? "Hand empty" : "Hand: " + string.Join(", ", names));
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
                            lines.Add("Status: " + status + FocusProbe.StatusTip(status));
                        }
                        V_ForesightHand foresight = UnityEngine.Object.FindObjectOfType<V_ForesightHand>();
                        M_ForesightHand fh = foresight != null && foresight.Model != null ? foresight.Model.TryCast<M_ForesightHand>() : null;
                        var held = fh != null ? fh.Foresights : null;
                        for (int i = 0; held != null && i < held.Count; i++)
                        {
                            M_ForesightCardInstance f = held[i];
                            if (f == null) continue;
                            IM_CardInstance im = f.Cast<Il2CppSystem.Object>().TryCast<IM_CardInstance>();
                            string name = im != null ? im.CardName : "Foresight";
                            string text = im != null && im.IData != null ? im.IData.SingleDescription : "";
                            lines.Add("Foresight: " + FocusProbe.Clean(name + ". " + text));
                        }
                    }
                    catch { }
                    return lines;
                }
                case 2:
                {
                    var lines = new List<string>();
                    foreach (IControllerFocusTarget t in NearbyInOrder(focus)) lines.Add(FocusProbe.Describe(t, out _));
                    if (lines.Count == 0) lines.Add("Nothing in reach");
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
                    if (subject == null) return new List<string> { "Nothing focused" };
                    return FocusProbe.DetailLines(subject);
                }
            }
        }
    }
}
