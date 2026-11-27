using LeftBehind.Audio;
using StarterAssets;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Something that is there one moment and gone the next, never while Walter is looking:
    ///   a shape under a dust sheet that is there from the start, and gone when he looks back;
    ///   a mannequin that appears behind him outside the attic, once, and is gone when he looks away.
    /// It appears (if it does not start there) when the story allows it, he is in the given area at
    /// the right distance, and the spot has been out of sight for a moment. Once he has seen it, it
    /// vanishes the next time it is out of sight. Either way it happens once in a playthrough.
    /// Placed by the editor's DreadBuilder.
    /// </summary>
    public class Apparition : MonoBehaviour
    {
        [SerializeField] private GameObject visual;
        [Tooltip("There from the start (a sheeted shape); otherwise it appears once (the mannequin).")]
        [SerializeField] private bool startShown = true;

        [Header("Appearing")]
        [SerializeField] private string requiredFlag = "";
        [Tooltip("Walter has to be inside this box (size zero: anywhere).")]
        [SerializeField] private Bounds playerArea;
        [SerializeField] private float minDistance = 3.5f;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private float quietSeconds = 10f;

        [Header("Vanishing")]
        [Tooltip("Seconds it has to be out of sight, after he has seen it, before it is gone.")]
        [SerializeField] private float vanishAfter = 0.6f;
        [Tooltip("Set once it has gone, so it never comes back.")]
        [SerializeField] private string doneFlag = "";
        [Tooltip("Walter breathes harder near it (as near the attic mannequins).")]
        [SerializeField] private bool frightens;

        private enum State { Waiting, Shown, Gone }

        private State state;
        private Transform player;
        private Camera viewCamera;
        private Renderer[] renderers;
        private Collider[] colliders;
        private Bounds spot;
        private float seenFor;
        private float unseenFor;
        private bool seen;

        public void Configure(GameObject v, bool shownAtStart, string flag, Bounds area, float min, float max,
            string doneAs, bool breathe, float quiet = 10f)
        {
            visual = v;
            startShown = shownAtStart;
            requiredFlag = flag;
            playerArea = area;
            minDistance = min;
            maxDistance = max;
            doneFlag = doneAs;
            frightens = breathe;
            quietSeconds = quiet;
        }

        private void Awake()
        {
            if (visual == null) visual = gameObject;
            renderers = visual.GetComponentsInChildren<Renderer>(true);
            colliders = visual.GetComponentsInChildren<Collider>(true);
            spot = PlayerView.RendererBounds(visual.transform, renderers);
        }

        private void Start()
        {
            if (StoryStateManager.Has(doneFlag)) { state = State.Gone; Show(false); }
            else if (startShown) { state = State.Shown; Show(true); }
            else { state = State.Waiting; Show(false); }
        }

        private void OnDisable() => Breathing.RemovePresence(transform);

        private void Show(bool on)
        {
            foreach (var r in renderers) if (r != null) r.enabled = on;
            foreach (var c in colliders) if (c != null) c.enabled = on;
            if (on && frightens) Breathing.AddPresence(transform);
            else Breathing.RemovePresence(transform);
        }

        private void Update()
        {
            if (state == State.Gone) return;
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc == null) return;
                player = fpc.transform;
            }
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            bool inView = PlayerView.CanSee(viewCamera, spot, visual.transform, 0.12f);
            if (inView) { seenFor += Time.deltaTime; unseenFor = 0f; }
            else { unseenFor += Time.deltaTime; seenFor = 0f; }

            if (state == State.Waiting)
            {
                if (!string.IsNullOrEmpty(requiredFlag) && !StoryStateManager.Has(requiredFlag)) return;
                if (playerArea.size != Vector3.zero && !playerArea.Contains(player.position + Vector3.up)) return;
                float d = Vector3.Distance(player.position, spot.center);
                if (d < minDistance || d > maxDistance || unseenFor < 1f) return;
                if (!ScarePacing.Ready(quietSeconds)) return;
                state = State.Shown;
                Show(true);
                return;
            }

            // Shown: once he has had a proper look, it goes the next time he looks away.
            if (seenFor > 0.25f && !seen)
            {
                seen = true;
                ScarePacing.Mark();
            }
            if (seen && unseenFor >= vanishAfter)
            {
                state = State.Gone;
                Show(false);
                StoryStateManager.Set(doneFlag);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerArea.size == Vector3.zero) return;
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.25f);
            Gizmos.DrawWireCube(playerArea.center, playerArea.size);
        }
    }
}
