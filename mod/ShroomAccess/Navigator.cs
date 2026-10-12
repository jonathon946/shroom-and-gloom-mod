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

        private static int _buffer;          // 0 = focused item, 1 = player
        private static int _line;
        private static IntPtr _bufferSubject;
        private static readonly string[] BufferNames = { "Item", "Player" };

        internal static bool InStatusRow => _inStatus;

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                FocusMode mode = focus.Mode;
                bool rows = focus.Scope == null && (mode == FocusMode.Free || IsWorld(mode) || IsHand(mode));
                InputBridge.NavigatorOwnsArrows = rows;
                if (!rows) _inStatus = false;

                if (InputBridge.CtrlHeld)
                {
                    int dr = (InputBridge.Key(InputActionId.PagePrev) ? 1 : 0) - (InputBridge.Key(InputActionId.PageNext) ? 1 : 0);
                    if (dr != 0) Settings.NudgeDirectRate(dr);
                }

                int dx = (InputBridge.Key(InputActionId.FocusNext) ? 1 : 0) - (InputBridge.Key(InputActionId.FocusPrev) ? 1 : 0);
                int dy = (InputBridge.Key(InputActionId.FocusDown) ? 1 : 0) - (InputBridge.Key(InputActionId.FocusUp) ? 1 : 0);
                if (dx == 0 && dy == 0) return;

                if (InputBridge.CtrlHeld) { Buffers(focus, dx, dy); return; }
                if (!rows) return;

                int world = focus.WorldFocusCandidates != null ? focus.WorldFocusCandidates.Count : 0;
                int hand = HeldCount(focus);

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
                    else Edge(focus);
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
            FocusProbe.Announce("Status, " + items.Count + (items.Count == 1 ? " item. " : " items. ") + items[_statusIndex]);
        }

        private static void StatusStep(S_ControllerFocus focus, int dx)
        {
            List<string> items = StatusItems(focus);
            int next = _statusIndex + dx;
            if (next < 0 || next >= items.Count) { FocusProbe.Announce(items[Mathf.Clamp(_statusIndex, 0, items.Count - 1)]); return; }
            _statusIndex = next;
            FocusProbe.Announce(items[_statusIndex]);
        }

        // At the end of a row: say where the player still is, never nothing.
        private static void Edge(S_ControllerFocus focus)
        {
            if (_inStatus) { List<string> items = StatusItems(focus); FocusProbe.Announce(items[Mathf.Clamp(_statusIndex, 0, items.Count - 1)]); }
            else FocusProbe.Reannounce(rowName: false);
        }

        // ---- Buffers ----

        private static void Buffers(S_ControllerFocus focus, int dx, int dy)
        {
            Il2CppSystem.Object subject = focus.CurrentFocus;
            IntPtr ptr = subject != null ? subject.Pointer : IntPtr.Zero;
            bool fresh = ptr != _bufferSubject;
            _bufferSubject = ptr;

            if (dx != 0)
            {
                _buffer = Mathf.Clamp(_buffer + dx, 0, BufferNames.Length - 1);
                _line = 0;
                List<string> opened = Lines(focus, subject);
                FocusProbe.Announce(BufferNames[_buffer] + ". " + (opened.Count > 0 ? opened[0] : "Nothing here."));
                return;
            }

            List<string> lines = Lines(focus, subject);
            if (lines.Count == 0) { FocusProbe.Announce("Nothing here."); return; }
            if (fresh && _buffer == 0) _line = dy > 0 ? 0 : lines.Count - 1;
            else _line = Mathf.Clamp(_line + dy, 0, lines.Count - 1);
            FocusProbe.Announce(lines[_line]);
        }

        private static List<string> Lines(S_ControllerFocus focus, Il2CppSystem.Object subject)
        {
            if (_buffer == 1)
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
                }
                catch { }
                return lines;
            }
            if (_inStatus)
            {
                // On the status row the focused "item" is the one status the player is on.
                List<string> items = StatusItems(focus);
                string item = items[Mathf.Clamp(_statusIndex, 0, items.Count - 1)];
                var detail = new List<string> { item };
                // The game's own hover text for that display, where it has one.
                if (item.StartsWith("Health")) detail.AddRange(Hud.Tip("Health"));
                else if (item.StartsWith("Energy")) detail.AddRange(Hud.Tip("Energy"));
                return detail;
            }
            return FocusProbe.DetailLines(subject);
        }
    }
}
