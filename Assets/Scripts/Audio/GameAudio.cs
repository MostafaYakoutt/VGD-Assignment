using System.Collections;
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
    ///   Loop 1: wind outside and the dining room clock;
    ///   Loop 2: the clock and a little wind;
    ///   Loop 3: heavy rain, thunder after each lightning flash, and Emily's music box upstairs,
    ///           which stops when Walter steps into the study;
    ///   Main menu: the music box, quietly.
    /// It also gives the player footsteps, and plays one-shots for doors, switches and the rewind.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private const float ThunderStart = 20.8f;     // where the thunderclap begins in Thunder.ogg
        private const float ThunderLength = 12f;

        private static GameAudio instance;
        private static AudioClip[] doorOpen, doorClose, footsteps, ticks;
        private static AudioClip rain, thunder, musicBox, wind, rewind, click;

        private AudioSource musicBoxSource;

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
            SceneManager.sceneLoaded += (s, m) => instance.SetUpScene();
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
            rewind = Resources.Load<AudioClip>("Audio/TapeRewind");
            click = Resources.Load<AudioClip>("Audio/Click");
        }

        // ------------------------------------------------------------------ per scene

        private void SetUpScene()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            musicBoxSource = null;

            var fpc = FindAnyObjectByType<FirstPersonController>();
            if (fpc != null && fpc.GetComponent<Footsteps>() == null)
                fpc.gameObject.AddComponent<Footsteps>().Configure(footsteps);

            var env = FindAnyObjectByType<LoopEnvironment>();
            int loop = env != null ? env.Loop : 0;
            var root = new GameObject("Ambience").transform;     // belongs to the scene, so it goes with it
            switch (loop)
            {
                case 0:
                    Loop2D(root, "MusicBox", musicBox, 0.22f, fromStart: true);
                    break;
                case 1:
                    Loop2D(root, "Wind", wind, 0.16f);
                    Clock(root, new Vector3(-5f, 2.0f, 9.45f));
                    break;
                case 2:
                    Loop2D(root, "Wind", wind, 0.07f);
                    Clock(root, new Vector3(-5f, 2.0f, 9.45f));
                    break;
                case 3:
                    Loop2D(root, "Rain", rain, 0.3f);
                    if (!StoryStateManager.Has("enteredStudyLoop3"))
                    {
                        // On a shelf in the children's room, upstairs; the player follows it.
                        musicBoxSource = Loop3D(root, "MusicBox", musicBox, new Vector3(-1.0f, 3.4f, -1.6f), 0.6f, 2f, 22f);
                    }
                    break;
            }
        }

        private void OnFlag(string flag)
        {
            if (flag == "enteredStudyLoop3" && musicBoxSource != null) StartCoroutine(FadeOut(musicBoxSource, 2.5f));
        }

        private static AudioSource Loop2D(Transform root, string name, AudioClip clip, float volume, bool fromStart = false)
        {
            if (clip == null) return null;
            var src = new GameObject(name).AddComponent<AudioSource>();
            src.transform.SetParent(root, false);
            src.clip = clip;
            src.loop = true;
            src.volume = volume;
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

        public static void PlayAt(AudioClip clip, Vector3 pos, float volume, float pitch = 1f)
        {
            if (clip == null) return;
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

        private static AudioClip Pick(AudioClip[] set) => set != null && set.Length > 0 ? set[Random.Range(0, set.Length)] : null;

        public static void DoorOpened(Vector3 pos) => PlayAt(Pick(doorOpen), pos, 0.55f, Random.Range(0.95f, 1.05f));

        public static void DoorClosed(Vector3 pos) => PlayAt(Pick(doorClose), pos, 0.5f, Random.Range(0.95f, 1.05f));

        /// <summary>A locked door, or one that will never open: the handle and latch rattle.</summary>
        public static void DoorRattled(Vector3 pos) => PlayAt(Pick(doorClose), pos, 0.35f, 1.45f);

        public static void Switch(Vector3 pos) => PlayAt(click, pos, 0.5f, Random.Range(0.95f, 1.05f));

        public static void Switch2D() => Play2D(click, 0.3f);

        public static void TapeRewind() => Play2D(rewind, 0.75f, 1f, ignorePause: true);

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
