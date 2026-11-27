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
        private float burstEnd;
        private float nextStep;
        private float flickerLevel = 1f;
        private float beamIntensity = -1f;     // the beam's full brightness, read the first time it flickers

        public bool HasFlashlight => item != null;
        public bool IsOn { get; private set; }

        /// <summary>True while the flashlight has died for a moment (see Die): F only clicks.</summary>
        public bool Dying { get; private set; }

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
            // Restart chapter after he had picked it up in this loop: hand over a fresh flashlight.
            // (It never comes with him into the next loop: LoopTransition takes the item away.)
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
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                if (Dying) LeftBehind.Audio.GameAudio.Switch2D();     // the switch clicks; nothing happens
                else Toggle();
            }

            UpdateSway();
            if (!Dying) UpdateFlicker();
        }

        /// <summary>
        /// The batteries failing for a moment: the beam sputters, dies for <paramref name="darkSeconds"/>
        /// whatever Walter does, then sputters back. Only if it is on to begin with.
        /// </summary>
        public System.Collections.IEnumerator Die(float darkSeconds)
        {
            if (item == null || !IsOn || Dying) yield break;
            Dying = true;
            ResetFlicker();
            yield return Sputter(0.8f);
            SetLevel(0f);
            yield return new WaitForSeconds(darkSeconds);
            yield return Sputter(0.6f);
            if (IsOn) SetLevel(1f);
            Dying = false;
        }

        private System.Collections.IEnumerator Sputter(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                SetLevel(Random.value < 0.5f ? 0.02f : Random.Range(0.25f, 0.9f));
                yield return new WaitForSeconds(Random.Range(0.04f, 0.13f));
            }
        }

        /// <summary>
        /// Near something wrong (a FlickerSource) the beam stutters: short bursts in which it dips,
        /// sometimes nearly out, then comes back. The nearer Walter is, the more often they come.
        /// </summary>
        private void UpdateFlicker()
        {
            float strength = IsOn ? LeftBehind.Story.FlickerSource.Strength(transform.position) : 0f;
            if (Time.time >= burstEnd)
            {
                // Between bursts: steady, and now and then a new burst (up to about two a second when right beside it).
                if (strength > 0f && Random.value < strength * strength * 2f * Time.deltaTime)
                {
                    burstEnd = Time.time + Random.Range(0.15f, 0.5f);
                    nextStep = 0f;
                }
                else
                {
                    SetLevel(1f);
                    return;
                }
            }
            if (Time.time < nextStep) return;
            nextStep = Time.time + Random.Range(0.03f, 0.09f);
            float r = Random.value;
            SetLevel(r < 0.25f ? 0.04f : r < 0.55f ? 0.25f : r < 0.8f ? 0.6f : 1f);
        }

        /// <summary>Back to full brightness, ending any burst (switching it, or carrying it to the next loop).</summary>
        private void ResetFlicker()
        {
            if (item != null && item.Beam != null && beamIntensity >= 0f) item.Beam.intensity = beamIntensity;
            flickerLevel = 1f;
            burstEnd = 0f;
        }

        /// <summary>The beam and bulb at a fraction of their full brightness.</summary>
        private void SetLevel(float level)
        {
            if (item == null || !IsOn || Mathf.Approximately(level, flickerLevel)) return;
            flickerLevel = level;
            if (item.Beam != null)
            {
                if (beamIntensity < 0f) beamIntensity = item.Beam.intensity;
                item.Beam.intensity = beamIntensity * level;
            }
            var bulb = item.BulbRenderer;
            if (bulb != null)
            {
                block ??= new MaterialPropertyBlock();
                bulb.GetPropertyBlock(block, item.BulbMaterialIndex);
                block.SetColor(EmissionColorId, bulbEmission * level);
                bulb.SetPropertyBlock(block, item.BulbMaterialIndex);
            }
        }

        public void Toggle()
        {
            SetOn(!IsOn);
            if (toggleSound != null) toggleSound.Play();
            else LeftBehind.Audio.GameAudio.Switch2D();
        }

        public void SetOn(bool on)
        {
            ResetFlicker();
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
