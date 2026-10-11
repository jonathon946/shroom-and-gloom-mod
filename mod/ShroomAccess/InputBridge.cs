using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using UnityEngine.InputSystem;

namespace ShroomAccess
{
    // The game's focus navigation only runs while a gamepad is "driving", and its focus actions are
    // bound to gamepad buttons only. This bridge keeps the game in gamepad mode and answers the
    // game's own action queries from the keyboard, so every screen the developers made pad-operable
    // becomes keyboard-operable without us re-implementing any navigation.
    internal static class InputBridge
    {
        internal static Action<string> Log = _ => { };
        internal static bool Enabled = true;
        private static bool _readFailed;

        // Keyboard controls that stand in for each game action, as Input System control paths.
        // Ctrl is reserved for mod commands.
        private static readonly Dictionary<InputActionId, string[]> Map = new Dictionary<InputActionId, string[]>
        {
            { InputActionId.FocusPrev, new[] { "leftArrow" } },
            { InputActionId.FocusNext, new[] { "rightArrow" } },
            { InputActionId.FocusUp, new[] { "upArrow" } },
            { InputActionId.FocusDown, new[] { "downArrow" } },
            { InputActionId.Confirm, new[] { "enter", "numpadEnter" } },
            { InputActionId.Cancel, new[] { "backspace" } },
            { InputActionId.Pause, new[] { "escape" } },
            { InputActionId.Inspect, new[] { "i" } },
            { InputActionId.ToggleDeckView, new[] { "tab" } },
            { InputActionId.PagePrev, new[] { "pageUp" } },
            { InputActionId.PageNext, new[] { "pageDown" } },
            { InputActionId.SortHand, new[] { "o" } },
            { InputActionId.RecentreView, new[] { "c" } },
        };

        // Our own button actions, one per game action. Actions are event-driven, so a key tapped and
        // released inside a single frame still counts; polling key state would miss it.
        private static Dictionary<InputActionId, InputAction> _actions;

        private static void EnsureActions()
        {
            if (_actions != null) return;
            _actions = new Dictionary<InputActionId, InputAction>();
            foreach (var pair in Map)
            {
                var action = new InputAction("ShroomAccess." + pair.Key, InputActionType.Button);
                foreach (string control in pair.Value)
                    InputActionSetupExtensions.AddBinding(action, "<Keyboard>/" + control);
                action.Enable();
                _actions[pair.Key] = action;
            }
            Log("Keyboard actions ready: " + _actions.Count);
        }

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            Patch(harmony, typeof(BasicInputSource), "SetInputContext", prefix: nameof(SetInputContextPrefix));
            Patch(harmony, typeof(S_InputManager), "GetActionDown", postfix: nameof(GetActionDownPostfix));
            Patch(harmony, typeof(S_InputManager), "GetAction", postfix: nameof(GetActionPostfix));
            Patch(harmony, typeof(S_InputManager), "GetActionUp", postfix: nameof(GetActionUpPostfix));
            Patch(harmony, typeof(S_InputManager), "GetAxis", postfix: nameof(GetAxisPostfix));
        }

        // Each hook is resolved by name and patched on its own, so one renamed method after a game
        // update disables one feature and says so, instead of silencing the mod.
        private static void Patch(HarmonyLib.Harmony harmony, Type type, string method, string prefix = null, string postfix = null)
        {
            try
            {
                MethodInfo target = AccessTools.Method(type, method);
                if (target == null) { Log("Hook missing: " + type.Name + "." + method); return; }
                const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
                harmony.Patch(target,
                    prefix != null ? new HarmonyMethod(typeof(InputBridge).GetMethod(prefix, flags)) : null,
                    postfix != null ? new HarmonyMethod(typeof(InputBridge).GetMethod(postfix, flags)) : null);
                Log("Hooked " + type.Name + "." + method);
            }
            catch (Exception ex)
            {
                Log("Hook failed: " + type.Name + "." + method + ": " + ex.Message);
            }
        }

        // Called every frame: put the game in gamepad mode if something moved it out.
        internal static void Tick(S_InputManager input)
        {
            if (!Enabled || input == null) return;
            try
            {
                EnsureActions();
                if (input.CurrentDeviceType == InputDeviceType.Gamepad) return;
                BasicInputSource src = input._inputSourceBehaviour != null ? input._inputSourceBehaviour.TryCast<BasicInputSource>() : null;
                if (src != null) src.SetInputContext(InputDeviceType.Gamepad, GamepadFamily.Xbox);
            }
            catch (Exception ex) { Log("InputBridge.Tick: " + ex.Message); }
        }

