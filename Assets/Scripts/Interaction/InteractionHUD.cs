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
            // The prompt and hint labels live in the scene; give them the game's type (see UIStyle).
            Style(promptLabel, LeftBehind.Story.UIStyle.Serif, 30);
            Style(hintLabel, LeftBehind.Story.UIStyle.SerifItalic, 30);
            if (crosshair != null)
            {
                idleCrosshair = new Color(LeftBehind.Story.UIStyle.Ink.r, LeftBehind.Story.UIStyle.Ink.g, LeftBehind.Story.UIStyle.Ink.b, 0.35f);
                activeCrosshair = new Color(LeftBehind.Story.UIStyle.Brass.r, LeftBehind.Story.UIStyle.Brass.g, LeftBehind.Story.UIStyle.Brass.b, 0.95f);
            }
            HidePrompt();
            if (hintLabel != null) hintLabel.enabled = false;
        }

        private void Update()
        {
            if (hintLabel != null && hintLabel.enabled && Time.time >= hintHideTime)
                hintLabel.enabled = false;
        }

        private static void Style(Text label, Font font, int size)
        {
            if (label == null) return;
            label.font = font;
            label.fontStyle = FontStyle.Normal;
            label.fontSize = size;
            label.supportRichText = true;
            label.color = LeftBehind.Story.UIStyle.Ink;
            var shadow = label.GetComponent<Shadow>();
            if (shadow == null) shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        public void ShowPrompt(string text)
        {
            if (promptLabel != null)
            {
                promptLabel.text = LeftBehind.Story.UIStyle.KeyHint(text);
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
            hintLabel.text = LeftBehind.Story.UIStyle.KeyHint(text);
            hintLabel.enabled = true;
            hintHideTime = Time.time + seconds;
        }
    }
}
