using System.Collections.Generic;
using LeftBehind.Interaction;
using LeftBehind.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Creates Assets/Scenes/MainMenu.unity (a camera and the MainMenu script, which builds its own UI),
    /// makes it the first scene in the build, and saves Assets/Resources/HeldFlashlight.prefab from the
    /// Loop 1 flashlight so a loop begun from Chapters or restarted can hand Walter his flashlight.
    /// Menu: Left Behind > Build Main Menu.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FlashlightPrefab = "Assets/Resources/HeldFlashlight.prefab";

        [MenuItem("Left Behind/Build Main Menu")]
        public static void Build()
        {
            SaveFlashlightPrefab();
            ImportPhotos();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();
            new GameObject("MainMenu").AddComponent<MainMenu>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void SaveFlashlightPrefab()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var source = Object.FindAnyObjectByType<FlashlightPickup>();
            var copy = Object.Instantiate(source.gameObject);
            copy.name = "HeldFlashlight";
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            System.IO.Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(copy, FlashlightPrefab);
            Object.DestroyImmediate(copy);      // the Loop 1 scene itself is left unchanged and unsaved
        }

        private static void ImportPhotos()
        {
            foreach (var name in new[] { "FamilyPhoto_Clear", "FamilyPhoto_Blurred" })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath($"Assets/Resources/Menu/{name}.png");
                if (imp == null) continue;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }
    }
}
