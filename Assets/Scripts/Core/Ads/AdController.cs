using System;
using UnityEngine;
using VContainer.Unity;

namespace Template.Core.Ads
{
    /// <summary>Applies ad policy (SDK readiness, "remove ads", interstitial cooldown, one fullscreen ad at a time) on top of the format units.</summary>
    public sealed class AdController : IAdController, IInitializable, IDisposable
    {
        private readonly AdConfig config;
        private readonly IAdsInitializer initializer;
        private readonly IAdSettings settings;
        private readonly IBannerAd banner;
        private readonly IInterstitialAd interstitial;
        private readonly IRewardedAd rewarded;
        private readonly Action interstitialClosedHandler;
        private readonly Action<bool> rewardedClosedHandler;

        private bool bannerRequested;
        private bool isShowingFullscreen;
        private float nextInterstitialTime;
        private Action interstitialFinished;
        private Action<bool> rewardedFinished;

        public AdController(
            AdConfig config,
            IAdsInitializer initializer,
            IAdSettings settings,
            IBannerAd banner,
            IInterstitialAd interstitial,
            IRewardedAd rewarded)
        {
            this.config = config;
            this.initializer = initializer;
            this.settings = settings;
            this.banner = banner;
            this.interstitial = interstitial;
            this.rewarded = rewarded;
            interstitialClosedHandler = OnInterstitialClosed;
            rewardedClosedHandler = OnRewardedClosed;
        }

        public event Action FullscreenAdOpened;
        public event Action FullscreenAdClosed;
        public event Action<bool> RewardedReadyChanged;

        public bool IsInterstitialReady =>
            !settings.AdsRemoved
            && !isShowingFullscreen
            && Time.realtimeSinceStartup >= nextInterstitialTime
            && interstitial.IsReady;

        public bool IsRewardedReady => !isShowingFullscreen && rewarded.IsReady;

        private bool CanShowBanner => initializer.IsInitialized && !settings.AdsRemoved;

        void IInitializable.Initialize()
        {
            settings.AdsRemovedChanged += OnAdsRemovedChanged;
            rewarded.ReadyChanged += OnRewardedReadyChanged;

            if (initializer.IsInitialized)
            {
                OnSdkInitialized();
            }
            else
            {
                initializer.Initialized += OnSdkInitialized;
            }
        }

        public void Dispose()
        {
            settings.AdsRemovedChanged -= OnAdsRemovedChanged;
            rewarded.ReadyChanged -= OnRewardedReadyChanged;
            initializer.Initialized -= OnSdkInitialized;
        }

        public void ShowBanner()
        {
            bannerRequested = true;
            if (CanShowBanner) banner.Show();
        }

        public void HideBanner()
        {
            bannerRequested = false;
            banner.Hide();
        }

        public bool ShowInterstitial(Action onFinished = null)
        {
            if (!IsInterstitialReady || !interstitial.Show(interstitialClosedHandler))
            {
                onFinished?.Invoke();
                return false;
            }

            interstitialFinished = onFinished;
            BeginFullscreen();
            return true;
        }

        public bool ShowRewarded(Action<bool> onFinished)
        {
            if (!IsRewardedReady || !rewarded.Show(rewardedClosedHandler))
            {
                onFinished?.Invoke(false);
                return false;
            }

            rewardedFinished = onFinished;
            BeginFullscreen();
            return true;
        }

        private void OnSdkInitialized()
        {
            initializer.Initialized -= OnSdkInitialized;
            rewarded.Load();
            if (settings.AdsRemoved) return;

            interstitial.Load();
            if (bannerRequested) banner.Show();
        }

        private void OnAdsRemovedChanged(bool removed)
        {
            if (removed)
            {
                banner.Destroy();
                return;
            }

            if (!initializer.IsInitialized) return;
            interstitial.Load();
            if (bannerRequested) banner.Show();
        }

        private void OnRewardedReadyChanged(bool ready)
        {
            RewardedReadyChanged?.Invoke(ready);
        }

        private void OnInterstitialClosed()
        {
            EndFullscreen();
            var callback = interstitialFinished;
            interstitialFinished = null;
            callback?.Invoke();
        }

        private void OnRewardedClosed(bool earned)
        {
            EndFullscreen();
            var callback = rewardedFinished;
            rewardedFinished = null;
            callback?.Invoke(earned);
        }

        private void BeginFullscreen()
        {
            isShowingFullscreen = true;
            FullscreenAdOpened?.Invoke();
        }

        private void EndFullscreen()
        {
            isShowingFullscreen = false;
            nextInterstitialTime = Time.realtimeSinceStartup + config.InterstitialCooldown;
            FullscreenAdClosed?.Invoke();
        }
    }
}
