using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Something Walter takes with E: he carries it from then on, it sets its flag and can
    /// start a conversation. The object either disappears (a fuse picked up) or stays where
    /// it is (a box the key was taken out of).
    /// </summary>
    public class PickupItem : Interactable
    {
        [SerializeField] private CarriedItem item;
        [SerializeField] private string flagToSet = "";
        [SerializeField] private DialogueData dialogue;
        [Tooltip("Hide the object once taken; otherwise it stays but can no longer be used.")]
        [SerializeField] private bool hideWhenTaken = true;

        private bool taken;

        public override bool CanInteract => base.CanInteract && !taken;

        public void Configure(string prompt, string id, string flag, DialogueData d, bool hide)
        {
            promptText = prompt;
            item = new CarriedItem { id = id };
            flagToSet = flag;
            dialogue = d;
            hideWhenTaken = hide;
        }

        private void Start()
        {
            // Already taken in this playthrough (e.g. the scene was reloaded).
            if (StoryStateManager.HasItem(item.id)) Take(silent: true);
        }

        public override void Interact(PlayerInteractor interactor) => Take(silent: false);

        private void Take(bool silent)
        {
            if (taken) return;
            taken = true;
            StoryStateManager.AddItem(item.id);
            StoryStateManager.Set(flagToSet);
            if (!silent) DialogueManager.Play(dialogue);
            if (hideWhenTaken) gameObject.SetActive(false);
        }
    }
}
