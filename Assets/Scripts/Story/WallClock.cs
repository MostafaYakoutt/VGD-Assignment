using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// The dining room clock. It shows the time of the loop: twenty to four on the winter
    /// afternoon, ten to seven on the autumn evening, and in Loop 3 it has stopped at eight
    /// minutes to midnight. While it runs the hands move once a second, in step with the tick
    /// GameAudio plays from the clock.
    /// </summary>
    public class WallClock : MonoBehaviour
    {
        [SerializeField] private Transform hourHand;
        [SerializeField] private Transform minuteHand;
        [SerializeField] private Transform secondHand;
        [Tooltip("Time shown when the scene starts, in seconds after midnight.")]
        [SerializeField] private float startSeconds = 15 * 3600 + 40 * 60;
        [SerializeField] private bool running = true;

        private float elapsed;

        public bool Running => running;

        public void Configure(Transform hour, Transform minute, Transform second, float seconds, bool isRunning)
        {
            hourHand = hour;
            minuteHand = minute;
            secondHand = second;
            startSeconds = seconds;
            running = isRunning;
            Apply(0f);
        }

        private void Start() => Apply(0f);

        private void Update()
        {
            if (!running) return;
            elapsed += Time.deltaTime;
            Apply(Mathf.Floor(elapsed));
        }

        private void Apply(float secondsPassed)
        {
            float t = startSeconds + secondsPassed;
            // The face looks along the clock's +Z, towards the viewer, so clockwise as seen is a positive turn about Z.
            Set(hourHand, t / 3600f % 12f * 30f);
            Set(minuteHand, t / 60f % 60f * 6f);
            Set(secondHand, Mathf.Floor(t) % 60f * 6f);
        }

        private static void Set(Transform hand, float degrees)
        {
            if (hand != null) hand.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
