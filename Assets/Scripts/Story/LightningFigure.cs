using System.Collections;
using LeftBehind.Loop;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Loop 3: once the landing lamps have gone out, lightning lights the landing and someone is
    /// standing at the far end of it. The next flash, a moment later, shows nobody there.
    /// The figure is only ever drawn during that first flash.
    /// </summary>
    public class LightningFigure : PacedScare
    {
        [Header("Figure")]
        [SerializeField] private GameObject figure;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 15f;

        private Bounds spot;
        private Renderer[] renderers;

        public void ConfigureFigure(GameObject f, float min, float max)
        {
            figure = f;
            minDistance = min;
            maxDistance = max;
        }

        private void Awake()
        {
            if (figure == null) return;
            renderers = figure.GetComponentsInChildren<Renderer>(true);
            spot = PlayerView.RendererBounds(figure.transform, renderers);
            Show(false);
        }

        private void Show(bool on)
        {
            if (renderers == null) return;
            foreach (var r in renderers) r.enabled = on;
        }

        protected override bool Armed()
        {
            if (figure == null || Lightning.Instance == null) return false;
            float d = Vector3.Distance(player.position, spot.center);
            if (d < minDistance || d > maxDistance) return false;
            // He has to be looking down the landing towards it.
            return PlayerView.CanSee(viewCamera, spot, figure.transform, -0.1f);
        }

        protected override IEnumerator Play()
        {
            yield return Lightning.Instance.Strike(2, 0.22f, (i, on) => Show(on && i == 0));
            Show(false);
            yield return new WaitForSeconds(2f);
        }
    }
}
