using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// Turns a lamp's lights (and any emissive bulb) on and off with E.
    /// </summary>
    public class LampSwitch : Interactable
    {
        [SerializeField] private Light[] lights;
        [Tooltip("Renderers whose emission is switched off with the lamp, such as a bulb.")]
        [SerializeField] private Renderer[] emissiveRenderers;
        [SerializeField] private bool isOn = true;
        [SerializeField] private string turnOnText = "Turn on lamp";
        [SerializeField] private string turnOffText = "Turn off lamp";
        [SerializeField] private AudioSource clickSound;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;

        public bool IsOn => isOn;

        public override string Prompt => isOn ? turnOffText : turnOnText;

        private void Reset()
        {
            lights = GetComponentsInChildren<Light>(true);
        }

        private void Start()
        {
            if (lights == null || lights.Length == 0) lights = GetComponentsInChildren<Light>(true);
            Apply();
        }

        public override void Interact(PlayerInteractor interactor)
        {
            SetOn(!isOn);
            if (clickSound != null) clickSound.Play();
        }

        public void SetOn(bool on)
        {
            isOn = on;
            Apply();
        }

        private void Apply()
        {
            foreach (var l in lights)
                if (l != null) l.enabled = isOn;

            if (emissiveRenderers == null) return;
            block ??= new MaterialPropertyBlock();
            foreach (var r in emissiveRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                if (isOn) block.Clear();
                else block.SetColor(EmissionColorId, Color.black);
                r.SetPropertyBlock(block);
            }
        }
    }
}
