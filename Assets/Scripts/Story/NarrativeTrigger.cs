using System.Collections;
using LeftBehind.Interaction;
using LeftBehind.Loop;
using StarterAssets;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A story beat. It fires once, when the scene starts, when the player walks into its
    /// zone, or when the player uses a chosen object, provided the required flags are set.
    /// Firing sets its flag, can show a title card, hand Walter an item, play a
    /// conversation and then update the objective.
    /// </summary>
    public class NarrativeTrigger : MonoBehaviour
    {
        public enum Mode { SceneStart, EnterZone, Use }

        [SerializeField] private Mode mode = Mode.EnterZone;
        [Tooltip("SceneStart: seconds after the scene (and any loop transition) has settled.")]
        [SerializeField] private float delay = 1f;
        [Tooltip("EnterZone: size of the box around this object's position.")]
        [SerializeField] private Vector3 zoneSize = new Vector3(4f, 3f, 4f);
        [Tooltip("Use: the object whose use fires this beat.")]
        [SerializeField] private Interactable target;

        [Header("Conditions")]
        [SerializeField] private string[] requiredFlags = new string[0];
        [Tooltip("The beat never fires once any of these is set.")]
        [SerializeField] private string[] blockedByFlags = new string[0];

        [Header("Effects")]
        [Tooltip("Set when the beat fires; also stops it firing again (even after a reload of the loop).")]
        [SerializeField] private string flagToSet = "";
        [SerializeField] private string titleCard = "";
        [SerializeField] private float titleHold = 2.5f;
        [SerializeField] private CarriedItem item;
        [SerializeField] private DialogueData dialogue;
        [Tooltip("Shown after the conversation (empty keeps the current objective).")]
        [SerializeField] private string objective = "";

        private bool fired;
        private Transform player;

        public void Configure(Mode m, string flag, DialogueData d, string obj, string[] required = null,
            Interactable useTarget = null, Vector3? zone = null, float startDelay = 1f)
        {
            mode = m;
            flagToSet = flag;
            dialogue = d;
            objective = obj;
            requiredFlags = required ?? new string[0];
            target = useTarget;
            if (zone.HasValue) zoneSize = zone.Value;
            delay = startDelay;
        }

        public void BlockBy(params string[] flags) => blockedByFlags = flags;

        public void SetTitle(string text, float hold) { titleCard = text; titleHold = hold; }

        public void SetItem(string id) => item = new CarriedItem { id = id };

        private void OnEnable()
        {
            if (mode == Mode.Use && target != null) target.Used += OnUsed;
        }

        private void OnDisable()
        {
            if (target != null) target.Used -= OnUsed;
        }

        private void Start()
        {
            if (mode == Mode.SceneStart) StartCoroutine(FireWhenSettled());
        }

        private IEnumerator FireWhenSettled()
        {
            while (LoopTransition.Busy) yield return null;
            yield return new WaitForSeconds(delay);
            while (!Ready()) yield return null;
            Fire();
        }

        private void Update()
        {
            if (mode != Mode.EnterZone || fired) return;
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc == null) return;
                player = fpc.transform;
            }
            var zone = new Bounds(transform.position, zoneSize);
            if (zone.Contains(player.position + Vector3.up * 0.5f) && Ready()) Fire();
        }

        private void OnUsed(PlayerInteractor _)
        {
            if (Ready()) Fire();
        }

        private bool Ready()
        {
            if (fired || StoryStateManager.Has(flagToSet) || !StoryStateManager.HasAll(requiredFlags)) return false;
            foreach (var f in blockedByFlags) if (StoryStateManager.Has(f)) return false;
            return true;
        }

        public void Fire()
        {
            if (!Ready()) return;
            fired = true;
            StoryStateManager.Set(flagToSet);
            StoryStateManager.AddItem(item.id);
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            if (!string.IsNullOrEmpty(titleCard))
            {
                StoryUI.Instance.ShowTitle(titleCard, titleHold);
                yield return new WaitForSecondsRealtime(titleHold + 2.6f);
            }
            bool done = dialogue == null;
            DialogueManager.Play(dialogue, () => done = true);
            while (!done) yield return null;
            if (!string.IsNullOrEmpty(objective)) StoryStateManager.SetObjective(objective);
        }

        private void OnDrawGizmosSelected()
        {
            if (mode != Mode.EnterZone) return;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
            Gizmos.DrawCube(transform.position, zoneSize);
        }
    }
}
