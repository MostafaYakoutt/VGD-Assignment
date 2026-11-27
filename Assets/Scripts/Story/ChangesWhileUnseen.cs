using LeftBehind.Interaction;
using StarterAssets;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// One authored change that happens behind Walter's back, once: a chair pulled out, a photo
    /// turned to the wall, a bear a step nearer the stairs, a door he left open now shut.
    /// It waits until
    ///   the story flag is set (if one is given),
    ///   Walter has seen the thing as it was (so he knows it has changed),
    ///   he is within reach of it but not right beside it, and inside the given area (if any),
    ///   and neither where it is nor where it will be has been in view for a moment,
    /// then moves the target to its new pose (or shuts the door) and sets its done flag.
    /// Placed by the editor's BehindYourBackBuilder.
    /// </summary>
    public class ChangesWhileUnseen : MonoBehaviour
    {
        /// <summary>Metres within which a changed thing's outline shows again.</summary>
        private const float QuietGlowRange = 1.2f;

        /// <summary>Metres within which the flashlight stutters near a changed thing.</summary>
        private const float FlickerRange = 3.5f;

        [Tooltip("What changes. Leave empty for a door: the door is the target.")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 movedPosition;
        [SerializeField] private Quaternion movedRotation = Quaternion.identity;
        [Tooltip("A door that shuts instead of a move. It only shuts if it is open.")]
        [SerializeField] private Door door;
        [Tooltip("Volume of the latch when the door shuts (0: silent).")]
        [SerializeField] private float latchVolume = 0.12f;

        [Header("When")]
        [Tooltip("Story flag that must be set first (empty: none).")]
        [SerializeField] private string requiredFlag = "";
        [Tooltip("Walter has to have seen it as it was, after the flag was set.")]
        [SerializeField] private bool mustSeeFirst = true;
        [Tooltip("Walter has to be inside this box (size zero: anywhere).")]
        [SerializeField] private Bounds playerArea;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 12f;
        [Tooltip("Seconds it has to be out of sight before it changes.")]
        [SerializeField] private float unseenSeconds = 0.6f;
        [Tooltip("Set once it has changed, so it never changes twice.")]
        [SerializeField] private string doneFlag = "";

        private Transform player;
        private Camera viewCamera;
        private Renderer[] renderers;
        private bool seenAsItWas;
        private float unseenFor;
        private bool done;

        public void ConfigureMove(Transform what, Vector3 position, Quaternion rotation)
        {
            target = what;
            movedPosition = position;
            movedRotation = rotation;
            door = null;
        }

        public void ConfigureDoor(Door d, float latch)
        {
            door = d;
            target = d.transform;
            latchVolume = latch;
        }

        public void ConfigureWhen(string flag, string done, float min, float max, Bounds area = default, bool seeFirst = true)
        {
            requiredFlag = flag;
            doneFlag = done;
            minDistance = min;
            maxDistance = max;
            playerArea = area;
            mustSeeFirst = seeFirst;
        }

        private void Start()
        {
            if (target != null) renderers = target.GetComponentsInChildren<Renderer>();
            // Already changed earlier in this playthrough: start as it was left.
            if (StoryStateManager.Has(doneFlag)) Change(silent: true);
        }

        private void Update()
        {
            if (done || target == null) return;
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc != null) player = fpc.transform;
            }
            if (viewCamera == null) viewCamera = Camera.main;
            if (player == null || viewCamera == null) return;

            if (!string.IsNullOrEmpty(requiredFlag) && !StoryStateManager.Has(requiredFlag)) return;
            if (door != null && !door.IsOpen)
            {
                // Only a door he has left open can shut behind him.
                seenAsItWas = false;
                unseenFor = 0f;
                return;
            }

            var now = PlayerView.RendererBounds(target, renderers);
            bool seen = PlayerView.CanSee(viewCamera, now, target);
            if (!seen && door == null)
            {
                var then = now;
                then.center += movedPosition - target.position;
                seen = PlayerView.CanSee(viewCamera, then, target);
            }
            if (seen)
            {
                seenAsItWas = true;
                unseenFor = 0f;
                return;
            }

            unseenFor += Time.deltaTime;
            if (mustSeeFirst && !seenAsItWas) return;
            if (unseenFor < unseenSeconds) return;
            float d = Vector3.Distance(player.position, now.center);
            if (d < minDistance || d > maxDistance) return;
            if (playerArea.size != Vector3.zero && !playerArea.Contains(player.position + Vector3.up)) return;

            Change(silent: false);
        }

        private void Change(bool silent)
        {
            done = true;
            if (door != null) door.SnapShut(silent ? 0f : latchVolume);
            else if (target != null) target.SetPositionAndRotation(movedPosition, movedRotation);
            // Its outline would point straight at what changed: only show it once he is right by it.
            var glow = target != null ? target.GetComponent<InteractableGlow>() : null;
            if (glow != null) glow.Quiet(QuietGlowRange);
            // Instead, the flashlight stutters near it.
            if (target != null && target.GetComponent<FlickerSource>() == null)
                target.gameObject.AddComponent<FlickerSource>().Configure(FlickerRange);
            // And if he examines it now, the view tilts: something is wrong with it.
            if (door == null && target != null && target.GetComponent<Interactable>() != null && target.GetComponent<FeelsWrong>() == null)
                target.gameObject.AddComponent<FeelsWrong>();
            StoryStateManager.Set(doneFlag);
        }

        private void OnDrawGizmosSelected()
        {
            if (door == null && target != null)
            {
                Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.8f);
                Gizmos.DrawLine(target.position, movedPosition);
                Gizmos.DrawWireSphere(movedPosition, 0.1f);
            }
            if (playerArea.size != Vector3.zero)
            {
                Gizmos.color = new Color(0.3f, 0.6f, 0.9f, 0.25f);
                Gizmos.DrawWireCube(playerArea.center, playerArea.size);
            }
        }
    }
}
