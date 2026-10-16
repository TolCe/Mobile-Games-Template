using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Template.Core.Ads
{
    /// <summary>Ad unit IDs and ad behaviour. Assigned on the ProjectLifetimeScope prefab.</summary>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Template/Ad Config")]
    public sealed class AdConfig : ScriptableObject
    {
        [Tooltip("Master switch. When off, consent and the SDK are skipped and no ads load; every Show call returns false and fires its callback immediately.")]
        [SerializeField] private bool adsEnabled = true;

        [Header("Ad Units")]
        [Tooltip("Defaults are Google's test units. Replace them before release; Tools > Checklist warns while test IDs are set.")]
        [SerializeField] private AdUnitIds android = new AdUnitIds(
            "ca-app-pub-3940256099942544/9214589741",
            "ca-app-pub-3940256099942544/1033173712",
            "ca-app-pub-3940256099942544/5224354917");

        [Tooltip("Defaults are Google's test units. Replace them before release; Tools > Checklist warns while test IDs are set.")]
        [SerializeField] private AdUnitIds ios = new AdUnitIds(
            "ca-app-pub-3940256099942544/2435281174",
            "ca-app-pub-3940256099942544/4411468910",
            "ca-app-pub-3940256099942544/1712485313");

        [Header("Behaviour")]
        [SerializeField] private AdPosition bannerPosition = AdPosition.Bottom;

        [Tooltip("Seconds after any fullscreen ad closes before an interstitial may show again.")]
        [Min(0f)]
        [SerializeField] private float interstitialCooldown = 30f;

        [Tooltip("Upper limit in seconds for the exponential back-off between failed load attempts.")]
        [Min(1f)]
        [SerializeField] private float maxRetryDelay = 64f;

        [Header("Privacy")]
        [Tooltip("Ask for consent through Google's UMP before initializing ads. Required for players in the EEA, UK and regulated US states.")]
        [SerializeField] private bool requestConsent = true;

        [Tooltip("Mark consent requests as coming from users under the age of consent.")]
        [SerializeField] private bool tagForUnderAgeOfConsent;

        [Header("Testing")]
        [Tooltip("Hashed device IDs, as printed in the device log on the first ad request. These devices always get test ads.")]
        [SerializeField] private string[] testDeviceIds = Array.Empty<string>();

        [Tooltip("Pretend test devices are in this region to try the consent form. Development builds only.")]
        [SerializeField] private DebugGeography debugGeography = DebugGeography.Disabled;

        public bool AdsEnabled => adsEnabled;
        public AdUnitIds Android => android;
        public AdUnitIds IOS => ios;

        /// <summary>Unit IDs for the platform this build targets.</summary>
        public AdUnitIds ActiveUnitIds
        {
#if UNITY_IOS
            get => ios;
#else
            get => android;
#endif
        }

        public AdPosition BannerPosition => bannerPosition;
        public float InterstitialCooldown => interstitialCooldown;
        public bool RequestConsent => requestConsent;
        public bool TagForUnderAgeOfConsent => tagForUnderAgeOfConsent;
        public IReadOnlyList<string> TestDeviceIds => testDeviceIds;
        public DebugGeography DebugGeography => debugGeography;

        /// <summary>Seconds to wait before the next load after <paramref name="failedAttempts"/> failures in a row: 2, 4, 8, … up to the max.</summary>
        public float GetRetryDelay(int failedAttempts)
        {
            return Mathf.Min(Mathf.Pow(2f, failedAttempts), maxRetryDelay);
        }
    }
}
