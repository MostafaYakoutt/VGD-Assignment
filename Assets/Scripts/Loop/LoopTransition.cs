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
    /// the camera cuts to the fixed loop shot, the frame "rewinds" like a VHS
    /// tape into black, the next loop's scene loads, and the new house fades
    /// in through static with a caption naming the time period.
    /// The previous scene is unloaded, so the trip is one-way.
    /// </summary>
    public class LoopTransition : MonoBehaviour
    {
        private const float ShotHold = 2.1f;      // 1.5 s blend into the loop shot plus a beat
        private const float RewindTime = 1.8f;
        private const float RevealTime = 2.0f;
        private const float CaptionHold = 3.0f;

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

        private RawImage frame;
        private Material mat;
        private Text rewindLabel;
        private Text caption;

        public static bool Play(string nextScene, int nextLoop, string arrivalCaption)
        {
            if (Busy || nextLoop <= CurrentLoop) return false;      // never back to a loop already played
            var go = new GameObject("LoopTransition");
            DontDestroyOnLoad(go);
            running = go.AddComponent<LoopTransition>();
            running.StartCoroutine(running.Run(nextScene, nextLoop, arrivalCaption));
            return true;
        }

        private IEnumerator Run(string nextScene, int nextLoop, string arrivalCaption)
        {
            // 1. Freeze the player and cut to the fixed loop shot.
            SetPlayerControl(false);
            if (CameraDirector.Instance != null) CameraDirector.Instance.SetLoopShot(true);
            yield return new WaitForSeconds(ShotHold);

            // 2. Capture the last frame of this loop and rewind it into black.
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            BuildOverlay(shot);

            var load = SceneManager.LoadSceneAsync(nextScene);
            load.allowSceneActivation = false;

            float roll = 0f;
            for (float t = 0f; t < RewindTime; t += Time.unscaledDeltaTime)
            {
                float k = t / RewindTime;
                roll -= Time.unscaledDeltaTime * Mathf.Lerp(0.05f, 2.5f, k * k);
                mat.SetFloat("_Intensity", Mathf.SmoothStep(0f, 1f, k * 1.6f));
                mat.SetFloat("_Roll", roll);
                mat.SetFloat("_Fade", Mathf.InverseLerp(0.7f, 1f, k));
                rewindLabel.enabled = Mathf.Repeat(t, 0.5f) < 0.3f;
                yield return null;
            }
            mat.SetFloat("_Fade", 1f);
            rewindLabel.enabled = false;

            while (load.progress < 0.9f) yield return null;
            load.allowSceneActivation = true;
            while (!load.isDone) yield return null;
            CurrentLoop = nextLoop;
            Destroy(shot);

            // 3. The new loop fades in through tracking noise, frozen until it settles.
            SetPlayerControl(false);
            mat.SetFloat("_UseTex", 0f);
            frame.texture = null;
            caption.text = arrivalCaption;
            for (float t = 0f; t < RevealTime; t += Time.unscaledDeltaTime)
            {
                float k = t / RevealTime;
                mat.SetFloat("_Fade", 1f - Mathf.SmoothStep(0f, 1f, k * 1.8f));
                mat.SetFloat("_Intensity", 1f - k);
                caption.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 2f) * 0.85f);
                yield return null;
            }
            mat.SetFloat("_Intensity", 0f);
            mat.SetFloat("_Fade", 0f);
            SetPlayerControl(true);

            yield return new WaitForSeconds(CaptionHold);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                caption.color = new Color(1f, 1f, 1f, (1f - t) * 0.85f);
                yield return null;
            }

            running = null;
            Destroy(mat);
            Destroy(gameObject);
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

        private void BuildOverlay(Texture shot)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var img = new GameObject("Frame", typeof(RectTransform), typeof(RawImage));
            img.transform.SetParent(transform, false);
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            frame = img.GetComponent<RawImage>();
            frame.raycastTarget = false;
            frame.texture = shot;
            // Loaded from Resources so the shader is included in builds.
            var template = Resources.Load<Material>("LoopRewind");
            mat = template != null ? new Material(template) : new Material(Shader.Find("LeftBehind/RewindOverlay"));
            mat.SetFloat("_UseTex", 1f);
            frame.material = mat;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rewindLabel = Label("Rewind", "<< REWIND", font, 44, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(80f, -70f), new Vector2(500f, 80f));
            caption = Label("Caption", "", font, 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1400f, 80f));
            caption.color = new Color(1f, 1f, 1f, 0f);
        }

        private Text Label(string name, string text, Font font, int size, TextAnchor align, Vector2 anchor,
            Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.color = Color.white;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(3f, -3f);
            return t;
        }
    }
}
