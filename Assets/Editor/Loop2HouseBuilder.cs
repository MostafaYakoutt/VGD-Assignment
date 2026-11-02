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
    /// Blueprints/Left_Behind_Loop2_Ground_Blueprint.png and _Upper_Blueprint.png:
    /// the same 20 x 20 m shell and front door, with the interior rearranged
    /// by the protagonist's dementia (ring hall, shrunk living room, a second
    /// living room, a door onto bricks, central stairs, a narrowing corridor,
    /// a master bedroom reached only through the bathroom).
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

        /// <summary>Door leaf on a hinge at the low edge of a gap. alongX: the wall runs along x at 'at'.</summary>
        private static Door Door(string name, bool alongX, float at, float c, float w, float floorY, bool locked = false)
        {
            var hinge = new GameObject(name);
            hinge.transform.SetParent(Group("Doors"), false);
            var pos = alongX ? new Vector3(c - w / 2f, floorY, at) : new Vector3(at, floorY, c - w / 2f);
            hinge.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, alongX ? 0f : -90f, 0f));
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

        private static GameObject P(string room, string name, string path, float f, float yaw, float x, float z, float y,
            char wallSide = ' ', float face = 0f, bool collider = true)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + path);
            if (asset == null) { Debug.LogWarning("Loop2 builder: missing " + path); return null; }
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

        private static void Note(float x, float y, float z, char facing)
        {
            bool alongX = facing == 'N' || facing == 'S';
            float d = 0.006f;
            Box("Furniture/Notes", "ReminderNote",
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
            // Ground floor outer walls (0.7 m), 0..2.6, windows from the ground floor blueprint.
            Wall("Shell", "North_G", true, 9.63f, 10.33f, -10.30f, 9.98f, 0f, Up, Win(-6.5f), Win(-1.5f), Win(5.5f));
            Wall("Shell", "South_G", true, -10.34f, -9.64f, -10.30f, 9.98f, 0f, Up, Win(-6.0f), Win(1.0f));
            Wall("Shell", "West_G", false, -10.30f, -9.60f, -10.34f, 10.33f, 0f, Up, Win(2.5f), Win(-7.0f));
            Wall("Shell", "East_G", false, 9.28f, 9.98f, -10.34f, 10.33f, 0f, Up, Win(1.5f, 0.8f), (-4.17f, 1.62f, 0f, Up));
            foreach (var x in new[] { -6.5f, -1.5f, 5.5f }) Window("Window_Large1", true, 9.63f, 10.33f, x, 1.2f, 0f);
            foreach (var x in new[] { -6.0f, 1.0f }) Window("Window_Large1", true, -10.34f, -9.64f, x, 1.2f, 0f);
            foreach (var z in new[] { 2.5f, -7.0f }) Window("Window_Large1", false, -10.30f, -9.60f, z, 1.2f, 0f);
            Window("Window_Small1", false, 9.28f, 9.98f, 1.5f, 0.8f, 0f);

            // Upper floor outer walls, 2.6..5.15, with the Loop Door opening at the narrow end of the corridor.
            Wall("Shell", "North_U", true, 9.63f, 10.33f, -10.30f, 9.98f, Up, Roof, Win(-7.0f), Win(-2.5f), Win(2.0f, 0.8f), Win(6.4f));
            Wall("Shell", "South_U", true, -10.34f, -9.64f, -10.30f, 9.98f, Up, Roof, Win(-6.5f));
            Wall("Shell", "West_U", false, -10.30f, -9.60f, -10.34f, 10.33f, Up, Roof, Win(6.0f), Win(-5.5f), Gap(-0.35f, 0.8f));
            Wall("Shell", "East_U", false, 9.28f, 9.98f, -10.34f, 10.33f, Up, Roof, Win(6.4f), Win(0.0f));
            foreach (var x in new[] { -7.0f, -2.5f, 6.4f }) Window("Window_Large1", true, 9.63f, 10.33f, x, 1.2f, Up);
            Window("Window_Small1", true, 9.63f, 10.33f, 2.0f, 0.8f, Up);
            Window("Window_Large1", true, -10.34f, -9.64f, -6.5f, 1.2f, Up);
            foreach (var z in new[] { 6.0f, -5.5f }) Window("Window_Large1", false, -10.30f, -9.60f, z, 1.2f, Up);
            foreach (var z in new[] { 6.4f, 0.0f }) Window("Window_Large1", false, 9.28f, 9.98f, z, 1.2f, Up);

            // Loop Door: a plain leaf in the west wall. It leads to Loop 3, which is not built yet.
            var ld = (GameObject)PrefabUtility.InstantiatePrefab(doorAsset, Group("Doors"));
            ld.name = "LoopDoor";
            ld.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(270f, 0f, 0f);
            ld.transform.localScale = new Vector3(0.78f / doorMesh.size.x, 0.06f / doorMesh.size.y, 2.1f / doorMesh.size.z);
            ld.transform.position = Vector3.zero;
            var lb = MB(ld);
            ld.transform.position += new Vector3(-9.95f - lb.center.x, Up - lb.min.y, -0.35f - lb.center.z);
            ld.AddComponent<BoxCollider>();
            var loopDoor = ld.AddComponent<LoopDoor>();
            var so = new SerializedObject(loopDoor);
            so.FindProperty("promptText").stringValue = "Open the door";
            so.FindProperty("nextLoop").intValue = 3;
            so.FindProperty("arrivalCaption").stringValue = "NIGHT. THE SAME NIGHT.";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Roof slab and a shadow-only block above it so sunlight only enters through the windows.
            Box("Shell", "Roof", -10.3f, 9.98f, UpCeil, Roof, -10.34f, 10.33f, ceiling, WorldTiling.Mode.Floor);
            var blocker = Box("Shell", "SunBlocker_ShadowsOnly", -10.5f, 10.5f, 5.6f, 7.2f, -10.5f, 10.5f, ceiling, null, 0f, false);
            blocker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        // ------------------------------------------------------------------ interior walls

        private static void BuildGroundWalls()
        {
            HWall(4.5f, -9.60f, 9.28f, 0f, Ceil, Gap(-3.0f, 1.2f), Gap(3.5f), Gap(7.0f));
            HWall(-4.5f, -9.60f, 5.0f, 0f, Ceil, Gap(-1.0f, 2.0f));
            HWall(0.5f, -9.60f, -5.0f, 0f, Ceil);
            VWall(-5.0f, -4.5f, 4.5f, 0f, Ceil, Gap(2.5f), Gap(-2.0f));
            VWall(5.0f, -9.64f, 4.5f, 0f, Ceil, (-3.75f, 1.45f, 0f, 2.1f), Gap(1.5f));
            HWall(-6.2f, 5.0f, 9.28f, 0f, Ceil, Gap(7.5f));
            HWall(-2.0f, 5.0f, 9.28f, 0f, Ceil);
            VWall(2.0f, 4.5f, 9.63f, 0f, Ceil, Gap(7.0f));
            // Core: the stairwell between two cupboards
            HWall(3.0f, -3.5f, 3.5f, 0f, Ceil);
            HWall(-3.0f, -3.5f, 3.5f, 0f, Ceil, Gap(0f, 1.4f));
            VWall(-3.5f, -3.0f, 3.0f, 0f, Ceil, Gap(0f));
            VWall(3.5f, -3.0f, 3.0f, 0f, Ceil, Gap(0f));
            VWall(-0.75f, -3.0f, 3.0f, 0f, Ceil);
            VWall(0.75f, -3.0f, 3.0f, 0f, Ceil);

            Door("Door_Dining", true, 4.5f, -3.0f, 1.2f, 0f);
            Door("Door_Kitchen", true, 4.5f, 3.5f, 0.9f, 0f);
            Door("Door_KitchenUtility", true, 4.5f, 7.0f, 0.9f, 0f);
            Door("Door_DiningKitchen", false, 2.0f, 7.0f, 0.9f, 0f);
            Door("Door_LivingRoom", false, -5.0f, 2.5f, 0.9f, 0f);
            Door("Door_Utility", false, 5.0f, 1.5f, 0.9f, 0f);
            Door("Door_Coats", true, -6.2f, 7.5f, 0.9f, 0f);
            Door("Door_CupboardWest", false, -3.5f, 0f, 0.9f, 0f);
            Door("Door_CupboardEast", false, 3.5f, 0f, 0.9f, 0f);
            // The door to nowhere: it opens onto a brick wall.
            var nowhere = Door("Door_ToNowhere", false, -5.0f, -2.0f, 0.9f, 0f);
            var nso = new SerializedObject(nowhere);
            nso.FindProperty("openAngle").floatValue = 55f;
            nso.ApplyModifiedPropertiesWithoutUndo();
            Box("Walls", "Bricks_BehindDoor", -5.32f, -5.08f, 0f, 2.15f, -2.5f, -1.5f, brick);
        }

        private static void BuildUpperWalls()
        {
            float y0 = Up, y1 = UpCeil;
            HWall(3.0f, -3.5f, 3.5f, y0, y1, Gap(-1.5f), Gap(2.0f));
            HWall(-3.0f, -3.5f, 9.28f, y0, y1);
            VWall(-3.5f, -3.0f, -0.8f, y0, y1);
            VWall(-3.5f, 0.85f, 3.0f, y0, y1);
            VWall(3.5f, -3.0f, 3.0f, y0, y1, Gap(0f));
            HWall(-0.8f, -9.60f, -3.5f, y0, y1, Gap(-6.5f));
            VWall(0.5f, 3.0f, 9.63f, y0, y1);
            VWall(3.5f, 3.0f, 9.63f, y0, y1, Gap(7.5f));
            HWall(3.0f, 3.5f, 9.28f, y0, y1);
            VWall(-3.5f, -9.64f, -3.0f, y0, y1);

            // The corridor to the Loop Door narrows: its north wall slants from z 0.80 at the landing to z 0.10 at the door.
            var a = new Vector2(-9.60f, 0.10f);
            var b = new Vector2(-3.5f, 0.80f);
            var d = b - a;
            var slant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slant.name = "Corridor_SlantedWall";
            slant.transform.SetParent(Group("Walls"), false);
            slant.transform.position = new Vector3((a.x + b.x) / 2f, (y0 + y1) / 2f, (a.y + b.y) / 2f + T / 2f);
            slant.transform.rotation = Quaternion.Euler(0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f);
            slant.transform.localScale = new Vector3(d.magnitude + 0.05f, y1 - y0, T);
            slant.GetComponent<Renderer>().sharedMaterial = wall;
            Tile(slant, WorldTiling.Mode.Wall, 2.5f);

            Door("Door_ChildrensRoom", true, 3.0f, -1.5f, 0.9f, Up);
            Door("Door_Bathroom", true, 3.0f, 2.0f, 0.9f, Up);
            Door("Door_BathToMaster", false, 3.5f, 7.5f, 0.9f, Up);
            Door("Door_SewingRoom", true, -0.8f, -6.5f, 0.9f, Up);
            Door("Door_Study_Locked", false, 3.5f, 0f, 0.9f, Up, locked: true);
        }

        private static void BuildFloorsAndStairs()
        {
            // Ground floor ceiling and upper floor, with the stair hole x -0.7..0.7, z -2.2..2.0.
            var pieces = new (string n, float x0, float x1, float z0, float z1)[]
            {
                ("West", -10f, -0.7f, -10f, 10f), ("East", 0.7f, 10f, -10f, 10f),
                ("South", -0.7f, 0.7f, -10f, -2.2f), ("North", -0.7f, 0.7f, 2.0f, 10f),
            };
            foreach (var p in pieces)
            {
                Box("Floors", "Ceiling_" + p.n, p.x0, p.x1, Ceil, Ceil + 0.02f, p.z0, p.z1, ceiling, WorldTiling.Mode.Floor);
                Box("Floors", "UpperFloor_" + p.n, p.x0, p.x1, Ceil + 0.02f, Up, p.z0, p.z1, floor, WorldTiling.Mode.Floor, 4f);
            }
            // One straight flight of 15 steps in the middle of the house, rising north.
            for (int i = 1; i <= 15; i++)
            {
                float z0 = -2.2f + (i - 1) * 0.28f, z1 = -2.2f + i * 0.28f;
                Box("Stairs", $"Step_{i:00}", -0.6f, 0.6f, 0f, i * Up / 15f, z0, z1, floor, WorldTiling.Mode.Floor, 4f);
            }
            // Railings round the hole on the upper floor (the north edge is the way off the stairs).
            Box("Stairs", "Railing_West", -0.8f, -0.7f, Up, Up + 1.0f, -2.3f, 1.6f, floor);
            Box("Stairs", "Railing_East", 0.7f, 0.8f, Up, Up + 1.0f, -2.3f, 1.6f, floor);
            Box("Stairs", "Railing_South", -0.8f, 0.8f, Up, Up + 1.0f, -2.3f, -2.2f, floor);
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
                float floorY = b.min.y > Up - 0.2f ? Up : 0f;
                if (b.min.y - floorY > 0.15f) continue;                         // only things standing on the floor
                if (b.max.y - floorY >= 1.0f || b.size.x < 0.05f || b.size.z < 0.05f) continue;
                var go = new GameObject("StepBlocker_" + col.gameObject.name);
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
            // Dining room: the table set for the whole family.
            var table = P("Dining", "Table", "Furniture/Table_RoundLarge.fbx", 0.68f, 0f, -4.5f, 7.0f, 0f);
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
                float half = s.dir.x != 0 ? hx : hz;
                var along = new Vector3(s.along, 0f, 0f);
                var chair = tb.center + s.dir * (half + 0.13f) + along;
                var plate = tb.center + s.dir * (half - (s.dir.x != 0 ? 0.30f : 0.22f)) + along;
                var right = new Vector3(Mathf.Cos(s.yaw * Mathf.Deg2Rad), 0f, -Mathf.Sin(s.yaw * Mathf.Deg2Rad));
                P("Dining", "Chair_" + si, "Furniture/Chair_2.fbx", 0.54f, s.yaw, chair.x, chair.z, 0f);
                P("Dining", "Plate_" + si, "Furniture/Plate_1.fbx", 0.73f, 0f, plate.x, plate.z, top, ' ', 0f, false);
                var fp = plate - right * 0.2f;
                var kp = plate + right * 0.2f;
                var sp = plate + right * 0.26f;
                P("Dining", "Fork_" + si, "Furniture/Fork.fbx", 0.48f, s.yaw + 180f, fp.x, fp.z, top, ' ', 0f, false);
                P("Dining", "Knife_" + si, "Furniture/Knife.fbx", 0.48f, s.yaw + 180f, kp.x, kp.z, top, ' ', 0f, false);
                P("Dining", "Spoon_" + si, "Furniture/Spoon.fbx", 0.48f, s.yaw + 180f, sp.x, sp.z, top, ' ', 0f, false);
                si++;
            }
            var sideboard = P("Dining", "Sideboard", "Furniture/Drawer_5.fbx", 0.55f, 90f, N, 7.0f, 0f, 'W', -9.60f);
            P("Dining", "FamilyPhoto", "Picture_Frames_FREE/Prefabs/SM_frame_01.prefab", 1f, 90f, -9.2f, 6.6f, Top(sideboard), ' ', 0f, false);
            P("Dining", "FamilyPhoto2", "Picture_Frames_FREE/Prefabs/SM_frame_01.prefab", 1f, 90f, -9.2f, 7.5f, Top(sideboard), ' ', 0f, false);
            WallPhoto("Dining", 0.2f, 1.25f, 9.63f, 180f);
            WallPhoto("Dining", 1.2f, 1.35f, 9.63f, 180f);
            P("Dining", "Rug", "Furniture/Carpet_2.fbx", 1f, 90f, -4.5f, 7.0f, 0f, ' ', 0f, false);
            P("Dining", "Plant", "Furniture/Houseplant_8.fbx", 0.6f, 0f, 1.5f, 5.0f, 0f, ' ', 0f, false);
            FloorLamp("Dining", 1.5f, 9.1f, true, true);
            CeilingLamp("Dining", -4.5f, 7.0f, Ceil, 1.2f, 7f, true);

            // Kitchen: counters along the north wall, every cupboard labelled.
            float cx = 2.45f;
            string[] run = { "Kitchen_3Drawers", "Kitchen_Oven_Large", "Kitchen_3Drawers", "Kitchen_Sink", "Kitchen_3Drawers", "Kitchen_3Drawers", "Kitchen_3Drawers", "Kitchen_3Drawers" };
            foreach (var m in run)
            {
                float f = m == "Kitchen_Oven_Large" ? 0.566f : 0.56f;
                var c = P("Kitchen", m, $"Furniture/{m}.fbx", f, 180f, cx, N, 0f, 'N', 9.63f);
                var cb = MB(c);
                c.transform.position += new Vector3(cx - cb.min.x, 0f, 0f);
                cb = MB(c);
                cx = cb.max.x + 0.005f;
                Note(cb.center.x, 0.62f, cb.min.z - 0.008f, 'S');
            }
            P("Kitchen", "Fridge", "Furniture/Kitchen_Fridge.fbx", 0.54f, 270f, N, 8.85f, 0f, 'E', 9.28f);
            Note(8.55f, 1.3f, 8.85f, 'W');
            Note(8.55f, 1.15f, 9.0f, 'W');
            var kt = P("Kitchen", "Table", "Furniture/Table_RoundSmall2.fbx", 0.62f, 0f, 5.6f, 6.6f, 0f);
            P("Kitchen", "Chair_W", "NextGen Furniture Pack/Prefabs/chair_1.prefab", 1f, 90f, 4.75f, 6.6f, 0f);
            P("Kitchen", "Chair_E", "NextGen Furniture Pack/Prefabs/chair_1.prefab", 1f, 270f, 6.45f, 6.6f, 0f);
            P("Kitchen", "Mug", "kitchen props/models/mug.fbx", 1f, 30f, 5.5f, 6.5f, Top(kt), ' ', 0f, false);
            Note(5.75f, Top(kt) + 0.004f, 6.75f, 'N');
            CeilingLamp("Kitchen", 5.6f, 6.8f, Ceil, 1.2f, 6.5f, true);

            // Utility: washer, shelf and the fuse box (the power upstairs is out).
            P("Utility", "Washer", "Furniture/Bathroom_WashingMachine.fbx", 0.58f, 0f, 5.75f, N, 0f, 'S', -1.95f);
            P("Utility", "Shelf", "Furniture/Shelf_1.fbx", 0.5f, 90f, N, 3.3f, 0f, 'W', 5.05f);
            Box("Furniture/Utility", "FuseBox", 9.14f, 9.28f, 1.3f, 1.95f, 2.95f, 3.55f, metal);
            Note(9.13f, 1.75f, 3.25f, 'W');
            Boxes("Utility", 8.6f, -1.55f, 2);
            CeilingLamp("Utility", 7.2f, 1.2f, Ceil, 0.9f, 5.5f, true);

            // Vestibule and coats.
            var hall = P("Vestibule", "HallCabinet", "NextGen Furniture Pack/Prefabs/commode_2.prefab", 1f, 180f, 7.2f, N, 0f, 'N', -2.05f);
            P("Vestibule", "Mirror", "Furniture/Bathroom_Mirror1.fbx", 0.8f, 180f, 7.2f, N, Top(hall) + 0.2f, 'N', -2.05f, false);
            Note(7.55f, Top(hall) + 0.5f, -2.14f, 'S');
            P("Vestibule", "Rug", "Furniture/Carpet_1.fbx", 1f, 0f, 7.6f, -4.1f, 0f, ' ', 0f, false);
            CeilingLamp("Vestibule", 7.3f, -4.1f, Ceil, 1.0f, 5.5f, true);
            P("Coats", "Shelf", "Furniture/Drawer_4.fbx", 0.5f, 0f, 6.3f, N, 0f, 'S', -9.64f);
            Boxes("Coats", 8.6f, -9.2f, 3);

            // The living room, shrunk around all its furniture.
            P("LivingRoom", "Fireplace", "Furniture/Fireplace.fbx", 0.51f, 180f, -7.6f, N, 0f, 'N', 4.45f);
            P("LivingRoom", "Couch", "Furniture/Couch_Large2.fbx", 0.4f, 0f, -7.6f, N, 0f, 'S', 0.55f);
            P("LivingRoom", "Armchair", "NextGen Furniture Pack/Prefabs/chair_5.prefab", 1f, 90f, -9.0f, 2.6f, 0f);
            P("LivingRoom", "Armchair2", "NextGen Furniture Pack/Prefabs/chair_3.prefab", 1f, 225f, -5.9f, 3.7f, 0f);
            P("LivingRoom", "CoffeeTable", "Furniture/Table_RoundSmall.fbx", 0.39f, 0f, -7.5f, 2.5f, 0f);
            P("LivingRoom", "SideTable", "NextGen Furniture Pack/Prefabs/table_1.prefab", 1f, 0f, -5.5f, 1.0f, 0f);
            FloorLamp("LivingRoom", -9.15f, 4.05f, true, true);

            // The other living room: wide and nearly empty.
            P("OtherLivingRoom", "Rug", "Furniture/Carpet_2.fbx", 1f, 0f, -2.7f, -7.3f, 0f, ' ', 0f, false);
            P("OtherLivingRoom", "Armchair", "NextGen Furniture Pack/Prefabs/chair_5.prefab", 1f, 180f, -2.7f, -7.2f, 0f);
            var tvTable = P("OtherLivingRoom", "TVTable", "NextGen Furniture Pack/Prefabs/table_4.prefab", 0.5f, 0f, -2.7f, N, 0f, 'S', -9.64f);
            float tvBase = Top(tvTable);
            Box("Furniture/OtherLivingRoom", "TV", -3.2f, -2.2f, tvBase, tvBase + 0.62f, -9.5f, -9.2f, metal);
            Box("Furniture/OtherLivingRoom", "TVScreen", -3.12f, -2.28f, tvBase + 0.06f, tvBase + 0.56f, -9.205f, -9.19f, screen, null, 0f, false);
            FloorLamp("OtherLivingRoom", -1.2f, -9.0f, true, true);

            // Ring hall: the same photograph on every side of the core, and lamps at the corners.
            WallPhoto("RingHall", 1.8f, 1.25f, 3.05f, 0f);
            WallPhoto("RingHall", 2.2f, 1.25f, -3.05f, 180f);
            WallPhoto("RingHall", 3.55f, 1.25f, 1.8f, 90f);
            WallPhoto("RingHall", -3.55f, 1.25f, 1.8f, 270f);
            foreach (var (lx, lz) in new[] { (-4.25f, 3.75f), (4.25f, 3.75f), (-4.25f, -3.75f), (4.25f, -3.75f), (0f, 3.75f), (0f, -3.75f) })
                CeilingLamp("RingHall", lx, lz, Ceil, 1.3f, 6f, true);

            // Core cupboards: labelled boxes on one side, a single note on the other.
            Boxes("Cupboards", -2.9f, 2.3f, 3);
            Boxes("Cupboards", -2.0f, 2.4f, 2);
            Boxes("Cupboards", -2.9f, -2.3f, 2);
            Note(-2.9f, 0.3f, 2.04f, 'S');
            Note(-2.0f, 0.3f, 2.14f, 'S');
            Note(-2.9f, 0.3f, -2.56f, 'S');
            Note(3.44f, 1.5f, -2.2f, 'W');
        }

        private static void FurnishUpper()
        {
            const float N = float.NaN;
            // Children's room: the biggest room upstairs, kept as if they still sleep here.
            P("ChildrensRoom", "Bed_A", "Furniture/Bed_Single.fbx", 0.5f, 180f, -8.1f, N, Up, 'N', 9.63f);
            P("ChildrensRoom", "Bed_B", "Furniture/Bed_Single.fbx", 0.5f, 180f, -5.9f, N, Up, 'N', 9.63f);
            P("ChildrensRoom", "Nightstand", "Furniture/NightStand_2.fbx", 0.5f, 180f, -7.0f, N, Up, 'N', 9.63f);
            P("ChildrensRoom", "ToyChest", "Furniture/Drawer_1.fbx", 0.35f, 90f, N, 5.0f, Up, 'W', -9.60f);
            var desk = P("ChildrensRoom", "Desk", "NextGen Furniture Pack/Prefabs/table_4.prefab", 0.5f, 180f, -2.2f, N, Up, 'N', 9.63f);
            ScaleHeight(desk, 0.66f, Up);
            P("ChildrensRoom", "DeskChair", "NextGen Furniture Pack/Prefabs/chair_4.prefab", 1f, 0f, -2.2f, 8.5f, Up);
            P("ChildrensRoom", "Shelf", "Furniture/Shelf_1.fbx", 0.5f, 270f, N, 6.0f, Up, 'E', 0.45f);
            P("ChildrensRoom", "Rug", "Furniture/Carpet_2.fbx", 1f, 0f, -5.5f, 4.5f, Up, ' ', 0f, false);
            CeilingLamp("ChildrensRoom", -4.6f, 5.6f, UpCeil, 1.2f, 7.5f, true);

            // Bathroom: the only way into the master bedroom.
            P("Bathroom", "Bathtub", "Furniture/Bathroom_Bathtub.fbx", 0.45f, 0f, N, 8.6f, Up, 'W', 0.55f);
            P("Bathroom", "Toilet", "Furniture/Bathroom_Toilet.fbx", 0.42f, 0f, 2.9f, N, Up, 'S', 3.05f);
            P("Bathroom", "Sink", "Furniture/Bathroom_Sink.fbx", 0.6f, 270f, N, 5.4f, Up, 'E', 3.45f);
            P("Bathroom", "Mirror", "Furniture/Bathroom_Mirror1.fbx", 0.8f, 270f, N, 5.4f, Up + 1.15f, 'E', 3.45f, false);
            CeilingLamp("Bathroom", 2.0f, 6.2f, UpCeil, 1.0f, 5.5f, true);

            // Master bedroom: one side of the bed slept in.
            P("MasterBedroom", "Bed", "Furniture/Bed_King.fbx", 0.56f, 180f, 6.5f, N, Up, 'N', 9.63f);
            var ns = P("MasterBedroom", "Nightstand_L", "Furniture/NightStand_1.fbx", 0.5f, 180f, 5.15f, N, Up, 'N', 9.63f);
            P("MasterBedroom", "Nightstand_R", "Furniture/NightStand_1.fbx", 0.5f, 180f, 7.85f, N, Up, 'N', 9.63f);
            var lamp = P("MasterBedroom", "BedsideLamp", "Furniture/Light_Desk.fbx", 0.6f, 180f, 5.15f, 9.35f, Top(ns), ' ', 0f, false);
            Lamp(lamp, MB(lamp).center + new Vector3(0f, 0.08f, -0.08f), 1.2f, 4.5f, true);
            P("MasterBedroom", "Wardrobe", "Furniture/Drawer_4.fbx", 0.75f, 270f, N, 4.6f, Up, 'E', 9.28f);
            P("MasterBedroom", "Rug", "Furniture/Carpet_2.fbx", 0.8f, 0f, 6.4f, 6.4f, Up, ' ', 0f, false);

            // Study: locked, and the power to it is out.
            var sdesk = P("Study", "Desk", "NextGen Furniture Pack/Prefabs/table_4.prefab", 0.7f, 180f, 6.3f, N, Up, 'N', 2.95f);
            ScaleHeight(sdesk, 0.76f, Up);
            P("Study", "DeskChair", "NextGen Furniture Pack/Prefabs/chair_6.prefab", 1f, 0f, 6.3f, 1.6f, Up);
            P("Study", "Files", "Furniture/Drawer_1.fbx", 0.5f, 270f, N, -1.6f, Up, 'E', 9.28f);
            P("Study", "Bookshelf", "Furniture/Bookshelf.fbx", 0.5f, 0f, 6.0f, N, Up, 'S', -2.95f);
            CeilingLamp("Study", 6.4f, 0f, UpCeil, 1.0f, 6f, false);

            // Sewing room: his wife's room, untouched.
            var st = P("SewingRoom", "SewingTable", "NextGen Furniture Pack/Prefabs/table_1.prefab", 1f, 90f, N, -3.9f, Up, 'W', -9.60f);
            P("SewingRoom", "Chair", "NextGen Furniture Pack/Prefabs/chair_1.prefab", 1f, 270f, -8.4f, -3.9f, Up);
            P("SewingRoom", "Shelf", "Furniture/Shelf_1.fbx", 0.5f, 90f, N, -7.4f, Up, 'W', -9.60f);
            P("SewingRoom", "Stool", "Furniture/Stool.fbx", 0.5f, 0f, -5.0f, -8.8f, Up);
            Boxes("SewingRoom", -4.2f, -9.1f, 2, Up);
            P("SewingRoom", "Rug", "Furniture/Carpet_1.fbx", 1f, 0f, -6.5f, -5.6f, Up, ' ', 0f, false);
            CeilingLamp("SewingRoom", -6.5f, -5.2f, UpCeil, 1.1f, 7f, true);

            // Landing and corridor: dim, so the walls closing in towards the Loop Door can still be seen.
            CeilingLamp("Landing", -2.3f, 0f, UpCeil, 0.9f, 5.5f, true);
            CeilingLamp("Landing", 2.3f, 0f, UpCeil, 0.9f, 5.5f, true);
            CeilingLamp("Landing", -6.5f, -0.35f, UpCeil, 0.8f, 5f, true);
        }
    }
}
