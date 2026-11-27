using System.Collections.Generic;
using UnityEngine;

namespace LeftBehind.Audio
{
    /// <summary>
    /// Walter's breathing. Silent most of the time; it rises as he comes near something that
    /// frightens him (the mannequins), getting louder and quicker the closer he is. Only things on
    /// his own floor count. Added to the player by GameAudio.
    /// </summary>
    public class Breathing : MonoBehaviour
    {
        private const float Radius = 7f;
        private const float MaxVolume = 0.55f;

        private static readonly List<Transform> presences = new List<Transform>();

        private AudioSource source;
        private float level;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => presences.Clear();

        public static void AddPresence(Transform t)
        {
            if (t != null && !presences.Contains(t)) presences.Add(t);
        }

        public static void RemovePresence(Transform t) => presences.Remove(t);

        public void Configure(AudioClip clip)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.playOnAwake = false;
            if (clip != null) source.Play();
        }

        /// <summary>0 when nothing frightening is near, 1 right beside it.</summary>
        public static float Fear(Vector3 at)
        {
            float best = 0f;
            for (int i = presences.Count - 1; i >= 0; i--)
            {
                var t = presences[i];
                if (t == null) { presences.RemoveAt(i); continue; }
                if (Mathf.Abs(t.position.y - at.y) > 2f) continue;     // another floor
                float k = 1f - Vector3.Distance(at, t.position) / Radius;
                if (k > best) best = k;
            }
            return best;
        }

        private void Update()
        {
            if (source == null) return;
            float target = Fear(transform.position);
            level = Mathf.MoveTowards(level, target, Time.deltaTime * (target > level ? 0.6f : 0.25f));
            source.volume = MaxVolume * level * level;
            source.pitch = 0.92f + 0.3f * level;
        }
    }
}
