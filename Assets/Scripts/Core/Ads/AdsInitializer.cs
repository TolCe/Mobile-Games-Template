using System;
using System.Collections.Generic;
using System.Threading;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using VContainer.Unity;

namespace Template.Core.Ads
{
    /// <summary>Gathers privacy consent through Google's UMP, then initializes the Mobile Ads SDK.</summary>
    public sealed class AdsInitializer : IAdsInitializer, IStartable
    {
        private readonly AdConfig config;
        private SynchronizationContext mainThread;
        private bool initializeRequested;

        public AdsInitializer(AdConfig config)
        {
            this.config = config;
        }

        public bool IsInitialized { get; private set; }

        public event Action Initialized;

        void IStartable.Start()
        {
            if (!config.AdsEnabled)
            {
                Debug.Log("[Ads] Ads are disabled in AdConfig.");
                return;
            }

            // Ad events are queued to the main thread by the SDK; UMP and init callbacks are marshalled here.
            mainThread = SynchronizationContext.Current;
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TestDeviceIds = new List<string>(config.TestDeviceIds)
            });

            if (config.RequestConsent)
            {
                GatherConsent();
            }
            else
            {
                InitializeSdk();
            }
        }

        private void GatherConsent()
        {
            var parameters = new ConsentRequestParameters { TagForUnderAgeOfConsent = config.TagForUnderAgeOfConsent };
            if (Debug.isDebugBuild)
            {
                parameters.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = config.DebugGeography,
                    TestDeviceHashedIds = new List<string>(config.TestDeviceIds)
                };
            }

            ConsentInformation.Update(parameters, error => Post(() => OnConsentInfoUpdated(error)));

            // Consent given in a previous session is still valid, so start loading ads while the update runs.
            if (ConsentInformation.CanRequestAds())
            {
                InitializeSdk();
            }
        }

        private void OnConsentInfoUpdated(FormError error)
        {
            if (error != null)
            {
                Debug.LogWarning($"[Ads] Consent info update failed: {error.Message}");
                InitializeIfAllowed();
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError => Post(() =>
            {
                if (formError != null) Debug.LogWarning($"[Ads] Consent form failed: {formError.Message}");
                InitializeIfAllowed();
            }));
        }

        private void InitializeIfAllowed()
        {
            if (ConsentInformation.CanRequestAds())
            {
                InitializeSdk();
            }
            else
            {
                Debug.Log("[Ads] Consent does not allow ad requests. Ads stay off this session.");
            }
        }

        private void InitializeSdk()
        {
            if (initializeRequested) return;
            initializeRequested = true;
            MobileAds.Initialize(status => Post(OnSdkInitialized));
        }

        private void OnSdkInitialized()
        {
            IsInitialized = true;
            Initialized?.Invoke();
        }

        private void Post(Action action)
        {
            mainThread.Post(static state => ((Action)state).Invoke(), action);
        }
    }
}
