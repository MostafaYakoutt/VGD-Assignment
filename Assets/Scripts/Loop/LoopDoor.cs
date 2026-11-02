using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Loop
{
    /// <summary>
    /// The Loop Door at the end of the Upper Landing. Using it starts the
    /// rewind into the next, earlier loop. A door without a next scene
    /// stays shut (the loop after it is not built yet).
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

        private InteractionHUD hud;

        public override string Prompt => string.IsNullOrEmpty(nextScene) ? closedText : promptText;

        public override bool CanInteract => base.CanInteract && !LoopTransition.Busy;

        private void Reset()
        {
            promptText = "Open the door";
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (string.IsNullOrEmpty(nextScene))
            {
                if (hud == null) hud = FindAnyObjectByType<InteractionHUD>();
                if (hud != null) hud.ShowHint("Not yet.", 2.5f);
                return;
            }
            LoopTransition.Play(nextScene, nextLoop, arrivalCaption);
        }
    }
}
