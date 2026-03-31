using System.Text.RegularExpressions;
using HarmonyLib;
using MonsterPromAccessibility.Speech;

namespace MonsterPromAccessibility.Hooks
{
    // Covers main event dialogue (scene text + speaker) and choice announcements.
    //
    // Dialogue flow:
    //   StartEvent -> L_AdvanceStepCurrentEvent (fires per scene)
    //     - sets EventTextLabel.text via TransferEventTextToLabel
    //     - private fields mEventIndexActive and mEventSceneCounter track position
    //
    // Choice flow:
    //   L_ShowEventChoiceOptions -> sets EventTextOption1Label + EventTextOption2Label
    //   L_ConfirmEventChoice     -> resolves to Option1/2 Success/Failure
    public static class EventHooks
    {
        private static SpeechRouter _speech;
        private static string _lastSpoken     = string.Empty;
        private static string _lastSpokenText = string.Empty; // text only, no speaker prefix
        private static string _lastSpeaker    = string.Empty;

        // Exposed so StatHooks can check whether a stat name was already mentioned in the
        // most recent story line — to avoid announcing the same info twice.
        public static string LastSpokenText => _lastSpokenText;

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        // Clears dedup state so stale text from a previous game session can't
        // silence the first matching line of a new game.
        public static void ResetState()
        {
            _lastSpoken     = string.Empty;
            _lastSpokenText = string.Empty;
            _lastSpeaker    = string.Empty;
        }

        // Fires whenever a new event scene is shown.
        // Reads the text and speaker from public + Traverse-accessed private fields.
        [HarmonyPatch(typeof(EventManager), "L_AdvanceStepCurrentEvent")]
        static class Patch_AdvanceStep
        {
            static void Postfix(EventManager __instance)
            {
                if (_speech == null) return;
                if (__instance.EventTextLabel == null) return;

                // EventTextLabel holds the full text (or is empty when the options
                // container is active). Read directly; it's public.
                string text = StripNguiMarkup(__instance.EventTextLabel.text);
                if (string.IsNullOrEmpty(text)) return;

                // Speaker name lives in the current scene's WhoSpeaksNPC.
                // mEventIndexActive and mEventSceneCounter are private ints.
                int eventIndex = Traverse.Create(__instance).Field("mEventIndexActive").GetValue<int>();
                int sceneIndex = Traverse.Create(__instance).Field("mEventSceneCounter").GetValue<int>();

                string speaker = string.Empty;
                if (__instance.EventsToUseRuntime != null
                    && eventIndex >= 0 && eventIndex < __instance.EventsToUseRuntime.Length)
                {
                    var flow = __instance.EventsToUseRuntime[eventIndex];
                    if (flow.EventScenes != null
                        && sceneIndex >= 0 && sceneIndex < flow.EventScenes.Length)
                    {
                        speaker = flow.EventScenes[sceneIndex].WhoSpeaksNPC ?? string.Empty;
                    }
                }

                // Only prepend the speaker name when it changes — not on every line.
                bool speakerChanged = speaker != _lastSpeaker;
                _lastSpeaker = speaker;

                string announcement = (speakerChanged && !string.IsNullOrEmpty(speaker))
                    ? $"{speaker}: {text}"
                    : text;

                if (announcement == _lastSpoken) return;
                if (text == _lastSpokenText) return; // same text, different speaker prefix
                _lastSpoken     = announcement;
                _lastSpokenText = text;
                _speech.Speak(announcement);
            }
        }

        // Fires when the choice UI opens. Announces both options so the player
        // knows what they are before deciding.
        [HarmonyPatch(typeof(EventManager), "L_ShowEventChoiceOptions")]
        static class Patch_ShowChoiceOptions
        {
            static void Postfix(EventManager __instance)
            {
                if (_speech == null) return;

                string opt1 = StripNguiMarkup(__instance.EventTextOption1Label?.text ?? string.Empty);
                string opt2 = StripNguiMarkup(__instance.EventTextOption2Label?.text ?? string.Empty);

                if (string.IsNullOrEmpty(opt1) && string.IsNullOrEmpty(opt2)) return;

                // Announce both options together so the player has the full picture.
                string announcement = $"Option 1: {opt1}. Option 2: {opt2}.";
                _speech.Speak(announcement);
            }
        }

        // Fires when the player commits to a choice.
        // aChoiceSelected: 0 = option 1, 1 = option 2.
        // The outcome (success/failure) is resolved inside this method; we announce
        // the outcome by reading the resulting scene text in the subsequent
        // L_AdvanceStepCurrentEvent postfix, so here we only confirm the selection.
        [HarmonyPatch(typeof(EventManager), "L_ConfirmEventChoice")]
        static class Patch_ConfirmEventChoice
        {
            static void Prefix(int aChoiceSelected)
            {
                if (_speech == null) return;
                string which = aChoiceSelected == 0 ? "option 1" : "option 2";
                _speech.Speak($"Chose {which}.");
            }
        }

        // Remove NGUI inline markup tags before passing text to speech.
        // NGUI tags are always lowercase: [b], [/b], [i], [/i], [color=...], [/color],
        // [url=...], [/url], [-], [00ffff] (hex colors), [size=N], [/size].
        // We do NOT strip [UPPERCASE] tokens — those are localization keys that ARE the text.
        private static readonly Regex NguiTag = new Regex(
            @"\[/?(?:[biuBIU]|color(?:=[^\]]+)?|url(?:=[^\]]+)?|size(?:=\d+)?|[0-9a-fA-F]{2,8}|-)\]",
            RegexOptions.Compiled);

        public static string StripNguiMarkup(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return NguiTag.Replace(text, string.Empty).Trim();
        }
    }
}
