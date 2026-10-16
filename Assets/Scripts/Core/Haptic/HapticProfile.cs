using RichHaptic.Data;
using UnityEngine;

namespace Template.Core.Haptic
{
    /// <summary>Designer-authored description of one haptic feedback. Reference it from components and play it through <see cref="IHapticPlayer"/>.</summary>
    [CreateAssetMenu(fileName = "New Haptic Profile", menuName = "Template/Haptic Profile")]
    public sealed class HapticProfile : ScriptableObject
    {
        [Tooltip("Played when no clip is assigned, or as a stand-in on devices that cannot render clips.")]
        [SerializeField] private PresetType preset = PresetType.LightImpact;

        [Tooltip("Optional authored clip. When assigned it is played instead of the preset.")]
        [SerializeField] private HapticClip clip;

        [Tooltip("0 is the highest. A haptic will not interrupt a higher-priority one that is still playing.")]
        [Range(0, 256)]
        [SerializeField] private int priority = HapticPriority.Default;

        [Tooltip("Minimum seconds between two plays of this profile. Stops repeated triggers from turning into a constant buzz.")]
        [Min(0f)]
        [SerializeField] private float cooldown = 0.05f;

        public PresetType Preset => preset;
        public HapticClip Clip => clip;
        public int Priority => priority;
        public float Cooldown => cooldown;
    }
}
