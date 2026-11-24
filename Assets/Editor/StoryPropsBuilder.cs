using LeftBehind.Interaction;
using LeftBehind.Loop;
using LeftBehind.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Voice = LeftBehind.Story.DialogueData.Voice;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Places the Part 1 props that change from loop to loop, under a "StoryProps" object in the
    /// open scene, with Walter's lines for each (in Assets/Story/Dialogue, kept if they exist):
    ///   wall clock      dining room; 15:40 and ticking, 18:50 and ticking, stopped at 23:52;
    ///   kitchen tap     drips (and starts again after being turned off), drips, running;
    ///   fridge calendar January 2025, October 2006, October 2004 in Ruth's hand;
    ///   pills           an organiser still full, one bottle from the clinic, none yet;
    ///   wedding ring    Walter's on the dresser, Ruth's on the dresser, none (she is wearing it).
    /// Rebuilding replaces the StoryProps object. Called at the end of each loop's story set-up;
    /// Menu: Left Behind > Story > Place Story Props (for the open loop scene).
    /// </summary>
    public static class StoryPropsBuilder
    {
        private const string RootName = "StoryProps";
        private static Transform root;
        private static int loop;

        [MenuItem("Left Behind/Story/Place Story Props")]
        public static void PlaceInOpenScene()
        {
            var env = Object.FindFirstObjectByType<LoopEnvironment>();
            if (env == null) { Debug.LogError("Story props: this scene has no LoopEnvironment."); return; }
            Place(env.Loop);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        public static void Place(int loopNumber)
        {
            loop = loopNumber;
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            root = new GameObject(RootName).transform;
            Physics.SyncTransforms();

            WallClockProp();
            KitchenTapProp();
            CalendarProp();
            PillsProp();
            RingProp();
        }

        // ------------------------------------------------------------------ wall clock

        private static void WallClockProp()
        {
            var commode = Find("commode_1", "Loop2House/Furniture/Dining/Commode");
            if (commode == null) return;
            var b = Bounds(commode);

            // Hang it on the wall the commode stands against.
            var from = new Vector3(b.center.x, 1.85f, b.center.z);
            RaycastHit best = default;
            float bestDist = float.MaxValue;
            foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                foreach (var hit in Physics.RaycastAll(from, dir, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.distance < bestDist && !hit.transform.IsChildOf(commode.transform))
                    {
                        best = hit;
                        bestDist = hit.distance;
                    }
            if (bestDist == float.MaxValue) { Debug.LogWarning("Story props: no wall behind the commode for the clock."); return; }

            var clock = new GameObject("WallClock");
            clock.transform.SetParent(root, false);
            clock.transform.position = best.point + best.normal * 0.026f;
            clock.transform.rotation = Quaternion.LookRotation(best.normal, Vector3.up);

            var wood = Loop3Builder.Tinted("ClockWood", new Color(0.24f, 0.14f, 0.07f));
            var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Exterior/Ext_DarkMetal.mat");
            var red = Loop3Builder.Tinted("ClockHandRed", new Color(0.6f, 0.09f, 0.07f));
            Prim(PrimitiveType.Cylinder, "Body", clock.transform, Vector3.zero, Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.34f, 0.025f, 0.34f), wood);
            Prim(PrimitiveType.Quad, "Face", clock.transform, new Vector3(0f, 0f, 0.0262f), Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.3f, 0.3f, 1f), Loop3Builder.PaperMaterial("ClockFaceOval"));
            var hour = Hand(clock.transform, "HourHand", 0.075f, 0.011f, 0.029f, dark);
            var minute = Hand(clock.transform, "MinuteHand", 0.115f, 0.007f, 0.0315f, dark);
            var second = Hand(clock.transform, "SecondHand", 0.125f, 0.0025f, 0.034f, red);
            Prim(PrimitiveType.Cylinder, "Pin", clock.transform, new Vector3(0f, 0f, 0.035f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.012f, 0.004f, 0.012f), dark);

            float time = loop == 1 ? 15 * 3600 + 40 * 60 : loop == 2 ? 18 * 3600 + 50 * 60 : 23 * 3600 + 52 * 60 + 17;
            clock.AddComponent<WallClock>().Configure(hour, minute, second, time, loop != 3);
            var col = clock.AddComponent<BoxCollider>();
            col.size = new Vector3(0.4f, 0.4f, 0.12f);
            var look = clock.AddComponent<Examinable>();
            look.Configure("Look at the clock");

            var lines = loop == 1
                ? D("L1_Clock",
                    (Voice.Inner, "", "Twenty to four."),
                    (Voice.Inner, "", "In the afternoon? It gets dark so early now, I can never tell."))
                : loop == 2
                    ? D("L2_Clock",
                        (Voice.Inner, "", "Ten to seven. Dinner's at seven."),
                        (Voice.Inner, "", "She'll be back by seven. She always is."))
                    : D("L3_Clock",
                        (Voice.Inner, "", "It's stopped. Eight minutes to midnight."),
                        (Voice.Inner, "", "Ruth winds it every Sunday. She'd never let it stop."));
            Beat("Clock", look, $"sawClockLoop{loop}", lines);
        }

        private static Transform Hand(Transform clock, string name, float length, float width, float z, Material mat)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(clock, false);
            pivot.localPosition = new Vector3(0f, 0f, z);
            Prim(PrimitiveType.Cube, "Blade", pivot, new Vector3(0f, length * 0.5f - 0.012f, 0f), Quaternion.identity,
                new Vector3(width, length, 0.003f), mat);
            return pivot;
        }

        // ------------------------------------------------------------------ kitchen tap

        private static void KitchenTapProp()
        {
            var sink = Find("Furniture_Added/Kitchen/Sink", "Loop2House/Furniture/Kitchen/Kitchen_Sink");
            if (sink == null) return;
            var mf = sink.GetComponentInChildren<MeshFilter>();
            var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
            int metal = System.Array.FindIndex(mats, m => m != null && m.name == "LightMetal");
            int white = System.Array.FindIndex(mats, m => m != null && m.name == "White");
            if (metal < 0 || white < 0) { Debug.LogWarning("Story props: the sink mesh has no tap or basin."); return; }
            var faucet = SubmeshBounds(mf, metal, out var faucetVerts);
            var basin = SubmeshBounds(mf, white, out _);

            // The spout is the end of the tap that reaches out over the basin.
            var toBasin = basin.center - faucet.center;
            toBasin.y = 0f;
            toBasin.Normalize();
            Vector3 tip = faucet.center;
            float far = float.MinValue;
            foreach (var v in faucetVerts)
            {
                if (v.y < faucet.max.y - 0.09f) continue;
                float d = Vector3.Dot(v - faucet.center, toBasin);
                if (d > far) { far = d; tip = v; }
            }
            tip.y -= 0.008f;
            float basinY = basin.min.y + 0.01f;

            var sb = Bounds(sink);
            var tapGo = new GameObject("KitchenTap");
            tapGo.transform.SetParent(root, false);
            tapGo.transform.position = sb.center;
            var col = tapGo.AddComponent<BoxCollider>();
            col.size = sb.size + Vector3.one * 0.04f;      // encloses the sink, so the ray finds the tap first

            var spout = new GameObject("Spout").transform;
            spout.SetParent(tapGo.transform, false);
            spout.position = tip;
            var water = Loop3Builder.Tinted("Water", new Color(0.62f, 0.7f, 0.78f));
            water.SetFloat("_Smoothness", 0.9f);
            water.EnableKeyword("_EMISSION");                                   // catches a little light, even in a dark kitchen
            water.SetColor("_EmissionColor", new Color(0.22f, 0.25f, 0.28f));
            water.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(water);
            float half = (tip.y - basinY) * 0.5f;
            var stream = Prim(PrimitiveType.Cylinder, "Stream", tapGo.transform, Vector3.zero, Quaternion.identity,
                new Vector3(0.014f, half, 0.014f), water);      // a little wider than real, to survive the low render scale
            stream.transform.position = new Vector3(tip.x, basinY + half, tip.z);
            var drop = Prim(PrimitiveType.Sphere, "Drop", tapGo.transform, Vector3.zero, Quaternion.identity,
                Vector3.one * 0.016f, water);
            drop.transform.position = tip;
            drop.SetActive(false);

            var tap = tapGo.AddComponent<KitchenTap>();
            tap.Configure(loop == 3 ? KitchenTap.State.Running : KitchenTap.State.Dripping, spout, basinY,
                stream.transform, drop.transform, loop == 3 ? 0f : 40f);

            var lines = loop == 1
                ? D("L1_Tap",
                    (Voice.Inner, "", "It still drips."),
                    (Voice.Inner, "", "I keep meaning to have someone fix it. Don't I?"))
                : loop == 2
                    ? D("L2_Tap",
                        (Voice.Inner, "", "All the way, like the note says."),
                        (Voice.Inner, "", "Ruth used to say the dripping kept her awake."))
                    : D("L3_Tap",
                        (Voice.Inner, "", "Who left the tap running?"),
                        (Voice.Inner, "", "Somebody's been washing something."));
            Beat("Tap", tap, $"usedTapLoop{loop}", lines);
        }

        // ------------------------------------------------------------------ calendar

        private static void CalendarProp()
        {
            var fridge = Find("Kitchen_Fridge", "Loop2House/Furniture/Kitchen/Fridge");
            if (fridge == null) return;
            var b = Bounds(fridge);

            // Upper left of the fridge door, clear of the note (Loop 2) and the clinic card (Loop 3).
            var cal = Prim(PrimitiveType.Quad, "FridgeCalendar", root, Vector3.zero, Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.22f, 0.3f, 1f), Loop3Builder.PaperMaterial($"Calendar_Loop{loop}"));
            cal.transform.position = new Vector3(b.min.x + 0.15f, 1.62f, b.max.z + 0.003f);
            cal.AddComponent<BoxCollider>().size = new Vector3(1.3f, 1.2f, 0.2f);

            string body, title = "Calendar on the fridge";
            DialogueData after;
            if (loop == 1)
            {
                body = "JANUARY 2025\n\n" +
                       "X X X X X\nX X X X X X X\nX X X X X X X\nX X X X X X X\nX X X X X\n\n" +
                       "WHAT AM I COUNTING DOWN TO?";
                after = D("L1_Calendar",
                    (Voice.Inner, "", "Every day crossed off."),
                    (Voice.Inner, "", "Crossed off until what?"));
            }
            else if (loop == 2)
            {
                body = "OCTOBER 2006\n\n" +
                       "Fri 6 — THEY COME BACK?\n" +
                       "Fri 13 — STAY OUT OF THE STUDY\n" +
                       "Fri 20 — THEY COME BACK?\n" +
                       "Fri 27 — THEY COME BACK?\n\n" +
                       "EVERY SUNDAY: WIND THE CLOCK.\nRUTH ALWAYS DID.";
                after = D("L2_Calendar",
                    (Voice.Inner, "", "Every Friday. They might come back on a Friday."),
                    (Voice.Inner, "", "And the thirteenth, circled twice. Why that day?"));
            }
            else
            {
                title = "Ruth's calendar";
                body = "OCTOBER 2004\n\n" +
                       "Fri 1 — Sam, swimming 5pm\n" +
                       "Fri 8 — Emily's school play, 6pm\n" +
                       "Sat 16 — Mum's birthday. Take the kids.\n" +
                       "Mon 18 — Sam, dentist 4.15\n" +
                       "Tue 19 — Walter, memory clinic 10.30\n\n" +
                       "DON'T LET HIM CANCEL.";
                after = D("L3_Calendar",
                    (Voice.Inner, "", "Ruth's writing."),
                    (Voice.Inner, "", "Her mother's birthday on Saturday. The clinic on Tuesday. She had it all planned."));
            }
            cal.AddComponent<ReadableNote>().Configure($"calendarLoop{loop}", title, body, $"readCalendarLoop{loop}",
                after, prompt: "Read the calendar");
        }

        // ------------------------------------------------------------------ pills

        private static void PillsProp()
        {
            if (loop == 3) return;       // the night of the crime: no diagnosis yet, only Ruth's appointment card
            var stand = Find("UpperFloor/Furniture/MasterBedroom/Nightstand_R", "Loop2House/Furniture/MasterBedroom/Nightstand_R");
            if (stand == null) return;
            var b = Bounds(stand);
            var pills = new GameObject("Pills");
            pills.transform.SetParent(root, false);
            pills.transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
            CheckClear(pills.transform.position, new Vector3(0.3f, 0.12f, 0.3f), stand);

            var amber = Loop3Builder.Tinted("PillBottle", new Color(0.55f, 0.29f, 0.07f));
            var white = Loop3Builder.Tinted("PillLabel", new Color(0.9f, 0.89f, 0.84f));
            if (loop == 1)
            {
                // A week's organiser, every compartment still full, and two bottles behind it.
                var box = Loop3Builder.Tinted("PillOrganiser", new Color(0.55f, 0.68f, 0.8f));
                var lid = Loop3Builder.Tinted("PillOrganiserLid", new Color(0.38f, 0.53f, 0.72f));
                Prim(PrimitiveType.Cube, "Organiser", pills.transform, new Vector3(0f, 0.011f, -0.06f), Quaternion.identity,
                    new Vector3(0.21f, 0.022f, 0.05f), box);
                for (int i = 0; i < 7; i++)
                    Prim(PrimitiveType.Cube, "Lid", pills.transform, new Vector3(-0.09f + i * 0.03f, 0.025f, -0.06f),
                        Quaternion.identity, new Vector3(0.026f, 0.006f, 0.046f), lid);
                Bottle(pills.transform, new Vector3(-0.05f, 0f, 0.07f), amber, white);
                Bottle(pills.transform, new Vector3(0.04f, 0f, 0.09f), amber, white);
            }
            else Bottle(pills.transform, Vector3.zero, amber, white);

            var col = pills.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.06f, 0f);
            col.size = new Vector3(0.36f, 0.16f, 0.36f);
            var look = pills.AddComponent<Examinable>();
            look.Configure("Look at the pills");

            var lines = loop == 1
                ? D("L1_Pills",
                    (Voice.Inner, "", "Monday, Tuesday, Wednesday... they're all still full."),
                    (Voice.Inner, "", "Have I taken them today? What is today?"))
                : D("L2_Pills",
                    (Voice.Inner, "", "From the memory clinic. 'One each morning.' It's never been opened."),
                    (Voice.Inner, "", "Ruth wanted me to go, so I went. There's nothing wrong with me."));
            Beat("Pills", look, $"sawPillsLoop{loop}", lines);
        }

        private static void Bottle(Transform parent, Vector3 local, Material amber, Material label)
        {
            Prim(PrimitiveType.Cylinder, "Bottle", parent, local + new Vector3(0f, 0.04f, 0f), Quaternion.identity,
                new Vector3(0.04f, 0.04f, 0.04f), amber);
            Prim(PrimitiveType.Cylinder, "Label", parent, local + new Vector3(0f, 0.04f, 0f), Quaternion.identity,
                new Vector3(0.0405f, 0.018f, 0.0405f), label);
            Prim(PrimitiveType.Cylinder, "Cap", parent, local + new Vector3(0f, 0.087f, 0f), Quaternion.identity,
                new Vector3(0.043f, 0.008f, 0.043f), label);
        }

        // ------------------------------------------------------------------ wedding ring

        private static void RingProp()
        {
            if (loop == 3) return;       // tonight Ruth is still wearing hers
            var dresser = Find("UpperFloor/Furniture/MasterBedroom/Dresser", "Loop2House/Furniture/MasterBedroom/Dresser");
            if (dresser == null) return;
            var b = Bounds(dresser);

            // A small dish on the dresser, on the first spot that is clear of anything standing there.
            var spot = new Vector3(b.center.x, b.max.y, b.center.z);
            bool longX = b.size.x > b.size.z;
            foreach (float off in new[] { 0.35f, -0.35f, 0f, 0.6f, -0.6f })
            {
                var p = new Vector3(b.center.x + (longX ? off : 0f), b.max.y, b.center.z + (longX ? 0f : off));
                if (IsClear(p, new Vector3(0.14f, 0.1f, 0.14f), dresser)) { spot = p; break; }
            }

            var dish = Prim(PrimitiveType.Cylinder, "RingDish", root, Vector3.zero, Quaternion.identity,
                new Vector3(0.09f, 0.005f, 0.09f), Loop3Builder.Tinted("Dish", new Color(0.9f, 0.88f, 0.82f)));
            dish.transform.position = spot + new Vector3(0f, 0.005f, 0f);

            var gold = Loop3Builder.Tinted("Gold", new Color(0.85f, 0.64f, 0.24f));
            gold.SetFloat("_Metallic", 0.85f);
            gold.SetFloat("_Smoothness", 0.6f);
            EditorUtility.SetDirty(gold);
            var ring = new GameObject("WeddingRing");
            ring.transform.SetParent(root, false);
            ring.transform.position = spot + new Vector3(0.012f, 0.0125f, -0.008f);
            ring.AddComponent<MeshFilter>().sharedMesh = RingMesh();
            var mr = ring.AddComponent<MeshRenderer>();
            mr.sharedMaterial = gold;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            var col = ring.AddComponent<BoxCollider>();      // the ring is 2 cm across; make it easy to aim at
            col.center = new Vector3(-0.012f, 0.03f, 0.008f);
            col.size = new Vector3(0.22f, 0.1f, 0.22f);

            var pickup = ring.AddComponent<PickupItem>();
            if (loop == 1)
                pickup.Configure("Take the ring", "weddingRing", "Wedding ring",
                    "My wedding ring. It won't go over the knuckle any more. Engraved inside: R & W, 1989.",
                    "hasWeddingRing", D("L1_Ring",
                        (Voice.Inner, "", "My ring. When did I take it off?"),
                        (Voice.Inner, "", "R and W. Ruth and Walter.")), hide: true);
            else
                pickup.Configure("Take the ring", "ruthRing", "Ruth's ring",
                    "Ruth's wedding ring, left on the dresser. She never takes it off, not even to sleep.",
                    "hasRuthRing", D("L2_Ring",
                        (Voice.Inner, "", "Ruth's ring."),
                        (Voice.Inner, "", "She went to her mother's and left her ring behind?")), hide: true);
            EditorUtility.SetDirty(pickup);
        }

        /// <summary>A plain band, 19 mm across, lying flat. Saved once as an asset so scenes can refer to it.</summary>
        private static Mesh RingMesh()
        {
            const string path = "Assets/Story/Meshes/Ring.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            const int seg = 20, side = 8;
            const float major = 0.0095f, minor = 0.0022f;
            var verts = new Vector3[(seg + 1) * (side + 1)];
            var tris = new int[seg * side * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var c = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int j = 0; j <= side; j++)
                {
                    float t = j * Mathf.PI * 2f / side;
                    verts[i * (side + 1) + j] = c * (major + minor * Mathf.Cos(t)) + Vector3.up * (minor * Mathf.Sin(t));
                }
            }
            int k = 0;
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b2 = a + side + 1;
                    tris[k++] = a; tris[k++] = a + 1; tris[k++] = b2;
                    tris[k++] = b2; tris[k++] = a + 1; tris[k++] = b2 + 1;
                }
            mesh = new Mesh { name = "Ring", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            System.IO.Directory.CreateDirectory("Assets/Story/Meshes");
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject Find(string loop1And3, string loop2)
        {
            var go = GameObject.Find(loop == 2 ? loop2 : loop1And3);
            if (go == null) Debug.LogWarning($"Story props: could not find {(loop == 2 ? loop2 : loop1And3)} in Loop {loop}.");
            return go;
        }

        private static void Beat(string name, Interactable target, string flag, DialogueData lines)
        {
            var go = new GameObject("Beat_" + name);
            go.transform.SetParent(root, false);
            go.transform.position = target.transform.position;
            go.AddComponent<NarrativeTrigger>().Configure(NarrativeTrigger.Mode.Use, flag, lines, "", useTarget: target);
        }

        private static DialogueData D(string name, params (Voice voice, string speaker, string text)[] lines) =>
            StoryContentBuilder.D(name, lines);

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Quaternion localRot,
            Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static Bounds SubmeshBounds(MeshFilter mf, int submesh, out System.Collections.Generic.List<Vector3> world)
        {
            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            world = new System.Collections.Generic.List<Vector3>();
            foreach (int i in mesh.GetTriangles(submesh)) world.Add(mf.transform.TransformPoint(verts[i]));
            var b = new Bounds(world[0], Vector3.zero);
            foreach (var v in world) b.Encapsulate(v);
            return b;
        }

        /// <summary>True when nothing but the surface itself is standing in this box (centred above <paramref name="baseCentre"/>).</summary>
        private static bool IsClear(Vector3 baseCentre, Vector3 size, GameObject surface)
        {
            var box = new Bounds(baseCentre + Vector3.up * (size.y * 0.5f + 0.01f), size);
            foreach (var r in Object.FindObjectsByType<Renderer>())
            {
                if (r.transform.IsChildOf(surface.transform) || r.transform.IsChildOf(root)) continue;
                if (r.bounds.Intersects(box)) return false;
            }
            return true;
        }

        private static void CheckClear(Vector3 baseCentre, Vector3 size, GameObject surface)
        {
            if (!IsClear(baseCentre, size, surface))
                Debug.LogWarning($"Story props: something already stands on {surface.name} in Loop {loop}.");
        }
    }
}
