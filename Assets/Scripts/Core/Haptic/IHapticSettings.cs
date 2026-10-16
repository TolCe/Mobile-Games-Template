namespace Template.Core.Haptic
{
    /// <summary>Player-facing haptic options, persisted between sessions. Bind settings-menu toggles and sliders to this.</summary>
    public interface IHapticSettings
    {
        bool Enabled { get; set; }

        /// <summary>Global strength multiplier, 0-1.</summary>
        float Intensity { get; set; }

        /// <summary>False on devices without a vibrator. Use it to hide the haptics option in menus.</summary>
        bool IsSupported { get; }
    }
}
