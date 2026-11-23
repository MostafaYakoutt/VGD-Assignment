using System.Collections;
using UnityEngine;

namespace LeftBehind.Loop
{
    /// <summary>
    /// Lightning for the storm in Loop 3: every so often the sky flashes once or twice,
    /// briefly lighting the house through the windows, then the scene's own lighting
    /// (set by its LoopEnvironment) comes back.
    /// </summary>
    public class Lightning : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Vector2 interval = new Vector2(7f, 18f);
        [SerializeField] private float flashIntensity = 2.2f;
        [SerializeField] private Color flashAmbient = new Color(0.32f, 0.36f, 0.45f);
        [SerializeField] private Color flashColor = new Color(0.8f, 0.86f, 1f);

        public void Configure(Light s) => sun = s;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(Random.Range(3f, 6f));
            while (true)
            {
                int flashes = Random.value < 0.6f ? 2 : 1;
                for (int i = 0; i < flashes; i++)
                {
                    yield return Flash(Random.Range(0.05f, 0.11f));
                    yield return new WaitForSeconds(Random.Range(0.06f, 0.18f));
                }
                yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
            }
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
