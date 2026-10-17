using StarterAssets;
using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// An object the player can pick up and look at. E brings it in front
    /// of the camera and freezes movement and mouse look; E again puts it
    /// back where it was and gives control back.
    /// The object's front is its local +Z, which is turned to face the camera.
    /// </summary>
    public class Inspectable : Interactable
    {
        [SerializeField] private string putBackText = "Put back";
        [Tooltip("Distance in front of the camera while inspecting.")]
        [SerializeField] private float inspectDistance = 0.6f;
        [Tooltip("Extra rotation applied while inspecting, in degrees.")]
        [SerializeField] private Vector3 inspectEuler = Vector3.zero;
        [Tooltip("Seconds to move to and from the camera.")]
        [SerializeField] private float moveTime = 0.35f;

        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3 visualCentreLocal;
        private Vector3 fromPosition;
        private Quaternion fromRotation;
        private float t = 1f;
        private bool inspecting;
        private PlayerInteractor holder;
        private FirstPersonController controller;
        private Camera viewCamera;

        public bool IsInspecting => inspecting;

        public override string Prompt => inspecting ? putBackText : promptText;

        private void Reset()
        {
            promptText = "Inspect";
        }

        private void Awake()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;

            // The pivot is often at the base, so centre the model's bounds in view instead.
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                visualCentreLocal = transform.InverseTransformPoint(bounds.center);
            }
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (inspecting) EndInspect();
            else BeginInspect(interactor);
        }

        private void BeginInspect(PlayerInteractor interactor)
        {
            holder = interactor;
            controller = interactor.GetComponent<FirstPersonController>();
            viewCamera = Camera.main;

            // Only take a new home pose when the object is resting in place.
            if (t >= 1f)
            {
                homePosition = transform.position;
                homeRotation = transform.rotation;
            }

            if (controller != null) controller.enabled = false;
            interactor.Focus = this;

            inspecting = true;
            StartMove();
        }

        private void EndInspect()
        {
            inspecting = false;
            StartMove();
        }

        private void StartMove()
        {
            fromPosition = transform.position;
            fromRotation = transform.rotation;
            t = 0f;
        }

        private void LateUpdate()
        {
            if (t >= 1f) return;

            t = moveTime > 0f ? Mathf.Min(1f, t + Time.deltaTime / moveTime) : 1f;
            float k = Mathf.SmoothStep(0f, 1f, t);

            Vector3 targetPosition;
            Quaternion targetRotation;
            if (inspecting && viewCamera != null)
            {
                Transform cam = viewCamera.transform;
                targetRotation = Quaternion.LookRotation(-cam.forward, cam.up) * Quaternion.Euler(inspectEuler);
                Vector3 centreOffset = targetRotation * Vector3.Scale(visualCentreLocal, transform.lossyScale);
                targetPosition = cam.position + cam.forward * inspectDistance - centreOffset;
            }
            else
            {
                targetPosition = homePosition;
                targetRotation = homeRotation;
            }

            transform.SetPositionAndRotation(
                Vector3.Lerp(fromPosition, targetPosition, k),
                Quaternion.Slerp(fromRotation, targetRotation, k));

            // Back home: hand control back to the player.
            if (t >= 1f && !inspecting) Release();
        }

        private void Release()
        {
            if (holder != null && holder.Focus == this) holder.Focus = null;
            if (controller != null) controller.enabled = true;
            holder = null;
            controller = null;
        }
    }
}
