using System.Collections.Generic;
using RichHaptic;
using RichHaptic.Data;
using UnityEngine;

namespace Template.Core.Haptic
{
    public sealed class HapticPlayer : IHapticPlayer
    {
        private readonly IHapticService service;
        private readonly Dictionary<HapticProfile, float> nextPlayableTimes = new Dictionary<HapticProfile, float>();

        public HapticPlayer(IHapticService service)
        {
            this.service = service;
        }

        public void Play(HapticProfile profile)
        {
            if (profile == null || !service.HapticsEnabled) return;

            var now = Time.unscaledTime;
            if (nextPlayableTimes.TryGetValue(profile, out var nextPlayableTime) && now < nextPlayableTime) return;
            nextPlayableTimes[profile] = now + profile.Cooldown;

            service.Priority = profile.Priority;
            if (profile.Clip != null)
            {
                service.FallbackPreset = profile.Preset;
                service.PlayClip(profile.Clip);
            }
            else
            {
                service.PlayPreset(profile.Preset);
            }
        }

        public void Play(PresetType preset, int priority = HapticPriority.Default)
        {
            service.Priority = priority;
            service.PlayPreset(preset);
        }

        public void StartContinuous(float intensity, float sharpness, int priority = HapticPriority.Default)
        {
            service.Priority = priority;
            service.StartContinuous(intensity, sharpness);
        }

        public void UpdateContinuous(float intensity, float sharpness)
        {
            service.UpdateContinuous(intensity, sharpness);
        }

        public void StopContinuous()
        {
            service.StopContinuous();
        }

        public void StopAll()
        {
            service.Stop();
        }
    }
}
