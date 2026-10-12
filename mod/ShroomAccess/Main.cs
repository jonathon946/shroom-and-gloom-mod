using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using ShroomAccess.Speech;

[assembly: MelonInfo(typeof(ShroomAccess.Main), "Shroom Access", "0.0.1", "Shroom Access contributors")]
[assembly: MelonGame("Team Lazerbeam", "Shroom and Gloom")]

namespace ShroomAccess
{
    // Spike 1: prove the loader runs this code inside the game and that Prism speaks.
    public class Main : MelonMod
    {
        private bool _greeted;

        public override void OnInitializeMelon()
        {
            Speaker.Log = msg => LoggerInstance.Msg(msg);
            FocusProbe.Log = msg => LoggerInstance.Msg(msg);
            DevBridge.Log = msg => LoggerInstance.Msg(msg);
            DevBridge.Init(MelonEnvironment.UserDataDirectory);
            InputBridge.Log = msg => LoggerInstance.Msg(msg);
            WorldAssist.Log = msg => LoggerInstance.Msg(msg);
            Navigator.Log = msg => LoggerInstance.Msg(msg);
            InputBridge.Install(HarmonyInstance);

            // prism.dll ships in the loader's UserLibs folder.
            string prism = Path.Combine(MelonEnvironment.UserLibsDirectory, "prism.dll");
            // SHROOMACCESS_BACKEND=SAPI forces a Windows voice; used for dev tests where no one is listening.
            string backend = Environment.GetEnvironmentVariable("SHROOMACCESS_BACKEND");

            if (Speaker.Init(prism, backend))
                LoggerInstance.Msg("Speech ready through " + Speaker.BackendName + ".");
            else
                LoggerInstance.Error("Speech is not available. The mod will stay silent.");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg("Scene initialised: " + buildIndex + " " + sceneName);
            if (_greeted || !Speaker.Ready) return;
            if (sceneName == null || sceneName.IndexOf("Title", StringComparison.OrdinalIgnoreCase) < 0) return;
            _greeted = true;
            FocusProbe.ArmedAt = UnityEngine.Time.realtimeSinceStartup + 3f;
            FocusProbe.Announce("Shroom Access loaded.");
        }

        public override void OnUpdate()
        {
            FocusProbe.Tick();
            WorldAssist.Tick();
            DevBridge.Tick();
        }

        public override void OnDeinitializeMelon()
        {
            Speaker.Shutdown();
        }
    }
}
