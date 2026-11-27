using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// Shows a found note full screen as a sheet of paper in Walter's handwriting.
    /// The player is frozen while reading; E, Esc or Enter puts it down.
    /// Some notes push in: the paper slowly grows to fill the view, the room behind it darkens and
    /// the camera behind closes in (the note on the study door).
    /// </summary>
    public class NoteReader : MonoBehaviour
    {
        private Image dim;
        private RectTransform paper;
        private RectTransform paperShadow;
        private bool pushIn;
        private float pushStart;
        private Text title;
        private Text body;
        private Action onClosed;
        private int openedFrame;

        public static bool IsOpen => StoryUI.Exists && StoryUI.Instance.GetComponent<NoteReader>().dim.gameObject.activeSelf;

        public static void Open(string titleText, string bodyText, Action closed = null, bool pushIn = false)
        {
            var r = StoryUI.Instance.GetComponent<NoteReader>();
            r.title.text = titleText;
            r.body.text = bodyText;
            r.onClosed = closed;
            r.openedFrame = Time.frameCount;
            r.pushIn = pushIn;
            r.pushStart = Time.unscaledTime;
            r.paper.localScale = r.paperShadow.localScale = Vector3.one;
            r.dim.color = new Color(0f, 0f, 0f, 0.78f);
            r.dim.gameObject.SetActive(true);
            StoryUI.Lock(r);
            if (pushIn && LeftBehind.CameraSystem.CameraDirector.Instance != null)
                LeftBehind.CameraSystem.CameraDirector.Instance.PushIn(true);
        }

        private void Awake()
        {
            var ui = StoryUI.Instance;
            dim = ui.MakeFullScreen(ui.Root, "NoteReader", new Color(0f, 0f, 0f, 0.78f));
            // A sheet of aged paper, set down slightly crooked, with a soft shadow under it.
            var shadowImage = ui.MakePanel(dim.transform, "PaperShadow", new Color(0f, 0f, 0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(10f, 8f), new Vector2(790f, 870f));
            shadowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            var paperImage = ui.MakePanel(dim.transform, "Paper", Color.white, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(780f, 860f));
            paperImage.sprite = UIStyle.Paper;
            paper = paperImage.rectTransform;
            paper.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            paperShadow = shadowImage.rectTransform;
            title = ui.MakeText(paper, "Title", 24, TextAnchor.UpperLeft, new Vector2(0.5f, 1f),
                new Vector2(0f, -44f), new Vector2(660f, 40f), shadow: false);
            UIStyle.Italic(title);
            title.color = new Color(0.36f, 0.3f, 0.24f, 0.85f);
            // Walter's own hand, in pencil.
            body = ui.MakeText(paper, "Body", 54, TextAnchor.UpperLeft, new Vector2(0.5f, 1f),
                new Vector2(0f, -104f), new Vector2(660f, 700f), shadow: false);
            body.font = UIStyle.Hand;
            body.color = UIStyle.Graphite;
            body.lineSpacing = 0.95f;
            var hint = ui.MakeText(dim.transform, "Hint", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(800f, 40f));
            UIStyle.Italic(hint);
            hint.text = UIStyle.KeyHint("[E] Put it down");
            hint.color = new Color(UIStyle.Ink.r, UIStyle.Ink.g, UIStyle.Ink.b, 0.7f);
            dim.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (dim.gameObject.activeSelf && pushIn)
            {
                // Slowly, over about twelve seconds: never enough to notice at once.
                float k = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - pushStart) / 12f);
                paper.localScale = paperShadow.localScale = Vector3.one * (1f + 0.09f * k);
                dim.color = new Color(0f, 0f, 0f, 0.78f + 0.08f * k);
            }
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
            EndPushIn();
            StoryUI.Unlock(this);
        }

        private void EndPushIn()
        {
            if (!pushIn) return;
            pushIn = false;
            paper.localScale = paperShadow.localScale = Vector3.one;
            if (LeftBehind.CameraSystem.CameraDirector.Instance != null)
                LeftBehind.CameraSystem.CameraDirector.Instance.PushIn(false);
        }

        private void Close()
        {
            dim.gameObject.SetActive(false);
            EndPushIn();
            StoryUI.Unlock(this);
            var cb = onClosed;
            onClosed = null;
            cb?.Invoke();
        }
    }
}
