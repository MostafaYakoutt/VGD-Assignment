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
            Rules(root,
                ObjectiveRules.R("", "loop1Complete"),
                ObjectiveRules.R("Go upstairs.", "hasFlashlight"),
                ObjectiveRules.R("Find another way out.", "triedFrontDoor"),
                ObjectiveRules.R("Get out of the house.", "introSeen"));

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

            var photo = Object.FindFirstObjectByType<Inspectable>();
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

            var flashlight = Object.FindFirstObjectByType<FlashlightPickup>();
            var fl = Beat(root, "Flashlight", NarrativeTrigger.Mode.Use, flashlight.transform.position);
            fl.Configure(NarrativeTrigger.Mode.Use, "hasFlashlight", D("L1_Flashlight",
                    (Voice.Inner, "", "Ruth's flashlight. She bought it for the power cuts."),
                    (Voice.Inner, "", "Upstairs, then. There has to be another way out.")),
                "", useTarget: flashlight);
            fl.SetItem("flashlight", "Flashlight",
                "Ruth bought it for the winter power cuts. She always kept spare batteries in the kitchen drawer.");

            var loopDoor = Object.FindFirstObjectByType<LoopDoor>();
            Undo.RecordObject(loopDoor, "Gate Loop Door");
            loopDoor.ConfigureGate(new[] { "sawFamilyPhoto", "hasFlashlight" }, D("L1_LoopDoorNotYet",
                (Voice.Inner, "", "Not yet. I've missed something downstairs.")), "loop1Complete");
            EditorUtility.SetDirty(loopDoor);

            Done();
        }

        // ------------------------------------------------------------------ Loop 2

        [MenuItem("Left Behind/Story/Set Up Loop 2")]
        public static void SetUpLoop2()
        {
            var root = NewRoot();
            Rules(root,
                ObjectiveRules.R("", "loop2Complete"),
                ObjectiveRules.R("Leave. Now.", "studyBricked"),
                ObjectiveRules.R("Open the study.", "hasStudyKey"),
                ObjectiveRules.R("Find the key to the study.", "powerRestored"),
                ObjectiveRules.R("Restore the power.", "sawDarkLanding"),
                ObjectiveRules.R("Go upstairs.", "readFridgeNote"),
                ObjectiveRules.R("Find Ruth.", "loop2Arrived"));

            Beat(root, "Arrival", NarrativeTrigger.Mode.SceneStart, Vector3.zero)
                .Configure(NarrativeTrigger.Mode.SceneStart, "loop2Arrived", D("L2_Arrival",
                        (Voice.Remembered, "RUTH", "Walter? Dinner's on the table."),
                        (Voice.Inner, "", "Ruth? ...Ruth!")),
                    "", startDelay: 0.8f);

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

            Readable("ReminderNote_Fridge", "fridgeNote", "Note on the fridge",
                "RUTH TOOK THE KIDS\nTO HER MOTHER'S.\n\nDON'T CALL.\n\n— W.", "readFridgeNote",
                D("L2_FridgeNote",
                    (Voice.Inner, "", "That's my handwriting."),
                    (Voice.Inner, "", "Why would I write that? Why can't I remember writing it?")));
            Readable("ReminderNote_Counter0", "noteOven", "Note on a cupboard", "OVEN OFF.\nCHECK TWICE.");
            Readable("ReminderNote_Counter1", "noteTap", "Note on the sink", "TURN THE TAP\nALL THE WAY.");
            Readable("ReminderNote_Counter3", "noteEaten", "Note on a drawer", "YOU HAVE ALREADY\nEATEN TODAY.");
            Readable("ReminderNote_Table", "noteTable", "Note on the kitchen table",
                "SET THE TABLE\nFOR EVERYONE.\n\nTHEY MIGHT\nCOME BACK TONIGHT.");
            Readable("ReminderNote_Hall", "noteKeys", "Note by the front door",
                "KEYS ARE IN YOUR COAT.\nYOUR COAT IS ON.");
            Readable("ReminderNote_FuseBox", "noteFuse", "Note on the fuse box", "SPARE FUSE:\nDINING ROOM SIDEBOARD.");

            // ---- The power: the upstairs lamps are dead until the spare fuse goes in.
            var upstairs = new System.Collections.Generic.List<LampSwitch>();
            foreach (var lamp in GameObject.Find("Loop2House").GetComponentsInChildren<LampSwitch>(true))
                if (lamp.transform.position.y > 2.6f) upstairs.Add(lamp);
            var circuit = new GameObject("PowerCircuit_Upstairs").AddComponent<PowerCircuit>();
            circuit.transform.SetParent(root, false);
            circuit.Configure("powerRestored", upstairs.ToArray());

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
            fuse.AddComponent<PickupItem>().Configure("Take the spare fuse", "spareFuse", "Spare fuse",
                "A ceramic fuse from the sideboard. Ruth always kept one spare, just in case.", "hasFuse",
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

            // ---- The study: the key is in Ruth's sewing box; behind the door, bricks.
            var sewingBox = GameObject.Find("Loop2House/Furniture/SewingRoom/SewingBox");
            var sewCol = sewingBox.GetComponent<BoxCollider>();
            if (sewCol == null) sewCol = sewingBox.AddComponent<BoxCollider>();
            sewCol.size = new Vector3(1.6f, 2.5f, 1.6f);
            var key = sewingBox.GetComponent<PickupItem>();
            if (key == null) key = sewingBox.AddComponent<PickupItem>();
            key.Configure("Open Ruth's sewing box", "studyKey", "Study key",
                "A small brass key, wrapped in thread at the bottom of Ruth's sewing box. She kept it from me.",
                "hasStudyKey", D("L2_StudyKey",
                    (Voice.Remembered, "RUTH", "Not tonight, Walter. Leave the study alone."),
                    (Voice.Inner, "", "The study key. Why did she hide it?")), hide: false);

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

            var loopDoor = Object.FindFirstObjectByType<LoopDoor>();
            Undo.RecordObject(loopDoor, "Gate Loop Door");
            loopDoor.ConfigureGate(new[] { "studyBricked" }, D("L2_LoopDoorNotYet",
                (Voice.Inner, "", "Not yet."),
                (Voice.Inner, "", "Not until I know what's in the study.")), "loop2Complete");
            EditorUtility.SetDirty(loopDoor);

            Done();
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

        private static void Readable(string objectName, string id, string title, string body, string flag = "",
            DialogueData after = null, string objective = "")
        {
            var go = GameObject.Find("Loop2House/Furniture/Notes/" + objectName);
            if (go == null) { Debug.LogWarning("Story: no note named " + objectName); return; }
            var col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.8f, 1.8f, 1.8f);     // the notes are only 8 cm across; make them easy to aim at
            var note = go.GetComponent<ReadableNote>();
            if (note == null) note = go.AddComponent<ReadableNote>();
            note.Configure(id, title, body, flag, after, objective);
            EditorUtility.SetDirty(note);
        }

        /// <summary>Loads the dialogue asset, or creates it with these lines if it does not exist yet.</summary>
        private static DialogueData D(string name, params (Voice voice, string speaker, string text)[] lines)
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
