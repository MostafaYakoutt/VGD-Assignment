using System.IO;
using LeftBehind.Interaction;
using LeftBehind.Loop;
using LeftBehind.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Voice = LeftBehind.Story.DialogueData.Voice;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Places the Part 3 story beats for Loop 1 and Loop 2 in the open scene (under a "Story"
    /// object) and creates their DialogueData assets in Assets/Story/Dialogue. Assets that
    /// already exist are kept, so lines edited in the Inspector are not overwritten.
    /// Menu: Left Behind > Story > Set Up Loop 1 / Set Up Loop 2. Loop 2 is also set up
    /// at the end of Left Behind > Build Loop 2 House.
    /// </summary>
    public static class StoryContentBuilder
    {
        private const string DialogueFolder = "Assets/Story/Dialogue";

        // ------------------------------------------------------------------ Loop 1

        [MenuItem("Left Behind/Story/Set Up Loop 1")]
        public static void SetUpLoop1()
        {
            var root = NewRoot();
            Rules(root, Loop1Rules());

            var intro = Beat(root, "Intro", NarrativeTrigger.Mode.SceneStart, Vector3.zero);
            intro.Configure(NarrativeTrigger.Mode.SceneStart, "introSeen", D("L1_Intro",
                    (Voice.Inner, "", "The front door. I was going out... wasn't I?"),
                    (Voice.Inner, "", "It's so quiet. Where is everyone?")),
                "", startDelay: 1.5f);
            intro.SetTitle("LEFT BEHIND", 2.5f);

            var frontDoor = FrontDoor("Open the front door");
            Beat(root, "FrontDoor", NarrativeTrigger.Mode.Use, frontDoor.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "triedFrontDoor", D("L1_FrontDoor",
                        (Voice.Inner, "", "It won't open."),
                        (Voice.Inner, "", "It isn't locked. It just... won't.")),
                    "", useTarget: frontDoor);

            var photo = Object.FindAnyObjectByType<Inspectable>();
            Loop3Builder.PhotoOnFrame(photo.gameObject, "FramePhoto_Blurred", Loop3Builder.PhotoFront);
            Beat(root, "FamilyPhoto", NarrativeTrigger.Mode.Use, photo.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "sawFamilyPhoto", D("L1_FamilyPhoto",
                        (Voice.Inner, "", "Ruth. And the children."),
                        (Voice.Inner, "", "Why can't I see their faces?")),
                    "", useTarget: photo);

            Beat(root, "EmptyTable", NarrativeTrigger.Mode.EnterZone, new Vector3(-4.9f, 1f, 5.2f))
                .Configure(NarrativeTrigger.Mode.EnterZone, "sawEmptyTable", D("L1_EmptyTable",
                        (Voice.Inner, "", "One chair. One plate."),
                        (Voice.Inner, "", "When did I start eating alone?")),
                    "", zone: new Vector3(9.2f, 3f, 8.4f));

            var flashlight = Object.FindAnyObjectByType<FlashlightPickup>();
            var fl = Beat(root, "Flashlight", NarrativeTrigger.Mode.Use, flashlight.transform.position);
            fl.Configure(NarrativeTrigger.Mode.Use, "hasFlashlight", D("L1_Flashlight",
                    (Voice.Inner, "", "Ruth's flashlight. She bought it for the power cuts."),
                    (Voice.Inner, "", "Upstairs, then. There has to be another way out.")),
                "", useTarget: flashlight);
            fl.SetItem("flashlight");

            // The study does not exist in Loop 1: where its door was there is only wall.
            var studyDoor = FindIncl<Door>("Door_Study");
            if (studyDoor != null) studyDoor.gameObject.SetActive(false);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "StudyWall";
            wall.transform.SetParent(root, false);
            wall.transform.position = new Vector3(2.5f, 2.6f + 1.05f, 2.15f);
            wall.transform.localScale = new Vector3(0.92f, 2.1f, 0.1f);
            wall.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall_DecrepitWallpaper.mat");
            var tiling = wall.AddComponent<LeftBehind.Rendering.WorldTiling>();
            var tso = new SerializedObject(tiling);
            tso.FindProperty("mode").enumValueIndex = (int)LeftBehind.Rendering.WorldTiling.Mode.Wall;
            tso.FindProperty("tileSize").floatValue = 2.5f;
            tso.ApplyModifiedPropertiesWithoutUndo();
            tiling.Apply();

            // The attic is open in the present: the family's things are packed away up there.
            var atticDoor = FindIncl<Door>("Door_Attic_Locked");
            if (atticDoor != null)
            {
                var aso = new SerializedObject(atticDoor);
                aso.FindProperty("locked").boolValue = false;
                aso.ApplyModifiedPropertiesWithoutUndo();
            }
            AtticBear(root);
            AtticKey(root);
            GateLoop1Door();

            StoryPropsBuilder.Place(1);
            Done();
        }

        /// <summary>
        /// Loop 1's objectives. Once the locked Loop Door has turned Walter away with only its key
        /// still missing, the objective sends him to the attic.
        /// </summary>
        public static ObjectiveRules.Rule[] Loop1Rules() => new[]
        {
            ObjectiveRules.R("", "loop1Complete"),
            new ObjectiveRules.Rule
            {
                objective = "Look in the attic.",
                allOf = new[] { "triedLoopDoor", "sawFamilyPhoto", "hasFlashlight" },
                noneOf = new[] { "hasAtticKey" },
            },
            ObjectiveRules.R("Go upstairs.", "hasFlashlight"),
            ObjectiveRules.R("Find another way out.", "triedFrontDoor"),
            ObjectiveRules.R("Get out of the house.", "introSeen"),
        };

        /// <summary>Sam's teddy bear, packed away on the toy chest at the far end of the attic. Look only.</summary>
        public static Examinable AtticBear(Transform root)
        {
            var bear = Loop3Builder.Teddy(root, "TeddyBear", new Vector3(-5.4f, 5.62f, -7.2f), 200f, 1.1f).AddComponent<Examinable>();
            bear.Configure("Look at the bear");
            Beat(root, "Teddy", NarrativeTrigger.Mode.Use, bear.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "sawTeddyLoop1", D("L1_Teddy",
                        (Voice.Inner, "", "A child's bear, packed away with the toys."),
                        (Voice.Inner, "", "Whose was it? Why can't I remember?")),
                    "", useTarget: bear);
            return bear;
        }

        /// <summary>
        /// The key to the Loop Door, on the toy chest beside Sam's bear at the far end of the attic,
        /// past Ruth's mannequins. Walter takes it (item loopDoorKey, flag hasAtticKey) and the door
        /// will not open without it; once it is gone the mannequins walk faster.
        /// </summary>
        public static PickupItem AtticKey(Transform root)
        {
            Transform chest = null;
            foreach (var t in Object.FindObjectsByType<Transform>())
                if (t.name == "ToyChest" && t.parent != null && t.parent.name == "ChildrensThings") chest = t;
            if (chest == null) { Debug.LogWarning("Attic key: no ToyChest under ChildrensThings."); return null; }
            var top = Bounds(chest.gameObject);
            // At the end of the chest away from the bear.
            var pos = new Vector3(top.max.x - 0.17f, top.max.y + 0.002f, top.center.z);
            var key = BrassKey(root, "AtticKey", pos, -35f);
            var pickup = key.AddComponent<PickupItem>();
            pickup.Configure("Take the key", "loopDoorKey", "hasAtticKey", D("L1_AtticKey",
                    (Voice.Inner, "", "A key. In with the children's toys."),
                    (Voice.Inner, "", "The door upstairs. Did I hide it up here... so I couldn't leave?")), hide: true);
            return pickup;
        }

        /// <summary>A small brass key built from primitives, with a collider bigger than it so it is easy to aim at.</summary>
        public static GameObject BrassKey(Transform parent, string name, Vector3 position, float yaw)
        {
            var brass = Loop3Builder.Tinted("Brass", new Color(0.72f, 0.56f, 0.26f));
            brass.SetFloat("_Metallic", 0.8f);
            brass.SetFloat("_Smoothness", 0.55f);
            EditorUtility.SetDirty(brass);
            var key = new GameObject(name);
            key.transform.SetParent(parent, false);
            key.transform.position = position;
            key.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Part(key.transform, PrimitiveType.Cylinder, "Bow", new Vector3(-0.035f, 0.002f, 0f), Quaternion.identity, new Vector3(0.032f, 0.002f, 0.032f), brass);
            Part(key.transform, PrimitiveType.Cube, "Shaft", new Vector3(0.008f, 0.002f, 0f), Quaternion.identity, new Vector3(0.058f, 0.004f, 0.006f), brass);
            Part(key.transform, PrimitiveType.Cube, "Bit", new Vector3(0.03f, 0.002f, -0.007f), Quaternion.identity, new Vector3(0.012f, 0.004f, 0.012f), brass);
            var col = key.AddComponent<BoxCollider>();       // 9 cm long: make it easy to aim at
            col.center = new Vector3(0f, 0.04f, 0f);
            col.size = new Vector3(0.22f, 0.1f, 0.2f);
            return key;
        }

        /// <summary>The Loop Door is locked: it opens once the photo is seen, the flashlight taken and its key found in the attic.</summary>
        public static void GateLoop1Door()
        {
            var loopDoor = Object.FindAnyObjectByType<LoopDoor>();
            Undo.RecordObject(loopDoor, "Gate Loop Door");
            var downstairs = D("L1_LoopDoorNotYet",
                (Voice.Inner, "", "Not yet. I've missed something downstairs."));
            var attic = D("L1_LoopDoorLocked",
                (Voice.Inner, "", "Locked. Where's the key?"),
                (Voice.Inner, "", "I put it away somewhere safe. Up in the attic, with everything else."));
            loopDoor.ConfigureGate(new[] { "sawFamilyPhoto", "hasFlashlight", "hasAtticKey" }, downstairs, "loop1Complete");
            loopDoor.ConfigureHints(new[]
            {
                new LoopDoor.Hint { flag = "sawFamilyPhoto", dialogue = downstairs },
                new LoopDoor.Hint { flag = "hasFlashlight", dialogue = downstairs },
                new LoopDoor.Hint { flag = "hasAtticKey", dialogue = attic },
            }, "triedLoopDoor");
            EditorUtility.SetDirty(loopDoor);
        }

        // ------------------------------------------------------------------ Loop 2

        [MenuItem("Left Behind/Story/Set Up Loop 2")]
        public static void SetUpLoop2()
        {
            var root = NewRoot();
            Rules(root, Loop2Rules());

            Beat(root, "Arrival", NarrativeTrigger.Mode.SceneStart, Vector3.zero)
                .Configure(NarrativeTrigger.Mode.SceneStart, "loop2Arrived", Loop2Arrival(), "", startDelay: 0.8f);

            var frontDoor = FrontDoor("Open the front door");
            Beat(root, "FrontDoor", NarrativeTrigger.Mode.Use, frontDoor.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "triedFrontDoorLoop2", D("L2_FrontDoor",
                        (Voice.Inner, "", "Still won't open."),
                        (Voice.Inner, "", "She'll be back soon. She always comes back.")),
                    "", useTarget: frontDoor);

            Beat(root, "TableForSix", NarrativeTrigger.Mode.EnterZone, new Vector3(-4.9f, 1f, 5.2f))
                .Configure(NarrativeTrigger.Mode.EnterZone, "sawTableForSix", D("L2_TableForSix",
                        (Voice.Inner, "", "Six places."),
                        (Voice.Inner, "", "There are only four of us.")),
                    "", zone: new Vector3(9.2f, 3f, 8.4f));

            Readable("ReminderNote_Fridge", "Note on the fridge",
                "RUTH TOOK THE KIDS\nTO HER MOTHER'S.\n\nDON'T CALL.\n\n— W.", "readFridgeNote",
                D("L2_FridgeNote",
                    (Voice.Inner, "", "That's my handwriting."),
                    (Voice.Inner, "", "Why would I write that? Why can't I remember writing it?")));
            Readable("ReminderNote_Counter0", "Note on a cupboard", "OVEN OFF.\nCHECK TWICE.");
            Readable("ReminderNote_Counter1", "Note on the sink", "TURN THE TAP\nALL THE WAY.");
            Readable("ReminderNote_Counter3", "Note on a drawer", "YOU HAVE ALREADY\nEATEN TODAY.");
            Readable("ReminderNote_Table", "Note on the kitchen table",
                "SET THE TABLE\nFOR EVERYONE.\n\nTHEY MIGHT\nCOME BACK TONIGHT.");
            Readable("ReminderNote_Hall", "Note by the front door",
                "KEYS ARE IN YOUR COAT.\nYOUR COAT IS ON.");
            foreach (var name in new[] { "Corridor", "Landing", "Dining" })
            {
                var group = GameObject.Find("Loop2House/Furniture/" + name);
                if (group == null) continue;
                foreach (Transform t in group.transform)
                {
                    if (t.name == "Photo")          // the round wall frames
                        Loop3Builder.PhotoOnFrame(t.gameObject, "FramePhoto_ClearOval", Loop3Builder.PhotoFront, 0.62f, 0.62f);
                    else if (t.name.StartsWith("FamilyPhoto"))
                        Loop3Builder.PhotoOnFrame(t.gameObject, "FramePhoto_Clear", Loop3Builder.PhotoFront);
                }
            }

            var bear2 = Loop3Builder.Teddy(root, "TeddyBear", new Vector3(-5.5f, 3.28f, -4.15f), 0f, 1.1f).AddComponent<Examinable>();
            bear2.Configure("Look at the bear");
            Beat(root, "Teddy", NarrativeTrigger.Mode.Use, bear2.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "sawTeddyLoop2", D("L2_Teddy",
                        (Voice.Inner, "", "Sam's bear, on his pillow."),
                        (Voice.Inner, "", "He'll want it when they get back.")),
                    "", useTarget: bear2);

            var drawing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            drawing.name = "EmilyDrawing";
            drawing.transform.SetParent(root, false);
            drawing.transform.position = new Vector3(-2.0f, 3.264f, -4.3f);
            drawing.transform.rotation = Quaternion.Euler(90f, -8f, 0f);
            drawing.transform.localScale = new Vector3(0.36f, 0.25f, 1f);
            Object.DestroyImmediate(drawing.GetComponent<MeshCollider>());
            drawing.AddComponent<BoxCollider>().size = new Vector3(1.4f, 1.6f, 0.6f);
            drawing.GetComponent<Renderer>().sharedMaterial = Loop3Builder.PaperMaterial("EmilyDrawing_Happy");
            drawing.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var happy = drawing.AddComponent<Examinable>();
            happy.Configure("Look at the drawing");
            Beat(root, "Drawing", NarrativeTrigger.Mode.Use, drawing.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "sawDrawingLoop2", D("L2_Drawing",
                        (Voice.Remembered, "EMILY", "Look, Daddy! It's all of us, in front of the house."),
                        (Voice.Inner, "", "She always drew us holding hands.")),
                    "", useTarget: happy);

            Readable("ReminderNote_FuseBox", "Note on the fuse box", "SPARE FUSE:\nDINING ROOM SIDEBOARD.");

            // ---- The power: the fuse has blown and every lamp in the house is dead until the spare goes in.
            Loop2Power(root);
            Loop2Flashlight(root);

            var landing = Beat(root, "DarkLanding", NarrativeTrigger.Mode.EnterZone, new Vector3(7.85f, 3.6f, 3.3f));
            landing.Configure(NarrativeTrigger.Mode.EnterZone, "sawDarkLanding", D("L2_DarkLanding",
                    (Voice.Inner, "", "It's dark up here. The lights are out."),
                    (Voice.Inner, "", "The fuse again. Sam hates the dark.")),
                "", zone: new Vector3(2.8f, 2.4f, 4.6f));
            landing.BlockBy("powerRestored");

            // The spare fuse, on top of the dining room sideboard.
            var sideboard = GameObject.Find("Loop2House/Furniture/Dining/Sideboard");
            var sb = Bounds(sideboard);
            var fuse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fuse.name = "SpareFuse";
            fuse.transform.SetParent(root, false);
            fuse.transform.localScale = new Vector3(0.05f, 0.05f, 0.12f);
            fuse.transform.position = new Vector3(sb.center.x, sb.max.y + 0.025f, 5.05f);
            fuse.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ceiling_Plaster.mat");
            fuse.GetComponent<BoxCollider>().size = new Vector3(5f, 5f, 2.5f);   // easier to aim at than the fuse itself
            fuse.AddComponent<PickupItem>().Configure("Take the spare fuse", "spareFuse", "hasFuse",
                D("L2_TakeFuse", (Voice.Inner, "", "The spare fuse. Right where the note said.")), hide: true);

            var fuseBoxGo = GameObject.Find("Loop2House/Furniture/Utility/FuseBox");
            var fuseBox = fuseBoxGo.GetComponent<FuseBox>();
            if (fuseBox == null) fuseBox = fuseBoxGo.AddComponent<FuseBox>();
            fuseBox.Configure("spareFuse", "powerRestored",
                D("L2_FuseBoxNoFuse",
                    (Voice.Inner, "", "One of them has blown."),
                    (Voice.Inner, "", "There's a spare somewhere. I wrote it down.")),
                D("L2_FuseFitted",
                    (Voice.Inner, "", "There."),
                    (Voice.Remembered, "SAM", "Dad, leave the landing light on?")));

            // ---- The study: the key lies by Ruth's ring on the dresser (see StudyKeyByRing); behind the door, bricks.
            var studyDoor = GameObject.Find("Loop2House/Doors/Door_Study_Locked");
            var keyed = studyDoor.GetComponent<KeyedDoor>();
            if (keyed == null) keyed = studyDoor.AddComponent<KeyedDoor>();
            keyed.Configure("studyKey", "studyBricked",
                D("L2_StudyLocked", (Voice.Inner, "", "Locked. I never lock it.")),
                D("L2_StudyBricked",
                    (Voice.Inner, "", "Bricks."),
                    (Voice.Inner, "", "Fresh bricks. Somebody walled it up."),
                    (Voice.Inner, "", "I did. Why would I do that?")),
                "Unlock the study", "Locked");

            var loopDoor = Object.FindAnyObjectByType<LoopDoor>();
            Undo.RecordObject(loopDoor, "Gate Loop Door");
            loopDoor.ConfigureGate(new[] { "studyBricked" }, D("L2_LoopDoorNotYet",
                (Voice.Inner, "", "Not yet."),
                (Voice.Inner, "", "Not until I know what's in the study.")), "loop2Complete");
            EditorUtility.SetDirty(loopDoor);

            StoryPropsBuilder.Place(2);
            StudyKeyByRing(root);       // after the props: it goes beside the ring dish
            Done();
        }

        /// <summary>
        /// Loop 2's objectives. The house is dark when he arrives, so the first thing is a light.
        /// </summary>
        public static ObjectiveRules.Rule[] Loop2Rules() => new[]
        {
            ObjectiveRules.R("", "loop2Complete"),
            ObjectiveRules.R("Leave. Now.", "studyBricked"),
            ObjectiveRules.R("Open the study.", "hasStudyKey"),
            ObjectiveRules.R("Find the key to the study.", "powerRestored"),
            new ObjectiveRules.Rule
            {
                objective = "Find the flashlight.",
                allOf = new[] { "loop2Arrived" },
                noneOf = new[] { "hasFlashlightLoop2" },
            },
            ObjectiveRules.R("Restore the power.", "sawDarkLanding"),
            ObjectiveRules.R("Go upstairs.", "readFridgeNote"),
            ObjectiveRules.R("Find Ruth.", "loop2Arrived"),
        };

        /// <summary>Ruth's voice, then the dark house: the power is out again.</summary>
        public static DialogueData Loop2Arrival() => D("L2_ArrivalDark",
            (Voice.Remembered, "RUTH", "Walter? Dinner's on the table."),
            (Voice.Inner, "", "Ruth? ...Ruth!"),
            (Voice.Inner, "", "The lights are out again. She keeps the flashlight on the commode."));

        /// <summary>
        /// Every lamp in the Loop 2 house on one circuit, dead until powerRestored (the spare fuse).
        /// The street lamps outside are not on it.
        /// </summary>
        public static PowerCircuit Loop2Power(Transform root)
        {
            var lamps = GameObject.Find("Loop2House").GetComponentsInChildren<LampSwitch>(true);
            var circuit = new GameObject("PowerCircuit_House").AddComponent<PowerCircuit>();
            circuit.transform.SetParent(root, false);
            circuit.Configure("powerRestored", lamps);
            return circuit;
        }

        /// <summary>
        /// Ruth's flashlight on the dining room commode, where it lay in Loop 1: he does not bring it
        /// with him from one loop to the next, so he has to find it again in the dark.
        /// </summary>
        public static FlashlightPickup Loop2Flashlight(Transform root)
        {
            var commode = GameObject.Find("Loop2House/Furniture/Dining/Commode");
            var prefab = Resources.Load<GameObject>("HeldFlashlight");
            if (commode == null || prefab == null) { Debug.LogWarning("Loop 2 flashlight: no commode or HeldFlashlight prefab."); return null; }
            var top = Bounds(commode);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.name = "Flashlight";
            go.transform.rotation = Quaternion.Euler(0f, 70f, 0f) * Quaternion.Euler(90f, 0f, 0f);   // lying down (the lens is its +Y)
            go.transform.position = new Vector3(top.center.x, top.max.y + 0.2f, top.center.z);
            var b = Bounds(go);
            go.transform.position += new Vector3(top.center.x - b.center.x, top.max.y + 0.002f - b.min.y, top.center.z - b.center.z);
            var pickup = go.GetComponent<FlashlightPickup>();
            pickup.enabled = true;
            var outline = go.transform.Find("Outline");
            if (outline != null) outline.gameObject.SetActive(true);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = true;

            var beat = Beat(root, "Flashlight", NarrativeTrigger.Mode.Use, go.transform.position);
            beat.Configure(NarrativeTrigger.Mode.Use, "hasFlashlightLoop2", D("L2_Flashlight",
                    (Voice.Inner, "", "Her flashlight. Right where she always leaves it."),
                    (Voice.Inner, "", "The fuse box, then. Before she gets home.")),
                "", useTarget: pickup);
            beat.SetItem("flashlight");
            return pickup;
        }

        /// <summary>
        /// Loop 2's study key: a small brass key on the master bedroom dresser, beside the dish with
        /// the wedding ring Ruth left behind. Taking it gives the studyKey item and sets hasStudyKey.
        /// </summary>
        public static PickupItem StudyKeyByRing(Transform root)
        {
            var dish = GameObject.Find("StoryProps/RingDish");
            var dresser = GameObject.Find("Loop2House/Furniture/MasterBedroom/Dresser");
            if (dish == null || dresser == null) { Debug.LogWarning("Study key: no ring dish or dresser in this scene."); return null; }
            var top = Bounds(dresser);
            var d = dish.transform.position;
            bool longX = top.size.x > top.size.z;

            // The first clear spot beside the dish, still on the dresser.
            var spot = d + (longX ? Vector3.right : Vector3.forward) * 0.16f;
            foreach (float off in new[] { 0.16f, -0.16f, 0.24f, -0.24f })
            {
                var p = d + (longX ? Vector3.right : Vector3.forward) * off;
                if (!top.Contains(new Vector3(p.x, top.center.y, p.z))) continue;
                var box = new Bounds(new Vector3(p.x, top.max.y + 0.03f, p.z), new Vector3(0.1f, 0.04f, 0.1f));
                bool clear = true;
                foreach (var r in Object.FindObjectsByType<Renderer>())
                    if (!r.transform.IsChildOf(dresser.transform) && r.bounds.Intersects(box)) { clear = false; break; }
                if (clear) { spot = p; break; }
            }

            var key = BrassKey(root, "StudyKey", new Vector3(spot.x, top.max.y + 0.002f, spot.z), 28f);
            var pickup = key.AddComponent<PickupItem>();
            pickup.Configure("Take the key", "studyKey", "hasStudyKey", D("L2_StudyKeyByRing",
                    (Voice.Remembered, "RUTH", "Not tonight, Walter. Leave the study alone."),
                    (Voice.Inner, "", "The study key. Right next to her ring."),
                    (Voice.Inner, "", "She never took her ring off. Why are they both here?")), hide: true);
            return pickup;
        }

        private static void Part(Transform parent, PrimitiveType type, string name, Vector3 local, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ Loop 3

        /// <summary>Called by Left Behind > Build Loop 3, after its props are placed.</summary>
        public static void SetUpLoop3()
        {
            var root = NewRoot();
            Rules(root,
                ObjectiveRules.R("", "choseToRemember"),
                ObjectiveRules.R("", "choseToLookAway"),
                ObjectiveRules.R("Open the study.", "readDelusionNote"),
                ObjectiveRules.R("Find the children.", "sawDrawing"),
                ObjectiveRules.R("Find Ruth.", "loop3Arrived"));

            Beat(root, "Arrival", NarrativeTrigger.Mode.SceneStart, Vector3.zero)
                .Configure(NarrativeTrigger.Mode.SceneStart, "loop3Arrived", D("L3_Arrival",
                        (Voice.Inner, "", "Every light in the house is on."),
                        (Voice.Inner, "", "Ruth? Where are you?")),
                    "", startDelay: 0.8f);

            var frontDoor = FrontDoor("Open the front door");
            Beat(root, "FrontDoor", NarrativeTrigger.Mode.Use, frontDoor.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "triedFrontDoorLoop3", D("L3_FrontDoor",
                        (Voice.Inner, "", "Locked. I locked it myself."),
                        (Voice.Inner, "", "Why would I lock us in?")),
                    "", useTarget: frontDoor);

            // No Loop Door tonight: the door at the end of the landing is just a door that won't open.
            var loopDoorGo = GameObject.Find("UpperFloor/Doors/LoopDoor");
            var oldLoop = loopDoorGo.GetComponent<LoopDoor>();
            if (oldLoop != null) Object.DestroyImmediate(oldLoop);
            var rattle = loopDoorGo.GetComponent<DoorRattle>();
            if (rattle == null) rattle = loopDoorGo.AddComponent<DoorRattle>();
            rattle.Configure(loopDoorGo.transform, "Open the door");
            Beat(root, "NoLoopDoor", NarrativeTrigger.Mode.Use, loopDoorGo.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "triedLoopDoorLoop3", D("L3_NoLoopDoor",
                        (Voice.Inner, "", "It won't open."),
                        (Voice.Inner, "", "There's no way out of tonight.")),
                    "", useTarget: rattle);

            var knifeBlock = Prop<Examinable>("KnifeBlock");
            knifeBlock.Configure("Look at the knife block");
            Beat(root, "KnifeBlock", NarrativeTrigger.Mode.Use, knifeBlock.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "sawKnifeBlock", D("L3_KnifeBlock",
                        (Voice.Inner, "", "One of the knives is missing."),
                        (Voice.Inner, "", "Ruth never leaves them out.")),
                    "", useTarget: knifeBlock);

            Prop<ReadableNote>("ClinicCard").Configure("Card on the fridge",
                "ST. MARY'S MEMORY CLINIC\n\nAppointment for: Walter Hale\nTuesday, 10:30\n\nPlease bring a family member with you.",
                "sawClinicCard", D("L3_ClinicCard",
                    (Voice.Inner, "", "Ruth booked this for me."),
                    (Voice.Inner, "", "She knew. She knew something was wrong with me.")), prompt: "Read the card");

            var drawing = Prop<Examinable>("EmilyDrawing");
            drawing.Configure("Look at the drawing");
            Beat(root, "Drawing", NarrativeTrigger.Mode.Use, drawing.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "sawDrawing", D("L3_Drawing",
                        (Voice.Remembered, "EMILY", "That's you, Daddy. I couldn't get your face right."),
                        (Voice.Inner, "", "She scribbled it out.")),
                    "", useTarget: drawing);

            var bear = Prop<Examinable>("TeddyBear");
            bear.Configure("Look at the bear");
            Beat(root, "Teddy", NarrativeTrigger.Mode.Use, bear.transform.position)
                .Configure(NarrativeTrigger.Mode.Use, "sawTeddy", D("L3_Teddy",
                        (Voice.Inner, "", "Sam's bear."),
                        (Voice.Inner, "", "He never goes anywhere without it.")),
                    "", useTarget: bear);

            var studyNote = GameObject.Find("UpperFloor/Doors/Door_Study/StudyDoorNote").AddComponent<ReadableNote>();
            studyNote.Configure("Note on the study door",
                "THAT WOMAN IS NOT RUTH.\n\nShe has Ruth's face and Ruth's voice, but she watches me. " +
                "The children are not mine. They whisper when I leave the room.\n\n" +
                "They want me to forget.\nI will not forget.\n\n\u2014 W.",
                "readDelusionNote", D("L3_DelusionNote",
                    (Voice.Inner, "", "My handwriting."),
                    (Voice.Inner, "", "No. That isn't... I didn't...")));
            EditorUtility.SetDirty(studyNote);

            var studyDoorGo = GameObject.Find("UpperFloor/Doors/Door_Study");
            var studyDoor = studyDoorGo.GetComponent<Door>();
            var dso = new SerializedObject(studyDoor);
            dso.FindProperty("locked").boolValue = true;
            dso.ApplyModifiedPropertiesWithoutUndo();
            var keyed = studyDoorGo.GetComponent<KeyedDoor>();
            if (keyed == null) keyed = studyDoorGo.AddComponent<KeyedDoor>();
            keyed.Configure("", "openedStudyLoop3",
                D("L3_StudyLocked",
                    (Voice.Inner, "", "Locked."),
                    (Voice.Inner, "", "There's something taped to the door.")),
                null, "Open the study", "Locked");
            keyed.ConfigureFlag("readDelusionNote");
            EditorUtility.SetDirty(keyed);

            var climax = D("L3_Study",
                (Voice.Inner, "", "The study."),
                (Voice.Inner, "", "The knife from the kitchen."),
                (Voice.Inner, "", "Ruth. Emily. Sam."),
                (Voice.Inner, "", "I could remember. Or I could close the door and forget, again."));
            if (climax.choices == null || climax.choices.Length == 0)
            {
                climax.choices = new[]
                {
                    new DialogueData.Choice { text = "Remember.", flagToSet = "choseToRemember", next = D("L3_Remember",
                        (Voice.Inner, "", "Ruth. Emily. Sam."),
                        (Voice.Inner, "", "I remember now. I remember all of it.")) },
                    new DialogueData.Choice { text = "Look away.", flagToSet = "choseToLookAway", next = D("L3_LookAway",
                        (Voice.Inner, "", "No."),
                        (Voice.Inner, "", "I don't want to know.")) },
                };
                EditorUtility.SetDirty(climax);
            }
            Beat(root, "Study", NarrativeTrigger.Mode.EnterZone, new Vector3(3.2f, 3.6f, 6.4f))
                .Configure(NarrativeTrigger.Mode.EnterZone, "enteredStudyLoop3", climax, "",
                    required: new[] { "readDelusionNote" }, zone: new Vector3(6f, 2.4f, 6.2f));

            var ending = new GameObject("EndingDirector").AddComponent<EndingDirector>();
            ending.transform.SetParent(root, false);
            ending.Configure("choseToRemember", "choseToLookAway", "SampleScene");

            StoryPropsBuilder.Place(3);
            Done();
        }

        private static T Prop<T>(string name) where T : Component
        {
            var go = GameObject.Find(Loop3Builder.PropsRoot + "/" + name);
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            EditorUtility.SetDirty(c);
            return c;
        }

        // ------------------------------------------------------------------ helpers

        private static void Rules(Transform root, params ObjectiveRules.Rule[] rules)
        {
            var go = new GameObject("ObjectiveRules");
            go.transform.SetParent(root, false);
            go.AddComponent<ObjectiveRules>().Configure(rules);
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Finds a component by object name, including inactive objects.</summary>
        private static T FindIncl<T>(string name) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
                if (c.name == name) return c;
            return null;
        }

        private static Transform NewRoot()
        {
            var old = GameObject.Find("Story");
            if (old != null) Object.DestroyImmediate(old);
            return new GameObject("Story").transform;
        }

        private static void Done()
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManagerActive());
        }

        private static UnityEngine.SceneManagement.Scene SceneManagerActive() =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        private static NarrativeTrigger Beat(Transform root, string name, NarrativeTrigger.Mode mode, Vector3 pos)
        {
            var go = new GameObject("Beat_" + name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            return go.AddComponent<NarrativeTrigger>();
        }

        /// <summary>The front door leaf (Door_1) never opens; using it rattles it.</summary>
        private static DoorRattle FrontDoor(string prompt)
        {
            var leaf = GameObject.Find("Door_1");
            var rattle = leaf.GetComponent<DoorRattle>();
            if (rattle == null) rattle = leaf.AddComponent<DoorRattle>();
            rattle.Configure(leaf.transform, prompt);
            EditorUtility.SetDirty(rattle);
            return rattle;
        }

        private static void Readable(string objectName, string title, string body, string flag = "",
            DialogueData after = null, string objective = "")
        {
            var go = GameObject.Find("Loop2House/Furniture/Notes/" + objectName);
            if (go == null) { Debug.LogWarning("Story: no note named " + objectName); return; }
            var col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 1.8f, 1.8f);     // the notes are only 8 cm across; make them easy to aim at
            var note = go.GetComponent<ReadableNote>();
            if (note == null) note = go.AddComponent<ReadableNote>();
            note.Configure(title, body, flag, after, objective);
            EditorUtility.SetDirty(note);
        }

        /// <summary>Loads the dialogue asset, or creates it with these lines if it does not exist yet.</summary>
        internal static DialogueData D(string name, params (Voice voice, string speaker, string text)[] lines)
        {
            string path = $"{DialogueFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory(DialogueFolder);
            var d = ScriptableObject.CreateInstance<DialogueData>();
            d.lines = new DialogueData.Line[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                d.lines[i] = new DialogueData.Line { voice = lines[i].voice, speaker = lines[i].speaker, text = lines[i].text };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }
    }
}
