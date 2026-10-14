using System;
using System.Linq;
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
        private static float _introDue, _introGiveUp;
        private static IntPtr _introScope;
        private static float _offerDue;
        private static float _offerGiveUp;
        private static bool _offerWaitSaid;
        private static string _offerKind = "";
        private static Component _offerRoot;
        private static bool _wasTarget;

        private static IntPtr _introSaidFor;
        private static float _screenDue;
        private static IntPtr _screenScope;

        // Screen text said a moment ago is not said again by another watcher.
        private static readonly Dictionary<string, float> _saidAt = new Dictionary<string, float>();

        private static bool SaidLately(string part)
        {
            float now = Time.realtimeSinceStartup;
            bool lately = _saidAt.TryGetValue(part, out float at) && now - at < 4f;
            _saidAt[part] = now;
            if (_saidAt.Count > 200) _saidAt.Clear();
            return lately;
        }

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
                    if (scope == null) _introSaidFor = IntPtr.Zero;
                    // A screen that is up for a blink on the way to another is not read: wait and see
                    // that it is still there. Choosing a character flips back through its first page.
                    _screenDue = 0f;
                    if (scope != null)
                    {
                        bool flipping = Walk.Scene.Contains("CharacterSelection") && Time.realtimeSinceStartup - InputBridge.LastConfirmAt < 0.5f;
                        _screenDue = Time.realtimeSinceStartup + (flipping ? 0.85f : 0.3f);
                        _screenScope = ptr;
                        FocusProbe.SuppressFocusUntil = Mathf.Max(FocusProbe.SuppressFocusUntil, _screenDue + 0.05f);
                    }
                    if (scope != null && scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name == "BowlShopEncounterUI" && Time.realtimeSinceStartup - _shopSaidAt > 30f && (_shopSaidAt = Time.realtimeSinceStartup) > 0f)
                        FocusProbe.Announce("A shop. Cards are paid for with food. Enter on a card to choose it, then Enter on a food card in your hand and Enter on the bowl to pay.", interrupt: false);
                    if (scope != null && scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name == "S_MapReminder")
                    {
                        System.Collections.Generic.List<string> map = MapReader.Lines();
                        Navigator.FocusChanged();
                        FocusProbe.Announce(map[0] + ". Control Up reads each area. M puts the map away.", interrupt: false);
                    }
                    string kind = scope != null ? scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name : "";
                    if (kind.Contains("ModifyCard") && ptr != _introSaidFor)
                    {
                        _introScope = ptr;
                        _introDue = Time.realtimeSinceStartup + 0.8f;
                        _introGiveUp = Time.realtimeSinceStartup + 2.5f;
                        FocusProbe.SuppressFocusUntil = _introDue + 0.2f;
                    }
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

                if (_screenDue > 0f && Time.realtimeSinceStartup >= _screenDue)
                {
                    _screenDue = 0f;
                    if (scope != null && ptr == _screenScope)
                    {
                        // The soup pot's heading and value are said by Soup, with the soups.
                        if (!Rewards.Handles(scope) && Soup.Screen(focus) == null) AnnounceScreen(scope);
                        Rewards.ScreenOpened(scope);
                    }
                }

                if (_introDue > 0f && Time.realtimeSinceStartup >= _introDue)
                {
                    List<string> words = scope != null && ptr == _introScope ? ScreenText(scope, 4) : new List<string>();
                    if (words.Count == 0 && scope != null && ptr == _introScope && Time.realtimeSinceStartup < _introGiveUp)
                    {
                        _introDue = Time.realtimeSinceStartup + 0.3f;
                        FocusProbe.SuppressFocusUntil = _introDue + 0.2f;
                    }
                    else
                    {
                        _introDue = 0f;
                        _introSaidFor = _introScope;
                        if (words.Count > 0)
                        {
                            _liveScope = ptr;
                            _liveText = ScreenText(scope, 6);
                            _lastScreenText = string.Join(" | ", ScreenText(scope, 4));
                            _lastScreenAt = Time.realtimeSinceStartup;
                            FocusProbe.Announce(string.Join(" ", words) + ". Left and Right choose one of your cards, Enter picks it. Up is No thanks.", interrupt: false);
                        }
                        FocusProbe.Reannounce();
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

                if (scope != null && _introDue == 0f && Time.realtimeSinceStartup >= _liveNext)
                {
                    _liveNext = Time.realtimeSinceStartup + 0.6f;
                    List<string> now = ScreenText(scope, 6);
                    if (_liveScope == ptr)
                    {
                        var fresh = new List<string>();
                        foreach (string part in now) if (!_liveText.Contains(part)) fresh.Add(part);
                        if (fresh.Count > 0 && fresh.Count <= 3 && Soup.Screen(focus) == null)
                        {
                            Log("[screen-change] " + string.Join(" | ", fresh));
                            foreach (string part in fresh) if (!SaidLately(part)) FocusProbe.Announce(part, interrupt: false);
                        }
                    }
                    _liveScope = ptr;
                    _liveText = now;
                }

                WatchShop(scope);

                // A card aimed at the player waits for a second Enter with nothing focused; say so, or
                // the first Enter seems to do nothing. Some (food) can be turned on other targets too.
                bool target = focus.Mode == FocusMode.Target;
                if (target && !_wasTarget && focus.CurrentFocus == null)
                    FocusProbe.Announce("Aimed at you. Enter uses it on yourself; Left and Right look for another target; Backspace cancels.", interrupt: false);
                _wasTarget = target;
            }
            catch (Exception ex) { Log("[messages] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static string _shopState = "";
        private static int _shopPaid, _shopPrice;
        private static string _shopBuying = "";
        private static IntPtr _shopCard;
        private static float _shopCheckAt;

        // The shop shows payment as food piling up in the bowl. Say how much is paid, and that the
        // card was bought: the shopkeeper's line alone does not make it clear.
        private static void WatchShop(IFocusScope scope)
        {
            BowlShopEncounterUI shop = scope != null ? scope.Cast<Il2CppSystem.Object>().TryCast<BowlShopEncounterUI>() : null;
            if (shop == null) { _shopState = ""; _shopCheckAt = 0f; return; }
            string state = shop._currentShopState.ToString();
            int paid = shop._paidCostAmountInBowl;
            if (state == "FeedMePlease" && _shopState != "FeedMePlease")
            {
                V_GameplayCardInstance card = shop._selectedCardToBuy;
                _shopBuying = card != null && card.IModel != null ? card.IModel.CardName : "";
                _shopCard = card != null && card.IModel != null ? card.IModel.Pointer : IntPtr.Zero;
                string digits = card != null ? new string(FocusProbe.ShopPrice(card).Where(char.IsDigit).ToArray()) : "";
                _shopPrice = int.TryParse(digits, out int price) ? price : 0;
                _shopPaid = paid;
                // The focus passes over the Nevermind button on its way to the food.
                FocusProbe.Flicker(0.5f);
            }
            else if (state == "FeedMePlease" && paid > _shopPaid)
            {
                FocusProbe.Announce("Paid " + paid + (_shopPrice > 0 ? " of " + _shopPrice : "") + ".", interrupt: false);
                _shopPaid = paid;
            }
            // Paying is over, by purchase or by Nevermind. A bought card leaves the shop's offer once
            // it has flown to the deck; look a moment later.
            if (_shopState == "FeedMePlease" && state != "FeedMePlease") _shopCheckAt = Time.realtimeSinceStartup + 1.5f;
            if (_shopCheckAt > 0f && Time.realtimeSinceStartup >= _shopCheckAt)
            {
                _shopCheckAt = 0f;
                bool stillOffered = false;
                var choices = shop._cardChoices;
                for (int i = 0; choices != null && i < choices.Count; i++)
                    if (choices[i] != null && choices[i].Pointer == _shopCard) { stillOffered = true; break; }
                Log("[shop] after paying: " + _shopBuying + " still offered=" + stillOffered + " paid=" + _shopPaid + " of " + _shopPrice);
                if (!stillOffered && _shopBuying.Length > 0) FocusProbe.Announce("Bought " + _shopBuying + ".", interrupt: false);
            }
            _shopState = state;
        }

        // After a change is applied the game shows the new card with a Take button under it. The card
        // cannot be focused, so it is read with the button.
        internal static string ModifyResult(S_ControllerFocus focus, Component button)
        {
            try
            {
                var inHand = new HashSet<IntPtr>();
                IM_Hand hand = focus.PlayerModel != null ? focus.PlayerModel.GetActiveHand() : null;
                for (int i = 0; hand != null && hand.HeldCards != null && i < hand.HeldCards.Count; i++)
                    if (hand.HeldCards[i] != null) inHand.Add(hand.HeldCards[i].Pointer);

                Camera cam = focus.FocusCamera != null ? focus.FocusCamera : Camera.main;
                Vector3 at = cam != null ? cam.WorldToViewportPoint(button.transform.position) : Vector3.zero;
                V_GameplayCardInstance best = null;
                float bestDistance = float.MaxValue;
                foreach (V_GameplayCardInstance card in UnityEngine.Object.FindObjectsOfType<V_GameplayCardInstance>())
                {
                    if (card == null || !card.gameObject.activeInHierarchy) continue;
                    IV_CardInstance view = card.Cast<Il2CppSystem.Object>().TryCast<IV_CardInstance>();
                    if (view == null || view.IModel == null || inHand.Contains(view.IModel.Pointer)) continue;
                    Vector3 p = cam != null ? cam.WorldToViewportPoint(card.transform.position) : Vector3.zero;
                    if (p.z < 0f) continue;
                    float d = (new Vector2(p.x, p.y) - new Vector2(at.x, at.y)).sqrMagnitude;
                    if (d < bestDistance) { bestDistance = d; best = card; }
                }
                if (best != null && bestDistance < 0.2f) return FocusProbe.Describe(best, out _);
            }
            catch (Exception ex) { Log("[messages] result: " + ex.Message); }
            return null;
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
                if (t.GetComponentInParent<CharacterSelection_Button>() != null) continue;
                string text = FocusProbe.Clean(t.text);
                if (text.Length < 2 || text.Length > 220 || parts.Contains(text)) continue;
                // Price tags belong to the cards; the pause menu prints build and session identifiers.
                if (text.StartsWith("COST") || text.StartsWith("UserID") || text.StartsWith("SessionID") || text.StartsWith("V 0.") || text.StartsWith("V 1.")) continue;
                parts.Add(text);
                if (parts.Count == limit + 2) break;
            }
            parts.RemoveAll(part => parts.Exists(other => other.Length > part.Length && other.Contains(part.TrimEnd(':', '.', ' '))));
            if (parts.Count > limit) parts.RemoveRange(limit, parts.Count - limit);
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
            // Each piece is its own utterance, so the screen reader pauses between them.
            foreach (string part in parts) if (!SaidLately(part)) FocusProbe.Announce(part, interrupt: false);
        }
    }
}
