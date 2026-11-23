using LeftBehind.Interaction;
using LeftBehind.Story;
using UnityEngine;

namespace LeftBehind.Loop
{
    /// <summary>
    /// The Loop Door at the end of the Upper Landing. Using it starts the
    /// rewind into the next, earlier loop, once the story flags it needs are set
    /// (otherwise Walter says why not). A door without a next scene stays shut
    /// (the loop after it is not built yet).
    /// </summary>
    public class LoopDoor : Interactable
    {
        [Tooltip("Scene of the next loop, as listed in Build Settings.")]
        [SerializeField] private string nextScene = "";
        [Tooltip("Loop number the next scene represents.")]
        [SerializeField] private int nextLoop = 2;
        [Tooltip("Shown on arrival, naming the time period.")]
        [SerializeField] private string arrivalCaption = "";
        [SerializeField] private string closedText = "It won't open";
        [Tooltip("Story flags that must all be set before the door opens.")]
        [SerializeField] private string[] requiredFlags = new string[0];
        [Tooltip("Played when a required flag is missing.")]
        [SerializeField] private DialogueData notYetDialogue;
        [Tooltip("Set when the door starts the rewind, e.g. loop1Complete.")]
        [SerializeField] private string completeFlag = "";

        private InteractionHUD hud;

        public override string Prompt => string.IsNullOrEmpty(nextScene) ? closedText : promptText;

        public override bool CanInteract => base.CanInteract && !LoopTransition.Busy;

        private void Reset()
        {
            promptText = "Open the door";
        }

        public void ConfigureGate(string[] required, DialogueData notYet, string complete)
        {
            requiredFlags = required ?? new string[0];
            notYetDialogue = notYet;
            completeFlag = complete;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!StoryStateManager.HasAll(requiredFlags))
            {
                if (notYetDialogue != null)
                {
                    if (!DialogueManager.Busy) DialogueManager.Play(notYetDialogue);
                }
                else ShowHint("Not yet.");
                return;
            }
            if (string.IsNullOrEmpty(nextScene))
            {
                ShowHint("Not yet.");
                return;
            }
            if (LoopTransition.Play(nextScene, nextLoop, arrivalCaption)) StoryStateManager.Set(completeFlag);
        }

        private void ShowHint(string text)
        {
            if (hud == null) hud = FindAnyObjectByType<InteractionHUD>();
            if (hud != null) hud.ShowHint(text, 2.5f);
        }
    }
}
