using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// Holds the picked-up flashlight in front of the camera and toggles
    /// its beam with F. Adds a little sway from mouse look and a bob
    /// while walking so the model feels held.
    /// </summary>
    public class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField] private Transform handAnchor;
        [SerializeField] private InteractionHUD hud;

        [Header("Held pose (local to the hand anchor)")]
        [SerializeField] private Vector3 heldPosition = new Vector3(0.2f, -0.2f, 0.35f);
        [Tooltip("Model's lens points along its local +Y, so X = 90 aims it forward.")]
        [SerializeField] private Vector3 heldEuler = new Vector3(86f, -4f, 0f);

        [Header("Sway and bob")]
        [SerializeField] private float swayAmount = 0.6f;
        [SerializeField] private float swayMax = 4f;
        [SerializeField] private float swaySmoothing = 10f;
        [SerializeField] private float bobAmount = 0.008f;
        [SerializeField] private float bobFrequency = 7f;

        [Header("Light")]
        [SerializeField] private Color bulbEmission = new Color(1.6f, 1.5f, 1.3f);
        [SerializeField] private AudioSource toggleSound;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private FlashlightPickup item;
        private Transform model;
        private StarterAssetsInputs input;
        private CharacterController controller;
        private MaterialPropertyBlock block;
        private Vector2 sway;
        private float bobTime;

        public bool HasFlashlight => item != null;
        public bool IsOn { get; private set; }

        // The held flashlight survives the scene change between loops.
        private static FlashlightPickup carried;
        private static bool carriedOn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            carried = null;
            carriedOn = false;
        }

        /// <summary>Called before the next loop loads: keep the held flashlight out of the old scene.</summary>
        public static void StashForSceneChange()
        {
            var holder = FindAnyObjectByType<PlayerFlashlight>();
            if (holder == null || holder.item == null) return;
            carried = holder.item;
            carriedOn = holder.IsOn;
            holder.model.SetParent(null, true);
            DontDestroyOnLoad(holder.model.gameObject);
            holder.item = null;
            holder.model = null;
        }

        /// <summary>Called once the next loop has loaded: hand the flashlight to the new player.</summary>
        public static void RestoreAfterSceneChange()
        {
            if (carried == null) return;
            var holder = FindAnyObjectByType<PlayerFlashlight>();
            if (holder == null) return;
            // Move it back into the loaded scene so it belongs to the player again.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(carried.gameObject, holder.gameObject.scene);
            holder.Equip(carried, showHint: false);
            holder.SetOn(carriedOn);
            carried = null;
        }

        private void Awake()
        {
            input = GetComponent<StarterAssetsInputs>();
            controller = GetComponent<CharacterController>();
            if (hud == null) hud = FindAnyObjectByType<InteractionHUD>();
            AnchorToHead();
        }

        /// <summary>
        /// The hand anchor sits under the Main Camera in the scenes, but Cinemachine moves that camera
        /// to whichever shot is live, so in the loop shot's pull-out the flashlight flew off with it.
        /// Hang it from the player's head (the point the first-person camera locks to, with no damping)
        /// instead: it looks the same in first person and stays with Walter when the camera pulls away.
        /// </summary>
        private void AnchorToHead()
        {
            var fpc = GetComponent<FirstPersonController>();
            if (handAnchor == null || fpc == null || fpc.CinemachineCameraTarget == null) return;
            var head = fpc.CinemachineCameraTarget.transform;
            if (handAnchor.parent == head) return;
            var local = handAnchor.localPosition;
            var localRot = handAnchor.localRotation;
            handAnchor.SetParent(head, false);
            handAnchor.localPosition = local;
            handAnchor.localRotation = localRot;
        }

        private System.Collections.IEnumerator Start()
        {
            // A loop restarted, or begun from Chapters: hand over a fresh flashlight if the story says
            // he has one. (A loop transition carries the real one across; wait until that has happened.)
            while (LeftBehind.Loop.LoopTransition.Busy) yield return null;
            yield return null;
            if (HasFlashlight || !LeftBehind.Story.StoryStateManager.HasItem("flashlight")) yield break;
            var prefab = Resources.Load<FlashlightPickup>("HeldFlashlight");
            if (prefab == null) yield break;
            var copy = Instantiate(prefab);
            var outlineChild = copy.transform.Find("Outline");
            if (outlineChild != null) outlineChild.gameObject.SetActive(false);
            copy.enabled = false;
            Equip(copy, showHint: false);
        }

        public void Equip(FlashlightPickup pickup) => Equip(pickup, showHint: true);

        public void Equip(FlashlightPickup pickup, bool showHint)
        {
            item = pickup;
            model = pickup.transform;

            foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;

            model.SetParent(handAnchor, false);
            model.localPosition = heldPosition;
            model.localRotation = Quaternion.Euler(heldEuler);
            model.localScale = Vector3.one;

            SetOn(false);
            if (showHint && hud != null) hud.ShowHint("[F] Flashlight", 4f);
        }

        private void Update()
        {
            if (item == null) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) Toggle();

            UpdateSway();
        }

        public void Toggle()
        {
            SetOn(!IsOn);
            if (toggleSound != null) toggleSound.Play();
            else LeftBehind.Audio.GameAudio.Switch2D();
        }

        public void SetOn(bool on)
        {
            IsOn = on;
            if (item == null) return;

            if (item.Beam != null) item.Beam.enabled = on;

            var bulb = item.BulbRenderer;
            if (bulb != null)
            {
                block ??= new MaterialPropertyBlock();
                bulb.GetPropertyBlock(block, item.BulbMaterialIndex);
                block.SetColor(EmissionColorId, on ? bulbEmission : Color.black);
                bulb.SetPropertyBlock(block, item.BulbMaterialIndex);
            }
        }

        private void UpdateSway()
        {
            Vector2 look = input != null ? input.look : Vector2.zero;
            Vector2 target = Vector2.ClampMagnitude(-look * swayAmount, swayMax);
            sway = Vector2.Lerp(sway, target, 1f - Mathf.Exp(-swaySmoothing * Time.deltaTime));

            float speed = controller != null ? new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude : 0f;
            float bobWeight = Mathf.Clamp01(speed / 4f);
            bobTime += Time.deltaTime * bobFrequency * bobWeight;
            Vector3 bob = new Vector3(Mathf.Cos(bobTime) * 0.5f, Mathf.Abs(Mathf.Sin(bobTime)) * -1f, 0f) * bobAmount * bobWeight;

            model.localPosition = heldPosition + bob;
            model.localRotation = Quaternion.Euler(heldEuler) * Quaternion.Euler(sway.y, 0f, -sway.x);
        }
    }
}
