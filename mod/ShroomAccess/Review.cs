using System;
using System.IO;
using Il2Cpp;
using Il2CppControllerNav;
using UnityEngine;

namespace ShroomAccess
{
    // Makes a play session reviewable afterwards. Every key the mod acts on is written to the log
    // beside what was spoken, and Ctrl+B drops a numbered mark: a line in the log, a picture of the
    // screen and a dump of the game's state at that moment, so the player can say "look at mark 3"
    // instead of describing something they could not see.
    internal static class Review
    {
        internal static Action<string> Log = _ => { };

        private static string _dir;
        private static int _marks;

        internal static void Init(string userDataDir)
        {
            _dir = Path.Combine(userDataDir, "ShroomAccess", "marks", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }

        internal static void Tick(S_ControllerFocus focus)
        {
            try
            {
                bool ctrl = InputBridge.CtrlHeld;
                foreach (InputActionId id in InputBridge.MappedIds)
                    if (InputBridge.Key(id))
                    {
                        InputBridge.LastKeyAt = Time.realtimeSinceStartup;
                        if (InputBridge.IsArrowKey(id)) InputBridge.LastArrowAt = Time.realtimeSinceStartup;
                        if (id == InputActionId.Confirm) InputBridge.LastConfirmAt = Time.realtimeSinceStartup;
                        Log("[key] " + (ctrl ? "Ctrl+" : "") + id);
                    }

                if (ctrl && InputBridge.NamedKey("b")) Mark(focus);
            }
            catch (Exception ex) { Log("[review] " + ex.GetType().Name + ": " + ex.Message); }
        }

        private static void Mark(S_ControllerFocus focus)
        {
            _marks++;
            Directory.CreateDirectory(_dir);
            string stem = Path.Combine(_dir, "mark-" + _marks);
            ScreenCapture.CaptureScreenshot(stem + ".png");
            File.WriteAllText(stem + ".txt", FocusProbe.StateDump());
            Log("[mark] " + _marks + " saved to " + stem);
            FocusProbe.Announce(Loc.T("review.mark", "Mark {0}.", _marks));
        }
    }
}
