using System.Collections;
using LeftBehind.CameraSystem;
using LeftBehind.Interaction;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LeftBehind.Loop
{
    /// <summary>
    /// Plays the loop transition and moves the player one loop back in time:
    /// the camera pulls out to the fixed loop shot, the frame fades to black,
    /// the next loop's scene loads, and the new house fades in from black.
    /// The previous scene is unloaded, so the trip is one-way. Walter arrives empty-handed: the
    /// flashlight stays behind in the loop he found it in.
    /// </summary>
    public class LoopTransition : MonoBehaviour
    {
        private const float ShotHold = 2.1f;      // 1.5 s blend into the loop shot plus a beat
        private const float FadeOutTime = 1.4f;
        private const float FadeInTime = 1.6f;

        private static LoopTransition running;

        /// <summary>Loop number the player is in: 1 present day, 2 years earlier, 3 the aftermath.</summary>
        public static int CurrentLoop { get; private set; } = 1;

        public static bool Busy => running != null;

        // Play mode may skip the domain reload, so statics would survive from the last session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CurrentLoop = 1;
            running = null;
        }

        /// <summary>Set by the loop scene's LoopEnvironment when it starts.</summary>
        internal static void EnterLoop(int loop)
        {
            if (!Busy) CurrentLoop = loop;
        }

        private Image black;

        // The transition shows no text and no rewind effect: the player is never told when, or that,
        // time has turned back.

        /// <summary>Moves to the next loop.</summary>
        public static bool Play(string nextScene, int nextLoop)
        {
            if (Busy || nextLoop <= CurrentLoop) return false;      // never back to a loop already played
            return Begin(nextScene, nextLoop);
        }

        /// <summary>
        /// The "look away" ending: the same transition, but all the way back to Loop 1 to start again.
        /// </summary>
        public static bool Restart(string firstScene)
        {
            if (Busy) return false;
            return Begin(firstScene, 1);
        }

        private static bool Begin(string scene, int loop)
        {
            var go = new GameObject("LoopTransition");
            DontDestroyOnLoad(go);
            running = go.AddComponent<LoopTransition>();
            running.StartCoroutine(running.Run(scene, loop));
            return true;
        }

        private IEnumerator Run(string nextScene, int nextLoop)
        {
            // 1. Freeze the player and pull the camera out to the fixed loop shot.
            SetPlayerControl(false);
            if (CameraDirector.Instance != null) CameraDirector.Instance.SetLoopShot(true);
            BuildOverlay();
            yield return new WaitForSeconds(ShotHold);

            // 2. Fade to black while the next loop loads.
            var load = SceneManager.LoadSceneAsync(nextScene);
            load.allowSceneActivation = false;
            yield return Fade(0f, 1f, FadeOutTime);

            while (load.progress < 0.9f) yield return null;
            // The flashlight is left behind with the old scene; without the item the new player is not
            // handed a fresh one either.
            LeftBehind.Story.StoryStateManager.RemoveItem("flashlight");
            load.allowSceneActivation = true;
            while (!load.isDone) yield return null;
            CurrentLoop = nextLoop;

            // 3. The new loop fades in from black, the player frozen until it has.
            SetPlayerControl(false);
            yield return Fade(1f, 0f, FadeInTime);
            SetPlayerControl(true);

            running = null;
            Destroy(gameObject);
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                black.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            black.color = new Color(0f, 0f, 0f, to);
        }

        private static void SetPlayerControl(bool on)
        {
            var fpc = FindAnyObjectByType<FirstPersonController>();
            if (fpc != null) fpc.enabled = on;
            var interactor = FindAnyObjectByType<PlayerInteractor>();
            if (interactor != null) interactor.enabled = on;
            if (!on)
            {
                var hud = FindAnyObjectByType<InteractionHUD>();
                if (hud != null) hud.HidePrompt();
            }
        }

        private void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var img = new GameObject("Black", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(transform, false);
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            black = img.GetComponent<Image>();
            black.raycastTarget = false;
            black.color = new Color(0f, 0f, 0f, 0f);
        }
    }
}
