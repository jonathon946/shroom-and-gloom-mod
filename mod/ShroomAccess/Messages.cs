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
        private static void AnnounceScreen(IFocusScope scope)
        {
            Component root = scope.Cast<Il2CppSystem.Object>().TryCast<Component>();
            if (root == null) return;
            var parts = new List<string>();
            foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (t == null || t.GetComponentInParent<Selectable>() != null) continue;
                if (IsControlLabel(t.transform)) continue;
                if (t.GetComponentInParent<V_GameplayCardInstance>() != null || t.GetComponentInParent<V_ForesightCardInstance>() != null) continue;
                string text = FocusProbe.Clean(t.text);
                if (text.Length < 2 || text.Length > 220 || parts.Contains(text)) continue;
                // The pause menu prints build and session identifiers; nobody needs those read out.
                if (text.StartsWith("UserID") || text.StartsWith("SessionID") || text.StartsWith("V 0.") || text.StartsWith("V 1.")) continue;
                parts.Add(text);
                if (parts.Count == 4) break;
            }
            if (parts.Count == 0) return;
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
