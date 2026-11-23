using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// The fuse box. Without the spare fuse Walter only finds the blown one; with it in his
    /// pockets, E fits the fuse (it is used up), sets the power flag and the dead lamps
    /// come back on.
    /// </summary>
    public class FuseBox : Interactable
    {
        [SerializeField] private string fuseItemId = "spareFuse";
        [SerializeField] private string powerFlag = "powerRestored";
        [SerializeField] private DialogueData noFuse;
        [SerializeField] private DialogueData fitted;

        public override string Prompt => StoryStateManager.HasItem(fuseItemId) ? "Fit the spare fuse" : promptText;

        public override bool CanInteract => base.CanInteract && !StoryStateManager.Has(powerFlag);

        private void Reset() => promptText = "Open the fuse box";

        public void Configure(string fuseId, string flag, DialogueData missing, DialogueData done)
        {
            promptText = "Open the fuse box";
            fuseItemId = fuseId;
            powerFlag = flag;
            noFuse = missing;
            fitted = done;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!StoryStateManager.HasItem(fuseItemId))
            {
                if (!DialogueManager.Busy) DialogueManager.Play(noFuse);
                return;
            }
            StoryStateManager.RemoveItem(fuseItemId);
            LeftBehind.Audio.GameAudio.Switch(transform.position);
            StoryStateManager.Set(powerFlag);
            DialogueManager.Play(fitted);
        }
    }
}
