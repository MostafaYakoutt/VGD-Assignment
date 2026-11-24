using LeftBehind.Audio;
using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// The kitchen tap. In the present it drips; turned off with E it holds for a while and then
    /// starts dripping again, because it never closes all the way. On the night of Loop 3 it is
    /// running when Walter arrives. E turns it off, or on again when it is off.
    /// </summary>
    public class KitchenTap : Interactable
    {
        public enum State { Off, Dripping, Running }

        [SerializeField] private State state = State.Dripping;
        [Tooltip("The water leaves the tap here.")]
        [SerializeField] private Transform spout;
        [Tooltip("Height of the bottom of the basin, where the water lands.")]
        [SerializeField] private float basinY = 0.75f;
        [SerializeField] private Transform stream;
        [SerializeField] private Transform drop;
        [SerializeField] private float dripInterval = 1.7f;
        [Tooltip("After being turned off, the tap starts dripping again this many seconds later (0: never).")]
        [SerializeField] private float dripsAgainAfter = 40f;

        private AudioSource runningSound;
        private float streamWidth;
        private float nextDrip;
        private float dropStart = -1f;
        private float dripResumes = -1f;

        public State Current => state;

        public override string Prompt => state switch
        {
            State.Running => "Turn off the tap",
            State.Dripping => "Turn the tap off tight",
            _ => "Turn on the tap",
        };

        public void Configure(State start, Transform spoutPoint, float basin, Transform streamObj, Transform dropObj, float resumeAfter)
        {
            state = start;
            spout = spoutPoint;
            basinY = basin;
            stream = streamObj;
            drop = dropObj;
            dripsAgainAfter = resumeAfter;
            promptText = "Use the tap";
        }

        private void Start()
        {
            if (stream != null) streamWidth = stream.localScale.x;
            runningSound = gameObject.AddComponent<AudioSource>();
            runningSound.clip = GameAudio.WaterRunning;
            runningSound.loop = true;
            runningSound.volume = 0.45f;
            runningSound.spatialBlend = 1f;
            runningSound.rolloffMode = AudioRolloffMode.Logarithmic;
            runningSound.minDistance = 1f;
            runningSound.maxDistance = 14f;
            runningSound.dopplerLevel = 0f;
            runningSound.playOnAwake = false;
            Apply();
            nextDrip = Time.time + Random.Range(0.2f, dripInterval);
        }

        public override void Interact(PlayerInteractor interactor)
        {
            GameAudio.Switch(spout != null ? spout.position : transform.position);
            if (state == State.Off)
            {
                state = State.Running;
                dripResumes = -1f;
            }
            else
            {
                state = State.Off;
                if (dripsAgainAfter > 0f) dripResumes = Time.time + dripsAgainAfter;
            }
            Apply();
        }

        private void Apply()
        {
            if (stream != null) stream.gameObject.SetActive(state == State.Running);
            if (drop != null && state != State.Dripping) drop.gameObject.SetActive(false);
            dropStart = -1f;
            if (runningSound == null) return;
            if (state == State.Running && !runningSound.isPlaying && runningSound.clip != null)
            {
                runningSound.time = Random.Range(0f, runningSound.clip.length * 0.8f);
                runningSound.Play();
            }
            else if (state != State.Running) runningSound.Stop();
        }

        private void Update()
        {
            if (state == State.Off && dripResumes > 0f && Time.time >= dripResumes)
            {
                state = State.Dripping;
                dripResumes = -1f;
                nextDrip = Time.time + dripInterval;
                Apply();
            }

            if (state == State.Running && stream != null)
            {
                // A little shimmer so the stream does not look like a rod.
                var s = stream.localScale;
                float w = streamWidth * Random.Range(0.8f, 1.15f);
                stream.localScale = new Vector3(w, s.y, w);
            }

            if (state != State.Dripping || spout == null) return;

            if (dropStart < 0f && Time.time >= nextDrip)
            {
                dropStart = Time.time;
                if (drop != null) drop.gameObject.SetActive(true);
            }
            if (dropStart < 0f) return;

            // Hang at the spout for a moment, then fall under gravity.
            float hang = 0.35f, t = Time.time - dropStart - hang;
            float y = spout.position.y - (t > 0f ? 0.5f * 9.81f * t * t : 0f);
            if (drop != null) drop.position = new Vector3(spout.position.x, Mathf.Max(y, basinY), spout.position.z);
            if (y <= basinY)
            {
                GameAudio.Drip(new Vector3(spout.position.x, basinY, spout.position.z));
                if (drop != null) drop.gameObject.SetActive(false);
                dropStart = -1f;
                nextDrip = Time.time + dripInterval * Random.Range(0.85f, 1.15f);
            }
        }
    }
}
