using System;
using System.Collections;
using UnityEngine;

namespace LeftBehind.Loop
{
    /// <summary>
    /// Lightning for the storm in Loop 3: every so often the sky flashes once or twice,
    /// briefly lighting the house through the windows, then the scene's own lighting
    /// (set by its LoopEnvironment) comes back. A scare can also call a strike when it needs one
    /// (the figure on the landing; see LightningFigure).
    /// </summary>
    public class Lightning : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Vector2 interval = new Vector2(7f, 18f);
        [SerializeField] private float flashIntensity = 2.2f;
        [SerializeField] private Color flashAmbient = new Color(0.32f, 0.36f, 0.45f);
        [SerializeField] private Color flashColor = new Color(0.8f, 0.86f, 1f);

        private bool striking;
        private float nextStrike;

        public static Lightning Instance { get; private set; }

        public void Configure(Light s) => sun = s;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            nextStrike = Time.time + UnityEngine.Random.Range(3f, 6f);
            while (true)
            {
                while (striking || Time.time < nextStrike) yield return null;
                yield return Strike(UnityEngine.Random.value < 0.6f ? 2 : 1, 0f, null);
            }
        }

        /// <summary>
        /// One strike: <paramref name="flashes"/> flashes in quick succession, then thunder.
        /// <paramref name="firstLength"/> holds the first flash longer (0: the usual).
        /// <paramref name="onFlash"/> is told when each flash starts (true) and ends (false).
        /// </summary>
        public IEnumerator Strike(int flashes, float firstLength, Action<int, bool> onFlash)
        {
            while (striking) yield return null;
            striking = true;
            for (int i = 0; i < flashes; i++)
            {
                float length = i == 0 && firstLength > 0f ? firstLength : UnityEngine.Random.Range(0.05f, 0.11f);
                onFlash?.Invoke(i, true);
                yield return Flash(length);
                onFlash?.Invoke(i, false);
                // A called strike leaves longer between its flashes, so the second is seen on its own.
                yield return new WaitForSeconds(onFlash != null ? UnityEngine.Random.Range(0.7f, 1.0f) : UnityEngine.Random.Range(0.06f, 0.18f));
            }
            LeftBehind.Audio.GameAudio.Thunder(UnityEngine.Random.Range(0.3f, 1.6f));
            nextStrike = Time.time + UnityEngine.Random.Range(interval.x, interval.y);
            striking = false;
        }

        private IEnumerator Flash(float length)
        {
            float baseIntensity = sun != null ? sun.intensity : 0f;
            Color baseColor = sun != null ? sun.color : Color.white;
            Color baseAmbient = RenderSettings.ambientLight;
            if (sun != null)
            {
                sun.intensity = flashIntensity;
                sun.color = flashColor;
            }
            RenderSettings.ambientLight = flashAmbient;
            yield return new WaitForSeconds(length);
            if (sun != null)
            {
                sun.intensity = baseIntensity;
                sun.color = baseColor;
            }
            RenderSettings.ambientLight = baseAmbient;
        }
    }
}
