using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using MonsterPromAccessibility.Speech;

namespace MonsterPromAccessibility.Hooks
{
    // UIButtonColor.OnHover(bool isOver) is the game's own method that fires
    // whenever the cursor enters or leaves any button — menus, in-game choices,
    // shop items, NPC select, everything. Patching it means we work through
    // NGUI's existing event path, not around it.
    public static class MenuHooks
    {
        private static SpeechRouter _speech;
        private static string _lastAnnounced   = string.Empty; // full text including any panel header prefix
        private static string _lastButtonText  = string.Empty; // pure button text, no header prefix
        // Tracks the last panel instance ID so we know when we've entered a new screen.
        private static int    _lastPanelInstanceId       = 0;
        private static string _lastQuizQuestionAnnounced = string.Empty;
        // True only while the player is actively choosing a location (school map or cafeteria).
        // Location buttons fire hover events via keyboard navigation even during story dialogue,
        // so we suppress them outside of the actual selection phase.
        private static bool   _locationSelectActive      = false;

        // Known button names mapped to exactly what should be spoken.
        // These take priority over any UILabel text or auto-cleaned name.
        // Add entries here whenever a button has a confusing or missing name.
        private static readonly Dictionary<string, string> ButtonHints =
            new Dictionary<string, string>
        {
            // Suppress — no useful text for blind users
            { "UrlButton", "" },

            // Main menu
            { "ModtoolLabel",             "Mod Tools" },
            { "PurchaseFranchiseButton",  "Purchase Monster Prom Franchise Bundle" },
            { "PurchaseConButton",        "Purchase Monster Prom 4: Monster Con" },
            { "PurchaseCampButton",       "Purchase Monster Prom 3: Camp" },
            { "PurchaseRoadtripButton",   "Purchase Monster Roadtrip" },
            { "PurchaseDlcButton",        "Purchase DLC: Second Term" },

            // Game-start flow
            { "Button_1P",            "1 Player" },
            { "Button_2P",            "2 Players" },
            { "Button_3P",            "3 Players" },
            { "Button_4P",            "4 Players" },
            { "Button_Vanilla",       "Base Game" },
            { "Button_DlcST",         "Second Term DLC" },
            { "Button_SingleShort",   "Short game: around 30 minutes" },
            { "Button_SingleRegular", "Regular game: around 60 minutes" },
            { "Button_MultiShort",    "Short game: around 30 minutes" },
            { "Button_MultiRegular",  "Regular game: around 60 minutes" },

            // PlayerIcon* handled dynamically in GetButtonText — see below.

            // Character customisation sub-panels (appear after clicking a PlayerIcon).
            // Name and pronoun buttons already have UILabel children with current values,
            // so FindLabelText handles those — we only need hints for the ones without labels.
            { "CustomizeBlueOkButton",   "Blue player: confirm" },
            { "CustomizeRedOkButton",    "Red player: confirm" },
            { "CustomizeGreenOkButton",  "Green player: confirm" },
            { "CustomizeYellowOkButton", "Yellow player: confirm" },

            // Navigation
            { "BackButton",             "Back" },
            { "BackLabel",              "Back" },
            { "NavigatePageDownButton", "Next Page" },
            { "NavigatePageUpButton",   "Previous Page" },

            // Location selection (school map) — Sign and Trigger variants for the same location
            { "AuditoriumSign",    "Auditorium" },
            { "AuditoriumTrigger", "Auditorium" },
            { "BathroomsSign",     "Bathrooms" },
            { "BathroomsTrigger",  "Bathrooms" },
            { "GymSign",           "Gym" },
            { "GymTrigger",        "Gym" },
            { "OutdoorsSign",      "Outdoors" },
            { "OutdoorsTrigger",   "Outdoors" },
            { "LibrarySign",       "Library" },
            { "LibraryTrigger",    "Library" },
            { "ClassSign",         "Classroom" },
            { "ClassTrigger",      "Classroom" },
            { "CafeteriaSign",     "Cafeteria" },
            { "locationButtonGo",  "Go to selected location" },

            // Cafeteria table selection
            { "Table1Button", "Table 1" },
            { "Table2Button", "Table 2" },
            { "Table3Button", "Table 3" },
            { "Table4Button", "Table 4" },
            { "Table5Button", "Table 5" },

            // Settings — volume controls
            { "MusicDownLabel", "Music Volume Down" },
            { "MusicUpLabel",   "Music Volume Up" },
            { "SfxDownLabel",   "SFX Volume Down" },
            { "SfxUpLabel",     "SFX Volume Up" },
            { "DisableVoiceLabel", "Toggle Voice Acting" },

            // Settings — option cycling (Left = previous, Right = next)
            { "ChangeWindowModeLabelLeft",  "Window Mode: Previous" },
            { "ChangeWindowModeLabelRight", "Window Mode: Next" },
            { "ChangeDisplayLeft",          "Display: Previous" },
            { "ChangeDisplayRight",         "Display: Next" },
            { "ChangeBlurLeft",             "Background Blur: Previous" },
            { "ChangeBlurRight",            "Background Blur: Next" },
            { "ChangeLanguageLeft",         "Language: Previous" },
            { "ChangeLanguageRight",        "Language: Next" },
        };

