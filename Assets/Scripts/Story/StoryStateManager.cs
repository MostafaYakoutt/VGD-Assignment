using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Something Walter carries (the flashlight, the spare fuse, the study key). Only its id
    /// matters: doors and the fuse box check for it. Kept as a struct so the ids already
    /// saved in the scenes still load.
    /// </summary>
    [Serializable]
    public struct CarriedItem
    {
        public string id;
    }

    /// <summary>
    /// The story state: flags, the current objective and the items Walter carries.
    /// It is static, so it survives the scene loads between loops; triggers set flags,
    /// and doors, dialogue, objectives and level loads read them.
    /// </summary>
    public static class StoryStateManager
    {
        private static readonly HashSet<string> flags = new HashSet<string>();
        private static readonly HashSet<string> items = new HashSet<string>();

        private static readonly HashSet<string> savedFlags = new HashSet<string>();
        private static readonly HashSet<string> savedItems = new HashSet<string>();
        private static string savedObjective = "";

        public static string Objective { get; private set; } = "";

        public static event Action<string> FlagSet;
        public static event Action<string> ObjectiveChanged;

        // Play mode may skip the domain reload, so clear the state from the last session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            flags.Clear();
            items.Clear();
            Objective = "";
            FlagSet = null;
            ObjectiveChanged = null;
        }

        /// <summary>Remembers the state at the start of a loop, for Restart chapter.</summary>
        public static void TakeSnapshot()
        {
            savedFlags.Clear(); savedFlags.UnionWith(flags);
            savedItems.Clear(); savedItems.UnionWith(items);
            savedObjective = Objective;
        }

        /// <summary>Puts the story back to how it was when the current loop began.</summary>
        public static void RestoreSnapshot()
        {
            flags.Clear(); flags.UnionWith(savedFlags);
            items.Clear(); items.UnionWith(savedItems);
            SetObjective(savedObjective);
        }

        /// <summary>Clears the story for a new playthrough (after an ending). Listeners stay subscribed.</summary>
        public static void ResetAll()
        {
            flags.Clear();
            items.Clear();
            SetObjective("");
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

        public static void AddItem(string id)
        {
            if (!string.IsNullOrEmpty(id)) items.Add(id);
        }

        public static bool HasItem(string id) => !string.IsNullOrEmpty(id) && items.Contains(id);

        public static void RemoveItem(string id) => items.Remove(id);
    }
}
