using UnityEngine;

namespace LeftBehind.Audio
{
    /// <summary>
    /// Footsteps for the first-person player: one step for every stride walked on the ground,
    /// picked at random from the step recordings with a little variation in pitch and volume.
    /// Added to the player by GameAudio.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Footsteps : MonoBehaviour
    {
        /// <summary>Metres walked per step; the head bob uses the same stride.</summary>
        public const float Stride = 0.75f;

        [SerializeField] private float volume = 0.3f;

        private AudioClip[] clips;
        private CharacterController controller;
        private AudioSource source;
        private Vector3 last;
        private float travelled;
        private int lastClip = -1;

        public void Configure(AudioClip[] steps) => clips = steps;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            last = transform.position;
        }

        private void Update()
        {
            var pos = transform.position;
            var delta = pos - last;
            last = pos;
            delta.y = 0f;
            float d = delta.magnitude;
            // Ignore teleports and frames where the player is frozen or in the air.
            if (d > 0.5f || !controller.enabled || !controller.isGrounded || clips == null || clips.Length == 0) return;
            travelled += d;
            if (travelled < Stride) return;
            travelled = 0f;
            int c = Random.Range(0, clips.Length);
            if (c == lastClip) c = (c + 1) % clips.Length;
            lastClip = c;
            source.pitch = Random.Range(0.9f, 1.05f);
            source.PlayOneShot(clips[c], volume * Random.Range(0.85f, 1f));
        }
    }
}
