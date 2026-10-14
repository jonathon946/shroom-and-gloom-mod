using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using ShroomAccess.Speech;

[assembly: MelonInfo(typeof(ShroomAccess.Main), "Shroom Access", ShroomAccess.UpdateCheck.Version, "Shroom Access contributors")]
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
            Hud.Log = msg => LoggerInstance.Msg(msg);
            Walk.Log = msg => LoggerInstance.Msg(msg);
            MapReader.Log = msg => LoggerInstance.Msg(msg);
            Rewards.Log = msg => LoggerInstance.Msg(msg);
            Soup.Log = msg => LoggerInstance.Msg(msg);
            DeckView.Log = msg => LoggerInstance.Msg(msg);
            CardPlay.Log = msg => LoggerInstance.Msg(msg);
            TextWatch.Log = msg => LoggerInstance.Msg(msg);
            Review.Log = msg => LoggerInstance.Msg(msg);
            Review.Init(MelonEnvironment.UserDataDirectory);
            AltText.Log = msg => LoggerInstance.Msg(msg);
            AltText.Init(MelonEnvironment.ModsDirectory);
            if (DevBridge.Enabled) AltText.EnableShots(MelonEnvironment.UserDataDirectory);
            InputBridge.Install(HarmonyInstance);
            Messages.Log = msg => LoggerInstance.Msg(msg);
            Messages.Install(HarmonyInstance);

            // prism.dll ships in the loader's UserLibs folder.
            string prism = Path.Combine(MelonEnvironment.UserLibsDirectory, "prism.dll");
            // SHROOMACCESS_BACKEND=SAPI forces a Windows voice; used for dev tests where no one is listening.
            string backend = Environment.GetEnvironmentVariable("SHROOMACCESS_BACKEND");

            Loc.Log = msg => LoggerInstance.Msg(msg);
            Loc.Load(Path.Combine(MelonEnvironment.ModsDirectory, "ShroomAccess", "strings.txt"));
            Settings.Log = msg => LoggerInstance.Msg(msg);
            Cue.Log = msg => LoggerInstance.Msg(msg);
            if (Speaker.Init(prism, backend))
            {
                LoggerInstance.Msg("Speech ready through " + Speaker.BackendName + ".");
                Settings.Load();
                UpdateCheck.Log = msg => LoggerInstance.Msg(msg);
                if (Settings.CheckForUpdates) UpdateCheck.Start(Settings.UpdateRepository);
            }
            else
                LoggerInstance.Error("Speech is not available. The mod will stay silent.");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg("Scene initialised: " + buildIndex + " " + sceneName);
            Walk.Scene = sceneName ?? "";
            FocusProbe.SceneChanged(sceneName);
            if (Walk.Scene.Contains("DeathWorld") && UnityEngine.Time.realtimeSinceStartup - Hud.DeathSaidAt > 60f) FocusProbe.Announce("You have died.", interrupt: false);
            if (_greeted || !Speaker.Ready) return;
            if (sceneName == null || sceneName.IndexOf("Title", StringComparison.OrdinalIgnoreCase) < 0) return;
            _greeted = true;
            FocusProbe.ArmedAt = UnityEngine.Time.realtimeSinceStartup + 3f;
            // The start-up screen is only the game's logo on black, and it is gone in seconds; the
            // title screen is described once so the player has the picture a sighted player gets.
            FocusProbe.Announce("Shroom Access loaded. Shroom and Gloom: the title drips in slimy green and frosty blue letters over a black, star-speckled splat. Below it, three doorways stand side by side: a round steel vault door chained shut, a wooden door wrapped in thorny vines, and a padlocked cave mouth. Control Left and Right switch buffers; the last one is Help.", interrupt: false);
        }

        public override void OnUpdate()
        {
            TextWatch.Tick();
            ShroomAccess.Speech.Speaker.PumpDirect();
            FocusProbe.Tick();
            WorldAssist.Tick();
            DevBridge.Tick();
            UpdateCheck.Tick(Walk.Scene.Contains("Title"));
        }

        public override void OnDeinitializeMelon()
        {
            Speaker.Shutdown();
        }
    }
}
