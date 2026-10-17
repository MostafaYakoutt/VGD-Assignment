using System.Collections;
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

        private bool rightMouseWasPressed;
        private float baseRotationSpeed;
        private Coroutine cinematic;

        public static CameraDirector Instance { get; private set; }

        public bool IsLookingCloser { get; private set; }
        public bool IsInCinematic { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (player != null) baseRotationSpeed = player.RotationSpeed;
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

        /// <summary>Cuts to the fixed loop shot, holds it, then returns to first person.</summary>
        public void PlayLoopShot(float holdSeconds)
        {
            if (cinematic != null) StopCoroutine(cinematic);
            cinematic = StartCoroutine(LoopShotRoutine(holdSeconds));
        }

        private IEnumerator LoopShotRoutine(float holdSeconds)
        {
            SetLoopShot(true);
            yield return new WaitForSeconds(holdSeconds);
            SetLoopShot(false);
            cinematic = null;
        }
    }
}
