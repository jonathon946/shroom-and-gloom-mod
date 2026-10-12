using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
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
        private static InputAction _moveUp, _moveDown, _moveLeft, _moveRight;

        private static InputAction Button(string name, params string[] controls)
        {
            var action = new InputAction("ShroomAccess." + name, InputActionType.Button);
            foreach (string control in controls)
                InputActionSetupExtensions.AddBinding(action, "<Keyboard>/" + control);
            action.Enable();
            return action;
        }

        private static void EnsureActions()
        {
            if (_actions != null) return;
            var made = new Dictionary<InputActionId, InputAction>();
            foreach (var pair in Map) made[pair.Key] = Button(pair.Key.ToString(), pair.Value);
            _moveUp = Button("MoveUp", "w"); _moveDown = Button("MoveDown", "s");
            _moveLeft = Button("MoveLeft", "a"); _moveRight = Button("MoveRight", "d");
            _actions = made;
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

        // ---- What the player (or a dev test standing in for the player) is pressing ----

        internal static bool CtrlHeld
        {
            get
            {
                if (_simCtrl && Time.frameCount - _simFrame <= SimHoldFrames) return true;
                Keyboard kb = Keyboard.current;
                return Application.isFocused && kb != null && kb.ctrlKey.isPressed;
            }
        }

        // True while a walk key is down. Screen readers cut speech on every key repeat, so anything
        // said while a key is held is lost; callers hold their lines until this is false.
        internal static bool MoveHeld
        {
            get
            {
                if (Time.realtimeSinceStartup < _injectedAxisUntil) return true;
                if (_moveUp == null || !Application.isFocused) return false;
                return _moveUp.IsPressed() || _moveDown.IsPressed() || _moveLeft.IsPressed() || _moveRight.IsPressed();
            }
        }

        // A key press as the player made it, before the mod or the game has interpreted it.
        // phase: 0 pressed this frame, 1 held, 2 released this frame.
        internal static bool Key(InputActionId id, int phase = 0)
        {
            if (id == _simId && id != InputActionId.None)
            {
                int age = Time.frameCount - _simFrame;
                if (phase == 0 ? age == 0 : phase == 1 ? age >= 0 && age < SimHoldFrames : age == SimHoldFrames) return true;
            }
            if (!Application.isFocused || _actions == null) return false;
            if (!_actions.TryGetValue(id, out InputAction action)) return false;
            return phase == 0 ? action.WasPressedThisFrame() : phase == 1 ? action.IsPressed() : action.WasReleasedThisFrame();
        }

        // Dev testing: a simulated key press, so tests need neither the keyboard nor window focus.
        private static InputActionId _simId = InputActionId.None;
        private static int _simFrame;
        private static bool _simCtrl;
        private const int SimHoldFrames = 4;

        internal static void Simulate(InputActionId id, bool ctrl = false)
        {
            _simId = id;
            _simCtrl = ctrl;
            _simFrame = Time.frameCount + 1;
        }

        // ---- What the game is told ----

        // The navigator claims the arrow keys on screens where it lays things out in rows. While it
        // does, the game sees an arrow only when the navigator forwards one.
        internal static bool NavigatorOwnsArrows;

        private static InputActionId _forwarded = InputActionId.None;
        private static int _forwardedFrame;

        // Press a game action on the game's behalf, on the next frame.
        internal static void Forward(InputActionId id)
        {
            _forwarded = id;
            _forwardedFrame = Time.frameCount + 1;
        }

        private static bool IsArrow(InputActionId id) =>
            id == InputActionId.FocusPrev || id == InputActionId.FocusNext || id == InputActionId.FocusUp || id == InputActionId.FocusDown;

        private static bool Read(InputActionId id, int phase)
        {
            try
            {
                if (id == _forwarded && id != InputActionId.None)
                {
                    int age = Time.frameCount - _forwardedFrame;
                    if (phase == 0 ? age == 0 : phase == 1 ? age >= 0 && age < SimHoldFrames : age == SimHoldFrames) return true;
                }
                if (!Map.ContainsKey(id)) return id == _simId && Key(id, phase);
                if (CtrlHeld) return false;
                if (NavigatorOwnsArrows && IsArrow(id)) return false;
                return Key(id, phase);
            }
            catch (Exception ex)
            {
                if (!_readFailed) { _readFailed = true; Log("InputBridge.Read: " + ex); }
            }
            return false;
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
        private static Vector2 _injectedAxis;
        private static float _injectedAxisUntil;

        internal static void InjectMove(float x, float y, float seconds)
        {
            _injectedAxis = new Vector2(x, y);
            _injectedAxisUntil = Time.realtimeSinceStartup + seconds;
        }

        private static void GetAxisPostfix(InputActionId actionId, ref Vector2 __result)
        {
            if (!Enabled || actionId != InputActionId.Move) return;
            try
            {
                if (Time.realtimeSinceStartup < _injectedAxisUntil) { __result = _injectedAxis; return; }
                if (__result.sqrMagnitude > 0.01f || !Application.isFocused || _moveUp == null) return;
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.ctrlKey.isPressed) return;
                float x = (_moveRight.IsPressed() ? 1f : 0f) - (_moveLeft.IsPressed() ? 1f : 0f);
                float y = (_moveUp.IsPressed() ? 1f : 0f) - (_moveDown.IsPressed() ? 1f : 0f);
                if (x != 0f || y != 0f) __result = new Vector2(x, y);
            }
            catch (Exception ex)
            {
                if (!_readFailed) { _readFailed = true; Log("InputBridge.GetAxis: " + ex); }
            }
        }
    }
}
