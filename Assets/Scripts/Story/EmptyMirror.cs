using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LeftBehind.Story
{
    /// <summary>
    /// A mirror that shows the room behind Walter and nothing else: not him, not the flashlight in
    /// his hand, only its light. A second camera renders the room from the mirrored viewpoint into a
    /// texture, which the glass (the LeftBehind/Mirror shader) shows; that camera never draws the
    /// player. It only renders while the glass is on screen and near.
    /// The first time Walter stands and looks into it, he says so.
    /// Placed by the editor's DreadBuilder.
    /// </summary>
    public class EmptyMirror : MonoBehaviour
    {
        private static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");

        [Tooltip("The glass. This object's +Z (not the glass's) points out of the mirror, into the room.")]
        [SerializeField] private Renderer glass;
        [SerializeField] private float renderWithin = 9f;
        [Tooltip("Fraction of the screen resolution the reflection is drawn at.")]
        [SerializeField] private float resolution = 0.5f;

        [Header("Walter notices")]
        [SerializeField] private float noticeWithin = 2.6f;
        [SerializeField] private string flag = "sawEmptyMirror";
        [SerializeField] private DialogueData lines;

        private Camera mirrorCam;
        private RenderTexture texture;
        private Transform player;
        private Renderer[] hidden = new Renderer[0];
        private bool[] wasOn = new bool[0];
        private float lookedFor;

        public void Configure(Renderer glassRenderer, DialogueData noticed)
        {
            glass = glassRenderer;
            lines = noticed;
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginContextRendering += BeforeFrame;
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            RenderPipelineManager.endCameraRendering += AfterCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginContextRendering -= BeforeFrame;
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            RenderPipelineManager.endCameraRendering -= AfterCamera;
            if (mirrorCam != null) mirrorCam.enabled = false;
        }

        private void OnDestroy()
        {
            if (texture != null) texture.Release();
            if (mirrorCam != null) Destroy(mirrorCam.gameObject);
        }

        private void Update()
        {
            if (player == null)
            {
                var fpc = FindAnyObjectByType<FirstPersonController>();
                if (fpc == null) return;
                player = fpc.transform;
            }
            Notice();
        }

        /// <summary>The first time he stands close and looks into the glass, he says what he sees.</summary>
        private void Notice()
        {
            if (lines == null || StoryStateManager.Has(flag) || DialogueManager.Busy) return;
            var cam = Camera.main;
            if (cam == null || glass == null) return;
            var toGlass = glass.bounds.center - cam.transform.position;
            bool looking = toGlass.magnitude < noticeWithin
                && Vector3.Dot(cam.transform.forward, toGlass.normalized) > 0.9f
                && Vector3.Dot(transform.forward, -toGlass.normalized) > 0.5f;
            lookedFor = looking ? lookedFor + Time.deltaTime : 0f;
            if (lookedFor < 1.2f) return;
            StoryStateManager.Set(flag);
            ScarePacing.Mark();
            DialogueManager.Play(lines);
        }

        // ------------------------------------------------------------------ the reflection

        private void BeforeFrame(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
        {
            var main = Camera.main;
            if (main == null || glass == null) return;
            bool show = glass.isVisible && Vector3.Distance(main.transform.position, glass.bounds.center) < renderWithin
                && Vector3.Dot(transform.forward, main.transform.position - glass.transform.position) > 0f;
            if (!show)
            {
                if (mirrorCam != null) mirrorCam.enabled = false;
                return;
            }
            Prepare(main);
            Place(main);
            // Everything that is Walter: his body, and whatever he is holding now (the flashlight).
            if (player != null)
            {
                hidden = player.GetComponentsInChildren<Renderer>(true);
                if (wasOn.Length != hidden.Length) wasOn = new bool[hidden.Length];
            }
            mirrorCam.enabled = true;
        }

        private void Prepare(Camera main)
        {
            int w = Mathf.Max(64, Mathf.RoundToInt(main.pixelWidth * resolution));
            int h = Mathf.Max(64, Mathf.RoundToInt(main.pixelHeight * resolution));
            if (texture == null || texture.width != w || texture.height != h)
            {
                if (texture != null) texture.Release();
                texture = new RenderTexture(w, h, 24) { name = "MirrorReflection", filterMode = FilterMode.Point };
                if (mirrorCam != null) mirrorCam.targetTexture = texture;
                glass.material.SetTexture(ReflectionTexId, texture);
            }
            if (mirrorCam != null) return;
            var go = new GameObject("MirrorCamera") { hideFlags = HideFlags.DontSave };
            mirrorCam = go.AddComponent<Camera>();
            mirrorCam.enabled = false;
            mirrorCam.targetTexture = texture;
            mirrorCam.clearFlags = CameraClearFlags.SolidColor;
            mirrorCam.backgroundColor = RenderSettings.fogColor;
            mirrorCam.depth = main.depth - 1;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = true;
            glass.material.SetTexture(ReflectionTexId, texture);
        }

        /// <summary>Puts the mirror camera where the main camera's reflection is, clipped at the glass.</summary>
        private void Place(Camera main)
        {
            var n = transform.forward;
            var p = glass.transform.position;
            Vector3 Reflect(Vector3 v) => v - 2f * Vector3.Dot(v, n) * n;
            var eye = main.transform.position;
            var pos = eye - 2f * Vector3.Dot(eye - p, n) * n;
            mirrorCam.transform.SetPositionAndRotation(pos,
                Quaternion.LookRotation(Reflect(main.transform.forward), Reflect(main.transform.up)));
            mirrorCam.fieldOfView = main.fieldOfView;
            mirrorCam.aspect = main.aspect;
            mirrorCam.nearClipPlane = main.nearClipPlane;
            mirrorCam.farClipPlane = main.farClipPlane;
            mirrorCam.backgroundColor = RenderSettings.fogColor;
            mirrorCam.ResetProjectionMatrix();
            // Nothing behind the glass (the wall it hangs on) may get in the way.
            var w2c = mirrorCam.worldToCameraMatrix;
            var cp = w2c.MultiplyPoint(p + n * 0.01f);
            var cn = w2c.MultiplyVector(n).normalized;
            mirrorCam.projectionMatrix = mirrorCam.CalculateObliqueMatrix(new Vector4(cn.x, cn.y, cn.z, -Vector3.Dot(cp, cn)));
        }

        private void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam != mirrorCam) return;
            for (int i = 0; i < hidden.Length; i++)
            {
                if (hidden[i] == null) continue;
                wasOn[i] = hidden[i].forceRenderingOff;
                hidden[i].forceRenderingOff = true;
            }
        }

        private void AfterCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam != mirrorCam) return;
            for (int i = 0; i < hidden.Length; i++)
                if (hidden[i] != null) hidden[i].forceRenderingOff = wasOn[i];
        }
    }
}
