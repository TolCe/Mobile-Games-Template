using RichHaptic.Data;

namespace Template.Core.Haptic
{
    /// <summary>Plays haptic feedback. Inject this instead of calling RichHaptic's static <c>Haptics</c> API.</summary>
    public interface IHapticPlayer
    {
        void Play(HapticProfile profile);
        void Play(PresetType preset, int priority = HapticPriority.Default);
        void StartContinuous(float intensity, float sharpness, int priority = HapticPriority.Default);
        void UpdateContinuous(float intensity, float sharpness);
        void StopContinuous();
        void StopAll();
    }
}