        // Panel context for containers whose labels are all inside buttons.
        // Keyed on the parent GameObject name that GetPanelHeader resolves to.
        private static readonly Dictionary<string, string> PanelHeaders =
            new Dictionary<string, string>
        {
            // Character customisation sub-panels — each has name, pronoun, and OK buttons
            // but no free-standing label, so the scan returns nothing without this hint.
            { "DataPanelBlue",   "Blue player customisation. Change name or pronouns." },
            { "DataPanelRed",    "Red player customisation. Change name or pronouns." },
            { "DataPanelGreen",  "Green player customisation. Change name or pronouns." },
            { "DataPanelYellow", "Yellow player customisation. Change name or pronouns." },
        };

        public static void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        // Called by LocationWatcher after it auto-announces a container, so that
        // GetPanelHeader treats it as already visited and won't prepend a redundant
        // header when the first button inside is hovered.
        public static void MarkContainerVisited(UnityEngine.GameObject container)
        {
            if (container == null) return;
            _lastPanelInstanceId = container.GetInstanceID();
        }

        // Called by LocationWatcher to open/close the location-selection announcement window.
        public static void SetLocationSelectActive(bool active)
        {
            _locationSelectActive = active;
        }

        // Returns true if the button lives inside a location-selection container.
        // Used to gate hover/click announcements to the selection phase only.
        private static bool IsInLocationContainer(UnityEngine.Transform t)
        {
            UnityEngine.Transform p = t.parent;
            if (p == null) return false;
            string n = p.gameObject.name;
            return n == "LocationsContainer" || n == "CafeteriaLocationsContainer";
        }

        [HarmonyPatch(typeof(UIButtonColor), "OnHover")]
        static class Patch_OnHover
        {
            static void Postfix(UIButtonColor __instance, bool isOver)
            {
                if (_speech == null) return;
                string text = GetButtonText(__instance);
                if (!isOver || string.IsNullOrEmpty(text)) return;

                // Suppress location-button hovers when the player isn't actively choosing.
                // The game keeps location buttons alive (and keyboard navigation fires hover
                // events on them) during story events and stat scenes.
                if (!_locationSelectActive && IsInLocationContainer(__instance.transform))
                    return;

                // Buttons with an explicit ButtonHints entry are already self-sufficient
                // announcements — skip GetPanelHeader so "ID TO JOIN:" etc. never gets
                // prepended to "Back", "Next Page", volume labels, and similar globals.
                string header = ButtonHints.ContainsKey(__instance.gameObject.name)
                    ? string.Empty
                    : GetPanelHeader(__instance);

                // Quiz option buttons (EventOptionsBox*, EventLoveOptionsBox*, etc.) live in
                // a separate container from the question label so GetPanelHeader can't find it.
                // Inject the stored question once per question — not once per option hover.
                if (string.IsNullOrEmpty(header)
                    && __instance.gameObject.name.Contains("OptionsBox")
                    && !string.IsNullOrEmpty(PopQuizHooks.CurrentQuestion)
                    && PopQuizHooks.CurrentQuestion != _lastQuizQuestionAnnounced)
                {
                    header = PopQuizHooks.CurrentQuestion;
                    _lastQuizQuestionAnnounced = PopQuizHooks.CurrentQuestion;
                }

                string fullText = string.IsNullOrEmpty(header) ? text : $"{header}. {text}";

                if (fullText == _lastAnnounced) return;
                _lastAnnounced  = fullText;
                _lastButtonText = text; // pure text without any header prefix
                _speech.Speak(fullText);
            }
        }

        // UICamera.set_controllerNavigationObject fires whenever controller focus moves
        // to a new GameObject. UIButtonColor.OnHover is only triggered by mouse hover,
        // not by controller navigation, so this is the correct hook point for controllers.
        // The parameter name "value" matches the C# property setter convention in the IL.
        [HarmonyPatch(typeof(UICamera), "set_controllerNavigationObject")]
        static class Patch_ControllerNavigationObject
        {
            static void Postfix(UnityEngine.GameObject value)
            {
                if (_speech == null || value == null) return;
                AnnounceControllerFocus(value);
            }
        }

