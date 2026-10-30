using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Logging;

namespace MonsterPromAccessibility.Speech.Providers
{
    // Prism (https://github.com/ethindp/prism) — screen reader / TTS abstraction.
    // Prism picks the best backend: a running screen reader (NVDA, JAWS, ...) first,
    // otherwise Windows TTS (SAPI / OneCore).
    //
    // The prismatoid .NET bindings need a newer runtime than Unity Mono, so this is a
    // minimal P/Invoke wrapper over Prism's C API. prism.dll (32-bit) is loaded via
    // LoadLibrary with the full path to the plugins folder first, so the [DllImport]
    // calls below resolve to that already-loaded module.
    public class PrismProvider : ISpeechProvider
    {
        private const string PrismLib = "prism.dll";
        private const int PRISM_OK = 0;
        private const int PRISM_ERROR_ALREADY_INITIALIZED = 15;
        private const ulong PRISM_BACKEND_SUPPORTS_OUTPUT = 1UL << 5;
        private const ulong PRISM_BACKEND_SUPPORTS_STOP = 1UL << 7;
        private const ulong PRISM_BACKEND_SUPPORTS_SET_RATE = 1UL << 12;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_init(IntPtr cfg);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void prism_shutdown(IntPtr ctx);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UIntPtr prism_registry_count(IntPtr ctx);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong prism_registry_id_at(IntPtr ctx, UIntPtr index);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_name(IntPtr ctx, ulong id);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_acquire(IntPtr ctx, ulong id);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_acquire_best(IntPtr ctx);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void prism_backend_free(IntPtr backend);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_backend_name(IntPtr backend);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong prism_backend_get_features(IntPtr backend);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_backend_initialize(IntPtr backend);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_backend_speak(IntPtr backend, byte[] text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_backend_output(IntPtr backend, byte[] text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_backend_stop(IntPtr backend);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_backend_set_rate(IntPtr backend, float rate);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_error_string(int error);

        private readonly ManualLogSource _log;
        private readonly string _preferredBackend;
        private IntPtr _ctx = IntPtr.Zero;
        private IntPtr _backend = IntPtr.Zero;
        private ulong _features;
        private bool _available;
        private int _rate;

        public string Name => "Prism";

        // Name of the Prism backend actually in use, e.g. "NVDA" or "SAPI".
        public string BackendName { get; private set; } = "none";

        public bool SupportsRate => (_features & PRISM_BACKEND_SUPPORTS_SET_RATE) != 0;

        // preferredBackend: Prism backend name (e.g. "NVDA", "JAWS", "SAPI", "OneCore"),
        // or blank for Prism's automatic choice.
        // rate: -10 (slowest) to 10 (fastest), 0 = backend default. Only applies to TTS backends.
        public PrismProvider(ManualLogSource log, string preferredBackend, int rate)
        {
            _log = log;
            _preferredBackend = preferredBackend ?? string.Empty;
            _rate = Math.Max(-10, Math.Min(10, rate));
        }

        public bool IsAvailable()
        {
            if (_available) return true;
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(dir, PrismLib);
                if (LoadLibraryW(path) == IntPtr.Zero)
                {
                    _log.LogWarning($"[Prism] LoadLibrary failed for: {path} (error {Marshal.GetLastWin32Error()})");
                    return false;
                }

                _ctx = prism_init(IntPtr.Zero);
                if (_ctx == IntPtr.Zero)
                {
                    _log.LogWarning("[Prism] prism_init failed.");
                    return false;
                }

                if (!string.IsNullOrEmpty(_preferredBackend))
                {
                    _backend = AcquireByName(_preferredBackend);
                    if (_backend == IntPtr.Zero)
                        _log.LogWarning($"[Prism] Preferred backend '{_preferredBackend}' not available, using automatic choice.");
                }

                if (_backend == IntPtr.Zero)
                    _backend = prism_registry_acquire_best(_ctx);

                if (_backend == IntPtr.Zero)
                {
                    _log.LogWarning("[Prism] No speech backend available.");
                    return false;
                }

                _features = prism_backend_get_features(_backend);
                BackendName = PtrToUtf8(prism_backend_name(_backend)) ?? "unknown";
                _available = true;
                _log.LogInfo($"[Prism] Using backend: {BackendName}");

                if (_rate != 0 && SupportsRate)
                    ApplyRate();

                return true;
            }
            catch (Exception e)
            {
                _log.LogWarning($"[Prism] Not available: {e.GetType().Name}: {e.Message}");
                return false;
            }
        }

        private IntPtr AcquireByName(string name)
        {
            ulong count = prism_registry_count(_ctx).ToUInt64();
            for (ulong i = 0; i < count; i++)
            {
                ulong id = prism_registry_id_at(_ctx, new UIntPtr(i));
                if (!string.Equals(PtrToUtf8(prism_registry_name(_ctx, id)), name, StringComparison.OrdinalIgnoreCase))
                    continue;

                IntPtr backend = prism_registry_acquire(_ctx, id);
                if (backend == IntPtr.Zero) return IntPtr.Zero;

                int result = prism_backend_initialize(backend);
                if (result == PRISM_OK || result == PRISM_ERROR_ALREADY_INITIALIZED)
                    return backend;

                _log.LogWarning($"[Prism] Backend '{name}' failed to initialize: {ErrorString(result)}");
                prism_backend_free(backend);
                return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }

        public void Initialize() { }

        public void Speak(string text, bool interrupt = true)
        {
            if (!_available || string.IsNullOrEmpty(text)) return;
            try
            {
                byte[] utf8 = ToUtf8(text);
                // Output = speech + braille on screen readers that support it.
                int result = (_features & PRISM_BACKEND_SUPPORTS_OUTPUT) != 0
                    ? prism_backend_output(_backend, utf8, interrupt)
                    : prism_backend_speak(_backend, utf8, interrupt);
                if (result != PRISM_OK)
                    _log.LogWarning($"[Prism] Speak failed: {ErrorString(result)}");
            }
            catch (Exception e)
            {
                _log.LogWarning($"[Prism] Speak failed: {e.GetType().Name}: {e.Message}");
            }
        }

        public void Stop()
        {
            if (!_available || (_features & PRISM_BACKEND_SUPPORTS_STOP) == 0) return;
            try { prism_backend_stop(_backend); } catch { }
        }

        // Adjusts rate by delta, clamps to [-10, 10]. Returns the new rate value.
        public int AdjustRate(int delta)
        {
            _rate = Math.Max(-10, Math.Min(10, _rate + delta));
            ApplyRate();
            return _rate;
        }

        private void ApplyRate()
        {
            // Prism rates are normalized to [0.0, 1.0]; map -10..10 onto that.
            int result = prism_backend_set_rate(_backend, (_rate + 10) / 20f);
            if (result != PRISM_OK)
                _log.LogWarning($"[Prism] Set rate failed: {ErrorString(result)}");
        }

        public void Dispose()
        {
            _available = false;
            try
            {
                if (_backend != IntPtr.Zero) { prism_backend_free(_backend); _backend = IntPtr.Zero; }
                if (_ctx != IntPtr.Zero) { prism_shutdown(_ctx); _ctx = IntPtr.Zero; }
            }
            catch { }
        }

        private static byte[] ToUtf8(string text)
        {
            int length = Encoding.UTF8.GetByteCount(text);
            byte[] bytes = new byte[length + 1];
            Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
            return bytes;
        }

        private static string PtrToUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            int length = 0;
            while (Marshal.ReadByte(ptr, length) != 0) length++;
            byte[] bytes = new byte[length];
            Marshal.Copy(ptr, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string ErrorString(int error)
        {
            try { return PtrToUtf8(prism_error_string(error)) ?? error.ToString(); }
            catch { return error.ToString(); }
        }
    }
}
