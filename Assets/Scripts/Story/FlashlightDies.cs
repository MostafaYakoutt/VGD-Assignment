using System.Collections;
using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// In the dark of Loop 2, Walter's flashlight sputters and dies for a few seconds, and nothing he
    /// does brings it back until it comes back on its own.
    /// </summary>
    public class FlashlightDies : PacedScare
    {
        [Header("Flashlight")]
        [SerializeField] private float darkSeconds = 3f;

        private PlayerFlashlight torch;

        public void ConfigureDark(float seconds) => darkSeconds = seconds;

        protected override bool Armed()
        {
            if (torch == null) torch = player.GetComponent<PlayerFlashlight>();
            return torch != null && torch.HasFlashlight && torch.IsOn && !torch.Dying;
        }

        protected override IEnumerator Play()
        {
            yield return torch.Die(darkSeconds);
        }
    }
}
