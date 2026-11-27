using System.Collections;
using System.Collections.Generic;
using LeftBehind.Audio;
using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A door closes somewhere in the house, out of Walter's sight. One he left open is really shut
    /// when he gets there; otherwise he only hears it. Picks the nearest door that is far enough
    /// away and that he cannot see.
    /// </summary>
    public class DistantDoor : PacedScare
    {
        [Header("Doors")]
        [SerializeField] private Door[] doors = new Door[0];
        [SerializeField] private float minDistance = 6f;
        [SerializeField] private float maxDistance = 16f;

        private Door chosen;

        public void ConfigureDoors(Door[] candidates, float min, float max)
        {
            doors = candidates;
            minDistance = min;
            maxDistance = max;
        }

        protected override bool Armed()
        {
            chosen = Pick();
            return chosen != null;
        }

        private Door Pick()
        {
            Door best = null;
            float bestScore = float.MaxValue;
            foreach (var d in doors)
            {
                if (d == null || !d.isActiveAndEnabled) continue;
                float dist = Vector3.Distance(player.position, d.transform.position);
                if (dist < minDistance || dist > maxDistance) continue;
                if (PlayerView.CanSee(viewCamera, PlayerView.RendererBounds(d.transform), d.transform, 0.15f)) continue;
                // An open door that can really shut is better than a sound alone.
                float score = dist - (d.IsOpen ? 100f : 0f);
                if (score < bestScore) { bestScore = score; best = d; }
            }
            return best;
        }

        protected override IEnumerator Play()
        {
            var d = chosen != null && !PlayerView.CanSee(viewCamera, PlayerView.RendererBounds(chosen.transform), chosen.transform, 0.15f)
                ? chosen : Pick();
            if (d == null) yield break;
            if (d.IsOpen) d.SnapShut(0f);
            GameAudio.Muffled(GameAudio.ADoorClose, d.transform.position, 0.85f, Random.Range(0.92f, 1f), 1400f);
            yield return new WaitForSeconds(2.5f);
        }

        /// <summary>Every door in the scene except the loop doors, the front door and the doors that never open.</summary>
        public static Door[] Candidates()
        {
            var list = new List<Door>();
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include))
                if (!d.Locked && d.GetComponent<LeftBehind.Loop.LoopDoor>() == null) list.Add(d);
            return list.ToArray();
        }
    }
}
