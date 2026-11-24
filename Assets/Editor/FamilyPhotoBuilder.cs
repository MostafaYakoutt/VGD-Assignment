using System.IO;
using UnityEditor;
using UnityEngine;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Makes every copy of the family photograph from one source photo
    /// (Tools/source_photos/W_E_Falk_family_portrait.jpg, public domain; see the CREDITS.txt there):
    ///   Resources/Menu/FamilyPhoto_Clear, _Blurred           the main menu (4:3)
    ///   Story/Textures/FramePhoto_Clear, _Blurred           the framed prints, mounted on a card mat (0.72:1)
    ///   Story/Textures/FramePhoto_ClearOval                 the round frames in Loop 2 (oval, cut out)
    ///   Resources/Portraits/{Walter,Ruth,Emily,Sam}[_Soft]  the dialogue portraits
    /// "Blurred" blurs only the faces. The photo is toned sepia to sit with the game's warm light.
    /// Menu: Left Behind > Story > Rebuild Family Photos.
    /// </summary>
    public static class FamilyPhotoBuilder
    {
        private const string SourcePath = "Tools/source_photos/W_E_Falk_family_portrait.jpg";

        /// <summary>A face in the source photo: centre and radii in pixels (top-left origin).</summary>
        private struct Face
        {
            public string name;
            public Vector2 centre, radius;
            public Face(string n, float x, float y, float rx, float ry) { name = n; centre = new Vector2(x, y); radius = new Vector2(rx, ry); }
        }

        // Father, the girl, the boy and the mother, left to right.
        private static readonly Face[] Faces =
        {
            new Face("Walter", 250f, 130f, 48f, 62f),
            new Face("Emily", 346f, 250f, 40f, 50f),
            new Face("Sam", 512f, 145f, 42f, 54f),
            new Face("Ruth", 760f, 182f, 45f, 60f),
        };

        private static float[] gray;
        private static int sw, sh;

        [MenuItem("Left Behind/Story/Rebuild Family Photos")]
        public static void Build()
        {
            if (!File.Exists(SourcePath)) { Debug.LogError("Family photo: no source at " + SourcePath); return; }
            Load();
            var blurred = BlurFaces(gray, 11);
            var photoCrop = new Rect(40f, 19f, 960f, 720f);          // 4:3, trims the scratched plate edges

            Save("Assets/Resources/Menu/FamilyPhoto_Clear.png", Print(gray, photoCrop, 640, 480, 0.35f));
            Save("Assets/Resources/Menu/FamilyPhoto_Blurred.png", Print(blurred, photoCrop, 640, 480, 0.35f));
            Save("Assets/Story/Textures/FramePhoto_Clear.png", Mounted(gray, photoCrop));
            Save("Assets/Story/Textures/FramePhoto_Blurred.png", Mounted(blurred, photoCrop));
            Save("Assets/Story/Textures/FramePhoto_ClearOval.png", Oval(gray));

            foreach (var f in Faces)
            {
                float side = f.radius.y * 2.6f;
                var crop = new Rect(f.centre.x - side / 2f, f.centre.y - side * 0.45f, side, side);
                var sharp = Print(gray, crop, 128, 128, 0.15f);
                Save($"Assets/Resources/Portraits/{f.name}.png", sharp);
                Save($"Assets/Resources/Portraits/{f.name}_Soft.png", Print(Blur(gray, sw, sh, 4, crop), crop, 128, 128, 0.15f));
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
            Debug.Log("Family photo: rebuilt the menu photo, framed prints, oval print and portraits.");
        }

        // ------------------------------------------------------------------ images

        private static void Load()
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(SourcePath));
            sw = tex.width;
            sh = tex.height;
            var px = tex.GetPixels();
            gray = new float[sw * sh];
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                    gray[y * sw + x] = px[(sh - 1 - y) * sw + x].grayscale;    // flip to top-left origin
            Object.DestroyImmediate(tex);
        }

        /// <summary>The photo with each face softly blurred inside an oval a little larger than the head.</summary>
        private static float[] BlurFaces(float[] img, int radius)
        {
            var result = (float[])img.Clone();
            foreach (var f in Faces)
            {
                var area = new Rect(f.centre - f.radius * 1.6f, f.radius * 3.2f);
                var blur = Blur(img, sw, sh, radius, area);
                for (int y = Mathf.Max(0, (int)area.yMin); y < Mathf.Min(sh, (int)area.yMax); y++)
                    for (int x = Mathf.Max(0, (int)area.xMin); x < Mathf.Min(sw, (int)area.xMax); x++)
                    {
                        float dx = (x - f.centre.x) / (f.radius.x * 1.25f), dy = (y - f.centre.y) / (f.radius.y * 1.2f);
                        float w = 1f - Smooth(0.75f, 1.05f, Mathf.Sqrt(dx * dx + dy * dy));
                        int i = y * sw + x;
                        result[i] = Mathf.Lerp(img[i], blur[i], w);
                    }
            }
            return result;
        }

        /// <summary>Three box blurs (close to a Gaussian), over the given area only, plus a margin.</summary>
        private static float[] Blur(float[] img, int w, int h, int r, Rect area)
        {
            var a = (float[])img.Clone();
            int x0 = Mathf.Max(0, (int)area.xMin - 3 * r), x1 = Mathf.Min(w, (int)area.xMax + 3 * r);
            int y0 = Mathf.Max(0, (int)area.yMin - 3 * r), y1 = Mathf.Min(h, (int)area.yMax + 3 * r);
            var tmp = new float[w * h];
            for (int pass = 0; pass < 3; pass++)
            {
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        float s = 0f; int n = 0;
                        for (int k = -r; k <= r; k++) { int xx = Mathf.Clamp(x + k, 0, w - 1); s += a[y * w + xx]; n++; }
                        tmp[y * w + x] = s / n;
                    }
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        float s = 0f; int n = 0;
                        for (int k = -r; k <= r; k++) { int yy = Mathf.Clamp(y + k, 0, h - 1); s += tmp[yy * w + x]; n++; }
                        a[y * w + x] = s / n;
                    }
            }
            return a;
        }

        private static float Sample(float[] img, float x, float y)
        {
            x = Mathf.Clamp(x, 0f, sw - 1.001f);
            y = Mathf.Clamp(y, 0f, sh - 1.001f);
            int ix = (int)x, iy = (int)y;
            float fx = x - ix, fy = y - iy;
            float a = Mathf.Lerp(img[iy * sw + ix], img[iy * sw + ix + 1], fx);
            float b = Mathf.Lerp(img[(iy + 1) * sw + ix], img[(iy + 1) * sw + ix + 1], fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>0 below a, 1 above b, eased in between (GLSL smoothstep; Mathf.SmoothStep is an interpolation).</summary>
        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static Color Sepia(float g) =>
            new Color(Mathf.Clamp01(g * 1.08f + 0.03f), Mathf.Clamp01(g * 0.97f + 0.02f), Mathf.Clamp01(g * 0.80f), 1f);

        /// <summary>A crop of the photo, resized, toned and darkened a little towards the corners.</summary>
        private static Color[,] Print(float[] img, Rect crop, int w, int h, float vignette)
        {
            var o = new Color[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float g = Sample(img, crop.x + u * crop.width, crop.y + v * crop.height);
                    float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 1.41f;
                    o[x, y] = Sepia(g * (1f - vignette * d * d));
                }
            return o;
        }

        /// <summary>The print centred on a cream card mat, to fill the portrait picture frames without stretching.</summary>
        private static Color[,] Mounted(float[] img, Rect crop)
        {
            const int W = 360, H = 500, PW = 336, PH = 252;
            var card = new Color(0.84f, 0.79f, 0.68f, 1f);
            var o = new Color[W, H];
            var print = Print(img, crop, PW, PH, 0.3f);
            int px0 = (W - PW) / 2, py0 = (H - PH) / 2 - 12;
            var rnd = new System.Random(5);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y));
                    float shade = 0.86f + 0.14f * Mathf.Clamp01(edge / 14f) - (float)rnd.NextDouble() * 0.025f;
                    o[x, y] = card * shade;
                    o[x, y].a = 1f;
                }
            for (int y = 0; y < PH; y++)
                for (int x = 0; x < PW; x++)
                    o[px0 + x, py0 + y] = print[x, y];
            // A thin pressed line around the print, and the studio's imprint under it.
            var line = card * 0.62f; line.a = 1f;
            for (int x = px0 - 5; x < px0 + PW + 5; x++) { o[x, py0 - 5] = line; o[x, py0 + PH + 4] = line; }
            for (int y = py0 - 5; y < py0 + PH + 5; y++) { o[px0 - 5, y] = line; o[px0 + PW + 4, y] = line; }
            var ink = card * 0.55f; ink.a = 1f;
            int iy = py0 + PH + 26;
            for (int x = W / 2 - 46; x < W / 2 + 46; x++)
                if ((x / 5) % 3 != 2) o[x, iy + (int)(Mathf.Sin(x * 0.9f) * 2f)] = ink;
            return o;
        }

        /// <summary>The family in an oval, the photo's backdrop carried out past its top and bottom, fading at the rim.</summary>
        private static Color[,] Oval(float[] img)
        {
            const int W = 300, H = 333;
            var crop = new Rect(60f, 0f, 865f, sh);
            float scale = crop.width / W;
            float top = (H - crop.height / scale) / 2f;
            var o = new Color[W, H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float sx = crop.x + (x + 0.5f) * scale, sy = (y + 0.5f - top) * scale;
                    float g = Sample(img, sx, sy);
                    if (sy < 0f || sy > sh) g = Mathf.Lerp(g, 0.55f, 0.6f);     // beyond the plate: plain backdrop
                    float dx = (x + 0.5f) / W * 2f - 1f, dy = (y + 0.5f) / H * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    g = Mathf.Lerp(g, 0.78f, Smooth(0.72f, 0.98f, r));  // the old oval fade
                    var c = Sepia(g);
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
