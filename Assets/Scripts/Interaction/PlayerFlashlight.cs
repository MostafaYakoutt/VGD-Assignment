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

        private void Awake()
        {
            input = GetComponent<StarterAssetsInputs>();
            controller = GetComponent<CharacterController>();
            if (hud == null) hud = FindAnyObjectByType<InteractionHUD>();
        }

        public void Equip(FlashlightPickup pickup)
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
            if (hud != null) hud.ShowHint("[F] Flashlight", 4f);
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
