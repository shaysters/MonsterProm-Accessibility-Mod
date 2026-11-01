using System.Collections.Generic;
using BepInEx.Logging;
using MonsterPromAccessibility.Speech.Providers;
using UnityEngine;

namespace MonsterPromAccessibility.Speech
{
    // Routes speech requests to Prism, which picks a running screen reader
    // (NVDA, JAWS, ...) or falls back to Windows TTS (SAPI / OneCore).
    public class SpeechRouter
    {
        private readonly List<ISpeechProvider> _providers = new List<ISpeechProvider>();
        private ISpeechProvider _active;
        private string _lastSpoken = string.Empty;

        // Time.realtimeSinceStartup of the last thing spoken, so the update notice can wait for a pause.
        public float LastSpeakTime { get; private set; }

        // How many lines have been spoken since the game started.
        public int SpeakCount { get; private set; }
        private ManualLogSource _log;

        // User preference: Prism backend name (e.g. NVDA, JAWS, SAPI, OneCore) to
        // override auto-detection. Empty string means auto-detect.
        public string PreferredProvider { get; set; } = string.Empty;

        // Speech rate (-10 to 10, default 0). Only applies to Windows TTS backends.
        public int SapiRate { get; set; } = 0;

        public SpeechRouter(ManualLogSource log)
        {
            _log = log;
        }

        public void Initialize()
        {
            _providers.Add(new PrismProvider(_log, PreferredProvider, SapiRate));

            SelectProvider();
        }

        private void SelectProvider()
        {
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
            LastSpeakTime = Time.realtimeSinceStartup;
            SpeakCount++;
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

        public string ActiveProviderName => (_active as PrismProvider)?.BackendName ?? "none";

        public void AdjustSapiRate(int delta)
        {
            if (!(_active is PrismProvider prism) || !prism.SupportsRate) return;
            int newRate = prism.AdjustRate(delta);
            Speak($"Speech rate {newRate}.", interrupt: true);
        }

        public void Dispose()
        {
            foreach (var p in _providers)
                try { p.Dispose(); } catch { }
        }
    }
}
