using System.Collections.Generic;
using HarmonyLib;
using MonsterPromAccessibility.Speech;
using NGameConstants;

namespace MonsterPromAccessibility.Hooks
{
    // AchievementsWrapper.SetAchievement is the store-agnostic achievement trigger
    // present on all builds (Steam, GOG, DRM-free). Hooking it here lets us announce
    // achievement unlocks without depending on Steamworks or GOG Galaxy.
    //
    // Display names are manually mapped from the EAchievement enum values (dumped
    // via DllInspect on 2026-03-23). COUNT is a sentinel and has no mapping.
    // If a value has no mapping, speaks "Achievement unlocked" and logs the missing key.
    public static class AchievementHooks
    {
        private static SpeechRouter _speech;

        private static readonly Dictionary<EAchievement, string> DisplayNames =
            new Dictionary<EAchievement, string>
        {
            { EAchievement.EPIC_LOSER,                    "Epic Loser" },
            { EAchievement.CASANOVA,                      "Casanova" },
            { EAchievement.THAT_WAS_UNEXPECTED,           "That Was Unexpected" },
            { EAchievement.ENDING_EXPLORER,               "Ending Explorer" },
            { EAchievement.ENDING_CONNOISSEUR,            "Ending Connoisseur" },
            { EAchievement.GOOD_ON_MY_OWN,                "Good on My Own" },
            { EAchievement.GO_PLAY_OUTSIDE,               "Go Play Outside" },
            { EAchievement.WOAH_YOU_REALLY_LIKE_THIS_GAME,"Woah, You Really Like This Game" },
            { EAchievement.HONEY_THIS_ISNT_HEALTHY,       "Honey, This Isn't Healthy" },
            { EAchievement.I_WANNA_BUY_YOUR_HEART,        "I Wanna Buy Your Heart" },
            { EAchievement.ENTER_THE_COVEN,               "Enter the Coven" },
            { EAchievement.GO_BLOBERT,                    "Go Blobert" },
            { EAchievement.LOVE_FROM_THE_DEPTHS,          "Love from the Depths" },
            { EAchievement.BOO,                           "Boo" },
            { EAchievement.CALCULESTER,                   "Calculester" },
            { EAchievement.ORGY_MANAGER,                  "Orgy Manager" },
            { EAchievement.PERV,                          "Perv" },
            { EAchievement.PARTY_24H,                     "Party 24 Hours" },
            { EAchievement.SMALL_BLADDER,                 "Small Bladder" },
            { EAchievement.DODGEBALL_GOD,                 "Dodgeball God" },
            { EAchievement.NERD,                          "Nerd" },
            { EAchievement.MONSTER_PROM_THE_MUSICAL,      "Monster Prom: The Musical" },
            { EAchievement.BOOKWORM,                      "Bookworm" },
            { EAchievement.OUTCAST,                       "Outcast" },
            { EAchievement.TRUE_TO_YOURSELF,              "True to Yourself" },
            { EAchievement.THE_WORST,                     "The Worst" },
            { EAchievement.FOREVER_ALONE,                 "Forever Alone" },
            { EAchievement.SHOPAHOLIC,                    "Shopaholic" },
            { EAchievement.BEST_AT_FAILING,               "Best at Failing" },
            { EAchievement.POLYAMOROUS,                   "Polyamorous" },
            { EAchievement.BROKE,                         "Broke" },
            { EAchievement.TIGER_MILLIONAIRE,             "Tiger Millionaire" },
            { EAchievement.GREAT_COMMANDER,               "Great Commander" },
            { EAchievement.HEY_LISTEN,                    "Hey, Listen" },
            { EAchievement.WELL_DONE_PLAYER,              "Well Done, Player" },
            { EAchievement.BIG_REVEAL,                    "Big Reveal" },
            { EAchievement.EVIL_TWIN,                     "Evil Twin" },
            { EAchievement.ULTIMATE_TRIP,                 "Ultimate Trip" },
            { EAchievement.THE_FUNERAL,                   "The Funeral" },
            { EAchievement.PRANK_MASTERZ,                 "Prank Masterz" },
            { EAchievement.CAGANER,                       "Caganer" },
            { EAchievement.CHRISTMAS_DINNER,              "Christmas Dinner" },
            { EAchievement.YOU_WERE_NAUGHTY,              "You Were Naughty" },
            { EAchievement.ENDING_FLOOD,                  "Ending Flood" },
            { EAchievement.EVENT_STORM,                   "Event Storm" },
            { EAchievement.OUTCOME_TSUNAMI,               "Outcome Tsunami" },
            { EAchievement.ULTIMATE_LORE,                 "Ultimate Lore" },
            { EAchievement.HANDS_UP,                      "Hands Up" },
            { EAchievement.TIGER_BILLIONAIRE,             "Tiger Billionaire" },
            { EAchievement.PUBLISH_MOD,                   "Publish Mod" },
            { EAchievement.PLAY_MOD,                      "Play Mod" },
            { EAchievement.CREATE_MOD_CHARACTER,          "Create Mod Character" },
            // COUNT is a sentinel value — no mapping intentional
        };

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        [HarmonyPatch(typeof(AchievementsWrapper), "SetAchievement")]
        static class Patch_SetAchievement
        {
            static void Prefix(EAchievement aAchievement)
            {
                if (_speech == null) return;

                string display;
                if (DisplayNames.TryGetValue(aAchievement, out display))
                {
                    _speech.Speak("Achievement unlocked: " + display);
                }
                else
                {
                    Plugin.Log.LogWarning("[Achievement] Missing display mapping for " + aAchievement);
                    _speech.Speak("Achievement unlocked");
                }
            }
        }
    }
}
