using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MonsterPromAccessibility.Hooks;
using MonsterPromAccessibility.Speech;

namespace MonsterPromAccessibility
{
    [BepInPlugin("com.accessibility.monsterprom", "Monster Prom Accessibility", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static SpeechRouter Speech;

        private Harmony _harmony;
        private ConfigEntry<string> _preferredProvider;
        private ConfigEntry<int> _sapiRate;

        private void Awake()
        {
            Log = Logger;
            try
            {
                _preferredProvider = Config.Bind(
                    "Speech",
                    "PreferredProvider",
                    "",
                    "Speech provider: Tolk, Orca, SAPI, or blank for auto-detect.");

                _sapiRate = Config.Bind(
                    "Speech",
                    "SAPIRate",
                    0,
                    "SAPI speech rate. Range: -10 (slowest) to 10 (fastest). Default: 0. Only applies when SAPI is the active provider.");

                Speech = new SpeechRouter(Log);
                Speech.PreferredProvider = _preferredProvider.Value;
                Speech.SapiRate = _sapiRate.Value;
                Speech.Initialize();

                EventHooks.Initialize(Speech);
                StatHooks.Initialize(Speech);
                PopQuizHooks.Initialize(Speech);
                UnlockHooks.Initialize(Speech);
                AchievementHooks.Initialize(Speech);
                ShopHooks.Initialize(Speech);
                GameFlowHooks.Initialize(Speech);
                GalleryHooks.Initialize(Speech);
                MenuHooks.Initialize(Speech);

                // Inject location-screen watcher — announces "Blue's turn. Choose a location."
                // automatically when the LocationsContainer activates, no hover required.
                var watcher = gameObject.AddComponent<LocationWatcher>();
                watcher.Initialize(Speech);

                // R key re-speaks the last TTS line (game does not use R).
                var repeatKey = gameObject.AddComponent<RepeatKeyListener>();
                repeatKey.Initialize(Speech);

                _harmony = new Harmony("com.accessibility.monsterprom");

                // Patch every [HarmonyPatch] nested class individually so that a single
                // missing method (e.g. L_StartNewGlobalTurnCallback absent in the demo)
                // only skips that one patch class instead of aborting everything.
                int applied = 0, skipped = 0;
                foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Length == 0)
                        continue;
                    try
                    {
                        _harmony.CreateClassProcessor(type).Patch();
                        applied++;
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"[MP-Accessibility] Skipped {type.Name}: {ex.Message}");
                        skipped++;
                    }
                }

                Speech.Speak("Monster Prom accessibility loaded.");
            }
            catch (Exception e)
            {
                Log.LogError($"[MP-Accessibility] Startup failed: {e}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Speech?.Dispose();
        }
    }
}
