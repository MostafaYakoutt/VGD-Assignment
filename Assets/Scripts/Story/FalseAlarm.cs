using System.Collections;
using LeftBehind.Audio;
using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A scare that turns out to be nothing, one in each loop, so the player never knows which kind
    /// the next one will be. The house goes quiet, then either
    ///   a sound comes from somewhere (the front door rattling, a branch at the window), or
    ///   Walter's light finds a figure standing in the dark (Ruth's dress form),
    /// he catches his breath, and he talks himself down.
    /// </summary>
    public class FalseAlarm : PacedScare
    {
        public enum Sound { None, DoorRattle, WindowTap }

        [Header("A sound")]
        [SerializeField] private Sound sound = Sound.None;
        [Tooltip("Where the sound may come from; the nearest one far enough from Walter is used.")]
        [SerializeField] private Vector3[] soundAt = new Vector3[0];
        [SerializeField] private float minSoundDistance = 3f;
        [SerializeField] private int bursts = 2;

        [Header("Or a figure")]
        [Tooltip("Something Walter mistakes for a person once it is in his light.")]
        [SerializeField] private Transform figure;
        [SerializeField] private float seeDistance = 7f;

        private Vector3 from;
        private Renderer[] figureRenderers;

        public void ConfigureSound(Sound s, Vector3[] positions, float minDistance, int count)
        {
            sound = s;
            soundAt = positions;
            minSoundDistance = minDistance;
            bursts = count;
            figure = null;
        }

        public void ConfigureFigure(Transform f, float distance)
        {
            figure = f;
            seeDistance = distance;
            sound = Sound.None;
        }

        protected override void Start()
        {
            base.Start();
            if (figure != null) figureRenderers = figure.GetComponentsInChildren<Renderer>();
        }

        protected override bool Armed()
        {
            if (figure != null) return true;            // he is in the right place; Play waits for him to see it
            return PickSource(out from);
        }

        private bool PickSource(out Vector3 best)
        {
            best = Vector3.zero;
            float bestDist = float.MaxValue;
            foreach (var p in soundAt)
            {
                if (Mathf.Abs(p.y - (player.position.y + 1.5f)) > 2f) continue;    // on his own floor
                float d = Vector3.Distance(player.position, p);
                if (d < minSoundDistance || d > bestDist) continue;
                bestDist = d;
                best = p;
            }
            return bestDist < float.MaxValue;
        }

        protected override IEnumerator Play()
        {
            if (figure != null)
            {
                // Wait (a while) for his light to land on it.
                float giveUp = Time.time + 40f;
                while (!InLight())
                {
                    if (Time.time > giveUp || !InArea(playerArea)) yield break;
                    yield return null;
                }
                GameAudio.Gasp();
                yield return new WaitForSeconds(1.4f);
                yield break;
            }

            for (int b = 0; b < bursts; b++)
            {
                if (sound == Sound.DoorRattle)
                {
                    int rattles = Random.Range(2, 4);
                    for (int i = 0; i < rattles; i++)
                    {
                        GameAudio.DoorRattled(from);
                        yield return new WaitForSeconds(Random.Range(0.25f, 0.4f));
                    }
                }
                else if (sound == Sound.WindowTap)
                {
                    GameAudio.Tap(from);
                    yield return new WaitForSeconds(1.2f);
                }
                yield return new WaitForSeconds(Random.Range(1.4f, 2.2f));
            }
            GameAudio.Gasp();
            yield return new WaitForSeconds(1.2f);
        }

        /// <summary>The figure is near, in view, near the middle of the screen, and lit by the flashlight (or a lamp).</summary>
        private bool InLight()
        {
            var b = PlayerView.RendererBounds(figure, figureRenderers);
            if (Vector3.Distance(player.position, b.center) > seeDistance) return false;
            var vp = viewCamera.WorldToViewportPoint(b.center);
            if (vp.z <= 0f || Mathf.Abs(vp.x - 0.5f) > 0.25f || Mathf.Abs(vp.y - 0.5f) > 0.3f) return false;
            if (!PlayerView.CanSee(viewCamera, b, figure, 0f)) return false;
            var torch = player.GetComponent<PlayerFlashlight>();
            return torch == null || torch.IsOn;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.4f, 0.9f, 0.6f, 0.8f);
            foreach (var p in soundAt) Gizmos.DrawWireSphere(p, 0.25f);
        }
    }
}