        // Shared logic for controller focus changes — mirrors OnHover Postfix but driven
        // by UICamera.controllerNavigationObject rather than UIButtonColor.OnHover.
        private static void AnnounceControllerFocus(UnityEngine.GameObject go)
        {
            var btn = go.GetComponent<UIButtonColor>();
            if (btn == null) return;

            string text = GetButtonText(btn);
            if (string.IsNullOrEmpty(text)) return;

            if (!_locationSelectActive && IsInLocationContainer(go.transform)) return;

            string header = ButtonHints.ContainsKey(go.name)
                ? string.Empty
                : GetPanelHeader(btn);

            string fullText = string.IsNullOrEmpty(header) ? text : $"{header}. {text}";

            if (fullText == _lastAnnounced) return;
            _lastAnnounced  = fullText;
            _lastButtonText = text;
            _speech.Speak(fullText);
        }

        [HarmonyPatch(typeof(UIButton), "OnClick")]
        static class Patch_OnClick
        {
            static void Prefix(UIButton __instance)
            {
                if (_speech == null) return;

                string name = __instance.gameObject.name;
                string text = GetButtonText(__instance);
                if (string.IsNullOrEmpty(text)) return;

                // Location container buttons are announced on hover only.
                // Any click — whether the intentional selection or a spurious Enter-key
                // repeat during a story scene — should be silent. The hover already spoke
                // the button name; the story text that follows is the confirmation.
                if (IsInLocationContainer(__instance.transform))
                {
                    _locationSelectActive = false;
                    return;
                }

                // Suppress if this button was the last one announced on hover.
                // Compare against _lastButtonText (pure text) not _lastAnnounced, because
                // _lastAnnounced includes a panel header prefix ("Blue. Auditorium") which
                // never matches the plain button text ("Auditorium") — causing the click to
                // re-announce the button on every Enter press through the whole event.
                if (text != _lastButtonText)
                    _speech.Speak(text);
            }
        }

        // Use the button's immediate parent as the "screen" boundary.
        // Each setup screen groups its buttons under a distinct parent container
        // (e.g. PlayerCountPanel, GameExpansionPanel) so the instance ID changes
        // whenever we move to a new screen — even when all screens share a root UIPanel.
        // On first entry to a parent, scan its children for non-button UILabels
        // (the prompt/question the sighted player sees at the top of the screen).
        private static string GetPanelHeader(UIButtonColor btn)
        {
            UnityEngine.Transform parent = btn.transform.parent;
            if (parent == null) return string.Empty;

            int id = parent.gameObject.GetInstanceID();
            if (id == _lastPanelInstanceId) return string.Empty;
            _lastPanelInstanceId = id;

            // Hard-coded panel context for containers whose labels are all inside buttons
            // (so the automatic scan finds nothing useful).
            string panelHint;
            if (PanelHeaders.TryGetValue(parent.gameObject.name, out panelHint))
                return panelHint;

            UILabel[] labels = parent.GetComponentsInChildren<UILabel>(includeInactive: false);
            foreach (UILabel label in labels)
            {
                // Skip any label that lives inside (or on) a button.
                if (label.GetComponentInParent<UIButtonColor>() != null) continue;
                string text = EventHooks.StripNguiMarkup(label.text);
                if (!string.IsNullOrEmpty(text) && HasAlphanumeric.IsMatch(text))
                    return text;
            }

            // Fallback: for the location-selection screen the prompt lives outside the
            // container. The game's own PlayerTurnLabel shows whose turn / which week it is.
            if (parent.gameObject.name == "LocationsContainer")
            {
                UnityEngine.GameObject turnLabelObj = UnityEngine.GameObject.Find("PlayerTurnLabel");
                if (turnLabelObj != null)
                {
                    UILabel lbl = turnLabelObj.GetComponent<UILabel>();
                    if (lbl != null)
                    {
                        string text = EventHooks.StripNguiMarkup(lbl.text);
                        if (!string.IsNullOrEmpty(text) && HasAlphanumeric.IsMatch(text))
                            return text;
                    }
                }
            }

            return string.Empty;
        }

