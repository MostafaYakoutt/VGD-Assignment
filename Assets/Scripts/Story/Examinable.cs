using LeftBehind.Interaction;

namespace LeftBehind.Story
{
    /// <summary>
    /// Something the player can look at with E (the knife block, a drawing, a toy). It does
    /// nothing itself; a NarrativeTrigger listening to it plays Walter's reaction.
    /// </summary>
    public class Examinable : Interactable
    {
        private void Reset() => promptText = "Look";

        public void Configure(string prompt) => promptText = prompt;

        public override void Interact(PlayerInteractor interactor) { }
    }
}
