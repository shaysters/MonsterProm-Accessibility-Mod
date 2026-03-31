using System.Collections.Generic;
using HarmonyLib;
using MonsterPromAccessibility.Speech;
using NGameConstants;

namespace MonsterPromAccessibility.Hooks
{
    // Covers game-state transitions not handled by EventHooks/StatHooks:
    //
    //   1. New global turn — GameManager.L_StartNewGlobalTurnCallback fires once per
    //      round/week with the new player order. Announces player order so the user
    //      always knows whose turn is next.
    //
    //   2. Prom NPC selection (EndingChooseNpcManager):
    //      a. FadeInEndingChooseNpc — screen opens; announce the header label + player.
    //      b. HoverNpcX() (9 methods, one per NPC) — user navigates to an NPC button.
    //         These are per-NPC instance methods with no parameters. Each one updates
    //         NpcNameLabel with the NPC's display name (including localisation), so we
    //         read the label in a shared Postfix helper after the method runs.
    //         NpcPromButton1-N are suppressed in MenuHooks (no UILabel children) so
    //         these hooks are the sole source of NPC names on this screen.
    //      c. L_ConfirmNpcChosen — player commits; announces "Going to prom with X."
    //
    //   NOTE: L_SelectNpc fires only on explicit keyboard-select, not on hover. The
    //   HoverNpcX hooks cover hover navigation, which is what NVDA reads as you move.
    public static class GameFlowHooks
    {
        private static SpeechRouter _speech;
        private static string _lastNpcAnnounced = string.Empty;

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        // Shared helper for all HoverNpcX Postfix patches.
        // Called after the game method has run, so NpcNameLabel already holds the
        // correct NPC display name (localised, and "None of them" for HoverNpcNone).
        private static void AnnounceNpcHover(EndingChooseNpcManager instance)
        {
            if (_speech == null || instance == null) return;
            string name = EventHooks.StripNguiMarkup(instance.NpcNameLabel?.text ?? string.Empty);
            if (string.IsNullOrEmpty(name) || name == _lastNpcAnnounced) return;
            _lastNpcAnnounced = name;
            _speech.Speak(name);
        }

        // GameManager.L_StartNewGlobalTurnCallback(List<EPlayerColor> aPlayerNewOrder)
        // fires once per global turn (once per round/week) with the new player order.
        // We announce the order so the blind user always knows who plays in what position.
        [HarmonyPatch(typeof(GameManager), "L_StartNewGlobalTurnCallback")]
        static class Patch_NewGlobalTurn
        {
            static void Postfix(List<EPlayerColor> aPlayerNewOrder)
            {
                if (_speech == null) return;
                EventHooks.ResetState();
                if (aPlayerNewOrder == null || aPlayerNewOrder.Count == 0) return;

                var sb = new System.Text.StringBuilder("New turn. Player order: ");
                for (int i = 0; i < aPlayerNewOrder.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    string playerName = GameManager.Instance != null
                        ? GameManager.Instance.GetPlayerNameByColor(aPlayerNewOrder[i])
                        : null;
                    if (string.IsNullOrEmpty(playerName))
                        playerName = aPlayerNewOrder[i].ToString();
                    sb.Append($"{i + 1}: {playerName}");
                }
                sb.Append(".");
                _speech.Speak(sb.ToString());
            }
        }

        // EndingChooseNpcManager.FadeInEndingChooseNpc(EPlayerColor aCurrentPlayer)
        // fires when the "choose your prom date" screen opens for aCurrentPlayer.
        // Reads LabelEndingChooseNpc (the game's own UI text) and prepends the player name.
        [HarmonyPatch(typeof(EndingChooseNpcManager), "FadeInEndingChooseNpc",
            new[] { typeof(EPlayerColor) })]
        static class Patch_FadeInEndingChooseNpc
        {
            static void Postfix(EndingChooseNpcManager __instance, EPlayerColor aCurrentPlayer)
            {
                if (_speech == null) return;
                _lastNpcAnnounced = string.Empty; // reset dedup on screen open

                string header = EventHooks.StripNguiMarkup(
                    __instance.LabelEndingChooseNpc?.text ?? string.Empty);

                string playerName = null;
                try { playerName = GameManager.Instance?.GetPlayerNameByColor(aCurrentPlayer); } catch { }
                if (string.IsNullOrEmpty(playerName)) playerName = aCurrentPlayer.ToString();

                string announcement = !string.IsNullOrEmpty(header)
                    ? $"{playerName}. {header}"
                    : $"{playerName}. Choose your prom date.";

                _speech.Speak(announcement);
            }
        }

