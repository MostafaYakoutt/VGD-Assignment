using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

namespace LeftBehind.Story
{
    /// <summary>
    /// One of Ruth's mannequins in the Loop 1 attic. While Walter is in the attic it walks
    /// towards him whenever he cannot see it, and freezes mid-step the moment it is in view.
    /// "In view" means a point of its body is inside the camera's view (a little wider than the
    /// screen, so it never moves at the very edge) with nothing solid in between. It never comes
    /// closer than an arm's length: there is no way to be hurt in this game. Once Sam's bear has
    /// been taken (hurryFlag) they walk faster, as if they want it back.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Mannequin : MonoBehaviour
    {
        private static readonly int WalkingId = Animator.StringToHash("Walking");

        [SerializeField] private Animator animator;
        [SerializeField] private Renderer body;
        [Tooltip("The mannequins only move while the player is inside this box (the attic).")]
        [SerializeField] private Bounds activeArea = new Bounds(new Vector3(-0.5f, 6.4f, 0f), new Vector3(18f, 3f, 19f));
        [SerializeField] private float stopDistance = 1.5f;
        [Tooltip("Fraction of the screen beyond each edge that still counts as being seen.")]
        [SerializeField] private float viewMargin = 0.08f;
        [Tooltip("Played the first time a mannequin is seen after it has moved.")]
        [SerializeField] private NarrativeTrigger movedBeat;
        [Tooltip("Once this story flag is set they walk faster (the bear has been taken).")]
        [SerializeField] private string hurryFlag = "hasTeddy";
        [SerializeField] private float hurryFactor = 1.6f;

        private NavMeshAgent agent;
        private Transform player;
        private Camera viewCamera;
        private Vector3 lastSeenAt;
        private float nextRepath;
        private float baseSpeed;

        public bool IsSeen { get; private set; }

        public void Configure(Animator anim, Renderer bodyRenderer, Bounds area, NarrativeTrigger beat)
        {
            animator = anim;
            body = bodyRenderer;
            activeArea = area;
            movedBeat = beat;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            // Walter's flashlight stutters near them.
            if (GetComponent<FlickerSource>() == null) gameObject.AddComponent<FlickerSource>().Configure(3.5f);
            agent.stoppingDistance = stopDistance;
            agent.autoBraking = false;
            baseSpeed = agent.speed;
            lastSeenAt = transform.position;
        }

        private void Start()
        {
            // Each one starts frozen somewhere different in its idle, so they do not stand in step.
            if (animator != null) animator.Play("Idle", 0, Random.value);
        }

        private void Update()
        {
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc != null) player = fpc.transform;
            }
            if (viewCamera == null) viewCamera = Camera.main;
            if (player == null || viewCamera == null || !agent.isOnNavMesh) return;

            bool wasSeen = IsSeen;
            IsSeen = Seen();
            bool playerInAttic = activeArea.Contains(player.position + Vector3.up);

            if (IsSeen || !playerInAttic)
            {
                Freeze();
                if (IsSeen && !wasSeen)
                {
                    if (movedBeat != null && Vector3.Distance(transform.position, lastSeenAt) > 1.5f) movedBeat.Fire();
                    lastSeenAt = transform.position;
                }
                return;
            }

            // Unseen: walk towards him (faster once the bear is gone, the walk sped up to match).
            float pace = StoryStateManager.Has(hurryFlag) ? hurryFactor : 1f;
            agent.speed = baseSpeed * pace;
            if (animator != null) animator.speed = pace;
            agent.isStopped = false;
            if (Time.time >= nextRepath)
            {
                agent.SetDestination(player.position);
                nextRepath = Time.time + 0.25f;
            }
            float toPlayer = Vector3.Distance(Flat(transform.position), Flat(player.position));
            bool walking = toPlayer > stopDistance + 0.05f && agent.velocity.sqrMagnitude > 0.01f;
            if (animator != null) animator.SetBool(WalkingId, walking);
            if (!walking)
            {
                // Arrived: turn to face him, so that is how he finds it.
                var dir = Flat(player.position - transform.position);
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 240f * Time.deltaTime);
            }
        }

        private void Freeze()
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (animator != null) animator.speed = 0f;
        }

        /// <summary>True when any of a few points on the body is in view and not hidden behind anything.</summary>
        private bool Seen()
        {
            var b = body != null ? body.bounds : new Bounds(transform.position + Vector3.up * 0.9f, new Vector3(0.5f, 1.8f, 0.5f));
            return PlayerView.CanSee(viewCamera, b, transform, viewMargin, IsMannequin);
        }

        /// <summary>Another mannequin does not hide this one.</summary>
        private static bool IsMannequin(Collider c) => c.GetComponentInParent<Mannequin>() != null;

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.3f, 0.3f, 0.25f);
            Gizmos.DrawWireCube(activeArea.center, activeArea.size);
        }
    }
}