        private static string GetButtonText(UIButtonColor btn)
        {
            if (btn == null) return string.Empty;

            string name = btn.gameObject.name;

            // 0. Cafeteria tables — "Table 2: Vera, Liam. 2 seats free."
            Match table = _tableButtonPattern.Match(name);
            if (table.Success) return CafeteriaTableDescriber.Describe(int.Parse(table.Groups[1].Value));

            // 1. Hand-written hint — most authoritative.
            string hint;
            if (ButtonHints.TryGetValue(name, out hint)) return hint;

            // 1a. Shop item containers — ShopHooks.SetSelectedItem announces these with
            //     full item name, price, and description. Return empty so OnHover fires nothing
            //     and there is no double-announcement when the game selects the item.
            if (_shopItemPattern.IsMatch(name)) return string.Empty;

            // 1b. Prom NPC buttons — GameFlowHooks.HoverNpcX patches announce the NPC name
            //     from NpcNameLabel after the game resolves it. Suppress raw "Npc Prom Button1".
            if (_npcPromButtonPattern.IsMatch(name)) return string.Empty;

            // 2. Player colour icons — include a call to action so the blind user knows
            //    that activating this button opens a customisation sub-panel.
            if (name.StartsWith("PlayerIcon") && name.Length > "PlayerIcon".Length)
            {
                string color = name.Substring("PlayerIcon".Length); // "Blue", "Red", …
                return $"{color} player. Activate to customise.";
            }

            // 3. Customise-name / customise-pronoun buttons.
            //    Pattern: Customize{Color}{Name|Pronoun}Button
            //    A sibling label (Customize{Color}{Name|Pronoun}Label) holds the current value.
            //    We look it up via Transform.Find on the shared parent container.
            var custMatch = _customizePattern.Match(name);
            if (custMatch.Success)
            {
                string color = custMatch.Groups[1].Value;   // "Blue", "Red", …
                string kind  = custMatch.Groups[2].Value;   // "Name" or "Pronoun"
                string labelName = $"Customize{color}{kind}Label";
                UnityEngine.Transform par = btn.transform.parent;
                if (par != null)
                {
                    UnityEngine.Transform labelT = par.Find(labelName);
                    if (labelT != null)
                    {
                        UILabel lbl = labelT.GetComponent<UILabel>();
                        if (lbl != null)
                        {
                            string val = EventHooks.StripNguiMarkup(lbl.text);
                            if (!string.IsNullOrEmpty(val))
                                return $"{color} player {kind.ToLower()}: {val}";
                        }
                    }
                }
                return $"{color} player: change {kind.ToLower()}";
            }

            // 4. UILabel text on the object or its children.
            string t = FindLabelText(btn.transform);
            if (!string.IsNullOrEmpty(t)) return t;

            // 5. Auto-clean the object name as a last resort.
            return CleanObjectName(name);
        }

        // Matches "CustomizeBlueNameButton", "CustomizeRedPronounButton", etc.
        private static readonly Regex _customizePattern = new Regex(
            @"^Customize(Blue|Red|Green|Yellow)(Name|Pronoun)Button$",
            RegexOptions.Compiled);

        // Matches "Item0Container" through "Item9Container" (and higher indices).
        // ShopHooks.SetSelectedItem announces these with full name/price/description,
        // so we suppress the raw object name here to avoid double-announcing.
        private static readonly Regex _shopItemPattern = new Regex(
            @"^Item\d+Container$",
            RegexOptions.Compiled);

        // Matches "NpcPromButton1" through "NpcPromButtonN" (vanilla and DLC).
        // These buttons have no UILabel children — the NPC name lives in NpcNameLabel
        // on EndingChooseNpcManager, which is updated by HoverNpcX() methods.
        // GameFlowHooks.HoverNpcX patches handle the announcement; suppress here.
        private static readonly Regex _tableButtonPattern = new Regex(@"^Table([1-5])Button$", RegexOptions.Compiled);

        private static readonly Regex _npcPromButtonPattern = new Regex(
            @"^NpcPromButton\d+$",
            RegexOptions.Compiled);

        // Strip trailing "Label"/"Button" suffix, then split camelCase into words.
        // "PlayLabel" -> "Play", "PurchaseFranchiseButton" -> "Purchase Franchise"
        private static readonly Regex CamelSplit = new Regex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

        private static readonly Regex InlineNoise = new Regex(@"\b(Label|Button|Color)\b\s*", RegexOptions.Compiled);
        private static readonly Regex ExtraSpaces  = new Regex(@"\s{2,}", RegexOptions.Compiled);

        private static string CleanObjectName(string name)
        {
            // Replace underscores with spaces so "Button_1P" splits into words correctly.
            string s = name.Replace('_', ' ');
            // Split camelCase, then remove Label/Button/Color wherever they appear.
            string words = CamelSplit.Replace(s, " ");
            words = InlineNoise.Replace(words, " ");
            return ExtraSpaces.Replace(words, " ").Trim();
        }

        private static readonly Regex HasAlphanumeric = new Regex(@"[a-zA-Z0-9]", RegexOptions.Compiled);

        private static string FindLabelText(UnityEngine.Transform root)
        {
            UILabel[] labels = root.GetComponentsInChildren<UILabel>(includeInactive: true);
            foreach (UILabel label in labels)
            {
                string t = EventHooks.StripNguiMarkup(label.text);
                // Skip symbol/icon-only strings like "_" or ">" that aren't readable text.
                if (!string.IsNullOrEmpty(t) && HasAlphanumeric.IsMatch(t)) return t;
            }
            return string.Empty;
        }
    }
}
