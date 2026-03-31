using HarmonyLib;
using MonsterPromAccessibility.Speech;

namespace MonsterPromAccessibility.Hooks
{
    // UnlockScreenManager is spawned as a new GameObject when the game wants to
    // show an unlock notification (an item, image, or other reward).
    // NotifyReady() is called after the screen is set up and visible, making it
    // the correct point to announce the unlock text.
    public static class UnlockHooks
    {
        private static SpeechRouter _speech;

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        [HarmonyPatch(typeof(UnlockScreenManager), "NotifyReady")]
        static class Patch_NotifyReady
        {
            static void Postfix(UnlockScreenManager __instance)
            {
                if (_speech == null) return;

                string text = EventHooks.StripNguiMarkup(__instance.labelToChange?.text ?? string.Empty);
                if (!string.IsNullOrEmpty(text))
                    _speech.Speak($"Unlocked: {text}");
            }
        }
    }
}
