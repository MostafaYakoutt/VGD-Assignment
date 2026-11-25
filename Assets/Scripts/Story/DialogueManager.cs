using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// Plays DialogueData in the dialogue box at the bottom of the screen, one line at a
    /// time with a typewriter effect. Lines advance on their own so the player can keep
    /// walking; a conversation with choices freezes the player until one is picked
    /// with the number keys. Conversations started while one is playing are queued.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        private const float CharsPerSecond = 45f;

        private static readonly Color InnerColour = new Color(0.86f, 0.86f, 0.82f, 1f);
        private static readonly Color RememberedColour = new Color(1f, 0.85f, 0.62f, 0.8f);

        private readonly Queue<(DialogueData data, Action done)> queue = new Queue<(DialogueData, Action)>();
        private Image box;
        private Image portrait;
        private Text speaker;
        private Text body;
        private Image choicePanel;
        private Text[] choiceLabels;
        private bool playing;

        public static bool Busy => StoryUI.Exists && StoryUI.Instance.GetComponent<DialogueManager>().playing;

        /// <summary>Plays (or queues) a conversation; onDone runs after it and any follow-up have finished.</summary>
        public static void Play(DialogueData data, Action onDone = null)
        {
            if (data == null) { onDone?.Invoke(); return; }
            var dm = StoryUI.Instance.GetComponent<DialogueManager>();
            dm.queue.Enqueue((data, onDone));
            if (!dm.playing) dm.StartCoroutine(dm.Run());
        }

        private void Awake()
        {
            var ui = StoryUI.Instance;
            box = ui.MakePanel(ui.Root, "DialogueBox", new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0f),
                new Vector2(0f, 60f), new Vector2(1180f, 160f));
            portrait = ui.MakePanel(box.transform, "Portrait", Color.white, new Vector2(0f, 0.5f), new Vector2(18f, 0f),
                new Vector2(124f, 124f));
            speaker = ui.MakeText(box.transform, "Speaker", 26, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(28f, -14f), new Vector2(700f, 34f));
            body = ui.MakeText(box.transform, "Text", 30, TextAnchor.UpperLeft, new Vector2(0f, 1f),
                new Vector2(28f, -50f), new Vector2(1124f, 100f));

            choicePanel = ui.MakePanel(ui.Root, "Choices", new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0f),
                new Vector2(0f, 240f), new Vector2(560f, 150f));
            choiceLabels = new Text[4];
            for (int i = 0; i < choiceLabels.Length; i++)
                choiceLabels[i] = ui.MakeText(choicePanel.transform, "Choice" + i, 32, TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(36f, -18f - i * 44f), new Vector2(500f, 42f));
            Hide();
        }

        /// <summary>Drops whatever is playing or queued (leaving a loop for the menu or a restart).</summary>
        public static void StopAll()
        {
            if (!StoryUI.Exists) return;
            var dm = StoryUI.Instance.GetComponent<DialogueManager>();
            dm.StopAllCoroutines();
            dm.queue.Clear();
            dm.playing = false;
            dm.Hide();
            StoryUI.Unlock(dm);
        }

        private void Hide()
        {
            box.gameObject.SetActive(false);
            choicePanel.gameObject.SetActive(false);
        }

        private IEnumerator Run()
        {
            playing = true;
            while (queue.Count > 0)
            {
                var (data, done) = queue.Dequeue();
                yield return Play(data);
                done?.Invoke();
            }
            Hide();
            playing = false;
        }

        private IEnumerator Play(DialogueData data)
        {
            foreach (var line in data.lines)
            {
                ShowLine(line);
                string text = line.text ?? "";
                // Typewriter.
                for (float shown = 0f; shown < text.Length; shown += Time.deltaTime * CharsPerSecond)
                {
                    body.text = Format(line, text.Substring(0, Mathf.Min(text.Length, (int)shown)));
                    if (SkipPressed()) break;
                    yield return null;
                }
                body.text = Format(line, text);
                float hold = Mathf.Max(1.8f, text.Length * 0.055f);
                for (float t = 0f; t < hold; t += Time.deltaTime)
                {
                    if (t > 0.2f && SkipPressed()) break;
                    yield return null;
                }
            }

            DialogueData next = null;
            if (data.choices != null && data.choices.Length > 0)
            {
                int picked = -1;
                yield return Choose(data.choices, i => picked = i);
                var choice = data.choices[picked];
                StoryStateManager.Set(choice.flagToSet);
                next = choice.next;
            }

            box.gameObject.SetActive(false);
            StoryStateManager.Set(data.flagToSetWhenFinished);
            if (!string.IsNullOrEmpty(data.objectiveWhenFinished)) StoryStateManager.SetObjective(data.objectiveWhenFinished);
            if (next != null) yield return Play(next);
        }

        private IEnumerator Choose(DialogueData.Choice[] choices, Action<int> picked)
        {
            StoryUI.Lock(this);
            choicePanel.gameObject.SetActive(true);
            choicePanel.rectTransform.sizeDelta = new Vector2(560f, 30f + choices.Length * 44f);
            for (int i = 0; i < choiceLabels.Length; i++)
            {
                bool on = i < choices.Length;
                choiceLabels[i].gameObject.SetActive(on);
                if (on) choiceLabels[i].text = $"{i + 1}.  {choices[i].text}";
            }
            int result = -1;
            while (result < 0)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) result = 0;
                    else if ((kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) && choices.Length > 1) result = 1;
                    else if ((kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) && choices.Length > 2) result = 2;
                    else if ((kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) && choices.Length > 3) result = 3;
                }
                yield return null;
            }
            choicePanel.gameObject.SetActive(false);
            StoryUI.Unlock(this);
            picked(result);
        }

        private static readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();

        // Play mode may skip the domain reload, and the sprites made in the last session are destroyed with it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPortraits() => portraits.Clear();

        /// <summary>Resources/Portraits/{Name}.png, or {Name}_Soft.png before Loop 3, when his memory is still blurred.</summary>
        private static Sprite PortraitFor(DialogueData.Line line)
        {
            if (line.portrait != null) return line.portrait;
            if (line.voice != DialogueData.Voice.Remembered || string.IsNullOrEmpty(line.speaker)) return null;
            string name = char.ToUpper(line.speaker[0]) + line.speaker.Substring(1).ToLower();
            if (LeftBehind.Loop.LoopTransition.CurrentLoop < 3) name += "_Soft";
            if (portraits.TryGetValue(name, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>("Portraits/" + name);
            var sprite = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
            portraits[name] = sprite;
            return sprite;
        }

        private void ShowLine(DialogueData.Line line)
        {
            box.gameObject.SetActive(true);
            var portraitSprite = PortraitFor(line);
            bool hasPortrait = portraitSprite != null;
            portrait.gameObject.SetActive(hasPortrait);
            if (hasPortrait)
            {
                portrait.sprite = portraitSprite;
                portrait.color = line.voice == DialogueData.Voice.Remembered ? new Color(1f, 1f, 1f, 0.7f) : Color.white;
            }
            float left = hasPortrait ? 160f : 28f;
            speaker.rectTransform.anchoredPosition = new Vector2(left, -14f);
            body.rectTransform.anchoredPosition = new Vector2(left, line.voice == DialogueData.Voice.Inner ? -30f : -50f);
            body.rectTransform.sizeDelta = new Vector2(1152f - left, 110f);

            switch (line.voice)
            {
                case DialogueData.Voice.Inner:
                    speaker.text = "";
                    body.fontStyle = FontStyle.Italic;
                    body.color = InnerColour;
                    break;
                case DialogueData.Voice.Remembered:
                    speaker.text = line.speaker;
                    speaker.color = RememberedColour;
                    body.fontStyle = FontStyle.Normal;
                    body.color = RememberedColour;
                    break;
                default:
                    speaker.text = line.speaker;
                    speaker.color = Color.white;
                    body.fontStyle = FontStyle.Normal;
                    body.color = Color.white;
                    break;
            }
        }

        private static string Format(DialogueData.Line line, string text) =>
            line.voice == DialogueData.Voice.Remembered ? "“" + text + (text.Length == (line.text ?? "").Length ? "”" : "") : text;

        private static bool SkipPressed()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        }
    }
}
