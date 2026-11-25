using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Whether Walter can see something: a point of it is inside the camera's view (a little wider
    /// than the screen, so nothing changes at the very edge) with nothing solid in between.
    /// Used by everything that only moves while unseen: the mannequins and ChangesWhileUnseen.
    /// </summary>
    public static class PlayerView
    {
        private const int IgnoreRaycastLayer = 2;
        private static readonly RaycastHit[] hits = new RaycastHit[8];
        private static readonly Vector3[] points = new Vector3[5];

        /// <summary>
        /// True when any of five points of <paramref name="bounds"/> (centre, top, bottom, left, right)
        /// is in view and not hidden. Colliders under <paramref name="self"/> never hide it, nor do
        /// those <paramref name="seeThrough"/> accepts.
        /// </summary>
        public static bool CanSee(Camera cam, Bounds bounds, Transform self, float margin = 0.08f,
            System.Func<Collider, bool> seeThrough = null)
        {
            if (cam == null) return false;
            var right = cam.transform.right * Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.6f;
            var up = Vector3.up * bounds.extents.y * 0.8f;
            points[0] = bounds.center;
            points[1] = bounds.center + up;
            points[2] = bounds.center - up;
            points[3] = bounds.center + right;
            points[4] = bounds.center - right;
            var eye = cam.transform.position;
            foreach (var p in points)
            {
                var vp = cam.WorldToViewportPoint(p);
                if (vp.z <= 0f || vp.z > cam.farClipPlane) continue;
                if (vp.x < -margin || vp.x > 1f + margin || vp.y < -margin || vp.y > 1f + margin) continue;
                if (LineOfSight(eye, p, self, seeThrough)) return true;
            }
            return false;
        }

        /// <summary>The bounds of every renderer under <paramref name="root"/> (or a small box at it if it has none).</summary>
        public static Bounds RendererBounds(Transform root) => RendererBounds(root, root.GetComponentsInChildren<Renderer>());

        /// <summary>The bounds of the given renderers (or a small box at <paramref name="root"/> if there are none).</summary>
        public static Bounds RendererBounds(Transform root, Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return new Bounds(root.position, Vector3.one * 0.2f);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static bool LineOfSight(Vector3 from, Vector3 to, Transform self, System.Func<Collider, bool> seeThrough)
        {
            var dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.001f) return true;
            int n = Physics.RaycastNonAlloc(from, dir / dist, hits, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (c is CharacterController || c.gameObject.layer == IgnoreRaycastLayer) continue;   // Walter himself, step blockers
                if (self != null && c.transform.IsChildOf(self)) continue;                           // the thing itself
                if (seeThrough != null && seeThrough(c)) continue;
                return false;
            }
            return true;
        }
    }
}
