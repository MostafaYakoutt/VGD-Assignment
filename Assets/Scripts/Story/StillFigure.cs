using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Holds a rigged figure (a mannequin, the figure on the landing) in one frame of its idle
    /// and never lets it move: a dress form does not breathe.
    /// </summary>
    public class StillFigure : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string state = "Idle";
        [Range(0f, 1f)] [SerializeField] private float pose = 0.3f;

        public void Configure(Animator a, string stateName, float at)
        {
            animator = a;
            state = stateName;
            pose = at;
        }

        private void Start()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            // Pose it even while it is hidden (an Apparition hides its renderers until it appears).
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(state, 0, pose);
            animator.Update(0f);
            animator.enabled = false;      // the pose stays as it was left
        }
    }
}
