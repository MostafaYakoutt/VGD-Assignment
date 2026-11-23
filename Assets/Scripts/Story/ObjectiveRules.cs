using System;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Works out the objective from the story flags, so it always matches the story state
    /// whatever order the player does things in. Rules are checked top to bottom and the
    /// first one whose flags are all set (and none of whose blocking flags are) wins.
    /// One per loop scene.
    /// </summary>
    public class ObjectiveRules : MonoBehaviour
    {
        [Serializable]
        public class Rule
        {
            public string[] allOf = new string[0];
            public string[] noneOf = new string[0];
            public string objective = "";
        }

        [SerializeField] private Rule[] rules = new Rule[0];

        public void Configure(Rule[] r) => rules = r;

        public static Rule R(string objective, params string[] allOf) => new Rule { objective = objective, allOf = allOf };

        private void OnEnable()
        {
            StoryStateManager.FlagSet += OnFlag;
            Evaluate();
        }

        private void OnDisable() => StoryStateManager.FlagSet -= OnFlag;

        private void OnFlag(string _) => Evaluate();

        private void Evaluate()
        {
            foreach (var rule in rules)
            {
                if (!StoryStateManager.HasAll(rule.allOf)) continue;
                bool blocked = false;
                foreach (var f in rule.noneOf) if (StoryStateManager.Has(f)) blocked = true;
                if (blocked) continue;
                StoryStateManager.SetObjective(rule.objective);
                return;
            }
            StoryStateManager.SetObjective("");
        }
    }
}
