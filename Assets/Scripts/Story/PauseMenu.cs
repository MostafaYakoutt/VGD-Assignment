using LeftBehind.Interaction;
using LeftBehind.Loop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// Esc in a loop: time stops and the menu offers Resume, Restart loop, Main menu and Quit,
    /// with the period card naming where (and when) Walter is.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private Image dim;
        private Text period;
        private Button first;
        private bool open;

        public static bool IsOpen => StoryUI.Exists && StoryUI.Instance.GetComponent<PauseMenu>().open;

        private void Awake()
        {
            var ui = StoryUI.Instance;
            dim = ui.MakeFullScreen(ui.Root, "PauseMenu", new Color(0f, 0f, 0f, 0.9f));
            dim.raycastTarget = true;     // swallow clicks meant for the world
            var title = ui.MakeText(dim.transform, "Title", 64, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f),
                new Vector2(160f, 250f), new Vector2(900f, 90f));
            title.text = "Paused";
            period = ui.MakeText(dim.transform, "Period", 30, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f),
                new Vector2(160f, 180f), new Vector2(1200f, 50f));
            period.fontStyle = FontStyle.Italic;
            period.color = new Color(1f, 0.85f, 0.62f, 0.85f);

            string[] labels = { "Resume", "Restart loop", "Main menu", "Quit" };
            UnityEngine.Events.UnityAction[] actions = { Close, RestartLoop, () => StoryChapters.ToMenu(), StoryChapters.Quit };
            for (int i = 0; i < labels.Length; i++)
            {
                var b = ui.MakeButton(dim.transform, labels[i], 40, new Vector2(0f, 0.5f), new Vector2(150f, 60f - i * 72f),
                    new Vector2(520f, 62f), actions[i]);
                b.GetComponentInChildren<Text>().rectTransform.anchoredPosition = new Vector2(10f, 0f);
                if (i == 0) first = b;
            }
            var hint = ui.MakeText(dim.transform, "Hint", 22, TextAnchor.MiddleLeft, new Vector2(0f, 0f),
                new Vector2(160f, 70f), new Vector2(1200f, 40f));
            hint.text = "Restart loop goes back to the moment you arrived in this loop.";
            hint.color = new Color(1f, 1f, 1f, 0.45f);
            dim.gameObject.SetActive(false);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame || StoryUI.EscapeConsumed) return;
            if (open) { Close(); return; }
            if (CanOpen()) Open();
        }

        private static bool CanOpen()
        {
            if (LoopTransition.Busy || NoteReader.IsOpen || Pockets.IsOpen) return false;
            var env = FindAnyObjectByType<LoopEnvironment>();
            if (env == null) return false;                          // the main menu, or no loop running
            var interactor = FindAnyObjectByType<PlayerInteractor>();
            return interactor == null || interactor.Focus == null;   // not while holding an object up to inspect
        }

        private void Open()
        {
            var env = FindAnyObjectByType<LoopEnvironment>();
            period.text = env != null ? env.Period : "";
            open = true;
            dim.gameObject.SetActive(true);
            StoryUI.Lock(this);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private void Close()
        {
            if (!open) return;
            open = false;
            dim.gameObject.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StoryUI.Unlock(this);
        }

        private void RestartLoop()
        {
            Close();
            StoryStateManager.RestoreSnapshot();
            StoryChapters.LoadGameScene(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            if (open) Time.timeScale = 1f;
        }
    }
}
