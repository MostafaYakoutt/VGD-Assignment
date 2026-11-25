using System.Collections.Generic;
using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// A door leaf that swings open and closed with E. Put it on the hinge
    /// (an empty at the hinge edge, leaf along its local +X). The door
    /// swings away from the player. Linked doors (the other half of a
    /// double door) open and close together with this one.
    ///
    /// On Start the door measures how far it can swing to each side before
    /// its leaf touches walls or furniture, and never opens past that. If
    /// the side away from the player is too cramped it opens the other way.
    /// While swinging it waits instead of pushing into the player.
    /// </summary>
    public class Door : Interactable
    {
        [SerializeField] private float openAngle = 95f;
        [Tooltip("If the preferred side has less room than this, open the other way.")]
        [SerializeField] private float minClearAngle = 70f;
        [SerializeField] private Door[] linkedDoors;
        [SerializeField] private string openText = "Open door";
        [SerializeField] private string closeText = "Close door";
        [SerializeField] private bool locked;
        [Tooltip("Degrees the door starts open (0 = shut). It opens to whichever side has room.")]
        [SerializeField] private float startAngle = 0f;
        [SerializeField] private string lockedText = "Locked";
        [SerializeField] private AudioSource swingSound;

        private const float AngleStep = 2.5f;
        private const float Shrink = 0.03f;

        private Quaternion closedRotation;
        private float currentAngle;
        private float targetAngle;
        private BoxCollider[] leafBoxes;
        private readonly HashSet<Collider> ignored = new HashSet<Collider>();
        private readonly Collider[] hits = new Collider[16];

        /// <summary>Seconds for a full swing: slow and heavy, as in the design.</summary>
        public const float SwingSeconds = 1.2f;

        public bool IsOpen { get; private set; }

        /// <summary>Largest clear angle on the positive and negative side.</summary>
        public float ClearPositive { get; private set; }
        public float ClearNegative { get; private set; }

        public bool Locked
        {
            get => locked;
            set => locked = value;
        }

        /// <summary>Prompt shown while locked (a KeyedDoor changes it when Walter carries the key).</summary>
        public string LockedText
        {
            get => lockedText;
            set => lockedText = value;
        }

        public override string Prompt => locked ? lockedText : IsOpen ? closeText : openText;

        private void Awake()
        {
            closedRotation = transform.localRotation;
            leafBoxes = GetComponentsInChildren<BoxCollider>();
        }

        private void Start()
        {
            // Own leaf, the other half of a double door, and whatever frames the
            // closed leaf (wall piers, lintel, floor) never count as obstacles.
            foreach (var b in leafBoxes) ignored.Add(b);
            if (linkedDoors != null)
                foreach (var d in linkedDoors)
                    if (d != null) foreach (var c in d.GetComponentsInChildren<Collider>()) ignored.Add(c);
            foreach (var b in leafBoxes)
            {
                GetBox(b, -0.02f, out var c, out var h, out var r);
                int n = Physics.OverlapBoxNonAlloc(c, h, hits, r, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++) ignored.Add(hits[i]);
            }

            ClearPositive = MeasureClearAngle(1f);
            ClearNegative = MeasureClearAngle(-1f);
            if (startAngle > 0f && !locked)
            {
                bool positive = ClearPositive >= ClearNegative;
                currentAngle = targetAngle = (positive ? 1f : -1f) * Mathf.Min(startAngle, positive ? ClearPositive : ClearNegative);
                IsOpen = true;
            }
            transform.localRotation = closedRotation * Quaternion.Euler(0f, currentAngle, 0f);
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (locked)
            {
                LeftBehind.Audio.GameAudio.DoorRattled(transform.position);
                return;
            }

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
            if (open) LeftBehind.Audio.GameAudio.DoorOpened(transform.position);

            if (open)
            {
                // A positive yaw swings the leaf (+X) towards local -Z.
                float side = Vector3.Dot(fromPosition - transform.position, transform.forward) >= 0f ? 1f : -1f;
                float room = side > 0f ? ClearPositive : ClearNegative;
                float otherRoom = side > 0f ? ClearNegative : ClearPositive;
                if (room < minClearAngle && otherRoom > room)
                {
                    side = -side;
                    room = otherRoom;
                }
                targetAngle = side * room;
            }
            else
            {
                targetAngle = 0f;
            }

            if (swingSound != null) swingSound.Play();
        }

        /// <summary>
        /// Shuts the door at once, with its linked doors, without the swing: for a door that has
        /// closed behind Walter's back. <paramref name="latchVolume"/> is how loud the latch clicks (0: silent).
        /// </summary>
        public void SnapShut(float latchVolume)
        {
            IsOpen = false;
            currentAngle = targetAngle = 0f;
            transform.localRotation = closedRotation;
            if (linkedDoors != null)
                foreach (var d in linkedDoors)
                    if (d != null && d != this && d.IsOpen) d.SnapShut(0f);
            if (latchVolume > 0f) LeftBehind.Audio.GameAudio.DoorLatched(transform.position, latchVolume);
        }

        private void Update()
        {
            if (Mathf.Approximately(currentAngle, targetAngle)) return;
            float next = Mathf.MoveTowards(currentAngle, targetAngle, openAngle / SwingSeconds * Time.deltaTime);
            transform.localRotation = closedRotation * Quaternion.Euler(0f, next, 0f);
            if (TouchesPlayer())
            {
                // Wait for the player to step aside rather than swing through them.
                transform.localRotation = closedRotation * Quaternion.Euler(0f, currentAngle, 0f);
                return;
            }
            bool wasOpen = !Mathf.Approximately(currentAngle, 0f);
            currentAngle = next;
            if (wasOpen && !IsOpen && Mathf.Approximately(currentAngle, 0f))
                LeftBehind.Audio.GameAudio.DoorClosed(transform.position);
        }

        private float MeasureClearAngle(float sign)
        {
            float clear = 0f;
            for (float a = AngleStep; a <= openAngle + 0.01f; a += AngleStep)
            {
                transform.localRotation = closedRotation * Quaternion.Euler(0f, sign * a, 0f);
                if (Blocked()) break;
                clear = a;
            }
            return clear;
        }

        private bool Blocked()
        {
            foreach (var b in leafBoxes)
            {
                GetBox(b, Shrink, out var c, out var h, out var r);
                int n = Physics.OverlapBoxNonAlloc(c, h, hits, r, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                    if (!ignored.Contains(hits[i]) && !(hits[i] is CharacterController)) return true;
            }
            return false;
        }

        private bool TouchesPlayer()
        {
            foreach (var b in leafBoxes)
            {
                GetBox(b, 0f, out var c, out var h, out var r);
                int n = Physics.OverlapBoxNonAlloc(c, h, hits, r, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                    if (hits[i] is CharacterController) return true;
            }
            return false;
        }

        private static void GetBox(BoxCollider b, float shrink, out Vector3 centre, out Vector3 half, out Quaternion rot)
        {
            Transform t = b.transform;
            centre = t.TransformPoint(b.center);
            Vector3 s = t.lossyScale;
            half = new Vector3(Mathf.Abs(b.size.x * s.x), Mathf.Abs(b.size.y * s.y), Mathf.Abs(b.size.z * s.z)) * 0.5f;
            half = new Vector3(Mathf.Max(0.005f, half.x - shrink), Mathf.Max(0.005f, half.y - shrink), Mathf.Max(0.005f, half.z - shrink));
            rot = t.rotation;
        }
    }
}
