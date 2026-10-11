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

        internal static void Say(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            lock (Gate)
            {
                if (!Ready) return;
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
