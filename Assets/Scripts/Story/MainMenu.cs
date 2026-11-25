using System.Collections;
using LeftBehind.Loop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// The main menu: Begin, Chapters (the loops already reached, in story order, named
    /// only by number so the menu never says that time turns back) and Quit,
    /// beside the family photograph. The faces in the photograph stay blurred until the
    /// player has finished the game by choosing to remember.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private RectTransform page;
        private GameObject mainList;
        private GameObject chapterList;
        private Button beginButton;
        private Button firstChapter;
        private Image fade;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StoryStateManager.ResetAll();

            var ui = StoryUI.Instance;
            page = ui.MakeFullScreen(ui.Root, "MainMenu", new Color(0.03f, 0.028f, 0.03f, 1f)).rectTransform;
            page.SetAsFirstSibling();

            var title = ui.MakeText(page, "Title", 96, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f),
                new Vector2(150f, 300f), new Vector2(900f, 130f));
            title.text = "LEFT BEHIND";

            Photo(ui);

            mainList = List(ui, "Main");
            beginButton = Item(ui, mainList.transform, "Begin", 0, () => StartCoroutine(Go(1)));
            Item(ui, mainList.transform, "Chapters", 1, ShowChapters);
            Item(ui, mainList.transform, "Quit", 2, StoryChapters.Quit);

            chapterList = List(ui, "Chapters");
            int reached = Mathf.Clamp(PlayerPrefs.GetInt(LoopEnvironment.ReachedKey, 1), 1, StoryChapters.Scenes.Length);
            for (int i = 0; i < StoryChapters.Scenes.Length; i++)
            {
                int loop = i + 1;
                bool open = loop <= reached;
                var b = Item(ui, chapterList.transform, open ? StoryChapters.Names[i] : "—  —  —", i,
                    () => StartCoroutine(Go(loop)));
                b.interactable = open;
                if (!open) b.GetComponentInChildren<Text>().color = new Color(1f, 1f, 1f, 0.3f);
                if (i == 0) firstChapter = b;
            }
            Item(ui, chapterList.transform, "Back", StoryChapters.Scenes.Length + 0.4f, ShowMain);
            chapterList.SetActive(false);

            var hint = ui.MakeText(page, "Hint", 20, TextAnchor.MiddleLeft, new Vector2(0f, 0f),
                new Vector2(156f, 60f), new Vector2(1200f, 34f));
            hint.text = "WASD move     Mouse look     E interact     F flashlight     Esc pause";
            hint.color = new Color(1f, 1f, 1f, 0.35f);

            fade = ui.MakeFullScreen(page, "Fade", new Color(0f, 0f, 0f, 1f));
            StartCoroutine(FadeIn());
            Select(beginButton);
        }

        private void Photo(StoryUI ui)
        {
            bool remembered = PlayerPrefs.GetString(EndingDirector.EndingKey, "") == "remember";
            var tex = Resources.Load<Texture2D>(remembered ? "Menu/FamilyPhoto_Clear" : "Menu/FamilyPhoto_Blurred");
            var frame = ui.MakePanel(page, "PhotoFrame", new Color(0.92f, 0.9f, 0.84f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-170f, 40f), new Vector2(640f, 500f));
            frame.rectTransform.pivot = new Vector2(1f, 0.5f);
            frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3.5f);
            var go = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(frame.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(592f, 444f);
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            var caption = ui.MakeText(frame.transform, "Caption", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f),
                new Vector2(0f, 2f), new Vector2(600f, 30f), shadow: false);
            caption.fontStyle = FontStyle.Italic;
            caption.color = new Color(0.25f, 0.2f, 0.18f, 1f);
            caption.text = remembered ? "Walter, Ruth, Emily and Sam." : "";
        }

        private GameObject List(StoryUI ui, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(page, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(140f, -60f);
            rt.sizeDelta = new Vector2(600f, 400f);
            return go;
        }

        private static Button Item(StoryUI ui, Transform list, string label, float row, UnityEngine.Events.UnityAction act)
        {
            var b = ui.MakeButton(list, label, 42, new Vector2(0f, 1f), new Vector2(0f, -row * 76f), new Vector2(560f, 64f), act);
            b.GetComponentInChildren<Text>().rectTransform.anchoredPosition = new Vector2(16f, 0f);
            return b;
        }

        private void ShowChapters()
        {
            mainList.SetActive(false);
            chapterList.SetActive(true);
            Select(firstChapter);
        }

        private void ShowMain()
        {
            chapterList.SetActive(false);
            mainList.SetActive(true);
            Select(beginButton);
        }

        private static void Select(Button b)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }

        private IEnumerator FadeIn()
        {
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                fade.color = new Color(0f, 0f, 0f, 1f - t / 1.2f);
                yield return null;
            }
            fade.gameObject.SetActive(false);
        }

        private IEnumerator Go(int loop)
        {
            fade.gameObject.SetActive(true);
            fade.transform.SetAsLastSibling();
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                fade.color = new Color(0f, 0f, 0f, t / 0.8f);
                yield return null;
            }
            Destroy(page.gameObject);      // the menu lives on the persistent story canvas
            StoryChapters.Start(loop);
        }

        private void OnDestroy()
        {
            if (page != null) Destroy(page.gameObject);
        }
    }
}
