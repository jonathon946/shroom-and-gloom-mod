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
                    DumpBindings(input);
                    DumpAsset(input);
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

                if (input != null && _bindingsDumped)
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
                    return;
                }

                string type = TypeName(current);
                string path = "";
                var texts = new List<string>();
                Component comp = current.TryCast<Component>();
                if (comp != null)
                {
                    path = PathOf(comp.transform);
                    foreach (TMP_Text t in comp.GetComponentsInChildren<TMP_Text>(false))
                    {
                        string s = t != null ? t.text : null;
                        if (!string.IsNullOrWhiteSpace(s)) texts.Add(s.Trim());
                    }
                }
                Log("[focus] " + type + " | " + path + " | " + string.Join(" / ", texts));

                string spoken = texts.Count > 0 ? string.Join(", ", texts.GetRange(0, Math.Min(3, texts.Count)))
                    : (comp != null ? comp.gameObject.name : type);
                Announce(StripTags(spoken));
            }
            catch (Exception ex)
            {
                Log("[probe] " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // Set by Main when the title screen loads: the time after which the one-off experiments run.
        internal static float ArmedAt;

        // Speech is confined to the game's own window: nothing is spoken while the player is elsewhere.
        internal static void Announce(string text, bool interrupt = true)
        {
            Log("[say] " + text);
            if (Application.isFocused) Speaker.Say(text, interrupt);
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
