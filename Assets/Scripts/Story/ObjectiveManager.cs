using UnityEngine;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// The objective line in the top-left corner: one short reminder in Walter's words,
    /// faded in whenever the story state changes it.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        private const float FadeTime = 1.2f;
        private const float Alpha = 0.8f;

        private Text label;
        private float fade = 1f;

        private void Awake()
        {
            var ui = StoryUI.Instance;
            label = ui.MakeText(ui.Root, "Objective", 32, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(60f, -42f), new Vector2(1000f, 50f));
            UIStyle.Italic(label);
            Show(StoryStateManager.Objective, instant: true);
            StoryStateManager.ObjectiveChanged += OnChanged;
        }

        private void OnDestroy() => StoryStateManager.ObjectiveChanged -= OnChanged;

        private void OnChanged(string text) => Show(text, instant: false);

        private void Show(string text, bool instant)
        {
            label.text = text;
            fade = instant ? 1f : 0f;
            Apply();
        }

        private void Update()
        {
            if (fade >= 1f) return;
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / FadeTime);
            Apply();
        }

        private void Apply() => label.color = new Color(UIStyle.Ink.r, UIStyle.Ink.g, UIStyle.Ink.b,
            string.IsNullOrEmpty(label.text) ? 0f : fade * Alpha);
    }
}
