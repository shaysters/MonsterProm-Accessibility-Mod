using System;
using System.Diagnostics;
using BepInEx.Logging;

namespace MonsterPromAccessibility.Speech.Providers
{
    // Linux speech via speech-dispatcher's spd-say command-line tool.
    // Orca (and most Linux AT) use speech-dispatcher as their backend, so
    // spd-say is the standard cross-AT way to output speech on Linux.
    // Only activates when running on a Unix/Linux platform.
    public class OrcaProvider : ISpeechProvider
    {
        public string Name => "Orca";

        private ManualLogSource _log;
        private bool _available;
        private Process _currentSpeech;

        public OrcaProvider(ManualLogSource log)
        {
            _log = log;
        }

        public bool IsAvailable()
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                return false;

            try
            {
                // Probe: spd-say --version exits 0 if speech-dispatcher is installed.
                using (var p = StartProcess("spd-say", "--version"))
                {
                    if (p == null) return false;
                    p.WaitForExit(2000);
                    _available = (p.ExitCode == 0);
                }

                if (!_available)
                    _log.LogWarning("[Orca] spd-say not usable (non-zero exit).");

                return _available;
            }
            catch (Exception e)
            {
                _log.LogWarning($"[Orca] Not available: {e.Message}");
                return false;
            }
        }

        public void Initialize() { }

        public void Speak(string text, bool interrupt = true)
        {
            if (!_available || string.IsNullOrEmpty(text)) return;
            try
            {
                if (interrupt) KillCurrent();
                _currentSpeech = StartProcess("spd-say", QuoteArg(text));
            }
            catch (Exception e)
            {
                _log.LogWarning($"[Orca] Speak failed: {e.Message}");
            }
        }

        public void Stop() => KillCurrent();

        public void Dispose()
        {
            KillCurrent();
            _available = false;
        }

        private void KillCurrent()
        {
            try
            {
                if (_currentSpeech != null && !_currentSpeech.HasExited)
                    _currentSpeech.Kill();
                _currentSpeech?.Dispose();
                _currentSpeech = null;
            }
            catch { }
        }

        // Wrap in double quotes and escape inner quotes/backslashes so that
        // Mono's argument splitter on Linux passes the text as a single token
        // to execvp regardless of spaces or special characters.
        private static string QuoteArg(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private static Process StartProcess(string exe, string args)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
            };
            return Process.Start(psi);
        }
    }
}
