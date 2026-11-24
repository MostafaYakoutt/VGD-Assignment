using System.Text;
using LeftBehind.Interaction;
using LeftBehind.Loop;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// The Pockets screen (Tab): what Walter is carrying, each item described in his
    /// own words, and the notes he has read so far, which can be read again with the
    /// number keys. Items are never dropped, so progress cannot be lost.
    /// </summary>
    public class Pockets : MonoBehaviour
    {
        private Image dim;
        private Text itemsText;
        private Text notesText;
        private bool open;
        private bool reading;

        public static bool IsOpen => StoryUI.Exists && StoryUI.Instance.GetComponent<Pockets>().open;

        private void Awake()
        {
            var ui = StoryUI.Instance;
            dim = ui.MakeFullScreen(ui.Root, "Pockets", new Color(0f, 0f, 0f, 0.7f));
            var panel = ui.MakePanel(dim.transform, "Panel", new Color(0.07f, 0.06f, 0.05f, 0.94f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1500f, 820f));
            var head = ui.MakeText(panel.transform, "Header", 40, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(60f, -40f), new Vector2(800f, 50f));
            head.text = "POCKETS";
            var itemsHead = ui.MakeText(panel.transform, "ItemsHeader", 28, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(60f, -120f), new Vector2(700f, 40f));
            itemsHead.text = "Carrying";
            itemsHead.color = new Color(1f, 0.85f, 0.62f, 1f);
            itemsText = ui.MakeText(panel.transform, "Items", 26, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(60f, -170f), new Vector2(760f, 600f));
            var notesHead = ui.MakeText(panel.transform, "NotesHeader", 28, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(880f, -120f), new Vector2(560f, 40f));
            notesHead.text = "Notes";
            notesHead.color = new Color(1f, 0.85f, 0.62f, 1f);
            notesText = ui.MakeText(panel.transform, "Notes", 26, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(880f, -170f), new Vector2(560f, 600f));
            var hint = ui.MakeText(panel.transform, "Hint", 22, TextAnchor.LowerLeft, new Vector2(0f, 0f),
                new Vector2(60f, 30f), new Vector2(1300f, 40f));
            hint.text = "[Tab] Close      [1-9] Read a note again";
            hint.color = new Color(1f, 1f, 1f, 0.55f);
            dim.gameObject.SetActive(false);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || reading) return;

            if (kb.tabKey.wasPressedThisFrame)
            {
                if (open) Close();
                else if (CanOpen()) Open();
                return;
            }
            if (!open) return;
            if (kb.escapeKey.wasPressedThisFrame) { StoryUI.ConsumeEscape(); Close(); return; }

            var notes = StoryStateManager.Notes;
            for (int i = 0; i < Mathf.Min(9, notes.Count); i++)
            {
                if (kb[Key.Digit1 + i].wasPressedThisFrame || kb[Key.Numpad1 + i].wasPressedThisFrame)
                {
                    reading = true;
                    dim.gameObject.SetActive(false);
                    NoteReader.Open(notes[i].title, notes[i].body, () =>
                    {
                        reading = false;
                        dim.gameObject.SetActive(open);
                    });
                    return;
                }
            }
        }

        private static bool CanOpen()
        {
            if (LoopTransition.Busy || NoteReader.IsOpen) return false;
            var interactor = FindAnyObjectByType<PlayerInteractor>();
            // Not while inspecting an object or while something else holds the player.
            if (interactor == null || !interactor.enabled || interactor.Focus != null) return false;
            return !StoryUI.Locked;
        }

        private void Open()
        {
            Refresh();
            open = true;
            dim.gameObject.SetActive(true);
            StoryUI.Lock(this);
        }

        public void Dismiss()
        {
            if (open) Close();
            reading = false;
        }

        private void Close()
        {
            open = false;
            dim.gameObject.SetActive(false);
            StoryUI.Unlock(this);
        }

        private void Refresh()
        {
            var sb = new StringBuilder();
            var items = StoryStateManager.Items;
            if (items.Count == 0) sb.Append("<i>Nothing. Empty pockets.</i>");
            foreach (var item in items)
                sb.Append("<b>").Append(item.name).Append("</b>\n<i>").Append(item.description).Append("</i>\n\n");
            itemsText.text = sb.ToString();

            sb.Clear();
            var notes = StoryStateManager.Notes;
            if (notes.Count == 0) sb.Append("<i>No notes yet.</i>");
            for (int i = 0; i < notes.Count; i++)
                sb.Append(i < 9 ? $"{i + 1}.  " : "     ").Append(notes[i].title).Append("\n\n");
            notesText.text = sb.ToString();
        }
    }
}
