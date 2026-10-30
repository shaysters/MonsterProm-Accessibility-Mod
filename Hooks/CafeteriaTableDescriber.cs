using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using NGameConstants;

namespace MonsterPromAccessibility.Hooks
{
    // Builds "Table 2: Vera, Liam, Shopkeeper. 1 seat free." for the cafeteria table buttons.
    //
    // GameManager.mAllCharactersInCafeteriaTables maps each seated character to a seat
    // key "Table{n}_{seat}" (seats 1-4). NPCs are keyed by upper-case id ("VERA"),
    // players who already sat down this turn by EPlayerColor name ("Blue").
    public static class CafeteriaTableDescriber
    {
        private const int SeatsPerTable = 4;

        private static readonly AccessTools.FieldRef<GameManager, Dictionary<string, string>> SeatsRef =
            AccessTools.FieldRefAccess<GameManager, Dictionary<string, string>>("mAllCharactersInCafeteriaTables");

        // English display names for ids that aren't a single capitalised word.
        private static readonly Dictionary<string, string> EnglishNames = new Dictionary<string, string>
        {
            { "WOLFPACK",         "Wolf Pack" },
            { "FIRSTCALCULESTER", "First Calculester" },
            { "FELLOWSTUDENT",    "Fellow Student" },
            { "EVILFAITH",        "Evil Faith" },
            { "BILLYTHEBACKER",   "Billy the Backer" },
        };

        public static string Describe(int tableNumber)
        {
            string fallback = $"Table {tableNumber}";
            try
            {
                if (GameManager.Instance == null) return fallback;
                Dictionary<string, string> seats = SeatsRef(GameManager.Instance);
                if (seats == null) return fallback;

                string prefix = $"Table{tableNumber}_";
                var seated = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> entry in seats)
                {
                    if (entry.Value != null && entry.Value.StartsWith(prefix, StringComparison.Ordinal))
                        seated[entry.Value + "|" + entry.Key] = GetDisplayName(entry.Key);
                }

                var sb = new StringBuilder(fallback).Append(": ");
                if (seated.Count > 0)
                    sb.Append(string.Join(", ", new List<string>(seated.Values).ToArray())).Append(". ");

                int free = Math.Max(0, SeatsPerTable - seated.Count);
                sb.Append(free == 0 ? "No seats free." : free == 1 ? "1 seat free." : $"{free} seats free.");
                return sb.ToString();
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[MP-Accessibility] Cafeteria table describe failed: {e.Message}");
                return fallback;
            }
        }

        private static string GetDisplayName(string id)
        {
            if (Enum.IsDefined(typeof(EPlayerColor), id))
            {
                string playerName = GameManager.Instance.GetPlayerNameByColor((EPlayerColor)Enum.Parse(typeof(EPlayerColor), id));
                return string.IsNullOrEmpty(playerName) ? id : EventHooks.StripNguiMarkup(playerName);
            }

            ELanguage language = GeneralManager.Instance != null ? GeneralManager.Instance.GameLanguage : ELanguage.English;
            string english;
            if (language == ELanguage.English && EnglishNames.TryGetValue(id, out english))
                return english;

            string titleCase = id.Substring(0, 1) + id.Substring(1).ToLower();
            return GameConstants.GetCharacterNameInLanguage(language, titleCase);
        }
    }
}
