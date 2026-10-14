using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Il2Cpp;
using Il2CppControllerNav;
using Il2CppTMPro;
using ShroomAccess.Speech;
using UnityEngine;

namespace ShroomAccess
{
    // Spike 2: watch the game's own controller focus and report what it lands on.
    // Logs everything; speaks a rough description so coverage can be judged by ear.
    internal static class FocusProbe
    {
        internal static Action<string> Log = _ => { };

        private static IntPtr _lastFocus;
        private static IntPtr _lastLoggedFocus;
        private static float _flickerUntil;

        // Focus lines in the next moment are passing through, whatever key was pressed; the one it
        // settles on is read afterwards.
        internal static void Flicker(float seconds) => _flickerUntil = Mathf.Max(_flickerUntil, Time.realtimeSinceStartup + seconds);

        // Something the player did (a play, a payment) or the game did (a deal) moves the focus.
        // Where it lands is noted but not read: the player knows where they are and moves on
        // with an arrow when ready. Any arrow pressed after this starts is read as usual.
        private static float _silentUntil, _silentFrom;
        internal static void Silent(float seconds)
        {
            float now = Time.realtimeSinceStartup;
            if (now >= _silentUntil) _silentFrom = now;
            _silentUntil = Mathf.Max(_silentUntil, now + seconds);
        }
        private static bool SilentNow => Time.realtimeSinceStartup < _silentUntil && InputBridge.LastArrowAt < _silentFrom;
        private static float _playedAt = -100f;
        private static string _playedName = "";

        // The focus the player last heard read, and its card's name. Enter in the hand waits for a
        // card the player has heard (InputBridge.ConfirmSwallowed).
        private static IntPtr _heardFocus;
        private static string _heardName = "";
        private static float _forceUntil;
        private static bool ForceRead => Time.realtimeSinceStartup < _forceUntil;

        private static string CardNameOf(Il2CppSystem.Object current)
        {
            try
            {
                IV_CardInstance view = current != null ? current.TryCast<IV_CardInstance>() : null;
                return view != null && view.IModel != null ? view.IModel.CardName ?? "" : "";
            }
            catch { return ""; }
        }

        // True when the hand's cursor is on a card the player has not heard (another card of the
        // same name counts as heard: Toasty after Toasty).
        internal static bool HandCardUnheard()
        {
            try
            {
                if (!S_ControllerFocus.TryGetFocus(out S_ControllerFocus focus) || focus == null) return false;
                if (focus.Mode.ToString() != "Hand" || focus.Scope != null || Navigator.InStatusRow) return false;
                Il2CppSystem.Object current = focus.CurrentFocus;
                if (current == null) return false;
                // A card already played, still under the cursor on its way out, is never what Enter is for.
                if (current.TryCast<IV_CardInstance>() != null && !InHand(focus, current)) return true;
                if (current.Pointer == _heardFocus) return false;
                string name = CardNameOf(current);
                return name.Length > 0 && name != _heardName;
            }
            catch { return false; }
        }

        // Read the focused thing now, past the rules that keep a moving focus quiet, if the player
        // has not heard it.
        internal static void ReadIfUnheard()
        {
            if (!HandCardUnheard()) return;
            _lastFocus = IntPtr.Zero;
            _forceUntil = Time.realtimeSinceStartup + 1.5f;
        }

        // A card has just been played: what follows from it in the next moments is not news.
        internal static void NotePlay(string name)
        {
            _playedAt = Time.realtimeSinceStartup;
            _playedName = name ?? "";
            try
            {
                if (S_ControllerFocus.TryGetFocus(out S_ControllerFocus focus) && focus != null && focus.CurrentFocus != null && focus.Mode == FocusMode.Target)
                    _targetName = FirstSentence(Describe(focus.CurrentFocus, out _));
                else _targetName = "";
            }
            catch { _targetName = ""; }
        }
        private static float _arrowAt;
        private static IntPtr _arrowFrom;
        private static string _arrowValue = "";
        private static string _lastState;
        private static bool _bindingsDumped;
        private static IntPtr _lastSelected;
        private static string _lastZone = "";
        internal static float SuppressFocusUntil;
        private static IntPtr _suppressedLogged;
        private static string _lastValue = "";
        private static float _protectUntil;
        // One line the player asked for has already waited behind a protected announcement.
        private static bool _protectedLineQueued;
        private static readonly HashSet<IntPtr> _knownNearby = new HashSet<IntPtr>();
        private static readonly Dictionary<IntPtr, bool> _doorOpen = new Dictionary<IntPtr, bool>();
        private static readonly Dictionary<IntPtr, V_SpellReceiverDoor> _doors = new Dictionary<IntPtr, V_SpellReceiverDoor>();

        private static void WatchDoors()
        {
            var gone = new List<IntPtr>();
            foreach (var pair in _doors)
            {
                V_SpellReceiverDoor door = pair.Value;
                bool alive = false, open = false;
                try { alive = door != null && door.gameObject != null; if (alive) open = door.Door.IsUnlockedAndOpen(); } catch { alive = false; }
                if (!alive) { gone.Add(pair.Key); continue; }
                if (open && !_doorOpen[pair.Key])
                {
                    _doorOpen[pair.Key] = true;
                    Announce(Loc.T("door.opened", "The door is open. You can walk on."), interrupt: false, holdWhileWalking: true);
                }
            }
            foreach (IntPtr key in gone) { _doors.Remove(key); _doorOpen.Remove(key); }
        }
        private static readonly InputActionId[] ActionIds =
        {
            InputActionId.Confirm, InputActionId.Cancel, InputActionId.Pause, InputActionId.EndTurn,
            InputActionId.FocusPrev, InputActionId.FocusNext, InputActionId.FocusUp, InputActionId.FocusDown,
            InputActionId.PagePrev, InputActionId.PageNext, InputActionId.Interact, InputActionId.Inspect,
            InputActionId.RecentreView, InputActionId.ToggleDeckView, InputActionId.ToggleMap, InputActionId.SortHand,
        };

