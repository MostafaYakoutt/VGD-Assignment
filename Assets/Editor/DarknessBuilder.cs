using System.Collections.Generic;
using LeftBehind.Interaction;
using LeftBehind.Loop;
using LeftBehind.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Makes the dark something the player has to deal with, not decoration:
    ///   Loop 1  a darker winter afternoon (less daylight, darker ambient). Only the lamps on the
    ///           way to the flashlight are on: the wall lamp by the front door, the reading lamp,
    ///           the two standing lamps by the dining room and one bulb in the attic. Upstairs is dark.
    ///           Once Walter has the flashlight, the standing lamps go out as he heads for the kitchen.
    ///   Loop 3  walking the landing towards the children's room (and the music box), the lamps
    ///           ahead go out: the children's room, the far end of the landing, then the one overhead.
    /// The flashlight's flicker near things that are wrong is set up at runtime (FlickerSource).
    /// Builds a "Darkness" object under the StoryProps root. Called by StoryPropsBuilder;
    /// Menu: Left Behind > Story > Place Darkness (for the open loop scene).
    /// </summary>
    public static class DarknessBuilder
    {
        private const string RootName = "Darkness";

        // Loop 1 lamps that stay on, by where their light is.
        private static readonly Vector3 FrontDoorWallLamp = new Vector3(-9.20f, 2.00f, -8.54f);
        private static readonly Vector3 ReadingLamp = new Vector3(-6.33f, 0.97f, -2.33f);
        private static readonly Vector3 DiningStandLamp = new Vector3(-0.64f, 1.51f, 9.12f);
        private static readonly Vector3 KitchenStandLamp = new Vector3(0.65f, 1.51f, 9.12f);
        private static readonly Vector3 AtticCentreBulb = new Vector3(0.00f, 7.37f, 0.00f);

        // Loop 3 lamps on the way to the children's room.
        private static readonly Vector3 ChildrensRoomLamp = new Vector3(-4.90f, 4.62f, -2.30f);
        private static readonly Vector3 LandingWestLamp = new Vector3(-6.50f, 4.62f, 1.10f);
        private static readonly Vector3 LandingMiddleLamp = new Vector3(-1.60f, 4.62f, 1.10f);

        [MenuItem("Left Behind/Story/Place Darkness")]
        public static void PlaceInOpenScene()
        {
            var env = Object.FindAnyObjectByType<LoopEnvironment>();
            if (env == null) { Debug.LogError("Darkness: this scene has no LoopEnvironment."); return; }
            var props = GameObject.Find("StoryProps");
            if (props == null) { Debug.LogError("Darkness: no StoryProps in this scene; run Place Story Props first."); return; }
            Place(props.transform, env.Loop);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        public static void Place(Transform parent, int loop)
        {
            var old = parent.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);

            switch (loop)
            {
                case 1:
                    DarkenLoop1();
                    // In the dining room, heading for the kitchen with the flashlight.
                    GoOut(root, "DiningLampsGoOut", "hasFlashlight", "lampsOutLoop1",
                        new Bounds(new Vector3(-1.7f, 1.25f, 5.15f), new Vector3(3.0f, 2.5f, 9.3f)),
                        DiningStandLamp, KitchenStandLamp);
                    break;
                case 3:
                    // On the landing, walking west towards the children's room.
                    GoOut(root, "LandingLampsGoOut", "", "lampsOutLoop3",
                        new Bounds(new Vector3(-0.25f, 3.6f, 1.12f), new Vector3(4.5f, 2f, 1.95f)),
                        ChildrensRoomLamp, LandingWestLamp, LandingMiddleLamp);
                    break;
            }
        }

        private static void DarkenLoop1()
        {
            var env = Object.FindAnyObjectByType<LoopEnvironment>();
            var so = new SerializedObject(env);
            // Was sun 1, ambient (0.12, 0.13, 0.16). Darker than this and the hall Walter starts in,
            // which has no lamp, is black before he finds the flashlight.
            so.FindProperty("sunIntensity").floatValue = 0.45f;
            so.FindProperty("ambient").colorValue = new Color(0.075f, 0.08f, 0.1f);
            so.FindProperty("fogColor").colorValue = new Color(0.07f, 0.08f, 0.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            env.Apply();

            var keepOn = new List<LampSwitch>();
            foreach (var p in new[] { FrontDoorWallLamp, ReadingLamp, DiningStandLamp, KitchenStandLamp, AtticCentreBulb })
            {
                var l = LampAt(p);
                if (l != null) keepOn.Add(l);
            }
            int off = 0;
            foreach (var lamp in Object.FindObjectsByType<LampSwitch>(FindObjectsInactive.Include))
            {
                bool on = keepOn.Contains(lamp);
                var lso = new SerializedObject(lamp);
                lso.FindProperty("isOn").boolValue = on;
                lso.ApplyModifiedPropertiesWithoutUndo();
                if (!on) off++;
            }
            Debug.Log($"Darkness: Loop 1 darkened; {keepOn.Count} lamps left on, {off} off.");
        }

        private static void GoOut(Transform root, string name, string flag, string done, Bounds area, params Vector3[] lampLights)
        {
            var lamps = new List<LampSwitch>();
            foreach (var p in lampLights)
            {
                var l = LampAt(p);
                if (l != null) lamps.Add(l);
            }
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<LampsGoOut>().Configure(lamps.ToArray(), area, flag, done);
        }

        /// <summary>The lamp whose light is at <paramref name="lightPosition"/>.</summary>
        private static LampSwitch LampAt(Vector3 lightPosition)
        {
            foreach (var lamp in Object.FindObjectsByType<LampSwitch>(FindObjectsInactive.Include))
                foreach (var light in lamp.GetComponentsInChildren<Light>(true))
                    if (Vector3.Distance(light.transform.position, lightPosition) < 0.3f) return lamp;
            Debug.LogWarning($"Darkness: no lamp with its light at {lightPosition}.");
            return null;
        }
    }
}
