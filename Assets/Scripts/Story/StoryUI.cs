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
    /// and the pause menu. Also owns the player lock used while reading or choosing.
    /// </summary>
    public class StoryUI : MonoBehaviour
    {
        private static StoryUI instance;
        private static readonly HashSet<object> locks = new HashSet<object>();
        private static int escapeUsedFrame = -1;

        /// <summary>Called by whatever handled Esc this frame (closing a note).</summary>
        public static void ConsumeEscape() => escapeUsedFrame = Time.frameCount;

        public static bool EscapeConsumed => escapeUsedFrame == Time.frameCount;

        public static StoryUI Instance
        {
            get
            {
                // Scripts recompiled while playing in the Editor clear this reference but leave the canvas
                // where it was; adopt it, or a second canvas (and a second pause menu, which nothing then
                // closes) would be built on top of it and swallow every click.
                if (instance == null) instance = FindAnyObjectByType<StoryUI>(FindObjectsInactive.Include);
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

        // The objective line should exist from the first frame of play. With domain and scene reload
        // turned off in the Editor, a canvas from an earlier play session can still be around, no longer
        // hearing the story (its listeners were cleared): remove it, or two objective lines overlap.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnPlay()
        {
            foreach (var old in FindObjectsByType<StoryUI>(FindObjectsInactive.Include))
                if (old != instance) DestroyImmediate(old.gameObject);
            _ = Instance;
        }

        private void Awake()
        {
            // Only ever one story canvas.
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;      // set first: the managers added below look it up in their own Awake
            Font = UIStyle.Serif;
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
            UIStyle.Spaced(titleCard, 10f);
            titleCard.color = Faded(0f);

            gameObject.AddComponent<ObjectiveManager>();
            gameObject.AddComponent<DialogueManager>();
            gameObject.AddComponent<NoteReader>();
            gameObject.AddComponent<PauseMenu>();

            // Menus are clicked with the mouse or driven with the arrow keys.
            gameObject.AddComponent<GraphicRaycaster>();
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }
        }

        // ------------------------------------------------------------------ menu buttons

        /// <summary>A text button: dim when idle, warm and marked when highlighted or selected.</summary>
        public Button MakeButton(Transform parent, string label, int size, Vector2 anchor, Vector2 pos, Vector2 sizeDelta,
            UnityEngine.Events.UnityAction onClick, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            // The background is a glow from a brass edge on the left, shown only when the item is highlighted.
            var bg = go.GetComponent<Image>();
            bg.sprite = UIStyle.Glow;
            bg.color = UIStyle.Brass;
            var text = MakeText(go.transform, "Label", size, align, new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta);
            text.text = label;
            text.raycastTarget = false;
            text.color = UIStyle.Ink;
            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.32f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.32f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.6f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.Vertical;
            button.navigation = nav;
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
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

        /// <summary>
        /// Closes every overlay (pause, a note, dialogue) and frees the player. Called whenever
        /// a scene is about to change, so nothing from the old loop is left covering the next screen.
        /// </summary>
        public static void CloseAll()
        {
            if (instance != null)
            {
                instance.GetComponent<PauseMenu>().Dismiss();
                instance.GetComponent<NoteReader>().Dismiss();
                instance.titleCard.color = Faded(0f);
            }
            DialogueManager.StopAll();
            locks.Clear();
            Apply();
        }

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
                titleCard.color = Faded(t / 1.2f * 0.9f);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                titleCard.color = Faded((1f - t / 1.2f) * 0.9f);
                yield return null;
            }
            titleCard.color = Faded(0f);
        }

        private static Color Faded(float alpha) => new Color(UIStyle.Ink.r, UIStyle.Ink.g, UIStyle.Ink.b, alpha);

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
            t.color = UIStyle.Ink;
            t.lineSpacing = 1.05f;
            if (shadow)
            {
                // A soft drop shadow, so the text reads against bright rooms without a box behind it.
                var s = go.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.75f);
                s.effectDistance = new Vector2(1.5f, -1.5f);
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
