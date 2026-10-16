using System;
using System.Threading;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Template.Core.Ads
{
    public sealed class AdSettings : IAdSettings
    {
        private const string AdsRemovedKey = "Ads.Removed";

        // UMP may call back on a background thread.
        private readonly SynchronizationContext mainThread = SynchronizationContext.Current;
        private bool adsRemoved = PlayerPrefs.GetInt(AdsRemovedKey, 0) == 1;

        public event Action<bool> AdsRemovedChanged;

        public bool AdsRemoved
        {
            get => adsRemoved;
            set
            {
                if (adsRemoved == value) return;
                adsRemoved = value;
                PlayerPrefs.SetInt(AdsRemovedKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AdsRemovedChanged?.Invoke(value);
            }
        }

        public bool IsPrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void ShowPrivacyOptions(Action onClosed = null)
        {
            ConsentForm.ShowPrivacyOptionsForm(error => mainThread.Post(_ =>
            {
                if (error != null) Debug.LogWarning($"[Ads] Privacy options form failed: {error.Message}");
                onClosed?.Invoke();
            }, null));
        }
    }
}
