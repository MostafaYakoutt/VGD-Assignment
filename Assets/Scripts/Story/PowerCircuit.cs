using LeftBehind.Interaction;
using UnityEngine;

namespace LeftBehind.Story
{
    /// <summary>
    /// A set of lamps that stay dead until a story flag is set (the upstairs lamps until
    /// the fuse is replaced). While the power is off the lamps are dark and their switches
    /// do nothing; when the flag is set they all come on.
    /// </summary>
    public class PowerCircuit : MonoBehaviour
    {
        [SerializeField] private string powerFlag = "powerRestored";
        [SerializeField] private LampSwitch[] lamps = new LampSwitch[0];

        public void Configure(string flag, LampSwitch[] l)
        {
            powerFlag = flag;
            lamps = l;
        }

        private void OnEnable() => StoryStateManager.FlagSet += OnFlag;
        private void OnDisable() => StoryStateManager.FlagSet -= OnFlag;

        private void Start() => SetPower(StoryStateManager.Has(powerFlag));

        private void OnFlag(string flag)
        {
            if (flag == powerFlag) SetPower(true);
        }

        private void SetPower(bool on)
        {
            foreach (var lamp in lamps)
            {
                if (lamp == null) continue;
                lamp.SetOn(on);
                lamp.enabled = on;      // a disabled switch cannot be used
            }
        }
    }
}
