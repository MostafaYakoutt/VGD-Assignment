using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>Something Walter carries, shown in the Pockets screen.</summary>
    [Serializable]
    public struct PocketItem
    {
        public string id;
        public string name;
        [TextArea] public string description;
    }

    /// <summary>A note Walter has read, kept in the Pockets screen's Notes list.</summary>
    [Serializable]
    public struct NoteEntry
    {
        public string id;
        public string title;
        [TextArea] public string body;
    }

    /// <summary>
    /// The story state: flags, the current objective, carried items and read notes.
    /// It is static, so it survives the scene loads between loops; triggers set flags,
    /// and doors, dialogue, objectives and level loads read them.
    /// </summary>
    public static class StoryStateManager
    {
        private static readonly HashSet<string> flags = new HashSet<string>();
        private static readonly List<PocketItem> items = new List<PocketItem>();
        private static readonly List<NoteEntry> notes = new List<NoteEntry>();

        private static readonly HashSet<string> savedFlags = new HashSet<string>();
        private static readonly List<PocketItem> savedItems = new List<PocketItem>();
        private static readonly List<NoteEntry> savedNotes = new List<NoteEntry>();
        private static string savedObjective = "";

        public static string Objective { get; private set; } = "";
        public static IReadOnlyList<PocketItem> Items => items;
        public static IReadOnlyList<NoteEntry> Notes => notes;

        public static event Action<string> FlagSet;
        public static event Action<string> ObjectiveChanged;
        public static event Action PocketsChanged;

        // Play mode may skip the domain reload, so clear the state from the last session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            flags.Clear();
            items.Clear();
            notes.Clear();
            Objective = "";
            FlagSet = null;
            ObjectiveChanged = null;
            PocketsChanged = null;
        }

        /// <summary>Remembers the state at the start of a loop, for Restart loop.</summary>
        public static void TakeSnapshot()
        {
            savedFlags.Clear(); savedFlags.UnionWith(flags);
            savedItems.Clear(); savedItems.AddRange(items);
            savedNotes.Clear(); savedNotes.AddRange(notes);
            savedObjective = Objective;
        }

        /// <summary>Puts the story back to how it was when the current loop began.</summary>
        public static void RestoreSnapshot()
        {
            flags.Clear(); flags.UnionWith(savedFlags);
            items.Clear(); items.AddRange(savedItems);
            notes.Clear(); notes.AddRange(savedNotes);
            SetObjective(savedObjective);
            PocketsChanged?.Invoke();
        }

        /// <summary>Clears the story for a new playthrough (after an ending). Listeners stay subscribed.</summary>
        public static void ResetAll()
        {
            flags.Clear();
            items.Clear();
            notes.Clear();
            SetObjective("");
            PocketsChanged?.Invoke();
        }

        public static bool Has(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public static bool HasAll(IEnumerable<string> required)
        {
            if (required == null) return true;
            foreach (var f in required)
                if (!string.IsNullOrEmpty(f) && !flags.Contains(f)) return false;
            return true;
        }

        public static void Set(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !flags.Add(flag)) return;
            Debug.Log($"[Story] {flag} = true");
            FlagSet?.Invoke(flag);
        }

        public static void SetObjective(string text)
        {
            text ??= "";
            if (text == Objective) return;
            Objective = text;
            ObjectiveChanged?.Invoke(text);
        }

        public static void AddItem(PocketItem item)
        {
            if (string.IsNullOrEmpty(item.id) || items.Exists(i => i.id == item.id)) return;
            items.Add(item);
            PocketsChanged?.Invoke();
        }

        public static bool HasItem(string id) => items.Exists(i => i.id == id);

        public static void RemoveItem(string id)
        {
            if (items.RemoveAll(i => i.id == id) > 0) PocketsChanged?.Invoke();
        }

        public static void AddNote(NoteEntry note)
        {
            if (string.IsNullOrEmpty(note.id) || notes.Exists(n => n.id == note.id)) return;
            notes.Add(note);
            PocketsChanged?.Invoke();
        }
    }
}
