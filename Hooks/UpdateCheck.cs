using System;
using System.Text.RegularExpressions;
using MonsterPromAccessibility.Speech;
using UnityEngine;
using UnityEngine.Networking;

namespace MonsterPromAccessibility.Hooks
{
    // Once per game start, asks GitHub for the latest release and says so if it's newer than the
    // installed mod. Reads the release manifest the installer itself uses
    // (releases/latest/download/loadstone-release.json: a plain redirect, no API token or rate limit).
    // Nothing is sent except the request itself, and a failed check is only logged.
    //
    // Polled from Update() rather than run as a coroutine: the game's .NET 2.0 runtime can't load
    // the IteratorStateMachine attribute the compiler puts on iterator methods.
    public class UpdateCheck : MonoBehaviour
    {
        private const string ManifestUrl = "https://github.com/shaysters/MonsterProm-Accessibility-Mod/releases/latest/download/loadstone-release.json";
        private static readonly Regex VersionField = new Regex("\"version\"\\s*:\\s*\"v?([0-9.]+)\"");


        private SpeechRouter _speech;
        private UnityWebRequest _request;
        private string _newerVersion;
        private bool _done;

        public void Initialize(SpeechRouter speech)
        {
            _speech = speech;
            _request = UnityWebRequest.Get(ManifestUrl);
            _request.timeout = 15;
            _request.SendWebRequest();
        }

        private void Update()
        {
            if (_done || _speech == null) return;

            if (_newerVersion != null)
            {
                if (!UpdateNotice.Ready(_speech.SpeakCount, Time.realtimeSinceStartup - _speech.LastSpeakTime)) return;
                _done = true;
                Plugin.Log.LogInfo("[MP-Accessibility] Update announced after " + _speech.SpeakCount + " spoken lines.");
                // Spoken through the router, so R repeats it.
                _speech.Speak("Monster Prom Accessibility version " + _newerVersion + " is available. Run the installer to update.", interrupt: false);
                return;
            }

            if (_request == null || !_request.isDone) return;
            try
            {
                if (_request.isNetworkError || _request.isHttpError)
                {
                    Plugin.Log.LogInfo("[MP-Accessibility] Update check skipped: " + _request.error);
                    _done = true;
                    return;
                }
                Match m = VersionField.Match(_request.downloadHandler.text);
                Version latest = m.Success ? Parse(m.Groups[1].Value) : null;
                Version installed = Parse(Plugin.Version);
                Plugin.Log.LogInfo("[MP-Accessibility] Update check: installed " + installed + ", latest " + latest);
                if (latest != null && installed != null && latest > installed)
                    _newerVersion = m.Groups[1].Value;
                else
                    _done = true;
            }
            finally
            {
                _request.Dispose();
                _request = null;
            }
        }

        private static Version Parse(string s)
        {
            try { return new Version(s); }
            catch { return null; }
        }
    }

    // When the update notice may be spoken: after the title menu's first line has been read
    // and the mod has then been quiet for a moment, so it neither cuts off the start-up speech
    // nor gets cut off by it. If the menu never speaks by itself, a longer silence will do.
    internal static class UpdateNotice
    {
        // Lines the mod speaks by itself before the title menu: the "loaded" message.
        private const int StartupLines = 1;
        private const float QuietSeconds = 2f;
        private const float FallbackQuietSeconds = 30f;

        internal static bool Ready(int spokenLines, float secondsQuiet)
        {
            if (secondsQuiet < QuietSeconds)
            {
                return false;
            }

            return spokenLines > StartupLines
                || (secondsQuiet >= FallbackQuietSeconds && Time.realtimeSinceStartup >= 30f);
        }
    }
}
