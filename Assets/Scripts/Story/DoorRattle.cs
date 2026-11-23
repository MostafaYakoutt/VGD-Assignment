using System.Collections;
using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A door that never opens, like the front door: using it shakes the leaf in its frame.
    /// The story reaction comes from a NarrativeTrigger listening to it.
    /// </summary>
    public class DoorRattle : Interactable
    {
        [Tooltip("The part that shakes; defaults to this transform.")]
        [SerializeField] private Transform leaf;
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private float amount = 0.012f;

        private bool shaking;

        private void Reset() => promptText = "Open the door";

        public void Configure(Transform leafTransform, string prompt)
        {
            leaf = leafTransform;
            promptText = prompt;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!shaking) StartCoroutine(Shake());
        }

        private IEnumerator Shake()
        {
            shaking = true;
            var t = leaf != null ? leaf : transform;
            var home = t.localPosition;
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                float k = 1f - e / duration;
                t.localPosition = home + new Vector3(Mathf.Sin(e * 90f), 0f, Mathf.Cos(e * 70f)) * amount * k;
                yield return null;
            }
            t.localPosition = home;
            shaking = false;
        }
    }
}
