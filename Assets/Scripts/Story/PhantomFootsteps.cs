using System.Collections;
using LeftBehind.Audio;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Someone walking about on the floor above Walter: footsteps, muffled by the ceiling, crossing
    /// from one end of the path to the other, a pause, then one more step. They are not his.
    /// Only while Walter is at least a storey below the path.
    /// </summary>
    public class PhantomFootsteps : PacedScare
    {
        [Header("Footsteps")]
        [Tooltip("Where each step falls, in order.")]
        [SerializeField] private Vector3[] steps = new Vector3[0];
        [SerializeField] private float stepSeconds = 0.6f;
        [Tooltip("Below 1: a heavy tread. Above 1: small feet.")]
        [SerializeField] private float pitch = 0.85f;
        [SerializeField] private float volume = 0.9f;
        [Tooltip("Low-pass cut-off in Hz: the floor between swallows the top end.")]
        [SerializeField] private float muffle = 650f;

        public void ConfigureSteps(Vector3[] path, float seconds, float stepPitch, float vol, float cutoff)
        {
            steps = path;
            stepSeconds = seconds;
            pitch = stepPitch;
            volume = vol;
            muffle = cutoff;
        }

        protected override bool Armed()
        {
            if (steps.Length == 0) return false;
            // He has to be underneath: a storey down, and not far off to the side.
            var mid = steps[steps.Length / 2];
            if (player.position.y > mid.y - 2f) return false;
            var flat = new Vector2(player.position.x - mid.x, player.position.z - mid.z);
            return flat.magnitude < 14f;
        }

        protected override IEnumerator Play()
        {
            for (int i = 0; i < steps.Length; i++)
            {
                // The last step comes after a pause, as if whoever it is stopped to listen.
                if (i == steps.Length - 1) yield return new WaitForSeconds(1.6f);
                GameAudio.Muffled(GameAudio.AFootstep, steps[i], volume * Random.Range(0.85f, 1f),
                    pitch * Random.Range(0.96f, 1.04f), muffle);
                yield return new WaitForSeconds(stepSeconds * Random.Range(0.9f, 1.1f));
            }
            yield return new WaitForSeconds(1.5f);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.9f, 0.9f, 0.3f, 0.8f);
            for (int i = 0; i < steps.Length; i++)
            {
                Gizmos.DrawWireSphere(steps[i], 0.12f);
                if (i > 0) Gizmos.DrawLine(steps[i - 1], steps[i]);
            }
        }
    }
}
