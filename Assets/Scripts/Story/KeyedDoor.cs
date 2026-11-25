using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A locked door that opens with an item Walter carries (the study and its key).
    /// Used without the key it plays a short line; with the key it unlocks, swings open,
    /// sets its flag and plays what he finds.
    /// </summary>
    [RequireComponent(typeof(Door))]
    public class KeyedDoor : MonoBehaviour
    {
        [SerializeField] private string keyItemId = "studyKey";
        [Tooltip("Used instead of an item when the key id is empty: the door opens once this flag is set.")]
        [SerializeField] private string requiredFlag = "";
        [SerializeField] private string flagToSet = "";
        [SerializeField] private DialogueData noKey;
        [SerializeField] private DialogueData opened;
        [SerializeField] private string unlockText = "Unlock the door";
        [SerializeField] private string lockedText = "Locked";

        private Door door;

        private bool HasKey => string.IsNullOrEmpty(keyItemId)
            ? StoryStateManager.Has(requiredFlag)
            : StoryStateManager.HasItem(keyItemId);

        public void ConfigureFlag(string flagNeeded) => requiredFlag = flagNeeded;

        public void Configure(string keyId, string flag, DialogueData missing, DialogueData onOpen, string unlock, string locked)
        {
            keyItemId = keyId;
            flagToSet = flag;
            noKey = missing;
            opened = onOpen;
            unlockText = unlock;
            lockedText = locked;
        }

        private void Awake() => door = GetComponent<Door>();

        private void OnEnable() => GetComponent<Door>().Used += OnUsed;
        private void OnDisable() => GetComponent<Door>().Used -= OnUsed;

        private void Start()
        {
            if (StoryStateManager.Has(flagToSet)) door.Locked = false;
        }

        private void Update()
        {
            if (door.Locked) door.LockedText = HasKey ? unlockText : lockedText;
        }

        private void OnUsed(PlayerInteractor interactor)
        {
            if (!door.Locked) return;
            if (!HasKey)
            {
                if (!DialogueManager.Busy) DialogueManager.Play(noKey);
                return;
            }
            door.Locked = false;
            door.SetOpen(true, interactor.transform.position);
            StoryStateManager.Set(flagToSet);
            DialogueManager.Play(opened);
        }
    }
}
