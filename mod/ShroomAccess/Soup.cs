using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using Il2CppControllerNav;
using UnityEngine;

namespace ShroomAccess
{
    // The soup pot's screen (AdditiveUnlockCampEncounterUI): a row of soups, each chained shut until
    // the soup value reaches its cost, and the player's food below. Food goes into the pot to raise
    // the value; an open soup is then taken. The game shows the locks only as chains on the pictures.
    internal static class Soup
    {
        internal static Action<string> Log = _ => { };

        private static IntPtr _screen;
        private static string _picked = "", _mode = "";
        private static float _upAt = -100f;
        private static bool _upRetried;

        private static string NameOf(Il2CppSystem.Object current)
        {
            IV_CardInstance card = current != null ? current.TryCast<IV_CardInstance>() : null;
            return card != null && card.IModel != null ? FocusProbe.Clean(card.IModel.CardName) : "";
        }
        private static int _value = -1;
        private static string _open = "";

        internal static AdditiveUnlockCampEncounterUI Screen(S_ControllerFocus focus)
        {
            try { return focus.Scope != null ? focus.Scope.Cast<Il2CppSystem.Object>().TryCast<AdditiveUnlockCampEncounterUI>() : null; }
            catch { return null; }
        }

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                AdditiveUnlockCampEncounterUI ui = Screen(focus);
                if (ui == null)
                {
                    // Taking a soup closes the screen and puts the soup in the hand.
                    if (_screen != IntPtr.Zero && _picked.Length > 0) FocusProbe.Announce("Made " + _picked + ". It is in your hand.", interrupt: false);
                    _screen = IntPtr.Zero;
                    _picked = "";
                    return;
                }
                string mode = focus.Mode.ToString();
                if (InputBridge.Key(InputActionId.Confirm) && mode == "List")
                {
                    string name = Suffix(focus, focus.CurrentFocus).Length > 0 ? NameOf(focus.CurrentFocus) : "";
                    if (name.Length > 0) _picked = name;
                }
                // After food goes in, the game deals the hand again and then puts the focus back on
                // it: an Up pressed in that moment reaches the soups and is pulled straight back.
                // Press it again for the player, once.
                if (InputBridge.Key(InputActionId.FocusUp)) { _upAt = Time.realtimeSinceStartup; _upRetried = false; }
                if (_mode == "List" && mode == "Hand" && !_upRetried && Time.realtimeSinceStartup - _upAt < 0.4f)
                {
                    _upRetried = true;
                    Log("[soup] the game pulled the focus back to the hand; pressing Up again");
                    InputBridge.Forward(InputActionId.FocusUp);
                    FocusProbe.Flicker(0.25f);
                }
                _mode = mode;
                var soups = Soups(ui);
                if (soups.Count == 0) return;
                int value = ui._paidAmount;
                string open = string.Join(", ", soups.Where(s => s.Open).Select(s => s.Name));
                if (ui.Pointer != _screen)
                {
                    _screen = ui.Pointer;
                    if (soups[0].Cost < 0) LogShape(ui);
                    _value = value;
                    _open = open;
                    var parts = soups.Select(s => s.Name + ", " + Needs(s) + (s.Open ? ", open" : ", locked"));
                    FocusProbe.Announce("Make a soup. Soup value " + value + ". " + string.Join(". ", parts)
                        + ". Put food in the pot to raise the value: Enter on a food card, then Enter on the pot. Up goes to the soups; Enter on an open one takes it.", interrupt: false);
                    return;
                }
                if (value != _value)
                {
                    string newly = string.Join(", ", soups.Where(s => s.Open && !_open.Split(new[] { ", " }, StringSplitOptions.None).Contains(s.Name)).Select(s => s.Name));
                    FocusProbe.Announce("Soup value " + value + "." + (newly.Length > 0 ? " Now open: " + newly + "." : ""), interrupt: false);
                    _value = value;
                    _open = open;
                }
            }
            catch (Exception ex) { Log("[soup] " + ex.GetType().Name + ": " + ex.Message); }
        }

        // For the focus line: ", needs 2, locked" when the card is one of the soups.
        internal static string Suffix(S_ControllerFocus focus, Il2CppSystem.Object current)
        {
            try
            {
                AdditiveUnlockCampEncounterUI ui = Screen(focus);
                if (ui == null || current == null) return "";
                foreach (Entry s in Soups(ui))
                    if (s.View == current.Pointer) return ", " + Needs(s) + (s.Open ? ", open" : ", locked");
            }
            catch { }
            return "";
        }

        // Each soup card carries its own price label, "COST: 2", shown under it.
        private static string CostLabel(V_GameplayCardInstance v)
        {
            foreach (Il2CppTMPro.TMP_Text text in v.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true))
                if (text != null && text.gameObject.name == "Purchase Text" && text.text != null) return text.text;
            return "";
        }

        // Dev: where the soup's cost label sits, when it was not found next to the card.
        private static void LogShape(AdditiveUnlockCampEncounterUI ui)
        {
            V_GameplayCardInstance v = ui.vCardInstances[0];
            Transform t = v.transform;
            var chain = new List<string>();
            for (int d = 0; t != null && d < 5; d++, t = t.parent) chain.Add(t.gameObject.name);
            Log("[soup] card chain: " + string.Join(" < ", chain));
            foreach (Il2CppTMPro.TMP_Text text in ui.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true))
                if (text != null && text.text != null && text.text.ToUpperInvariant().Contains("COST"))
                    Log("[soup] text '" + text.text + "' at " + text.transform.parent.name + " < " + (text.transform.parent.parent != null ? text.transform.parent.parent.name : ""));
            foreach (Il2CppTMPro.TMP_Text text in v.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true))
                if (text != null && !string.IsNullOrWhiteSpace(text.text)) Log("[soup] on card '" + text.text + "' (" + text.gameObject.name + ")");
        }

        private static string Needs(Entry s) => s.Cost >= 0 ? "needs " + s.Cost : "cost unknown";

        private struct Entry
        {
            internal IntPtr View;
            internal string Name;
            internal int Cost;
            internal bool Open;
        }

        private static List<Entry> Soups(AdditiveUnlockCampEncounterUI ui)
        {
            var list = new List<Entry>();
            var views = ui.vCardInstances;
            for (int i = 0; views != null && i < views.Count; i++)
            {
                V_GameplayCardInstance v = views[i];
                if (v == null || v.IModel == null) continue;
                string digits = new string(CostLabel(v).Where(char.IsDigit).ToArray());
                bool open = false;
                try { open = ui.IsCardUnlocked(i); } catch { }
                list.Add(new Entry { View = v.Pointer, Name = FocusProbe.Clean(v.IModel.CardName), Cost = int.TryParse(digits, out int c) ? c : -1, Open = open });
            }
            return list;
        }
    }
}
