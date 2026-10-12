using System;
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
        private static List<IControllerFocusTarget> NearbyInOrder(S_ControllerFocus focus)
        {
            var list = new List<IControllerFocusTarget>();
            WorldFocusCandidates world = focus.WorldFocusCandidates;
            Camera cam = focus.FocusCamera;
            var keyed = new List<KeyValuePair<float, IControllerFocusTarget>>();
            for (int i = 0; world != null && i < world.Count; i++)
            {
                IControllerFocusTarget t = world.At(i);
                if (t == null) continue;
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
            items.Add("Health " + h.CurrentHealth + " of " + h.MaxHealth + (h.Armour > 0 ? ", armour " + h.Armour : ""));
            items.Add("Energy " + p.Energy + " of " + p.EnergySoftCap);
            items.Add("Gunk " + p.StoredGunk);
            IM_Hand hand = p.GetActiveHand();
            string threat = Hud.Threat();
            if (threat.Length > 0) items.Add(threat);
            if (hand != null) items.Add("Draw pile " + hand.CardsInDeckCount + ", discard pile " + hand.CardsInDiscardCount);
            return items;
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
            if (next < 0 || next >= count) { Cue.Play(Cue.Edge); return; }
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

        private static readonly string[] BufferNames = { "Item", "Player", "Surroundings", "Messages" };
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
                if (next != _buffer) { _buffer = next; _line = 0; }
                List<string> opened = Lines(focus);
                FocusProbe.Speak(BufferNames[_buffer] + ". " + (opened.Count > 0 ? opened[Mathf.Clamp(_line, 0, opened.Count - 1)] : "Nothing here."));
                return;
            }

            List<string> lines = Lines(focus);
            if (lines.Count == 0) { FocusProbe.Speak("Nothing here."); return; }
            // Ctrl+Up is "next line"; dy is -1 for Up.
            _line = Mathf.Clamp(_line - dy, 0, lines.Count - 1);
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
                    Il2CppSystem.Object subject = VirtualSubject ?? focus.CurrentFocus;
                    if (subject == null) return new List<string> { "Nothing focused" };
                    return FocusProbe.DetailLines(subject);
                }
            }
        }
    }
}
