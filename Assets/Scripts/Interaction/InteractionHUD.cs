using UnityEngine;
using UnityEngine.UI;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// Crosshair dot, the interaction prompt under it, and a short-lived
    /// hint line near the bottom of the screen.
    /// </summary>
    public class InteractionHUD : MonoBehaviour
    {
        [SerializeField] private Text promptLabel;
        [SerializeField] private Image crosshair;
        [SerializeField] private Text hintLabel;
        [SerializeField] private Color idleCrosshair = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color activeCrosshair = new Color(1f, 1f, 1f, 0.9f);

        private float hintHideTime;

        private void Awake()
        {
            HidePrompt();
            if (hintLabel != null) hintLabel.enabled = false;
        }

        private void Update()
        {
            if (hintLabel != null && hintLabel.enabled && Time.time >= hintHideTime)
                hintLabel.enabled = false;
        }

        public void ShowPrompt(string text)
        {
            if (promptLabel != null)
            {
                promptLabel.text = text;
                promptLabel.enabled = true;
            }
            if (crosshair != null) crosshair.color = activeCrosshair;
        }

        public void HidePrompt()
        {
            if (promptLabel != null) promptLabel.enabled = false;
            if (crosshair != null) crosshair.color = idleCrosshair;
        }

        public void ShowHint(string text, float seconds)
        {
            if (hintLabel == null) return;
            hintLabel.text = text;
            hintLabel.enabled = true;
            hintHideTime = Time.time + seconds;
        }
    }
}
