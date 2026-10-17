using UnityEngine;

namespace LeftBehind.Rendering
{
    /// <summary>
    /// Tiles the material's base map by the object's real size, so one
    /// material can be shared by walls and floors of any length without
    /// stretching. Uses a MaterialPropertyBlock, so the shared material
    /// is left untouched.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class WorldTiling : MonoBehaviour
    {
        public enum Mode
        {
            /// <summary>Unity cube walls: tile the two large faces by length and height.</summary>
            Wall,
            /// <summary>Unity cube floors: tile the top face by width and depth.</summary>
            Floor,
            /// <summary>Meshes whose UVs are already in metres (ProBuilder default).</summary>
            MetreUVs,
        }

        [SerializeField] private Mode mode = Mode.Wall;
        [Tooltip("Real-world size of one texture repeat, in metres.")]
        [SerializeField] private float tileSize = 2.5f;

        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private MaterialPropertyBlock block;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        public void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null || tileSize <= 0f) return;

            Vector3 s = transform.lossyScale;
            Vector2 tiling;
            switch (mode)
            {
                case Mode.Floor:
                    tiling = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.z)) / tileSize;
                    break;
                case Mode.MetreUVs:
                    tiling = Vector2.one / tileSize;
                    break;
                default:
                    // The large faces run along whichever horizontal axis is longer.
                    float length = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
                    tiling = new Vector2(length, Mathf.Abs(s.y)) / tileSize;
                    break;
            }

            block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetVector(BaseMapStId, new Vector4(tiling.x, tiling.y, 0f, 0f));
            r.SetPropertyBlock(block);
        }
    }
}
