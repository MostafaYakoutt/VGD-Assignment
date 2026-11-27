using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LeftBehind.CameraSystem
{
    /// <summary>
    /// Switches between the Cinemachine cameras by priority:
    /// first person (default), look closer (hold right mouse) and the
    /// fixed loop shot used during the loop transition.
    /// Blend times come from the CinemachineBrain's blend settings.
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera firstPersonCam;
        [SerializeField] private CinemachineCamera lookCloserCam;
        [SerializeField] private CinemachineCamera loopShotCam;
        [SerializeField] private int inactivePriority = 0;
        [SerializeField] private int activePriority = 20;

        [Header("Look closer")]
        [SerializeField] private FirstPersonController player;
        [Tooltip("Mouse sensitivity multiplier while looking closer.")]
        [SerializeField] private float lookCloserSensitivity = 0.5f;

        [Tooltip("Extra vignette while looking closer, so the edges darken.")]
        [SerializeField] private float lookCloserVignette = 0.25f;

        [Header("Push-in")]
        [Tooltip("Degrees the view narrows by during a slow push-in (reading the note on the study door).")]
        [SerializeField] private float pushInDegrees = 12f;
        [Tooltip("Seconds the push-in takes to reach its closest.")]
        [SerializeField] private float pushInSeconds = 10f;

        private float push, pushTarget;
        private bool lensDirty;

        private bool rightMouseWasPressed;
        private UnityEngine.Rendering.Universal.Vignette vignette;
        private float baseVignette;
        private float vignetteBlend;
        private float baseRotationSpeed;

        public static CameraDirector Instance { get; private set; }

        public bool IsLookingCloser { get; private set; }
        public bool IsInCinematic { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (player != null) baseRotationSpeed = player.RotationSpeed;
            // A runtime copy of the volume profile, so the asset itself is never changed.
            var volume = FindAnyObjectByType<UnityEngine.Rendering.Volume>();
            if (volume != null && volume.profile.TryGet(out vignette)) baseVignette = vignette.intensity.value;
            if (firstPersonCam != null) firstPersonCam.Priority = 10;
            SetLookCloser(false);
            SetLoopShot(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            bool pressed = mouse != null && mouse.rightButton.isPressed;
            if (pressed != rightMouseWasPressed)
            {
                rightMouseWasPressed = pressed;
                SetLookCloser(pressed);
            }
            if (vignette != null)
            {
                vignetteBlend = Mathf.MoveTowards(vignetteBlend, IsLookingCloser ? 1f : 0f, Time.deltaTime / 0.35f);
                vignette.intensity.Override(baseVignette + lookCloserVignette * Mathf.SmoothStep(0f, 1f, vignetteBlend));
            }
            UpdatePushIn();
        }

        /// <summary>A slow push-in (true) while Walter reads something terrible; false eases back out.</summary>
        public void PushIn(bool on) => pushTarget = on ? 1f : 0f;

        private void UpdatePushIn()
        {
            // Unscaled: the game may be paused behind the note reader.
            float dt = Time.unscaledDeltaTime;
            push = Mathf.MoveTowards(push, pushTarget, dt / (pushTarget > push ? pushInSeconds : 0.8f));
            if (push == 0f && !lensDirty) return;
            lensDirty = push != 0f;

            float fovLess = pushInDegrees * Mathf.SmoothStep(0f, 1f, push);
            if (firstPersonCam != null) firstPersonCam.Lens.FieldOfView = PlayerTuning.FieldOfView - fovLess;
            if (lookCloserCam != null) lookCloserCam.Lens.FieldOfView = PlayerTuning.LookCloserFieldOfView - fovLess * 0.5f;
        }

        public void SetLookCloser(bool on)
        {
            IsLookingCloser = on && !IsInCinematic;
            if (lookCloserCam != null)
                lookCloserCam.Priority = IsLookingCloser ? activePriority : inactivePriority;
            if (player != null)
                player.RotationSpeed = IsLookingCloser ? baseRotationSpeed * lookCloserSensitivity : baseRotationSpeed;
        }

        public void SetLoopShot(bool on)
        {
            IsInCinematic = on;
            if (on) SetLookCloser(false);
            if (loopShotCam != null)
                loopShotCam.Priority = on ? activePriority + 10 : inactivePriority;
        }
    }
}
