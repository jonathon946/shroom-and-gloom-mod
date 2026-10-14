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

        internal static string Scene = "";
        private static bool _wasBlocked;
        private static bool _noteDone;
        private static float _noteCheckAt;

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                bool blocked = !focus.MoveStickWalks;
                InputBridge.WalkBlocked = blocked;

                // The game has halted the walk while the key is still down.
                if (blocked && !_wasBlocked && InputBridge.MoveHeld) FocusProbe.Stopped();
                _wasBlocked = blocked;

                // A new game opens on a hand holding up a note: a picture of a stick pushed forward.
                // It has no words, so it is said once, until the player has taken a step.
                if (!_noteDone && !blocked && Scene.Contains("PT_T00") && Time.realtimeSinceStartup > _noteCheckAt)
                {
                    _noteCheckAt = Time.realtimeSinceStartup + 1f;
                    S_InitialTutorial tutorial = UnityEngine.Object.FindObjectOfType<S_InitialTutorial>();
                    if (tutorial != null && (tutorial.IsOver || tutorial._hasSteppedForward)) _noteDone = true;
                    else if (tutorial != null && tutorial._isActive && focus.PlayerModel != null)
                    {
                        _noteDone = true;
                        FocusProbe.Announce(Loc.T("walk.tutorial_note", "A hand holds up a note with a picture: push forward. Hold W to walk."));
                    }
                }

                int pressed = InputBridge.MovePressed();
                if (pressed != 0 && blocked)
                {
                    Il2CppBackend.M_Player p = focus.PlayerModel;
                    string why = Hud.InFight(focus) ? Loc.T("walk.in_fight", "You are in a fight.")
                        : focus.Scope != null ? Loc.T("walk.screen_open", "Finish with this screen first.")
                        : p != null && p.IsInExploreEncounter ? Loc.T("walk.camp", "Use your cards on what is nearby, or press Space to end the turn.")
                        : Loc.T("walk.something_nearby", "Something nearby needs dealing with first.");
                    // In the voice a held key cannot silence: the player is very likely still holding W.
                    string blockedText = Loc.T("walk.blocked", "Can't walk right now.") + " " + why;
                    Log("[say-direct] " + blockedText);
                    Cue.Play(Cue.Edge);
                    if (Application.isFocused) Speaker.SayDirect(blockedText, interrupt: true);
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
                            string text = (InputBridge.MoveY < 0 ? Loc.T("walk.cant_back", "Can't go back from here.") : Loc.T("walk.cant_forward", "Can't go further this way.")) + FocusProbe.TakeSeen();
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
