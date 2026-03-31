using System.Collections.Generic;
using BepInEx.Logging;
using MonsterPromAccessibility.Speech.Providers;
using UnityEngine;

namespace MonsterPromAccessibility.Speech
{
    // Routes speech requests to the highest-priority available provider.
    // Provider selection order:
    //   1. User-configured preference (set via config file)
    //   2. Tolk  — Windows: NVDA, JAWS, or any Tolk-supported screen reader
    //   3. Orca  — Linux: speech-dispatcher / spd-say
    //   4. SAPI  — Windows: built-in TTS fallback (no screen reader required)
    public class SpeechRouter
    {
        private readonly List<ISpeechProvider> _providers = new List<ISpeechProvider>();
        private ISpeechProvider _active;
        private string _lastSpoken = string.Empty;
        private ManualLogSource _log;

        // User preference: set to provider Name to override auto-detection.
        // Empty string means auto-detect.
        public string PreferredProvider { get; set; } = string.Empty;

        // SAPI speech rate (-10 to 10, default 0). Only applies when SAPI is active.
        public int SapiRate { get; set; } = 0;

        public SpeechRouter(ManualLogSource log)
        {
            _log = log;
        }

        public void Initialize()
        {
            _providers.Add(new TolkProvider(_log)); // Windows: NVDA/JAWS via Tolk
            _providers.Add(new OrcaProvider(_log)); // Linux: speech-dispatcher / Orca
            _providers.Add(new SAPIProvider(_log, SapiRate)); // Windows: built-in SAPI TTS fallback

            SelectProvider();
        }

        private void SelectProvider()
        {
            if (!string.IsNullOrEmpty(PreferredProvider))
            {
                foreach (var p in _providers)
                {
                    if (p.Name == PreferredProvider && p.IsAvailable())
                    {
                        SetActive(p);
                        return;
                    }
                }
                _log.LogWarning($"[SpeechRouter] Preferred provider '{PreferredProvider}' not available, falling back.");
            }

            foreach (var p in _providers)
            {
                if (p.IsAvailable())
                {
                    SetActive(p);
                    return;
                }
            }
        }

        private void SetActive(ISpeechProvider p)
        {
            p.Initialize();
            _active = p;
        }

        public void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!Application.isFocused) return;
            text = text.Replace('\n', ' ').Replace('\r', ' ');
            _lastSpoken = text;
            _active?.Speak(text, interrupt);
        }

        public void RepeatLast()
        {
            if (!string.IsNullOrEmpty(_lastSpoken))
                _active?.Speak(_lastSpoken, interrupt: true);
        }

        public void Stop()
        {
            _active?.Stop();
        }

        public string ActiveProviderName => _active?.Name ?? "none";

        public void AdjustSapiRate(int delta)
        {
            if (!(_active is SAPIProvider sapi)) return;
            int newRate = sapi.AdjustRate(delta);
            Speak($"SAPI rate {newRate}.", interrupt: true);
        }

        public void Dispose()
        {
            foreach (var p in _providers)
                try { p.Dispose(); } catch { }
        }
    }
}
