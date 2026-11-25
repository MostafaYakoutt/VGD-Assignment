using System.Collections.Generic;
using System.IO;
using LeftBehind.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Gives every interactable the flashlight's kind of outline, fainter. For each visible mesh
    /// under an interactable it adds a "Glow" child with two copies of the mesh: a Mask that marks
    /// the object's own pixels (LeftBehind/OutlineMask) and a Hull with smoothed normals, pushed out
    /// by the outline shader, that draws the line around them (LeftBehind/Outline). The smoothed
    /// meshes are saved as assets, because most of the furniture meshes cannot be read at runtime
    /// in a build. Safe to run again: it replaces the glow it made before.
    /// Run it again after a builder recreates the interactables.
    /// </summary>
    public static class InteractableGlowBuilder
    {
        private const string GlowName = "Glow";
        private const string MaterialPath = "Assets/Materials/InteractableOutline.mat";
        private const string MaskPath = "Assets/Materials/PickupOutlineMask.mat";
        private const string MeshFolder = "Assets/Materials/GlowMeshes";

        /// <summary>The flashlight's line is opaque white; this one is fainter.</summary>
        private static readonly Color GlowColor = new Color(1f, 1f, 1f, 0.35f);

        private static readonly string[] LoopScenes =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Loop2_YearsEarlier.unity",
            "Assets/Scenes/Loop3_TheAftermath.unity",
        };

        [MenuItem("Left Behind/Story/Add Interactable Glow")]
        public static void AddToOpenScene()
        {
            int n = Build(out int meshes);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"Interactable glow: {n} interactables outlined ({meshes} meshes) in the open scene.");
        }

        [MenuItem("Left Behind/Story/Add Interactable Glow To All Loops")]
        public static void AddToAllLoops()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            foreach (var path in LoopScenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int n = Build(out int meshes);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Interactable glow: {n} interactables outlined ({meshes} meshes) in {scene.name}.");
            }
            if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start, OpenSceneMode.Single);
        }

        private static int Build(out int meshCount)
        {
            var outline = OutlineMaterial();
            var mask = AssetDatabase.LoadAssetAtPath<Material>(MaskPath);
            var smoothCache = new Dictionary<Mesh, Mesh>();
            int count = 0;
            meshCount = 0;

            foreach (var it in Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include))
            {
                if (it is FlashlightPickup) continue;          // has its own, brighter outline

                RemoveOld(it);
                var skip = Excluded(it);
                var candidates = new List<MeshRenderer>();
                foreach (var r in it.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.GetComponentInParent<Interactable>(true) != it) continue;   // belongs to a nested interactable
                    if (!r.enabled || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;
                    if (skip.Contains(r.transform)) continue;
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    candidates.Add(r);
                }
                // A cut-out texture on a quad (the clock face) would be outlined as a square: leave those
                // to the solid parts round them, unless the object has nothing else.
                bool anySolid = candidates.Exists(r => !CutOut(r));
                var pieces = new List<GameObject>();
                foreach (var r in candidates)
                {
                    if (anySolid && CutOut(r)) continue;
                    var mf = r.GetComponent<MeshFilter>();

                    var glow = new GameObject(GlowName);
                    glow.layer = r.gameObject.layer;
                    glow.transform.SetParent(r.transform, false);
                    int subs = mf.sharedMesh.subMeshCount;
                    Piece(glow.transform, "Mask", mf.sharedMesh, mask, subs);
                    Piece(glow.transform, "Hull", Smoothed(mf.sharedMesh, smoothCache), outline, subs);
                    pieces.Add(glow);
                    meshCount++;
                }
                if (pieces.Count == 0) continue;

                var component = it.GetComponent<InteractableGlow>();
                if (component == null) component = Undo.AddComponent<InteractableGlow>(it.gameObject);
                component.Configure(it, pieces.ToArray());
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                count++;
            }
            AssetDatabase.SaveAssets();
            return count;
        }

        /// <summary>Glow made by an earlier run, so running again replaces rather than doubles it.</summary>
        private static void RemoveOld(Interactable it)
        {
            var old = new List<GameObject>();
            foreach (var t in it.GetComponentsInChildren<Transform>(true))
                if (t.name == GlowName && t.Find("Hull") != null && t.Find("Mask") != null) old.Add(t.gameObject);
            foreach (var go in old) Object.DestroyImmediate(go);
        }

        private static bool CutOut(Renderer r)
        {
            foreach (var m in r.sharedMaterials)
                if (m != null && (m.IsKeywordEnabled("_ALPHATEST_ON")
                                  || (m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f)
                                  || (m.renderQueue >= 2450 && m.renderQueue < 2500)))
                    return true;
            return false;
        }

        /// <summary>Meshes that should not be outlined: the kitchen tap's water.</summary>
        private static HashSet<Transform> Excluded(Interactable it)
        {
            var skip = new HashSet<Transform>();
            if (it is KitchenTap)
            {
                var so = new SerializedObject(it);
                foreach (var field in new[] { "stream", "drop" })
                    if (so.FindProperty(field).objectReferenceValue is Transform t)
                        foreach (var c in t.GetComponentsInChildren<Transform>(true)) skip.Add(c);
            }
            return skip;
        }

        private static void Piece(Transform parent, string name, Mesh mesh, Material material, int subMeshes)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            var mats = new Material[subMeshes];
            for (int i = 0; i < subMeshes; i++) mats[i] = material;
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            r.allowOcclusionWhenDynamic = false;
        }

        private static Material OutlineMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (m == null)
            {
                m = new Material(Shader.Find("LeftBehind/Outline"));
                AssetDatabase.CreateAsset(m, MaterialPath);
            }
            m.SetColor("_OutlineColor", GlowColor);
            m.SetFloat("_OutlineWidth", 4f);          // the thinnest line that stays unbroken at render scale 0.4
            m.SetFloat("_StencilRef", 64f);           // the same ref the mask writes
            m.SetFloat("_DepthPull", 0.12f);          // shows round doors in their frames and things against walls
            m.renderQueue = 2010;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// A copy of the mesh with the directions the outline is pushed along: each vertex's normal
        /// averaged over every vertex at the same position (so the line stays closed at hard edges),
        /// plus the direction out from the mesh's centre (so flat things facing the camera, like the
        /// clock face, a note or a photo, still get a line all round). Saved as an asset, and
        /// rewritten in place on every run so the scenes keep their references.
        /// </summary>
        private static Mesh Smoothed(Mesh source, Dictionary<Mesh, Mesh> cache)
        {
            if (cache.TryGetValue(source, out var done)) return done;

            string id = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long local)
                ? guid.Substring(0, 8) + "_" + local : "scene_" + source.vertexCount;
            string file = $"{MeshFolder}/{Safe(source.name)}_{id}_Glow.asset";

            var verts = source.vertices;
            var normals = source.normals;
            var sums = new Dictionary<Vector3Int, Vector3>();
            var keys = new Vector3Int[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var k = Vector3Int.RoundToInt(verts[i] * 10000f);
                keys[i] = k;
                var n = normals.Length == verts.Length ? normals[i] : Vector3.zero;
                sums[k] = sums.TryGetValue(k, out var s) ? s + n : n;
            }
            var centre = source.bounds.center;
            var smooth = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
                smooth[i] = (sums[keys[i]].normalized + (verts[i] - centre).normalized).normalized;

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(file);
            var mesh = existing != null ? existing : new Mesh();
            mesh.Clear();
            mesh.name = Path.GetFileNameWithoutExtension(file);
            mesh.indexFormat = source.indexFormat;
            mesh.vertices = verts;
            mesh.normals = smooth;
            mesh.subMeshCount = source.subMeshCount;
            for (int s = 0; s < source.subMeshCount; s++) mesh.SetTriangles(source.GetTriangles(s), s);
            mesh.RecalculateBounds();

            if (existing != null) EditorUtility.SetDirty(mesh);
            else
            {
                if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/Materials", "GlowMeshes");
                AssetDatabase.CreateAsset(mesh, file);
            }
            cache[source] = mesh;
            return mesh;
        }

        private static string Safe(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
