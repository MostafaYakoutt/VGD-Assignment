using System.Collections;
using System.Collections.Generic;
using LeftBehind.Interaction;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// The story interface: one overlay canvas, built in code and kept across loop scenes,
    /// that holds the objective line, the dialogue box, the title card, the note reader
    /// and the Pockets screen. Also owns the player lock used while reading or choosing.
    /// </summary>
    public class StoryUI : MonoBehaviour
    {
        private static StoryUI instance;
        private static readonly HashSet<object> locks = new HashSet<object>();

        public static StoryUI Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("StoryUI");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<StoryUI>();
                }
                return instance;
            }
        }

        public static bool Exists => instance != null;

        public Font Font { get; private set; }
        public RectTransform Root { get; private set; }

        private Text titleCard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            locks.Clear();
        }

        // The objective line and Pockets should exist from the first frame of play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnPlay() => _ = Instance;

        private void Awake()
        {
            instance = this;      // set first: the managers added below look it up in their own Awake
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;      // above the HUD, below the loop transition (1000)
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Root = (RectTransform)transform;

            titleCard = MakeText(Root, "TitleCard", 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f), new Vector2(1600f, 140f));
            titleCard.color = new Color(1f, 1f, 1f, 0f);

            gameObject.AddComponent<ObjectiveManager>();
            gameObject.AddComponent<DialogueManager>();
            gameObject.AddComponent<NoteReader>();
            gameObject.AddComponent<Pockets>();
        }

        // ------------------------------------------------------------------ player lock

        /// <summary>Freezes movement, look and interaction until every owner has unlocked.</summary>
        public static void Lock(object owner)
        {
            if (locks.Add(owner)) Apply();
        }

        public static void Unlock(object owner)
        {
            if (locks.Remove(owner)) Apply();
        }

        public static bool Locked => locks.Count > 0;

        private static void Apply()
        {
            bool free = locks.Count == 0;
            var fpc = FindAnyObjectByType<FirstPersonController>();
            var interactor = FindAnyObjectByType<PlayerInteractor>();
            // An object being inspected holds the controller itself; leave it to the Inspectable.
            bool inspecting = interactor != null && interactor.Focus != null;
            if (fpc != null && !inspecting) fpc.enabled = free;
            if (interactor != null) interactor.enabled = free;
            if (!free)
            {
                var hud = FindAnyObjectByType<InteractionHUD>();
                if (hud != null) hud.HidePrompt();
            }
        }

        // ------------------------------------------------------------------ title card

        public void ShowTitle(string text, float hold) => StartCoroutine(Title(text, hold));

        private IEnumerator Title(string text, float hold)
        {
            titleCard.text = text;
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                titleCard.color = new Color(1f, 1f, 1f, t / 1.2f * 0.9f);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                titleCard.color = new Color(1f, 1f, 1f, (1f - t / 1.2f) * 0.9f);
                yield return null;
            }
            titleCard.color = new Color(1f, 1f, 1f, 0f);
        }

        // ------------------------------------------------------------------ building helpers

        public Text MakeText(Transform parent, string name, int size, TextAnchor align, Vector2 anchor, Vector2 pos,
            Vector2 sizeDelta, bool shadow = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.alignment = align;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = Color.white;
            if (shadow)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.85f);
                s.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public Image MakePanel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public Image MakeFullScreen(Transform parent, string name, Color color)
        {
            var img = MakePanel(parent, name, color, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }
    }
}
