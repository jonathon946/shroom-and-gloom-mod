using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppControllerNav;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShroomAccess
{
    // The end of a run: the words spoken to the dead, the page of numbers, and the rewards for
    // quests finished along the way. The reward for a card quest is a sealed pack that the game
    // opens only on a mouse click; here Enter opens it and the cards inside are read.
    internal static class Rewards
    {
        internal static Action<string> Log = _ => { };

        private static float _statsDue;
        private static int _statsTries;
        private static Component _statsRoot;
        private static IntPtr _statsSaidFor;
        private static readonly List<string> _statLines = new List<string>();

        private static float _cardsDue, _cardsGiveUp;

        private static CardUnlockPack _pack;
        private static float _packLookAt;
        private static IntPtr _packSaidFor;

        private const string PackPrompt = "A sealed pack of cards. Enter opens it.";

        private static string Kind(IFocusScope scope) =>
            scope != null ? scope.Cast<Il2CppSystem.Object>().GetIl2CppType().Name : "";

        // The page of numbers is read here in full, not by the general screen reader.
        internal static bool Handles(IFocusScope scope) => Kind(scope) == "RoundUpUIManager";

        // For the buffers: the page of numbers, line by line.
        internal static List<string> StatLines(IFocusScope scope) => Handles(scope) && _statLines.Count > 0 ? new List<string>(_statLines) : null;

        internal static void ScreenOpened(IFocusScope scope)
        {
            try
            {
                string kind = Kind(scope);
                float now = Time.realtimeSinceStartup;
                if (kind == "RoundUpUIManager")
                {
                    _statsRoot = scope.Cast<Il2CppSystem.Object>().TryCast<Component>();
                    // Coming back to the page from a reward is not news; the buffer still holds it.
                    if (_statsRoot != null && _statsRoot.Pointer != _statsSaidFor) { _statsDue = now + 1.5f; _statsTries = 0; }
                }
                else if (kind == "DeathText")
                {
                    FocusProbe.Announce("Space continues.", interrupt: false);
                }
                else if (kind == "CardPackClaimedUI")
                {
                    _cardsDue = now + 0.6f;
                    _cardsGiveUp = now + 5f;
                }
            }
            catch (Exception ex) { Log("[rewards] opened: " + ex.Message); }
        }

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                if (_statsDue > 0f && now >= _statsDue) ReadStats(now);
                if (_cardsDue > 0f && now >= _cardsDue) ReadCards(now);

                if (now >= _packLookAt)
                {
                    _packLookAt = now + 0.5f;
                    _pack = UnityEngine.Object.FindObjectOfType<CardUnlockPack>();
                }
                if (_pack == null || !_pack.gameObject.activeInHierarchy || !_pack.EnabledForClick || _pack._clicked) return;

                if (_packSaidFor != _pack.Pointer)
                {
                    _packSaidFor = _pack.Pointer;
                    FocusProbe.Announce(PackPrompt, interrupt: false);
                }
                if (InputBridge.CtrlHeld) return;
                if (InputBridge.Key(InputActionId.Confirm))
                {
                    Log("[rewards] opening the pack");
                    _pack.OnPointerClick(new PointerEventData(EventSystem.current));
                    // The pack can be opened once; ask again only for the next one.
                    _packLookAt = now + 1.5f;
                    _pack = null;
                }
                else if (InputBridge.Key(InputActionId.FocusPrev) || InputBridge.Key(InputActionId.FocusNext)
                    || InputBridge.Key(InputActionId.FocusUp) || InputBridge.Key(InputActionId.FocusDown))
                {
                    FocusProbe.Speak(PackPrompt);
                }
            }
            catch (Exception ex) { Log("[rewards] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static void ReadStats(float now)
        {
            _statsDue = 0f;
            if (_statsRoot == null) return;
            var lines = new List<string>();
            bool waiting = false;
            foreach (TMP_Text t in _statsRoot.GetComponentsInChildren<TMP_Text>(false))
            {
                if (t != null && t.gameObject.name == "RUN STATS HEADING" && !string.IsNullOrWhiteSpace(t.text)) lines.Add(FocusProbe.Clean(t.text));
            }
            foreach (LabelValueStatEntryUI entry in _statsRoot.GetComponentsInChildren<LabelValueStatEntryUI>(false))
            {
                if (entry == null || entry.LabelText == null || entry.ValueText == null) continue;
                // The health orb beside the page reuses the same parts for its two numbers.
                if (UnderNamed(entry.transform, "Health Orb")) continue;
                string label = FocusProbe.Clean(entry.LabelText.text).TrimEnd(':', ' ');
                string value = FocusProbe.Clean(entry.ValueText.text);
                if (label.Length == 0) continue;
                if (value.Length == 0) { waiting = true; continue; }
                lines.Add(label + ": " + value);
            }
            // The numbers are filled in a moment after the page appears.
            if ((waiting || lines.Count < 2) && _statsTries++ < 6) { _statsDue = now + 0.5f; return; }
            if (lines.Count == 0) return;

            _statsSaidFor = _statsRoot.Pointer;
            _statLines.Clear();
            _statLines.AddRange(lines);
            Navigator.FocusChanged();
            FocusProbe.Announce(string.Join(". ", lines) + ". Control Up reads these again. Enter continues.", interrupt: false);
        }

        private static void ReadCards(float now)
        {
            _cardsDue = 0f;
            var cards = new List<KeyValuePair<float, string>>();
            foreach (V_GameplayCardInstance card in UnityEngine.Object.FindObjectsOfType<V_GameplayCardInstance>())
            {
                if (card == null || !card.gameObject.activeInHierarchy) continue;
                string line = FocusProbe.Describe(card, out _);
                if (string.IsNullOrWhiteSpace(line)) continue;
                cards.Add(new KeyValuePair<float, string>(card.transform.position.x, line.TrimEnd('.', ' ')));
            }
            if (cards.Count == 0)
            {
                if (now < _cardsGiveUp) _cardsDue = now + 0.5f;
                else Log("[rewards] no cards found in the opened pack");
                return;
            }
            cards.Sort((a, b) => a.Key.CompareTo(b.Key));
            var names = new List<string>();
            foreach (var pair in cards) names.Add(pair.Value);
            FocusProbe.Announce("The pack holds " + names.Count + (names.Count == 1 ? " card: " : " cards: ") + string.Join(". ", names) + ".", interrupt: false);
        }

        private static bool UnderNamed(Transform t, string name)
        {
            for (int i = 0; t != null && i < 8; i++, t = t.parent) if (t.gameObject.name == name) return true;
            return false;
        }
    }
}
