using System.IO;
using UnityEditor;
using UnityEngine;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Makes every copy of the family photograph from one source photo
    /// (Tools/source_photos/Family_playing_a_board.jpg, public domain; see the CREDITS.txt there):
    ///   Resources/Menu/FamilyPhoto_Clear, _Blurred           the main menu (4:3)
    ///   Story/Textures/FramePhoto_Clear, _Blurred           the framed prints, behind a mat (0.72:1)
    ///   Story/Textures/FramePhoto_ClearOval                 the round frames in Loop 2 (oval, cut out)
    ///   Resources/Portraits/{Walter,Ruth,Emily,Sam}[_Soft]  the dialogue portraits
    /// "Blurred" blurs only the faces. The photo is toned like a colour print from the early 2000s
    /// that has hung in a sunny room for a while: a little faded, warm, softer blacks.
    /// Menu: Left Behind > Story > Rebuild Family Photos.
    /// </summary>
    public static class FamilyPhotoBuilder
    {
        private const string SourcePath = "Tools/source_photos/Family_playing_a_board.jpg";

        /// <summary>The source is halved on load: every copy is far smaller, and blurring is quicker.</summary>
        private const int Downsample = 2;

        /// <summary>A face in the source photo: centre and radii in full-size source pixels (top-left origin).</summary>
        private struct Face
        {
            public string name;
            public Vector2 centre, radius;
            /// <summary>Centre and side of the dialogue portrait's square crop; around the face unless given.</summary>
            public Vector2 portrait;
            public float portraitSide;
            public Face(string n, float x, float y, float rx, float ry, float px = -1f, float py = -1f, float side = -1f)
            {
                name = n;
                centre = new Vector2(x, y) / Downsample;
                radius = new Vector2(rx, ry) / Downsample;
                portrait = px < 0f ? centre : new Vector2(px, py) / Downsample;
                portraitSide = side < 0f ? radius.y * 2.6f : side / Downsample;
            }
        }

        // Ruth (in profile), Walter, Sam and Emily, left to right, around the board game.
        // Ruth faces Walter, her profile almost touching his cheek, so her portrait is a tighter
        // crop of its own that stops short of his face.
        private static readonly Face[] Faces =
        {
            new Face("Ruth", 645f, 790f, 90f, 135f, 640f, 800f, 250f),
            new Face("Walter", 840f, 735f, 100f, 140f),
            new Face("Sam", 1630f, 955f, 115f, 145f),
            new Face("Emily", 2250f, 705f, 100f, 140f),
        };

        private static Color[] photo;
        private static int sw, sh;

        [MenuItem("Left Behind/Story/Rebuild Family Photos")]
        public static void Build()
        {
            if (!File.Exists(SourcePath)) { Debug.LogError("Family photo: no source at " + SourcePath); return; }
            Load();
            var blurred = BlurFaces(photo, 14);
            // 4:3 at the photo's full height, centred on the family.
            var photoCrop = S(new Rect(160f, 0f, 2657f, 1993f));

            Save("Assets/Resources/Menu/FamilyPhoto_Clear.png", Print(photo, photoCrop, 640, 480, 0.3f));
            Save("Assets/Resources/Menu/FamilyPhoto_Blurred.png", Print(blurred, photoCrop, 640, 480, 0.3f));
            Save("Assets/Story/Textures/FramePhoto_Clear.png", Mounted(photo, photoCrop));
            Save("Assets/Story/Textures/FramePhoto_Blurred.png", Mounted(blurred, photoCrop));
            Save("Assets/Story/Textures/FramePhoto_ClearOval.png", Oval(photo));

            foreach (var f in Faces)
            {
                float side = f.portraitSide;
                var crop = new Rect(f.portrait.x - side / 2f, f.portrait.y - side * 0.45f, side, side);
                Save($"Assets/Resources/Portraits/{f.name}.png", Print(photo, crop, 128, 128, 0.15f));
                Save($"Assets/Resources/Portraits/{f.name}_Soft.png", Print(Blur(photo, 5, crop), crop, 128, 128, 0.15f));
            }
            AssetDatabase.Refresh();
            // Keep the prints at their own size: squeezing them to a power of two blurs the faces.
            foreach (var path in new[] { "Assets/Resources/Menu/FamilyPhoto_Clear.png", "Assets/Resources/Menu/FamilyPhoto_Blurred.png",
                         "Assets/Story/Textures/FramePhoto_Clear.png", "Assets/Story/Textures/FramePhoto_Blurred.png",
                         "Assets/Story/Textures/FramePhoto_ClearOval.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null || importer.npotScale == TextureImporterNPOTScale.None) continue;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            photo = null;
            Debug.Log("Family photo: rebuilt the menu photo, framed prints, oval print and portraits.");
        }

        /// <summary>A rectangle in full-size source pixels, in the halved image's pixels.</summary>
        private static Rect S(Rect r) => new Rect(r.x / Downsample, r.y / Downsample, r.width / Downsample, r.height / Downsample);

        // ------------------------------------------------------------------ images

        private static void Load()
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(SourcePath));
            int fw = tex.width, fh = tex.height;
            var px = tex.GetPixels();
            Object.DestroyImmediate(tex);
            sw = fw / Downsample;
            sh = fh / Downsample;
            photo = new Color[sw * sh];
            float n = Downsample * Downsample;
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                {
                    var c = Color.clear;
                    for (int j = 0; j < Downsample; j++)
                        for (int i = 0; i < Downsample; i++)
                            c += px[(fh - 1 - (y * Downsample + j)) * fw + x * Downsample + i];   // flip to top-left origin
                    photo[y * sw + x] = c / n;
                }
        }

        /// <summary>The photo with each face softly blurred inside an oval a little larger than the head.</summary>
        private static Color[] BlurFaces(Color[] img, int radius)
        {
            var result = (Color[])img.Clone();
            foreach (var f in Faces)
            {
                var area = new Rect(f.centre - f.radius * 1.6f, f.radius * 3.2f);
                var blur = Blur(img, radius, area);
                for (int y = Mathf.Max(0, (int)area.yMin); y < Mathf.Min(sh, (int)area.yMax); y++)
                    for (int x = Mathf.Max(0, (int)area.xMin); x < Mathf.Min(sw, (int)area.xMax); x++)
                    {
                        float dx = (x - f.centre.x) / (f.radius.x * 1.25f), dy = (y - f.centre.y) / (f.radius.y * 1.2f);
                        float w = 1f - Smooth(0.75f, 1.05f, Mathf.Sqrt(dx * dx + dy * dy));
                        int i = y * sw + x;
                        result[i] = Color.Lerp(img[i], blur[i], w);
                    }
            }
            return result;
        }

        /// <summary>Three box blurs (close to a Gaussian), over the given area only, plus a margin.</summary>
        private static Color[] Blur(Color[] img, int r, Rect area)
        {
            var a = (Color[])img.Clone();
            int x0 = Mathf.Max(0, (int)area.xMin - 3 * r), x1 = Mathf.Min(sw, (int)area.xMax + 3 * r);
            int y0 = Mathf.Max(0, (int)area.yMin - 3 * r), y1 = Mathf.Min(sh, (int)area.yMax + 3 * r);
            var tmp = new Color[sw * sh];
            float n = 2 * r + 1;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        var s = Color.clear;
                        for (int k = -r; k <= r; k++) s += a[y * sw + Mathf.Clamp(x + k, 0, sw - 1)];
                        tmp[y * sw + x] = s / n;
                    }
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        var s = Color.clear;
                        for (int k = -r; k <= r; k++) s += tmp[Mathf.Clamp(y + k, 0, sh - 1) * sw + x];
                        a[y * sw + x] = s / n;
                    }
            }
            return a;
        }

        private static Color Sample(Color[] img, float x, float y)
        {
            x = Mathf.Clamp(x, 0f, sw - 1.001f);
            y = Mathf.Clamp(y, 0f, sh - 1.001f);
            int ix = (int)x, iy = (int)y;
            float fx = x - ix, fy = y - iy;
            var a = Color.Lerp(img[iy * sw + ix], img[iy * sw + ix + 1], fx);
            var b = Color.Lerp(img[(iy + 1) * sw + ix], img[(iy + 1) * sw + ix + 1], fx);
            return Color.Lerp(a, b, fy);
        }

        /// <summary>0 below a, 1 above b, eased in between (GLSL smoothstep; Mathf.SmoothStep is an interpolation).</summary>
        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>An early-2000s colour print, a few years on: a little less colour, softer blacks, warmer.</summary>
        private static Color Faded(Color c, float darken)
        {
            float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
            c = Color.Lerp(new Color(lum, lum, lum), c, 0.82f);
            c = new Color(c.r * 1.04f, c.g * 0.99f, c.b * 0.88f);
            c = c * 0.86f + new Color(0.07f, 0.06f, 0.05f);
            c *= darken;
            return new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
        }

        /// <summary>A crop of the photo, resized, toned and darkened a little towards the corners.</summary>
        private static Color[,] Print(Color[] img, Rect crop, int w, int h, float vignette)
        {
            var o = new Color[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    var c = Sample(img, crop.x + u * crop.width, crop.y + v * crop.height);
                    float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 1.41f;
                    o[x, y] = Faded(c, 1f - vignette * d * d);
                }
            return o;
        }

        /// <summary>The print behind an off-white mat with a bevelled window, to fill the portrait frames without stretching.</summary>
        private static Color[,] Mounted(Color[] img, Rect crop)
        {
            const int W = 360, H = 500, PW = 336, PH = 252;
            var mat = new Color(0.9f, 0.88f, 0.83f, 1f);
            var o = new Color[W, H];
            var print = Print(img, crop, PW, PH, 0.25f);
            int px0 = (W - PW) / 2, py0 = (H - PH) / 2;
            var rnd = new System.Random(5);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y));
                    float shade = 0.88f + 0.12f * Mathf.Clamp01(edge / 14f) - (float)rnd.NextDouble() * 0.015f;
                    var c = mat * shade;
                    c.a = 1f;
                    o[x, y] = c;
                }
            for (int y = 0; y < PH; y++)
                for (int x = 0; x < PW; x++)
                    o[px0 + x, py0 + y] = print[x, y];
            // The mat's bevel: a pale cut edge around the window, lit from above.
            var lit = new Color(0.97f, 0.96f, 0.93f, 1f);
            var shaded = new Color(0.78f, 0.76f, 0.71f, 1f);
            for (int b = 1; b <= 3; b++)
            {
                for (int x = px0 - b; x < px0 + PW + b; x++) { o[x, py0 - b] = lit; o[x, py0 + PH - 1 + b] = shaded; }
                for (int y = py0 - b; y < py0 + PH + b; y++) { o[px0 - b, y] = lit; o[px0 + PW - 1 + b, y] = shaded; }
            }
            return o;
        }

        /// <summary>The family in an oval, cropped close around the four faces and softly darkened at the rim.</summary>
        private static Color[,] Oval(Color[] img)
        {
            const int W = 300, H = 333;
            // Wide enough for all four faces, Ruth's at the left included; the oval is taller than the photo,
            // so its top and bottom run past it (sampling clamps to the edge rows, which the rim's fade covers).
            var crop = S(new Rect(300f, 820f - 1194f, 2150f, 2389f));
            var o = new Color[W, H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                    float dx = u * 2f - 1f, dy = v * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    var c = Faded(Sample(img, crop.x + u * crop.width, crop.y + v * crop.height), 1f - 0.45f * Smooth(0.8f, 1f, r));
                    c.a = r < 1f ? 1f : 0f;
                    o[x, y] = c;
                }
            return o;
        }

        private static void Save(string path, Color[,] img)
        {
            int w = img.GetLength(0), h = img.GetLength(1);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[(h - 1 - y) * w + x] = img[x, y];                         // back to Unity's bottom-up rows
            tex.SetPixels(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
