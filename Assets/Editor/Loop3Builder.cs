using System.Collections.Generic;
using System.IO;
using LeftBehind.Interaction;
using LeftBehind.Loop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Builds Loop 3 ("the aftermath", the night of the crime) as Assets/Scenes/Loop3_TheAftermath.unity.
    /// It starts from a copy of the Loop 1 scene, because Loop 3 is the house's true layout, then makes it
    /// that night: the whole family's places set, every lamp on, a storm outside with lightning, and the
    /// props the story needs (knife block, clinic card, Emily's drawing, Sam's bear, the note on the study
    /// door, and the study itself). Rebuilding replaces the scene, so make Loop 3 changes here.
    /// Menu: Left Behind > Build Loop 3.
    /// </summary>
    public static class Loop3Builder
    {
        public const string ScenePath = "Assets/Scenes/Loop3_TheAftermath.unity";
        private const string SourcePath = "Assets/Scenes/SampleScene.unity";
        public const string PropsRoot = "Loop3Props";

        private static Transform props;

        [MenuItem("Left Behind/Build Loop 3")]
        public static void Build()
        {
            AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SourcePath, ScenePath)) throw new System.Exception("Could not copy " + SourcePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Loop 1 only: the flashlight Walter has already picked up (he carries it in).
            var flashlight = GameObject.Find("Flashlight");
            if (flashlight != null) Object.DestroyImmediate(flashlight);

            // The table is set for the whole family tonight.
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "FamilyPlaces") t.gameObject.SetActive(true);

            // Every lamp in the house is on.
            foreach (var lamp in Object.FindObjectsByType<LampSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(lamp);
                so.FindProperty("isOn").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Storm();
            props = new GameObject(PropsRoot).transform;
            Props();
            StoryContentBuilder.SetUpLoop3();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuild();
        }

        // ------------------------------------------------------------------ lighting

        private static void Storm()
        {
            var env = Object.FindFirstObjectByType<LoopEnvironment>();
            var so = new SerializedObject(env);
            so.FindProperty("loop").intValue = 3;
            so.FindProperty("period").stringValue = "The same night, a storm";
            so.FindProperty("sunEuler").vector3Value = new Vector3(40f, 200f, 0f);
            so.FindProperty("sunColor").colorValue = new Color(0.45f, 0.52f, 0.75f);
            so.FindProperty("sunIntensity").floatValue = 0.06f;
            so.FindProperty("ambient").colorValue = new Color(0.27f, 0.22f, 0.17f);
            so.FindProperty("fogColor").colorValue = new Color(0.035f, 0.035f, 0.05f);
            so.FindProperty("fogDensity").floatValue = 0.04f;
            so.ApplyModifiedPropertiesWithoutUndo();
            env.Apply();
            var sun = (Light)new SerializedObject(env).FindProperty("sun").objectReferenceValue;
            var lightning = env.gameObject.GetComponent<Lightning>() ?? env.gameObject.AddComponent<Lightning>();
            lightning.Configure(sun);

            // By day Loop 1 is lit through its windows; tonight only the lamps light the house.
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type != LightType.Point) continue;
                l.intensity *= 1.8f;
                l.range *= 1.3f;
            }
        }

        // ------------------------------------------------------------------ props

        private static void Props()
        {
            var cardboard = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Attic_Cardboard.mat");
            var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Exterior/Ext_DarkMetal.mat");
            var paper = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ceiling_Plaster.mat");

            // Knife block on the kitchen counter by the oven: three handles and one empty slot.
            var counter = Bounds(GameObject.Find("Kitchen_3Drawers"));
            var block = Cube("KnifeBlock", new Vector3(counter.center.x, counter.max.y + 0.11f, counter.max.z - 0.16f),
                new Vector3(0.18f, 0.22f, 0.12f), cardboard, true);
            for (int i = 0; i < 4; i++)
            {
                float z = -0.045f + i * 0.03f;
                if (i == 2)
                    Cube("EmptySlot", block.transform.position + new Vector3(0f, 0.1105f, z), new Vector3(0.05f, 0.002f, 0.012f), dark, false, block.transform);
                else
                    Cube("Handle", block.transform.position + new Vector3(0f, 0.15f, z), new Vector3(0.03f, 0.08f, 0.018f), dark, false, block.transform);
            }
            block.GetComponent<BoxCollider>().size = new Vector3(2.5f, 2.2f, 3f);

            // Ruth's memory clinic card on the fridge.
            var fridge = Bounds(GameObject.Find("Kitchen_Fridge"));
            var card = Cube("ClinicCard", new Vector3(fridge.center.x, 1.45f, fridge.max.z + 0.003f),
                new Vector3(0.16f, 0.1f, 0.004f), PaperMaterial("ClinicCard"), true);
            card.GetComponent<BoxCollider>().size = new Vector3(2.6f, 3f, 12f);

            // Emily's drawing, lying on the children's desk.
            var desk = Bounds(GameObject.Find("UpperFloor/Furniture/ChildrensRoom/Desk"));
            var drawing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            drawing.name = "EmilyDrawing";
            drawing.transform.SetParent(props, false);
            drawing.transform.position = new Vector3(desk.center.x, desk.max.y + 0.004f, desk.center.z);
            drawing.transform.rotation = Quaternion.Euler(90f, 10f, 0f);
            drawing.transform.localScale = new Vector3(0.36f, 0.25f, 1f);
            Object.DestroyImmediate(drawing.GetComponent<MeshCollider>());
            drawing.AddComponent<BoxCollider>().size = new Vector3(1.4f, 1.6f, 0.6f);
            drawing.GetComponent<Renderer>().sharedMaterial = PaperMaterial("EmilyDrawing");
            drawing.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Sam's bear at the top of the stairs.
            var bear = new GameObject("TeddyBear");
            bear.transform.SetParent(props, false);
            bear.transform.position = new Vector3(7.2f, 2.6f, 4.85f);
            bear.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            bear.transform.localScale = Vector3.one * 1.4f;
            var plush = Tinted("Plush", new Color(0.55f, 0.37f, 0.22f));
            Sphere("Body", bear.transform, new Vector3(0f, 0.11f, 0f), new Vector3(0.2f, 0.22f, 0.17f), plush);
            Sphere("Head", bear.transform, new Vector3(0f, 0.28f, 0.01f), new Vector3(0.15f, 0.14f, 0.14f), plush);
            Sphere("EarL", bear.transform, new Vector3(-0.06f, 0.35f, 0f), Vector3.one * 0.05f, plush);
            Sphere("EarR", bear.transform, new Vector3(0.06f, 0.35f, 0f), Vector3.one * 0.05f, plush);
            Sphere("ArmL", bear.transform, new Vector3(-0.11f, 0.14f, 0.03f), new Vector3(0.06f, 0.11f, 0.06f), plush);
            Sphere("ArmR", bear.transform, new Vector3(0.11f, 0.14f, 0.03f), new Vector3(0.06f, 0.11f, 0.06f), plush);
            Sphere("LegL", bear.transform, new Vector3(-0.06f, 0.03f, 0.07f), new Vector3(0.07f, 0.06f, 0.1f), plush);
            Sphere("LegR", bear.transform, new Vector3(0.06f, 0.03f, 0.07f), new Vector3(0.07f, 0.06f, 0.1f), plush);
            Sphere("Nose", bear.transform, new Vector3(0f, 0.27f, 0.075f), Vector3.one * 0.03f, dark);
            var bc = bear.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.2f, 0f);
            bc.size = new Vector3(0.45f, 0.5f, 0.45f);

            // The note taped to the study door, on the landing side. It swings with the door.
            var studyDoor = GameObject.Find("UpperFloor/Doors/Door_Study");
            var note = Cube("StudyDoorNote", new Vector3(2.5f, 2.6f + 1.45f, 2.124f), new Vector3(0.21f, 0.28f, 0.004f),
                PaperMaterial("NoteScrawl"), true);
            note.transform.SetParent(studyDoor.transform, true);
            note.GetComponent<BoxCollider>().size = new Vector3(1.3f, 1.2f, 2f);

            // Inside the study: what is left of that night. Nothing more is shown.
            var rug = Bounds(GameObject.Find("UpperFloor/Furniture/Study/Rug"));
            var stain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stain.name = "Stain";
            stain.transform.SetParent(props, false);
            stain.transform.position = new Vector3(rug.center.x - 0.2f, rug.max.y + 0.002f, rug.center.z + 0.2f);
            stain.transform.localScale = new Vector3(1.1f, 0.001f, 0.75f);
            Object.DestroyImmediate(stain.GetComponent<Collider>());
            stain.GetComponent<Renderer>().sharedMaterial = StainMaterial();
            stain.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var knifeAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Furniture/Knife.fbx");
            var knife = (GameObject)PrefabUtility.InstantiatePrefab(knifeAsset, props);
            knife.name = "Knife";
            knife.transform.rotation = Quaternion.Euler(0f, 35f, 0f) * knifeAsset.transform.rotation;
            knife.transform.localScale = knifeAsset.transform.localScale * 0.6f;
            knife.transform.position = Vector3.zero;
            var kb = Bounds(knife);
            knife.transform.position += new Vector3(rug.center.x + 0.45f - kb.center.x, rug.max.y + 0.003f - kb.min.y, rug.center.z + 0.5f - kb.center.z);

            // The desk chair lies on its side.
            var chair = GameObject.Find("UpperFloor/Furniture/Study/DeskChair");
            if (chair != null)
            {
                chair.transform.rotation = Quaternion.Euler(0f, 0f, 90f) * chair.transform.rotation;
                var cb = Bounds(chair);
                chair.transform.position += new Vector3(0f, 2.6f - cb.min.y, -0.4f);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject Cube(string name, Vector3 pos, Vector3 size, Material mat, bool collider, Transform parent = null)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = name;
            c.transform.SetParent(parent != null ? parent : props, true);
            c.transform.position = pos;
            c.transform.localScale = parent != null
                ? new Vector3(size.x / parent.lossyScale.x, size.y / parent.lossyScale.y, size.z / parent.lossyScale.z)
                : size;
            c.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(c.GetComponent<Collider>());
            return c;
        }

        private static void Sphere(string name, Transform parent, Vector3 local, Vector3 size, Material mat)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            s.transform.SetParent(parent, false);
            s.transform.localPosition = local;
            s.transform.localScale = size;
            s.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(s.GetComponent<Collider>());
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>A paper material from Assets/Story/Textures/{name}.png, faintly self-lit so it reads in the dark.</summary>
        private static Material PaperMaterial(string name)
        {
            string path = $"Assets/Story/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            string tex = $"Assets/Story/Textures/{name}.png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(tex);
            imp.filterMode = FilterMode.Point;
            imp.maxTextureSize = 512;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            Directory.CreateDirectory("Assets/Story/Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.05f);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
            m.SetColor("_EmissionColor", new Color(0.18f, 0.18f, 0.18f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material Tinted(string name, Color color)
        {
            string path = $"Assets/Story/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            Directory.CreateDirectory("Assets/Story/Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material StainMaterial()
        {
            const string path = "Assets/Story/Materials/Stain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            Directory.CreateDirectory("Assets/Story/Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.17f, 0.04f, 0.035f));
            m.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void AddToBuild()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == ScenePath)) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