        // One patch per HoverNpcX() method — all are no-param instance methods on
        // EndingChooseNpcManager (confirmed from DLL). Each reads NpcNameLabel via the
        // shared helper after the game method has updated it.
        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcDamien")]
        static class Patch_HoverNpcDamien     { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcPolly")]
        static class Patch_HoverNpcPolly      { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcScott")]
        static class Patch_HoverNpcScott      { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcVera")]
        static class Patch_HoverNpcVera       { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcMiranda")]
        static class Patch_HoverNpcMiranda    { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcLiam")]
        static class Patch_HoverNpcLiam       { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcCalculester")]
        static class Patch_HoverNpcCalculester { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcZoe")]
        static class Patch_HoverNpcZoe        { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        [HarmonyPatch(typeof(EndingChooseNpcManager), "HoverNpcNone")]
        static class Patch_HoverNpcNone       { static void Postfix(EndingChooseNpcManager __instance) => AnnounceNpcHover(__instance); }

        // EndingChooseNpcManager.L_ConfirmNpcChosen(String aNpcName)
        // fires when the player confirms their prom date choice.
        // Announces before the method so the user hears it as the action completes.
        [HarmonyPatch(typeof(EndingChooseNpcManager), "L_ConfirmNpcChosen",
            new[] { typeof(string) })]
        static class Patch_ConfirmNpcChosen
        {
            static void Prefix(string aNpcName)
            {
                if (_speech == null || string.IsNullOrEmpty(aNpcName)) return;
                _speech.Speak($"Going to prom with {aNpcName}.");
            }
        }

        // CreditsManager.FinishEpilogueEventAndPlayVideo fires after all epilogue
        // NARRATOR story events complete and the credits scroll begins.
        [HarmonyPatch(typeof(CreditsManager), "FinishEpilogueEventAndPlayVideo")]
        static class Patch_CreditsStart
        {
            static void Postfix()
            {
                _speech?.Speak("You have reached the credits. Press Escape to go back to the main menu.");
            }
        }

        // MapNavigation.OnlineSetChosenResultAwards fires when the end-of-game
        // results page is populated with award text for the featured player and NPC.
        // This is the screen that appears after all epilogue events complete.
        [HarmonyPatch(typeof(MapNavigation), "OnlineSetChosenResultAwards",
            new[] { typeof(EPlayerColor), typeof(string), typeof(string), typeof(string) })]
        static class Patch_ResultAwards
        {
            static void Postfix(MapNavigation __instance, string aPlayerAward, string aNpc, string aNpcAward)
            {
                if (_speech == null) return;

                string playerName = EventHooks.StripNguiMarkup(
                    __instance.Results_PlayerAwardName?.text ?? string.Empty);
                string playerAward = EventHooks.StripNguiMarkup(aPlayerAward ?? string.Empty);

                var sb = new System.Text.StringBuilder("Results. ");
                if (!string.IsNullOrEmpty(playerName))
                    sb.Append($"{playerName}: ");
                if (!string.IsNullOrEmpty(playerAward))
                    sb.Append($"{playerAward}");
                sb.Append(".");

                if (!string.IsNullOrEmpty(aNpc) && aNpc.ToUpper() != "NONE"
                    && !string.IsNullOrEmpty(aNpcAward))
                {
                    string npcName = EventHooks.StripNguiMarkup(
                        __instance.Results_NpcAwardName?.text ?? aNpc);
                    string npcAward = EventHooks.StripNguiMarkup(aNpcAward);
                    sb.Append($" {npcName}: {npcAward}.");
                }

                _speech.Speak(sb.ToString());
            }
        }
    }
}
