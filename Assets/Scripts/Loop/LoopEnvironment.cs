using UnityEngine;

namespace LeftBehind.Loop
{
    /// <summary>
    /// The lighting preset of one loop: sun, ambient light, fog and the
    /// colour behind the fog. Each loop scene has one, so the time of day
    /// and season change with the loop (grey winter afternoon, autumn
    /// evening, rainy night). Applied in the editor as well, so the scene
    /// view matches the game.
    /// </summary>
    [ExecuteAlways]
    public class LoopEnvironment : MonoBehaviour
    {
        [Tooltip("Which loop this scene is: 1 present day, 2 years earlier, 3 the aftermath.")]
        [SerializeField] private int loop = 1;
        [SerializeField] private string period = "Present day, winter afternoon";
        [SerializeField] private Light sun;
        [SerializeField] private Vector3 sunEuler = new Vector3(50f, 330f, 0f);
        [SerializeField] private Color sunColor = new Color(0.82f, 0.88f, 1f);
        [SerializeField] private float sunIntensity = 1f;
        [SerializeField] private Color ambient = new Color(0.12f, 0.13f, 0.16f);
        [SerializeField] private Color fogColor = new Color(0.10f, 0.12f, 0.15f);
        [SerializeField] private float fogDensity = 0.06f;

        public string Period => period;

        public int Loop => loop;

        private void OnEnable()
        {
            Apply();
            if (Application.isPlaying) LoopTransition.EnterLoop(loop);
        }
        private void OnValidate() => Apply();

        [ContextMenu("Apply")]
        public void Apply()
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(sunEuler);
                sun.color = sunColor;
                sun.intensity = sunIntensity;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            foreach (var cam in Camera.allCameras)
                cam.backgroundColor = fogColor;
            var main = Camera.main;
            if (main != null) main.backgroundColor = fogColor;
        }
    }
}
