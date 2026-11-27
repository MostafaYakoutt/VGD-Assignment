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
    /// Esc in a loop: time stops and the menu offers Resume, Restart chapter, Main menu and Quit.
    /// It never names the loop or the time period: that stays for the player to work out.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private Image dim;
        private Button first;
        private Button[] buttons;
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

            string[] labels = { "Resume", "Restart chapter", "Main menu", "Quit" };
            buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var b = ui.MakeButton(dim.transform, labels[i], 40, new Vector2(0f, 0.5f), new Vector2(150f, 60f - i * 72f),
                    new Vector2(520f, 62f), null);
                b.GetComponentInChildren<Text>().rectTransform.anchoredPosition = new Vector2(10f, 0f);
                buttons[i] = b;
            }
            first = buttons[0];
            Wire();
            var hint = ui.MakeText(dim.transform, "Hint", 22, TextAnchor.MiddleLeft, new Vector2(0f, 0f),
                new Vector2(160f, 70f), new Vector2(1200f, 40f));
            hint.text = "Restart chapter goes back to the start of this chapter.";
            hint.color = new Color(1f, 1f, 1f, 0.45f);
            dim.gameObject.SetActive(false);
        }

        // Listeners added in code are not kept when the Editor reloads scripts during play; the
        // buttons survive, so hook them up again whenever this wakes (OnEnable follows a reload).
        private void OnEnable() => Wire();

        private void Wire()
        {
            if (buttons == null) return;
            UnityEngine.Events.UnityAction[] actions = { Close, RestartLoop, () => StoryChapters.ToMenu(), StoryChapters.Quit };
            for (int i = 0; i < buttons.Length && i < actions.Length; i++)
            {
                if (buttons[i] == null) continue;
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(actions[i]);
            }
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
            if (LoopTransition.Busy || NoteReader.IsOpen) return false;
            var env = FindAnyObjectByType<LoopEnvironment>();
            if (env == null) return false;                          // the main menu, or no loop running
            var interactor = FindAnyObjectByType<PlayerInteractor>();
            return interactor == null || interactor.Focus == null;   // not while holding an object up to inspect
        }

        private void Open()
        {
            open = true;
            dim.gameObject.SetActive(true);
            StoryUI.Lock(this);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        /// <summary>Shuts the menu without touching the cursor (leaving the loop for the menu or a restart).</summary>
        public void Dismiss()
        {
            if (!open) return;
            open = false;
            dim.gameObject.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            StoryUI.Unlock(this);
        }

        private void Close()
        {
            if (!open) return;
            open = false;
            dim.gameObject.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
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
