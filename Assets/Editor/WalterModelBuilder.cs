using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Turns the free "PS1 Humanoid Base Model" by ELMatero6 (CGTrader, Royalty Free licence;
    /// Assets/Characters/Walter/Walter_PS1.fbx) into Walter, aged seventy-two.
    /// The model came with Blender's default cube UVs, every face stacked on the others, so:
    ///   1. each part (body, head, upper arms, forearms) is copied with fresh, non-overlapping UVs
    ///      from Unity's unwrapper (bone weights kept) into Assets/Characters/Walter/Meshes;
    ///   2. Tools/walter_textures.py paints a low-resolution texture for each part from the rest-pose
    ///      position of every texel: grey hair round a bald crown, an old face, a cardigan over a
    ///      shirt, slacks and slippers, in 15-bit colour like the PS1;
    ///   3. the Walter prefab is built: the model scaled to 1.75 m, turned to face +Z, with those
    ///      meshes and point-filtered materials, and his arms lowered from the T-pose to his sides;
    ///   4. (Put Walter In Every Loop) the prefab goes under the player in each loop scene, with a
    ///      PlayerBody that shows him only when the camera pulls away for the loop shot.
    /// Menu: Left Behind > Walter > Build Walter. Needs Python on the PATH for step 2.
    /// </summary>
    public static class WalterModelBuilder
    {
        private const string Folder = "Assets/Characters/Walter";
        private const string ModelPath = Folder + "/Walter_PS1.fbx";
        private const string MeshFolder = Folder + "/Meshes";
        private const string TextureFolder = Folder + "/Textures";
        private const string MaterialFolder = Folder + "/Materials";
        private const string PrefabPath = Folder + "/Walter.prefab";
        private const string MeshDump = "Library/WalterMesh.json";
        private const float Height = 1.75f;
        private const float ArmDrop = 72f;      // degrees from the T-pose down to his sides
        private const string BodyName = "WalterBody";

        private static readonly string[] LoopScenes =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Loop2_YearsEarlier.unity",
            "Assets/Scenes/Loop3_TheAftermath.unity",
        };

        /// <summary>Renderer name in the FBX, our part name, texture size.</summary>
        private static readonly (string renderer, string part, int size)[] Parts =
        {
            ("Body", "Body", 256),
            ("Haed", "Head", 256),
            ("LBicep", "UpperArmL", 128),
            ("RBicept", "UpperArmR", 128),
            ("LHand", "ForearmL", 128),
            ("RHand", "ForearmR", 128),
        };

        [MenuItem("Left Behind/Walter/Build Walter")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) { Debug.LogError("Walter: no model at " + ModelPath); return; }
            var meshes = Unwrap(model);
            if (!PaintTextures()) return;
            var materials = Materials();
            BuildPrefab(model, meshes, materials);
            AssetDatabase.SaveAssets();
            Debug.Log("Walter: built " + PrefabPath);
        }

        // ------------------------------------------------------------------ 1. UVs

        private static Dictionary<string, Mesh> Unwrap(GameObject model)
        {
            Directory.CreateDirectory(MeshFolder);
            var result = new Dictionary<string, Mesh>();
            var json = new StringBuilder("{");
            var ci = CultureInfo.InvariantCulture;
            bool first = true;
            foreach (var (rendererName, part, size) in Parts)
            {
                var r = Find(model.transform, rendererName).GetComponent<Renderer>();
                var src = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>().sharedMesh;
                var mesh = Object.Instantiate(src);
                mesh.name = "Walter_" + part;
                UnwrapParam.SetDefaults(out var p);
                p.packMargin = 4f / size;
                Unwrapping.GenerateSecondaryUVSet(mesh, p);
                var uv = new List<Vector2>();
                mesh.GetUVs(1, uv);
                mesh.SetUVs(0, uv);
                mesh.SetUVs(1, (List<Vector2>)null);

                string path = $"{MeshFolder}/Walter_{part}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (old != null) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); mesh = old; }
                else AssetDatabase.CreateAsset(mesh, path);
                result[part] = mesh;

                // Rest-pose positions and normals, in the model's own space, for the painter.
                var v = mesh.vertices;
                var n = mesh.normals;
                if (!first) json.Append(',');
                first = false;
                json.Append($"\"{part}\":{{\"size\":{size},\"v\":[");
                for (int i = 0; i < v.Length; i++)
                {
                    var w = r.transform.TransformPoint(v[i]);
                    json.Append(i > 0 ? "," : "").Append(string.Format(ci, "[{0:F4},{1:F4},{2:F4}]", w.x, w.y, w.z));
                }
                json.Append("],\"n\":[");
                for (int i = 0; i < n.Length; i++)
                {
                    var w = r.transform.TransformDirection(n[i]).normalized;
                    json.Append(i > 0 ? "," : "").Append(string.Format(ci, "[{0:F3},{1:F3},{2:F3}]", w.x, w.y, w.z));
                }
                json.Append("],\"uv\":[");
                for (int i = 0; i < uv.Count; i++)
                    json.Append(i > 0 ? "," : "").Append(string.Format(ci, "[{0:F5},{1:F5}]", uv[i].x, uv[i].y));
                json.Append("],\"t\":[").Append(string.Join(",", mesh.triangles)).Append("]}");
            }
            json.Append('}');
            File.WriteAllText(MeshDump, json.ToString());
            AssetDatabase.SaveAssets();
            return result;
        }

        // ------------------------------------------------------------------ 2. textures

        private static bool PaintTextures()
        {
            Directory.CreateDirectory(TextureFolder);
            var info = new ProcessStartInfo("python", $"Tools/walter_textures.py \"{MeshDump}\" \"{TextureFolder}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
            };
            try
            {
                using var proc = Process.Start(info);
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode != 0) { Debug.LogError("Walter: painting the textures failed.\n" + error); return false; }
                if (output.Length > 0) Debug.Log(output);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Walter: could not run Python (Tools/walter_textures.py): " + e.Message);
                return false;
            }
            AssetDatabase.Refresh();
            return true;
        }

        // ------------------------------------------------------------------ 3. materials and prefab

        private static Dictionary<string, Material> Materials()
        {
            Directory.CreateDirectory(MaterialFolder);
            var result = new Dictionary<string, Material>();
            foreach (var (_, part, size) in Parts)
            {
                string tex = $"{TextureFolder}/Walter_{part}.png";
                var imp = (TextureImporter)AssetImporter.GetAtPath(tex);
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.maxTextureSize = size;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();

                string path = $"{MaterialFolder}/Walter_{part}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(m, path);
                }
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", part == "Head" ? 0.18f : 0.05f);
                EditorUtility.SetDirty(m);
                result[part] = m;
            }
            return result;
        }

        private static void BuildPrefab(GameObject model, Dictionary<string, Mesh> meshes, Dictionary<string, Material> materials)
        {
            var root = new GameObject("Walter");
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            body.name = "Model";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);     // the model faces -Z

            foreach (var (rendererName, part, _) in Parts)
            {
                var r = Find(body.transform, rendererName).GetComponent<Renderer>();
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.sharedMesh = meshes[part];
                    smr.updateWhenOffscreen = true;
                }
                else r.GetComponent<MeshFilter>().sharedMesh = meshes[part];
                r.sharedMaterial = materials[part];
            }

            LowerArms(body.transform);

            // Scale so the top of his head is at 1.75 m, feet on the ground.
            var bounds = Bounds(body);
            float scale = Height / bounds.size.y;
            body.transform.localScale = Vector3.one * scale;
            bounds = Bounds(body);
            body.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Turns each upper arm down about the model's front-to-back axis, so he stands with his arms at his sides.</summary>
        private static void LowerArms(Transform model)
        {
            foreach (var name in new[] { "Bone.003_L.001", "Bone.003_R.001" })
            {
                var bone = Find(model, name);
                // The bone names do not say which side is which, so look at where the arm points.
                float side = Mathf.Sign(model.InverseTransformPoint(bone.GetChild(0).position).x
                                        - model.InverseTransformPoint(bone.position).x);
                bone.rotation = Quaternion.AngleAxis(-side * ArmDrop, model.forward) * bone.rotation;
            }
        }

        // ------------------------------------------------------------------ 4. the player

        [MenuItem("Left Behind/Walter/Put Walter In Every Loop")]
        public static void PutInEveryLoop()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            foreach (var path in LoopScenes)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                PutOnPlayer();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(start)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(start, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        /// <summary>Puts Walter's body under the player in the open scene, replacing the Starter Assets capsule mesh.</summary>
        public static void PutOnPlayer()
        {
            var fpc = Object.FindAnyObjectByType<StarterAssets.FirstPersonController>();
            if (fpc == null) { Debug.LogWarning("Walter: no player in " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); return; }
            var player = fpc.transform;
            var old = player.Find(BodyName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // The placeholder capsule mesh the Starter Assets player is drawn with.
            foreach (var mr in player.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.GetComponent<MeshFilter>() != null && mr.GetComponent<MeshFilter>().sharedMesh != null
                    && mr.GetComponent<MeshFilter>().sharedMesh.name == "Capsule")
                    mr.enabled = false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var body = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player);
            body.name = BodyName;
            var cc = player.GetComponent<CharacterController>();
            float feet = cc != null ? cc.center.y - cc.height * 0.5f : 0f;
            body.transform.localPosition = new Vector3(0f, feet, 0f);
            body.transform.localRotation = Quaternion.identity;
            LeftBehind.CameraSystem.PlayerBody.FitTo(body.transform, 1f);     // at runtime it is sized to the loop's eye height
            body.AddComponent<LeftBehind.CameraSystem.PlayerBody>();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        }

        private static Bounds Bounds(GameObject go)
        {
            var b = new Bounds();
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                Mesh m = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>().sharedMesh;
                foreach (var v in m.vertices)
                {
                    var w = r.transform.TransformPoint(v);
                    if (!any) { b = new Bounds(w, Vector3.zero); any = true; }
                    else b.Encapsulate(w);
                }
            }
            return b;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            throw new System.Exception("Walter: no " + name + " in the model");
        }
    }
}
