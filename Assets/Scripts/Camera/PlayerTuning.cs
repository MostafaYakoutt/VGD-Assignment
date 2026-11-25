using LeftBehind.Interaction;
using LeftBehind.Loop;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LeftBehind.CameraSystem
{
    /// <summary>
    /// How Walter moves and sees in each loop, set when a loop scene loads. He is older the
    /// earlier the loop is in the game, so in Loop 1 he walks slowest, stands lowest and his
    /// head bobs most; by Loop 3 he is fifty-two again. Also sets the first-person field of view,
    /// the look-closer view and how far he can reach.
    /// </summary>
    public static class PlayerTuning
    {
        // Loop 1 (aged 72), Loop 2 (54), Loop 3 (52).
        private static readonly float[] WalkSpeed = { 2.2f, 2.5f, 2.8f };
        private static readonly float[] EyeHeight = { 1.55f, 1.60f, 1.65f };
        private static readonly float[] BobAmount = { 0.022f, 0.015f, 0.011f };

        public const float FieldOfView = 70f;
        public const float LookCloserFieldOfView = 45f;
        public const float Reach = 1.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Apply();
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) => Apply();

        private static void Apply()
        {
            var fpc = Object.FindAnyObjectByType<FirstPersonController>();
            var env = Object.FindAnyObjectByType<LoopEnvironment>();
            if (fpc == null || env == null) return;
            int i = Mathf.Clamp(env.Loop, 1, 3) - 1;

            fpc.MoveSpeed = WalkSpeed[i];
            fpc.SprintSpeed = WalkSpeed[i];
            var root = fpc.CinemachineCameraTarget.transform;
            var p = root.localPosition;
            p.y = EyeHeight[i] / Mathf.Max(0.01f, fpc.transform.lossyScale.y);
            root.localPosition = p;

            var bob = fpc.GetComponent<HeadBob>();
            if (bob == null) bob = fpc.gameObject.AddComponent<HeadBob>();
            bob.Configure(root, BobAmount[i]);

            foreach (var cam in Object.FindObjectsByType<CinemachineCamera>())
            {
                if (cam.name == "CM_FirstPerson") cam.Lens.FieldOfView = FieldOfView;
                else if (cam.name == "CM_LookCloser") cam.Lens.FieldOfView = LookCloserFieldOfView;
            }

            var interactor = fpc.GetComponent<PlayerInteractor>();
            if (interactor == null) interactor = Object.FindAnyObjectByType<PlayerInteractor>();
            if (interactor != null) interactor.Range = Reach;
        }
    }

    /// <summary>
    /// A small head bob while walking: the view dips with each step and sways side to side
    /// over every two steps. It eases out when he stops.
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        private Transform head;
        private float amount;
        private Vector3 basePos;
        private Vector3 last;
        private float phase;
        private float weight;
        private CharacterController controller;

        public void Configure(Transform cameraRoot, float bobAmount)
        {
            if (head != null) head.localPosition = basePos;
            head = cameraRoot;
            amount = bobAmount;
            basePos = cameraRoot.localPosition;
            last = transform.position;
        }

        private void Awake() => controller = GetComponent<CharacterController>();

        private void LateUpdate()
        {
            if (head == null) return;
            var delta = transform.position - last;
            last = transform.position;
            delta.y = 0f;
            float d = delta.magnitude;
            bool walking = d > 0.0005f && d < 0.5f && controller != null && controller.enabled && controller.isGrounded;
            weight = Mathf.MoveTowards(weight, walking ? 1f : 0f, Time.deltaTime * 3f);
            if (walking) phase += d / LeftBehind.Audio.Footsteps.Stride * Mathf.PI;     // half a cycle per step
            float scale = 1f / Mathf.Max(0.01f, transform.lossyScale.y);
            float dip = -Mathf.Abs(Mathf.Sin(phase)) * amount;
            float sway = Mathf.Sin(phase * 0.5f) * amount * 0.6f;
            head.localPosition = basePos + new Vector3(sway, dip, 0f) * scale * weight;
        }
    }
}
