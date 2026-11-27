using System.Collections;
using LeftBehind.Loop;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// Plays one of the two endings once the choice in the study is made.
    /// Remember: the screen fades to black and the game's name comes up, then it returns to the
    /// main menu by itself (any key returns sooner), where the photograph now shows their faces. Look away: the loop transition
    /// takes him back to Loop 1 and it starts again.
    /// The ending reached is saved for the main menu (PlayerPrefs "LeftBehind.Ending").
    /// </summary>
    public class EndingDirector : MonoBehaviour
    {
        public const string EndingKey = "LeftBehind.Ending";

        /// <summary>Seconds the title stays up before the game returns to the menu.</summary>
        private const float TitleHold = 5f;

        [SerializeField] private string rememberFlag = "choseToRemember";
        [SerializeField] private string lookAwayFlag = "choseToLookAway";
        [SerializeField] private string firstScene = "SampleScene";

        private bool started;

        public void Configure(string remember, string lookAway, string scene)
        {
            rememberFlag = remember;
            lookAwayFlag = lookAway;
            firstScene = scene;
        }

        private void OnEnable() => StoryStateManager.FlagSet += OnFlag;
        private void OnDisable() => StoryStateManager.FlagSet -= OnFlag;

        private void OnFlag(string flag)
        {
            if (started) return;
            if (flag == rememberFlag) { started = true; StartCoroutine(Remember()); }
            else if (flag == lookAwayFlag) { started = true; StartCoroutine(LookAway()); }
        }

        private static IEnumerator WaitForDialogue()
        {
            yield return null;      // the follow-up conversation is queued in the same frame as the flag
            while (DialogueManager.Busy) yield return null;
            yield return new WaitForSeconds(0.8f);
        }

        private IEnumerator LookAway()
        {
            yield return WaitForDialogue();
            PlayerPrefs.SetString(EndingKey, "lookAway");
            PlayerPrefs.Save();
            StoryStateManager.ResetAll();
            LoopTransition.Restart(firstScene);
        }

        private IEnumerator Remember()
        {
            yield return WaitForDialogue();
            PlayerPrefs.SetString(EndingKey, "remember");
            PlayerPrefs.Save();
            StoryUI.Lock(this);

            var ui = StoryUI.Instance;
            LeftBehind.Audio.GameAudio.FadeOutAll(3f);
            var black = ui.MakeFullScreen(ui.Root, "Ending", new Color(0f, 0f, 0f, 0f));
            black.transform.SetAsLastSibling();
            yield return Fade(black, 0f, 1f, 3f);

            // Only the game's name, then back to the menu.
            var title = ui.MakeText(black.transform, "Title", 84, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(1600f, 140f), shadow: false);
            title.text = "LEFT BEHIND";
            yield return FadeText(title, 0f, 0.9f, 2f);
            for (float t = 0f; t < TitleHold; t += Time.unscaledDeltaTime)
            {
                if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) break;
                yield return null;
            }
            yield return FadeText(title, 0.9f, 0f, 1.5f);

            StoryUI.Unlock(this);
            Destroy(black.gameObject);
            StoryChapters.ToMenu();
        }

        private static IEnumerator Fade(Image img, float from, float to, float time)
        {
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                img.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / time));
                yield return null;
            }
            img.color = new Color(0f, 0f, 0f, to);
        }

        private static IEnumerator FadeText(Text text, float from, float to, float time)
        {
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                text.color = new Color(1f, 1f, 1f, Mathf.Lerp(from, to, t / time));
                yield return null;
            }
            text.color = new Color(1f, 1f, 1f, to);
        }
    }
}
