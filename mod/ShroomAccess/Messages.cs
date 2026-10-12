using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppControllerNav;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShroomAccess
{
    // Text the game puts on screen for a sighted player to read in passing: the short prompts it
    // flashes up, and the heading and question on a screen as it opens ("A Gift From Below. Choose
    // a Foresight", or what a Yes / No box is asking).
    internal static class Messages
    {
        internal static Action<string> Log = _ => { };

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(GeneralPromptText), "Say");
                if (target == null) { Log("Hook missing: GeneralPromptText.Say"); return; }
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(Messages).GetMethod(nameof(PromptPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                Log("Hooked GeneralPromptText.Say");
            }
            catch (Exception ex) { Log("Hook failed: GeneralPromptText.Say: " + ex.Message); }
        }

        private static readonly List<string> _pending = new List<string>();

        private static void PromptPostfix(string text)
        {
            if (!string.IsNullOrWhiteSpace(text)) _pending.Add(text);
        }

        private static IntPtr _lastScope;
        private static float _shopSaidAt = -100f;
        private static float _liveNext;
        private static IntPtr _liveScope;
        private static List<string> _liveText = new List<string>();
        private static string _lastScreenText = "";
        private static float _lastScreenAt;
        private static float _offerDue;
        private static float _offerGiveUp;
        private static bool _offerWaitSaid;
        private static string _offerKind = "";
        private static Component _offerRoot;
        private static bool _wasTarget;

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                foreach (string text in _pending) FocusProbe.Announce(FocusProbe.Clean(text), interrupt: false, holdWhileWalking: true);
                _pending.Clear();

                IFocusScope scope = focus.Scope;
                IntPtr ptr = scope != null ? scope.Pointer : IntPtr.Zero;
                if (ptr != _lastScope)
                {
                    _lastScope = ptr;
                    if (scope != null) AnnounceScreen(scope);
                    if (scope != null && scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name == "BowlShopEncounterUI" && Time.realtimeSinceStartup - _shopSaidAt > 30f && (_shopSaidAt = Time.realtimeSinceStartup) > 0f)
                        FocusProbe.Announce("A shop. Cards are paid for with food. Enter on a card to choose it, then Enter on a food card in your hand and Enter on the bowl to pay.", interrupt: false);
                    if (scope != null && scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name == "S_MapReminder")
                        FocusProbe.Announce("The map is open. Reading the map is not built yet. Press M to put it away.", interrupt: false);
                    string kind = scope != null ? scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name : "";
                    if (kind.Contains("LootAll") || kind.Contains("CardChoice") || kind.Contains("ForesightChoice"))
                    {
                        // The cards are dealt out one after another and the game's focus follows
                        // each; wait for them, then say once what is on offer.
                        _offerKind = kind;
                        _offerRoot = scope.Cast<Il2CppSystem.Object>().TryCast<Component>();
                        _offerDue = Time.realtimeSinceStartup + 1.4f;
                        _offerGiveUp = Time.realtimeSinceStartup + 25f;
                        _offerWaitSaid = false;
                        FocusProbe.SuppressFocusUntil = _offerDue;
                    }
                }

                if (_offerDue > 0f && Time.realtimeSinceStartup >= _offerDue)
                {
                    _offerDue = 0f;
                    var names = new List<string>();
                    if (_offerRoot != null)
                    {
                        foreach (V_GameplayCardInstance c in _offerRoot.GetComponentsInChildren<V_GameplayCardInstance>(false))
                        {
                            IV_CardInstance iv = c != null ? c.Cast<Il2CppSystem.Object>().TryCast<IV_CardInstance>() : null;
                            if (iv != null && iv.IModel != null) names.Add(iv.IModel.CardName);
                        }
                        foreach (V_ForesightCardInstance c in _offerRoot.GetComponentsInChildren<V_ForesightCardInstance>(false))
                        {
                            IV_CardInstance iv = c != null ? c.Cast<Il2CppSystem.Object>().TryCast<IV_CardInstance>() : null;
                            if (iv != null && iv.IModel != null) names.Add(iv.IModel.CardName);
                        }
                    }
                    // A chest can take several seconds to rise and deal. Keep looking until the cards
                    // are there, and tell the player to wait, or the screen seems dead.
                    if (names.Count == 0 && focus.Scope != null && Time.realtimeSinceStartup < _offerGiveUp)
                    {
                        if (!_offerWaitSaid)
                        {
                            _offerWaitSaid = true;
                            FocusProbe.Announce("Wait: the cards are still being dealt. Stay in the game window until they are read.", interrupt: false);
                        }
                        _offerDue = Time.realtimeSinceStartup + 0.5f;
                        FocusProbe.SuppressFocusUntil = _offerDue;
                        return;
                    }
                    // One card on offer is read by its own focus line; only a spread needs introducing.
                    if (names.Count > 1)
                    {
                        string lead = _offerKind.Contains("LootAll")
                            ? "You are offered all of these, " + names.Count + " cards: "
                            : "Choose one of " + names.Count + ": ";
                        string how = _offerKind.Contains("LootAll")
                            ? ". Left and Right read each card. Enter, or Down to Take, takes them all."
                            : ". Left and Right to look, Enter to choose.";
                        FocusProbe.Announce(lead + string.Join(", ", names) + how, interrupt: false);
                    }
                    FocusProbe.Reannounce();
                }

                if (scope != null && Time.realtimeSinceStartup >= _liveNext)
                {
                    _liveNext = Time.realtimeSinceStartup + 0.6f;
                    List<string> now = ScreenText(scope, 6);
                    if (_liveScope == ptr)
                    {
                        var fresh = new List<string>();
                        foreach (string part in now) if (!_liveText.Contains(part)) fresh.Add(part);
                        if (fresh.Count > 0 && fresh.Count <= 3)
                        {
                            Log("[screen-change] " + string.Join(" | ", fresh));
                            FocusProbe.Announce(string.Join(". ", fresh).Replace("!.", "!").Replace("?.", "?"), interrupt: false);
                        }
                    }
                    _liveScope = ptr;
                    _liveText = now;
                }

                // A card that needs no target (it acts on you) still waits for a second Enter, with
                // nothing focused; say so, or the first Enter seems to do nothing.
                bool target = focus.Mode == FocusMode.Target;
                if (target && !_wasTarget && focus.CurrentFocus == null)
                    FocusProbe.Announce("This card acts on you. Enter to play it, Backspace to cancel.", interrupt: false);
                _wasTarget = target;
            }
            catch (Exception ex) { Log("[messages] " + ex.GetType().Name + ": " + ex.Message); }
        }

        // A label that belongs to a control beside it (a slider's name) is read with that control.
        private static bool IsControlLabel(Transform t)
        {
            Transform parent = t.parent;
            if (parent == null) return false;
            foreach (Selectable sibling in parent.GetComponentsInChildren<Selectable>(false))
                if (sibling != null) return true;
            return false;
        }

        // The words on a screen that are not on its buttons or cards: headings, questions, notes.
        private static List<string> ScreenText(IFocusScope scope, int limit)
        {
            var parts = new List<string>();
            Component root = scope.Cast<Il2CppSystem.Object>().TryCast<Component>();
            if (root == null) return parts;
            foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (t == null || t.GetComponentInParent<Selectable>() != null) continue;
                if (IsControlLabel(t.transform)) continue;
                if (t.GetComponentInParent<V_GameplayCardInstance>() != null || t.GetComponentInParent<V_ForesightCardInstance>() != null) continue;
                string text = FocusProbe.Clean(t.text);
                if (text.Length < 2 || text.Length > 220 || parts.Contains(text)) continue;
                // Price tags belong to the cards; the pause menu prints build and session identifiers.
                if (text.StartsWith("COST") || text.StartsWith("UserID") || text.StartsWith("SessionID") || text.StartsWith("V 0.") || text.StartsWith("V 1.")) continue;
                parts.Add(text);
                if (parts.Count == limit) break;
            }
            return parts;
        }

        private static void AnnounceScreen(IFocusScope scope)
        {
            List<string> parts = ScreenText(scope, 4);
            if (parts.Count == 0) return;
            // Coming back to a screen (from the pause menu, or from a step inside it) is not news.
            string joined = string.Join(" | ", parts);
            if (joined == _lastScreenText && Time.realtimeSinceStartup - _lastScreenAt < 30f) return;
            _lastScreenText = joined;
            _lastScreenAt = Time.realtimeSinceStartup;
            Log("[screen] " + string.Join(" | ", parts));
            var sb = new System.Text.StringBuilder();
            foreach (string part in parts)
            {
                sb.Append(part);
                char last = part[part.Length - 1];
                sb.Append(last == '.' || last == '?' || last == '!' ? " " : ". ");
            }
            FocusProbe.Announce(sb.ToString().Trim(), interrupt: false);
        }
    }
}
