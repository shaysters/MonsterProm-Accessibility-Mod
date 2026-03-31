using System.Text;
using MonsterPromAccessibility.Speech;
using NGameConstants;
using UnityEngine;

namespace MonsterPromAccessibility.Hooks
{
    // Injected as a MonoBehaviour on the Plugin's own GameObject.
    //
    // Keyboard:   R key        — re-speaks the last TTS line.
    // Controller: Left Bumper  — same repeat function.
    //
    // Keyboard:   E key        — reads current player's stat values aloud.
    // Keyboard:   F4 / F5      — decrease / increase SAPI speech rate (SAPI only).
    //
    // Controller cold-start fix (main menu only):
    //     NGUI requires selectedObject to be non-null before controller navigation
    //     works. The main menu scene sets no default selection, so pressing a
    //     direction or button with a controller does nothing until the player first
    //     presses a keyboard key. We detect any controller input while selectedObject
    //     is null and force-select the first available UIKeyNavigation target, giving
    //     the controller an entry point without requiring a keyboard press.
    public class RepeatKeyListener : MonoBehaviour
    {
        private SpeechRouter _speech;
        private bool _axisWasNeutral = true;

        public void Initialize(SpeechRouter speech)
        {
            _speech = speech;
        }

        private void Update()
        {
            if (_speech == null) return;

            // R / Left Bumper — repeat last TTS line.
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton4))
                _speech.RepeatLast();

            // E — read current player's stats aloud.
            if (Input.GetKeyDown(KeyCode.E))
                SpeakCurrentStats();

            // F4 / F5 — decrease / increase SAPI speech rate.
            if (Input.GetKeyDown(KeyCode.F4))
                _speech.AdjustSapiRate(-1);
            if (Input.GetKeyDown(KeyCode.F5))
                _speech.AdjustSapiRate(1);

            if (UICamera.current == null) return;

            // Controller cold-start fix.
            // Only runs when nothing is selected AND a controller is connected.
            if (UICamera.selectedObject == null
                && InputManager.Instance != null
                && InputManager.Instance.GetNumberOfConnectedControllers() > 0
                && AnyControllerInputThisFrame())
            {
                if (UIKeyNavigation.list != null && UIKeyNavigation.list.size > 0)
                {
                    UICamera.selectedObject = UIKeyNavigation.list[0].gameObject;
                }
            }
        }

        private void SpeakCurrentStats()
        {
            if (_speech == null) return;
            if (GameManager.Instance == null || StatsManager.Instance == null)
            {
                _speech.Speak("Stats not available.");
                return;
            }

            EPlayerColor color = GameManager.Instance.CurrentPlayerColor;
            string name = GameManager.Instance.GetPlayerNameByColor(color);
            if (string.IsNullOrEmpty(name)) name = color.ToString();

            var sb = new StringBuilder(name);
            sb.Append(" — ");

            EStat[] stats = { EStat.Smarts, EStat.Boldness, EStat.Creativity,
                              EStat.Charm, EStat.Fun, EStat.Money };

            for (int i = 0; i < stats.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                int val = StatsManager.Instance.GetStatInt(color, stats[i]);
                sb.Append($"{stats[i]} {val}");
            }
            sb.Append(".");

            _speech.Speak(sb.ToString());
        }

        // Returns true on the first frame any controller button (0-9) is pressed,
        // or when a stick/d-pad axis first crosses the movement threshold.
        // Threshold of 0.7 avoids false triggers from stick drift.
        private bool AnyControllerInputThisFrame()
        {
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    _axisWasNeutral = true;
                    return true;
                }
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool axisActive = Mathf.Abs(h) > 0.7f || Mathf.Abs(v) > 0.7f;
            bool firstPush  = axisActive && _axisWasNeutral;
            _axisWasNeutral = !axisActive;
            return firstPush;
        }
    }
}
