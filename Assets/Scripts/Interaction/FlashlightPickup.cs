using UnityEngine;

namespace LeftBehind.Interaction
{
    /// <summary>
    /// The flashlight lying in the world. Using it with E hands the model
    /// to the player's PlayerFlashlight, which holds it in first person.
    /// </summary>
    public class FlashlightPickup : Interactable
    {
        [SerializeField] private Light beam;
        [Tooltip("Renderer with the bulb material; its emission follows the beam.")]
        [SerializeField] private Renderer bulbRenderer;
        [Tooltip("Index of the bulb material on bulbRenderer.")]
        [SerializeField] private int bulbMaterialIndex = 3;
        [Tooltip("Outline shown while the flashlight can be picked up.")]
        [SerializeField] private GameObject outline;

        public Light Beam => beam;
        public Renderer BulbRenderer => bulbRenderer;
        public int BulbMaterialIndex => bulbMaterialIndex;

        private void Reset()
        {
            promptText = "Pick up flashlight";
            beam = GetComponentInChildren<Light>(true);
            bulbRenderer = GetComponentInChildren<Renderer>();
        }

        public override void Interact(PlayerInteractor interactor)
        {
            var holder = interactor.GetComponent<PlayerFlashlight>();
            if (holder == null || holder.HasFlashlight) return;

            if (outline != null) outline.SetActive(false);
            holder.Equip(this);
            enabled = false;
        }
    }
}
