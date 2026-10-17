using UnityEngine;
using UnityEngine.InputSystem;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// Casts a ray from the camera centre, shows the prompt of the
    /// Interactable being looked at, and uses it when E is pressed.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float range = 2.5f;
        [SerializeField] private LayerMask layers = ~0;
        [SerializeField] private InteractionHUD hud;

        private Interactable current;

        public Interactable Current => current;

        /// <summary>
        /// While set (for example an object being inspected), E always goes
        /// to this Interactable instead of whatever the ray hits.
        /// </summary>
        public Interactable Focus { get; set; }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (hud == null) hud = FindAnyObjectByType<InteractionHUD>();
        }

        private void Update()
        {
            current = FindTarget();

            if (hud != null)
            {
                if (current != null) hud.ShowPrompt("[E] " + current.Prompt);
                else hud.HidePrompt();
            }

            var keyboard = Keyboard.current;
            if (current != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                current.Interact(this);
            }
        }

        private Interactable FindTarget()
        {
            if (Focus != null) return Focus;
            if (viewCamera == null) return null;

            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, range, layers, QueryTriggerInteraction.Ignore))
                return null;

            var target = hit.collider.GetComponentInParent<Interactable>();
            return target != null && target.CanInteract ? target : null;
        }
    }
}
