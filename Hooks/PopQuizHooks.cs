using HarmonyLib;
using MonsterPromAccessibility.Speech;

namespace MonsterPromAccessibility.Hooks
{
    // Pop quiz flow:
    //   L_StartQuiz -> StartQuestion(index) [private] -> shows question text via typewriter
    //   L_AdvanceStepPopQuiz -> shows answer options (public)
    //   L_ConfirmPopQuizAnswer(playerIndex, optionIndex) -> records answer (public)
    //
    // We cannot directly hook private StartQuestion, but we can hook
    // L_AdvanceStepPopQuiz (which fires just before options appear) and read
    // EventTextLabel.text at that moment for the question text. Options are then
    // announced by reading the option labels that become active.
    public static class PopQuizHooks
    {
        private static SpeechRouter _speech;

        // Exposed so MenuHooks can prepend the question when quiz options are hovered.
        public static string CurrentQuestion = string.Empty;

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        // Fires when the typewriter finishes and answer options appear.
        // At this point EventTextLabel.text has the full question.
        // We read both question and all visible option labels.
        [HarmonyPatch(typeof(PopQuizManager), "L_AdvanceStepPopQuiz")]
        static class Patch_AdvanceStep
        {
            static void Postfix(PopQuizManager __instance)
            {
                if (_speech == null) return;

                string question = EventHooks.StripNguiMarkup(__instance.EventTextLabel?.text ?? string.Empty);
                CurrentQuestion = question;

                bool isLove = Traverse.Create(__instance).Field("mActiveQuestionIsLove").GetValue<bool>();

                UILabel[] optionLabels = isLove
                    ? __instance.EventTextLoveOptionsLabels
                    : __instance.EventTextOptionsLabels;

                if (optionLabels == null || optionLabels.Length == 0)
                {
                    _speech.Speak(question);
                    return;
                }

                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(question))
                    sb.Append(question).Append(". ");

                for (int i = 0; i < optionLabels.Length; i++)
                {
                    string opt = EventHooks.StripNguiMarkup(optionLabels[i]?.text ?? string.Empty);
                    if (!string.IsNullOrEmpty(opt))
                        sb.Append($"Option {i + 1}: {opt}. ");
                }

                _speech.Speak(sb.ToString().Trim());
            }
        }

        // Answer confirmation is handled by MenuHooks OnClick ("Confirmed.").
        // No extra announcement needed here — the hover already read the option text.
    }
}
