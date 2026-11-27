using System;
using System.Collections.Generic;
using LeftBehind.Interaction;
using LeftBehind.Loop;
using LeftBehind.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Builds the Loop 2 ("years earlier") house in the open scene, following
    /// Blueprints/Left_Behind_Loop2_Ground_Blueprint.png and _Upper_Blueprint.png.
    /// Loop 2 is the real house, on Loop 1's wall lines, as the protagonist remembers
    /// it two years after the night. Only a few things are wrong, each with a story
    /// reason: the table set for six, the same photo repeated along the corridor and
    /// landing, the study that opens onto fresh bricks, a landing that narrows towards
    /// the Loop Door, and the rooms that are sealed in Loop 1 (Room B and the sewing
    /// room) still open here.
    /// Menu: Left Behind > Build Loop 2 House (replaces any existing Loop2House).
    /// </summary>
    public static class Loop2HouseBuilder
    {
        public const string RootName = "Loop2House";
        private const float Ceil = 2.45f;     // ground floor ceiling underside
        private const float Up = 2.60f;       // upper floor level
        private const float UpCeil = 5.00f;   // upper ceiling underside
        private const float Roof = 5.15f;
        private const float T = 0.1f;         // interior wall thickness
        private const float GroundTop = 0.05f; // top of the ground floor slab (the scene's floor cube)

        private static Transform root;
        private static Material wall, floor, ceiling, brick, cardboard, note, metal, screen;
        private static GameObject doorAsset;
        private static Bounds doorMesh;

        private static readonly (float c, float w, float sill, float top)[] None = Array.Empty<(float, float, float, float)>();

        [MenuItem("Left Behind/Build Loop 2 House")]
        public static void Build()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            root = new GameObject(RootName).transform;

            LoadMaterials();
            doorAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Furniture/Door_1.fbx");
            doorMesh = doorAsset.GetComponent<MeshFilter>().sharedMesh.bounds;

            BuildShell();
            BuildGroundWalls();
            BuildUpperWalls();
            BuildFloorsAndStairs();
            FurnishGround();
            FurnishUpper();
            BlockLowFurniture();
            StoryContentBuilder.SetUpLoop2();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }

        // ------------------------------------------------------------------ materials

        private static void LoadMaterials()
        {
            wall = Variant("Loop2_Wallpaper", "Assets/Materials/Wall_DecrepitWallpaper.mat", new Color(1f, 0.9f, 0.78f));
            floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Floor_OldWoodenFloor.mat");
            ceiling = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ceiling_Plaster.mat");
            brick = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Attic_ChimneyBrick.mat");
            cardboard = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Attic_Cardboard.mat");
            metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Exterior/Ext_DarkMetal.mat");
            screen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Exterior/Ext_WindowDark.mat");
            note = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Loop2_StickyNote.mat");
            if (note == null)
            {
                note = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                note.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.35f));
                note.SetFloat("_Smoothness", 0.1f);
                note.EnableKeyword("_EMISSION");
                note.SetColor("_EmissionColor", new Color(0.25f, 0.2f, 0.05f));
                AssetDatabase.CreateAsset(note, "Assets/Materials/Loop2_StickyNote.mat");
            }
        }

        private static Material Variant(string name, string sourcePath, Color tint)
        {
            string path = $"Assets/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(AssetDatabase.LoadAssetAtPath<Material>(sourcePath));
            m.SetColor("_BaseColor", tint);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ helpers

        private static Transform Group(string path)
        {
            var t = root;
            foreach (var part in path.Split('/'))
            {
                var c = t.Find(part);
                if (c == null)
                {
                    var g = new GameObject(part);
                    g.transform.SetParent(t, false);
                    c = g.transform;
                }
                t = c;
            }
            return t;
        }

        private static Bounds MB(GameObject go)
        {
            bool has = false;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var w = mf.transform.TransformPoint(v);
                    if (!has) { b = new Bounds(w, Vector3.zero); has = true; }
                    else b.Encapsulate(w);
                }
            }
            return b;
        }

        private static void Tile(GameObject g, WorldTiling.Mode mode, float size)
        {
            var wt = g.AddComponent<WorldTiling>();
            var so = new SerializedObject(wt);
            so.FindProperty("mode").enumValueIndex = (int)mode;
            so.FindProperty("tileSize").floatValue = size;
            so.ApplyModifiedPropertiesWithoutUndo();
            wt.Apply();
        }

        private static GameObject Box(string group, string name, float x0, float x1, float y0, float y1, float z0, float z1,
            Material mat, WorldTiling.Mode? tile = null, float tileSize = 2.5f, bool collider = true)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = name;
            c.transform.SetParent(Group(group), false);
            c.transform.position = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f);
            c.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            c.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(c.GetComponent<Collider>());
            if (tile.HasValue) Tile(c, tile.Value, tileSize);
            return c;
        }

        /// <summary>Wall along x (at z = t0..t1) or along z (at x = t0..t1), from a to b, with openings.</summary>
        private static void Wall(string group, string name, bool alongX, float t0, float t1, float a, float b, float y0, float y1,
            params (float c, float w, float sill, float top)[] ops)
        {
            var sorted = new List<(float c, float w, float sill, float top)>(ops);
            sorted.Sort((p, q) => p.c.CompareTo(q.c));
            float cursor = a;
            int i = 0;
            void Piece(string n, float s0, float s1, float h0, float h1)
            {
                if (s1 - s0 < 0.001f || h1 - h0 < 0.001f) return;
                if (alongX) Box(group, n, s0, s1, h0, h1, t0, t1, wall, WorldTiling.Mode.Wall);
                else Box(group, n, t0, t1, h0, h1, s0, s1, wall, WorldTiling.Mode.Wall);
            }
            foreach (var o in sorted)
            {
                float o0 = o.c - o.w / 2f, o1 = o.c + o.w / 2f;
                Piece($"{name}_Pier{i}", cursor, o0, y0, y1);
                Piece($"{name}_Sill{i}", o0, o1, y0, y0 + o.sill);
                Piece($"{name}_Head{i}", o0, o1, y0 + o.top, y1);
                cursor = o1;
                i++;
            }
            Piece($"{name}_Pier{i}", cursor, b, y0, y1);
        }

        private static (float, float, float, float) Win(float c, float w = 1.2f) => (c, w, 0.9f, 2.2f);
        private static (float, float, float, float) Gap(float c, float w = 0.9f) => (c, w, 0f, 2.1f);

        private static void HWall(float z, float x0, float x1, float y0, float y1, params (float, float, float, float)[] ops) =>
            Wall("Walls", $"H{z:0.0}_{y0:0}", true, z - T / 2, z + T / 2, x0, x1, y0, y1, ops);

        private static void VWall(float x, float z0, float z1, float y0, float y1, params (float, float, float, float)[] ops) =>
            Wall("Walls", $"V{x:0.0}_{y0:0}", false, x - T / 2, x + T / 2, z0, z1, y0, y1, ops);

        private static void Window(string model, bool alongX, float t0, float t1, float c, float w, float floorY)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Furniture/{model}.fbx");
            var win = (GameObject)PrefabUtility.InstantiatePrefab(asset, Group("Windows"));
            float yaw = alongX ? (t0 > 0 ? 180f : 0f) : (t0 > 0 ? 270f : 90f);
            win.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.rotation;
            win.transform.localScale = asset.transform.localScale;
            win.transform.position = Vector3.zero;
            var bb = MB(win);
            float bw = alongX ? bb.size.x : bb.size.z;
            var s = win.transform.localScale;
            s.x *= w / bw;
            s.z *= 1.3f / bb.size.y;
            s.y *= 0.8f;
            win.transform.localScale = s;
            bb = MB(win);
            float tm = (t0 + t1) / 2f;
            var target = alongX ? new Vector3(c, floorY + 0.9f, tm) : new Vector3(tm, floorY + 0.9f, c);
            win.transform.position += new Vector3(target.x - bb.center.x, target.y - bb.min.y, target.z - bb.center.z);
            var r = win.GetComponent<Renderer>();
            var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WindowGlass.mat");
            var mats = r.sharedMaterials;
            for (int k = 0; k < mats.Length; k++) if (mats[k] != null && mats[k].name == "Glass") mats[k] = glass;
            r.sharedMaterials = mats;
            win.AddComponent<BoxCollider>();
        }

        /// <summary>
        /// Door leaf on a hinge at the low edge of a gap (or the high edge when highEnd, as the second half
        /// of a double door). alongX: the wall runs along x at 'at'.
        /// </summary>
        private static Door Door(string name, bool alongX, float at, float c, float w, float floorY, bool locked = false,
            bool highEnd = false, Transform parent = null)
        {
            var hinge = new GameObject(name);
            hinge.transform.SetParent(parent != null ? parent : Group("Doors"), false);
            float edge = highEnd ? c + w / 2f : c - w / 2f;
            var pos = alongX ? new Vector3(edge, floorY, at) : new Vector3(at, floorY, edge);
            float yaw = (alongX ? 0f : -90f) + (highEnd ? 180f : 0f);
            hinge.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(doorAsset);
            leaf.name = "Leaf";
            leaf.transform.SetParent(hinge.transform, false);
            const float gap = 0.01f;
            var sc = new Vector3((w - gap * 2f) / doorMesh.size.x, 0.05f / doorMesh.size.y, 2.1f / doorMesh.size.z);
            leaf.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);
            leaf.transform.localScale = sc;
            leaf.transform.localPosition = new Vector3(gap - doorMesh.min.x * sc.x, -doorMesh.min.z * sc.z, doorMesh.center.y * sc.y);
            leaf.AddComponent<BoxCollider>();
            var d = hinge.AddComponent<Door>();
            if (locked)
            {
                var so = new SerializedObject(d);
                so.FindProperty("locked").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return d;
        }

        /// <summary>Two linked leaves filling an opening of width w, like Loop 1's double doors.</summary>
        private static void DoubleDoor(string name, bool alongX, float at, float c, float w, float floorY)
        {
            var group = Group("Doors/" + name);
            var a = Door("Hinge_A", alongX, at, c - w / 4f, w / 2f, floorY, parent: group);
            var b = Door("Hinge_B", alongX, at, c + w / 4f, w / 2f, floorY, highEnd: true, parent: group);
            foreach (var (self, other) in new[] { (a, b), (b, a) })
            {
                var so = new SerializedObject(self);
                var linked = so.FindProperty("linkedDoors");
                linked.arraySize = 1;
                linked.GetArrayElementAtIndex(0).objectReferenceValue = other;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject P(string room, string name, string path, float f, float yaw, float x, float z, float y,
            char wallSide = ' ', float face = 0f, bool collider = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + path);
            if (asset == null) { Debug.LogWarning("Loop2 builder: missing " + path); return null; }
            if (y == 0f) y = GroundTop;   // the ground floor slab's top surface
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, Group("Furniture/" + room));
            go.name = name;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.rotation;
            go.transform.localScale = asset.transform.localScale * f;
            go.transform.position = Vector3.zero;
            var b = MB(go);
            if (wallSide == 'W') x = face + b.size.x * 0.5f + 0.005f;
            if (wallSide == 'E') x = face - b.size.x * 0.5f - 0.005f;
            if (wallSide == 'S') z = face + b.size.z * 0.5f + 0.005f;
            if (wallSide == 'N') z = face - b.size.z * 0.5f - 0.005f;
            go.transform.position += new Vector3(x - b.center.x, y - b.min.y, z - b.center.z);
            if (collider && go.GetComponent<MeshFilter>() != null) go.AddComponent<BoxCollider>();
            return go;
        }

        private static float Top(GameObject go) => MB(go).max.y;

        private static void ScaleHeight(GameObject go, float height, float floorY)
        {
            var b = MB(go);
            var s = go.transform.localScale;
            s.y *= height / b.size.y;          // prefab roots here are unrotated, so local y is up
            go.transform.localScale = s;
            b = MB(go);
            go.transform.position += new Vector3(0f, floorY - b.min.y, 0f);
        }

        private static Light Lamp(GameObject host, Vector3 lightPos, float intensity, float range, bool on, bool shadows = false)
        {
            var lg = new GameObject("Lamp Light");
            lg.transform.SetParent(host.transform, true);
            lg.transform.position = lightPos;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.76f, 0.5f);
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Hard : LightShadows.None;
            if (host.GetComponent<Collider>() == null)
            {
                var bc = host.AddComponent<BoxCollider>();
                var mf = host.GetComponent<MeshFilter>();
                if (mf != null) { bc.center = mf.sharedMesh.bounds.center; bc.size = mf.sharedMesh.bounds.size * 1.2f; }
            }
            var sw = host.AddComponent<LampSwitch>();
            var so = new SerializedObject(sw);
            var lp = so.FindProperty("lights");
            lp.arraySize = 1;
            lp.GetArrayElementAtIndex(0).objectReferenceValue = l;
            so.FindProperty("isOn").boolValue = on;
            so.ApplyModifiedPropertiesWithoutUndo();
            l.enabled = on;
            return l;
        }

        private static void CeilingLamp(string room, float x, float z, float ceilY, float intensity, float range, bool on)
        {
            var g = P(room, "CeilingLamp", "Furniture/Light_CeilingSingle.fbx", 0.7f, 0f, x, z, 0f, ' ', 0f, false);
            var b = MB(g);
            g.transform.position += new Vector3(0f, ceilY - b.max.y, 0f);
            Lamp(g, new Vector3(x, MB(g).min.y - 0.05f, z), intensity, range, on);
        }

        private static void FloorLamp(string room, float x, float z, bool on, bool shadows = false)
        {
            var g = P(room, "FloorLamp", "Furniture/Light_Stand2.fbx", 0.75f, 0f, x, z, 0f, ' ', 0f, false);
            Lamp(g, new Vector3(x, Top(g) - 0.15f, z), 1.6f, 6f, on, shadows);
        }

        private static void Note(float x, float y, float z, char facing, string name = "ReminderNote")
        {
            bool alongX = facing == 'N' || facing == 'S';
            float d = 0.006f;
            Box("Furniture/Notes", name,
                alongX ? x - 0.04f : x - d, alongX ? x + 0.04f : x + d, y - 0.04f, y + 0.04f,
                alongX ? z - d : z - 0.04f, alongX ? z + d : z + 0.04f, note, null, 0f, false);
        }

        private static void WallPhoto(string room, float x, float bottom, float z, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Picture_Frames_FREE/Prefabs/SM_frame_02.prefab");
            var wallMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/SM_frame_02_Wall.asset");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, Group("Furniture/" + room));
            go.name = "Photo";
            const float s = 3.5f;
            go.transform.localScale = Vector3.one * s;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(10f, 0f, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = wallMesh;
            // Flat on the wall: no collider, so it never snags the player in the narrow ring hall.
            var mc = go.GetComponent<MeshCollider>();
            if (mc != null) Object.DestroyImmediate(mc);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            go.transform.position = new Vector3(x, bottom, z) + fwd * (0.002f - 0.0134f * s);
        }

        private static void Boxes(string room, float x, float z, int stack, float y = 0f)
        {
            if (y == 0f) y = GroundTop;
            for (int i = 0; i < stack; i++)
            {
                float w = 0.6f - i * 0.06f, d = 0.5f - i * 0.05f;
                Box("Furniture/" + room, "Box", x - w / 2, x + w / 2, y + i * 0.42f, y + (i + 1) * 0.42f, z - d / 2, z + d / 2, cardboard,
                    null, 0f, i == 0);
            }
        }

        // ------------------------------------------------------------------ shell

        private static void BuildShell()
        {
            // Ground floor outer walls (0.7 m), 0..2.6, with Loop 1's windows and front door.
            Wall("Shell", "North_G", true, 9.63f, 10.33f, -10.30f, 9.98f, 0f, Up, Win(-7.9f), Win(-2.3f), (3.85f, 1.2f, 1.1f, 2.2f));
            Wall("Shell", "South_G", true, -10.34f, -9.64f, -10.30f, 9.98f, 0f, Up, Win(-5.2f), Win(6.0f));
            Wall("Shell", "West_G", false, -10.30f, -9.60f, -10.34f, 10.33f, 0f, Up, Win(-6.2f), Win(-1.85f));
            Wall("Shell", "East_G", false, 9.28f, 9.98f, -10.34f, 10.33f, 0f, Up, (4.4f, 0.8f, 1.0f, 2.2f), (-4.17f, 1.62f, 0f, Up));
            foreach (var x in new[] { -7.9f, -2.3f }) Window("Window_Large1", true, 9.63f, 10.33f, x, 1.2f, 0f);
            Window("Window_Large1", true, 9.63f, 10.33f, 3.85f, 1.2f, 0.2f);
            foreach (var x in new[] { -5.2f, 6.0f }) Window("Window_Large1", true, -10.34f, -9.64f, x, 1.2f, 0f);
            foreach (var z in new[] { -6.2f, -1.85f }) Window("Window_Large1", false, -10.30f, -9.60f, z, 1.2f, 0f);
            Window("Window_Small1", false, 9.28f, 9.98f, 4.4f, 0.8f, 0.1f);

            // Upper floor outer walls, 2.6..5.15. The Loop Door sits at the narrow end of the landing.
            Wall("Shell", "North_U", true, 9.63f, 10.33f, -10.30f, 9.98f, Up, Roof, Win(-7.9f), Win(-2.3f), Win(3.85f));
            Wall("Shell", "South_U", true, -10.34f, -9.64f, -10.30f, 9.98f, Up, Roof, Win(3.0f));
            Wall("Shell", "West_U", false, -10.30f, -9.60f, -10.34f, 10.33f, Up, Roof, Win(-1.85f), Win(7.0f), Gap(0.575f, 0.9f));
            Wall("Shell", "East_U", false, 9.28f, 9.98f, -10.34f, 10.33f, Up, Roof, Win(-3.0f, 0.8f), Win(4.4f, 0.8f));
            foreach (var x in new[] { -7.9f, -2.3f, 3.85f }) Window("Window_Large1", true, 9.63f, 10.33f, x, 1.2f, Up);
            Window("Window_Large1", true, -10.34f, -9.64f, 3.0f, 1.2f, Up);
            foreach (var z in new[] { -1.85f, 7.0f }) Window("Window_Large1", false, -10.30f, -9.60f, z, 1.2f, Up);
            foreach (var z in new[] { -3.0f, 4.4f }) Window("Window_Small1", false, 9.28f, 9.98f, z, 0.8f, Up);

            // Loop Door: a plain leaf in the west wall, leading to Loop 3.
            var ld = (GameObject)PrefabUtility.InstantiatePrefab(doorAsset, Group("Doors"));
            ld.name = "LoopDoor";
            ld.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(270f, 0f, 0f);
            // Fills the 0.9 m opening with a 1 cm gap each side, like Loop 1's Loop Door in its 1.0 m opening.
            ld.transform.localScale = new Vector3(0.88f / doorMesh.size.x, 0.06f / doorMesh.size.y, 2.1f / doorMesh.size.z);
            ld.transform.position = Vector3.zero;
            var lb = MB(ld);
            ld.transform.position += new Vector3(-9.95f - lb.center.x, Up - lb.min.y, 0.575f - lb.center.z);
            ld.AddComponent<BoxCollider>();
            var loopDoor = ld.AddComponent<LoopDoor>();
            var so = new SerializedObject(loopDoor);
            so.FindProperty("promptText").stringValue = "Open the door";
            so.FindProperty("nextScene").stringValue = "Loop3_TheAftermath";
            so.FindProperty("nextLoop").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Roof slab and a shadow-only block above it so sunlight only enters through the windows.
            Box("Shell", "Roof", -10.3f, 9.98f, UpCeil, Roof, -10.34f, 10.33f, ceiling, WorldTiling.Mode.Floor);
            var blocker = Box("Shell", "SunBlocker_ShadowsOnly", -10.5f, 10.5f, 5.6f, 7.2f, -10.5f, 10.5f, ceiling, null, 0f, false);
            blocker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        // ------------------------------------------------------------------ interior walls

        private static (float, float, float, float) Opening(float c, float w) => (c, w, 0f, 2.15f);

        private static void BuildGroundWalls()
        {
            // The same wall lines as Loop 1.
            HWall(0.0f, -9.60f, 9.28f, 0f, Ceil, Opening(-3.15f, 3.48f), Opening(3.0f, 0.9f));          // dining | living, kitchen | pantry and coats
            HWall(-7.27f, -9.60f, 6.42f, 0f, Ceil, Opening(-3.385f, 3.0f), Opening(3.6f, 0.9f));        // living and utility | south corridor
            VWall(-0.14f, -7.27f, 0f, 0f, Ceil);                                               // living | pantry and utility
            VWall(-0.14f, 0f, 9.63f, 0f, Ceil, Opening(3.12f, 1.94f));                         // dining | kitchen
            VWall(6.42f, -7.27f, 0f, 0f, Ceil);                                                // pantry and utility | coats and vestibule
            VWall(6.42f, 4.68f, 9.63f, 0f, Ceil);                                                         // kitchen | stairs
            HWall(-2.29f, 6.42f, 9.28f, 0f, Ceil, Opening(8.6f, 0.9f));                                  // vestibule | coat cupboard
            HWall(3.26f, 6.42f, 9.28f, 0f, Ceil);                                              // kitchen alcove | back hall
            HWall(-3.6f, -0.14f, 6.42f, 0f, Ceil);                                             // pantry | utility (Room B in Loop 1)

            // Loop 1's doors on the main route: double doors in the wide openings, one door into the back hall.
            DoubleDoor("Doorway_Corridor_Living", true, -7.27f, -3.385f, 3.0f, 0.05f);
            DoubleDoor("Doorway_Living_Dining", true, 0.0f, -3.15f, 3.48f, 0.05f);
            DoubleDoor("Doorway_Dining_Kitchen", false, -0.14f, 3.12f, 1.94f, 0.05f);
            Box("Walls", "BackHall_Lintel", 6.37f, 6.47f, 2.15f, Ceil, 3.31f, 4.68f, wall, WorldTiling.Mode.Wall);
            // Its hinge sits on the face of the back hall's south wall, so near 90 degrees the leaf would lie
            // inside that wall. Stop at 80 degrees.
            var backHall = Door("Door_BackHall", false, 6.42f, 3.995f, 1.37f, 0.05f);
            var bso = new SerializedObject(backHall);
            bso.FindProperty("openAngle").floatValue = 80f;
            bso.ApplyModifiedPropertiesWithoutUndo();

            Door("Door_Pantry", true, 0.0f, 3.0f, 0.9f, GroundTop);
            Door("Door_Utility", true, -7.27f, 3.6f, 0.9f, GroundTop);
            Door("Door_Coats", true, -2.29f, 8.6f, 0.9f, GroundTop);
        }

        private static void BuildUpperWalls()
        {
            float y0 = Up, y1 = UpCeil;
            // Landing: the south wall is straight; the north wall runs straight to x -3 and then slants in.
            HWall(0.05f, -9.60f, 9.28f, y0, y1, Gap(-3.0f), Gap(3.0f), Gap(7.4f, 1.0f));
            HWall(2.15f, -3.0f, 6.42f, y0, y1, Gap(-1.8f), Gap(2.5f));
            VWall(-0.14f, 2.15f, 9.63f, y0, y1);                     // master | study
            VWall(6.42f, 2.15f, 9.63f, y0, y1);                      // study | stairwell
            VWall(-0.14f, -4.65f, 0.05f, y0, y1);                    // children | bathroom
            HWall(-4.65f, -9.60f, 6.42f, y0, y1);                    // children and bathroom | south strip
            VWall(6.42f, -9.64f, 0.05f, y0, y1, Gap(-8.0f));         // back landing | sewing room
            VWall(-0.14f, -9.64f, -4.65f, y0, y1);                   // sealed strip | sewing room

            // From 2.1 m wide at x -3 down to 1.0 m at the Loop Door.
            var a = new Vector2(-9.60f, 1.10f);
            var b = new Vector2(-3.0f, 2.10f);
            var d = b - a;
            var slant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slant.name = "Landing_SlantedWall";
            slant.transform.SetParent(Group("Walls"), false);
            slant.transform.position = new Vector3((a.x + b.x) / 2f, (y0 + y1) / 2f, (a.y + b.y) / 2f + T / 2f);
            slant.transform.rotation = Quaternion.Euler(0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f);
            slant.transform.localScale = new Vector3(d.magnitude + 0.06f, y1 - y0, T);
            slant.GetComponent<Renderer>().sharedMaterial = wall;
            Tile(slant, WorldTiling.Mode.Wall, 2.5f);

            Door("Door_MasterBedroom", true, 2.15f, -1.8f, 0.9f, Up);
            Door("Door_ChildrensRoom", true, 0.05f, -3.0f, 0.9f, Up);
            Door("Door_Bathroom", true, 0.05f, 3.0f, 0.9f, Up);
            Door("Door_BackLanding", true, 0.05f, 7.4f, 1.0f, Up);
            Door("Door_SewingRoom", false, 6.42f, -8.0f, 0.9f, Up);
            // The study: locked, and behind it a wall of fresh brick.
            Door("Door_Study_Locked", true, 2.15f, 2.5f, 0.9f, Up, locked: true);
            Box("Walls", "Bricks_Study", 1.95f, 3.05f, Up, Up + 2.12f, 2.22f, 2.44f, brick);
        }

        private static void BuildFloorsAndStairs()
        {
            // Ground floor ceiling and upper floor, with Loop 1's stair hole x 6.47..9.28, z 5.63..9.63.
            var pieces = new (string n, float x0, float x1, float z0, float z1)[]
            {
                ("West", -10f, 6.47f, -10f, 10f), ("EastSouth", 6.47f, 10f, -10f, 5.63f), ("EastNorth", 6.47f, 10f, 9.63f, 10f),
            };
            foreach (var p in pieces)
            {
                Box("Floors", "Ceiling_" + p.n, p.x0, p.x1, Ceil, Ceil + 0.02f, p.z0, p.z1, ceiling, WorldTiling.Mode.Floor);
                Box("Floors", "UpperFloor_" + p.n, p.x0, p.x1, Ceil + 0.02f, Up, p.z0, p.z1, floor, WorldTiling.Mode.Floor, 4f);
            }

            // Loop 1's switchback: flight 1 rises north on the west side, a half landing, flight 2 rises south.
            const int n = 12;
            const float zStart = 5.63f, zLand = 8.98f, half = Up / 2f;
            float depth = (zLand - zStart) / n;
            for (int i = 1; i <= n; i++)
            {
                float top = i * half / (n + 1);
                Box("Stairs", $"Flight1_Step_{i:00}", 6.47f, 7.82f, 0f, top, zStart + (i - 1) * depth, zStart + i * depth, floor, WorldTiling.Mode.Floor, 4f);
            }
            Box("Stairs", "HalfLanding", 6.47f, 9.28f, 0f, half, zLand, 9.63f, floor, WorldTiling.Mode.Floor, 4f);
            for (int i = 1; i <= n; i++)
            {
                float top = half + i * half / (n + 1);
                Box("Stairs", $"Flight2_Step_{i:00}", 7.92f, 9.28f, 0f, top, zLand - i * depth, zLand - (i - 1) * depth, floor, WorldTiling.Mode.Floor, 4f);
            }
            Box("Stairs", "StairSpine", 7.82f, 7.92f, 0f, Up + 1.0f, zStart, 8.6f, wall, WorldTiling.Mode.Wall);
            // Railing on the upper floor over the drop to flight 1.
            Box("Stairs", "Railing_Stairwell", 6.47f, 7.82f, Up, Up + 1.0f, 5.58f, 5.66f, floor);
        }

        /// <summary>
        /// Low furniture (coffee tables, TV tables, stools) can be stepped onto by the character
        /// controller. Replace each low collider with a straight-sided box at least 1 m tall.
        /// </summary>
        private static void BlockLowFurniture()
        {
            var furniture = Group("Furniture");
            var blockers = Group("Furniture/StepBlockers");
            foreach (var col in furniture.GetComponentsInChildren<Collider>())
            {
                if (col.transform.IsChildOf(blockers)) continue;
                if (col.GetComponentInParent<Interactable>() != null) continue;   // lamp switches keep their own collider
                var b = MB(col.gameObject);
                float floorY = b.min.y > Up - 0.2f ? Up : GroundTop;
                if (b.min.y - floorY > 0.15f) continue;                         // only things standing on the floor
                if (b.max.y - floorY >= 1.0f || b.size.x < 0.05f || b.size.z < 0.05f) continue;
                var go = new GameObject("StepBlocker_" + col.gameObject.name);
                go.layer = 2;      // Ignore Raycast: it stops feet, not the interaction ray (the fuse sits inside one)
                go.transform.SetParent(blockers, false);
                go.transform.position = new Vector3(b.center.x, floorY + 0.5f, b.center.z);
                go.AddComponent<BoxCollider>().size = new Vector3(b.size.x, 1.0f, b.size.z);
                Object.DestroyImmediate(col);
            }
        }

        // ------------------------------------------------------------------ furniture

        private static void FurnishGround()
        {
            const float N = float.NaN;
            // Dining room: the table set for six. There were only ever four of them.
            var table = P("Dining", "Table", "Furniture/Table_RoundLarge.fbx", 0.68f, 0f, -5.0f, 5.0f, 0f);
            var tb = MB(table);
            float top = tb.max.y, hx = tb.extents.x, hz = tb.extents.z;
            var seats = new (float yaw, Vector3 dir, float along)[]
            {
                (270f, Vector3.right, 0f), (90f, Vector3.left, 0f),
                (0f, Vector3.back, -0.6f), (0f, Vector3.back, 0.6f),
                (180f, Vector3.forward, -0.6f), (180f, Vector3.forward, 0.6f),
            };
            int si = 0;
            foreach (var s in seats)
            {
                float halfW = s.dir.x != 0 ? hx : hz;
                var along = new Vector3(s.along, 0f, 0f);
                var chair = tb.center + s.dir * (halfW + 0.13f) + along;
                var plate = tb.center + s.dir * (halfW - (s.dir.x != 0 ? 0.30f : 0.22f)) + along;
                var right = new Vector3(Mathf.Cos(s.yaw * Mathf.Deg2Rad), 0f, -Mathf.Sin(s.yaw * Mathf.Deg2Rad));
                P("Dining", "Chair_" + si, "Furniture/Chair_2.fbx", 0.54f, s.yaw, chair.x, chair.z, 0f);
                // The plate's dish sits below its lowest vertex line, so lift it clear of the table top.
                P("Dining", "Plate_" + si, "Furniture/Plate_1.fbx", 0.73f, 0f, plate.x, plate.z, top + 0.045f, ' ', 0f, false);
                var fp = plate - right * 0.2f;
                var kp = plate + right * 0.2f;
                var sp = plate + right * 0.26f;
                P("Dining", "Fork_" + si, "Furniture/Fork.fbx", 0.48f, s.yaw + 180f, fp.x, fp.z, top, ' ', 0f, false);
                P("Dining", "Knife_" + si, "Furniture/Knife.fbx", 0.48f, s.yaw + 180f, kp.x, kp.z, top, ' ', 0f, false);
                P("Dining", "Spoon_" + si, "Furniture/Spoon.fbx", 0.48f, s.yaw + 180f, sp.x, sp.z, top, ' ', 0f, false);
                si++;
            }
            // The sideboard holds the spare fuse; the family photos stand on it.
            var sideboard = P("Dining", "Sideboard", "Furniture/Drawer_5.fbx", 0.55f, 90f, N, 5.0f, 0f, 'W', -9.60f);
            P("Dining", "FamilyPhoto", "Picture_Frames_FREE/Prefabs/SM_frame_01.prefab", 1f, 90f, -9.2f, 4.6f, Top(sideboard), ' ', 0f, false);
            P("Dining", "FamilyPhoto2", "Picture_Frames_FREE/Prefabs/SM_frame_01.prefab", 1f, 90f, -9.2f, 5.5f, Top(sideboard), ' ', 0f, false);
            P("Dining", "Commode", "NextGen Furniture Pack/Prefabs/commode_1.prefab", 1f, 180f, -5.0f, N, 0f, 'N', 9.63f);
            P("Dining", "Bookshelf", "Furniture/Bookshelf.fbx", 0.5f, 90f, N, 8.3f, 0f, 'W', -9.60f);
            P("Dining", "Rug", "Furniture/Carpet_2.fbx", 1f, 90f, -5.0f, 5.0f, 0f, ' ', 0f, false);
            P("Dining", "Plant", "Furniture/Houseplant_8.fbx", 0.6f, 0f, -1.0f, 1.0f, 0f, ' ', 0f, false);
            FloorLamp("Dining", -0.9f, 9.0f, true, true);
            CeilingLamp("Dining", -5.0f, 5.0f, Ceil, 1.2f, 7f, true);

            // Kitchen: counters along the north wall, every cupboard labelled.
            float cx = 0.0f;
            int unit = 0;
            // Units are about 0.57 m wide, so eleven of them run from the dining wall to the stairs wall (x 0..6.37).
            string[] run = { "Kitchen_3Drawers", "Kitchen_Sink", "Kitchen_3Drawers", "Kitchen_3Drawers", "Kitchen_Oven_Large", "Kitchen_3Drawers",
                             "Kitchen_3Drawers", "Kitchen_3Drawers", "Kitchen_3Drawers", "Kitchen_3Drawers", "Kitchen_3Drawers", "Kitchen_3Drawers" };
            foreach (var m in run)
            {
                float f = m == "Kitchen_Oven_Large" ? 0.566f : 0.56f;
                var c = P("Kitchen", m, $"Furniture/{m}.fbx", f, 180f, cx, N, 0f, 'N', 9.63f);
                var cb = MB(c);
                if (cx + cb.size.x > 6.40f) { Object.DestroyImmediate(c); break; }
                c.transform.position += new Vector3(cx - cb.min.x, 0f, 0f);
                cb = MB(c);
                cx = cb.max.x + 0.005f;
                Note(cb.center.x, 0.62f, cb.min.z - 0.008f, 'S', $"ReminderNote_Counter{unit++}");
            }
            // The fridge with Walter's note: "RUTH TOOK THE KIDS TO HER MOTHER'S. DON'T CALL. - W."
            // In the middle of the south wall, as in every loop (clear of the pantry door at x 2.55..3.45).
            P("Kitchen", "Fridge", "Furniture/Kitchen_Fridge.fbx", 0.54f, 0f, 4.6f, N, 0f, 'S', 0.05f);
            Note(4.5f, 1.3f, 0.75f, 'N', "ReminderNote_Fridge");
            P("Kitchen", "Oven", "Furniture/Kitchen_Oven_Large.fbx", 0.566f, 270f, N, 1.6f, 0f, 'E', 9.28f);
            P("Kitchen", "Counter_Alcove", "Furniture/Kitchen_3Drawers.fbx", 0.56f, 270f, N, 2.55f, 0f, 'E', 9.28f);
            var kt = P("Kitchen", "Table", "Furniture/Table_RoundSmall2.fbx", 0.62f, 0f, 3.0f, 5.4f, 0f);
            P("Kitchen", "Chair_W", "NextGen Furniture Pack/Prefabs/chair_1.prefab", 1f, 90f, 2.15f, 5.4f, 0f);
            P("Kitchen", "Chair_E", "NextGen Furniture Pack/Prefabs/chair_1.prefab", 1f, 270f, 3.85f, 5.4f, 0f);
            P("Kitchen", "Mug", "kitchen props/models/mug.fbx", 1f, 30f, 2.9f, 5.3f, Top(kt), ' ', 0f, false);
            Note(3.15f, Top(kt) + 0.004f, 5.55f, 'N', "ReminderNote_Table");
            CeilingLamp("Kitchen", 3.0f, 5.4f, Ceil, 1.2f, 6.5f, true);
            CeilingLamp("BackHall", 7.85f, 4.1f, Ceil, 0.9f, 5f, true);

            // Pantry (Room B in Loop 1): shelves of labelled jars, the overflow of his notes.
            P("Pantry", "Shelf_W", "Furniture/Shelf_1.fbx", 0.5f, 90f, N, -1.8f, 0f, 'W', -0.09f);
            P("Pantry", "Shelf_E", "Furniture/Shelf_1.fbx", 0.5f, 270f, N, -1.8f, 0f, 'E', 6.37f);
            Boxes("Pantry", 1.4f, -3.2f, 2);
            Boxes("Pantry", 4.8f, -3.2f, 3);
            for (int i = 0; i < 6; i++) Note(0.42f, 0.5f + i * 0.28f, -1.2f - i * 0.25f, 'E');
            for (int i = 0; i < 6; i++) Note(5.86f, 0.5f + i * 0.28f, -1.3f - i * 0.22f, 'W');
            CeilingLamp("Pantry", 3.1f, -1.8f, Ceil, 1.0f, 5.5f, true);

            // Utility (Room B in Loop 1): washer, shelf and the fuse box. The power upstairs is out.
            P("Utility", "Washer", "Furniture/Bathroom_WashingMachine.fbx", 0.58f, 0f, 1.0f, N, 0f, 'S', -7.22f);
            P("Utility", "Shelf", "Furniture/Shelf_1.fbx", 0.5f, 90f, N, -5.3f, 0f, 'W', -0.09f);
            Box("Furniture/Utility", "FuseBox", 6.23f, 6.37f, 1.3f, 1.95f, -5.8f, -5.2f, metal);
            Note(6.22f, 1.75f, -5.5f, 'W', "ReminderNote_FuseBox");
            Boxes("Utility", 5.6f, -4.1f, 2);
            CeilingLamp("Utility", 3.1f, -5.4f, Ceil, 0.9f, 5.5f, true);

            // Vestibule and the coat cupboard.
            var hall = P("Vestibule", "HallCabinet", "NextGen Furniture Pack/Prefabs/commode_2.prefab", 1f, 180f, 7.15f, N, 0f, 'N', -2.34f);
            P("Vestibule", "Mirror", "Furniture/Bathroom_Mirror1.fbx", 0.8f, 180f, 7.15f, N, Top(hall) + 0.2f, 'N', -2.34f, false);
            Note(7.5f, Top(hall) + 0.5f, -2.36f, 'S', "ReminderNote_Hall");
            P("Vestibule", "Rug", "Furniture/Carpet_1.fbx", 1f, 0f, 7.85f, -4.6f, 0f, ' ', 0f, false);
            CeilingLamp("Vestibule", 7.85f, -4.6f, Ceil, 1.0f, 5.5f, true);
            P("Coats", "Shelf", "Furniture/Drawer_4.fbx", 0.5f, 180f, 7.3f, N, 0f, 'N', -0.05f);
            Boxes("Coats", 6.9f, -1.8f, 2);

            // Living room: the whole suite is still here, but he only uses one armchair, pulled up to the TV.
            P("LivingRoom", "Fireplace", "Furniture/Fireplace.fbx", 0.51f, 90f, N, -3.6f, 0f, 'W', -9.60f);
            P("LivingRoom", "Couch", "Furniture/Couch_Large2.fbx", 0.4f, 270f, -6.0f, -3.6f, 0f);
            P("LivingRoom", "Couch2", "Furniture/Couch_Large2.fbx", 0.4f, 0f, -7.6f, N, 0f, 'S', -7.22f);
            P("LivingRoom", "CoffeeTable", "Furniture/Table_RoundSmall.fbx", 0.39f, 0f, -7.6f, -3.6f, 0f);
            var tvTable = P("LivingRoom", "TVTable", "NextGen Furniture Pack/Prefabs/table_4.prefab", 0.5f, 270f, N, -2.6f, 0f, 'E', -0.19f);
            float tvBase = Top(tvTable);
            Box("Furniture/LivingRoom", "TV", -0.55f, -0.25f, tvBase, tvBase + 0.62f, -3.1f, -2.1f, metal);
            Box("Furniture/LivingRoom", "TVScreen", -0.565f, -0.55f, tvBase + 0.06f, tvBase + 0.56f, -3.02f, -2.18f, screen, null, 0f, false);
            P("LivingRoom", "Armchair", "NextGen Furniture Pack/Prefabs/chair_5.prefab", 1f, 90f, -2.6f, -2.6f, 0f);
            P("LivingRoom", "SideTable", "NextGen Furniture Pack/Prefabs/table_1.prefab", 1f, 0f, -2.6f, -3.6f, 0f);
            P("LivingRoom", "Rug", "Furniture/Carpet_2.fbx", 1f, 90f, -6.8f, -3.6f, 0f, ' ', 0f, false);
            FloorLamp("LivingRoom", -9.15f, -6.75f, true, true);

            // South corridor: the same family photograph, five times along its north wall.
            foreach (var x in new[] { -8.0f, -6.0f, -0.9f, 1.3f, 5.2f })
                WallPhoto("Corridor", x, 1.25f, -7.32f, 180f);
            var console = P("Corridor", "ConsoleTable", "NextGen Furniture Pack/Prefabs/table_2.prefab", 0.8f, 0f, N, -8.45f, 0f, 'W', -9.60f);
            P("Corridor", "Plant", "Furniture/Houseplant_5.fbx", 0.6f, 0f, -9.15f, -9.2f, Top(console), ' ', 0f, false);
            foreach (var lx in new[] { -6.5f, -0.5f, 5.0f })
                CeilingLamp("Corridor", lx, -8.45f, Ceil, 1.0f, 6f, true);
        }

        private static void FurnishUpper()
        {
            const float N = float.NaN;
            // Master bedroom: one side of the bed slept in.
            P("MasterBedroom", "Bed", "Furniture/Bed_King.fbx", 0.56f, 180f, -5.0f, N, Up, 'N', 9.63f);
            var ns = P("MasterBedroom", "Nightstand_L", "Furniture/NightStand_1.fbx", 0.5f, 180f, -6.35f, N, Up, 'N', 9.63f);
            P("MasterBedroom", "Nightstand_R", "Furniture/NightStand_1.fbx", 0.5f, 180f, -3.65f, N, Up, 'N', 9.63f);
            var lamp = P("MasterBedroom", "BedsideLamp", "Furniture/Light_Desk.fbx", 0.6f, 180f, -6.35f, 9.35f, Top(ns), ' ', 0f, false);
            Lamp(lamp, MB(lamp).center + new Vector3(0f, 0.08f, -0.08f), 1.2f, 4.5f, true);
            P("MasterBedroom", "Wardrobe", "Furniture/Drawer_4.fbx", 0.75f, 90f, N, 5.5f, Up, 'W', -9.60f);
            P("MasterBedroom", "Dresser", "Furniture/Drawer_5.fbx", 0.5f, 270f, N, 5.5f, Up, 'E', -0.19f);
            P("MasterBedroom", "Rug", "Furniture/Carpet_2.fbx", 0.8f, 0f, -5.0f, 6.2f, Up, ' ', 0f, false);
            CeilingLamp("MasterBedroom", -5.0f, 5.6f, UpCeil, 1.1f, 7f, true);

            // Children's room: kept ready, beds made, toys out.
            P("ChildrensRoom", "Bed_A", "Furniture/Bed_Single.fbx", 0.5f, 0f, -7.7f, N, Up, 'S', -4.60f);
            P("ChildrensRoom", "Bed_B", "Furniture/Bed_Single.fbx", 0.5f, 0f, -5.5f, N, Up, 'S', -4.60f);
            P("ChildrensRoom", "Nightstand", "Furniture/NightStand_2.fbx", 0.5f, 0f, -6.6f, N, Up, 'S', -4.60f);
            P("ChildrensRoom", "ToyChest", "Furniture/Drawer_1.fbx", 0.35f, 90f, N, -1.5f, Up, 'W', -9.60f);
            var desk = P("ChildrensRoom", "Desk", "NextGen Furniture Pack/Prefabs/table_4.prefab", 0.5f, 0f, -2.0f, N, Up, 'S', -4.60f);
            ScaleHeight(desk, 0.66f, Up);
            P("ChildrensRoom", "DeskChair", "NextGen Furniture Pack/Prefabs/chair_4.prefab", 1f, 180f, -2.0f, -3.4f, Up);
            P("ChildrensRoom", "Shelf", "Furniture/Shelf_1.fbx", 0.5f, 270f, N, -2.6f, Up, 'E', -0.19f);
            P("ChildrensRoom", "Rug", "Furniture/Carpet_Round.fbx", 1f, 0f, -5.5f, -1.6f, Up, ' ', 0f, false);
            CeilingLamp("ChildrensRoom", -4.9f, -2.3f, UpCeil, 1.1f, 7f, true);

            // Bathroom.
            P("Bathroom", "Bathtub", "Furniture/Bathroom_Bathtub.fbx", 0.45f, 0f, N, -3.3f, Up, 'W', -0.09f);
            P("Bathroom", "Toilet", "Furniture/Bathroom_Toilet.fbx", 0.42f, 0f, 5.2f, N, Up, 'S', -4.60f);
            P("Bathroom", "Sink", "Furniture/Bathroom_Sink.fbx", 0.6f, 270f, N, -2.2f, Up, 'E', 6.37f);
            P("Bathroom", "Mirror", "Furniture/Bathroom_Mirror1.fbx", 0.8f, 270f, N, -2.2f, Up + 1.15f, 'E', 6.37f, false);
            CeilingLamp("Bathroom", 3.1f, -2.3f, UpCeil, 1.0f, 5.5f, true);

            // Ruth's sewing room (sealed in Loop 1): untouched. The study key is in her sewing box.
            var st = P("SewingRoom", "SewingTable", "NextGen Furniture Pack/Prefabs/table_1.prefab", 1f, 90f, N, -6.0f, Up, 'W', -0.09f);
            P("SewingRoom", "Chair", "NextGen Furniture Pack/Prefabs/chair_1.prefab", 1f, 270f, 1.4f, -6.0f, Up);
            var sb = MB(st);
            Box("Furniture/SewingRoom", "SewingBox", sb.center.x - 0.15f, sb.center.x + 0.15f, sb.max.y, sb.max.y + 0.12f,
                sb.center.z - 0.5f, sb.center.z - 0.28f, cardboard, null, 0f, false);
            P("SewingRoom", "Shelf", "Furniture/Shelf_1.fbx", 0.5f, 180f, 3.6f, N, Up, 'N', -4.70f);
            P("SewingRoom", "Stool", "Furniture/Stool.fbx", 0.5f, 0f, 4.3f, -8.6f, Up);
            Boxes("SewingRoom", 5.7f, -9.1f, 2, Up);
            P("SewingRoom", "Rug", "Furniture/Carpet_1.fbx", 1f, 0f, 3.1f, -7.2f, Up, ' ', 0f, false);
            CeilingLamp("SewingRoom", 3.1f, -7.2f, UpCeil, 1.1f, 7f, true);

            // Back landing (the attic stairs hall in Loop 1): storage and a closed attic hatch.
            P("BackLanding", "Shelf", "Furniture/Shelf_1.fbx", 0.5f, 270f, N, -3.0f, Up, 'E', 9.23f);
            Boxes("BackLanding", 8.7f, -5.2f, 3, Up);
            Boxes("BackLanding", 8.7f, -6.0f, 2, Up);
            Box("Furniture/BackLanding", "AtticHatch", 7.3f, 8.5f, UpCeil - 0.02f, UpCeil, -4.0f, -2.8f, metal, null, 0f, false);
            CeilingLamp("BackLanding", 7.85f, -6.5f, UpCeil, 0.9f, 5.5f, true);

            // Landing: the same photograph again, and lamps that are dim until the power is back.
            foreach (var x in new[] { -7.0f, -1.0f, 1.5f, 4.8f })
                WallPhoto("Landing", x, Up + 1.25f, 0.10f, 0f);
            CeilingLamp("Landing", -6.0f, 0.75f, UpCeil, 0.8f, 5f, true);
            CeilingLamp("Landing", 0.0f, 1.1f, UpCeil, 0.9f, 5.5f, true);
            CeilingLamp("Landing", 5.0f, 1.1f, UpCeil, 0.9f, 5.5f, true);
            CeilingLamp("Landing", 7.85f, 3.0f, UpCeil, 0.9f, 5.5f, true);
        }
    }
}
