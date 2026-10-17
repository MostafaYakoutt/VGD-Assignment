using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// Base class for anything the player can use with E.
    /// Needs a collider on this object or one of its children.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Shown on the HUD when the player looks at this object.")]
        [SerializeField] protected string promptText = "Interact";

        public virtual string Prompt => promptText;

        public virtual bool CanInteract => isActiveAndEnabled;

        public abstract void Interact(PlayerInteractor interactor);
    }
}
