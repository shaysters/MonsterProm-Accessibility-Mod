using System.Text;
using HarmonyLib;
using UnityEngine;

namespace MonsterPromAccessibility.Hooks
{
    // Online lobby match code.
    //
    // OnlineLobbyOnMatchView.SetMatchInfo() writes the room code into
    // matchNameLabel. It runs on Show and again every time the player list
    // refreshes, so we only announce when the code changes, and reset on Hide.
    //
    // The announcement is delayed slightly and queued (not interrupting) so the
    // focus announcement for the button the game hovers on open doesn't cut it off.
    // C key (see RepeatKeyListener) re-reads the code while in the lobby.
    public static class OnlineCodeHooks
    {
        private const float AnnounceDelay = 0.4f;

        private static OnlineLobbyOnMatchView _view;
        private static string _lastCode;
        private static string _pendingCode;
        private static float _pendingAt;

        [HarmonyPatch(typeof(OnlineLobbyOnMatchView), "SetMatchInfo")]
        static class Patch_SetMatchInfo
        {
            static void Postfix(OnlineLobbyOnMatchView __instance)
            {
                _view = __instance;
                string code = ReadCode();
                if (string.IsNullOrEmpty(code) || code == _lastCode) return;

                _lastCode = code;
                _pendingCode = code;
                _pendingAt = Time.unscaledTime + AnnounceDelay;
            }
        }

        [HarmonyPatch(typeof(OnlineLobbyOnMatchView), "Hide")]
        static class Patch_Hide
        {
            static void Postfix(OnlineLobbyOnMatchView __instance)
            {
                if (_view != __instance) return;
                _view = null;
                _lastCode = null;
                _pendingCode = null;
            }
        }

        // Called every frame from RepeatKeyListener.Update.
        public static void Tick()
        {
            if (_pendingCode == null || Time.unscaledTime < _pendingAt) return;

            string code = _pendingCode;
            _pendingCode = null;
            Plugin.Speech?.Speak($"Online code: {SpellOut(code)}. Press C to hear it again.", interrupt: false);
        }

        // C key.
        public static void SpeakCurrentCode()
        {
            if (Plugin.Speech == null) return;

            string code = ReadCode();
            if (string.IsNullOrEmpty(code))
            {
                Plugin.Speech.Speak("No online code right now.");
                return;
            }

            _pendingCode = null;
            Plugin.Speech.Speak($"Online code: {SpellOut(code)}");
        }

        private static string ReadCode()
        {
            if (_view == null || _view.matchNameLabel == null) return null;
            if (!_view.matchNameLabel.gameObject.activeInHierarchy) return null;
            return EventHooks.StripNguiMarkup(_view.matchNameLabel.text ?? string.Empty).Trim();
        }

        // "48271" -> "4, 8, 2, 7, 1" so screen readers read digit by digit.
        private static string SpellOut(string code)
        {
            var sb = new StringBuilder();
            foreach (char c in code)
            {
                if (char.IsWhiteSpace(c)) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
