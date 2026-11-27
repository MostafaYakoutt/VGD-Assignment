using System.Collections;
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
            else LeftBehind.Audio.GameAudio.Switch(transform.position);
        }

        public void SetOn(bool on)
        {
            isOn = on;
            Apply();
        }

        /// <summary>
        /// The bulb failing: it buzzes, stutters on and off for <paramref name="seconds"/>, then goes
        /// out with a click. Used when the lamps go out ahead of Walter (LampsGoOut).
        /// </summary>
        public IEnumerator FlickerOut(float seconds)
        {
            if (!isOn) yield break;
            LeftBehind.Audio.GameAudio.LampBuzz(transform.position);
            float end = Time.time + seconds;
            bool lit = true;
            while (Time.time < end)
            {
                lit = !lit || Random.value < 0.3f;
                ShowLit(lit);
                // Dark gaps grow longer as it dies.
                float k = 1f - (end - Time.time) / seconds;
                yield return new WaitForSeconds(lit ? Random.Range(0.03f, 0.12f) : Random.Range(0.04f, 0.1f + 0.25f * k));
            }
            SetOn(false);
            LeftBehind.Audio.GameAudio.Switch(transform.position);
        }

        private void Apply() => ShowLit(isOn);

        private void ShowLit(bool lit)
        {
            foreach (var l in lights)
                if (l != null) l.enabled = lit;

            if (emissiveRenderers == null) return;
            block ??= new MaterialPropertyBlock();
            foreach (var r in emissiveRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                if (lit) block.Clear();
                else block.SetColor(EmissionColorId, Color.black);
                r.SetPropertyBlock(block);
            }
        }
    }
}
