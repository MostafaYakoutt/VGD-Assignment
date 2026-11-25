using System.Collections.Generic;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Something wrong that makes Walter's flashlight stutter as he comes near it: the attic
    /// mannequins, and anything that has changed behind his back. The closer he is, the more
    /// often the beam dips (see PlayerFlashlight). Nothing else marks these things, so the player
    /// learns to dread the flicker rather than look for an outline.
    /// </summary>
    public class FlickerSource : MonoBehaviour
    {
        private static readonly List<FlickerSource> active = new List<FlickerSource>();

        [Tooltip("Metres within which the flashlight starts to flicker.")]
        [SerializeField] private float radius = 4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active.Clear();

        public void Configure(float metres) => radius = metres;

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        /// <summary>How strongly the flashlight at <paramref name="position"/> should flicker: 0 out of range of all, 1 right on top of one.</summary>
        public static float Strength(Vector3 position)
        {
            float best = 0f;
            foreach (var s in active)
            {
                if (s == null || s.radius <= 0f) continue;
                float k = 1f - Vector3.Distance(position, s.transform.position) / s.radius;
                if (k > best) best = k;
            }
            return best;
        }
    }
}
