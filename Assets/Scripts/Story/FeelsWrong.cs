using System.Collections;
using LeftBehind.CameraSystem;
using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// Something wrong to look at (the faceless photo, the full pill box, the stopped clock, the
    /// missing knife, anything that has moved behind Walter's back). When he examines it the view
    /// rolls slightly off level, and stays that way while he talks about it or holds it.
    /// Placed by the editor's DreadBuilder; ChangesWhileUnseen adds it to whatever it moves.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class FeelsWrong : MonoBehaviour
    {
        private const float MinSeconds = 2.5f;

        private Interactable interactable;
        private PlayerInteractor holder;
        private Coroutine running;

        private void Awake() => interactable = GetComponent<Interactable>();

        private void OnEnable() => interactable.Used += OnUsed;

        private void OnDisable()
        {
            interactable.Used -= OnUsed;
            if (running != null && CameraDirector.Instance != null) CameraDirector.Instance.Tilt(false);
            running = null;
        }

        private void OnUsed(PlayerInteractor by)
        {
            holder = by;
            if (running == null) running = StartCoroutine(TiltWhileLooking());
        }

        private IEnumerator TiltWhileLooking()
        {
            var director = CameraDirector.Instance;
            if (director == null) { running = null; yield break; }
            director.Tilt(true);
            float start = Time.unscaledTime;
            // A frame for the conversation (or the inspection) to begin.
            yield return null;
            while (Time.unscaledTime - start < MinSeconds || DialogueManager.Busy || NoteReader.IsOpen || Held())
                yield return null;
            director.Tilt(false);
            running = null;
        }

        private bool Held() => holder != null && holder.Focus == interactable;
    }
}
