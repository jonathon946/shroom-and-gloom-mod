using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ShroomAccess
{
    // The mod's own short sounds. They are generated, not shipped as files, and played flat (not
    // positioned in the world) on an object that survives scene changes.
    internal static class Cue
    {
        internal static Action<string> Log = _ => { };

        internal const int Arrival = 0;
        internal const int Edge = 1;

        private static AudioSource _source;
        private static AudioClip[] _clips;
        private static int _failures;

        internal static void Play(int cue)
        {
            if (_failures >= 3) return;
            try
            {
                if (_source == null || _clips == null || _clips[cue] == null) Build();
                if (!Application.isFocused) return;
                _source.PlayOneShot(_clips[cue], Settings.CueVolume);
                _failures = 0;
            }
            catch (Exception ex)
            {
                // A scene load can unload the clips or the host; build them again next time. Only
                // repeated failures in a row switch the sounds off.
                _failures++;
                _source = null;
                _clips = null;
                Log("[cue] " + (_failures >= 3 ? "disabled: " : "rebuilding: ") + ex.GetType().Name + ": " + ex.Message);
                if (_failures < 3) Play(cue);
            }
        }

        private static void Build()
        {
            var host = new GameObject("ShroomAccessCues");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            _source = host.AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.ignoreListenerPause = true;
            _clips = new[]
            {
                // Something has come into reach: two quick rising notes.
                Tones("ShroomAccessArrival", new[] { 660f, 990f }, 0.07f),
                // The end of a row, or a way that is closed: one low, dull note.
                Tones("ShroomAccessEdge", new[] { 180f }, 0.09f),
            };
        }

        private static AudioClip Tones(string name, float[] frequencies, float secondsEach)
        {
            const int rate = 44100;
            int per = (int)(rate * secondsEach);
            var data = new float[per * frequencies.Length];
            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < per; i++)
                {
                    // A short fade at each end keeps the note from clicking.
                    float edge = Math.Min(1f, Math.Min(i, per - 1 - i) / (rate * 0.008f));
                    data[n * per + i] = (float)Math.Sin(2 * Math.PI * frequencies[n] * i / rate) * 0.6f * edge;
                }
            }
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(new Il2CppStructArray<float>(data), 0);
            // Not owned by any scene: without this the game's asset clean-up on a scene load destroys it.
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }
    }
}
