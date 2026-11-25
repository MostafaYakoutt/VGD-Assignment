using LeftBehind.Interaction;
using LeftBehind.Loop;
using LeftBehind.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// The things that change behind Walter's back (see ChangesWhileUnseen), each once, each placed
    /// by hand, getting bolder from loop to loop:
    ///   Loop 1  the one chair at the table set for one is pulled out, as if someone just stood up;
    ///           the family photo by the front door is turned to face the wall.
    ///   Loop 2  the children's room door he walked through is shut when he turns round;
    ///           Sam's bear has climbed off the pillow and stands at the foot of the bed, facing the door.
    ///   Loop 3  the bear at the top of the stairs has taken a step towards them and turned to face them.
    /// Builds a "BehindYourBack" object under the StoryProps root. Called by StoryPropsBuilder;
    /// Menu: Left Behind > Story > Place Behind-Your-Back Changes (for the open loop scene).
    /// </summary>
    public static class BehindYourBackBuilder
    {
        private const string RootName = "BehindYourBack";

        [MenuItem("Left Behind/Story/Place Behind-Your-Back Changes")]
        public static void PlaceInOpenScene()
        {
            var env = Object.FindAnyObjectByType<LoopEnvironment>();
            if (env == null) { Debug.LogError("Behind your back: this scene has no LoopEnvironment."); return; }
            var props = GameObject.Find("StoryProps");
            if (props == null) { Debug.LogError("Behind your back: no StoryProps in this scene; run Place Story Props first."); return; }
            Place(props.transform, env.Loop);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
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
                case 1:
                    ChairPulledOut(root);
                    PhotoToTheWall(root);
                    break;
                case 2:
                    DoorShutsBehindHim(root);
                    BearOffThePillow(root);
                    break;
                case 3:
                    BearTowardsTheStairs(root);
                    break;
            }
        }

        // ------------------------------------------------------------------ Loop 1

        private static void ChairPulledOut(Transform root)
        {
            var chair = GameObject.Find("Chair_2");
            var table = GameObject.Find("Table_RoundLarge");
            if (chair == null || table == null) { Debug.LogWarning("Behind your back: no Chair_2 / Table_RoundLarge in Loop 1."); return; }
            var tableCentre = PlayerView.RendererBounds(table.transform).center;
            var away = chair.transform.position - tableCentre;
            away.y = 0f;
            away.Normalize();
            var c = Change(root, "Chair");
            c.ConfigureMove(chair.transform, chair.transform.position + away * 0.45f,
                Quaternion.Euler(0f, 22f, 0f) * chair.transform.rotation);
            c.ConfigureWhen("sawEmptyTable", "changed_L1_Chair", 2.5f, 10f);
        }

        private static void PhotoToTheWall(Transform root)
        {
            var photo = Object.FindAnyObjectByType<Inspectable>();
            if (photo == null) { Debug.LogWarning("Behind your back: no family photo (Inspectable) in Loop 1."); return; }
            TurnAbout(root, "Photo", photo.transform, 180f, "sawFamilyPhoto", "changed_L1_Photo", 2.5f, 12f);
        }

        // ------------------------------------------------------------------ Loop 2

        private static void DoorShutsBehindHim(Transform root)
        {
            var door = FindDoor("Door_ChildrensRoom");
            if (door == null) return;
            var c = Change(root, "ChildrensRoomDoor");
            c.ConfigureDoor(door, 0.12f);
            // Well inside the children's room (south of the landing wall at z 0), not in the doorway.
            var room = new Bounds(new Vector3(-5.35f, 3.6f, -3.3f), new Vector3(8.3f, 2f, 4.2f));
            c.ConfigureWhen("", "changed_L2_ChildrensDoor", 1.5f, 9f, room);
        }

        private static void BearOffThePillow(Transform root)
        {
            var bear = GameObject.Find("Story/TeddyBear");
            if (bear == null) { Debug.LogWarning("Behind your back: no Story/TeddyBear in Loop 2."); return; }
            // On the floor at the foot of Sam's bed, facing the door.
            var spot = OnFloor(new Vector3(-5.5f, 3.4f, -2.3f), bear.transform);
            var face = new Vector3(-3.0f, 0f, 0.05f) - new Vector3(spot.x, 0f, spot.z);
            var c = Change(root, "Bear");
            c.ConfigureMove(bear.transform, spot, Quaternion.LookRotation(face));
            c.ConfigureWhen("sawTeddyLoop2", "changed_L2_Bear", 2f, 9f);
        }

        // ------------------------------------------------------------------ Loop 3

        private static void BearTowardsTheStairs(Transform root)
        {
            var bear = GameObject.Find("Loop3Props/TeddyBear");
            if (bear == null) { Debug.LogWarning("Behind your back: no Loop3Props/TeddyBear in Loop 3."); return; }
            // One step nearer the head of the stairs (x 7.87..9.27, z 5.66), against the stairwell
            // railing and clear of the way down, turned to face the top step.
            var spot = OnFloor(new Vector3(7.55f, 3.4f, 5.22f), bear.transform);
            var c = Change(root, "Bear");
            c.ConfigureMove(bear.transform, spot, Quaternion.Euler(0f, 90f, 0f));
            c.ConfigureWhen("sawTeddy", "changed_L3_Bear", 2f, 10f);
        }

        // ------------------------------------------------------------------ helpers

        private static ChangesWhileUnseen Change(Transform root, string name)
        {
            var go = new GameObject("Change_" + name);
            go.transform.SetParent(root, false);
            return go.AddComponent<ChangesWhileUnseen>();
        }

        /// <summary>Turns <paramref name="target"/> about the vertical axis through the middle of what you see of it.</summary>
        private static void TurnAbout(Transform root, string name, Transform target, float degrees,
            string flag, string done, float min, float max)
        {
            var centre = PlayerView.RendererBounds(target).center;
            var turn = Quaternion.Euler(0f, degrees, 0f);
            var c = Change(root, name);
            c.ConfigureMove(target, centre + turn * (target.position - centre), turn * target.rotation);
            c.ConfigureWhen(flag, done, min, max);
        }

        /// <summary>The floor (or whatever is solid) below <paramref name="from"/>, ignoring <paramref name="self"/>.</summary>
        private static Vector3 OnFloor(Vector3 from, Transform self)
        {
            float best = float.MaxValue;
            var point = from;
            const int notStepBlockers = ~(1 << 2);       // the invisible blockers over the beds are on Ignore Raycast
            foreach (var hit in Physics.RaycastAll(from, Vector3.down, 3f, notStepBlockers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(self) || hit.distance >= best) continue;
                best = hit.distance;
                point = hit.point;
            }
            if (best == float.MaxValue) Debug.LogWarning($"Behind your back: nothing to stand on below {from}.");
            return point;
        }

        private static Door FindDoor(string name)
        {
            foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include))
                if (d.name == name) return d;
            Debug.LogWarning("Behind your back: no door called " + name + ".");
            return null;
        }
    }
}
