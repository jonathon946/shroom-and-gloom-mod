using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ShroomAccess.Speech
{
    // The one place that talks to a screen reader. Everything else calls Speaker.Say.
    // A Prism backend is not thread-safe, so every call is made under one lock.
    internal static class Speaker
    {
        private static readonly object Gate = new object();
        private static IntPtr _ctx;
        private static IntPtr _backend;
        private static ulong _features;

        internal static bool Ready { get; private set; }
        internal static string BackendName { get; private set; } = "none";

        internal static Action<string> Log = _ => { };

        // backendOverride: null for the best available backend, or a Prism backend name such as "SAPI".
        internal static bool Init(string prismPath, string backendOverride = null)
        {
            lock (Gate)
            {
                if (Ready) return true;
                try
                {
                    if (!File.Exists(prismPath))
                    {
                        Log("Speech: prism.dll not found at " + prismPath);
                        return false;
                    }
                    // Load by full path first, so the "prism" imports resolve to this copy and no other.
                    NativeLibrary.Load(prismPath);

                    var cfg = PrismNative.prism_config_init();
                    _ctx = PrismNative.prism_init(ref cfg);
                    if (_ctx == IntPtr.Zero)
                    {
                        Log("Speech: prism_init returned no context.");
                        return false;
                    }

                    if (!string.IsNullOrEmpty(backendOverride))
                    {
                        ulong id = PrismNative.prism_registry_id(_ctx, Utf8(backendOverride));
                        if (id != 0) _backend = PrismNative.prism_registry_create(_ctx, id);
                        if (_backend == IntPtr.Zero) Log("Speech: backend '" + backendOverride + "' is not available; using the best one.");
                    }
                    if (_backend == IntPtr.Zero) _backend = PrismNative.prism_registry_create_best(_ctx);
                    if (_backend == IntPtr.Zero)
                    {
                        Log("Speech: Prism found no usable backend.");
                        return false;
                    }

                    int e = PrismNative.prism_backend_initialize(_backend);
                    if (e != PrismNative.Ok && e != PrismNative.ErrorAlreadyInitialized)
                    {
                        Log("Speech: backend initialise failed: " + ErrorText(e));
                        return false;
                    }

                    _features = PrismNative.prism_backend_get_features(_backend);
                    BackendName = PtrToUtf8(PrismNative.prism_backend_name(_backend)) ?? "?";
                    Ready = true;
                    OpenDirectVoice();
                    Log("Speech: Prism " + (PtrToUtf8(PrismNative.prism_version_string()) ?? "?")
                        + ", backend " + BackendName + ", features 0x" + _features.ToString("X"));
                    return true;
                }
                catch (Exception ex)
                {
                    Log("Speech: Prism failed to start: " + ex);
                    return false;
                }
            }
        }

        // The two voices never speak at once. The screen reader cannot be asked whether it is
        // still talking, so its lines are timed from their length; the direct voice can be asked.
        //   asked: the player pressed a key for this line. It is said at once and the direct
        //          voice gives way (its line is said again afterwards).
        //   not asked: the line waits until the direct voice has finished.
        private static long _mainBusyUntil;
        // Set every frame: a walk key is down. The direct voice exists for that time only; once
        // the key is up, whatever it had not yet said is given to the screen reader.
        internal static bool WalkHeld;
        private static readonly System.Collections.Generic.Queue<string> _mainDeferred = new System.Collections.Generic.Queue<string>();
        private static string _directCurrent;
        private static int _directCurrentCuts;

        internal static void Say(string text, bool interrupt = true, bool asked = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            lock (Gate)
            {
                if (!Ready) return;
                if (_direct != IntPtr.Zero)
                {
                    if (asked) HushDirect();
                    else if (DirectActive())
                    {
                        if (_mainDeferred.Count < 12) _mainDeferred.Enqueue(text);
                        return;
                    }
                }
                SayNow(text, interrupt);
            }
        }

        private static bool DirectActive()
        {
            if (_direct == IntPtr.Zero) return false;
            if (_directQueue.Count > 0 || Environment.TickCount64 - _directStartedAt < 250) return true;
            try { return PrismNative.prism_backend_is_speaking(_direct, out bool speaking) == PrismNative.Ok && speaking; }
            catch { return false; }
        }

        // Stop the direct voice and put what it was saying back at the head of its queue.
        private static void HushDirect()
        {
            try
            {
                bool speaking = Environment.TickCount64 - _directStartedAt < 250
                    || (PrismNative.prism_backend_is_speaking(_direct, out bool s) == PrismNative.Ok && s);
                if (!speaking) return;
                PrismNative.prism_backend_stop(_direct);
                _directStartedAt = 0;
                if (_directCurrent != null && _directCurrentCuts++ < 2)
                {
                    var rest = new System.Collections.Generic.List<string>(_directQueue);
                    _directQueue.Clear();
                    _directQueue.Enqueue(_directCurrent);
                    foreach (string line in rest) _directQueue.Enqueue(line);
                }
                _directCurrent = null;
            }
            catch (Exception ex) { Log("Speech: hush threw: " + ex.Message); }
        }

        private static void SayNow(string text, bool interrupt)
        {
            {
                long now = Environment.TickCount64;
                long length = Math.Min(12000L, 400L + 45L * text.Length);
                _mainBusyUntil = (interrupt ? now : Math.Max(now, _mainBusyUntil)) + length;
                try
                {
                    byte[] utf8 = Utf8(text);
                    // "output" is speech plus braille; fall back to plain speech where a backend lacks it.
                    int e = (_features & PrismNative.SupportsOutput) != 0
                        ? PrismNative.prism_backend_output(_backend, utf8, interrupt)
                        : PrismNative.prism_backend_speak(_backend, utf8, interrupt);
                    if (e != PrismNative.Ok) Log("Speech: say failed: " + ErrorText(e));
                }
                catch (Exception ex)
                {
                    Log("Speech: say threw: " + ex.Message);
                }
            }
        }

        // ---- The direct voice ----
        // A screen reader cancels its own speech on every key press, and a held key repeats, so
        // nothing said through the screen reader survives while the player holds a walk key.
        // Lines that must be heard then go to a Windows voice driven directly, which no screen
        // reader can cancel. When the main backend is already a Windows voice there is no need.
        private static IntPtr _direct;
        internal static string DirectName { get; private set; } = "none";
        internal static bool HasDirect => _direct != IntPtr.Zero;

        private static void OpenDirectVoice()
        {
            if (BackendName == "OneCore" || BackendName == "SAPI") return;
            foreach (string name in new[] { "OneCore", "SAPI" })
            {
                try
                {
                    ulong id = PrismNative.prism_registry_id(_ctx, Utf8(name));
                    if (id == 0) continue;
                    IntPtr backend = PrismNative.prism_registry_create(_ctx, id);
                    if (backend == IntPtr.Zero) continue;
                    int e = PrismNative.prism_backend_initialize(backend);
                    if (e != PrismNative.Ok && e != PrismNative.ErrorAlreadyInitialized)
                    {
                        PrismNative.prism_backend_free(backend);
                        continue;
                    }
                    _direct = backend;
                    DirectName = name;
                    Log("Speech: direct voice " + name + " ready.");
                    return;
                }
                catch (Exception ex) { Log("Speech: direct voice " + name + " failed: " + ex.Message); }
            }
            Log("Speech: no direct voice available.");
        }

        // rate: 0 slowest, 0.5 the voice's normal speed, 1 fastest.
        internal static void SetDirectRate(float rate)
        {
            lock (Gate)
            {
                if (_direct == IntPtr.Zero) return;
                try { PrismNative.prism_backend_set_rate(_direct, Math.Max(0f, Math.Min(1f, rate))); }
                catch (Exception ex) { Log("Speech: direct rate failed: " + ex.Message); }
            }
        }

        // Speaks through the direct voice; through the main backend when there is none.
        // Lines wait their turn: the Windows voices cut the current line when given another, so
        // the queue is kept here and each line starts only when the one before has finished.
        private static readonly System.Collections.Generic.Queue<string> _directQueue = new System.Collections.Generic.Queue<string>();
        private static long _directStartedAt;

        internal static void SayDirect(string text, bool interrupt = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            lock (Gate)
            {
                if (!Ready) return;
                if (_direct == IntPtr.Zero) { Say(text, false); return; }
                if (interrupt) _directQueue.Clear();
                if (_directQueue.Count < 12 && !_directQueue.Contains(text)) _directQueue.Enqueue(text);
            }
        }

        // Call once a frame.
        internal static void PumpDirect()
        {
            lock (Gate)
            {
                if (!Ready || _direct == IntPtr.Zero) return;
                if (_directQueue.Count == 0 && _mainDeferred.Count == 0) return;
                try
                {
                    // Just after a line is handed over the voice may not report speaking yet.
                    if (Environment.TickCount64 - _directStartedAt < 250) return;
                    if (PrismNative.prism_backend_is_speaking(_direct, out bool speaking) == PrismNative.Ok && speaking) return;
                    if (_directQueue.Count > 0 && !WalkHeld)
                    {
                        // The walk is over: the screen reader can be heard again, so it takes the rest.
                        var rest = new System.Collections.Generic.List<string>(_directQueue);
                        _directQueue.Clear();
                        foreach (string line in _mainDeferred) rest.Add(line);
                        _mainDeferred.Clear();
                        foreach (string line in rest) { Log("[handed-over] " + line); SayNow(line, false); }
                        return;
                    }
                    if (_directQueue.Count == 0)
                    {
                        // The direct voice has finished: now the lines that waited for it.
                        var waited = new System.Collections.Generic.List<string>(_mainDeferred);
                        _mainDeferred.Clear();
                        foreach (string line in waited) SayNow(line, false);
                        return;
                    }
                    string text = _directQueue.Dequeue();
                    if (text != _directCurrent) _directCurrentCuts = 0;
                    _directCurrent = text;
                    int e = PrismNative.prism_backend_speak(_direct, Utf8(text), false);
                    _directStartedAt = Environment.TickCount64;
                    if (e != PrismNative.Ok) Log("Speech: direct say failed: " + ErrorText(e));
                }
                catch (Exception ex) { Log("Speech: direct say threw: " + ex.Message); }
            }
        }

        internal static void Stop()
        {
            lock (Gate)
            {
                if (!Ready) return;
                try { PrismNative.prism_backend_stop(_backend); }
                catch (Exception ex) { Log("Speech: stop threw: " + ex.Message); }
            }
        }

        internal static void Shutdown()
        {
            lock (Gate)
            {
                if (!Ready) return;
                Ready = false;
                try
                {
                    if (_direct != IntPtr.Zero) PrismNative.prism_backend_free(_direct);
                    _direct = IntPtr.Zero;
                    PrismNative.prism_backend_free(_backend);
                    PrismNative.prism_shutdown(_ctx);
                }
                catch (Exception ex) { Log("Speech: shutdown threw: " + ex.Message); }
                _backend = IntPtr.Zero;
                _ctx = IntPtr.Zero;
            }
        }

        // Prism rejects invalid UTF-8; the encoder replaces lone surrogates, so the bytes are always valid.
        private static byte[] Utf8(string s)
        {
            int n = Encoding.UTF8.GetByteCount(s);
            var bytes = new byte[n + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
            return bytes;
        }

        private static string PtrToUtf8(IntPtr p) => p == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(p);

        private static string ErrorText(int e) => (PtrToUtf8(PrismNative.prism_error_string(e)) ?? "error") + " (" + e + ")";
    }
}
