using System;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// One conversation: its lines, optional choices, and the flag and objective it
    /// sets when it ends. Edited in the Inspector; played by the DialogueManager.
    /// </summary>
    [CreateAssetMenu(menuName = "Left Behind/Dialogue", fileName = "Dialogue")]
    public class DialogueData : ScriptableObject
    {
        public enum Voice
        {
            /// <summary>Walter's own thoughts: italic, no name.</summary>
            Inner,
            /// <summary>A family member as Walter remembers them: named, faded.</summary>
            Remembered,
            /// <summary>Someone speaking aloud.</summary>
            Spoken,
        }

        [Serializable]
        public class Line
        {
            public Voice voice = Voice.Inner;
            [Tooltip("Shown for Remembered and Spoken lines.")]
            public string speaker = "";
            [TextArea(2, 4)] public string text = "";
            [Tooltip("Optional; cut from the family photograph.")]
            public Sprite portrait;
        }

        [Serializable]
        public class Choice
        {
            public string text = "";
            [Tooltip("Flag set when this choice is picked.")]
            public string flagToSet = "";
            [Tooltip("Optional conversation played after this choice.")]
            public DialogueData next;
        }

        public Line[] lines = Array.Empty<Line>();
        [Tooltip("If set, the player must pick one before the conversation ends.")]
        public Choice[] choices = Array.Empty<Choice>();
        public string flagToSetWhenFinished = "";
        [Tooltip("Objective shown when the conversation ends (empty keeps the current one).")]
        public string objectiveWhenFinished = "";
    }
}