        internal static void Tick()
        {
            try
            {
                if (!S_ControllerFocus.TryGetFocus(out S_ControllerFocus focus) || focus == null) return;

                S_InputManager input = focus.Input;
                if (ArmedAt > 0) InputBridge.Tick(input);
                if (!_bindingsDumped && input != null && ArmedAt > 0 && Time.realtimeSinceStartup > ArmedAt)
                {
                    _bindingsDumped = true;
                    if (DevBridge.Enabled) DumpAsset(input);
                }

                string state = "mode=" + focus.Mode
                    + " pad=" + (input != null && input.PadDrives)
                    + " device=" + (input != null ? input.CurrentDeviceType.ToString() : "?")
                    + " scope=" + TypeName(focus.Scope)
                    + " blockedBy=" + (focus.BlockedBy ?? "")
                    + " combat=" + focus.InCombat;
                if (state != _lastState)
                {
                    _lastState = state;
                    Log("[state] " + state);
                }
                FocusMode mode = focus.Mode;
                Speaker.WalkHeld = InputBridge.MoveHeld;
                Perf.Lap("Focus.Start");
                Messages.Tick(focus);
                Perf.Lap("Messages");
                Navigator.Tick(focus);
                Perf.Lap("Navigator");
                Review.Tick(focus);
                Perf.Lap("Review");
                Walk.Tick(focus);
                Perf.Lap("Walk");
                CardPlay.Tick(focus);
                Perf.Lap("CardPlay");
                AnnounceArrivals(focus);
                Perf.Lap("Arrivals");
                WatchDoors();
                Perf.Lap("Doors");
                AnnounceHandChanges(focus);
                Perf.Lap("HandChanges");
                Hud.AnnounceChanges(focus);
                Perf.Lap("Hud");
                Rewards.Tick(focus);
                Perf.Lap("Rewards");
                Soup.Tick(focus);
                Perf.Lap("Soup");
                DeckView.Tick(focus);
                Perf.Lap("DeckView");
                Summaries(focus);
                Perf.Lap("Summaries");
                AltText.Tick();
                Perf.Lap("AltText");
                FlushHeld();
                Perf.Lap("FlushHeld");

                if (input != null && _bindingsDumped && DevBridge.Enabled)
                {
                    foreach (InputActionId id in ActionIds)
                    {
                        bool down = false;
                        try { down = input.GetActionDown(id); } catch { }
                        if (down) Log("[action] " + id + " down");
                    }
                }

                var es = UnityEngine.EventSystems.EventSystem.current;
                GameObject sel = es != null ? es.currentSelectedGameObject : null;
                IntPtr selPtr = sel != null ? sel.Pointer : IntPtr.Zero;
                if (selPtr != _lastSelected)
                {
                    _lastSelected = selPtr;
                    Log("[selected] " + (sel != null ? PathOf(sel.transform) : "none"));
                }

                Il2CppSystem.Object current = focus.CurrentFocus;
                IntPtr ptr = current != null ? current.Pointer : IntPtr.Zero;
                if (!InputBridge.NavigatorOwnsArrows && !InputBridge.CtrlHeld && ptr != IntPtr.Zero && !Navigator.InStatusRow
                    && Time.frameCount != Navigator.PileArrowFrame
                    && (InputBridge.Key(InputActionId.FocusPrev) || InputBridge.Key(InputActionId.FocusNext) || InputBridge.Key(InputActionId.FocusUp) || InputBridge.Key(InputActionId.FocusDown)))
                {
                    _arrowAt = Time.realtimeSinceStartup;
                    _arrowFrom = ptr;
                    _arrowValue = LiveValue(current);
                }
                if (InputBridge.Key(InputActionId.Confirm) && InputBridge.ConfirmSwallowed())
                {
                    Log("[guard] Enter on a card not yet heard: reading it first");
                    if (current != null && current.TryCast<IV_CardInstance>() != null && !InHand(focus, current)) Navigator.SettleHand(focus);
                    _lastFocus = IntPtr.Zero;
                    _forceUntil = Time.realtimeSinceStartup + 1.5f;
                }
                // Applying a card change makes the game's focus skip across three things in a second.
                if (InputBridge.Key(InputActionId.Confirm) && InputBridge.ConfirmHeld(InputActionId.Confirm))
                    Log("[hold] Enter held back: the card change is not ready");
                else if (mode == FocusMode.Target && TypeName(focus.Scope).Contains("ModifyCard") && InputBridge.Key(InputActionId.Confirm))
                {
                    _flickerUntil = Time.realtimeSinceStartup + 1.6f;
                    // Released as soon as Take is focused (below).
                    InputBridge.HoldConfirm(2.5f);
                }
                else if (mode == FocusMode.Hand && TypeName(focus.Scope).Contains("ModifyCard") && InputBridge.Key(InputActionId.Confirm))
                {
                    IV_CardInstance picked = current != null ? current.TryCast<IV_CardInstance>() : null;
                    ModifyPicked = picked != null && picked.IModel != null ? picked.IModel.CardName : null;
                }
                if (_arrowAt > 0f && Time.realtimeSinceStartup - _arrowAt > 0.12f)
                {
                    if (ptr == _arrowFrom && LiveValue(current) == _arrowValue && Time.realtimeSinceStartup >= SuppressFocusUntil)
                    {
                        Log("[cue] edge: the arrow moved nothing");
                        Cue.Play(Cue.Edge);
                    }
                    _arrowAt = 0f;
                }
                if (ptr == _lastFocus)
                {
                    // Same control, new value: the player has just adjusted it.
                    string value = LiveValue(current);
                    if (value != _lastValue)
                    {
                        _lastValue = value;
                        if (value.Length > 0) Speak(value);
                    }
                    return;
                }
                _lastValue = LiveValue(current);
                _lastFocus = ptr;
                if (current == null)
                {
                    _lastLoggedFocus = IntPtr.Zero;
                    Log("[focus] none");
                    // Choosing a target with nothing focused means the card is aimed at the player.
                    if (mode == FocusMode.Target && focus.Scope == null && InputBridge.ArrowJustPressed) Speak(Loc.T("target.you", "You."));
                    // Still among the targets: coming back to one does not need "Choose a target" again.
                    if (mode != FocusMode.Target) _lastZone = "";
                    return;
                }

                string line = Describe(current, out string detail);
                if (ptr != _lastLoggedFocus) { _lastLoggedFocus = ptr; Log("[focus] " + TypeName(current) + " | " + detail); }
                Navigator.FocusChanged();
                Navigator.VirtualSubject = null;
                // Looking the thing up notes it if it has no appearance description yet.
                try { DetailLines(current); } catch { }
                if (Navigator.InStatusRow) return;
                // A card just played keeps the focus for a moment as it leaves the hand; it is no longer one
                // the player can choose, and reading it would name the wrong card before the next Enter.
                if (mode.ToString() == "Hand" && focus.Scope == null && current.TryCast<IV_CardInstance>() != null && !InHand(focus, current))
                {
                    if (_suppressedLogged != ptr) { _suppressedLogged = ptr; Log("[leaving] " + line); }
                    return;
                }

                // Name the row the player has just moved into, so they know what they are among:
                // their own cards, things in the world, valid targets for a cast, or an offer.
                string zoneBefore = _lastZone;
                string zone = ZoneOf(focus, current, mode);
                if (zone == "offer") _offerSeenAt = Time.realtimeSinceStartup;
                string spoken = line;
                if (zone != _lastZone)
                {
                    _lastZone = zone;
                    if (zone == "food") spoken = Loc.T("row.food", "Your food, {0}.", Cards(HeldCount(focus))) + " " + line;
                    else if (zone == "shop") spoken = Loc.T("row.shop", "For sale.") + " " + line;
                    else if (zone == "hand" && TypeName(focus.Scope).Contains("ModifyCard")) spoken = Loc.T("row.modify", "Choose a card to change, {0}.", Cards(HeldCount(focus))) + " " + line;
                    else if (zone == "hand" && TypeName(focus.Scope).Contains("AdditiveUnlock")) spoken = Loc.T("row.pot_food", "Your food for the pot, {0}.", Cards(HeldCount(focus))) + " " + line;
                    else if (zone == "hand") spoken = Loc.T("row.hand", "Your hand, {0}.", Cards(HeldCount(focus))) + " " + line;
                    else if (zone == "nearby" && focus.InCombat) spoken = Loc.T("row.enemies", "Enemies, {0}.", Navigator.NearbyInOrder(focus).Count) + " " + line;
                    else if (zone == "nearby") spoken = Loc.T("row.nearby", "Nearby, {0}.", Things(focus.WorldFocusCandidates != null ? focus.WorldFocusCandidates.Count : 0)) + " " + line;
                    else if (zone == "targets" && current.TryCast<ModifyCardEncounterUI>() != null) spoken = line;
                    else if (zone == "targets" && current.TryCast<IM_CardInstance>() != null) spoken = Loc.T("row.target_cards", "Choose one of your cards.") + " " + line;
                    else if (zone == "targets") spoken = Loc.T("row.targets", "Choose a target.") + " " + line;
                    else if (zone == "offer" && current.TryCast<IV_CardInstance>() != null) spoken = Loc.T("row.offered", "Offered: {0} Enter takes it.", line);
                }
                if (TypeName(focus.Scope).Contains("ModifyCard"))
                {
                    Component button = current.TryCast<Component>();
                    if (button != null && button.gameObject.name == "Take It Button")
                    {
                        InputBridge.HoldConfirmUntil = 0f;
                        string result = Messages.ModifyResult(focus, button);
                        if (!string.IsNullOrWhiteSpace(result)) 
                        {
                            GameObject decline = GameObject.Find("No Thanks Button");
                            spoken = Loc.T("modify.result", "The card is now: {0}. Enter takes it.", result.TrimEnd('.', ' '))
                                + (decline != null && decline.activeInHierarchy ? " " + Loc.T("modify.no_thanks", "Left is No thanks.") : "");
                        }
                    }
                }
                string deckPlace = DeckView.Suffix(focus, current);
                if (deckPlace.Length > 0) spoken = spoken.TrimEnd('.', ' ') + deckPlace;
                string soup = Soup.Suffix(focus, current);
                if (soup.Length > 0) spoken = spoken.TrimEnd('.', ' ') + soup + ".";
                if (zone == "shop")
                {
                    string place = ShopPlace(focus, current);
                    if (place.Length > 0) spoken = spoken.TrimEnd('.', ' ') + ", " + place;
                }
                if (zone == "hand" || zone == "nearby")
                {
                    Navigator.RowPosition(focus, current, out int index, out int count);
                    if (count > 1) spoken = spoken.TrimEnd('.', ' ') + ", " + Loc.T("pos.of", "{0} of {1}", index, count);
                }
                if (zone == "targets" && current.TryCast<IM_CardInstance>() != null)
                {
                    CardTargetPosition(focus, out int index, out int count);
                    if (count > 1) spoken = spoken.TrimEnd('.', ' ') + ", " + Loc.T("pos.of", "{0} of {1}", index, count);
                }
                // Two enemies of one name read the same; where each stands tells them apart.
                else if (zone == "targets" && current.TryCast<V_Enemy>() != null)
                {
                    Navigator.RowPosition(focus, current, out int index, out int count);
                    if (count > 1) spoken = spoken.TrimEnd('.', ' ') + ", " + Loc.T("pos.of", "{0} of {1}", index, count);
                }
                // The last card has gone, or the enemies are acting and the next hand is being dealt:
                // the game moves the focus around by itself. The empty hand is said once by the HUD,
                // the new hand with "Drew N cards".
                if (!ForceRead && !InputBridge.ArrowJustPressed && focus.Scope == null && (zone == "hand" || (zone == "nearby" && Hud.InFight(focus)))
                    && (HeldCount(focus) == 0 || Hud.EnemyTurnNow))
                {
                    if (_suppressedLogged != ptr) { _suppressedLogged = ptr; Log("[quiet] " + spoken); }
                    return;
                }
                if (SilentNow && !ForceRead)
                {
                    if (_suppressedLogged != ptr) { _suppressedLogged = ptr; Log("[silent] " + spoken); }
                    return;
                }
                if (!ForceRead && zone == "hand" && focus.Scope == null && !InputBridge.ArrowJustPressed && Time.realtimeSinceStartup - _playedAt < 2.5f
                    && Time.realtimeSinceStartup >= SuppressFocusUntil)
                {
                    Log("[quiet] " + spoken);
                    return;
                }
                if (!ForceRead && ((Time.realtimeSinceStartup < SuppressFocusUntil && !InputBridge.ArrowJustPressed) || Time.realtimeSinceStartup < _flickerUntil
                    || (InputBridge.MoveHeld && !InputBridge.ArrowJustPressed)))
                {
                    if (_suppressedLogged != ptr) { _suppressedLogged = ptr; Log("[suppressed] " + spoken); }
                    _lastFocus = IntPtr.Zero;
                    _lastZone = zoneBefore;
                    return;
                }
                _forceUntil = 0f;
                _heardFocus = ptr;
                _heardName = CardNameOf(current);
                // Let a line that announces an arrival finish before the first focus line follows it.
                Announce(spoken, interrupt: true);
            }
            catch (Exception ex)
            {
                Log("[probe] " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static int HeldCount(S_ControllerFocus focus)
        {
            try
            {
                IM_Hand hand = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
                return hand != null && hand.HeldCards != null ? hand.HeldCards.Count : 0;
            }
            catch { return 0; }
        }

        // A shop lays its cards out in rows. Say which row and where along it.
        private static string ShopPlace(S_ControllerFocus focus, Il2CppSystem.Object current)
        {
            try
            {
                Component root = focus.Scope.Cast<Il2CppSystem.Object>().TryCast<Component>();
                Component me = current.TryCast<Component>();
                Camera cam = focus.FocusCamera != null ? focus.FocusCamera : Camera.main;
                if (root == null || me == null || cam == null) return "";
                Vector3 mine = cam.WorldToViewportPoint(me.transform.position);
                var rows = new List<float>();
                int before = 0, inRow = 0;
                foreach (V_GameplayCardInstance c in root.GetComponentsInChildren<V_GameplayCardInstance>(false))
                {
                    if (c == null || ShopPrice(c).Length == 0) continue;
                    Vector3 at = cam.WorldToViewportPoint(c.transform.position);
                    bool known = false;
                    foreach (float y in rows) if (Mathf.Abs(y - at.y) < 0.08f) { known = true; break; }
                    if (!known) rows.Add(at.y);
                    if (Mathf.Abs(at.y - mine.y) < 0.08f)
                    {
                        inRow++;
                        if (at.x < mine.x - 0.01f) before++;
                    }
                }
                if (inRow == 0) return "";
                rows.Sort();
                rows.Reverse();
                int row = 1;
                for (int i = 0; i < rows.Count; i++) if (Mathf.Abs(rows[i] - mine.y) < 0.08f) row = i + 1;
                string rowName = rows.Count < 2 ? "" : row == 1 ? Loc.T("shop.top_row", "top row") + ", " : row == rows.Count ? Loc.T("shop.bottom_row", "bottom row") + ", " : Loc.T("shop.row", "row {0}", row) + ", ";
                return rowName + Loc.T("pos.of", "{0} of {1}", before + 1, inRow);
            }
            catch { return ""; }
        }

        private static string Cards(int n) => n == 1 ? Loc.T("count.card", "{0} card", n) : Loc.T("count.cards", "{0} cards", n);
        private static string Things(int n) => n == 1 ? Loc.T("count.thing", "{0} thing", n) : Loc.T("count.things", "{0} things", n);

        private static string ZoneOf(S_ControllerFocus focus, Il2CppSystem.Object current, FocusMode mode)
        {
            if (mode == FocusMode.Target) return "targets";
            bool isCard = current.TryCast<IV_CardInstance>() != null;
            string scope = TypeName(focus.Scope);
            if (scope.Contains("LootAll") || scope.Contains("CardChoice")) return "offer";
            if (scope.Contains("BowlShop")) return isCard ? (mode.ToString() == "Hand" ? "food" : "shop") : "shop-buttons";
            // The focus can reach a card a moment before the game's mode says Hand.
            if (isCard) return mode.ToString() == "Hand" || InHand(focus, current) ? "hand" : "";
            if (current.TryCast<IControllerFocusTarget>() != null && current.TryCast<UnityEngine.UI.Selectable>() == null) return "nearby";
            return "";
        }

        private static bool InHand(S_ControllerFocus focus, Il2CppSystem.Object current)
        {
            try
            {
                IV_CardInstance view = current.TryCast<IV_CardInstance>();
                IntPtr model = view != null && view.IModel != null ? view.IModel.Pointer : IntPtr.Zero;
                IM_Hand hand = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
                var held = hand != null ? hand.HeldCards : null;
                for (int i = 0; held != null && i < held.Count; i++)
                    if (held[i] != null && held[i].Pointer == model) return true;
            }
            catch { }
            return false;
        }

        // Walking stops without a word when the player reaches something. Say what has come into reach.
        private static void AnnounceArrivals(S_ControllerFocus focus)
        {
            // Enemies summoned one after another (a boss's babies) are said together.
            if (_joinDue > 0f && Time.realtimeSinceStartup >= _joinDue)
            {
                _joinDue = 0f;
                if (_inFight && _joinPending.Count > 0)
                {
                    Cue.Play(Cue.Arrival);
                    Announce(Loc.T("fight.joined", "Joined the fight: {0}.", Group(_joinPending)), interrupt: false, holdWhileWalking: true);
                }
                _joinPending.Clear();
            }
            WorldFocusCandidates world = focus.WorldFocusCandidates;
            int n = world != null ? world.Count : 0;
            // An empty list is a menu over the world, not the world emptied; while a target is being
            // chosen the list holds only what the card can be used on. Either way, what was there is
            // still there: keep it known however long the menu stays up.
            if (n == 0 || focus.Mode == FocusMode.Target || focus.Scope != null)
            {
                float stamp = Time.realtimeSinceStartup;
                foreach (IntPtr key in new List<IntPtr>(_nearbyAt.Keys)) _nearbyAt[key] = stamp;
                return;
            }
            var fresh = new List<string>();
            var freshOthers = new List<string>();
            var freshEnemies = new List<string>();
            float time = Time.realtimeSinceStartup;
            for (int i = 0; i < n; i++)
            {
                IControllerFocusTarget t = world.At(i);
                if (t == null) continue;
                if (focus.InCombat && !WorldFocusCandidates.InCombatScope(t)) continue;
                V_SpellReceiverDoor door = t.Cast<Il2CppSystem.Object>().TryCast<V_SpellReceiverDoor>();
                if (door != null && !_doors.ContainsKey(t.Pointer)) { _doors[t.Pointer] = door; _doorOpen[t.Pointer] = Safe(() => door.Door.IsUnlockedAndOpen()); }
                // Known means seen in the last twenty seconds: the list drops things for a moment
                // when the game changes mode, and that is not the thing arriving again.
                bool known = _nearbyAt.TryGetValue(t.Pointer, out float last) && time - last < 20f;
                _nearbyAt[t.Pointer] = time;
                if (known) continue;
                string name = FirstSentence(Describe(t, out _));
                // Building plots are furniture; they are listed when a camp card looks for a place.
                if (name.StartsWith(Loc.T("plot.empty", "Empty plot"))) continue;
                // The thing just built, lit or changed by the card played a moment ago.
                if (time - _playedAt < 3f && _playedName.Length > 0 && (name.StartsWith(_playedName) || t.Cast<Il2CppSystem.Object>().TryCast<SpellReceiverRoom>() != null)) continue;
                if (time - _playedAt < 3f && name == _targetName) continue;
                fresh.Add(name);
                if (t.Cast<Il2CppSystem.Object>().TryCast<V_Enemy>() != null) freshEnemies.Add(name);
                else if (_seenEver.Add(t.Pointer)) freshOthers.Add(name);
            }
            if (fresh.Count == 0) return;

            if (_inFight)
            {
                // The opening line names who is there; later arrivals have joined.
                _seen.AddRange(freshOthers);
                if (time - _combatStartAt < CombatOpening || freshEnemies.Count == 0) return;
                // Wait a little for more; never more than three seconds after the first.
                if (_joinPending.Count == 0) _joinFirstAt = time;
                _joinPending.AddRange(freshEnemies);
                _joinDue = Mathf.Min(time + 1.8f, _joinFirstAt + 3f);
                return;
            }
            Cue.Play(Cue.Arrival);
            // On the move, or with a summary about to be given, the sound is enough.
            if (InputBridge.MoveHeld || time - _lastWalkAt < 1.5f || _summaryDue > 0f || _afterFight)
            {
                _seen.AddRange(freshOthers);
                Log("[seen] " + string.Join(", ", fresh));
                return;
            }
            Announce(Loc.T("nearby.list", "Nearby: {0}.", string.Join("|", fresh)), interrupt: false, holdWhileWalking: true);
            _protectUntil = Time.realtimeSinceStartup + 1.5f;
            _protectedLineQueued = false;
        }

        private static readonly Dictionary<IntPtr, float> _nearbyAt = new Dictionary<IntPtr, float>();
        private static readonly List<string> _joinPending = new List<string>();
        private static float _joinDue, _joinFirstAt;
        private static string _targetName = "";

        // Set by Main when the title screen loads: the time after which the one-off experiments run.
        internal static float ArmedAt;

        // A short spoken line for any focusable thing, plus a longer diagnostic string for the log.
        internal static string Describe(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase item, out string detail)
        {
            detail = "";
            if (item == null) return Loc.T("thing.nothing", "nothing");
            Il2CppSystem.Object obj = item.Cast<Il2CppSystem.Object>();

            IV_CardInstance card = obj.TryCast<IV_CardInstance>();
            if (card != null)
            {
                IM_CardInstance model = card.IModel;
                ICardData data = model != null ? model.IData : null;
                string name = model != null ? model.CardName : null;
                string desc = data != null ? data.SingleDescription : null;
                GameplayCardData gameplay = data != null ? data.TryCast<GameplayCardData>() : null;
                string cost = gameplay != null ? ", " + Loc.T("card.cost", "cost {0}", gameplay.EnergyUse) : "";
                string uses = CardUses(gameplay);
                detail = "card name=" + name + " deck=" + (data != null ? data.DeckType.ToString() : "?") + cost + " uses=" + uses + " desc=" + desc;
                Component cardComponent = obj.TryCast<Component>();
                string price = cardComponent != null ? ShopPrice(cardComponent) : "";
                string buy = price.Length > 0 ? ". " + Loc.T("card.for_sale", "For sale: {0} food", price.Replace("COST:", "").Trim()) : "";
                // The label under the title: uses left, battery, evolve level ("Dead battery").
                string label = CardLabel(obj);
                if (label.Length > 0) name += ", " + label;
                detail += " label=" + label;
                return Clean(name + cost + (uses.Length > 0 ? ". " + uses : "") + (string.IsNullOrEmpty(desc) ? "" : ". " + desc) + buy);
            }

            CampStructureSpellReceiver receiver = obj.TryCast<CampStructureSpellReceiver>();
            if (receiver != null)
            {
                BaseRoomEncounter encounter = receiver.ParentEncounter;
                string name = encounter != null ? Humanize(encounter.gameObject.name) : Humanize(receiver.gameObject.name);
                string accepts = Accepts(Safe(() => receiver.GetPromptsAndInteractions().Item2));
                detail = "receiver name=" + name + " encounter=" + (encounter != null ? encounter.gameObject.name : "") + " accepts=" + accepts + " interactive=" + receiver.IsInteractive;
                return Clean(name + (accepts.Length > 0 ? ". " + accepts : ""));
            }

            V_SpellReceiverDoor door = obj.TryCast<V_SpellReceiverDoor>();
            if (door != null)
            {
                BaseDoor baseDoor = Safe(() => door.Door);
                // The sign on a door says what lies behind it.
                string sign = baseDoor != null && baseDoor.Tooltip != null ? Safe(() => baseDoor.Tooltip.Text) : null;
                bool open = baseDoor != null && Safe(() => baseDoor.IsUnlockedAndOpen());
                string accepts = open ? "" : Accepts(Safe(() => door.GetPromptsAndInteractions().Item2));
                detail = "door object=" + door.gameObject.name + " sign=" + sign + " open=" + open + " accepts=" + accepts + " canUnlock=" + door.CanUnlock;
                return Clean(Loc.T("door.name", "Door") + (open ? ", " + Loc.T("state.open", "open") : "") + (string.IsNullOrWhiteSpace(sign) ? "" : ". " + sign) + (accepts.Length > 0 ? ". " + accepts : ""));
            }

            CharacterSelection_Button character = obj.TryCast<CharacterSelection_Button>();
            if (character != null)
            {
                string name = character._className != null ? character._className.text : Humanize(character.gameObject.name);
                bool unlocked = Safe(() => character._isUnlocked);
                detail = "character " + name + " unlocked=" + unlocked;
                return Clean(name + (unlocked ? "" : ", " + Loc.T("state.locked", "locked")));
            }

            SwapOutObjectButton swap = obj.TryCast<SwapOutObjectButton>();
            if (swap != null)
            {
                string price = FirstText(swap, "COST");
                string what = swap.gameObject.name.Contains("Forget") ? Loc.T("shop.forget", "Forget a card") : Humanize(swap.gameObject.name);
                detail = "swap " + swap.gameObject.name + " " + price;
                return Clean(what + (price.Length > 0 ? ", " + Loc.T("shop.price_food", "{0} food", price.Replace(":", "")) : ""));
            }

            Bowl bowl = obj.TryCast<Bowl>();
            if (bowl != null)
            {
                string price = FirstText(bowl, "COST");
                detail = "bowl " + price;
                return Clean(Loc.T("shop.bowl", "The bowl. Enter pays with this food") + (price.Length > 0 ? ". " + Loc.T("shop.bowl_due", "{0} food still to pay", price.Replace(":", "")) : ""));
            }

            SpellReceiverRoom plot = obj.TryCast<SpellReceiverRoom>();
            if (plot != null)
            {
                // A place in a camp where a Camp card can be built, or where something already stands.
                string where = Humanize(plot.gameObject.name).Replace("Room Spell Target", "").Replace("Floor", "").Replace("  ", " ").Trim().ToLowerInvariant();
                V_Hazard on = plot.GetComponentInChildren<V_Hazard>();
                string what;
                if (on != null) what = Describe(on, out _);
                else if (Safe(() => plot.IsHoldingAStructure))
                {
                    BaseRoomEncounter built = plot.GetComponentInChildren<BaseRoomEncounter>();
                    what = built != null ? Humanize(built.gameObject.name) : Loc.T("plot.built", "Something built");
                }
                else what = Loc.T("plot.empty", "Empty plot");
                var badges = new List<string>();
                foreach (TMP_Text t in plot.GetComponentsInChildren<TMP_Text>(false))
                    if (t != null && !string.IsNullOrWhiteSpace(t.text)) badges.Add(t.text.Trim());
                detail = "plot " + plot.gameObject.name + " what=" + what + " badges=" + string.Join("/", badges) + " location=" + plot.Location;
                return Clean(what + (where.Length > 0 ? ", " + where : "") + (on == null && badges.Count > 0 ? ", " + Loc.T("plot.marked", "marked {0}", string.Join(" ", badges)) : ""));
            }

            Chest chest = obj.TryCast<Chest>();
            if (chest != null)
            {
                string path = PathOf(chest.transform);
                string kind = path.Contains("Foresight") ? Loc.T("chest.foresight", "Foresight chest") : path.Contains("Class Cards") ? Loc.T("chest.class", "Class card chest") : Loc.T("chest.plain", "Chest");
                string accepts = Accepts(Safe(() => chest.GetPromptsAndInteractions().Item2));
                detail = "chest " + path + " accepts=" + accepts + " interactive=" + Safe(() => chest.IsInteractive);
                return Clean(kind + (accepts.Length > 0 ? ". " + accepts : ""));
            }

            ModifyCardEncounterUI modify = obj.TryCast<ModifyCardEncounterUI>();
            if (modify != null)
            {
                var parts = new List<string>();
                foreach (TMP_Text t in modify.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (t == null || string.IsNullOrWhiteSpace(t.text)) continue;
                    if (t.GetComponentInParent<UnityEngine.UI.Selectable>() != null) continue;
                    parts.Add(t.text.Trim());
                }
                string chosen = null;
                try { chosen = modify._augementingMCard != null ? modify._augementingMCard.CardName : null; } catch { }
                if (string.IsNullOrWhiteSpace(chosen)) chosen = ModifyPicked;
                string effect = ModifyEffect(modify, withMadeCards: false);
                detail = "modify " + string.Join(" / ", parts) + " chosen=" + chosen;
                // The card picked and what it is about to gain, then what Enter does.
                if (!string.IsNullOrWhiteSpace(chosen) && effect.Length > 0)
                    return Clean(Loc.T("modify.apply_to", "{0} gains: {1}. Enter applies it; Backspace goes back", chosen, effect.TrimEnd('.')));
                return Loc.T("modify.apply", "Enter applies it to the card you chose; Backspace goes back");
            }

            // A card in the hand as the target of another card (Vile Oil): the game focuses the
            // card itself rather than its picture, so the picture is found and read instead.
            IM_CardInstance targetCard = obj.TryCast<IM_CardInstance>();
            if (targetCard != null)
            {
                V_GameplayCardInstance shown = CardViewOf(targetCard.Pointer);
                if (shown != null)
                {
                    string line = Describe(shown, out detail);
                    detail = "card target: " + detail;
                    return line;
                }
                ICardData data = Safe(() => targetCard.IData);
                string desc = data != null ? Safe(() => data.SingleDescription) : null;
                detail = "card target (no view) name=" + targetCard.CardName + " desc=" + desc;
                return Clean(targetCard.CardName + (string.IsNullOrWhiteSpace(desc) ? "" : ". " + desc));
            }

            V_Enemy enemy = obj.TryCast<V_Enemy>();
            if (enemy != null)
            {
                M_Enemy m = Safe(() => enemy.Model);
                detail = "enemy object=" + enemy.gameObject.name;
                if (m == null) return Humanize(enemy.gameObject.name);
                string line = EnemyName(enemy, m) + ". " + EnemyHealth(m);
                string intent = string.Join(", ", EnemyIntents(m, brief: true));
                if (intent.Length > 0) line += ". " + Loc.T("enemy.will", "Will {0}", intent);
                string statuses = string.Join(", ", EnemyStatuses(m));
                if (statuses.Length > 0) line += ". " + statuses;
                return Clean(line);
            }

            V_Hazard hazard = obj.TryCast<V_Hazard>();
            if (hazard != null)
            {
                string name = Safe(() => hazard.Model.NiceName.GetText());
                if (string.IsNullOrWhiteSpace(name)) name = Safe(() => hazard.HazardName);
                if (string.IsNullOrWhiteSpace(name)) name = Humanize(hazard.gameObject.name);
                string accepts = Accepts(Safe(() => hazard.GetPromptsAndInteractions().Item2));
                string effect = HazardEffect(Safe(() => hazard.Model));
                detail = "hazard name=" + name + " object=" + hazard.gameObject.name + " accepts=" + accepts + " effect=" + effect;
                return Clean(name + (effect.Length > 0 ? ". " + effect : "") + (accepts.Length > 0 ? ". " + accepts : ""));
            }

            EncounterStartWorldObject starter = obj.TryCast<EncounterStartWorldObject>();
            if (starter != null)
            {
                detail = "encounterStart " + starter.gameObject.name + " dist=" + starter.PlayerDistance.ToString("F1");
                return Humanize(starter.gameObject.name);
            }

            Component comp = obj.TryCast<Component>();
            if (comp != null)
            {
                var texts = new List<string>();
                foreach (TMP_Text t in comp.GetComponentsInChildren<TMP_Text>(false))
                {
                    string s = t != null ? t.text : null;
                    if (!string.IsNullOrWhiteSpace(s)) texts.Add(s.Trim());
                }
                detail = PathOf(comp.transform) + " | " + string.Join(" / ", texts);

                PathSelectionNode path = comp.GetComponentInParent<PathSelectionNode>();
                if (path != null && texts.Count == 0)
                    return Humanize(path.gameObject.name) + ", " + Humanize(path.CurrentPathState.ToString());

                string own = texts.Count > 0 ? string.Join(", ", texts.GetRange(0, Math.Min(3, texts.Count))) : "";
                if (own.Length > 160) own = texts[0];
                string role = ControlRole(comp);
                string value = ControlValue(comp);
                // In a settings row the name is the label beside the control and the control's own
                // text is its current setting ("Ambient occlusion" / "ON").
                string section = SectionLabel(comp);
                string label;
                if (section != null)
                {
                    label = section;
                    if (value.Length == 0) value = own;
                }
                else label = own.Length > 0 ? own : ControlLabel(comp);
                string spoken = Clean(label + (role.Length > 0 ? ", " + role : "") + (value.Length > 0 ? ", " + value : ""));
                if (comp.gameObject.name == "AllowControllerButton")
                    spoken += ". " + Loc.T("settings.allow_controller", "Leave this on: the screen reader mod steers the game through its controller support");
                string heading = HeadingAbove(comp);
                if (heading != _lastHeading)
                {
                    _lastHeading = heading;
                    if (heading.Length > 0) spoken = heading + ". " + spoken;
                }
                return spoken;
            }
            return TypeName(obj);
        }

        // What the card does when cast on something in the world, as printed on its face ("Fix or Break").
        private static string CardUses(GameplayCardData gameplay)
        {
            if (gameplay == null) return "";
            try
            {
                string text = gameplay.BuildOrableDescription(out string _);
                return string.IsNullOrWhiteSpace(text) ? "" : Clean(text).TrimEnd('.');
            }
            catch { return ""; }
        }

        internal static string EnemyName(V_Enemy view, M_Enemy m)
        {
            string name = Safe(() => m.GetName());
            if (string.IsNullOrWhiteSpace(name) || name.Contains("(Clone)")) name = Safe(() => m.LocalizedEnemyName.GetText());
            if (string.IsNullOrWhiteSpace(name)) name = Humanize(view.gameObject.name);
            return Clean(name.Replace("(Clone)", ""));
        }

        internal static string EnemyHealth(M_Enemy m)
        {
            EnemyHealthData h = m.HealthData;
            return Loc.T("health.of", "Health {0} of {1}", h.CurrentHealth, h.MaxHealth) + (h.Armour > 0 ? ", " + Loc.T("health.armour", "armour {0}", h.Armour) : "");
        }

        // What the enemy will do on its turn. brief: "Deal Damage 3"; otherwise the game's own sentence.
        internal static List<string> EnemyIntents(M_Enemy m, bool brief)
        {
            var list = new List<string>();
            try
            {
                EnemyTurnAction action = m.CurrentTurnAction;
                var pairs = action != null ? action.EnemyIntentPairs : null;
                for (int i = 0; pairs != null && i < pairs.Count; i++)
                {
                    EnemyIntentPair pair = pairs[i];
                    if (pair == null || pair.Intent == null) continue;
                    string label = Humanize(pair.Intent.name).Replace("Enemy Intent", "").Replace("  ", " ").Trim();
                    if (label.StartsWith("Enemy ")) label = label.Substring(6);
                    // The game shows these two as a dagger and a shield with a number.
                    if (label == "Deal Damage") label = Loc.T("intent.attack", "attack for");
                    else if (label == "Block Damage") label = Loc.T("intent.block", "block");
                    else if (label.Length > 0 && label != "Nothing") label = Loc.T("intent.use", "use {0}", label);
                    if (label.Length == 0 || label == "Nothing") continue;
                    string text = null;
                    if (!brief) text = Safe(() => pair.Intent.GetIntentToolTipText("ShroomAccess", pair, m.StatusEffects));
                    if (string.IsNullOrWhiteSpace(text)) text = label + (pair.ModifiedNumber != 0 ? " " + pair.ModifiedNumber : "");
                    else text = AfterWill(Clean(text));
                    list.Add(Clean(text));
                }
            }
            catch (Exception ex) { Log("[enemy] intents: " + ex.Message); }
            return list;
        }

        // A hazard in a camp (a creature, The Encroaching Gloom) adds threat, or gives warmth, and
        // may act at the end of the turn like an enemy. Said as the game's own words where it has them.
        internal static string HazardEffect(M_Hazard m)
        {
            if (m == null) return "";
            var parts = new List<string>();
            try
            {
                int total = m.GetTotalThreatLevel(out int warmth, out int threat);
                if (threat > 0) parts.Add(Loc.T("hazard.adds_threat", "Adds threat {0}", threat));
                if (warmth > 0) parts.Add(Loc.T("hazard.gives_warmth", "Gives warmth {0}", warmth));
                if (threat == 0 && warmth == 0 && total != 0) parts.Add(Loc.T("hazard.threat", "Threat {0}", total));
            }
            catch { }
            try
            {
                EnemyTurnAction action = m.CurrentTurnAction;
                var pairs = action != null ? action.EnemyIntentPairs : null;
                for (int i = 0; pairs != null && i < pairs.Count; i++)
                {
                    EnemyIntentPair pair = pairs[i];
                    if (pair == null || pair.Intent == null) continue;
                    string text = Safe(() => pair.Intent.GetIntentToolTipText("ShroomAccess", pair, m.StatusEffects));
                    if (string.IsNullOrWhiteSpace(text)) text = Humanize(pair.Intent.name).Replace("Enemy Intent", "").Trim() + (pair.ModifiedNumber != 0 ? " " + pair.ModifiedNumber : "");
                    else
                    {
                        // The game's own lines ("THE GLOOM BECKONS YOU FORWARD / IF YOU REST, TAKE 5
                        // DAMAGE"), one sentence each, in ordinary case.
                        var lines = text.Replace("\\n", "\n").Split('\n').Select(l => Clean(l).TrimEnd('.', ' ')).Where(l => l.Length > 0)
                            .Select(l => l.Substring(0, 1).ToUpperInvariant() + l.Substring(1).ToLowerInvariant());
                        // A line ending in a comma runs on into the next.
                        var sb = new StringBuilder();
                        foreach (string l in lines)
                        {
                            if (sb.Length > 0) sb.Append(sb[sb.Length - 1] == ',' ? " " + l.ToLowerInvariant() : ". " + l);
                            else sb.Append(l);
                        }
                        text = sb.ToString();
                    }
                    if (text.Trim().Length > 0) parts.Add(Clean(text));
                }
            }
            catch (Exception ex) { Log("[hazard] intents: " + ex.Message); }
            return string.Join(". ", parts);
        }

        // What all the enemies' attack intents add up to, as a sighted player totals the daggers
        // above their heads. Armour and statuses are not taken off.
        internal static string IncomingDamage()
        {
            int total = IncomingTotal(out int hits);
            return hits == 0 ? Loc.T("status.no_attacks", "No attacks coming")
                : hits == 1 ? Loc.T("status.incoming_one", "Incoming: {0} damage from {1} attack", total, hits)
                : Loc.T("status.incoming", "Incoming: {0} damage from {1} attacks", total, hits);
        }

        internal static int IncomingTotal(out int hits)
        {
            int total = 0;
            hits = 0;
            try
            {
                foreach (V_Enemy view in Hud.EnemyViews())
                {
                    M_Enemy m = view != null ? Safe(() => view.Model) : null;
                    if (m == null || !m.IsAlive) continue;
                    EnemyTurnAction action = m.CurrentTurnAction;
                    var pairs = action != null ? action.EnemyIntentPairs : null;
                    for (int i = 0; pairs != null && i < pairs.Count; i++)
                    {
                        EnemyIntentPair pair = pairs[i];
                        if (pair == null || pair.Intent == null || !IsDamageIntent(pair.Intent.name)) continue;
                        total += pair.ModifiedNumber;
                        hits++;
                    }
                }
            }
            catch (Exception ex) { Log("[enemy] incoming: " + ex.Message); }
            return total;
        }

        // An attack, or a Megashroom's explosion: its number is damage to the player.
        internal static bool IsDamageIntent(string intentName)
        {
            string name = Humanize(intentName ?? "");
            return name.Contains("Deal Damage") || name.IndexOf("Explode", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // The game words what an enemy will do as "DEALS 3 DAMAGE", "SHIELDS SELF". After "Will"
        // that needs the plain verb.
        private static string AfterWill(string sentence)
        {
            string s = sentence.Trim().TrimEnd('.').ToLowerInvariant();
            if (s.StartsWith("will ")) s = s.Substring(5);
            int space = s.IndexOf(' ');
            string verb = space > 0 ? s.Substring(0, space) : s;
            string rest = space > 0 ? s.Substring(space) : "";
            if (verb.EndsWith("ies") && verb.Length > 4) verb = verb.Substring(0, verb.Length - 3) + "y";
            else if (verb.EndsWith("s") && !verb.EndsWith("ss") && verb.Length > 3) verb = verb.Substring(0, verb.Length - 1);
            return verb + rest;
        }

        internal static List<string> EnemyStatuses(M_Enemy m)
        {
            var list = new List<string>();
            try
            {
                var effects = m.StatusEffects;
                for (int i = 0; effects != null && i < effects.Count; i++)
                {
                    StatusEffectPair pair = effects[i];
                    if (pair == null || pair.HiddenFromDisplay || pair.StatusEffect == null) continue;
                    list.Add(Humanize(pair.StatusEffect.name) + (pair.Number != 0 ? " " + pair.Number : ""));
                }
            }
            catch (Exception ex) { Log("[enemy] statuses: " + ex.Message); }
            return list;
        }

        // ---- Ordinary controls: sliders, checkboxes, dropdowns, tabs, buttons with no text ----

        // A control with no text of its own is named by the label beside it, else by its object name.
        private static string ControlLabel(Component comp)
        {
            Transform parent = comp.transform.parent;
            if (parent != null && comp.TryCast<UnityEngine.UI.Button>() == null)
            {
                foreach (TMP_Text t in parent.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (t == null || string.IsNullOrWhiteSpace(t.text)) continue;
                    UnityEngine.UI.Selectable owner = t.GetComponentInParent<UnityEngine.UI.Selectable>();
                    if (owner != null && owner.Pointer != comp.Pointer) continue;
                    return t.text.Trim();
                }
            }
            string name = Humanize(comp.gameObject.name);
            if (name.StartsWith("Hamburger")) return Loc.T("control.menu", "Menu");
            string deckButton = DeckView.ButtonName(comp.gameObject.name);
            if (deckButton.Length > 0 && DeckDisplay.DeckDisplayIsActive) return deckButton;
            if (name == "Pack Icon") return Loc.T("control.pack_icon", "Starting cards. Enter opens the deck");
            // The bare arrow in the corner of the character screen.
            if (name == "Button" && Walk.Scene.Contains("CharacterSelection")) return Loc.T("control.back", "Back");
            foreach (string suffix in new[] { " Button", " Slider", " Toggle", " Dropdown" })
                if (name.EndsWith(suffix)) name = name.Substring(0, name.Length - suffix.Length);
            return name;
        }

        private static string ControlRole(Component comp)
        {
            if (comp.TryCast<UnityEngine.UI.Slider>() != null) return Loc.T("role.slider", "slider");
            if (comp.TryCast<UnityEngine.UI.Toggle>() != null) return Loc.T("role.checkbox", "checkbox");
            if (comp.TryCast<TMP_Dropdown>() != null) return Loc.T("role.dropdown", "dropdown");
            if (comp.gameObject.name.EndsWith("TabButton")) return Loc.T("role.tab", "tab");
            // The Controls page rebinds controller buttons; the mod's keys are fixed.
            if (comp.transform.parent != null && comp.transform.parent.GetComponent<Il2CppControllerNav.FocusRebindSection>() != null) return Loc.T("role.rebind", "controller button, Enter to rebind");
            return "";
        }

        private static string _lastHeading = "";

        // Plain text that sits above a row of settings as a heading for the rows below it.
        private static string HeadingAbove(Component comp)
        {
            try
            {
                Transform row = comp.transform;
                for (int depth = 0; row.parent != null && depth < 4; depth++)
                {
                    bool isSection = false;
                    foreach (Component c in row.GetComponents<Component>())
                        if (c != null && c.GetIl2CppType().Name.EndsWith("Section")) { isSection = true; break; }
                    if (isSection) break;
                    row = row.parent;
                }
                if (comp.GetComponentInParent<GameSettingsUI>() == null) return "";
                Transform list = row.parent;
                if (list == null) return "";
                for (int i = row.GetSiblingIndex() - 1; i >= 0; i--)
                {
                    Transform sibling = list.GetChild(i);
                    if (!sibling.gameObject.activeInHierarchy) continue;
                    if (sibling.GetComponentInChildren<UnityEngine.UI.Selectable>() != null) continue;
                    var parts = new List<string>();
                    foreach (TMP_Text t in sibling.GetComponentsInChildren<TMP_Text>(false))
                    {
                        string text = t != null ? Clean(t.text) : "";
                        if (text.Length > 1) parts.Add(text.TrimEnd('.', '!').TrimStart('-'));
                    }
                    if (parts.Count > 0) return string.Join(". ", parts);
                }
            }
            catch { }
            return "";
        }

        // The current setting shown by a control: a slider's position, a checkbox's state, a
        // dropdown's choice, or the text on a settings button that names its own state ("ON").
        private static string LiveValue(Il2CppSystem.Object item)
        {
            Component control = item != null ? item.TryCast<Component>() : null;
            if (control == null) return "";
            string value = ControlValue(control);
            if (value.Length == 0 && control.TryCast<UnityEngine.UI.Button>() != null && SectionLabel(control) != null)
            {
                TMP_Text own = control.GetComponentInChildren<TMP_Text>();
                value = own != null ? Clean(own.text) : "";
            }
            return value;
        }

        // The label of the settings row a control sits in, if it sits in one.
        private static string SectionLabel(Component comp)
        {
            try
            {
                if (comp.GetComponentInParent<GameSettingsUI>() == null) return null;
                Transform t = comp.transform.parent;
                for (int depth = 0; t != null && depth < 4; depth++, t = t.parent)
                {
                    bool isSection = false;
                    foreach (Component c in t.GetComponents<Component>())
                        if (c != null && c.GetIl2CppType().Name.EndsWith("Section")) { isSection = true; break; }
                    if (!isSection) continue;
                    for (int i = 0; i < t.childCount; i++)
                    {
                        TMP_Text text = t.GetChild(i).GetComponent<TMP_Text>();
                        if (text != null && !string.IsNullOrWhiteSpace(text.text)) return text.text.Trim();
                    }
                    return null;
                }
            }
            catch { }
            return null;
        }

        internal static string ControlValue(Component comp)
        {
            try
            {
                UnityEngine.UI.Slider slider = comp.TryCast<UnityEngine.UI.Slider>();
                if (slider != null)
                    return slider.wholeNumbers ? ((int)slider.value).ToString() : Loc.T("value.percent", "{0} percent", (int)Math.Round(slider.normalizedValue * 100f));
                UnityEngine.UI.Toggle toggle = comp.TryCast<UnityEngine.UI.Toggle>();
                if (toggle != null) return toggle.isOn ? Loc.T("value.checked", "checked") : Loc.T("value.not_checked", "not checked");
                TMP_Dropdown dropdown = comp.TryCast<TMP_Dropdown>();
                if (dropdown != null && dropdown.captionText != null) return dropdown.captionText.text;
            }
            catch { }
            return "";
        }

        private static string FirstText(Component within, string startsWith)
        {
            try
            {
                foreach (TMP_Text t in within.GetComponentsInChildren<TMP_Text>(false))
                    if (t != null && t.text != null && t.text.Trim().StartsWith(startsWith, StringComparison.OrdinalIgnoreCase)) return t.text.Trim();
            }
            catch { }
            return "";
        }

        // In a shop each card stands in its own slot with a price tag under it.
        internal static string ShopPrice(Component cardView)
        {
            try
            {
                Transform t = cardView.transform.parent;
                for (int depth = 0; t != null && depth < 3; depth++, t = t.parent)
                {
                    if (t.GetComponentsInChildren<V_GameplayCardInstance>(false).Length != 1) return "";
                    string price = FirstText(t.GetComponent<Transform>(), "COST");
                    if (price.Length > 0) return price;
                }
            }
            catch { }
            return "";
        }

        private static T Safe<T>(Func<T> read)
        {
            try { return read(); } catch { return default; }
        }

        // The card actions a world object will accept ("Dig", "Unlock"), as the game names them.
        private static string Accepts(Il2CppSystem.Collections.Generic.List<OrableWithMeta> list)
        {
            if (list == null) return "";
            var names = new List<string>();
            bool mysterious = false;
            for (int i = 0; i < list.Count; i++)
            {
                OrableWithMeta item = list[i];
                if (item == null) continue;
                if (item.IsMysterious) { mysterious = true; continue; }
                OrableCardIntent intent = item.OrableIntent;
                if (intent == null) continue;
                string label = intent.EnvironmentPromptTooltip;
                if (string.IsNullOrWhiteSpace(label)) label = Humanize(intent.name);
                if (!names.Contains(label)) names.Add(label);
            }
            if (mysterious) names.Add(names.Count > 0 ? Loc.T("accepts.also_unknown", "Also takes something unknown.") : Loc.T("accepts.unknown", "Takes something unknown."));
            return string.Join(" ", names);
        }

        // "ShovelChoiceEncounterWorldObject" -> "Shovel Choice"; "UI_PathSeletion_Path_03" -> "Path 03".
        internal static string Humanize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = raw.Replace("(Clone)", "").Replace("EncounterWorldObject", "").Replace("WorldObject", "")
                .Replace(" Encounter", "").Replace(" With UI", "").Replace("PT ", "").Replace("Choice", "")
                .Replace("UI_PathSeletion_", "").Replace("Button_", "").Replace("RawImage_", "").Replace("_BG", "").Replace("_", " ").Trim();
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(s[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            // Joined-up names ("SoupPotEncounterWithUI") only show these words once split.
            return sb.ToString().Replace(" With UI", "").Replace(" Encounter", "");
        }

        // Rich-text tags out, whitespace tidy, no doubled full stops.
        internal static string Clean(string s)
        {
            // The game's prompt strings carry a literal backslash-n as well as real line breaks.
            s = StripTags(s ?? "").Replace("\\n", " ").Replace("\n", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
            while (s.Contains(". .")) s = s.Replace(". .", ".");
            s = s.Replace("!.", "!").Replace("?.", "?");
            return s.Replace("..", ".");
        }

        // Dev: everything the focus system could currently land on, as text.
        internal static string StateDump()
        {
            var sb = new StringBuilder();
            try
            {
                if (!S_ControllerFocus.TryGetFocus(out S_ControllerFocus focus) || focus == null) return "no focus service";
                sb.AppendLine("mode=" + focus.Mode + " combat=" + focus.InCombat + " blockedBy=" + focus.BlockedBy + " scope=" + TypeName(focus.Scope) + " stack=" + focus.StackDepth);
                try
                {
                    V_Player vp = focus.Player;
                    sb.AppendLine("nav: pos=" + (vp != null ? vp.transform.position.ToString("F1") : "?") + " stickWalks=" + focus.MoveStickWalks
                        + " gates=" + focus.GatesPassed + " padOwnsCast=" + focus.PadOwnsCast + " handBusy=" + focus.HandBusy
                        + " offersHand=" + focus.OffersHandUnasked + " confirmRearm=" + focus._confirmRearmPending + " stepRearm=" + focus._stepRearmPending);
                }
                catch (Exception ex) { sb.AppendLine("nav ! " + ex.Message); }
                try
                {
                    S_InitialTutorial tut = UnityEngine.Object.FindObjectOfType<S_InitialTutorial>();
                    sb.AppendLine("tutorial: " + (tut == null ? "none" : "over=" + tut.IsOver + " active=" + tut._isActive + " stepped=" + tut._hasSteppedForward));
                    foreach (DrawExploreDeckFromHiddenHandTrigger t in UnityEngine.Object.FindObjectsOfType<DrawExploreDeckFromHiddenHandTrigger>())
                        sb.AppendLine("drawTrigger: " + PathOf(t.transform) + " pos=" + t.transform.position.ToString("F1") + " once=" + t.TriggerOnlyOnce);
                    foreach (Trigger t in UnityEngine.Object.FindObjectsOfType<Trigger>())
                        sb.AppendLine("trigger: " + PathOf(t.transform) + " pos=" + t.transform.position.ToString("F1") + " type=" + TypeName(t));
                    foreach (EncounterStartWorldObject o in UnityEngine.Object.FindObjectsOfType<EncounterStartWorldObject>())
                        sb.AppendLine("encounterStart: " + o.gameObject.name + " pos=" + o.transform.position.ToString("F1") + " dist=" + o.PlayerDistance.ToString("F1") + " inRange=" + o.PlayerInRange + " canInteract=" + o.CanInteract + " focusable=" + o.IsFocusable + " clicked=" + o.HasBeenClicked);
                    foreach (TutorialMissDigSpot t in UnityEngine.Object.FindObjectsOfType<TutorialMissDigSpot>())
                        sb.AppendLine("missDigSpot: " + PathOf(t.transform) + " pos=" + t.transform.position.ToString("F1") + " inside=" + t._playerInside);
                }
                catch (Exception ex) { sb.AppendLine("tutorial ! " + ex.Message); }
                Il2CppSystem.Object cur = focus.CurrentFocus;
                sb.AppendLine("focus: " + (cur != null ? TypeName(cur) + " :: " + Describe(cur, out string d0) + " :: " + d0 : "none"));

                try
                {
                    Il2CppBackend.M_Player pm = focus.PlayerModel;
                    if (pm != null)
                    {
                        sb.AppendLine("player: hidden=" + pm.IsInHidden + " explore=" + pm.IsInExplore + " combat=" + pm.IsInCombat
                            + " exploreEncounter=" + pm.IsInExploreEncounter + " startingSeq=" + pm.IsRunningStartingCardSequence
                            + " hp=" + pm.HealthData.CurrentHealth + "/" + pm.HealthData.MaxHealth + " armour=" + pm.HealthData.Armour
                            + " energy=" + pm.Energy + "/" + pm.EnergySoftCap + " gunk=" + pm.StoredGunk + " handState=" + pm.CurrentHandState
                            + " exploreCards=" + pm.NumberOfExploreCards + " combatCards=" + pm.NumberOfCombatCards + " inHand=" + pm.NumberOfCardsInHand);
                        IM_Hand active = pm.GetActiveHand();
                        if (active != null)
                        {
                            var held = active.HeldCards;
                            sb.AppendLine("active hand: held=" + (held != null ? held.Count : 0) + " deck=" + active.CardsInDeckCount + " discard=" + active.CardsInDiscardCount);
                            for (int i = 0; held != null && i < held.Count; i++)
                            {
                                IM_CardInstance c = held[i];
                                ICardData cd = c.IData;
                                sb.AppendLine("  held[" + i + "] " + c.CardName + " :: " + (cd != null ? cd.SingleDescription : ""));
                            }
                        }
                        else sb.AppendLine("active hand: none");
                    }
                    else sb.AppendLine("player: none");
                }
                catch (Exception ex) { sb.AppendLine("player ! " + ex.Message); }

                HandFocusCandidates hand = focus.HandFocusCandidates;
                int hn = hand != null ? hand.Count : 0;
                sb.AppendLine("hand candidates: " + hn);
                for (int i = 0; i < hn; i++)
                {
                    try { sb.AppendLine("  [" + i + "] " + Describe(hand.At(i), out string d) + " :: " + d); }
                    catch (Exception ex) { sb.AppendLine("  [" + i + "] ! " + ex.Message); }
                }

                WorldFocusCandidates world = focus.WorldFocusCandidates;
                int wn = world != null ? world.Count : 0;
                sb.AppendLine("world candidates: " + wn);
                for (int i = 0; i < wn; i++)
                {
                    try
                    {
                        IControllerFocusTarget t = world.At(i);
                        sb.AppendLine("  [" + i + "] " + TypeName(t) + " :: " + Describe(t, out string d) + " :: " + d);
                    }
                    catch (Exception ex) { sb.AppendLine("  [" + i + "] ! " + ex.Message); }
                }
            }
            catch (Exception ex) { sb.AppendLine("! " + ex); }
            return sb.ToString();
        }

        // Speech is confined to the game's own window: nothing is spoken while the player is elsewhere.
        // holdWhileWalking: for lines the player did not ask for. A screen reader cuts speech on every
        // key repeat, so a line spoken while W is held is never heard; it waits for the key to come up.
        internal static void Announce(string text, bool interrupt = true, bool holdWhileWalking = false, bool repeatable = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (!interrupt && Time.realtimeSinceStartup - _campAt < 3f && text.StartsWith("Energy "))
            {
                Log("[folded] " + text);
                return;
            }
            if (!interrupt && Time.realtimeSinceStartup - _combatStartAt < CombatOpening)
            {
                // The opening line of a fight names the enemies; the turn and the energy go without saying.
                if (text.StartsWith("Nearby:") || text.StartsWith("Energy ") || text.StartsWith("New turn") || text.StartsWith("Your turn"))
                {
                    Log("[folded] " + text);
                    return;
                }
                // The hand is dealt: let the card under the cursor be read after this line.
                if (text.StartsWith("Drew ")) SuppressFocusUntil = Mathf.Min(SuppressFocusUntil, Time.realtimeSinceStartup + 0.8f);
            }
            if (!interrupt)
            {
                // Things that happen together (a fight starting, cards dealt, enemies appearing)
                // arrive as a burst. Gather the burst and say it once, in an order that makes sense.
                if (_batch.Count == 0) _batchAt = Time.realtimeSinceStartup;
                // The same words twice are usually one thing seen twice; two enemies of one name
                // dying together are two events (repeatable).
                if (repeatable || !_batch.Contains(text)) _batch.Add(text);
                Log("[queued] " + text);
                return;
            }
            FlushBatch(force: true);
            if (InputBridge.MoveHeld && Speaker.HasDirect)
            {
                Log("[say-direct] " + text);
                Remember(text);
                if (Application.isFocused) Speaker.SayDirect(text);
                return;
            }
            Log("[say] " + text);
            Remember(text);
            // An announcement just made is protected: the next line waits for it to finish. Only the
            // first waits; a second key press means the player is moving on, and from then on every
            // line cuts off the one before, so speech never trails behind the focus.
            bool protect = Time.realtimeSinceStartup <= _protectUntil && !(_protectedLineQueued && InputBridge.JustPressed);
            if (protect && InputBridge.JustPressed) _protectedLineQueued = true;
            if (!protect) _protectUntil = 0f;
            else Log("[waits] the line above waits for the announcement before it");
            if (Application.isFocused) Speaker.Say(text, !protect, asked: InputBridge.JustPressed);
        }

        // ---- Arrivals, fights and camps ----
        // Walking: things coming into reach make the arrival sound and are remembered, not spoken.
        // Stopping at a camp: one line says what was seen. A fight: one line names the enemies, and
        // what was seen on the way in is kept until the fight is over.
        private const float CombatOpening = 4.5f;
        private static readonly List<string> _seen = new List<string>();
        // Things already counted towards a summary; a thing that drops out of reach and comes back is not listed twice.
        private static readonly HashSet<IntPtr> _seenEver = new HashSet<IntPtr>();
        private static float _combatStartAt = -100f;
        private static bool _combatLinePending;
        private static bool _inFight;
        private static float _summaryDue;
        private static string _summaryLead = "";
        private static string _summaryTail = "";
        private static float _lastWalkAt = -100f;
        private static float _campAt = -100f;
        private static bool _afterFight;
        private static float _combatEndAt, _clearSince;

        internal static void SceneChanged(string sceneName)
        {
            // Opening a door streams the next tunnel in as a scene of its own, while the room the
            // player stands in stays: what has been seen is still known.
            if (System.Text.RegularExpressions.Regex.IsMatch(sceneName ?? "", @"^P\w*_T\d")) return;
            _seen.Clear();
            _seenEver.Clear();
            _knownNearby.Clear();
            _summaryDue = 0f;
            _combatLinePending = false;
            _inFight = false;
            _afterFight = false;
            _nearbyAt.Clear();
        }

        // The game has stopped the walk at something that is not a camp (an offer on the path, a
        // closed way). Said only when there is something to list.
        internal static void Stopped()
        {
            if (_inFight || _summaryDue > 0f) return;
            _summaryLead = Loc.T("summary.stopped", "You have stopped.");
            _summaryTail = "";
            _summaryDue = Time.realtimeSinceStartup + 0.6f;
        }

        // What has been seen since the last summary, for a line that is being said anyway.
        internal static string TakeSeen()
        {
            if (_seen.Count == 0 || _inFight) return "";
            string text = " " + Loc.T("summary.seen", "Seen nearby: {0}.", Group(_seen, ". "));
            _seen.Clear();
            return text;
        }

        internal static void CampReached()
        {
            if (_summaryDue > 0f && _summaryLead.StartsWith("Combat over")) _summaryLead += " " + Loc.T("summary.camp", "You have stopped at a camp.");
            else _summaryLead = Loc.T("summary.camp", "You have stopped at a camp.");
            _summaryTail = "";
            _summaryDue = Time.realtimeSinceStartup + 0.5f;
            _campAt = Time.realtimeSinceStartup;
            _afterFight = false;
            // The hand is dealt one card at a time from here; "Drew N cards" follows it.
            Silent(2.5f);
        }

        internal static void CombatStarted()
        {
            _inFight = true;
            _combatStartAt = Time.realtimeSinceStartup;
            _combatLinePending = true;
            _summaryDue = 0f;
            Silent(CombatOpening);
            // Lines gathered a moment ago that the opening line replaces.
            _batch.RemoveAll(l => l.StartsWith("Nearby:") || l.StartsWith("Energy ") || l.StartsWith("New turn") || l.StartsWith("Your turn"));
        }

        internal static void CombatEnded()
        {
            _inFight = false;
            _combatLinePending = false;
            Announce(Loc.T("combat.over", "Combat over."), interrupt: false);
            // What was seen on the way in is said once every screen the fight leads to (the reward,
            // the map) has been dealt with, so that it is the last thing heard before walking on.
            _afterFight = true;
            _combatEndAt = Time.realtimeSinceStartup;
            _clearSince = Time.realtimeSinceStartup;
        }

        private static string Plural(string name)
        {
            if (name.EndsWith("s")) return name;
            if (name.EndsWith("x") || name.EndsWith("ch") || name.EndsWith("sh")) return name + "es";
            // Toasty, Terry: a consonant before the y.
            if (name.Length > 1 && name.EndsWith("y") && "aeiou".IndexOf(char.ToLowerInvariant(name[name.Length - 2])) < 0) return name.Substring(0, name.Length - 1) + "ies";
            return name + "s";
        }

        // "Door, Door, Chest" -> "2 Doors, Chest"
        internal static string Group(IEnumerable<string> things) => Group(things, ", ");

        // In a summary the things are separated by full stops: the walking voice runs commas together.
        internal static string Group(IEnumerable<string> things, string separator, bool countOne = false)
        {
            var names = new List<string>();
            var counts = new Dictionary<string, int>();
            foreach (string raw in things)
            {
                string name = raw.Trim();
                if (name.Length == 0) continue;
                if (!counts.ContainsKey(name)) { counts[name] = 0; names.Add(name); }
                counts[name]++;
            }
            var parts = new List<string>();
            foreach (string name in names)
                parts.Add(counts[name] == 1 ? (countOne ? "1 " + name : name) : counts[name] + " " + Plural(name));
            return string.Join(separator, parts);
        }

        private static string FirstSentence(string line)
        {
            int stop = line.IndexOf('.');
            return stop > 0 ? line.Substring(0, stop) : line;
        }

        private static void Summaries(S_ControllerFocus focus)
        {
            float now = Time.realtimeSinceStartup;
            if (InputBridge.MoveHeld) _lastWalkAt = now;

            // Wait a moment for the enemies to step in, and for the walk key to be let go so the
            // screen reader can say it; if the key stays down the walking voice says it instead.
            if (_combatLinePending && now >= _combatStartAt + 1.2f)
            {
                _combatLinePending = false;
                var enemies = new List<string>();
                var others = new List<string>();
                foreach (IControllerFocusTarget t in Navigator.NearbyInOrder(focus))
                {
                    string name = FirstSentence(Describe(t, out _));
                    if (t.Cast<Il2CppSystem.Object>().TryCast<V_Enemy>() != null) enemies.Add(name); else others.Add(name);
                }
                // Every name with its count ("2 Terrys, 1 Touchy"); the walking voice runs commas
                // together, so it gets full stops between them.
                string sep = InputBridge.MoveHeld && Speaker.HasDirect ? ". " : ", ";
                Announce(Loc.T("combat.started", "Combat started.")
                    + (enemies.Count > 0 ? " " + Loc.T("combat.enemies", "Enemies: {0}.", Group(enemies, sep, countOne: true)) : "")
                    + (others.Count > 0 ? " " + Loc.T("combat.also_here", "Also here: {0}.", Group(others, sep)) : ""), interrupt: false);
            }

            if (_summaryDue > 0f && now >= _summaryDue && !_inFight)
            {
                _summaryDue = 0f;
                if (_seen.Count == 0 && _summaryLead == Loc.T("summary.stopped", "You have stopped.")) return;
                string line = _summaryLead + (_seen.Count > 0 ? " " + Loc.T("summary.seen", "Seen nearby: {0}.", Group(_seen, ". ")) : "") + " " + _summaryTail;
                _seen.Clear();
                Announce(line.Trim(), interrupt: false);
            }

            if (_afterFight && !_inFight)
            {
                if (focus.Scope != null || focus.Mode == FocusMode.Suppressed) _clearSince = now;
                else if (now - _clearSince > 1.5f && now - _combatEndAt > 2.5f)
                {
                    _afterFight = false;
                    if (_seen.Count > 0) Announce(Loc.T("summary.seen", "Seen nearby: {0}.", Group(_seen, ". ")), interrupt: false);
                    _seen.Clear();
                }
            }
        }

        private static readonly List<string> _batch = new List<string>();
        private static float _batchAt;

        // Lower comes first. Within a tier, lines keep the order they happened in.
        private static int Tier(string line)
        {
            if (line.StartsWith("Combat") || line.StartsWith("You have stopped")) return 0;
            if (line.StartsWith("Nearby:")) return 1;
            if (line.StartsWith("Your turn") || line.StartsWith("Enemy turn") || line.StartsWith("New turn")) return 2;
            if (line.StartsWith("Drew ") || line.StartsWith("Added to hand")) return 3;
            if (line.StartsWith("Energy ")) return 4;
            return 5;
        }

        internal static void FlushBatch(bool force = false)
        {
            if (_batch.Count == 0) return;
            if (!force && Time.realtimeSinceStartup - _batchAt < 0.3f) return;
            // With no second voice, anything said while a walk key is down is lost: keep waiting.
            if (InputBridge.MoveHeld && !Speaker.HasDirect && !force) return;

            var lines = new List<string>(_batch);
            _batch.Clear();
            if (lines.Contains(Loc.T("turn.yours", "Your turn."))) lines.Remove(Loc.T("turn.new", "New turn."));

            // Several "Nearby:" lines become one list.
            var nearby = new List<string>();
            lines.RemoveAll(l =>
            {
                if (!l.StartsWith("Nearby: ")) return false;
                nearby.Add(l.Substring(8).TrimEnd('.'));
                return true;
            });
            if (nearby.Count > 0)
            {
                var names = new List<string>();
                var counts = new Dictionary<string, int>();
                foreach (string group in nearby)
                    foreach (string raw in group.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string name = raw.Trim();
                        if (!counts.ContainsKey(name)) { counts[name] = 0; names.Add(name); }
                        counts[name]++;
                    }
                var parts = new List<string>();
                foreach (string name in names)
                    parts.Add(counts[name] == 1 ? name : counts[name] + " " + Plural(name));
                lines.Add(Loc.T("nearby.list", "Nearby: {0}.", string.Join(", ", parts)));
            }

            var ordered = new List<string>();
            for (int tier = 0; tier <= 5; tier++)
                foreach (string l in lines) if (Tier(l) == tier) ordered.Add(l);

            foreach (string l in ordered) Remember(l);
            string all = string.Join(" ", ordered);
            _protectUntil = Time.realtimeSinceStartup + 1.5f + all.Length * 0.03f;
            _protectedLineQueued = false;
            // Each line is handed over on its own, so there is a pause between them.
            bool direct = InputBridge.MoveHeld && Speaker.HasDirect;
            foreach (string l in ordered)
            {
                Log((direct ? "[say-direct] " : "[say] ") + l);
                if (!Application.isFocused) continue;
                if (direct) Speaker.SayDirect(l); else Speaker.Say(l, false, asked: false);
            }
        }

        private static readonly List<string> _held = new List<string>();

        private static void FlushHeld() => FlushBatch();

        // For the buffers: spoken, logged, but not added to the message history (reading the
        // history should not fill it with itself).
        internal static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (InputBridge.MoveHeld && Speaker.HasDirect)
            {
                Log("[say-direct] " + text);
                if (Application.isFocused) Speaker.SayDirect(text, interrupt: true);
                return;
            }
            Log("[say] " + text);
            if (Application.isFocused) Speaker.Say(text, true);
        }

        private static readonly List<string> _history = new List<string>();

        private static void Remember(string text)
        {
            _history.Add(text);
            if (_history.Count > 200) _history.RemoveAt(0);
        }

        // Newest first.
        internal static List<string> History()
        {
            var list = new List<string>(_history);
            list.Reverse();
            if (list.Count == 0) list.Add(Loc.T("history.empty", "No messages yet"));
            return list;
        }

        // Speak the focused thing again, with or without its row name.
        internal static void Reannounce(bool rowName = true)
        {
            _lastFocus = IntPtr.Zero;
            if (rowName) _lastZone = "";
        }

        // The player has moved to a row the game knows nothing about (the status row).
        internal static void LeaveRow()
        {
            _lastZone = "status";
        }

        // Cards arrive in the hand from things other than offers (lighting a torch gives a match).
        private static readonly HashSet<IntPtr> _knownHeld = new HashSet<IntPtr>();
        private static bool _heldPrimed;
        private static float _offerSeenAt = -100f;

        private static void AnnounceHandChanges(S_ControllerFocus focus)
        {
            IM_Hand hand = null;
            try { hand = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null; } catch { }
            var held = hand != null ? hand.HeldCards : null;
            if (held == null) { _knownHeld.Clear(); _heldPrimed = false; return; }
            var now = new HashSet<IntPtr>();
            var added = new List<string>();
            for (int i = 0; i < held.Count; i++)
            {
                IM_CardInstance c = held[i];
                if (c == null) continue;
                now.Add(c.Pointer);
                if (_heldPrimed && !_knownHeld.Contains(c.Pointer)) added.Add(c.CardName);
            }
            _knownHeld.Clear();
            foreach (IntPtr ptr in now) _knownHeld.Add(ptr);
            _heldPrimed = true;
            if (added.Count > 0)
            {
                _pendingAdded.AddRange(added);
                _pendingAddedAt = Time.realtimeSinceStartup + 0.5f;
                // The game's focus hops along the cards as they are dealt; none of it is read.
                // An explore turn deals one card at a time, more slowly than a fight; keep quiet
                // from the first card on.
                if (focus.Scope == null) Silent(_pendingAddedAt + 0.6f - Time.realtimeSinceStartup);
            }
            if (_pendingAdded.Count == 0 || Time.realtimeSinceStartup < _pendingAddedAt) return;

            // Wait a moment first: a card taken from an offer becomes the focused card and is read
            // as such, and saying it twice helps nobody.
            Il2CppSystem.Object cur = focus.CurrentFocus;
            IV_CardInstance focused = cur != null ? cur.TryCast<IV_CardInstance>() : null;
            string focusedName = focused != null && focused.IModel != null ? focused.IModel.CardName : null;
            if (_pendingAdded.Count == 1) _pendingAdded.RemoveAll(n => n == focusedName);
            // Inside a screen (a shop, the soup pot, a card change) the hand is swapped for the
            // screen's own cards; the screen's line says what they are for.
            if (_pendingAdded.Count > 0 && focus.Scope != null)
            {
                Log("[hand] " + string.Join(", ", _pendingAdded));
                _pendingAdded.Clear();
            }
            if (_pendingAdded.Count > 0)
            {
                // A whole hand arriving is a draw; the player reads the cards themselves.
                Announce(_pendingAdded.Count > 1 ? Loc.T("hand.drew", "Drew {0} cards.", _pendingAdded.Count) : Loc.T("hand.added", "Added to hand: {0}.", _pendingAdded[0]), interrupt: false, holdWhileWalking: true);
                _protectUntil = Time.realtimeSinceStartup + 1.5f;
                _protectedLineQueued = false;
            }
            _pendingAdded.Clear();
        }

        private static readonly List<string> _pendingAdded = new List<string>();
        private static float _pendingAddedAt;

        // The lines of the item buffer: everything worth knowing about one thing, one fact per line.
        internal static List<string> DetailLines(Il2CppSystem.Object item)
        {
            var lines = new List<string>();
            if (item == null) return lines;
            try
            {
                IV_CardInstance card = item.TryCast<IV_CardInstance>();
                if (card != null)
                {
                    IM_CardInstance model = card.IModel;
                    ICardData data = model != null ? model.IData : null;
                    lines.Add(Clean(model != null ? model.CardName : Loc.T("card.unnamed", "Card")));
                    GameplayCardData gameplay = data != null ? data.TryCast<GameplayCardData>() : null;
                    if (gameplay != null) lines.Add(Loc.T("card.cost_energy", "Cost {0} energy", gameplay.EnergyUse));
                    if (data != null) lines.Add(Loc.T("card.class_deck", "{0}, {1} deck", Humanize(data.ClassType.ToString()), Humanize(data.DeckType.ToString()).ToLowerInvariant()));
                    string uses = CardUses(gameplay);
                    if (uses.Length > 0) lines.Add(uses);
                    string desc = data != null ? data.SingleDescription : null;
                    foreach (string sentence in Sentences(Clean(desc))) lines.Add(sentence);
                    foreach (string keyword in KeywordLines(desc)) lines.Add(keyword);
                    string label = CardLabel(item, out string labelTip);
                    if (label.Length > 0) lines.Add(label + (labelTip.Length > 0 && !labelTip.Equals(label, StringComparison.OrdinalIgnoreCase) ? ": " + labelTip : ""));
                    AddLooks(lines, "card", model != null ? model.CardName : null);
                    return lines;
                }
                V_Enemy enemy = item.TryCast<V_Enemy>();
                if (enemy != null && Safe(() => enemy.Model) != null)
                {
                    M_Enemy m = enemy.Model;
                    lines.Add(EnemyName(enemy, m));
                    lines.Add(EnemyHealth(m));
                    foreach (string intent in EnemyIntents(m, brief: false)) lines.Add(Loc.T("enemy.will", "Will {0}", intent));
                    if (lines.Count == 2) lines.Add(Loc.T("enemy.no_intent", "No intent showing"));
                    foreach (string status in EnemyStatuses(m)) lines.Add(status + StatusTip(status));
                    AddLooks(lines, "enemy", EnemyName(enemy, m));
                    return lines;
                }
                string whole = Describe(item, out _);
                foreach (string sentence in Sentences(whole)) lines.Add(sentence);
                Component generic = item.TryCast<Component>();
                if (generic != null && item.TryCast<IControllerFocusTarget>() == null)
                {
                    // A button or panel: everything written on it, not just the first few words.
                    lines.Clear();
                    foreach (TMP_Text t in generic.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (t == null || string.IsNullOrWhiteSpace(t.text)) continue;
                        // One line of the buffer per line of text, so long blocks (the credits) can be stepped through.
                        foreach (string raw in t.text.Split(new char[] { (char)10 }))
                        {
                            string text = Clean(raw);
                            if (text.Length > 0 && !lines.Contains(text)) lines.Add(text);
                        }
                    }
                    if (lines.Count == 0) lines.Add(whole);
                }
                // Appearance lines are for things in the world, not for buttons and sliders.
                // Not the card-change screen and not a button label in capitals ("MAKE A SOUP"): those have no picture of their own.
                if (lines.Count > 0 && item.TryCast<IControllerFocusTarget>() != null && item.TryCast<UnityEngine.UI.Selectable>() == null)
                {
                    string thing = lines[0].Split(',')[0];
                    if (item.TryCast<ModifyCardEncounterUI>() == null && thing != thing.ToUpperInvariant()) AddLooks(lines, "thing", thing);
                }
                if (lines.Count == 1) lines.Add(Loc.T("detail.nothing_more", "The game shows nothing more about this"));
                // A thing lying in the world shows a sighted player nothing but itself until it is
                // opened, so there is nothing more to read here; say how to find out.
                if (item.TryCast<EncounterStartWorldObject>() != null) lines.Add(Loc.T("detail.not_opened", "Not opened yet. Enter opens it"));
            }
            catch (Exception ex) { lines.Add(Loc.T("detail.error", "Could not read this: {0}", ex.Message)); }
            return lines;
        }

        // "Vulnerable 3" -> ": WILL RECEIVE 150% MORE DAMAGE." from the game's keyword text.
        internal static string StatusTip(string status)
        {
            try
            {
                string name = status, number = "";
                int space = status.LastIndexOf(' ');
                if (space > 0 && int.TryParse(status.Substring(space + 1), out _)) { name = status.Substring(0, space); number = status.Substring(space + 1); }
                string id = name.Trim().ToUpperInvariant().Replace(' ', '_');
                foreach (string key in new[] { "Enemy_TOOLTIP_STATUS_" + id + (number.Length > 0 ? "[[N]=" + number + "]" : ""), "Enemy_TOOLTIP_STATUS_" + id, "Card_TOOLTIP_WORD_" + id })
                {
                    if (KeywordTooltipRegistry.TryGetTooltipFromLinkID(key, out string tip) && !string.IsNullOrWhiteSpace(tip))
                    {
                        string text = Clean(tip);
                        // The tooltip usually begins with the status's own name; do not say it twice.
                        int colon = text.IndexOf(':');
                        if (colon > 0 && colon < name.Length + 4) text = text.Substring(colon + 1).Trim();
                        return ": " + text;
                    }
                }
            }
            catch (Exception ex) { Log("[status] " + status + ": " + ex.Message); }
            return "";
        }

        private static void AddLooks(List<string> lines, string kind, string name)
        {
            string looks = AltText.Lookup(kind, name);
            if (!string.IsNullOrWhiteSpace(looks)) lines.Add(Loc.T("detail.looks", "Looks: {0}", looks));
        }

        private static IEnumerable<string> Sentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            foreach (string part in text.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries))
            {
                string t = part.Trim().TrimEnd('.');
                if (t.Length > 0) yield return t;
            }
        }

        // Card text marks its keywords as links; the game holds an explanation for each link.
        private static List<string> KeywordLines(string richText)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(richText)) return lines;
            var seen = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(richText, "<link=([^>]+)>(.*?)</link>"))
            {
                string key = m.Groups[1].Value;
                string word = Clean(m.Groups[2].Value);
                if (!seen.Add(key)) continue;
                try
                {
                    if (KeywordTooltipRegistry.TryGetTooltipFromLinkID(key, out string tip) && !string.IsNullOrWhiteSpace(tip))
                        lines.Add(word + ": " + Clean(tip));
                }
                catch (Exception ex) { Log("[keyword] " + key + ": " + ex.Message); }
            }
            return lines;
        }

        private static void DumpAsset(S_InputManager input)
        {
            try
            {
                BasicInputSource src = input._inputSourceBehaviour.TryCast<BasicInputSource>();
                if (src == null) { Log("[asset] input source is not BasicInputSource"); return; }
                var maps = src._actions.actionMaps;
                for (int m = 0; m < maps.Count; m++)
                {
                    var map = maps[m];
                    Log("[map] " + map.name + " enabled=" + map.enabled);
                    var actions = map.actions;
                    for (int a = 0; a < actions.Count; a++)
                    {
                        var action = actions[a];
                        var sb = new StringBuilder();
                        var bindings = action.bindings;
                        for (int b = 0; b < bindings.Count; b++)
                        {
                            var bind = bindings[b];
                            sb.Append(" [" + bind.path + " {" + bind.groups + "}" + (bind.isPartOfComposite ? " part:" + bind.name : "") + "]");
                        }
                        Log("[asset] " + map.name + "/" + action.name + ":" + sb);
                    }
                }
            }
            catch (Exception ex) { Log("[asset] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static void ForcePad(S_InputManager input)
        {
            try
            {
                BasicInputSource src = input._inputSourceBehaviour.TryCast<BasicInputSource>();
                src.SetInputContext(InputDeviceType.Gamepad, GamepadFamily.Xbox);
                Log("[experiment] SetInputContext(Gamepad, Xbox) called");
            }
            catch (Exception ex) { Log("[experiment] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static void DumpBindings(S_InputManager input)
        {
            foreach (InputActionId id in Enum.GetValues(typeof(InputActionId)))
            {
                try
                {
                    string display = input.GetBindingDisplayText(id);
                    input.TryGetBindingControlPath(id, out string path);
                    Log("[binding] " + id + " = " + display + " | " + path);
                }
                catch (Exception ex)
                {
                    Log("[binding] " + id + " failed: " + ex.Message);
                }
            }
        }

        // The card on screen that shows this card.
        internal static V_GameplayCardInstance CardViewOf(IntPtr model)
        {
            try
            {
                foreach (V_GameplayCardInstance v in UnityEngine.Object.FindObjectsOfType<V_GameplayCardInstance>())
                {
                    IV_CardInstance iv = v != null ? v.Cast<Il2CppSystem.Object>().TryCast<IV_CardInstance>() : null;
                    if (iv != null && iv.IModel != null && iv.IModel.Pointer == model) return v;
                }
            }
            catch (Exception ex) { Log("[card target] " + ex.Message); }
            return null;
        }

        // Where the focused card is among the cards the one being played can go on: the game keeps
        // a mask over the hand of the cards that can be chosen.
        private static void CardTargetPosition(S_ControllerFocus focus, out int index, out int count)
        {
            index = 0; count = 0;
            try
            {
                TargetFocusResolver resolver = focus.TargetResolver;
                CardTargetSet set = resolver != null ? resolver._cardTargets : null;
                if (set == null || set._mask == null) return;
                int at = resolver._cardIndex;
                for (int i = 0; i < set._mask.Length && i < Math.Max(set._count, 0); i++)
                {
                    if (!set._mask[i]) continue;
                    count++;
                    if (i <= at) index = count;
                }
                if (_cardTargetLogged != at) { _cardTargetLogged = at; Log("[card target] index " + at + " -> " + index + " of " + count + " (legal " + set.LegalCount + ")"); }
            }
            catch (Exception ex) { Log("[card target] position: " + ex.Message); }
        }

        private static int _cardTargetLogged = -1;

        // The card picked on a card-change screen, in case the game has not recorded it yet.
        internal static string ModifyPicked;

        // What a card change gives, as its screen prints it ("IF FATAL, DRAW 2 CARDS"), and, when it
        // makes a card, what that card does. The heading ("SELECT A CARD TO GAIN:") is left out.
        internal static string ModifyEffect(ModifyCardEncounterUI modify, bool withMadeCards)
        {
            var parts = new List<string>();
            var made = new List<string>();
            try
            {
                foreach (TMP_Text t in modify.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (t == null || string.IsNullOrWhiteSpace(t.text)) continue;
                    if (t.GetComponentInParent<UnityEngine.UI.Selectable>() != null) continue;
                    if (t.GetComponentInParent<V_GameplayCardInstance>() != null) continue;
                    string text = Clean(t.text);
                    if (text.Length == 0 || text.EndsWith(":") || parts.Contains(text)) continue;
                    parts.Add(text);
                    if (!withMadeCards) continue;
                    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(t.text, "<link=mC>(.*?)</link>"))
                    {
                        string name = Clean(m.Groups[1].Value);
                        ICardData card = null;
                        try
                        {
                            if (!modify.TryGetLinkPreviewCard(m.Groups[1].Value, out card)) card = null;
                            if (card == null && !modify.TryGetLinkPreviewCard(name, out card)) card = null;
                        }
                        catch (Exception ex) { Log("[modify] preview " + name + ": " + ex.Message); }
                        string desc = card != null ? Clean(Safe(() => card.SingleDescription)) : "";
                        Log("[modify] made card " + name + ": " + (card != null ? desc : "no preview"));
                        if (desc.Length > 0) made.Add(Loc.T("modify.made_card", "{0} does: {1}", System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant()), desc.TrimEnd('.')));
                    }
                }
            }
            catch (Exception ex) { Log("[modify] effect: " + ex.Message); }
            string effect = string.Join(". ", parts.Select(p => p.TrimEnd('.')));
            if (made.Count > 0) effect += ". " + string.Join(". ", made);
            return effect;
        }

        // The small label printed under a card's title, when the card shows one, and what its
        // tooltip says about it.
        internal static string CardLabel(Il2CppSystem.Object card) => CardLabel(card, out _);

        internal static string CardLabel(Il2CppSystem.Object card, out string tooltip)
        {
            tooltip = "";
            try
            {
                V_GameplayCardInstance v = card != null ? card.TryCast<V_GameplayCardInstance>() : null;
                if (v == null || v._usesRoot == null || !v._usesRoot.gameObject.activeInHierarchy || v._usesText == null) return "";
                string text = Clean(v._usesText.text);
                if (v._usesTooltip != null) tooltip = Clean(Safe(() => v._usesTooltip.GetTooltip()) ?? "");
                if (text.Length == 0) return "";
                // Shown in capitals on the card; said in ordinary case.
                return text.Length > 1 ? text.Substring(0, 1).ToUpperInvariant() + text.Substring(1).ToLowerInvariant() : text;
            }
            catch { return ""; }
        }

        internal static string TypeName(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase o)
        {
            if (o == null) return "null";
            try { return o.Cast<Il2CppSystem.Object>().GetIl2CppType().FullName; } catch { return "?"; }
        }

        private static string PathOf(Transform t)
        {
            var sb = new StringBuilder();
            int depth = 0;
            while (t != null && depth++ < 6)
            {
                sb.Insert(0, "/" + t.name);
                t = t.parent;
            }
            return sb.ToString();
        }

        // TextMeshPro rich-text tags (<color=...>, <sprite ...>) are not for speaking.
        internal static string StripTags(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('<') < 0) return s;
            var sb = new StringBuilder(s.Length);
            bool inTag = false;
            foreach (char c in s)
            {
                if (c == '<') inTag = true;
                else if (c == '>' && inTag) inTag = false;
                else if (!inTag) sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
