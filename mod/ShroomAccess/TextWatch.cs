using System;
using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;

namespace ShroomAccess
{
    // Text the game shows without anything being focused: the loading screen's tip and its
    // "press space" line, unlock and reward banners, the death text. These parts of the screen are
    // watched, and any words that newly become visible are spoken.
    internal static class TextWatch
    {
        internal static Action<string> Log = _ => { };

        // Named objects whose visible text is watched. Found again whenever a scene replaces them.
        private static readonly string[] RootNames = { "LoadingScreen", "Meta Root", "SomethingHasChangedText" };
        private static readonly GameObject[] Roots = new GameObject[RootNames.Length];
        private static readonly HashSet<string> Showing = new HashSet<string>();
        private static float _next;

        internal static void Tick()
        {
            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 0.4f;
            try
            {
                var now = new HashSet<string>();
                var fresh = new List<string>();
                for (int r = 0; r < RootNames.Length; r++)
                {
                    if (Roots[r] == null) Roots[r] = GameObject.Find(RootNames[r]);
                    GameObject root = Roots[r];
                    if (root == null || !root.activeInHierarchy) continue;
                    foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (t == null || !Visible(t)) continue;
                        string text = FocusProbe.Clean(t.text);
                        if (text.Length < 2 || IsNumber(text)) continue;
                        string key = RootNames[r] + "|" + text;
                        if (!now.Add(key)) continue;
                        if (!Showing.Contains(key)) fresh.Add(RootNames[r] == "LoadingScreen" && fresh.Count == 0 ? "Loading. " + text : text);
                    }
                }
                Showing.Clear();
                foreach (string key in now) Showing.Add(key);
                if (fresh.Count == 0) return;
                Log("[text] " + string.Join(" | ", fresh));
                FocusProbe.Announce(string.Join(". ", fresh).Replace(">>", "").Trim(), interrupt: false, holdWhileWalking: true);
            }
            catch (Exception ex) { Log("[text] " + ex.GetType().Name + ": " + ex.Message); }
        }

        // The game fades these in and out with canvas groups instead of switching them off.
        private static bool Visible(TMP_Text t)
        {
            if (t.color.a < 0.4f) return false;
            float alpha = 1f;
            Transform walk = t.transform;
            for (int depth = 0; walk != null && depth < 10; depth++, walk = walk.parent)
            {
                CanvasGroup group = walk.GetComponent<CanvasGroup>();
                if (group != null) alpha *= group.alpha;
                if (alpha < 0.5f) return false;
            }
            return true;
        }

        private static bool IsNumber(string s)
        {
            foreach (char c in s) if (!char.IsDigit(c) && c != '/' && c != ' ') return false;
            return true;
        }
    }
}
