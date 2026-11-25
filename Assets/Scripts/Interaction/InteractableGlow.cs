using LeftBehind.Loop;
using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// The faint outline that marks an object the player can use, the same kind of line as the
    /// flashlight's but dimmer. The outline pieces are baked into the scene by the editor's
    /// InteractableGlowBuilder (Left Behind > Story > Add Interactable Glow); this only shows them
    /// while the object can be used, so a taken item, a restored fuse box or a photo held up close
    /// stops glowing.
    ///
    /// The outlines tell the player "this is a game, you are safe", so they fade from the house as
    /// the chapters go on: in Chapter I they show at any distance, in Chapter II only within a few
    /// metres, in Chapter III only within arm's reach, and never on doors or lamps. Something that
    /// has just changed behind Walter's back is quieter still (see Quiet), so its outline does not
    /// give it away.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableGlow : MonoBehaviour
    {
        /// <summary>Metres from the camera within which outlines show, by loop (index 1..3); 0 means any distance.</summary>
        private static readonly float[] RangeByLoop = { 0f, 0f, 3f, 1.5f };

        /// <summary>From this loop on, doors and lamps have no outline at all.</summary>
        private const int NoDoorsOrLampsFrom = 3;

        /// <summary>An outline at the edge of its range does not flicker: it hides only this much further out.</summary>
        private const float Hysteresis = 0.25f;

        [SerializeField] private Interactable target;
        [SerializeField] private GameObject[] outlines = new GameObject[0];

        private bool shown = true;
        private bool never;
        private float range;
        private Collider[] colliders;
        private Camera viewCamera;

        public void Configure(Interactable interactable, GameObject[] pieces)
        {
            target = interactable;
            outlines = pieces;
        }

        /// <summary>
        /// Shows the outline only within <paramref name="metres"/> from now on (or the chapter's range,
        /// if that is nearer): for something that has just moved while Walter was not looking.
        /// </summary>
        public void Quiet(float metres)
        {
            range = range <= 0f ? metres : Mathf.Min(range, metres);
        }

        private void Start()
        {
            var env = FindAnyObjectByType<LoopEnvironment>();
            int loop = env != null ? Mathf.Clamp(env.Loop, 0, RangeByLoop.Length - 1) : 1;
            if (range <= 0f) range = RangeByLoop[loop];
            else if (RangeByLoop[loop] > 0f) range = Mathf.Min(range, RangeByLoop[loop]);
            never = loop >= NoDoorsOrLampsFrom && (target is Door || target is LeftBehind.Story.DoorRattle || target is LampSwitch);
            if (target != null) colliders = target.GetComponentsInChildren<Collider>();
        }

        private void LateUpdate()
        {
            bool show = !never && target != null && target.ShowsGlow && InRange();
            if (show == shown) return;
            shown = show;
            foreach (var o in outlines)
                if (o != null) o.SetActive(show);
        }

        private bool InRange()
        {
            if (range <= 0f) return true;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return true;
            var eye = viewCamera.transform.position;
            float d = Distance(eye);
            return d <= (shown ? range + Hysteresis : range);
        }

        /// <summary>Distance from <paramref name="eye"/> to the nearest edge of the object, not its pivot (a door's is at the hinge).</summary>
        private float Distance(Vector3 eye)
        {
            if (colliders == null || colliders.Length == 0) return Vector3.Distance(eye, transform.position);
            float best = float.MaxValue;
            foreach (var c in colliders)
            {
                if (c == null || !c.enabled) continue;
                best = Mathf.Min(best, Vector3.Distance(eye, c.bounds.ClosestPoint(eye)));
            }
            return best == float.MaxValue ? Vector3.Distance(eye, transform.position) : best;
        }
    }
}
