using System.Collections.Generic;
using LeftBehind.Loop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Dresses the house with the small things people leave about, from the Low Poly Ultimate Pack
    /// (Assets/polyperfect), so each loop tells its part of the story through clutter:
    ///   Loop 1  the present: one old man's leftovers in an emptied house. Tins on the counter and
    ///           an open one on the table, his cane by the armchair, a magnifying glass on the
    ///           magazines, one coat, one toothbrush, a glass of water, an empty plant pot where
    ///           the plant was, boxes he packed and never moved. The children's room stays bare.
    ///   Loop 2  years earlier: he lives alone as if they were still here, and forgets. Cold cups
    ///           of coffee all over the house, three cartons of milk and two boxes of cereal, all the
    ///           same; the family's coats on the hooks, four toothbrushes, the children's room kept
    ///           ready (Emily's easel and paints, Sam's ball and skateboard).
    ///   Loop 3  the night itself: the whole family's home in use. Dinner on the stove, fruit on
    ///           the table, the television, school things, toys on the floor and on the landing.
    /// Nothing goes in the study (the final scene) or the attic (the mannequins' NavMesh).
    /// Each prop is placed on whatever is below it (a counter, a table, the floor) and skipped
    /// with a warning if it would sit inside something else. Small props have no collider.
    /// Builds a "HouseDressing" object under the StoryProps root. Called by StoryPropsBuilder;
    /// Menu: Left Behind > Story > Dress The House (for the open loop scene).
    /// </summary>
    public static class HouseDressingBuilder
    {
        private const string RootName = "HouseDressing";
        private const string PackFolder = "Assets/polyperfect/Low Poly Ultimate Pack";
        private const string PrefabFolder = PackFolder + "/M/- Prefabs_M/";

        private const float Ground = 0.05f;
        private const float Upper = 2.6f;

        private static Transform root;
        private static int placed, skipped;

        // Room-facing directions for things against a wall.
        private static readonly Vector3 North = Vector3.forward, South = Vector3.back, East = Vector3.right, West = Vector3.left;

        [MenuItem("Left Behind/Story/Dress The House")]
        public static void PlaceInOpenScene()
        {
            var env = Object.FindAnyObjectByType<LoopEnvironment>();
            if (env == null) { Debug.LogError("House dressing: this scene has no LoopEnvironment."); return; }
            var props = GameObject.Find("StoryProps");
            if (props == null) { Debug.LogError("House dressing: no StoryProps in this scene; run Place Story Props first."); return; }
            Place(props.transform, env.Loop);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        public static void Place(Transform parent, int loop)
        {
            ConvertPackMaterials();
            var old = parent.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            root = new GameObject(RootName).transform;
            root.SetParent(parent, false);
            placed = skipped = 0;
            Physics.SyncTransforms();

            switch (loop)
            {
                case 1: Loop1(); break;
                case 2: Loop2(); break;
                case 3: Loop3(); break;
            }
            Debug.Log($"House dressing: Loop {loop}, {placed} props placed, {skipped} skipped.");
        }

        // ------------------------------------------------------------------ Loop 1: the present

        private static void Loop1()
        {
            // Kitchen: he eats out of tins.
            Put("Food_M/can-beans", 6.0f, 9.3f, 0f, 0.65f);
            Put("Food_M/can-beans", 6.15f, 9.42f, 40f, 0.65f);
            Put("Food_M/can-beans-open", 3.0f, 4.35f, 20f, 0.6f);
            Put("Drinks_M/milk-box", 3.55f, 9.3f, 10f, 0.7f);
            Put("Survival_M/garbage-bag", 1.45f, 0.35f, 30f, 0.8f, solid: true);

            // Living room: where he sits.
            Put("Furniture_M/magazines-pile", -7.75f, -3.8f, 20f);
            Put("Tools_M/magnifying-glass", -7.55f, -3.55f, -30f);
            Put("Tools_M/cane", -7.05f, -1.65f, 0f, 1f, tiltZ: 8f);
            Put("Furniture_M/pot-medium-circle-empty", -9.3f, -5.4f, 0f, 1.2f, solid: true);
            Put("Electronics_M/radio-small", -0.5f, -4.6f, -90f, 1.2f);

            // Entrance: one coat left, and boxes he packed and never moved.
            Hang("Furniture_M/hanger-wall-clothes-b", 8.0f, 1.85f, -2.3f, South);
            var box = Put("Props_M/package-box", 8.85f, -6.85f, 10f, 0.55f, solid: true);
            if (box != null) PutOn(box, "Props_M/package-box", 0.02f, 0.03f, -15f, 0.45f, solid: true);

            // Upstairs.
            Put("Furniture_M/cup-bathroom", 0.08f, -2.62f, 0f, 1f, Upper);
            Put("Furniture_M/toothbrush", 0.12f, -2.75f, 70f, 1f, Upper);
            Put("Survival_M/toilet-paper", 4.6f, -4.15f, 0f, 1f, Upper);
            Put("Drinks_M/cup-small", -6.28f, 9.2f, 0f, 1f, Upper);
            Put("Props_M/package-box", -9.15f, 2.6f, 0f, 0.6f, Upper, solid: true);
            Put("Props_M/package-box-open", -8.25f, 2.7f, 15f, 0.45f, Upper, solid: true);
        }

        // ------------------------------------------------------------------ Loop 2: years earlier

        private static void Loop2()
        {
            // Kitchen: the same things bought again and again, and coffee he forgot to drink.
            Put("Drinks_M/milk-box", 5.45f, 9.3f, 0f, 0.7f);
            Put("Drinks_M/milk-box", 5.6f, 9.32f, 5f, 0.7f);
            Put("Drinks_M/milk-box", 5.75f, 9.3f, -5f, 0.7f);
            Put("Food_M/cereal-box", 4.75f, 9.42f, 0f, 0.8f);
            Put("Food_M/cereal-box", 4.97f, 9.42f, 0f, 0.8f);
            Put("Drinks_M/coffee-cup", 4.3f, 9.25f, 30f, 0.6f);
            Put("Drinks_M/coffee-cup", 2.65f, 5.75f, 0f, 0.6f);
            Put("Drinks_M/mokka-pot", 2.6f, 9.28f, 0f, 0.9f);
            Put("Dishes_M/pan", 9.0f, 1.6f, 0f);
            var board = Put("Dishes_M/board-cutting", 9.0f, 2.55f, 90f);
            if (board != null) PutOn(board, "Food_M/bread-round", 0f, 0f, 0f, 0.7f);
            Put("Tools_M/dish-brush", 0.3f, 9.35f, 0f, 0.8f);
            Put("Furniture_M/bin-house-small-full", 0.2f, 8.6f, 0f, 1f, solid: true);
            Hang("Dishes_M/rail-kitchen-utensil", 5.6f, 1.75f, 9.6f, South);

            // Dining room: the table he sets for six.
            Put("Dishes_M/bowl-salad", -5.0f, 5.0f, 0f);
            Put("Survival_M/candle", -5.5f, 5.0f, 0f, 1.2f);
            Put("Survival_M/candle", -4.5f, 5.0f, 0f, 1.2f);
            Put("Furniture_M/napkin-stack", -9.3f, 5.1f, 90f, 0.7f);
            Put("Drinks_M/coffee-cup", -4.6f, 9.4f, 0f, 0.6f);

            // Living room: the plant is still alive.
            Put("Furniture_M/magazines-pile", -7.6f, -3.6f, -15f);
            Put("Furniture_M/pot-medium-circle-plant", -9.3f, -5.0f, 0f, 0.9f, solid: true);
            Put("Drinks_M/coffee-cup", -2.6f, -3.6f, 0f, 0.6f);

            // Coats room and hall: all four coats still hang there.
            Hang("Furniture_M/hanger-wall-clothes-a", 6.5f, 1.85f, -1.1f, East);
            Put("Furniture_M/bench-storage-shoes", 6.75f, -1.1f, 90f, 1f, solid: true);
            Put("Props_M/cap-baseball", 7.5f, -2.55f, 160f, 0.9f);

            // Children's room, kept ready for them.
            Put("Furniture_M/stand-easel-canvas-vertical-nature", -0.85f, -3.7f, 0f, 0.65f, Upper, solid: true, face: West);
            Put("Tools_M/palette-color", -1.55f, -4.38f, 90f, 1f, Upper);
            Put("Tools_M/paintbrush-round-paint", -1.4f, -4.3f, 0f, 1f, Upper, tiltX: 90f);
            Put("Music_M/soprano-recorder", -2.45f, -4.35f, 80f, 1f, Upper, tiltX: 90f);
            Put("Electronics_M/radio-kids", -6.6f, -4.35f, 0f, 0.8f, Upper);
            Put("Furniture_M/trophy-bronze", -0.35f, -2.6f, -90f, 1.6f, Upper);
            Put("Sport_M/skateboard", -8.6f, -0.6f, 90f, 1f, Upper);
            Put("Sport_M/soccer-ball", -4.0f, -4.2f, 0f, 0.85f, Upper);
            Put("Electronics_M/console-handheld", -7.7f, -3.3f, 30f, 0.8f, Upper);
            Hang("Furniture_M/pinboard", -2.0f, 4.0f, -4.6f, North);

            // Bathroom: four toothbrushes.
            Put("Furniture_M/toothbrush-holder", 6.25f, -1.95f, 0f, 1f, Upper);
            Put("Furniture_M/soap-tray", 6.25f, -2.45f, 90f, 1f, Upper);
            Hang("Furniture_M/towel-bathroom-large-hang", 1.2f, 4.0f, -4.6f, North);
            Put("Furniture_M/mat-bathroom", 1.0f, -3.3f, 90f, 1f, Upper);
            Put("Furniture_M/toilet-brush-holder", 5.7f, -4.35f, 0f, 1f, Upper);
            Put("Survival_M/toilet-paper", 4.6f, -4.35f, 0f, 1f, Upper);

            // Master bedroom: another cup.
            Put("Drinks_M/coffee-cup", -6.2f, 9.2f, 0f, 0.6f, Upper);
        }

        // ------------------------------------------------------------------ Loop 3: the night

        private static void Loop3()
        {
            // Kitchen: dinner on the go.
            Put("Dishes_M/pan", 9.0f, 2.4f, 0f);
            Put("Dishes_M/pot-medium", 9.0f, 2.72f, 0f);
            var board = Put("Dishes_M/board-cutting", 5.0f, 9.3f, 0f);
            if (board != null) PutOn(board, "Food_M/bread-round", 0.05f, 0f, 0f, 0.7f);
            Put("Drinks_M/teapot", 2.9f, 9.3f, 200f, 0.6f);
            Put("Food_M/cereal-box", 6.1f, 9.42f, 0f, 0.8f);
            Put("Food_M/salt-drum", 9.1f, 1.95f, 0f, 0.7f);
            Put("Tools_M/dish-brush", 9.05f, 1.6f, 0f, 0.8f);
            var bowl = Put("Dishes_M/bowl-serving", 2.95f, 4.35f, 0f, 1.3f);
            if (bowl != null)
            {
                PutOn(bowl, "Food_M/apple", -0.04f, 0.02f, 0f, 0.55f, sink: 0.07f);
                PutOn(bowl, "Food_M/apple", 0.05f, -0.03f, 50f, 0.55f, sink: 0.07f);
                PutOn(bowl, "Food_M/banana", 0f, 0.05f, 30f, 0.8f, sink: 0.06f);
            }
            Put("Drinks_M/jug-milk", 2.5f, 4.5f, 0f);
            Hang("Dishes_M/rail-kitchen-utensil", 1.8f, 1.75f, 9.6f, South);

            // Dining room.
            Put("Drinks_M/wine-bottle-red", -5.05f, 5.15f, 0f, 0.9f);
            Put("Drinks_M/glass-drink", -4.7f, 4.75f, 0f);
            Put("Drinks_M/glass-drink", -5.4f, 5.4f, 0f);
            Put("Furniture_M/napkin-stack", -0.5f, 7.1f, 0f, 0.7f);

            // Living room: the family's evening.
            Put("Electronics_M/television-old", -0.5f, -4.0f, 0f, 1f, solid: true, face: West);
            Put("Electronics_M/radio-medium", -0.5f, -3.2f, -90f);
            Put("Furniture_M/magazines-pile", -7.8f, -3.85f, -10f);
            Put("Drinks_M/cup-small-tea", -7.55f, -3.55f, 0f);
            Put("Furniture_M/pot-medium-circle-plant", -9.3f, -5.4f, 0f, 0.9f, solid: true);
            Put("Sport_M/soccer-ball", -5.4f, -5.6f, 0f, 0.85f);

            // Entrance: everyone's coats and shoes.
            Hang("Furniture_M/hanger-wall-clothes-a", 7.6f, 1.85f, -2.3f, South);
            Hang("Furniture_M/hanger-wall-clothes-c", 8.7f, 1.85f, -2.3f, South);
            Put("Furniture_M/bench-storage-shoes", 8.0f, -2.55f, 0f, 1f, solid: true, face: South);
            Put("Props_M/cap-baseball", 6.75f, -3.9f, 120f, 0.9f);
            Put("Tools_M/broom", 8.95f, 3.6f, 90f, 1f, tiltZ: 8f);

            // Children's room: in use.
            Put("Furniture_M/stand-easel-canvas-vertical-nature", -0.8f, -2.9f, 0f, 0.65f, Upper, solid: true, face: West);
            Put("Tools_M/palette-color", -1.33f, -4.38f, 90f, 1f, Upper);
            Put("Tools_M/paintbrush-round-paint", -1.3f, -4.2f, 10f, 1f, Upper, tiltX: 90f);
            Put("Music_M/soprano-recorder", -2.25f, -4.35f, 80f, 1f, Upper, tiltX: 90f);
            Put("Electronics_M/radio-kids", -6.7f, -4.35f, 0f, 0.8f, Upper);
            Put("Furniture_M/trophy-bronze", -0.35f, -1.4f, -90f, 1.6f, Upper);
            Put("Sport_M/skateboard", -8.6f, -0.6f, 90f, 1f, Upper);
            Put("Sport_M/basketball", -4.4f, -4.15f, 0f, 0.85f, Upper);
            Put("Electronics_M/console-handheld", -7.7f, -3.4f, 30f, 0.8f, Upper);
            Hang("Furniture_M/pinboard", -1.8f, 4.0f, -4.6f, North);

            // Landing: a ball left outside their door.
            Put("Sport_M/soccer-ball", -4.2f, 0.7f, 0f, 0.85f, Upper);

            // Bathroom.
            Put("Furniture_M/toothbrush-holder", 0.08f, -2.62f, 90f, 1f, Upper);
            Put("Furniture_M/soap-tray", 0.08f, -1.62f, 0f, 1f, Upper);
            Hang("Furniture_M/towel-bathroom-large-hang", 1.6f, 4.0f, -4.6f, North);
            Put("Furniture_M/mat-bathroom", 2.85f, -3.5f, 0f, 1f, Upper);
            Put("Furniture_M/toilet-brush-holder", 6.2f, -1.25f, 0f, 1f, Upper);
            Put("Survival_M/toilet-paper", 4.6f, -4.2f, 0f, 1f, Upper);

            // Master bedroom.
            Put("Drinks_M/cup-small", -3.68f, 9.2f, 0f, 1f, Upper);
            Put("Furniture_M/mirror-table-small", -0.55f, 6.2f, -90f, 1f, Upper);
            Put("Furniture_M/bag-storage-clothes", -9.2f, 2.6f, 0f, 1f, Upper, solid: true);
        }

        // ------------------------------------------------------------------ placing

        /// <summary>
        /// Puts a prop at (x, z) on whatever is below it on that floor, turned <paramref name="yaw"/>
        /// degrees (or to face <paramref name="face"/>). Returns null if it would sit inside something.
        /// </summary>
        private static GameObject Put(string prefab, float x, float z, float yaw, float scale = 1f, float floor = Ground,
            bool solid = false, Vector3 face = default, float tiltX = 0f, float tiltZ = 0f)
        {
            var go = Spawn(prefab, scale, Rotation(yaw, face, tiltX, tiltZ));
            if (go == null) return null;
            float top = SurfaceBelow(new Vector3(x, floor + 2.1f, z), floor);
            Settle(go, new Vector3(x, top, z));
            return Keep(go, solid);
        }

        /// <summary>Puts a prop on top of another one just placed (props have no colliders to land on).</summary>
        private static GameObject PutOn(GameObject below, string prefab, float dx, float dz, float yaw, float scale = 1f,
            float sink = 0f, bool solid = false)
        {
            var b = Bounds(below);
            var go = Spawn(prefab, scale, Quaternion.Euler(0f, yaw, 0f));
            if (go == null) return null;
            Settle(go, new Vector3(b.center.x + dx, b.max.y - sink, b.center.z + dz));
            go.transform.SetParent(below.transform, true);
            return Keep(go, solid, ignore: below);
        }

        /// <summary>
        /// Hangs a wall prop facing <paramref name="face"/> (into the room): its back flat against the wall
        /// nearest (x, z) behind it, its top at <paramref name="top"/>.
        /// </summary>
        private static GameObject Hang(string prefab, float x, float top, float z, Vector3 face, float scale = 1f)
        {
            var go = Spawn(prefab, scale, Quaternion.LookRotation(face));
            if (go == null) return null;
            var wall = new Vector3(x, top, z);
            // Find the wall's actual surface: look back at it from a little way into the room.
            var probe = wall + Vector3.down * 0.25f + face * 0.6f;
            if (Physics.Raycast(probe, -face, out var hit, 1.2f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            {
                float off = Vector3.Dot(hit.point - wall, face);
                if (Mathf.Abs(off) > 0.15f)
                {
                    // Much further back than the wall should be: a window or a doorway, not a wall.
                    Debug.LogWarning($"House dressing: no wall behind {go.name} at ({x:F2}, {z:F2}); skipped.");
                    Object.DestroyImmediate(go);
                    skipped++;
                    return null;
                }
                wall += face * off;
            }
            var b = Bounds(go);
            float depth = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(face.x), Mathf.Abs(face.y), Mathf.Abs(face.z))));
            var move = new Vector3(x - b.center.x, top - b.max.y, z - b.center.z);
            move -= face * Vector3.Dot(move, face);                                     // along the wall and up only
            move += face * (Vector3.Dot(wall - b.center, face) + depth + 0.005f);       // then back against it
            go.transform.position += move;
            return Keep(go, false);
        }

        private static Quaternion Rotation(float yaw, Vector3 face, float tiltX, float tiltZ)
        {
            var turn = face != default ? Quaternion.LookRotation(face) : Quaternion.Euler(0f, yaw, 0f);
            return turn * Quaternion.Euler(tiltX, 0f, tiltZ);
        }

        private static GameObject Spawn(string prefab, float scale, Quaternion rotation)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + prefab + ".prefab");
            if (asset == null) { Debug.LogWarning("House dressing: no prefab " + prefab); skipped++; return null; }
            var go = Object.Instantiate(asset, root);
            go.name = asset.name;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            return go;
        }

        /// <summary>Moves <paramref name="go"/> so the middle of its base is at <paramref name="at"/>.</summary>
        private static void Settle(GameObject go, Vector3 at)
        {
            var b = Bounds(go);
            go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        }

        /// <summary>Keeps the prop if it does not sit inside anything solid; gives it a box if it should block Walter.</summary>
        private static GameObject Keep(GameObject go, bool solid, GameObject ignore = null)
        {
            var b = Bounds(go);
            var shrunk = new Bounds(b.center + Vector3.up * 0.02f, Vector3.Max(b.size * 0.8f - Vector3.up * 0.03f, Vector3.one * 0.01f));
            foreach (var c in Physics.OverlapBox(shrunk.center, shrunk.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.gameObject.layer == 2 || c is CharacterController) continue;      // step blockers, the player
                if (ignore != null && c.transform.IsChildOf(ignore.transform)) continue;
                Debug.LogWarning($"House dressing: {go.name} at {b.center:F2} would sit inside {Path(c.transform)}; skipped.");
                Object.DestroyImmediate(go);
                skipped++;
                return null;
            }
            // Never on or in a story prop (the pills, the ring, the calendar...), collider or not.
            var lifted = new Bounds(b.center - Vector3.up * 0.03f, b.size);
            foreach (var r in root.parent.GetComponentsInChildren<Renderer>())
            {
                if (r.transform.IsChildOf(root) || !r.bounds.Intersects(lifted)) continue;
                Debug.LogWarning($"House dressing: {go.name} at {b.center:F2} would touch story prop {Path(r.transform)}; skipped.");
                Object.DestroyImmediate(go);
                skipped++;
                return null;
            }
            float biggest = Mathf.Max(b.size.x, b.size.y, b.size.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                if (biggest < 0.35f) r.shadowCastingMode = ShadowCastingMode.Off;
            if (solid)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    mf.gameObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            placed++;
            return go;
        }

        /// <summary>The top of whatever solid thing is below <paramref name="from"/> (the floor if nothing).</summary>
        private static float SurfaceBelow(Vector3 from, float floor)
        {
            const int notStepBlockers = ~(1 << 2);
            float best = float.MaxValue, y = floor;
            foreach (var hit in Physics.RaycastAll(from, Vector3.down, 2.6f, notStepBlockers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider is CharacterController || hit.transform.IsChildOf(root) || hit.distance >= best) continue;
                best = hit.distance;
                y = hit.point.y;
            }
            // Some furniture (Loop 2's kitchen counters and tables) has no collider: use the top of what you see of it.
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if (r.transform.IsChildOf(root) || r.GetComponent<Collider>() != null) continue;
                var b = r.bounds;
                if (b.size.x < 0.05f || b.size.z < 0.05f || b.size.y > 2.2f || b.size.x > 4f || b.size.z > 4f) continue;   // pictures, notes, walls, floors
                if (b.max.y >= from.y || b.max.y <= y) continue;
                if (from.x < b.min.x || from.x > b.max.x || from.z < b.min.z || from.z > b.max.z) continue;
                y = b.max.y;
            }
            return y;
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;

        // ------------------------------------------------------------------ materials

        /// <summary>The pack's materials use the Built-in Standard shader; give them URP's Lit with the same colours.</summary>
        private static void ConvertPackMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { PackFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == lit) continue;
                var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                var emission = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
                bool transparent = path.Contains("/Transparent/") || path.Contains("GLASS");
                m.shader = lit;
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", transparent ? 0.6f : 0.12f);
                m.SetFloat("_Metallic", 0f);
                if (path.Contains("/Emission/"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emission.maxColorComponent > 0f ? emission : color);
                }
                if (transparent)
                {
                    if (color.a > 0.9f) color.a = 0.35f;
                    m.SetColor("_BaseColor", color);
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite", 0f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.renderQueue = (int)RenderQueue.Transparent;
                }
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
