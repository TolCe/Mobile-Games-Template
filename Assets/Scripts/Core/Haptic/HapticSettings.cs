using RichHaptic;
using RichHaptic.Data;
using UnityEngine;
using VContainer.Unity;

namespace Template.Core.Haptic
{
    /// <summary>Persists the player's haptic options and applies them to RichHaptic on startup.</summary>
    public sealed class HapticSettings : IHapticSettings, IInitializable
    {
        private const string EnabledKey = "Haptic.Enabled";
        private const string IntensityKey = "Haptic.Intensity";

        private readonly IHapticService service;

        public HapticSettings(IHapticService service)
        {
            this.service = service;
        }

        public bool Enabled
        {
            get => service.HapticsEnabled;
            set
            {
                if (service.HapticsEnabled == value) return;
                service.HapticsEnabled = value;
                PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public float Intensity
        {
            get => service.OutputLevel;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(service.OutputLevel, clamped)) return;
                service.OutputLevel = clamped;
                PlayerPrefs.SetFloat(IntensityKey, clamped);
                PlayerPrefs.Save();
            }
        }

        public bool IsSupported => service.SupportLevel != SupportLevel.None;

        void IInitializable.Initialize()
        {
            service.HapticsEnabled = PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            service.OutputLevel = PlayerPrefs.GetFloat(IntensityKey, 1f);
        }
    }
}
