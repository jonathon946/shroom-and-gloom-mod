using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace ShroomAccess
{
    // How long the mod's own work takes each frame, part by part. Every frame the mod spends time
    // in is a frame the game waits for, and a slow frame is a key press acted on late. A summary
    // goes to the log every half minute, so a tester's log shows whether the mod is the lag.
    internal static class Perf
    {
        internal static Action<string> Log = _ => { };

        private static readonly Stopwatch _watch = new Stopwatch();
        private static readonly Dictionary<string, double> _total = new Dictionary<string, double>();
        private static readonly Dictionary<string, double> _worst = new Dictionary<string, double>();
        private static double _lap, _frameModWorst, _frameModTotal, _gameFrameWorst, _gameFrameTotal;
        private static int _frames;
        private static float _next;

        internal static void BeginFrame()
        {
            _watch.Restart();
            _lap = 0;
        }

        // The time since the last lap is charged to this part.
        internal static void Lap(string part)
        {
            double now = _watch.Elapsed.TotalMilliseconds;
            double spent = now - _lap;
            _lap = now;
            _total.TryGetValue(part, out double t);
            _total[part] = t + spent;
            if (!_worst.TryGetValue(part, out double w) || spent > w) _worst[part] = spent;
        }

        internal static void EndFrame()
        {
            double mod = _watch.Elapsed.TotalMilliseconds;
            _frames++;
            _frameModTotal += mod;
            if (mod > _frameModWorst) _frameModWorst = mod;
            double frame = Time.unscaledDeltaTime * 1000.0;
            _gameFrameTotal += frame;
            if (frame > _gameFrameWorst) _gameFrameWorst = frame;
            if (mod > 50) Log("[perf] slow frame: mod " + mod.ToString("0") + " ms");

            float real = Time.realtimeSinceStartup;
            if (_next == 0f) _next = real + 30f;
            if (real < _next) return;
            _next = real + 30f;
            var parts = _total.OrderByDescending(p => p.Value).Take(6)
                .Select(p => p.Key + " " + (p.Value / _frames).ToString("0.0") + "/" + _worst[p.Key].ToString("0"));
            Log("[perf] " + _frames + " frames, frame avg " + (_gameFrameTotal / _frames).ToString("0") + " ms worst " + _gameFrameWorst.ToString("0")
                + "; mod avg " + (_frameModTotal / _frames).ToString("0.0") + " worst " + _frameModWorst.ToString("0")
                + "; parts avg/worst ms: " + string.Join(", ", parts) + Memory());
            _total.Clear(); _worst.Clear();
            _frames = 0; _frameModTotal = 0; _frameModWorst = 0; _gameFrameTotal = 0; _gameFrameWorst = 0;
        }

        // Memory, so a long session's log shows whether something is piling up: the game's own
        // heap (used of reserved), the mod's heap, and how many collections each has run.
        private static bool _memoryFailed;

        private static string Memory()
        {
            if (_memoryFailed) return "";
            try
            {
                const double mb = 1024.0 * 1024.0;
                return "; memory MB: game " + (UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / mb).ToString("0")
                    + " of " + (UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / mb).ToString("0")
                    + " gc " + Il2CppSystem.GC.GetCollectionCount(0)
                    + ", mod " + (GC.GetTotalMemory(false) / mb).ToString("0") + " gc " + GC.CollectionCount(0);
            }
            catch (Exception ex)
            {
                _memoryFailed = true;
                return "; memory unavailable: " + ex.GetType().Name;
            }
        }
    }
}
