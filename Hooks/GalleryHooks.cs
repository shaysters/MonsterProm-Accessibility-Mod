using System.Text.RegularExpressions;
using HarmonyLib;
using MonsterPromAccessibility.Speech;

namespace MonsterPromAccessibility.Hooks
{
    // GalleryManager tab and image focus accessibility.
    //
    // TAB HOOKS (SetTabX):
    //   Five Postfix patches — one per public tab method. Each speaks a fixed
    //   hard-coded string per the plan requirements (no dynamic UI text reading).
    //
    // IMAGE FOCUS HOOK (ShowFocusImage):
    //   Source order (strict):
    //   1. GalleryManager.FocusTextLabel — public UILabel the game populates
    //      with descriptive text when an image is focused (confirmed from DLL).
    //   2. Fallback: "Gallery image selected" — if FocusTextLabel is empty or
    //      contains no alphanumeric text for this image type.
    public static class GalleryHooks
    {
        private static SpeechRouter _speech;
        private static readonly Regex HasAlpha = new Regex(@"[a-zA-Z0-9]", RegexOptions.Compiled);

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        [HarmonyPatch(typeof(GalleryManager), "SetTabEndings")]
        static class Patch_SetTabEndings
        {
            static void Postfix()
            {
                if (_speech == null) return;
                _speech.Speak("Gallery tab: Endings");
            }
        }

        [HarmonyPatch(typeof(GalleryManager), "SetTabPolaroids")]
        static class Patch_SetTabPolaroids
        {
            static void Postfix()
            {
                if (_speech == null) return;
                _speech.Speak("Gallery tab: Polaroids");
            }
        }

        [HarmonyPatch(typeof(GalleryManager), "SetTabFanart")]
        static class Patch_SetTabFanart
        {
            static void Postfix()
            {
                if (_speech == null) return;
                _speech.Speak("Gallery tab: Fanart");
            }
        }

        [HarmonyPatch(typeof(GalleryManager), "SetTabMonstersonas")]
        static class Patch_SetTabMonstersonas
        {
            static void Postfix()
            {
                if (_speech == null) return;
                _speech.Speak("Gallery tab: Monstersonas");
            }
        }

        [HarmonyPatch(typeof(GalleryManager), "SetTabMoreArt")]
        static class Patch_SetTabMoreArt
        {
            static void Postfix()
            {
                if (_speech == null) return;
                _speech.Speak("Gallery tab: More Art");
            }
        }

        // ShowFocusImage(GalleryImage aGalleryImage) — fires when an image receives focus.
        // GalleryImage has no text fields (sprite-only). Read FocusTextLabel instead,
        // which is a public UILabel on GalleryManager the game populates for the focused image.
        [HarmonyPatch(typeof(GalleryManager), "ShowFocusImage")]
        static class Patch_ShowFocusImage
        {
            static void Postfix(GalleryManager __instance)
            {
                if (_speech == null) return;

                // Source: GalleryManager.FocusTextLabel (public UILabel, set by game on focus)
                string labelText = EventHooks.StripNguiMarkup(
                    __instance.FocusTextLabel?.text ?? string.Empty);

                if (!string.IsNullOrEmpty(labelText) && HasAlpha.IsMatch(labelText))
                {
                    _speech.Speak(labelText);
                    return;
                }

                _speech.Speak("Gallery image selected");
            }
        }
    }
}