        private static void SetInputContextPrefix(ref InputDeviceType deviceType, ref GamepadFamily gamepadFamily)
        {
            if (!Enabled) return;
            deviceType = InputDeviceType.Gamepad;
            if (gamepadFamily == GamepadFamily.Unknown) gamepadFamily = GamepadFamily.Xbox;
        }

        private static void GetActionDownPostfix(InputActionId actionId, ref bool __result)
        {
            if (!__result && Enabled) __result = Read(actionId, 0);
        }

        private static void GetActionPostfix(InputActionId actionId, ref bool __result)
        {
            if (!__result && Enabled) __result = Read(actionId, 1);
        }

        private static void GetActionUpPostfix(InputActionId actionId, ref bool __result)
        {
            if (!__result && Enabled) __result = Read(actionId, 2);
        }

        // Walking: W and S (and A, D) stand in for the left stick.
        private static InputAction _moveUp, _moveDown, _moveLeft, _moveRight;
        private static UnityEngine.Vector2 _injectedAxis;
        private static float _injectedAxisUntil;

        internal static void InjectMove(float x, float y, float seconds)
        {
            _injectedAxis = new UnityEngine.Vector2(x, y);
            _injectedAxisUntil = UnityEngine.Time.realtimeSinceStartup + seconds;
        }

        private static InputAction Button(string name, string control)
        {
            var action = new InputAction("ShroomAccess." + name, InputActionType.Button);
            InputActionSetupExtensions.AddBinding(action, "<Keyboard>/" + control);
            action.Enable();
            return action;
        }

        private static void GetAxisPostfix(InputActionId actionId, ref UnityEngine.Vector2 __result)
        {
            if (!Enabled || actionId != InputActionId.Move) return;
            try
            {
                if (UnityEngine.Time.realtimeSinceStartup < _injectedAxisUntil) { __result = _injectedAxis; return; }
                if (__result.sqrMagnitude > 0.01f || !UnityEngine.Application.isFocused) return;
                if (_moveUp == null)
                {
                    _moveUp = Button("MoveUp", "w"); _moveDown = Button("MoveDown", "s");
                    _moveLeft = Button("MoveLeft", "a"); _moveRight = Button("MoveRight", "d");
                }
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.ctrlKey.isPressed) return;
                float x = (_moveRight.IsPressed() ? 1f : 0f) - (_moveLeft.IsPressed() ? 1f : 0f);
                float y = (_moveUp.IsPressed() ? 1f : 0f) - (_moveDown.IsPressed() ? 1f : 0f);
                if (x != 0f || y != 0f) __result = new UnityEngine.Vector2(x, y);
            }
            catch (Exception ex)
            {
                if (!_readFailed) { _readFailed = true; Log("InputBridge.GetAxis: " + ex); }
            }
        }

        // Dev testing: a simulated press of one game action, so tests need neither the keyboard nor
        // window focus. Down on the next frame, held for a few, then released.
        private static InputActionId _injected = InputActionId.None;
        private static int _injectedFrame;
        private const int InjectHoldFrames = 4;

        internal static void Inject(InputActionId id)
        {
            _injected = id;
            _injectedFrame = UnityEngine.Time.frameCount + 1;
        }

        private static bool ReadInjected(InputActionId id, int phase)
        {
            if (id != _injected || id == InputActionId.None) return false;
            int age = UnityEngine.Time.frameCount - _injectedFrame;
            if (phase == 0) return age == 0;
            if (phase == 1) return age >= 0 && age < InjectHoldFrames;
            return age == InjectHoldFrames;
        }

        // phase: 0 pressed this frame, 1 held, 2 released this frame.
        private static bool Read(InputActionId id, int phase)
        {
            if (ReadInjected(id, phase)) return true;
            // Keys typed into another window are not for the game.
            if (!UnityEngine.Application.isFocused) return false;
            try
            {
                if (!Map.ContainsKey(id)) return false;
                EnsureActions();
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.ctrlKey.isPressed) return false;
                InputAction action = _actions[id];
                return phase == 0 ? action.WasPressedThisFrame() : phase == 1 ? action.IsPressed() : action.WasReleasedThisFrame();
            }
            catch (Exception ex)
            {
                if (!_readFailed) { _readFailed = true; Log("InputBridge.Read: " + ex); }
            }
            return false;
        }
    }
}
