using StarterAssets;
using UnityEngine;

namespace LeftBehind.CameraSystem
{
    /// <summary>
    /// Walter's body (the Walter prefab) under the player. The first-person camera sits inside his
    /// head, so the body is hidden while the player is Walter and shown when the camera pulls away
    /// from him for the loop shot. It is sized so his eyes are at the loop's eye height (lower when
    /// he is older; see PlayerTuning).
    /// </summary>
    public class PlayerBody : MonoBehaviour
    {
        /// <summary>Eye height of the Walter prefab at scale 1 (the prefab is 1.75 m tall).</summary>
        private const float ModelEyeHeight = 1.60f;

        private Renderer[] renderers;
        private FirstPersonController player;
        private bool shown = true;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            player = GetComponentInParent<FirstPersonController>();
            Show(false);
        }

        private void Start()
        {
            // PlayerTuning sets the eye height as the scene loads, before Start.
            if (player == null || player.CinemachineCameraTarget == null) return;
            float eyeHeight = player.CinemachineCameraTarget.transform.position.y - transform.position.y;
            if (eyeHeight > 0.5f) FitTo(transform, eyeHeight / ModelEyeHeight);
        }

        /// <summary>
        /// Scales the body evenly to <paramref name="worldScale"/>, undoing the player capsule's own
        /// uneven scale (the Starter Assets capsule is squashed), so Walter is never stretched.
        /// </summary>
        public static void FitTo(Transform body, float worldScale)
        {
            var parent = body.parent != null ? body.parent.lossyScale : Vector3.one;
            body.localScale = new Vector3(worldScale / parent.x, worldScale / parent.y, worldScale / parent.z);
        }

        private void LateUpdate()
        {
            var director = CameraDirector.Instance;
            Show(director != null && director.IsInCinematic);
        }

        private void Show(bool on)
        {
            if (on == shown) return;
            shown = on;
            foreach (var r in renderers)
                if (r != null) r.enabled = on;
        }
    }
}
