using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx.Logging;

namespace MonsterPromAccessibility.Speech.Providers
{
    // Tolk is loaded via LoadLibrary with the full path to the plugins folder,
    // bypassing Mono's P/Invoke search (which looks relative to the game EXE).
    public class TolkProvider : ISpeechProvider
    {
        public string Name => "Tolk";

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private delegate void VoidFunc();
        private delegate bool BoolFunc();
        private delegate bool OutputFunc([MarshalAs(UnmanagedType.LPWStr)] string str, bool interrupt);
        private IntPtr _lib = IntPtr.Zero;
        private VoidFunc  _load;
        private VoidFunc  _unload;
        private BoolFunc  _isLoaded;
        private BoolFunc  _hasSpeech;
        private OutputFunc _output;
        private BoolFunc  _silence;

        private bool _available;
        private ManualLogSource _log;

        public TolkProvider(ManualLogSource log)
        {
            _log = log;
        }

        public bool IsAvailable()
        {
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(dir, "Tolk.dll");

                // Pre-load the NVDA controller client so that when Tolk_Load() internally
                // calls LoadLibrary("nvdaControllerClient32.dll") by short name, Windows
                // finds it already resident in the process and returns the cached handle.
                // Without this, the short-name search misses our plugins folder entirely.
                string clientDll = IntPtr.Size == 8 ? "nvdaControllerClient64.dll" : "nvdaControllerClient32.dll";
                string clientPath = Path.Combine(dir, clientDll);
                if (File.Exists(clientPath))
                {
                    LoadLibraryW(clientPath);
                }
                else
                {
                    _log.LogWarning($"[Tolk] NVDA client DLL not found at: {clientPath}");
                }

                _lib = LoadLibraryW(path);
                if (_lib == IntPtr.Zero)
                {
                    _log.LogWarning($"[Tolk] LoadLibrary failed for: {path} (error {Marshal.GetLastWin32Error()})");
                    return false;
                }

                _load     = GetFunc<VoidFunc>  ("Tolk_Load");
                _unload   = GetFunc<VoidFunc>  ("Tolk_Unload");
                _isLoaded = GetFunc<BoolFunc>  ("Tolk_IsLoaded");
                _hasSpeech= GetFunc<BoolFunc>  ("Tolk_HasSpeech");
                _output   = GetFunc<OutputFunc>("Tolk_Output");
                _silence  = GetFunc<BoolFunc>  ("Tolk_Silence");

                if (_load == null || _output == null)
                {
                    _log.LogWarning("[Tolk] Required exports not found.");
                    return false;
                }

                _load();
                _available = _isLoaded != null && _isLoaded() && _hasSpeech != null && _hasSpeech();

                if (!_available)
                {
                    _log.LogWarning("[Tolk] Loaded but no speech output available.");
                }

                return _available;
            }
            catch (Exception e)
            {
                _log.LogWarning($"[Tolk] Not available: {e.Message}");
                return false;
            }
        }

        private T GetFunc<T>(string name) where T : class
        {
            IntPtr proc = GetProcAddress(_lib, name);
            if (proc == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer(proc, typeof(T)) as T;
        }

        public void Initialize() { }

        public void Speak(string text, bool interrupt = true)
        {
            if (!_available || string.IsNullOrEmpty(text)) return;
            try { _output(text, interrupt); }
            catch (Exception e) { _log.LogWarning($"[Tolk] Speak failed: {e.Message}"); }
        }

        public void Stop()
        {
            if (!_available) return;
            try { _silence(); } catch { }
        }

        public void Dispose()
        {
            try { _unload?.Invoke(); } catch { }
            if (_lib != IntPtr.Zero) { FreeLibrary(_lib); _lib = IntPtr.Zero; }
            _available = false;
        }
    }
}
