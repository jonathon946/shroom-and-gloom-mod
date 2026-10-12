using System;
using Il2Cpp;
using Il2CppControllerNav;
using ShroomAccess.Speech;
using UnityEngine;

namespace ShroomAccess
{
    // W and S walk, and only walk. When the game will not let the player walk, the walk keys are
    // not allowed to fall through to the game's stick (which would step the focus instead), and the
    // player is told that they cannot walk, not left pressing a silent key.
    internal static class Walk
    {
        internal static Action<string> Log = _ => { };

        private static bool _holding;
        private static float _holdStart;
        private static Vector3 _holdFrom;
        private static bool _toldStuck;

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                bool blocked = !focus.MoveStickWalks;
                InputBridge.WalkBlocked = blocked;

                int pressed = InputBridge.MovePressed();
                if (pressed != 0 && blocked)
                {
                    Il2CppBackend.M_Player p = focus.PlayerModel;
                    string why = focus.InCombat ? " You are in a fight."
                        : p != null && p.IsInExploreEncounter ? " Use your cards on what is nearby, or press Space to end the turn."
                        : " Something nearby needs dealing with first.";
                    FocusProbe.Speak("Can't walk right now." + why);
                    return;
                }

                // Walking is allowed but the character is not moving: the path is closed that way.
                bool held = InputBridge.MoveHeld && !blocked;
                V_Player player = focus.Player;
                if (held && player != null)
                {
                    Vector3 at = player.transform.position;
                    if (!_holding) { _holding = true; _holdStart = Time.realtimeSinceStartup; _holdFrom = at; _toldStuck = false; }
                    else if (!_toldStuck && Time.realtimeSinceStartup - _holdStart > 0.9f)
                    {
                        if ((at - _holdFrom).sqrMagnitude < 0.02f)
                        {
                            _toldStuck = true;
                            Cue.Play(Cue.Edge);
                            string text = InputBridge.MoveY < 0 ? "Can't go back from here." : "Can't go further this way.";
                            Log("[say-direct] " + text);
                            if (Application.isFocused) Speaker.SayDirect(text);
                        }
                        else { _holdStart = Time.realtimeSinceStartup; _holdFrom = at; }
                    }
                }
                else _holding = false;
            }
            catch (Exception ex) { Log("[walk] " + ex.GetType().Name + ": " + ex.Message); }
        }
    }
}
