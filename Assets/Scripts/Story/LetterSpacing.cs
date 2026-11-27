using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>Extra space between the letters of a single line of text (Text has no tracking of its own).</summary>
    public class LetterSpacing : BaseMeshEffect
    {
        [SerializeField] private float spacing = 6f;
        private static readonly List<UIVertex> verts = new List<UIVertex>();

        public float Spacing
        {
            get => spacing;
            set { spacing = value; if (graphic != null) graphic.SetVerticesDirty(); }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Mathf.Approximately(spacing, 0f)) return;
            verts.Clear();
            vh.GetUIVertexStream(verts);
            int glyphs = verts.Count / 6;
            if (glyphs < 2) return;
            float total = spacing * (glyphs - 1);
            var text = GetComponent<Text>();
            float shift = 0f;
            if (text != null)
            {
                switch (text.alignment)
                {
                    case TextAnchor.UpperCenter: case TextAnchor.MiddleCenter: case TextAnchor.LowerCenter: shift = -total / 2f; break;
                    case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight: shift = -total; break;
                }
            }
            for (int g = 0; g < glyphs; g++)
                for (int k = 0; k < 6; k++)
                {
                    int i = g * 6 + k;
                    var v = verts[i];
                    v.position.x += g * spacing + shift;
                    verts[i] = v;
                }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
