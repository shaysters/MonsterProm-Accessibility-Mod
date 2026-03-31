using HarmonyLib;
using MonsterPromAccessibility.Speech;

namespace MonsterPromAccessibility.Hooks
{
    // Covers shop accessibility:
    //   1. Shopkeeper dialogue (greetings, bye, "I have nothing for you", etc.)
    //      SetShopkeeperDialogue receives the full string before the typewriter starts —
    //      hooking it gives us clean text without waiting for the letter-by-letter display.
    //   2. Item selection: name, price, and description when the player navigates to an item.
    //      SetSelectedItem fires on hover/keyboard focus before UIButton.OnClick, so the
    //      player hears the item info the moment they reach it.
    //   3. Purchase confirmation: announces "Purchased <name>." so the player knows the
    //      transaction went through without needing visual feedback.
    //
    // MenuHooks.GetButtonText suppresses the raw "Item4Container" name via _shopItemPattern
    // so this hook is the sole announcer for shop items — no double-speak.
    public static class ShopHooks
    {
        private static SpeechRouter _speech;

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        // Fires when the shopkeeper starts a new dialogue line (greeting, bye, "nothing", etc.).
        // Receives the full text string before the typewriter effect begins letter-by-letter
        // display, so we always get the complete sentence.
        [HarmonyPatch(typeof(ShopManager), "SetShopkeeperDialogue")]
        static class Patch_SetShopkeeperDialogue
        {
            static void Prefix(string aDialogueLine)
            {
                if (_speech == null || string.IsNullOrEmpty(aDialogueLine)) return;
                string text = EventHooks.StripNguiMarkup(aDialogueLine);
                if (string.IsNullOrEmpty(text)) return;
                _speech.Speak(text);
            }
        }

        // Fires when the player moves focus to a shop item (hover or keyboard navigation).
        // At this point ShopManager has already updated ItemInfoNameLabel, ItemInfoDescriptionLabel,
        // and ItemPriceLabel with the selected item's data — we read those directly.
        [HarmonyPatch(typeof(ShopManager), "SetSelectedItem")]
        static class Patch_SetSelectedItem
        {
            static void Postfix(ShopManager __instance)
            {
                if (_speech == null) return;

                string name  = EventHooks.StripNguiMarkup(__instance.ItemInfoNameLabel?.text  ?? string.Empty);
                string price = EventHooks.StripNguiMarkup(__instance.ItemPriceLabel?.text     ?? string.Empty);
                string desc  = EventHooks.StripNguiMarkup(__instance.ItemInfoDescriptionLabel?.text ?? string.Empty);

                if (string.IsNullOrEmpty(name)) return;

                string announcement = name;
                if (!string.IsNullOrEmpty(price)) announcement += $". Price: {price}";
                if (!string.IsNullOrEmpty(desc))  announcement += $". {desc}";

                _speech.Speak(announcement);
            }
        }

        // Fires when the player confirms a purchase.
        // Announces "Purchased <name>." so the player gets tactile confirmation
        // without relying on visual feedback or sound alone.
        [HarmonyPatch(typeof(ShopManager), "ConfirmSelectedItem")]
        static class Patch_ConfirmSelectedItem
        {
            static void Prefix(ShopManager __instance)
            {
                if (_speech == null) return;
                string name = EventHooks.StripNguiMarkup(__instance.ItemInfoNameLabel?.text ?? string.Empty);
                if (!string.IsNullOrEmpty(name))
                    _speech.Speak($"Purchased {name}.");
            }
        }
    }
}
