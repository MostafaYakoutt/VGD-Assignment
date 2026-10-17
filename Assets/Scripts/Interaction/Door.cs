using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// A door leaf that swings open and closed with E. Put it on the hinge
    /// (an empty at the hinge edge, leaf along its local +X). The door
    /// swings away from the player. Linked doors (the other half of a
    /// double door) open and close together with this one.
    /// </summary>
    public class Door : Interactable
    {
        [SerializeField] private float openAngle = 95f;
        [Tooltip("Swing speed in degrees per second.")]
        [SerializeField] private float swingSpeed = 160f;
        [SerializeField] private Door[] linkedDoors;
        [SerializeField] private string openText = "Open door";
        [SerializeField] private string closeText = "Close door";
        [SerializeField] private bool locked;
        [SerializeField] private string lockedText = "Locked";
        [SerializeField] private AudioSource swingSound;

        private Quaternion closedRotation;
        private float currentAngle;
        private float targetAngle;

        public bool IsOpen { get; private set; }

        public bool Locked
        {
            get => locked;
            set => locked = value;
        }

        public override string Prompt => locked ? lockedText : IsOpen ? closeText : openText;

        private void Awake()
        {
            closedRotation = transform.localRotation;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (locked) return;

            bool open = !IsOpen;
            Vector3 from = interactor.transform.position;
            SetOpen(open, from);
            if (linkedDoors == null) return;
            foreach (var door in linkedDoors)
                if (door != null && door != this) door.SetOpen(open, from);
        }

        /// <summary>Opens away from <paramref name="fromPosition"/>, or closes.</summary>
        public void SetOpen(bool open, Vector3 fromPosition)
        {
            if (open == IsOpen) return;
            IsOpen = open;

            if (open)
            {
                // A positive yaw swings the leaf (+X) towards local -Z.
                float side = Vector3.Dot(fromPosition - transform.position, transform.forward);
                targetAngle = side >= 0f ? openAngle : -openAngle;
            }
            else
            {
                targetAngle = 0f;
            }

            if (swingSound != null) swingSound.Play();
        }

        private void Update()
        {
            if (Mathf.Approximately(currentAngle, targetAngle)) return;
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, swingSpeed * Time.deltaTime);
            transform.localRotation = closedRotation * Quaternion.Euler(0f, currentAngle, 0f);
        }
    }
}
