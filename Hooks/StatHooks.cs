using HarmonyLib;
using MonsterPromAccessibility.Speech;
using NGameConstants;

namespace MonsterPromAccessibility.Hooks
{
    // Announces stat changes as they happen.
    // Hooks the (EPlayerColor, string, int) overloads of IncreaseStat and DecreaseStat
    // because these are the terminal overloads that all other overloads funnel into.
    public static class StatHooks
    {
        private static SpeechRouter _speech;

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        [HarmonyPatch(typeof(StatsManager), "IncreaseStat",
            new[] { typeof(EPlayerColor), typeof(string), typeof(int) })]
        static class Patch_IncreaseStat
        {
            static void Prefix(EPlayerColor aPlayerColor, string aStat, int aNumber)
            {
                if (_speech == null || aNumber <= 0) return;
                // The event story line often says "You gain +N STAT" in the same tick.
                // If it already mentioned this stat, skip — we'd just interrupt it with
                // a duplicate. StatHooks still fires when no story line precedes the change
                // (e.g. shop purchases, background effects).
                if (StoryAlreadyMentionedStat(aStat)) return;
                _speech.Speak($"{aPlayerColor} {aStat} plus {aNumber}.");
            }
        }

        [HarmonyPatch(typeof(StatsManager), "DecreaseStat",
            new[] { typeof(EPlayerColor), typeof(string), typeof(int), typeof(bool) })]
        static class Patch_DecreaseStat
        {
            static void Prefix(EPlayerColor aPlayerColor, string aStat, int aNumber)
            {
                if (_speech == null || aNumber <= 0) return;
                if (StoryAlreadyMentionedStat(aStat)) return;
                _speech.Speak($"{aPlayerColor} {aStat} minus {aNumber}.");
            }
        }

        // Returns true if the most recent story dialogue line already contains this stat name,
        // meaning the player already heard about this change and a second announcement would
        // only cut off what is currently being spoken.
        private static bool StoryAlreadyMentionedStat(string statName)
        {
            string last = EventHooks.LastSpokenText;
            if (string.IsNullOrEmpty(last)) return false;
            return last.IndexOf(statName, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
