using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using ShroomAccess.Speech;

namespace ShroomAccess
{
    // Player settings, kept in the loader's preferences file (UserData\MelonPreferences.cfg).
    internal static class Settings
    {
        internal static Action<string> Log = _ => { };

        private static MelonPreferences_Entry<float> _directRate;
        private static MelonPreferences_Entry<float> _cueVolume;
        private static MelonPreferences_Entry<bool> _checkUpdates;
        private static MelonPreferences_Entry<string> _repository;

        internal static bool CheckForUpdates => _checkUpdates == null || _checkUpdates.Value;
        internal static string UpdateRepository => _repository != null ? _repository.Value : "";

        internal static float CueVolume => _cueVolume != null ? _cueVolume.Value : 0.5f;

        internal static void Load()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory("ShroomAccess");
            _directRate = category.CreateEntry("WalkingVoiceRate", -1f, "Walking voice rate",
                "Speed of the voice used while a walk key is held. 0 slowest, 0.5 normal, 1 fastest. -1 follows the screen reader's rate where it can be read.");
            _cueVolume = category.CreateEntry("CueVolume", 0.5f, "Cue volume", "Loudness of the mod's own sounds, 0 to 1.");
            _checkUpdates = category.CreateEntry("CheckForUpdates", true, "Check for updates",
                "Once at start, ask GitHub whether a newer version of the mod has been released. Nothing is downloaded.");
            _repository = category.CreateEntry("UpdateRepository", "jonathon946/shroom-and-gloom-mod", "Update repository",
                "The GitHub project (owner/name) whose releases are checked.");

            LoadKeys();

            float rate = _directRate.Value;
            if (rate < 0f)
            {
                rate = ScreenReaderRate();
                Log("Walking voice rate follows the screen reader: " + rate.ToString("F2"));
            }
            Speaker.SetDirectRate(rate);
        }

        // Every key the mod listens to, in its own section of the preferences file: one entry per
        // action, keys separated by commas, named as the Input System names them ("leftArrow",
        // "enter", "w"). A name it does not know is logged and the default kept, so no action can
        // be left without a key by a typing mistake.
        private static void LoadKeys()
        {
            MelonPreferences_Category keys = MelonPreferences.CreateCategory("ShroomAccessKeys", "Shroom Access keys");
            foreach (var id in new List<Il2Cpp.InputActionId>(InputBridge.Map.Keys))
                InputBridge.Map[id] = Read(keys, id.ToString(), InputBridge.Map[id]);
            foreach (string name in new List<string>(InputBridge.WalkKeys.Keys))
                InputBridge.WalkKeys[name] = Read(keys, name, InputBridge.WalkKeys[name]);
            foreach (string name in new List<string>(InputBridge.CommandKeys.Keys))
                InputBridge.CommandKeys[name] = Read(keys, "CtrlPlus" + name, InputBridge.CommandKeys[name]);
        }

        private static string[] Read(MelonPreferences_Category keys, string name, string[] defaults)
        {
            MelonPreferences_Entry<string> entry = keys.CreateEntry(name, string.Join(", ", defaults));
            var chosen = new List<string>();
            foreach (string raw in (entry.Value ?? "").Split(','))
            {
                string key = raw.Trim();
                if (key.Length == 0) continue;
                if (InputBridge.IsKeyName(key)) chosen.Add(key);
                else Log("Keys: \"" + key + "\" for " + name + " is not a key name; ignored.");
            }
            if (chosen.Count == 0) { Log("Keys: " + name + " has no usable key; using " + string.Join(", ", defaults)); return defaults; }
            return chosen.ToArray();
        }

        internal static void NudgeDirectRate(int direction)
        {
            if (_directRate == null || !Speaker.HasDirect) return;
            float current = _directRate.Value < 0f ? ScreenReaderRate() : _directRate.Value;
            float next = Math.Max(0f, Math.Min(1f, current + 0.05f * direction));
            _directRate.Value = next;
            MelonPreferences.Save();
            Speaker.SetDirectRate(next);
            Speaker.SayDirect(Loc.T("settings.voice_rate", "Walking voice rate {0} percent.", (int)Math.Round(next * 100)));
        }

        // The rate the player has chosen in their screen reader, as 0 to 1, or 0.5 when unknown.
        // Only NVDA keeps it somewhere a program can read: its configuration file. Synthesizers
        // differ, so the same number is not the same speed; this is a starting point the player can
        // adjust, not a match.
        private static float ScreenReaderRate()
        {
            try
            {
                if (Speaker.BackendName != "NVDA") return 0.5f;
                string ini = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nvda", "nvda.ini");
                if (!File.Exists(ini)) return 0.5f;
                string synth = null;
                bool inSpeech = false, inSynth = false;
                foreach (string raw in File.ReadAllLines(ini))
                {
                    string line = raw.Trim();
                    if (line.StartsWith("[") && !line.StartsWith("[["))
                    {
                        inSpeech = line == "[speech]";
                        inSynth = false;
                        continue;
                    }
                    if (!inSpeech) continue;
                    if (line.StartsWith("[["))
                    {
                        inSynth = synth != null && line == "[[" + synth + "]]";
                        continue;
                    }
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key = line.Substring(0, eq).Trim(), value = line.Substring(eq + 1).Trim();
                    if (!inSynth && key == "synth") synth = value;
                    else if (inSynth && key == "rate" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float percent))
                        return Math.Max(0f, Math.Min(1f, percent / 100f));
                }
            }
            catch (Exception ex) { Log("Could not read the screen reader's rate: " + ex.Message); }
            return 0.5f;
        }
    }
}
