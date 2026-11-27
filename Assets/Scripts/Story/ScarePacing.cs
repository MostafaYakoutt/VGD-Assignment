using LeftBehind.Loop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LeftBehind.Story
{
    /// <summary>
    /// Keeps the scares apart, so each one comes after a long stretch of quiet instead of on top of
    /// the last. Every scare marks the time it happened; a paced scare (see PacedScare) only goes off
    /// once enough quiet has passed, and never while Walter is talking, reading or between loops.
    /// Each loop starts quiet: the clock is reset when a scene loads.
    /// </summary>
    public static class ScarePacing
    {
        private static float lastScare;

        public static float SecondsSinceLastScare => Time.time - lastScare;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Hook()
        {
            lastScare = 0f;
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) => Mark();

        /// <summary>Something frightening has just happened: the quiet starts again from now.</summary>
        public static void Mark() => lastScare = Time.time;

        /// <summary>True when at least <paramref name="quietSeconds"/> have passed since the last scare and nothing else has the player's attention.</summary>
        public static bool Ready(float quietSeconds)
        {
            if (LoopTransition.Busy || DialogueManager.Busy || NoteReader.IsOpen) return false;
            return SecondsSinceLastScare >= quietSeconds;
        }
    }
}
