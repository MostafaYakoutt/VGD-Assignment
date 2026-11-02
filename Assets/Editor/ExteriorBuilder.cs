using System.Collections.Generic;
using LeftBehind.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Builds the scenery seen through the windows: lawn, picket fence with
    /// an open gate, the front path, a back garden with a swing set and shed,
    /// bare winter trees and pines, the street with lamps, and neighbouring
    /// houses fading into the fog. All meshes are generated low-poly and
    /// saved under Assets/Meshes/Exterior, so the result is deterministic and
    /// can be rebuilt from the menu (Left Behind > Rebuild Exterior).
    /// Nothing outside has a collider: the player can never leave the house.
    /// </summary>
    public static class ExteriorBuilder
    {
        private const string RootName = "Exterior";
        private const string MeshFolder = "Assets/Meshes/Exterior";
        private const string MatFolder = "Assets/Materials/Exterior";
        private const float FenceHalf = 19.5f;
        private const float GateZ0 = -5.0f;
        private const float GateZ1 = -3.2f;

        private static Mesh cube;
        private static Mesh cylinder;
        private static Mesh cone;
        private static Mesh prism;

        [MenuItem("Left Behind/Rebuild Exterior")]
        public static void Build()
        {
            cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            // Resources "Cylinder.fbx" is 2 m wide; the primitive's mesh is the 1 m one the rods expect.
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            cone = MakeCone(7);
            prism = MakePrism();
            EnsureFolder(MeshFolder);
            EnsureFolder(MatFolder);

            var old = GameObject.Find(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build exterior");

            var m = new Materials();
            BuildGround(root.transform, m);
            BuildFence(root.transform, m);
            BuildPathAndGarden(root.transform, m);
            BuildStreet(root.transform, m);
            BuildNeighbours(root.transform, m);
            BuildTrees(root.transform, m);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        // ------------------------------------------------------------------ materials

        private class Materials
        {
            public readonly Material Grass, Bark, Pine, Fence, Path, Stone, Asphalt, Line, Metal, Wood,
                NeighbourWall, NeighbourWallB, Roof, WindowDark, WindowLit, LampHead, Hedge;

            public Materials()
            {
                Grass = Lit("Ext_Grass", new Color(0.46f, 0.44f, 0.34f), 0.05f);
                Grass.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass.jpg"));
                var gn = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass_normal.jpg");
                if (gn != null) { Grass.SetTexture("_BumpMap", gn); Grass.EnableKeyword("_NORMALMAP"); }
                Bark = Lit("Ext_Bark", new Color(0.2f, 0.19f, 0.18f), 0.05f, 0.3f);
                Pine = Lit("Ext_PineNeedles", new Color(0.1f, 0.14f, 0.11f), 0.05f, 0.3f);
                Fence = Lit("Ext_FenceWhitewash", new Color(0.56f, 0.55f, 0.5f), 0.1f, 0.3f);
                Path = Lit("Ext_Gravel", new Color(0.34f, 0.33f, 0.31f), 0.05f, 0.3f);
                Stone = Lit("Ext_Concrete", new Color(0.42f, 0.42f, 0.41f), 0.05f, 0.3f);
                Asphalt = Lit("Ext_Asphalt", new Color(0.11f, 0.11f, 0.12f), 0.15f);
                Line = Lit("Ext_RoadLine", new Color(0.55f, 0.52f, 0.42f), 0.1f);
                Metal = Lit("Ext_DarkMetal", new Color(0.18f, 0.18f, 0.2f), 0.35f, 0.5f);
                Wood = Lit("Ext_WeatheredWood", new Color(0.3f, 0.24f, 0.18f), 0.05f, 0.5f);
                NeighbourWall = Lit("Ext_NeighbourWall", new Color(0.36f, 0.34f, 0.32f), 0.05f, 0.45f);
                NeighbourWallB = Lit("Ext_NeighbourWallB", new Color(0.3f, 0.31f, 0.33f), 0.05f, 0.45f);
                Roof = Lit("Ext_RoofTiles", new Color(0.15f, 0.13f, 0.13f), 0.1f, 0.4f);
                Hedge = Lit("Ext_Hedge", new Color(0.1f, 0.15f, 0.09f), 0.05f, 0.6f);
                WindowDark = Lit("Ext_WindowDark", new Color(0.05f, 0.06f, 0.08f), 0.8f);
                WindowLit = Lit("Ext_WindowLit", new Color(0.4f, 0.3f, 0.18f), 0.3f);
                Emissive(WindowLit, new Color(1f, 0.68f, 0.32f) * 3.5f);
                LampHead = Lit("Ext_StreetLampGlow", new Color(0.9f, 0.8f, 0.6f), 0.3f);
                Emissive(LampHead, new Color(1f, 0.75f, 0.45f) * 4f);
            }

            private static Material Lit(string name, Color c, float smooth, float fill = 0f)
            {
                string path = $"{MatFolder}/{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.SetColor("_BaseColor", c);
                mat.SetFloat("_Smoothness", smooth);
                // Shadowed outdoor surfaces only get the dark ambient light and vanish into the
                // fog, so a little self-illumination acts as a soft fill light (outside only).
                if (fill > 0f) Emissive(mat, c * fill);
                else { mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black); }
                EditorUtility.SetDirty(mat);
                return mat;
            }

            private static void Emissive(Material mat, Color e)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", e);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        // ------------------------------------------------------------------ mesh building

        private class Builder
        {
            private readonly Dictionary<Material, List<CombineInstance>> parts = new Dictionary<Material, List<CombineInstance>>();
            private readonly List<Material> order = new List<Material>();

            public void Add(Mesh mesh, Matrix4x4 m, Material mat)
            {
                if (!parts.TryGetValue(mat, out var list))
                {
                    list = new List<CombineInstance>();
                    parts[mat] = list;
                    order.Add(mat);
                }
                list.Add(new CombineInstance { mesh = mesh, transform = m });
            }

            public void Box(Vector3 centre, Vector3 size, Material mat) => Box(centre, size, Quaternion.identity, mat);

            public void Box(Vector3 centre, Vector3 size, Quaternion rot, Material mat) =>
                Add(cube, Matrix4x4.TRS(centre, rot, size), mat);

            public void Rod(Vector3 a, Vector3 b, float radius, Material mat)
            {
                Vector3 d = b - a;
                Add(cylinder, Matrix4x4.TRS((a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, d),
                    new Vector3(radius * 2f, d.magnitude * 0.5f, radius * 2f)), mat);
            }

            public void Cone(Vector3 basePos, float radius, float height, float yaw, Material mat) =>
                Add(cone, Matrix4x4.TRS(basePos, Quaternion.Euler(0f, yaw, 0f), new Vector3(radius, height, radius)), mat);

            /// <summary>Gable roof: ridge along local z, eaves on local x.</summary>
            public void Roof(Vector3 eaveCentre, float width, float height, float length, float yaw, Material mat) =>
                Add(prism, Matrix4x4.TRS(eaveCentre, Quaternion.Euler(0f, yaw, 0f), new Vector3(width, height, length)), mat);

            public (Mesh mesh, Material[] mats) Bake(string name)
            {
                var subs = new List<CombineInstance>();
                foreach (var mat in order)
                {
                    var part = new Mesh { indexFormat = IndexFormat.UInt32 };
                    part.CombineMeshes(parts[mat].ToArray(), true, true);
                    subs.Add(new CombineInstance { mesh = part, transform = Matrix4x4.identity });
                }
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(subs.ToArray(), false, true);
                mesh.RecalculateBounds();
                string path = $"{MeshFolder}/{name}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                return (mesh, order.ToArray());
            }
        }

        private static GameObject Spawn(Transform parent, string name, (Mesh mesh, Material[] mats) baked,
            Vector3 pos, float yaw = 0f, float scale = 1f, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = baked.mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = baked.mats;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        private static Transform Group(Transform root, string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root, false);
            return g.transform;
        }

        // ------------------------------------------------------------------ parts of the scene

        private static void BuildGround(Transform root, Materials m)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Lawn";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, -0.05f, 0f);   // top at y 0, under the house slab
            ground.transform.localScale = new Vector3(100f, 0.1f, 100f);
            var r = ground.GetComponent<MeshRenderer>();
            r.sharedMaterial = m.Grass;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var wt = ground.AddComponent<WorldTiling>();
            var so = new SerializedObject(wt);
            so.FindProperty("mode").enumValueIndex = (int)WorldTiling.Mode.Floor;
            so.FindProperty("tileSize").floatValue = 2.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            wt.Apply();
        }

        private static void FenceRun(Builder b, Vector3 from, Vector3 to, Material mat)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            Vector3 dir = d / len;
            Vector3 outward = Vector3.Cross(Vector3.up, dir) * 0.03f;
            var rot = Quaternion.LookRotation(dir);
            for (float s = 0.11f; s < len; s += 0.22f)
                b.Box(from + dir * s + Vector3.up * 0.5f, new Vector3(0.025f, 1.0f, 0.08f), rot, mat);
            for (float s = 0f; s <= len + 0.01f; s += len / Mathf.Max(1, Mathf.Round(len / 2.4f)))
                b.Box(from + dir * s + Vector3.up * 0.57f, new Vector3(0.1f, 1.15f, 0.1f), rot, mat);
            foreach (float y in new[] { 0.3f, 0.75f })
                b.Box(from + d * 0.5f + Vector3.up * y - outward, new Vector3(0.03f, 0.08f, len), rot, mat);
        }

        private static void BuildFence(Transform root, Materials m)
        {
            float h = FenceHalf;
            var b = new Builder();
            FenceRun(b, new Vector3(-h, 0f, h), new Vector3(h, 0f, h), m.Fence);
            FenceRun(b, new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h), m.Fence);
            FenceRun(b, new Vector3(-h, 0f, -h), new Vector3(-h, 0f, h), m.Fence);
            FenceRun(b, new Vector3(h, 0f, h), new Vector3(h, 0f, GateZ1), m.Fence);
            FenceRun(b, new Vector3(h, 0f, GateZ0), new Vector3(h, 0f, -h), m.Fence);
            Spawn(root, "PicketFence", b.Bake("PicketFence"), Vector3.zero);

            // The gate stands open, swung out towards the street.
            var g = new Builder();
            FenceRun(g, Vector3.zero, new Vector3(0f, 0f, GateZ1 - GateZ0), m.Fence);
            Spawn(root, "Gate_Open", g.Bake("FenceGate"), new Vector3(h, 0f, GateZ0), 60f);
        }

        private static void BuildPathAndGarden(Transform root, Materials m)
        {
            var path = new Builder();
            path.Box(new Vector3(10.4f, 0.09f, -4.1f), new Vector3(0.9f, 0.18f, 2.2f), m.Stone);         // door step
            path.Box(new Vector3(15.4f, 0.01f, -4.1f), new Vector3(9.2f, 0.02f, 1.5f), m.Path);          // gravel path
            path.Box(new Vector3(19.5f, 0.6f, GateZ1 + 0.06f), new Vector3(0.14f, 1.2f, 0.14f), m.Fence); // gate posts
            path.Box(new Vector3(19.5f, 0.6f, GateZ0 - 0.06f), new Vector3(0.14f, 1.2f, 0.14f), m.Fence);
            path.Rod(new Vector3(20.3f, 0f, -2.6f), new Vector3(20.3f, 1.1f, -2.6f), 0.04f, m.Wood);      // mailbox
            path.Box(new Vector3(20.3f, 1.22f, -2.6f), new Vector3(0.45f, 0.25f, 0.22f), m.Metal);
            Spawn(root, "FrontPath", path.Bake("FrontPath"), Vector3.zero);

            // Back garden, seen from the living room windows: a swing set with one empty swing.
            var swing = new Builder();
            foreach (float x in new[] { -1.3f, 1.3f })
            {
                swing.Rod(new Vector3(x, 0f, -0.9f), new Vector3(x, 2.3f, 0f), 0.05f, m.Metal);
                swing.Rod(new Vector3(x, 0f, 0.9f), new Vector3(x, 2.3f, 0f), 0.05f, m.Metal);
            }
            swing.Rod(new Vector3(-1.4f, 2.3f, 0f), new Vector3(1.4f, 2.3f, 0f), 0.05f, m.Metal);
            foreach (float sx in new[] { -0.6f, 0.6f })
            {
                swing.Rod(new Vector3(sx - 0.22f, 2.3f, 0f), new Vector3(sx - 0.22f, 0.55f, 0f), 0.012f, m.Metal);
                swing.Rod(new Vector3(sx + 0.22f, 2.3f, 0f), new Vector3(sx + 0.22f, 0.55f, 0f), 0.012f, m.Metal);
                swing.Box(new Vector3(sx, 0.53f, 0f), new Vector3(0.5f, 0.04f, 0.2f), m.Wood);
            }
            Spawn(root, "SwingSet", swing.Bake("SwingSet"), new Vector3(-15f, 0f, -3.5f), 90f);

            // Garden shed in the north-west corner, seen from the dining room.
            var shed = new Builder();
            shed.Box(new Vector3(0f, 1.1f, 0f), new Vector3(3f, 2.2f, 2.4f), m.Wood);
            shed.Roof(new Vector3(0f, 2.2f, 0f), 2.9f, 0.9f, 3.4f, 90f, m.Roof);
            shed.Box(new Vector3(0f, 0.95f, 1.21f), new Vector3(0.9f, 1.9f, 0.04f), m.WindowDark);
            Spawn(root, "Shed", shed.Bake("Shed"), new Vector3(-15.5f, 0f, 15.5f), 180f);

            // A bench in the north garden and hedges along the fence.
            var bench = new Builder();
            bench.Box(new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.06f, 0.45f), m.Wood);
            bench.Box(new Vector3(0f, 0.75f, -0.2f), new Vector3(1.6f, 0.35f, 0.05f), m.Wood);
            foreach (float x in new[] { -0.7f, 0.7f }) bench.Box(new Vector3(x, 0.22f, 0f), new Vector3(0.06f, 0.45f, 0.4f), m.Metal);
            Spawn(root, "Bench", bench.Bake("Bench"), new Vector3(-2.5f, 0f, 16.5f), 180f);

            var hedge = new Builder();
            var rng = new System.Random(3);
            for (float x = -18.5f; x < -5f; x += 1.5f)
                hedge.Box(new Vector3(x, 0.6f, 18.6f), new Vector3(1.6f, 1.2f + (float)rng.NextDouble() * 0.3f, 0.9f), m.Hedge);
            for (float z = -18f; z < -8f; z += 1.5f)
                hedge.Box(new Vector3(-18.6f, 0.6f, z), new Vector3(0.9f, 1.2f + (float)rng.NextDouble() * 0.3f, 1.6f), m.Hedge);
            Spawn(root, "Hedges", hedge.Bake("Hedges"), Vector3.zero);
        }

        private static void BuildStreet(Transform root, Materials m)
        {
            var b = new Builder();
            b.Box(new Vector3(21.75f, 0.06f, 0f), new Vector3(2.5f, 0.12f, 100f), m.Stone);       // pavement
            b.Box(new Vector3(26.5f, 0.01f, 0f), new Vector3(7f, 0.02f, 100f), m.Asphalt);       // road
            for (float z = -48f; z < 48f; z += 4f)
                b.Box(new Vector3(26.5f, 0.025f, z), new Vector3(0.15f, 0.01f, 2f), m.Line);
            b.Box(new Vector3(31.25f, 0.06f, 0f), new Vector3(2.5f, 0.12f, 100f), m.Stone);       // far pavement
            Spawn(root, "Street", b.Bake("Street"), Vector3.zero, 0f, 1f, false);

            var lamp = new Builder();
            lamp.Rod(Vector3.zero, new Vector3(0f, 4.2f, 0f), 0.07f, m.Metal);
            lamp.Rod(new Vector3(0f, 4.15f, 0f), new Vector3(-0.9f, 4.4f, 0f), 0.04f, m.Metal);
            lamp.Box(new Vector3(-0.95f, 4.3f, 0f), new Vector3(0.5f, 0.12f, 0.3f), m.Metal);
            lamp.Box(new Vector3(-0.95f, 4.22f, 0f), new Vector3(0.4f, 0.04f, 0.22f), m.LampHead);
            var baked = lamp.Bake("StreetLamp");
            var lamps = Group(root, "StreetLamps");
            foreach (float z in new[] { -12f, 10f, 32f })
            {
                var go = Spawn(lamps, "StreetLamp", baked, new Vector3(22.3f, 0.12f, z));
                var lg = new GameObject("Lamp Light");
                lg.transform.SetParent(go.transform, false);
                lg.transform.localPosition = new Vector3(-0.95f, 3.9f, 0f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.75f, 0.45f);
                l.intensity = 3f;
                l.range = 12f;
                l.shadows = LightShadows.None;
            }
        }

        private static (Mesh, Material[]) House(string name, float w, float depth, float h, float roofH,
            string litPattern, Material wall, Materials m)
        {
            var b = new Builder();
            b.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, depth), wall);
            b.Roof(new Vector3(0f, h, 0f), depth + 0.6f, roofH, w + 0.6f, 90f, m.Roof);
            b.Box(new Vector3(w * 0.3f, h + roofH * 0.55f, 0f), new Vector3(0.6f, roofH * 1.1f, 0.6f), wall);  // chimney
            float front = depth * 0.5f + 0.02f;
            b.Box(new Vector3(0f, 1.05f, front), new Vector3(1.0f, 2.1f, 0.06f), m.WindowDark);              // door
            int i = 0;
            int rows = h > 4.5f ? 2 : 1;
            for (int row = 0; row < rows; row++)
            {
                float y = row == 0 ? 1.6f : h - 1.3f;
                foreach (float x in new[] { -w * 0.33f, -w * 0.16f, w * 0.16f, w * 0.33f })
                {
                    if (row == 0 && Mathf.Abs(x) < 1f) continue;
                    bool lit = i < litPattern.Length && litPattern[i] == '1';
                    b.Box(new Vector3(x, y, front), new Vector3(0.9f, 1.1f, 0.06f), lit ? m.WindowLit : m.WindowDark);
                    i++;
                }
            }
            return b.Bake(name);
        }

        private static void BuildNeighbours(Transform root, Materials m)
        {
            var g = Group(root, "Neighbours");
            // Fronts face the house. Far clip is 30 m, so they read as foggy silhouettes with a few lit windows.
            Spawn(g, "House_East_A", House("NeighbourHouse_A", 9f, 8f, 5.6f, 3f, "01000010", m.NeighbourWall, m), new Vector3(38f, 0f, -6f), -90f);
            Spawn(g, "House_East_B", House("NeighbourHouse_B", 8f, 7f, 3.2f, 2.4f, "0010", m.NeighbourWallB, m), new Vector3(37f, 0f, 13f), -90f);
            Spawn(g, "House_North", House("NeighbourHouse_C", 10f, 8f, 5.6f, 3.2f, "00000100", m.NeighbourWallB, m), new Vector3(-6f, 0f, 33f), 180f);
            Spawn(g, "House_NorthEast", House("NeighbourHouse_D", 8f, 7f, 5.2f, 2.8f, "0000000", m.NeighbourWall, m), new Vector3(14f, 0f, 35f), 180f);
            Spawn(g, "House_West", House("NeighbourHouse_E", 9f, 8f, 5.6f, 3f, "10000000", m.NeighbourWall, m), new Vector3(-34f, 0f, 3f), 90f);
            Spawn(g, "House_South", House("NeighbourHouse_F", 10f, 8f, 3.4f, 2.6f, "0100", m.NeighbourWallB, m), new Vector3(4f, 0f, -34f), 0f);
        }

        private static void Branch(Builder b, Vector3 p, Vector3 dir, float len, float r, int depth, System.Random rng, Material mat)
        {
            Vector3 end = p + dir * len;
            b.Rod(p, end, r, mat);
            if (depth == 0) return;
            int kids = depth >= 3 ? 2 : 2 + rng.Next(2);
            Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            for (int k = 0; k < kids; k++)
            {
                float spin = (float)rng.NextDouble() * 360f;
                float tilt = 22f + (float)rng.NextDouble() * 28f;
                Vector3 nd = Quaternion.AngleAxis(spin, dir) * (Quaternion.AngleAxis(tilt, side) * dir);
                nd = (nd + Vector3.up * 0.2f).normalized;
                Vector3 start = Vector3.Lerp(p, end, k == 0 ? 1f : 0.6f + 0.35f * (float)rng.NextDouble());
                Branch(b, start, nd, len * (0.6f + 0.15f * (float)rng.NextDouble()), Mathf.Max(0.05f, r * 0.7f), depth - 1, rng, mat);
            }
        }

        private static void BuildTrees(Transform root, Materials m)
        {
            var bare = new List<(Mesh, Material[])>();
            for (int v = 0; v < 4; v++)
            {
                var b = new Builder();
                var rng = new System.Random(11 + v * 7);
                var lean = new Vector3((float)rng.NextDouble() * 0.16f - 0.08f, 1f, (float)rng.NextDouble() * 0.16f - 0.08f).normalized;
                Branch(b, Vector3.zero, lean, 2.2f + (float)rng.NextDouble() * 0.6f, 0.15f, 4, rng, m.Bark);
                bare.Add(b.Bake("BareTree_" + v));
            }
            var pines = new List<(Mesh, Material[])>();
            for (int v = 0; v < 3; v++)
            {
                var b = new Builder();
                b.Rod(Vector3.zero, new Vector3(0f, 1.3f, 0f), 0.13f, m.Bark);
                int layers = 4 + v;
                for (int i = 0; i < layers; i++)
                {
                    float t = i / (float)layers;
                    b.Cone(new Vector3(0f, 0.9f + i * 1.0f, 0f), Mathf.Lerp(1.7f, 0.5f, t), 2.1f - t * 0.6f, i * 23f, m.Pine);
                }
                pines.Add(b.Bake("Pine_" + v));
            }

            var g = Group(root, "Trees");
            var placeRng = new System.Random(7);
            var placed = new List<Vector2>();

            // A few trees placed by hand in front of windows that would otherwise look onto bare lawn.
            var featured = new (Vector2 pos, bool pine, int variant, float yaw, float scale)[]
            {
                (new Vector2(-5.5f, -15.5f), false, 0, 40f, 1.15f),   // corridor south windows
                (new Vector2(6.5f, -16f), true, 1, 0f, 1.0f),
                (new Vector2(2.6f, 14.5f), false, 2, 200f, 1.1f),     // kitchen north window
                (new Vector2(-9.0f, 15.5f), true, 0, 0f, 1.05f),      // dining north windows
                (new Vector2(15.5f, 5.5f), false, 3, 120f, 1.2f),     // back hall east window
                (new Vector2(-16.0f, 7.0f), false, 1, 300f, 1.1f),    // master bedroom west window
            };
            foreach (var f in featured)
            {
                var mesh = f.pine ? pines[f.variant] : bare[f.variant];
                Spawn(g, f.pine ? "Pine" : "BareTree", mesh, new Vector3(f.pos.x, 0f, f.pos.y), f.yaw, f.scale);
                placed.Add(f.pos);
            }
            bool Free(Vector2 p)
            {
                if (Mathf.Abs(p.x) < 13.5f && Mathf.Abs(p.y) < 13.5f) return false;       // keep clear of the house and its windows
                if (p.x > 19f && p.x < 33f) return false;                                     // street and pavements
                if (p.x > 9f && p.x < 21f && p.y > -7f && p.y < -1.5f) return false;          // front path and gate
                if ((p - new Vector2(-15f, -3.5f)).magnitude < 3.5f) return false;           // swing set
                if ((p - new Vector2(-15.5f, 15.5f)).magnitude < 3.5f) return false;         // shed
                if ((p - new Vector2(-2.5f, 16.5f)).magnitude < 2.5f) return false;          // bench
                if (Mathf.Abs(p.x + 18.6f) < 1.5f && p.y < -7f) return false;                 // west hedge
                if (Mathf.Abs(p.y - 18.6f) < 1.5f && p.x < -4f) return false;                 // north hedge
                foreach (var q in placed) if ((p - q).magnitude < 4.2f) return false;
                return true;
            }
            int bareCount = 0, pineCount = 0;
            for (int attempt = 0; attempt < 4000 && (bareCount < 22 || pineCount < 16); attempt++)
            {
                var p = new Vector2((float)placeRng.NextDouble() * 80f - 40f, (float)placeRng.NextDouble() * 80f - 40f);
                if (p.magnitude > 42f || !Free(p)) continue;
                bool pine = bareCount >= 22 || (pineCount < 16 && placeRng.NextDouble() < 0.4);
                var set = pine ? pines : bare;
                var mesh = set[placeRng.Next(set.Count)];
                float yaw = (float)placeRng.NextDouble() * 360f;
                float scale = 0.85f + (float)placeRng.NextDouble() * 0.45f;
                Spawn(g, pine ? "Pine" : "BareTree", mesh, new Vector3(p.x, 0f, p.y), yaw, scale);
                placed.Add(p);
                if (pine) pineCount++; else bareCount++;
            }
        }

        // ------------------------------------------------------------------ primitive meshes

        private static Mesh MakeCone(int sides)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                int s = v.Count;
                v.Add(p0); v.Add(Vector3.up); v.Add(p1);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
                s = v.Count;
                v.Add(p0); v.Add(p1); v.Add(Vector3.zero);           // underside
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }
            var mesh = new Mesh { name = "LowPolyCone" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.SetUVs(0, new Vector2[v.Count]);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh MakePrism()
        {
            // Cross-section in x-y: eaves at x = +-0.5, y = 0; ridge at x = 0, y = 1. Extruded along z -0.5..0.5.
            Vector3 L0 = new Vector3(-0.5f, 0f, -0.5f), R0 = new Vector3(0.5f, 0f, -0.5f), T0 = new Vector3(0f, 1f, -0.5f);
            Vector3 L1 = new Vector3(-0.5f, 0f, 0.5f), R1 = new Vector3(0.5f, 0f, 0.5f), T1 = new Vector3(0f, 1f, 0.5f);
            var v = new List<Vector3>();
            var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int s = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 }); }
            void Tri(Vector3 a, Vector3 b, Vector3 c) { int s = v.Count; v.Add(a); v.Add(b); v.Add(c); t.AddRange(new[] { s, s + 1, s + 2 }); }
            Quad(L0, T0, T1, L1);   // west slope
            Quad(R1, T1, T0, R0);   // east slope
            Tri(L0, R0, T0);        // gable at -z
            Tri(R1, L1, T1);        // gable at +z
            Quad(L1, R1, R0, L0);   // underside
            var mesh = new Mesh { name = "GableRoof" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.SetUVs(0, new Vector2[v.Count]);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
