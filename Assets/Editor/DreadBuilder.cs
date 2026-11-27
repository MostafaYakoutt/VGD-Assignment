using System.Collections.Generic;
using System.IO;
using LeftBehind.Interaction;
using LeftBehind.Loop;
using LeftBehind.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Voice = LeftBehind.Story.DialogueData.Voice;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// The dread pass: what makes each loop more unsettling, paced so that every scare comes after
    /// a long quiet (see ScarePacing and PacedScare). Per loop:
    ///   Loop 1  footsteps crossing the landing above him once he has the flashlight; a door closing
    ///           out of sight; the knife on the table and a reading chair moving behind his back;
    ///           three shapes under dust sheets that are gone when he looks back; after the attic key,
    ///           a mannequin standing on the landing behind him, once; a hall mirror that shows the
    ///           room behind him but not him; a false alarm at the front door; thicker fog in the attic.
    ///   Loop 2  small footsteps running across the landing in the dark; a door closing; the flashlight
    ///           dying for a moment; a chair at the table for six pulled out and the bedroom door shut
    ///           behind him; a false alarm: Ruth's dress form standing at the end of the corridor.
    ///   Loop 3  heavy footsteps in the attic overhead; a door closing; a family chair turned away and
    ///           the bedroom door shut behind him; the figure at the end of the landing in a lightning
    ///           flash; a false alarm: a branch tapping on a window; thicker attic fog; a darker
    ///           vignette and more film grain; the note on the study door pushes in as it is read.
    /// Things that are wrong tilt the view when examined (FeelsWrong). The room tone, Walter's
    /// breathing, the flickering lamps and the music box are set up at runtime.
    /// Builds a "Dread" object under the StoryProps root, replacing the last one.
    /// Menu: Left Behind > Story > Place Dread (open loop) / Place Dread In Every Loop.
    /// </summary>
    public static class DreadBuilder
    {
        private const string RootName = "Dread";
        private const string MeshPath = "Assets/Story/Meshes/SheetedFigure.asset";
        private const string SheetMaterialPath = "Assets/Story/Materials/DustSheet.mat";
        private const string MirrorMaterialPath = "Assets/Story/Materials/MirrorGlass.mat";
        private const string Loop3ProfilePath = "Assets/Settings/Loop3Profile.asset";
        private const string MannequinModel = "Assets/Characters/Mannequin/Mannequin_Ch36.fbx";
        private const string MannequinController = "Assets/Characters/Mannequin/Mannequin.controller";
        private const string WalterPrefab = "Assets/Characters/Walter/Walter.prefab";

        private static readonly string[] LoopScenes =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Loop2_YearsEarlier.unity",
            "Assets/Scenes/Loop3_TheAftermath.unity",
        };

        // The house, by floor (floors at 0.05, 2.6 and, for the attic, 5.15).
        private static readonly Bounds GroundFloor = new Bounds(new Vector3(0f, 1.2f, 0f), new Vector3(20f, 2.4f, 20f));
        private static readonly Bounds UpperFloor = new Bounds(new Vector3(0f, 3.7f, 0f), new Vector3(20f, 2.2f, 20f));
        private static readonly Bounds Attic = new Bounds(new Vector3(0.4f, 6.4f, 0f), new Vector3(20f, 3f, 19.6f));
        private static readonly Bounds MasterBedroom = new Bounds(new Vector3(-5.6f, 3.6f, 6.1f), new Vector3(7.6f, 2f, 7f));

        [MenuItem("Left Behind/Story/Place Dread")]
        public static void PlaceInOpenScene()
        {
            var env = Object.FindAnyObjectByType<LoopEnvironment>();
            if (env == null) { Debug.LogError("Dread: this scene has no LoopEnvironment."); return; }
            var props = GameObject.Find("StoryProps");
            if (props == null) { Debug.LogError("Dread: no StoryProps in this scene; run Place Story Props first."); return; }
            Place(props.transform, env.Loop);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("Left Behind/Story/Place Dread In Every Loop")]
        public static void PlaceInEveryLoop()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            foreach (var path in LoopScenes)
            {
                EditorSceneManager.OpenScene(path);
                PlaceInOpenScene();
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start);
        }

        public static void Place(Transform parent, int loop)
        {
            var old = parent.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);
            Physics.SyncTransforms();

            switch (loop)
            {
                case 1: Loop1(root); break;
                case 2: Loop2(root); break;
                case 3: Loop3(root); break;
            }
        }

        // ------------------------------------------------------------------ Loop 1

        private static void Loop1(Transform root)
        {
            // Sound: someone crossing the landing above, as he decides to go upstairs.
            var steps = Scare<PhantomFootsteps>(root, "Footsteps_Landing");
            steps.ConfigureSteps(Line(new Vector3(2.2f, 2.7f, 1.1f), new Vector3(-3.0f, 2.7f, 0.8f), 0.65f, new Vector3(-3.45f, 2.7f, 0.35f)),
                0.62f, 0.86f, 1f, 600f);
            steps.ConfigurePacing("hasFlashlight", "", "heardFootstepsLoop1", GroundFloor, 25f, 3f,
                StoryContentBuilder.D("L1_FootstepsUpstairs",
                    (Voice.Inner, "", "Someone's upstairs."),
                    (Voice.Inner, "", "Ruth? ...Is that you?")));

            DoorCloses(root, "sawEmptyTable", "doorClosedLoop1", 50f);

            // Pacing: one false alarm. The front door rattles, as if someone is trying it.
            var alarm = Scare<FalseAlarm>(root, "FalseAlarm_FrontDoor");
            alarm.ConfigureSound(FalseAlarm.Sound.DoorRattle, new[] { new Vector3(9.33f, 1.0f, -3.32f) }, 3.5f, 2);
            alarm.ConfigurePacing("introSeen", "", "falseAlarmLoop1", GroundFloor, 35f, 3.5f,
                StoryContentBuilder.D("L1_FalseAlarm",
                    (Voice.Inner, "", "Someone's at the door."),
                    (Voice.Inner, "", "...No. It's the wind. It's only the wind.")));

            // More that moves while he isn't looking.
            var knife = GameObject.Find("Knife");
            if (knife != null) TurnAbout(root, "Knife", knife.transform, 100f, "sawEmptyTable", "changed_L1_Knife", 2f, 9f);
            var chair = GameObject.Find("Furniture_Added/Living/ReadingChair_A");
            if (chair != null) TurnAbout(root, "ReadingChair", chair.transform, 160f, "hasFlashlight", "changed_L1_ReadingChair", 3f, 11f);

            // Shapes under dust sheets that are gone when he looks back.
            int sheets = 0;
            foreach (var spot in new[]
            {
                new Vector3(-1.3f, 0.05f, -6.6f),     // living room, by the corridor doorway
                new Vector3(-9.0f, 0.05f, 8.9f),      // dining room, the far corner
                new Vector3(-1.2f, 0.05f, 8.8f),      // dining room, the other corner
                new Vector3(-8.9f, 2.62f, 8.9f),      // master bedroom, the corner
                new Vector3(-0.6f, 2.62f, 8.9f),      // master bedroom, by the window
                new Vector3(-9.0f, 0.05f, -6.6f),     // living room, by the window
                new Vector3(-8.9f, 2.62f, -4.6f),     // children's room, the corner
            })
            {
                if (sheets >= 3) break;
                if (!Clear(spot, 0.45f)) { Debug.Log($"Dread: dust sheet spot {spot} is taken; skipped."); continue; }
                var facing = new Vector3(0f, spot.x < -5f ? 90f : -90f, 0f);
                var sheet = SheetedFigure(root, "SheetedShape_" + sheets, spot, Quaternion.Euler(facing) * Quaternion.Euler(0f, 17f * sheets - 15f, 0f));
                sheet.AddComponent<Apparition>().Configure(sheet, true, "", default, 0f, 0f, "sheetGone_L1_" + sheets, false);
                sheets++;
            }

            // A mannequin on the landing behind him, once, after he takes the key from the attic.
            var mannequin = StillMannequin(root, "Mannequin_Landing", new Vector3(5.3f, 2.62f, 1.15f), Quaternion.Euler(0f, -90f, 0f));
            if (mannequin != null)
                mannequin.AddComponent<Apparition>().Configure(mannequin, false, "hasAtticKey",
                    new Bounds(new Vector3(-4.2f, 3.6f, 1.1f), new Vector3(11.6f, 2.2f, 2.2f)), 4f, 15f,
                    "mannequinOnLanding", true, quiet: 8f);

            // The hall mirror, over the table with the family photo.
            Mirror(root, new Vector3(-9.585f, 1.33f, -8.48f), Quaternion.Euler(0f, 90f, 0f));

            Fog(root, Attic, 2.3f);
            Wrong("SM_frame_01", "StoryProps/Pills", "Story/TeddyBear");
        }

        // ------------------------------------------------------------------ Loop 2

        private static void Loop2(Transform root)
        {
            // Small feet running across the landing in the dark: the children are away.
            var steps = Scare<PhantomFootsteps>(root, "Footsteps_Child");
            steps.ConfigureSteps(new[]
                {
                    new Vector3(-5.0f, 2.7f, -2.6f), new Vector3(-4.4f, 2.7f, -1.7f), new Vector3(-3.7f, 2.7f, -0.8f),
                    new Vector3(-3.45f, 2.7f, 0.3f), new Vector3(-2.6f, 2.7f, 1.0f), new Vector3(-1.7f, 2.7f, 1.1f),
                    new Vector3(-0.8f, 2.7f, 1.1f), new Vector3(0.1f, 2.7f, 1.1f), new Vector3(1.0f, 2.7f, 1.1f),
                    new Vector3(1.6f, 2.7f, 1.4f),
                },
                0.36f, 1.3f, 0.75f, 700f);
            steps.ConfigurePacing("hasFlashlightLoop2", "", "heardFootstepsLoop2", GroundFloor, 25f, 3f,
                StoryContentBuilder.D("L2_FootstepsUpstairs",
                    (Voice.Inner, "", "Sam?"),
                    (Voice.Inner, "", "...You're meant to be at your grandmother's.")));

            DoorCloses(root, "hasFuse", "doorClosedLoop2", 40f);

            // The flashlight dies for a moment while the power is out.
            var dies = Scare<FlashlightDies>(root, "FlashlightDies");
            dies.ConfigureDark(3f);
            dies.ConfigurePacing("hasFlashlightLoop2", "powerRestored", "flashlightDiedLoop2", GroundFloor, 30f, 2.5f,
                StoryContentBuilder.D("L2_FlashlightDies",
                    (Voice.Inner, "", "Come on. Come on..."),
                    (Voice.Inner, "", "There.")));

            // Pacing: one false alarm. A figure at the end of the corridor: Ruth's dress form.
            var form = StillMannequin(root, "DressForm", new Vector3(-8.85f, 0.05f, -8.55f), Quaternion.Euler(0f, 90f, 0f), 0.08f);
            if (form != null)
            {
                var alarm = Scare<FalseAlarm>(root, "FalseAlarm_DressForm");
                alarm.ConfigureFigure(form.transform, 7.5f);
                alarm.ConfigurePacing("hasFlashlightLoop2", "", "falseAlarmLoop2",
                    new Bounds(new Vector3(-2f, 1.2f, -8.45f), new Vector3(15.5f, 2.4f, 2.6f)), 20f, 3f,
                    StoryContentBuilder.D("L2_DressForm",
                        (Voice.Inner, "", "Ruth?"),
                        (Voice.Inner, "", "...Her dress form. She leaves it everywhere.")));
            }

            var chair = GameObject.Find("Loop2House/Furniture/Dining/Chair_0");
            if (chair != null) PullOut(root, "Chair", chair.transform, new Vector3(-4.9f, 0.5f, 5.0f), "sawTableForSix", "changed_L2_Chair");
            DoorShuts(root, "Door_MasterBedroom", MasterBedroom, "changed_L2_BedroomDoor");

            Wrong("StoryProps/Pills");
        }

        // ------------------------------------------------------------------ Loop 3

        private static void Loop3(Transform root)
        {
            // Heavy footsteps across the attic, above him.
            var steps = Scare<PhantomFootsteps>(root, "Footsteps_Attic");
            steps.ConfigureSteps(Line(new Vector3(-4.0f, 5.25f, -4.0f), new Vector3(3.0f, 5.25f, 2.0f), 0.75f, new Vector3(3.5f, 5.25f, 2.6f)),
                0.8f, 0.78f, 1f, 500f);
            steps.ConfigurePacing("sawDrawing", "", "heardFootstepsLoop3", UpperFloor, 25f, 3f,
                StoryContentBuilder.D("L3_FootstepsAttic",
                    (Voice.Inner, "", "Someone's in the attic."),
                    (Voice.Inner, "", "Ruth? Ruth, answer me.")));

            DoorCloses(root, "loop3Arrived", "doorClosedLoop3", 45f);

            // The figure at the end of the landing, in a lightning flash, once the lamps have gone out.
            var figure = Silhouette(root, "LandingFigure", new Vector3(-9.0f, 2.62f, 1.2f), Quaternion.Euler(0f, 90f, 0f));
            if (figure != null)
            {
                var flash = Scare<LightningFigure>(root, "LightningFigure");
                flash.ConfigureFigure(figure, 5f, 15f);
                flash.ConfigurePacing("lampsOutLoop3", "", "sawLandingFigure",
                    new Bounds(new Vector3(-0.5f, 3.6f, 1.1f), new Vector3(9f, 2.2f, 2.2f)), 4f, 2f,
                    StoryContentBuilder.D("L3_LandingFigure",
                        (Voice.Inner, "", "Who's there?"),
                        (Voice.Inner, "", "...There's no one. There's no one there.")));
            }

            // Pacing: one false alarm. Something knocking at a window.
            var windows = new List<Vector3>();
            foreach (var group in new[] { "Walls_Windows/Windows", "UpperFloor/Windows" })
            {
                var g = GameObject.Find(group);
                if (g == null) continue;
                foreach (Transform w in g.transform)
                {
                    var r = w.GetComponentInChildren<Renderer>();
                    if (r != null) windows.Add(r.bounds.center);
                }
            }
            var alarm = Scare<FalseAlarm>(root, "FalseAlarm_Branch");
            alarm.ConfigureSound(FalseAlarm.Sound.WindowTap, windows.ToArray(), 2.5f, 2);
            alarm.ConfigurePacing("loop3Arrived", "", "falseAlarmLoop3", default, 30f, 3f,
                StoryContentBuilder.D("L3_FalseAlarm",
                    (Voice.Inner, "", "Who's that?"),
                    (Voice.Inner, "", "...A branch. Just a branch on the glass.")));

            var chair = GameObject.Find("Furniture_Added/Dining/FamilyPlaces/Family_Chair_1");
            if (chair != null) TurnAbout(root, "FamilyChair", chair.transform, 180f, "loop3Arrived", "changed_L3_Chair", 2.5f, 10f);
            DoorShuts(root, "Door_MasterBedroom", MasterBedroom, "changed_L3_BedroomDoor");

            // The note on the study door: a slow push-in while it is read.
            var note = GameObject.Find("UpperFloor/Doors/Door_Study/StudyDoorNote");
            var readable = note != null ? note.GetComponent<ReadableNote>() : null;
            if (readable != null)
            {
                readable.SetPushIn(true);
                EditorUtility.SetDirty(readable);
            }

            Fog(root, Attic, 2.3f);
            Wrong("StoryProps/WallClock", "Loop3Props/KnifeBlock", "Loop3Props/EmilyDrawing");
            DarkerLoop3();
        }

        // ------------------------------------------------------------------ scares

        private static T Scare<T>(Transform root, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            return go.AddComponent<T>();
        }

        private static void DoorCloses(Transform root, string flag, string done, float quiet)
        {
            var d = Scare<DistantDoor>(root, "DoorClosesOutOfSight");
            d.ConfigureDoors(DistantDoor.Candidates(), 6f, 16f);
            d.ConfigurePacing(flag, "", done, default, quiet, 2.5f);
        }

        /// <summary>Points every <paramref name="spacing"/> metres from a to b, then <paramref name="last"/>.</summary>
        private static Vector3[] Line(Vector3 a, Vector3 b, float spacing, Vector3 last)
        {
            var list = new List<Vector3>();
            int n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / spacing));
            for (int i = 0; i <= n; i++)
            {
                // Left, right: a step either side of the line.
                var p = Vector3.Lerp(a, b, i / (float)n);
                var side = Vector3.Cross(Vector3.up, (b - a).normalized) * (i % 2 == 0 ? 0.12f : -0.12f);
                list.Add(p + side);
            }
            list.Add(last);
            return list.ToArray();
        }

        private static void TurnAbout(Transform root, string name, Transform target, float degrees, string flag, string done, float min, float max)
        {
            var centre = PlayerView.RendererBounds(target).center;
            var turn = Quaternion.Euler(0f, degrees, 0f);
            var c = Scare<ChangesWhileUnseen>(root, "Change_" + name);
            c.ConfigureMove(target, centre + turn * (target.position - centre), turn * target.rotation);
            c.ConfigureWhen(flag, done, min, max);
        }

        private static void PullOut(Transform root, string name, Transform chair, Vector3 tableCentre, string flag, string done)
        {
            var away = chair.position - tableCentre;
            away.y = 0f;
            away.Normalize();
            var c = Scare<ChangesWhileUnseen>(root, "Change_" + name);
            c.ConfigureMove(chair, chair.position + away * 0.45f, Quaternion.Euler(0f, -25f, 0f) * chair.rotation);
            c.ConfigureWhen(flag, done, 2.5f, 10f);
        }

        private static void DoorShuts(Transform root, string doorName, Bounds room, string done)
        {
            Door door = null;
            foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include))
                if (d.name == doorName) door = d;
            if (door == null) { Debug.LogWarning("Dread: no door called " + doorName); return; }
            var c = Scare<ChangesWhileUnseen>(root, "Change_" + doorName);
            c.ConfigureDoor(door, 0.15f);
            c.ConfigureWhen("", done, 1.5f, 9f, room);
        }

        private static void Wrong(params string[] paths)
        {
            foreach (var path in paths)
            {
                var go = GameObject.Find(path);
                if (go == null || go.GetComponent<Interactable>() == null) { Debug.LogWarning("Dread: nothing to examine at " + path); continue; }
                if (go.GetComponent<FeelsWrong>() == null) go.AddComponent<FeelsWrong>();
            }
        }

        private static void Fog(Transform root, Bounds area, float times)
        {
            var go = new GameObject("AtticFog");
            go.transform.SetParent(root, false);
            go.AddComponent<FogZone>().Configure(area, times);
        }

        /// <summary>True when nothing solid but the floor is within <paramref name="radius"/> of a figure standing at <paramref name="foot"/>.</summary>
        private static bool Clear(Vector3 foot, float radius)
        {
            var centre = foot + Vector3.up * 0.95f;
            foreach (var c in Physics.OverlapBox(centre, new Vector3(radius, 0.85f, radius), Quaternion.identity, ~(1 << 2), QueryTriggerInteraction.Ignore))
                if (!(c is CharacterController)) return false;
            return true;
        }

        // ------------------------------------------------------------------ figures

        /// <summary>A shape under a dust sheet, about a person's height: head, shoulders, the cloth falling to the floor.</summary>
        private static GameObject SheetedFigure(Transform root, string name, Vector3 foot, Quaternion rotation)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(root, false);
            holder.transform.SetPositionAndRotation(foot, rotation);
            var go = new GameObject("Sheet");
            go.transform.SetParent(holder.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = SheetMesh();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SheetMaterialPath) ?? Loop3Builder.Tinted("DustSheet", new Color(0.74f, 0.72f, 0.67f));
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.3f;
            col.height = 1.8f;
            return holder;
        }

        private static Mesh SheetMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh != null) return mesh;
            // Profile: height, radius. A hem spread on the floor, hips, shoulders, a neck, a head.
            var profile = new[]
            {
                (0.00f, 0.44f), (0.04f, 0.40f), (0.35f, 0.33f), (0.75f, 0.28f), (1.05f, 0.25f),
                (1.30f, 0.27f), (1.40f, 0.28f), (1.47f, 0.21f), (1.52f, 0.12f), (1.58f, 0.13f),
                (1.66f, 0.135f), (1.73f, 0.10f), (1.77f, 0.05f), (1.785f, 0f),
            };
            const int around = 22;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var rng = new System.Random(1989);
            var phase = new float[around];
            for (int j = 0; j < around; j++) phase[j] = (float)rng.NextDouble();
            for (int i = 0; i < profile.Length; i++)
            {
                var (y, r) = profile[i];
                float fold = Mathf.Clamp01((1.35f - y) / 1.35f);            // folds hang from the shoulders down
                bool head = y > 1.45f;
                for (int j = 0; j < around; j++)
                {
                    float a = j / (float)around * Mathf.PI * 2f;
                    float k = 1f + fold * (0.07f * Mathf.Sin(a * 7f + y * 2.5f) + 0.05f * (phase[j] - 0.5f));
                    float depth = head ? 1f : 0.72f;                        // shoulders wider than deep
                    verts.Add(new Vector3(Mathf.Cos(a) * r * k, y, Mathf.Sin(a) * r * k * depth));
                }
            }
            for (int i = 0; i < profile.Length - 1; i++)
                for (int j = 0; j < around; j++)
                {
                    int a = i * around + j, b = i * around + (j + 1) % around;
                    int c = a + around, d = b + around;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            mesh = new Mesh { name = "SheetedFigure" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
            AssetDatabase.CreateAsset(mesh, MeshPath);
            return mesh;
        }

        /// <summary>One of Ruth's mannequins, standing still (no walking, no NavMesh).</summary>
        private static GameObject StillMannequin(Transform root, string name, Vector3 foot, Quaternion rotation, float pose = 0.35f)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MannequinModel);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MannequinController);
            if (model == null) { Debug.LogWarning("Dread: no mannequin model at " + MannequinModel); return null; }
            var m = (GameObject)PrefabUtility.InstantiatePrefab(model, root);
            m.name = name;
            m.transform.SetPositionAndRotation(foot, rotation);
            m.transform.localScale = Vector3.one * 0.96f;
            var skin = m.GetComponentInChildren<SkinnedMeshRenderer>();
            skin.sharedMaterial = Loop3Builder.Tinted("MannequinPlaster", new Color(0.8f, 0.77f, 0.7f));
            skin.updateWhenOffscreen = true;
            var anim = m.GetComponent<Animator>();
            if (anim == null) anim = m.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            m.AddComponent<StillFigure>().Configure(anim, "Idle", pose);
            var col = m.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.radius = 0.26f;
            col.height = 1.9f;
            return m;
        }

        /// <summary>A dark figure (Walter's model in black), for the landing in Loop 3.</summary>
        private static GameObject Silhouette(Transform root, string name, Vector3 foot, Quaternion rotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WalterPrefab);
            if (prefab == null) { Debug.LogWarning("Dread: no Walter prefab at " + WalterPrefab); return null; }
            var f = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            f.name = name;
            f.transform.SetPositionAndRotation(foot, rotation);
            var dark = Loop3Builder.Tinted("Silhouette", new Color(0.035f, 0.035f, 0.04f));
            foreach (var r in f.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = dark;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = true;
            }
            foreach (var c in f.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            return f;
        }

        // ------------------------------------------------------------------ the mirror

        private static void Mirror(Transform root, Vector3 centre, Quaternion facing)
        {
            const float w = 0.5f, h = 0.56f, frame = 0.055f;
            var go = new GameObject("HallMirror");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(centre, facing);

            var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.name = "Glass";
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            glass.transform.SetParent(go.transform, false);
            glass.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            glass.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);      // a quad faces -Z; turn it to face the room
            glass.transform.localScale = new Vector3(w, h, 1f);
            var gr = glass.GetComponent<MeshRenderer>();
            gr.sharedMaterial = MirrorMaterial();
            gr.shadowCastingMode = ShadowCastingMode.Off;

            var wood = Loop3Builder.Tinted("MirrorFrame", new Color(0.22f, 0.14f, 0.09f));
            void Bar(string n, Vector3 pos, Vector3 size)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = n;
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.SetParent(go.transform, false);
                b.transform.localPosition = pos;
                b.transform.localScale = size;
                b.GetComponent<MeshRenderer>().sharedMaterial = wood;
            }
            Bar("FrameTop", new Vector3(0f, (h + frame) / 2f, 0.02f), new Vector3(w + 2f * frame, frame, 0.04f));
            Bar("FrameBottom", new Vector3(0f, -(h + frame) / 2f, 0.02f), new Vector3(w + 2f * frame, frame, 0.04f));
            Bar("FrameLeft", new Vector3(-(w + frame) / 2f, 0f, 0.02f), new Vector3(frame, h, 0.04f));
            Bar("FrameRight", new Vector3((w + frame) / 2f, 0f, 0.02f), new Vector3(frame, h, 0.04f));
            Bar("Backing", new Vector3(0f, 0f, 0.004f), new Vector3(w + 2f * frame, h + 2f * frame, 0.008f));

            var mirror = go.AddComponent<EmptyMirror>();
            mirror.Configure(gr, StoryContentBuilder.D("L1_EmptyMirror",
                (Voice.Inner, "", "The hall. The lamp. The door behind me."),
                (Voice.Inner, "", "...Where am I? Why aren't I in it?")));
        }

        private static Material MirrorMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MirrorMaterialPath);
            if (m != null) return m;
            var shader = Shader.Find("LeftBehind/Mirror");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, MirrorMaterialPath);
            return m;
        }

        // ------------------------------------------------------------------ Loop 3's look

        /// <summary>A darker vignette (its own copy of the volume profile) and stronger film grain.</summary>
        private static void DarkerLoop3()
        {
            var volume = Object.FindAnyObjectByType<Volume>();
            if (volume != null)
            {
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Loop3ProfilePath);
                if (profile == null)
                {
                    var src = AssetDatabase.GetAssetPath(volume.sharedProfile);
                    AssetDatabase.CopyAsset(src, Loop3ProfilePath);
                    profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Loop3ProfilePath);
                }
                if (profile.TryGet(out Vignette v))
                {
                    v.intensity.Override(0.5f);
                    v.smoothness.Override(0.5f);
                    v.color.Override(new Color(0.02f, 0.015f, 0.015f));
                }
                EditorUtility.SetDirty(profile);
                volume.sharedProfile = profile;
                EditorUtility.SetDirty(volume);
            }
            var env = Object.FindAnyObjectByType<LoopEnvironment>();
            var so = new SerializedObject(env);
            so.FindProperty("extraGrain").floatValue = 0.035f;
            so.ApplyModifiedPropertiesWithoutUndo();
            env.Apply();
        }
    }
}
