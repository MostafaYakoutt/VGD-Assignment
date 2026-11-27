using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LeftBehind.Story
{
    /// <summary>
    /// The look of every box and line of text in the game: a quiet serif (Crimson Text) in warm
    /// off-white, old brass for accents, no hard-edged boxes (soft fades and hairlines instead),
    /// and Walter's notes in pencil handwriting (Reenie Beanie) on aged paper. Fonts are in
    /// Resources/Fonts (SIL Open Font Licence, beside them); the fades and the paper are drawn here.
    /// </summary>
    public static class UIStyle
    {
        /// <summary>Text: warm off-white, like old paper under a lamp.</summary>
        public static readonly Color Ink = new Color(0.93f, 0.9f, 0.83f, 1f);
        /// <summary>Walter's inner voice: a little greyer than spoken text.</summary>
        public static readonly Color Inner = new Color(0.84f, 0.83f, 0.79f, 1f);
        /// <summary>Accents (speaker names, keys, hairlines): old brass.</summary>
        public static readonly Color Brass = new Color(0.79f, 0.65f, 0.45f, 1f);
        /// <summary>Pencil on paper.</summary>
        public static readonly Color Graphite = new Color(0.17f, 0.16f, 0.18f, 0.92f);
        public const string BrassHex = "#C9A673";

        private static Font serif, serifItalic, serifBold, hand;
        private static Sprite fadeUp, hairline, glow, paper;

        public static Font Serif => serif != null ? serif : serif = Load("CrimsonText-Regular");
        public static Font SerifItalic => serifItalic != null ? serifItalic : serifItalic = Load("CrimsonText-Italic");
        public static Font SerifBold => serifBold != null ? serifBold : serifBold = Load("CrimsonText-SemiBold");
        public static Font Hand => hand != null ? hand : hand = Load("ReenieBeanie");

        private static Font Load(string name)
        {
            var f = Resources.Load<Font>("Fonts/" + name);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ------------------------------------------------------------------ text

        /// <summary>Sets a text to the italic face (the real one, not a slanted regular).</summary>
        public static void Italic(Text t)
        {
            t.font = SerifItalic;
            t.fontStyle = FontStyle.Normal;
        }

        /// <summary>Spreads a single line of capitals apart by <paramref name="pixels"/> between letters.</summary>
        public static void Spaced(Text t, float pixels)
        {
            // The spacing has to come before the shadow, or the shadow's copy of each letter is spread twice.
            var shadow = t.GetComponent<Shadow>();
            Color c = shadow != null ? shadow.effectColor : default;
            Vector2 d = shadow != null ? shadow.effectDistance : default;
            if (shadow != null) Object.DestroyImmediate(shadow);
            var s = t.GetComponent<LetterSpacing>();
            if (s == null) s = t.gameObject.AddComponent<LetterSpacing>();
            s.Spacing = pixels;
            if (shadow != null)
            {
                var again = t.gameObject.AddComponent<Shadow>();
                again.effectColor = c;
                again.effectDistance = d;
            }
        }

        /// <summary>"[E] Read the note" becomes the key in brass, then the action: "E   Read the note".</summary>
        public static string KeyHint(string text)
        {
            if (string.IsNullOrEmpty(text) || text[0] != '[') return text;
            int close = text.IndexOf(']');
            if (close < 0) return text;
            return $"<color={BrassHex}>{text.Substring(1, close - 1)}</color>   {text.Substring(close + 1).TrimStart()}";
        }

        // ------------------------------------------------------------------ shapes

        /// <summary>Transparent at the top, solid at the bottom: the shade behind the dialogue.</summary>
        public static Sprite FadeUp => fadeUp != null ? fadeUp : fadeUp = Gradient(1, 64, (x, y) => Mathf.SmoothStep(0f, 1f, 1f - y));

        /// <summary>A line that fades out at both ends.</summary>
        public static Sprite Hairline => hairline != null ? hairline : hairline = Gradient(128, 1, (x, y) => Mathf.Sin(x * Mathf.PI));

        /// <summary>A glow that starts at a bright edge on the left and fades out to the right: a selected menu item.</summary>
        public static Sprite Glow => glow != null ? glow : glow = Gradient(128, 1, (x, y) => x < 0.02f ? 1f : 0.55f * Mathf.Pow(1f - x, 1.6f));

        /// <summary>Aged writing paper: uneven tone, darker towards the edges.</summary>
        public static Sprite Paper => paper != null ? paper : paper = MakePaper();

        private static Sprite Gradient(int w, int h, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = Mathf.Clamp01(alpha(w > 1 ? x / (w - 1f) : 0f, h > 1 ? y / (h - 1f) : 0f));
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakePaper()
        {
            const int w = 195, h = 215;          // a quarter of the sheet's size, drawn point-filtered like the rest of the game
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var px = new Color[w * h];
            var rng = new System.Random(1989);
            var paperTone = new Color(0.86f, 0.82f, 0.71f);
            var stain = new Color(0.72f, 0.62f, 0.45f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (w - 1f), v = y / (h - 1f);
                    // Darker towards the edges and corners, as old paper yellows.
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float age = 1f - Mathf.SmoothStep(0f, 0.18f, edge);
                    float blotch = Mathf.PerlinNoise(u * 5f + 3.1f, v * 5f + 7.7f);
                    float grain = (float)rng.NextDouble() * 0.05f - 0.025f;
                    var c = Color.Lerp(paperTone, stain, 0.55f * age + 0.18f * blotch) + new Color(grain, grain, grain);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>A brass hairline, <paramref name="width"/> wide, fading out at both ends.</summary>
        public static Image Line(StoryUI ui, Transform parent, string name, Vector2 anchor, Vector2 pos, float width, float alpha = 0.55f)
        {
            var img = ui.MakePanel(parent, name, new Color(Brass.r, Brass.g, Brass.b, alpha), anchor, pos, new Vector2(width, 2f));
            img.sprite = Hairline;
            return img;
        }
    }
}
