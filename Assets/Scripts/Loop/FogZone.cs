using StarterAssets;
using UnityEngine;

namespace LeftBehind.Loop
{
    /// <summary>
    /// A part of the house where the air is thicker (the attic): while Walter is inside the box the
    /// fog thickens to a multiple of the loop's own, and it thins again when he leaves.
    /// Placed by the editor's DreadBuilder.
    /// </summary>
    public class FogZone : MonoBehaviour
    {
        [SerializeField] private Bounds area;
        [Tooltip("Fog density inside, as a multiple of the loop's (LoopEnvironment).")]
        [SerializeField] private float multiplier = 2.2f;
        [SerializeField] private float blendSeconds = 2f;

        private Transform player;
        private LoopEnvironment env;
        private float blend;

        public void Configure(Bounds box, float times)
        {
            area = box;
            multiplier = times;
        }

        private void Update()
        {
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc == null) return;
                player = fpc.transform;
            }
            if (env == null) env = FindAnyObjectByType<LoopEnvironment>();
            if (env == null) return;
            float target = area.Contains(player.position + Vector3.up) ? 1f : 0f;
            if (Mathf.Approximately(blend, target)) return;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime / blendSeconds);
            RenderSettings.fogDensity = env.FogDensity * Mathf.Lerp(1f, multiplier, Mathf.SmoothStep(0f, 1f, blend));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.7f, 0.7f, 0.8f, 0.25f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
