using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A note, label or letter the player can read with E. Reading it opens the note
    /// reader, copies it to the Notes list in Pockets and, the first time, sets its flag,
    /// plays a follow-up conversation and updates the objective.
    /// </summary>
    public class ReadableNote : Interactable
    {
        [SerializeField] private string noteId = "";
        [SerializeField] private string title = "A note";
        [TextArea(3, 8)] [SerializeField] private string body = "";
        [SerializeField] private string flagToSet = "";
        [SerializeField] private DialogueData afterReading;
        [SerializeField] private string objective = "";

        private bool readOnce;

        private void Reset() => promptText = "Read the note";

        public void Configure(string id, string noteTitle, string noteBody, string flag = "", DialogueData after = null,
            string obj = "", string prompt = "Read the note")
        {
            noteId = id;
            title = noteTitle;
            body = noteBody;
            flagToSet = flag;
            afterReading = after;
            objective = obj;
            promptText = prompt;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            StoryStateManager.AddNote(new NoteEntry { id = noteId, title = title, body = body });
            bool first = !readOnce && !StoryStateManager.Has(flagToSet);
            readOnce = true;
            NoteReader.Open(title, body, () =>
            {
                if (!first) return;
                StoryStateManager.Set(flagToSet);
                DialogueManager.Play(afterReading, () =>
                {
                    if (!string.IsNullOrEmpty(objective)) StoryStateManager.SetObjective(objective);
                });
            });
        }
    }
}
