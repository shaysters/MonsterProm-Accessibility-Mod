using MonsterPromAccessibility.Speech;
using UnityEngine;

namespace MonsterPromAccessibility.Hooks
{
    // Injected as a MonoBehaviour on the Plugin's own GameObject.
    // Watches LocationsContainer (school map) and CafeteriaLocationsContainer (cafeteria
    // tables) and announces context the moment the player can actually interact —
    // i.e. when at least one UIButton inside becomes enabled. This fires AFTER any
    // character dialogue or stat announcements that play while the container is visible
    // but the buttons are still disabled.
    public class LocationWatcher : MonoBehaviour
    {
        private SpeechRouter _speech;

        // Track per-container state: was it "ready" (active + buttons enabled) last frame?
        private bool _schoolReady = false;
        private bool _cafeReady   = false;

        // Cached container references — found once, re-queried only if destroyed.
        private GameObject _schoolContainer;
        private GameObject _cafeContainer;

        public void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        private void Update()
        {
            if (_speech == null) return;

            CheckContainer("LocationsContainer",          ref _schoolReady, ref _schoolContainer, isSchool: true);
            CheckContainer("CafeteriaLocationsContainer", ref _cafeReady,   ref _cafeContainer,   isSchool: false);
        }

        private void CheckContainer(string containerName, ref bool wasReady, ref GameObject cached, bool isSchool)
        {
            if (cached == null) cached = GameObject.Find(containerName);
            GameObject container = cached;
            bool isReady = IsContainerReady(container);

            if (isReady && !wasReady)
            {
                _speech.Speak(BuildAnnouncement(isSchool));
                // Allow hover announcements for location buttons now that selection is open.
                MenuHooks.SetLocationSelectActive(true);
                // Mark the container as visited so GetPanelHeader doesn't prepend a
                // redundant header when the user's cursor is already over a button.
                MenuHooks.MarkContainerVisited(container);
            }

            wasReady = isReady;
        }

        // A container is "ready" when it's active AND at least one UIButton inside is enabled.
        // The game disables buttons during the intro character speech and enables them
        // only when the player is actually allowed to choose — so this fires at the right moment.
        private static bool IsContainerReady(GameObject container)
        {
            if (container == null || !container.activeInHierarchy) return false;

            UIButton[] buttons = container.GetComponentsInChildren<UIButton>(includeInactive: false);
            foreach (UIButton btn in buttons)
                if (btn.isEnabled) return true;

            return false;
        }

        private static string BuildAnnouncement(bool isSchool)
        {
            // GameManager.GetCurrentPlayerName() returns the player's actual display name
            // (custom name or character default like "Vickie") rather than just the colour.
            string playerText = string.Empty;
            if (GameManager.Instance != null)
                playerText = GameManager.Instance.GetCurrentPlayerName() ?? string.Empty;

            string action = isSchool ? "Choose a location." : "Choose a table.";

            return string.IsNullOrEmpty(playerText)
                ? action
                : $"{playerText}'s turn. {action}";
        }
    }
}
