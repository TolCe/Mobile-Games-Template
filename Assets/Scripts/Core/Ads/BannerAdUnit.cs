using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using PrimeTween;
using UnityEngine;

namespace Template.Core.Ads
{
    /// <summary>One adaptive anchored banner, retried with back-off when a load fails.</summary>
    public sealed class BannerAdUnit : IBannerAd, IDisposable
    {
        private readonly AdConfig config;
        private readonly Action handleLoaded;
        private BannerView view;
        private bool visible;
        private int failedAttempts;
        private Tween retry;

        public BannerAdUnit(AdConfig config)
        {
            this.config = config;
            handleLoaded = HandleLoaded;
        }

        public void Show()
        {
            visible = true;
            if (view != null)
            {
                view.Show();
                return;
            }

            var unitId = config.ActiveUnitIds.Banner;
            if (string.IsNullOrEmpty(unitId)) return;

            var size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            view = new BannerView(unitId, size, config.BannerPosition);
            view.OnBannerAdLoaded += OnLoaded;
            view.OnBannerAdLoadFailed += OnLoadFailed;
            Load();
        }

        public void Hide()
        {
            visible = false;
            view?.Hide();
        }

        public void Destroy()
        {
            retry.Stop();
            failedAttempts = 0;
            if (view == null) return;

            view.OnBannerAdLoaded -= OnLoaded;
            view.OnBannerAdLoadFailed -= OnLoadFailed;
            view.Destroy();
            view = null;
        }

        public void Dispose()
        {
            Destroy();
        }

        private void Load()
        {
            view?.LoadAd(new AdRequest());
        }

        // SDK events can arrive on a background thread.
        private void OnLoaded()
        {
            MobileAdsEventExecutor.ExecuteInUpdate(handleLoaded);
        }

        private void OnLoadFailed(LoadAdError error)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() => HandleLoadFailed(error));
        }

        private void HandleLoaded()
        {
            failedAttempts = 0;

            // A banner makes itself visible on every load, including auto-refresh.
            if (!visible) view?.Hide();
        }

        private void HandleLoadFailed(LoadAdError error)
        {
            if (view == null) return;

            Debug.LogWarning($"[Ads] Banner failed to load: {error.GetMessage()}");
            failedAttempts++;
            retry = Tween.Delay(this, config.GetRetryDelay(failedAttempts), static unit => unit.Load(), useUnscaledTime: true);
        }
    }
}
