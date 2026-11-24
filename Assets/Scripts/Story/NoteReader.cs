using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// Shows a found note full screen as a sheet of paper in Walter's handwriting.
    /// The player is frozen while reading; E, Esc or Enter puts it down.
    /// </summary>
    public class NoteReader : MonoBehaviour
    {
        private Image dim;
        private Text title;
        private Text body;
        private Action onClosed;
        private int openedFrame;

        public static bool IsOpen => StoryUI.Exists && StoryUI.Instance.GetComponent<NoteReader>().dim.gameObject.activeSelf;

        public static void Open(string titleText, string bodyText, Action closed = null)
        {
            var r = StoryUI.Instance.GetComponent<NoteReader>();
            r.title.text = titleText;
            r.body.text = bodyText;
            r.onClosed = closed;
            r.openedFrame = Time.frameCount;
            r.dim.gameObject.SetActive(true);
            StoryUI.Lock(r);
        }

        private void Awake()
        {
            var ui = StoryUI.Instance;
            dim = ui.MakeFullScreen(ui.Root, "NoteReader", new Color(0f, 0f, 0f, 0.78f));
            var paper = ui.MakePanel(dim.transform, "Paper", new Color(0.92f, 0.89f, 0.8f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(780f, 860f));
            var ink = new Color(0.16f, 0.13f, 0.12f, 1f);
            title = ui.MakeText(paper.transform, "Title", 26, TextAnchor.UpperLeft, new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(660f, 40f), shadow: false);
            title.color = new Color(0.35f, 0.3f, 0.27f, 1f);
            body = ui.MakeText(paper.transform, "Body", 40, TextAnchor.UpperLeft, new Vector2(0.5f, 1f),
                new Vector2(0f, -120f), new Vector2(660f, 680f), shadow: false);
            body.color = ink;
            body.fontStyle = FontStyle.Italic;
            body.lineSpacing = 1.25f;
            var hint = ui.MakeText(dim.transform, "Hint", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(800f, 40f));
            hint.text = "[E] Put it down";
            hint.color = new Color(1f, 1f, 1f, 0.6f);
            dim.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!dim.gameObject.activeSelf || Time.frameCount == openedFrame) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            {
                if (kb.escapeKey.wasPressedThisFrame) StoryUI.ConsumeEscape();
                Close();
            }
        }

        /// <summary>Puts the note away without running its follow-up.</summary>
        public void Dismiss()
        {
            if (!dim.gameObject.activeSelf) return;
            onClosed = null;
            dim.gameObject.SetActive(false);
            StoryUI.Unlock(this);
        }

        private void Close()
        {
            dim.gameObject.SetActive(false);
            StoryUI.Unlock(this);
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }
    }
}
