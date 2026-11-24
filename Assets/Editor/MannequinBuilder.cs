using LeftBehind.Story;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using Voice = LeftBehind.Story.DialogueData.Voice;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Ruth's mannequins in the Loop 1 attic: four figures (the Mixamo Ch36 character in a plaster
    /// material) standing about the room, all facing the attic door. Each walks towards Walter while
    /// he cannot see it and freezes when he looks (see the Mannequin script).
    /// Builds, under the given StoryProps root:
    ///   an attic-only NavMesh (baked to Assets/Scenes/SampleScene/NavMesh-Attic.asset), with a
    ///     strip in front of the attic door left unwalkable so none of them can stand in the doorway;
    ///   the Animator Controller (Idle, Walk; a Walking bool) if it does not exist yet;
    ///   the mannequins and two of Walter's lines.
    /// Called by StoryPropsBuilder for Loop 1.
    /// </summary>
    public static class MannequinBuilder
    {
        private const string Folder = "Assets/Characters/Mannequin";
        private const string ModelPath = Folder + "/Mannequin_Ch36.fbx";
        private const string ControllerPath = Folder + "/Mannequin.controller";
        private const string NavMeshPath = "Assets/Scenes/SampleScene/NavMesh-Attic.asset";

        // The attic room, without the stair-head (which starts at x = 7.95).
        private static readonly Bounds Attic = new Bounds(new Vector3(-0.85f, 6.45f, 0f), new Vector3(17.5f, 2.7f, 19.2f));
        private static readonly Vector3 AtticDoor = new Vector3(8.0f, 5.15f, -6.45f);

        private static readonly Vector3[] Spots =
        {
            new Vector3(3.0f, 5.15f, -3.0f),
            new Vector3(-1.2f, 5.15f, 2.5f),
            new Vector3(-2.0f, 5.15f, -6.0f),
            new Vector3(5.5f, 5.15f, 1.8f),
        };

        public static void Place(Transform root)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) { Debug.LogWarning("Mannequins: no model at " + ModelPath); return; }

            var group = new GameObject("AtticMannequins").transform;
            group.SetParent(root, false);
            BakeNavMesh(group);

            var controller = Controller();
            var plaster = Loop3Builder.Tinted("MannequinPlaster", new Color(0.8f, 0.77f, 0.7f));
            plaster.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(plaster);

            var entered = Beat(group, "Mannequins", NarrativeTrigger.Mode.EnterZone, new Vector3(6.4f, 6.2f, -6.45f));
            entered.Configure(NarrativeTrigger.Mode.EnterZone, "sawMannequins", StoryContentBuilder.D("L1_Mannequins",
                    (Voice.Inner, "", "Ruth's mannequins, from her sewing room."),
                    (Voice.Inner, "", "Why are they all up here, facing the door?")),
                "", zone: new Vector3(2.4f, 2.4f, 2.2f));
            var moved = Beat(group, "MannequinMoved", NarrativeTrigger.Mode.Use, Attic.center);
            moved.Configure(NarrativeTrigger.Mode.Use, "mannequinMoved", StoryContentBuilder.D("L1_MannequinMoved",
                    (Voice.Inner, "", "That one wasn't there a moment ago."),
                    (Voice.Inner, "", "...Was it?")),
                "");     // no target: the mannequins fire it themselves

            for (int i = 0; i < Spots.Length; i++)
            {
                if (!NavMesh.SamplePosition(Spots[i], out var hit, 1.5f, NavMesh.AllAreas))
                {
                    Debug.LogWarning($"Mannequins: no NavMesh near {Spots[i]}; skipped.");
                    continue;
                }
                var m = (GameObject)PrefabUtility.InstantiatePrefab(model, group);
                m.name = "Mannequin_" + i;
                m.transform.position = hit.position;
                var face = AtticDoor - hit.position;
                face.y = 0f;
                m.transform.rotation = Quaternion.LookRotation(face);
                m.transform.localScale = Vector3.one * 0.96f;       // the model is 1.88 m; about 1.8 m

                var skin = m.GetComponentInChildren<SkinnedMeshRenderer>();
                skin.sharedMaterial = plaster;
                skin.updateWhenOffscreen = true;                     // its bounds are used to tell whether it is seen

                var anim = m.GetComponent<Animator>();
                if (anim == null) anim = m.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var agent = m.AddComponent<NavMeshAgent>();
                agent.speed = 1.25f;                                 // matches the stride of the walk clip
                agent.acceleration = 6f;
                agent.angularSpeed = 300f;
                agent.radius = 0.3f;
                agent.height = 1.8f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

                var col = m.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.95f, 0f);
                col.radius = 0.26f;
                col.height = 1.9f;
                m.AddComponent<Rigidbody>().isKinematic = true;

                m.AddComponent<Mannequin>().Configure(anim, skin, Attic, moved);
            }
        }

        private static void BakeNavMesh(Transform group)
        {
            var surfaceGo = new GameObject("AtticNavMesh");
            surfaceGo.transform.SetParent(group, false);
            var surface = surfaceGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            // From the attic floor (5.15) to just under the flat ceiling (7.75): the sloping roof
            // inside this range keeps them out of the low eaves, and the top of the ceiling is left out.
            surface.center = new Vector3(Attic.center.x, 6.15f, Attic.center.z);
            surface.size = new Vector3(Attic.size.x, 2.3f, Attic.size.z);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            // Keep them out of the doorway, so Walter can always get out.
            var keepOut = new GameObject("DoorKeepOut").AddComponent<NavMeshModifierVolume>();
            keepOut.transform.SetParent(group, false);
            keepOut.center = AtticDoor + new Vector3(-0.8f, 1.0f, 0f);
            keepOut.size = new Vector3(1.6f, 2.0f, 2.2f);
            keepOut.area = 1;                                        // Not Walkable

            LimitStepHeight(surface.agentTypeID, 0.25f);
            surface.BuildNavMesh();
            System.IO.Directory.CreateDirectory("Assets/Scenes/SampleScene");
            AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        }

        /// <summary>
        /// The default agent can step 0.75 m, enough to climb onto the stored boxes and tables.
        /// Lower it (in ProjectSettings/NavMeshAreas.asset; nothing else in the game uses a NavMesh).
        /// </summary>
        private static void LimitStepHeight(int agentTypeID, float climb)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
            if (asset.Length == 0) return;
            var so = new SerializedObject(asset[0]);
            var settings = so.FindProperty("m_Settings");
            for (int i = 0; i < settings.arraySize; i++)
            {
                var s = settings.GetArrayElementAtIndex(i);
                if (s.FindPropertyRelative("agentTypeID").intValue != agentTypeID) continue;
                s.FindPropertyRelative("agentClimb").floatValue = climb;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AnimatorController Controller()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Animations/Mannequin_Idle.anim");
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Animations/Mannequin_Walk.anim");
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            var walkState = sm.AddState("Walk");
            walkState.motion = walk;
            sm.defaultState = idleState;
            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.2f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walking");
            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.3f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walking");
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static NarrativeTrigger Beat(Transform parent, string name, NarrativeTrigger.Mode mode, Vector3 pos)
        {
            var go = new GameObject("Beat_" + name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go.AddComponent<NarrativeTrigger>();
        }
    }
}
