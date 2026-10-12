using System;
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
        private static string _lastState;
        private static bool _bindingsDumped;
        private static IntPtr _lastSelected;
        private static string _lastZone = "";
        private static float _protectUntil;
        private static readonly HashSet<IntPtr> _knownNearby = new HashSet<IntPtr>();
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
                Navigator.Tick(focus);
                Review.Tick(focus);
                AnnounceArrivals(focus);
                AnnounceHandChanges(focus);
                Hud.AnnounceChanges(focus);
                FlushHeld();

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
                if (ptr == _lastFocus) return;
                _lastFocus = ptr;
                if (current == null)
                {
                    Log("[focus] none");
                    _lastZone = "";
                    return;
                }

                string line = Describe(current, out string detail);
                Log("[focus] " + TypeName(current) + " | " + detail);
                if (Navigator.InStatusRow) return;

                // Name the row the player has just moved into, so they know what they are among:
                // their own cards, things in the world, valid targets for a cast, or an offer.
                string zone = ZoneOf(focus, current, mode);
                if (zone == "offer") _offerSeenAt = Time.realtimeSinceStartup;
                string spoken = line;
                if (zone != _lastZone)
                {
                    _lastZone = zone;
                    if (zone == "hand") spoken = "Your hand, " + Count(HeldCount(focus), "card") + ". " + line;
                    else if (zone == "nearby" && focus.InCombat) spoken = "Enemies, " + (focus.WorldFocusCandidates != null ? focus.WorldFocusCandidates.Count : 0) + ". " + line;
                    else if (zone == "nearby") spoken = "Nearby, " + Count(focus.WorldFocusCandidates != null ? focus.WorldFocusCandidates.Count : 0, "thing") + ". " + line;
                    else if (zone == "targets") spoken = "Choose a target. " + line;
                    else if (zone == "offer" && current.TryCast<IV_CardInstance>() != null) spoken = "Offered: " + line + " Enter takes it.";
                }
                // Let a line that announces an arrival finish before the first focus line follows it.
                Announce(spoken, interrupt: Time.realtimeSinceStartup > _protectUntil);
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

        private static string Count(int n, string noun) => n + " " + noun + (n == 1 ? "" : "s");

        private static string ZoneOf(S_ControllerFocus focus, Il2CppSystem.Object current, FocusMode mode)
        {
            if (mode == FocusMode.Target) return "targets";
            bool isCard = current.TryCast<IV_CardInstance>() != null;
            string scope = TypeName(focus.Scope);
            if (scope.Contains("LootAll") || scope.Contains("CardChoice")) return "offer";
            if (isCard) return "hand";
            if (current.TryCast<IControllerFocusTarget>() != null && current.TryCast<UnityEngine.UI.Selectable>() == null) return "nearby";
            return "";
        }

        // Walking stops without a word when the player reaches something. Say what has come into reach.
        private static void AnnounceArrivals(S_ControllerFocus focus)
        {
            WorldFocusCandidates world = focus.WorldFocusCandidates;
            int n = world != null ? world.Count : 0;
            if (n == 0) { _knownNearby.Clear(); return; }
            var fresh = new List<string>();
            var now = new HashSet<IntPtr>();
            for (int i = 0; i < n; i++)
            {
                IControllerFocusTarget t = world.At(i);
                if (t == null) continue;
                now.Add(t.Pointer);
                if (_knownNearby.Contains(t.Pointer)) continue;
                string name = Describe(t, out _);
                int stop = name.IndexOf('.');
                fresh.Add(stop > 0 ? name.Substring(0, stop) : name);
            }
            _knownNearby.Clear();
            foreach (IntPtr ptr in now) _knownNearby.Add(ptr);
            if (fresh.Count == 0 || focus.Mode == FocusMode.Target) return;
            Cue.Play(Cue.Arrival);
            Announce("Nearby: " + string.Join(", ", fresh) + ".", interrupt: false, holdWhileWalking: true);
            _protectUntil = Time.realtimeSinceStartup + 1.5f;
        }

        // Set by Main when the title screen loads: the time after which the one-off experiments run.
        internal static float ArmedAt;

        // A short spoken line for any focusable thing, plus a longer diagnostic string for the log.
        internal static string Describe(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase item, out string detail)
        {
            detail = "";
            if (item == null) return "nothing";
            Il2CppSystem.Object obj = item.Cast<Il2CppSystem.Object>();

            IV_CardInstance card = obj.TryCast<IV_CardInstance>();
            if (card != null)
            {
                IM_CardInstance model = card.IModel;
                ICardData data = model != null ? model.IData : null;
                string name = model != null ? model.CardName : null;
                string desc = data != null ? data.SingleDescription : null;
                GameplayCardData gameplay = data != null ? data.TryCast<GameplayCardData>() : null;
                string cost = gameplay != null ? ", cost " + gameplay.EnergyUse : "";
                string uses = CardUses(gameplay);
                detail = "card name=" + name + " deck=" + (data != null ? data.DeckType.ToString() : "?") + cost + " uses=" + uses + " desc=" + desc;
                return Clean(name + cost + (uses.Length > 0 ? ". " + uses : "") + (string.IsNullOrEmpty(desc) ? "" : ". " + desc));
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
                return Clean("Door" + (open ? ", open" : "") + (string.IsNullOrWhiteSpace(sign) ? "" : ". " + sign) + (accepts.Length > 0 ? ". " + accepts : ""));
            }

            V_Enemy enemy = obj.TryCast<V_Enemy>();
            if (enemy != null)
            {
                M_Enemy m = Safe(() => enemy.Model);
                detail = "enemy object=" + enemy.gameObject.name;
                if (m == null) return Humanize(enemy.gameObject.name);
                string line = EnemyName(enemy, m) + ". " + EnemyHealth(m);
                string intent = string.Join(", ", EnemyIntents(m, brief: true));
                if (intent.Length > 0) line += ". Intends " + intent;
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
                detail = "hazard name=" + name + " object=" + hazard.gameObject.name + " accepts=" + accepts;
                return Clean(name + (accepts.Length > 0 ? ". " + accepts : ""));
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

                return Clean(texts.Count > 0 ? string.Join(", ", texts.GetRange(0, Math.Min(3, texts.Count))) : Humanize(comp.gameObject.name));
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
            return "Health " + h.CurrentHealth + " of " + h.MaxHealth + (h.Armour > 0 ? ", armour " + h.Armour : "");
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
                    if (label.Length == 0 || label == "Nothing") continue;
                    string text = null;
                    if (!brief) text = Safe(() => pair.Intent.GetIntentToolTipText("ShroomAccess", pair, m.StatusEffects));
                    if (string.IsNullOrWhiteSpace(text)) text = label + (pair.ModifiedNumber != 0 ? " " + pair.ModifiedNumber : "");
                    list.Add(Clean(text));
                }
            }
            catch (Exception ex) { Log("[enemy] intents: " + ex.Message); }
            return list;
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

        private static T Safe<T>(Func<T> read)
        {
            try { return read(); } catch { return default; }
        }

        // The card actions a world object will accept ("Dig", "Unlock"), as the game names them.
        private static string Accepts(Il2CppSystem.Collections.Generic.List<OrableWithMeta> list)
        {
            if (list == null) return "";
            var names = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                OrableWithMeta item = list[i];
                if (item == null) continue;
                if (item.IsMysterious) { names.Add("something unknown"); continue; }
                OrableCardIntent intent = item.OrableIntent;
                if (intent == null) continue;
                string label = intent.EnvironmentPromptTooltip;
                if (string.IsNullOrWhiteSpace(label)) label = Humanize(intent.name);
                if (!names.Contains(label)) names.Add(label);
            }
            return string.Join(" ", names);
        }

        // "ShovelChoiceEncounterWorldObject" -> "Shovel Choice"; "UI_PathSeletion_Path_03" -> "Path 03".
        internal static string Humanize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = raw.Replace("(Clone)", "").Replace("EncounterWorldObject", "").Replace("WorldObject", "")
                .Replace(" Encounter", "").Replace("PT ", "").Replace("Choice", "")
                .Replace("UI_PathSeletion_", "").Replace("RawImage_", "").Replace("_BG", "").Replace("_", " ").Trim();
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(s[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        // Rich-text tags out, whitespace tidy, no doubled full stops.
        internal static string Clean(string s)
        {
            // The game's prompt strings carry a literal backslash-n as well as real line breaks.
            s = StripTags(s ?? "").Replace("\\n", " ").Replace("\n", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
            while (s.Contains(". .")) s = s.Replace(". .", ".");
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
        internal static void Announce(string text, bool interrupt = true, bool holdWhileWalking = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (holdWhileWalking && InputBridge.MoveHeld && Speaker.HasDirect)
            {
                // Live, through the voice the held key cannot silence.
                Log("[say-direct] " + text);
                if (Application.isFocused) Speaker.SayDirect(text, interrupt: false);
                return;
            }
            if (holdWhileWalking && InputBridge.MoveHeld)
            {
                // Several arrivals during one walk read as one list.
                int last = _held.Count - 1;
                if (last >= 0 && text.StartsWith("Nearby: ") && _held[last].StartsWith("Nearby: "))
                    _held[last] = _held[last].TrimEnd('.') + ", " + text.Substring(8);
                else if (!_held.Contains(text)) _held.Add(text);
                Log("[held] " + text);
                return;
            }
            Log("[say] " + text);
            if (Application.isFocused) Speaker.Say(text, interrupt);
        }

        private static readonly List<string> _held = new List<string>();

        private static void FlushHeld()
        {
            if (_held.Count == 0 || InputBridge.MoveHeld) return;
            string all = string.Join(" ", _held);
            _held.Clear();
            Announce(all, interrupt: false);
            _protectUntil = Time.realtimeSinceStartup + 1.5f;
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
                _pendingAddedAt = Time.realtimeSinceStartup + 2f;
            }
            if (_pendingAdded.Count == 0 || Time.realtimeSinceStartup < _pendingAddedAt) return;

            // Wait a moment first: a card taken from an offer becomes the focused card and is read
            // as such, and saying it twice helps nobody.
            Il2CppSystem.Object cur = focus.CurrentFocus;
            IV_CardInstance focused = cur != null ? cur.TryCast<IV_CardInstance>() : null;
            string focusedName = focused != null && focused.IModel != null ? focused.IModel.CardName : null;
            _pendingAdded.RemoveAll(n => n == focusedName);
            if (_pendingAdded.Count > 0)
            {
                Announce("Added to hand: " + string.Join(", ", _pendingAdded) + ".", interrupt: false, holdWhileWalking: true);
                _protectUntil = Time.realtimeSinceStartup + 1.5f;
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
                    lines.Add(Clean(model != null ? model.CardName : "Card"));
                    GameplayCardData gameplay = data != null ? data.TryCast<GameplayCardData>() : null;
                    if (gameplay != null) lines.Add("Cost " + gameplay.EnergyUse + " energy");
                    if (data != null) lines.Add(Humanize(data.ClassType.ToString()) + ", " + Humanize(data.DeckType.ToString()).ToLowerInvariant() + " deck");
                    string uses = CardUses(gameplay);
                    if (uses.Length > 0) lines.Add(uses);
                    string desc = data != null ? data.SingleDescription : null;
                    foreach (string sentence in Sentences(Clean(desc))) lines.Add(sentence);
                    foreach (string keyword in KeywordLines(desc)) lines.Add(keyword);
                    AddLooks(lines, "card", model != null ? model.CardName : null);
                    return lines;
                }
                V_Enemy enemy = item.TryCast<V_Enemy>();
                if (enemy != null && Safe(() => enemy.Model) != null)
                {
                    M_Enemy m = enemy.Model;
                    lines.Add(EnemyName(enemy, m));
                    lines.Add(EnemyHealth(m));
                    foreach (string intent in EnemyIntents(m, brief: false)) lines.Add("Intends: " + intent);
                    if (lines.Count == 2) lines.Add("No intent showing");
                    foreach (string status in EnemyStatuses(m)) lines.Add(status);
                    AddLooks(lines, "enemy", EnemyName(enemy, m));
                    return lines;
                }
                string whole = Describe(item, out _);
                foreach (string sentence in Sentences(whole)) lines.Add(sentence);
                if (lines.Count > 0) AddLooks(lines, "thing", lines[0].Split(',')[0]);
                // A thing lying in the world shows a sighted player nothing but itself until it is
                // opened, so there is nothing more to read here; say how to find out.
                if (item.TryCast<EncounterStartWorldObject>() != null) lines.Add("Not opened yet. Enter opens it");
            }
            catch (Exception ex) { lines.Add("Could not read this: " + ex.Message); }
            return lines;
        }

        private static void AddLooks(List<string> lines, string kind, string name)
        {
            string looks = AltText.Lookup(kind, name);
            if (!string.IsNullOrWhiteSpace(looks)) lines.Add("Looks: " + looks);
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

        private static string TypeName(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase o)
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
