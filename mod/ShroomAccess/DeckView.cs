using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using Il2CppControllerNav;
using UnityEngine;
using UnityEngine.UI;

namespace ShroomAccess
{
    // The deck view (DeckDisplay): a title ("DRAW PILE"), buttons for the draw pile, the full deck
    // and the combat, explore and foresight decks, and a grid of cards. In a run the game opens it
    // only from the pile buttons on its HUD, which a gamepad cannot reach; Tab opens it here.
    // Tab: draw pile, then discard pile, then closed.
    internal static class DeckView
    {
        internal static Action<string> Log = _ => { };

        private static string _shown = "";
        private static string _kind = "";
        private static float _settleAt;
        private static string _pending = "";

        internal static DeckDisplay Screen(S_ControllerFocus focus)
        {
            try { return focus.Scope != null ? focus.Scope.Cast<Il2CppSystem.Object>().TryCast<DeckDisplay>() : null; }
            catch { return null; }
        }

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                DeckDisplay deck = Screen(focus);
                bool tab = InputBridge.Key(InputActionId.ToggleDeckView) && !InputBridge.CtrlHeld;
                if (deck == null)
                {
                    _shown = "";
                    // Only in a run, with nothing else on screen.
                    if (tab && focus.PlayerModel != null && focus.Scope == null && Press("Draw Deck Image"))
                    {
                        Log("[deck] opened the draw pile");
                        // The pile shown is that of the deck in play: the combat deck in a fight.
                        _kind = Hud.InFight(focus) ? "Combat deck" : "Explore deck";
                        // The summary comes first; the button the focus lands on is found with the arrows.
                        FocusProbe.Silent(2f);
                    }
                    return;
                }

                string title = Title(deck);
                // Enter on one of the deck buttons changes what is shown; the summary says it first.
                if (InputBridge.Key(InputActionId.Confirm) && focus.CurrentFocus != null)
                {
                    Component b = focus.CurrentFocus.TryCast<Component>();
                    string label = b != null ? ButtonName(b.gameObject.name) : "";
                    if (label.Length > 0)
                    {
                        FocusProbe.Silent(1.5f);
                        if (label.Contains("combat")) _kind = "Combat deck";
                        else if (label.Contains("explore")) _kind = "Explore deck";
                        else if (label.Contains("foresight")) _kind = "";
                    }
                }
                if (tab)
                {
                    if (title.StartsWith("Draw", StringComparison.OrdinalIgnoreCase)) { deck.OnShowDiscardPile(); FocusProbe.Silent(1.5f); Log("[deck] discard pile"); }
                    else if (deck._closeButton != null) { deck._closeButton.onClick.Invoke(); Log("[deck] closed"); }
                    return;
                }

                // Say what is shown once the cards have been laid out.
                string now = title + "|" + string.Join(",", Cards(deck).Select(c => Name(c)));
                if (now != _pending) { _pending = now; _settleAt = Time.realtimeSinceStartup + 0.4f; }
                if (_settleAt > 0f && Time.realtimeSinceStartup >= _settleAt && _pending != _shown)
                {
                    _settleAt = 0f;
                    _shown = _pending;
                    var names = Cards(deck).Select(c => Name(c)).Where(n => n.Length > 0).ToList();
                    // "Combat deck, draw pile"; the full deck and foresights need no deck name.
                    string what = _kind.Length > 0 && !title.ToUpperInvariant().Contains("FULL") && !title.ToUpperInvariant().Contains("FORESIGHT")
                        ? _kind + ", " + title.ToLowerInvariant() : Sentence(title);
                    string line = names.Count == 0
                        ? what + ": empty."
                        : what + ", " + names.Count + (names.Count == 1 ? " card: " : " cards: ") + FocusProbe.Group(names, ", ") + ".";
                    FocusProbe.Announce(line, interrupt: false);
                    FocusProbe.Silent(1.5f);
                }
            }
            catch (Exception ex) { Log("[deck] " + ex.GetType().Name + ": " + ex.Message); }
        }

        // For a focused card in the deck view: "3 of 12". For its buttons: what they show.
        internal static string Suffix(S_ControllerFocus focus, Il2CppSystem.Object current)
        {
            try
            {
                DeckDisplay deck = Screen(focus);
                if (deck == null || current == null) return "";
                var cards = Cards(deck);
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i].Pointer == current.Pointer) return ", " + (i + 1) + " of " + cards.Count;
            }
            catch { }
            return "";
        }

        internal static string ButtonName(string objectName)
        {
            string n = objectName.Trim();
            if (n.StartsWith("Draw Pile")) return "Show the draw pile";
            if (n.StartsWith("Full Deck")) return "Show the full deck";
            if (n.StartsWith("Combat Deck")) return "Show the combat deck";
            if (n.StartsWith("Explore Deck")) return "Show the explore deck";
            if (n.StartsWith("Foresight Deck")) return "Show the foresight cards";
            if (n.StartsWith("Close")) return "Close the deck view";
            return "";
        }

        private static string Title(DeckDisplay deck)
        {
            string t = deck._titleText != null ? FocusProbe.Clean(deck._titleText.text) : "";
            return t.Length > 0 ? t : "Deck";
        }

        private static string Sentence(string title) =>
            title.Length > 1 ? title.Substring(0, 1).ToUpperInvariant() + title.Substring(1).ToLowerInvariant() : title;

        // The cards on show, top row first, left to right.
        private static List<Component> Cards(DeckDisplay deck)
        {
            var list = new List<Component>();
            Transform root = deck._cardsParentTransform != null ? deck._cardsParentTransform : deck.transform;
            foreach (V_GameplayCardInstance c in root.GetComponentsInChildren<V_GameplayCardInstance>(false)) if (c != null) list.Add(c);
            foreach (V_ForesightCardInstance c in root.GetComponentsInChildren<V_ForesightCardInstance>(false)) if (c != null) list.Add(c);
            return list.OrderByDescending(c => Mathf.Round(c.transform.position.y * 2f)).ThenBy(c => c.transform.position.x).ToList();
        }

        private static string Name(Component c)
        {
            IV_CardInstance iv = c.Cast<Il2CppSystem.Object>().TryCast<IV_CardInstance>();
            return iv != null && iv.IModel != null ? FocusProbe.Clean(iv.IModel.CardName) : "";
        }

        private static bool Press(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
                foreach (Button b in UnityEngine.Object.FindObjectsOfType<Button>())
                    if (b != null && b.gameObject.name == objectName) { go = b.gameObject; break; }
            Button button = go != null ? go.GetComponent<Button>() : null;
            if (button == null || !button.interactable) return false;
            button.onClick.Invoke();
            return true;
        }
    }
}
