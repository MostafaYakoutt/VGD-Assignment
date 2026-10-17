using UnityEngine;
using UnityEngine.UI;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// Crosshair dot plus the interaction prompt under it.
    /// </summary>
    public class InteractionHUD : MonoBehaviour
    {
        [SerializeField] private Text promptLabel;
        [SerializeField] private Image crosshair;
        [SerializeField] private Color idleCrosshair = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color activeCrosshair = new Color(1f, 1f, 1f, 0.9f);

        private void Awake()
        {
            HidePrompt();
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
    }
}
