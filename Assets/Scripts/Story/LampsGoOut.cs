using System.Collections;
using LeftBehind.Interaction;
using StarterAssets;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// The lamps ahead of Walter go out one by one as he comes towards them, each with the click
    /// of its switch, leaving him with his flashlight. Happens once, when he walks into the area
    /// (after the story flag, if one is given). Lamps already off are skipped; he can switch them
    /// back on afterwards. Placed by the editor's DarknessBuilder.
    /// </summary>
    public class LampsGoOut : MonoBehaviour
    {
        [Tooltip("In the order they go out.")]
        [SerializeField] private LampSwitch[] lamps = new LampSwitch[0];
        [SerializeField] private Bounds area;
        [Tooltip("Story flag that must be set first (empty: none).")]
        [SerializeField] private string requiredFlag = "";
        [Tooltip("Set once it has happened, so it never happens twice.")]
        [SerializeField] private string doneFlag = "";
        [SerializeField] private Vector2 gap = new Vector2(0.45f, 0.8f);

        private Transform player;
        private bool done;

        public void Configure(LampSwitch[] inOrder, Bounds zone, string flag, string doneAs)
        {
            lamps = inOrder;
            area = zone;
            requiredFlag = flag;
            doneFlag = doneAs;
        }

        private void Start()
        {
            // Already happened earlier in this playthrough: the lamps stay out.
            if (!StoryStateManager.Has(doneFlag)) return;
            done = true;
            foreach (var l in lamps)
                if (l != null) l.SetOn(false);
        }

        private void Update()
        {
            if (done) return;
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc == null) return;
                player = fpc.transform;
            }
            if (!string.IsNullOrEmpty(requiredFlag) && !StoryStateManager.Has(requiredFlag)) return;
            if (!area.Contains(player.position + Vector3.up)) return;
            done = true;
            StoryStateManager.Set(doneFlag);
            StartCoroutine(GoOut());
        }

        private IEnumerator GoOut()
        {
            ScarePacing.Mark();
            yield return new WaitForSeconds(0.3f);
            foreach (var l in lamps)
            {
                if (l == null || !l.IsOn) continue;
                // Each one flickers before it dies.
                yield return l.FlickerOut(Random.Range(0.7f, 1.3f));
                yield return new WaitForSeconds(Random.Range(gap.x, gap.y));
            }
            ScarePacing.Mark();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(area.center, area.size);
            Gizmos.color = new Color(0.9f, 0.3f, 0.2f, 0.8f);
            foreach (var l in lamps)
                if (l != null) Gizmos.DrawLine(area.center, l.transform.position);
        }
    }
}
