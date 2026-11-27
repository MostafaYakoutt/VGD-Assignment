using System.Collections;
using LeftBehind.Audio;
using LeftBehind.Loop;
using StarterAssets;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A scare that happens once, when the story allows it, Walter is in the right place, and the
    /// house has been quiet for long enough (see ScarePacing). Just before it, the house holds its
    /// breath: the ambience drops away for a few seconds. Afterwards Walter may say something.
    /// The subclasses say what has to be true for it (Armed) and what happens (Play).
    /// Placed by the editor's DreadBuilder.
    /// </summary>
    public abstract class PacedScare : MonoBehaviour
    {
        [Header("When")]
        [Tooltip("Story flag that must be set first (empty: none).")]
        [SerializeField] protected string requiredFlag = "";
        [Tooltip("It never happens once this flag is set (empty: none).")]
        [SerializeField] protected string blockedByFlag = "";
        [Tooltip("Set once it has happened, so it never happens twice.")]
        [SerializeField] protected string doneFlag = "";
        [Tooltip("Walter has to be inside this box (size zero: anywhere).")]
        [SerializeField] protected Bounds playerArea;
        [Tooltip("Seconds of quiet since the last scare before this one may happen.")]
        [SerializeField] protected float quietSeconds = 45f;
        [Tooltip("Seconds the ambience drops away before it happens (0: no hush).")]
        [SerializeField] protected float hushSeconds = 3f;

        [Header("After")]
        [Tooltip("What Walter says afterwards (none: nothing).")]
        [SerializeField] protected DialogueData afterLines;

        protected Transform player;
        protected Camera viewCamera;
        private bool running;
        private bool done;

        public void ConfigurePacing(string required, string blocked, string doneAs, Bounds area,
            float quiet, float hush, DialogueData after = null)
        {
            requiredFlag = required;
            blockedByFlag = blocked;
            doneFlag = doneAs;
            playerArea = area;
            quietSeconds = quiet;
            hushSeconds = hush;
            afterLines = after;
        }

        protected virtual void Start()
        {
            if (StoryStateManager.Has(doneFlag)) done = true;
        }

        protected virtual void Update()
        {
            if (done || running) return;
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc == null) return;
                player = fpc.transform;
            }
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null || !Allowed() || !Armed()) return;
            if (!ScarePacing.Ready(quietSeconds)) return;
            StartCoroutine(Run());
        }

        private bool Allowed()
        {
            if (LoopTransition.Busy) return false;
            if (!string.IsNullOrEmpty(requiredFlag) && !StoryStateManager.Has(requiredFlag)) return false;
            if (StoryStateManager.Has(blockedByFlag)) return false;
            return InArea(playerArea);
        }

        protected bool InArea(Bounds area) => area.size == Vector3.zero || area.Contains(player.position + Vector3.up);

        private IEnumerator Run()
        {
            running = true;
            ScarePacing.Mark();
            if (hushSeconds > 0f)
            {
                GameAudio.Hush(true);
                yield return new WaitForSeconds(hushSeconds);
                if (!Allowed() || !Armed())
                {
                    // He walked away while the house held its breath: try again later.
                    GameAudio.Hush(false);
                    running = false;
                    yield break;
                }
            }
            yield return Play();
            GameAudio.Hush(false);
            done = true;
            StoryStateManager.Set(doneFlag);
            ScarePacing.Mark();
            if (afterLines != null) DialogueManager.Play(afterLines);
            running = false;
        }

        /// <summary>Whether it can happen now (beyond the flags, the area and the pacing).</summary>
        protected abstract bool Armed();

        /// <summary>The scare itself.</summary>
        protected abstract IEnumerator Play();

        protected virtual void OnDisable()
        {
            if (running) GameAudio.Hush(false);
            running = false;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (playerArea.size == Vector3.zero) return;
            Gizmos.color = new Color(0.6f, 0.3f, 0.9f, 0.25f);
            Gizmos.DrawWireCube(playerArea.center, playerArea.size);
        }
    }
}
