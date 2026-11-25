using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// The faint outline that marks an object the player can use, the same kind of line as the
    /// flashlight's but dimmer. The outline pieces are baked into the scene by the editor's
    /// InteractableGlowBuilder (Left Behind > Story > Add Interactable Glow); this only shows them
    /// while the object can be used, so a taken item, a restored fuse box or a photo held up close
    /// stops glowing.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableGlow : MonoBehaviour
    {
        [SerializeField] private Interactable target;
        [SerializeField] private GameObject[] outlines = new GameObject[0];

        private bool shown = true;

        public void Configure(Interactable interactable, GameObject[] pieces)
        {
            target = interactable;
            outlines = pieces;
        }

        private void LateUpdate()
        {
            bool show = target != null && target.ShowsGlow;
            if (show == shown) return;
            shown = show;
            foreach (var o in outlines)
                if (o != null) o.SetActive(show);
        }
    }
}
