using System;
using System.Diagnostics;
using BepInEx.Logging;

namespace MonsterPromAccessibility.Speech.Providers
{
    // SAPI via a persistent PowerShell bridge process.
    // Unity Mono does not support System.Speech or COM interop, so we spawn one
    // powershell.exe process that owns the SpVoice COM object and reads text lines
    // from stdin. Each Speak() call is a single stdin write — low latency after startup.
    public class SAPIProvider : ISpeechProvider
    {
        public string Name => "SAPI";

        private Process _ps;
        private bool _available;
        private ManualLogSource _log;
        private int _rate;

        public SAPIProvider(ManualLogSource log, int rate = 0)
        {
            _log = log;
            _rate = Math.Max(-10, Math.Min(10, rate));
        }

        public bool IsAvailable()
        {
            try
            {
                // PowerShell script: monitor parent PID so the bridge exits when the game exits
                // (even on crash or task-manager kill), then speak lines from stdin.
                // Flags: 3 = SVSFlagsAsync (1) | SVSFPurgeBeforeSpeak (2)
                int gamePid = Process.GetCurrentProcess().Id;
                string script =
                    $"$ppid={gamePid};" +
                    "try{$p=[System.Diagnostics.Process]::GetProcessById($ppid);" +
                    "$p.EnableRaisingEvents=$true;" +
                    "Register-ObjectEvent -InputObject $p -EventName 'Exited' -Action {[Environment]::Exit(0)} | Out-Null}catch{};" +
                    "$v=New-Object -ComObject SAPI.SpVoice;" +
                    $"$v.Rate={_rate};" +
                    "while($true){$t=[Console]::ReadLine();if($t -eq $null){break};" +
                    "if($t.StartsWith('__RATE__')){$v.Rate=[int]$t.Substring(8);continue};" +
                    "$v.Speak($t,3)}";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"" + script + "\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                };

                _ps = Process.Start(psi);
                if (_ps == null)
                {
                    _log.LogWarning("[SAPI] Failed to start PowerShell bridge.");
                    return false;
                }

                // WaitForExit(500): returns true if process exits within 500ms (failure),
                // false if still running after 500ms (success). Returns early on fast failure.
                if (_ps.WaitForExit(500))
                {
                    _log.LogWarning("[SAPI] PowerShell bridge exited immediately — SAPI may not be installed.");
                    return false;
                }

                _available = true;
                return true;
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[SAPI] IsAvailable failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public void Initialize() { }

        // Adjusts rate by delta, clamps to [-10, 10], sends command to bridge.
        // Returns the new rate value.
        public int AdjustRate(int delta)
        {
            _rate = Math.Max(-10, Math.Min(10, _rate + delta));
            try { _ps.StandardInput.WriteLine($"__RATE__{_rate}"); } catch { }
            return _rate;
        }

        public void Speak(string text, bool interrupt = true)
        {
            if (!_available || string.IsNullOrEmpty(text)) return;
            try
            {
                // Strip newlines — each line is one speak command
                text = text.Replace('\n', ' ').Replace('\r', ' ');
                _ps.StandardInput.WriteLine(text);
            }
            catch (Exception e)
            {
                _log.LogWarning($"[SAPI] Speak failed: {e.GetType().Name}: {e.Message}");
                _available = false;
            }
        }

        public void Stop()
        {
            // Sending an empty line triggers Speak("", 3) which purges the queue
            if (!_available) return;
            try { _ps.StandardInput.WriteLine(""); } catch { }
        }

        public void Dispose()
        {
            _available = false;
            try
            {
                _ps?.StandardInput.Close();
                _ps?.Kill();
                _ps?.Dispose();
            }
            catch { }
            _ps = null;
        }
    }
}
