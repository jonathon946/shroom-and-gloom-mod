using System;
using System.Runtime.InteropServices;

namespace ShroomAccess.Speech
{
    // P/Invoke surface for Prism (https://github.com/ethindp/prism, MPL-2.0).
    // Declarations follow prism.h from release v0.18.3: __cdecl, UTF-8 char*, one-byte bool.
    internal static class PrismNative
    {
        private const string Lib = "prism";

        internal const int Ok = 0;
        internal const int ErrorAlreadyInitialized = 15;

        internal const ulong SupportsSpeak = 1UL << 2;
        internal const ulong SupportsBraille = 1UL << 4;
        internal const ulong SupportsOutput = 1UL << 5;
        internal const ulong SupportsIsSpeaking = 1UL << 6;
        internal const ulong SupportsStop = 1UL << 7;

        [StructLayout(LayoutKind.Sequential)]
        internal struct PrismConfig
        {
            public byte version;
            public IntPtr registry;
            public IntPtr availability_callback;
            public IntPtr availability_userdata;
            public uint availability_poll_interval_ms;
            public uint availability_debounce_samples;
            public uint availability_backoff_max_ms;
            public byte availability_auto_power_manage;
            public IntPtr availability_baseline_callback;
        }

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern PrismConfig prism_config_init();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr prism_init(ref PrismConfig cfg);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void prism_shutdown(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr prism_registry_create_best(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong prism_registry_id(IntPtr ctx, byte[] nameUtf8);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr prism_registry_create(IntPtr ctx, ulong id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void prism_backend_free(IntPtr backend);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr prism_backend_name(IntPtr backend);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong prism_backend_get_features(IntPtr backend);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int prism_backend_initialize(IntPtr backend);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int prism_backend_speak(IntPtr backend, byte[] textUtf8, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int prism_backend_output(IntPtr backend, byte[] textUtf8, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int prism_backend_stop(IntPtr backend);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr prism_error_string(int error);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr prism_version_string();
    }
}
