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
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private const int IgnoreRaycastLayer = 2;

        public Interactable Current => current;

        /// <summary>How far the player can reach, in metres from the camera.</summary>
        public float Range
        {
            get => range;
            set => range = value;
        }

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
                var used = current;
                used.Interact(this);
                used.NotifyUsed(this);
            }
        }

        private Interactable FindTarget()
        {
            if (Focus != null) return Focus;
            if (viewCamera == null) return null;

            // The camera sits inside Walter's own capsule, so looking steeply down (at a sink or a low
            // table) the ray can hit the capsule first. Skip it, and skip colliders on the Ignore Raycast
            // layer (the invisible step blockers over furniture), and take the nearest other hit.
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            int n = Physics.RaycastNonAlloc(ray, hits, range, layers, QueryTriggerInteraction.Ignore);
            Collider nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (c is CharacterController || c.gameObject.layer == IgnoreRaycastLayer || hits[i].distance >= nearestDistance) continue;
                nearest = hits[i].collider;
                nearestDistance = hits[i].distance;
            }
            if (nearest == null) return null;

            var target = nearest.GetComponentInParent<Interactable>();
            return target != null && target.CanInteract ? target : null;
        }
    }
}
