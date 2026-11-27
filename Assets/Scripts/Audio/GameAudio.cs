using System.Collections;
using System.Collections.Generic;
using LeftBehind.Loop;
using LeftBehind.Story;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LeftBehind.Audio
{
    /// <summary>
    /// All of the game's sound, loaded from Resources/Audio (sources in CREDITS.txt there).
    /// Kept across scenes; when a scene loads it sets up that loop's ambience:
    ///   Loop 1: wind outside and the dining room clock (ticking from the WallClock);
    ///   Loop 2: the clock and a little wind (the clock has stopped in Loop 3);
    ///   Loop 3: heavy rain, thunder after each lightning flash, and Emily's music box upstairs,
    ///           which stops when Walter steps into the study;
    ///   Main menu: the music box, quietly.
    /// Under each loop runs a quiet room tone (the empty house), different in each.
    /// The ambience can be hushed before a scare, so the house seems to hold its breath (see PacedScare).
    /// It also gives the player footsteps and breathing (louder near the mannequins), and plays
    /// one-shots for doors, switches and the tap, and the muffled sounds of the rest of the house.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private const float ThunderStart = 20.8f;     // where the thunderclap begins in Thunder.ogg
        private const float ThunderLength = 12f;

        private static GameAudio instance;
        private static AudioClip[] doorOpen, doorClose, footsteps, ticks;
        private static AudioClip rain, thunder, musicBox, wind, click, drip, waterRunning;
        private static AudioClip roomTone, breathing, tap, lampBuzz;

        /// <summary>Loudness of the ambience while it is hushed before a scare.</summary>
        private const float HushLevel = 0.25f;

        private AudioSource musicBoxSource;
        private readonly List<(AudioSource source, float volume)> ambience = new List<(AudioSource, float)>();
        private float hushLevel = 1f;
        private bool hushed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameAudio>();
            Load();
            SceneManager.sceneLoaded += (s, m) => { if (instance != null) instance.SetUpScene(); };
            StoryStateManager.FlagSet += instance.OnFlag;
            instance.SetUpScene();
        }

        private static void Load()
        {
            doorOpen = Resources.LoadAll<AudioClip>("Audio/DoorOpen");
            doorClose = Resources.LoadAll<AudioClip>("Audio/DoorClose");
            footsteps = Resources.LoadAll<AudioClip>("Audio/Footsteps");
            ticks = Resources.LoadAll<AudioClip>("Audio/Ticks");
            rain = Resources.Load<AudioClip>("Audio/Rain");
            thunder = Resources.Load<AudioClip>("Audio/Thunder");
            musicBox = Resources.Load<AudioClip>("Audio/MusicBox");
            wind = Resources.Load<AudioClip>("Audio/Wind");
            click = Resources.Load<AudioClip>("Audio/Click");
            drip = Resources.Load<AudioClip>("Audio/Drip");
            waterRunning = Resources.Load<AudioClip>("Audio/WaterRunning");
            roomTone = Resources.Load<AudioClip>("Audio/RoomTone");
            breathing = Resources.Load<AudioClip>("Audio/Breathing");
            tap = Resources.Load<AudioClip>("Audio/Tap");
            lampBuzz = Resources.Load<AudioClip>("Audio/LampBuzz");
        }

        // ------------------------------------------------------------------ per scene

        private void SetUpScene()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            musicBoxSource = null;
            ambience.Clear();
            hushed = false;
            hushLevel = 1f;

            var fpc = FindAnyObjectByType<FirstPersonController>();
            if (fpc != null && fpc.GetComponent<Footsteps>() == null)
                fpc.gameObject.AddComponent<Footsteps>().Configure(footsteps);
            if (fpc != null && fpc.GetComponent<Breathing>() == null)
                fpc.gameObject.AddComponent<Breathing>().Configure(breathing);

            var env = FindAnyObjectByType<LoopEnvironment>();
            int loop = env != null ? env.Loop : 0;
            var root = new GameObject("Ambience").transform;     // belongs to the scene, so it goes with it
            // The tick comes from the clock on the dining room wall, if it is there and still running.
            var wallClock = FindAnyObjectByType<WallClock>();
            var clockPos = wallClock != null ? wallClock.transform.position : new Vector3(-5f, 2.0f, 9.45f);
            bool clockRuns = wallClock == null || wallClock.Running;
            switch (loop)
            {
                case 0:
                    Loop2D(root, "MusicBox", musicBox, 0.22f, fromStart: true);
                    break;
                case 1:
                    Ambient(Loop2D(root, "Wind", wind, 0.16f));
                    Ambient(Loop2D(root, "RoomTone", roomTone, 0.2f));                   // the empty house, a low hum
                    if (clockRuns) Clock(root, clockPos);
                    break;
                case 2:
                    Ambient(Loop2D(root, "Wind", wind, 0.07f));
                    Ambient(Loop2D(root, "RoomTone", roomTone, 0.13f, pitch: 1.08f));    // a little warmer: a lived-in house
                    if (clockRuns) Clock(root, clockPos);
                    break;
                case 3:
                    Ambient(Loop2D(root, "Rain", rain, 0.3f));
                    Ambient(Loop2D(root, "RoomTone", roomTone, 0.12f, pitch: 0.9f));     // lower, under the storm
                    if (!StoryStateManager.Has("enteredStudyLoop3"))
                    {
                        // On a shelf in the children's room, upstairs; the player follows it. It winds
                        // down when he comes close (see MusicBox).
                        musicBoxSource = Loop3D(root, "MusicBox", musicBox, new Vector3(-1.0f, 3.4f, -1.6f), 0.6f, 2f, 22f);
                        if (musicBoxSource != null) musicBoxSource.gameObject.AddComponent<MusicBox>();
                    }
                    break;
            }
        }

        private void OnFlag(string flag)
        {
            if (flag == "enteredStudyLoop3" && musicBoxSource != null) StartCoroutine(FadeOut(musicBoxSource, 2.5f));
        }

        private void Ambient(AudioSource src)
        {
            if (src != null) ambience.Add((src, src.volume));
        }

        private static AudioSource Loop2D(Transform root, string name, AudioClip clip, float volume, bool fromStart = false, float pitch = 1f)
        {
            if (clip == null) return null;
            var src = new GameObject(name).AddComponent<AudioSource>();
            src.transform.SetParent(root, false);
            src.clip = clip;
            src.loop = true;
            src.volume = volume;
            src.pitch = pitch;
            src.spatialBlend = 0f;
            if (!fromStart) src.time = Random.Range(0f, clip.length * 0.8f);     // ambience never starts the same way twice
            src.Play();
            return src;
        }

        private static AudioSource Loop3D(Transform root, string name, AudioClip clip, Vector3 pos, float volume, float min, float max)
        {
            var src = Loop2D(root, name, clip, volume);
            if (src == null) return null;
            src.transform.position = pos;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = min;
            src.maxDistance = max;
            src.dopplerLevel = 0f;
            return src;
        }

        private static void Clock(Transform root, Vector3 pos)
        {
            if (ticks == null || ticks.Length == 0) return;
            var go = new GameObject("Clock");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1.5f;
            src.maxDistance = 20f;
            src.volume = 0.45f;
            src.dopplerLevel = 0f;
            go.AddComponent<ClockTicker>().Configure(src, ticks);
            instance.Ambient(src);
        }

        // ------------------------------------------------------------------ hush

        /// <summary>The house holds its breath (true): the ambience drops away until released (false).</summary>
        public static void Hush(bool on)
        {
            if (instance != null) instance.hushed = on;
        }

        private void Update()
        {
            float target = hushed ? HushLevel : 1f;
            if (Mathf.Approximately(hushLevel, target)) return;
            // It drops away over two seconds, and comes back more slowly.
            hushLevel = Mathf.MoveTowards(hushLevel, target, Time.deltaTime / (hushed ? 2f : 4f));
            foreach (var (source, volume) in ambience)
                if (source != null) source.volume = volume * hushLevel;
        }

        private static IEnumerator FadeOut(AudioSource src, float time)
        {
            float start = src.volume;
            for (float t = 0f; t < time && src != null; t += Time.deltaTime)
            {
                src.volume = Mathf.Lerp(start, 0f, t / time);
                yield return null;
            }
            if (src != null) src.Stop();
        }

        // ------------------------------------------------------------------ one-shots

        public static AudioSource PlayAt(AudioClip clip, Vector3 pos, float volume, float pitch = 1f)
        {
            if (clip == null) return null;
            var go = new GameObject("OneShot_" + clip.name);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1.5f;
            src.maxDistance = 20f;
            src.dopplerLevel = 0f;
            src.Play();
            Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
            return src;
        }

        /// <summary>A sound from elsewhere in the house, through walls or a floor: only its low end gets through.</summary>
        public static void Muffled(AudioClip clip, Vector3 pos, float volume, float pitch, float cutoff)
        {
            var src = PlayAt(clip, pos, volume, pitch);
            if (src == null) return;
            src.maxDistance = 30f;
            src.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = cutoff;
        }

        public static void Play2D(AudioClip clip, float volume, float pitch = 1f, bool ignorePause = false)
        {
            if (clip == null || instance == null) return;
            var src = instance.gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = ignorePause;
            src.Play();
            Destroy(src, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        public static AudioClip AFootstep => Pick(footsteps);

        public static AudioClip ADoorClose => Pick(doorClose);

        /// <summary>A twig knocking on a window pane.</summary>
        public static void Tap(Vector3 pos) => PlayAt(tap, pos, 0.7f, Random.Range(0.95f, 1.05f));

        /// <summary>A bulb buzzing as it fails.</summary>
        public static void LampBuzz(Vector3 pos) => PlayAt(lampBuzz, pos, 0.35f, Random.Range(0.9f, 1.1f));

        /// <summary>Walter catching his breath: one sharp breath in.</summary>
        public static void Gasp()
        {
            if (instance != null && breathing != null) instance.StartCoroutine(instance.PlayGasp());
        }

        private IEnumerator PlayGasp()
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = breathing;
            src.spatialBlend = 0f;
            src.volume = 0.75f;
            src.pitch = 1.45f;
            src.Play();
            yield return new WaitForSeconds(0.95f);       // just the breath in
            for (float t = 0f; t < 0.25f && src != null; t += Time.deltaTime)
            {
                src.volume = 0.75f * (1f - t / 0.25f);
                yield return null;
            }
            if (src != null) Destroy(src);
        }

        private static AudioClip Pick(AudioClip[] set) => set != null && set.Length > 0 ? set[Random.Range(0, set.Length)] : null;

        public static void DoorOpened(Vector3 pos) => PlayAt(Pick(doorOpen), pos, 0.55f, Random.Range(0.95f, 1.05f));

        public static void DoorClosed(Vector3 pos) => PlayAt(Pick(doorClose), pos, 0.5f, Random.Range(0.95f, 1.05f));

        /// <summary>A door that has shut behind Walter's back: only the latch, quietly.</summary>
        public static void DoorLatched(Vector3 pos, float volume) => PlayAt(Pick(doorClose), pos, volume, 1.15f);

        /// <summary>A locked door, or one that will never open: the handle and latch rattle.</summary>
        public static void DoorRattled(Vector3 pos) => PlayAt(Pick(doorClose), pos, 0.35f, 1.45f);

        public static void Switch(Vector3 pos) => PlayAt(click, pos, 0.5f, Random.Range(0.95f, 1.05f));

        public static void Switch2D() => Play2D(click, 0.3f);

        /// <summary>A drop from the kitchen tap landing in the sink.</summary>
        public static void Drip(Vector3 pos) => PlayAt(drip, pos, 0.4f, Random.Range(0.9f, 1.12f));

        /// <summary>The kitchen tap running; the tap plays it on its own looping source.</summary>
        public static AudioClip WaterRunning => waterRunning;

        /// <summary>Thunder after a lightning flash; it rumbles out over a few seconds.</summary>
        public static void Thunder(float delay)
        {
            if (instance != null && thunder != null) instance.StartCoroutine(instance.PlayThunder(delay));
        }

        private IEnumerator PlayThunder(float delay)
        {
            yield return new WaitForSeconds(delay);
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = thunder;
            src.spatialBlend = 0f;
            src.volume = Random.Range(0.6f, 0.85f);
            src.pitch = Random.Range(0.9f, 1.05f);
            src.time = ThunderStart;
            src.Play();
            float length = ThunderLength / src.pitch;
            float v = src.volume;
            for (float t = 0f; t < length && src != null; t += Time.deltaTime)
            {
                src.volume = v * Mathf.Clamp01((length - t) / 4f);   // fade out the tail
                yield return null;
            }
            if (src != null) Destroy(src);
        }

        /// <summary>Fades every sound out (the Remember ending).</summary>
        public static void FadeOutAll(float time)
        {
            if (instance != null) instance.StartCoroutine(instance.FadeListener(time));
        }

        private IEnumerator FadeListener(float time)
        {
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                AudioListener.volume = 1f - t / time;
                yield return null;
            }
            AudioListener.volume = 0f;
        }
    }

    /// <summary>
    /// Emily's music box in Loop 3. When Walter comes within a couple of metres of it, it winds down
    /// and stops, as if it knew he was there. Once he has gone back out onto the landing it starts
    /// again, quietly, from the beginning. (GameAudio fades it out for good when he enters the study.)
    /// </summary>
    public class MusicBox : MonoBehaviour
    {
        private const float StopWithin = 2.6f;
        private const float StartBeyond = 6.5f;

        private AudioSource source;
        private Transform player;
        private float baseVolume;
        private bool stopped;
        private Coroutine running;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            baseVolume = source.volume;
        }

        private void Update()
        {
            if (source == null || StoryStateManager.Has("enteredStudyLoop3")) return;
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc == null) return;
                player = fpc.transform;
            }
            float d = Vector3.Distance(player.position + Vector3.up * 1.5f, transform.position);
            if (!stopped && d < StopWithin)
            {
                stopped = true;
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(WindDown());
                ScarePacing.Mark();
            }
            else if (stopped && d > StartBeyond)
            {
                stopped = false;
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(StartAgain());
            }
        }

        private IEnumerator WindDown()
        {
            float v = source.volume, p = source.pitch;
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                float k = t / 1.6f;
                source.pitch = Mathf.Lerp(p, 0.55f, k * k);          // the spring running out
                source.volume = Mathf.Lerp(v, 0f, k * k * k);
                yield return null;
            }
            source.Stop();
            source.pitch = 1f;
        }

        private IEnumerator StartAgain()
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            source.pitch = 1f;
            source.time = 0f;
            source.volume = 0f;
            source.Play();
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                source.volume = baseVolume * 0.7f * t / 3f;
                yield return null;
            }
        }
    }

    /// <summary>A wall clock: one tick a second, alternating between recorded ticks so it never sounds looped.</summary>
    public class ClockTicker : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip[] ticks;
        private float next;
        private int i;

        public void Configure(AudioSource src, AudioClip[] clips)
        {
            source = src;
            ticks = clips;
        }

        private void Update()
        {
            if (source == null || ticks == null || ticks.Length == 0 || Time.time < next) return;
            next = Time.time + 1f;
            source.pitch = i % 2 == 0 ? 1f : 0.94f;     // tick, tock
            source.PlayOneShot(ticks[i % ticks.Length]);
            i++;
        }
    }
}
