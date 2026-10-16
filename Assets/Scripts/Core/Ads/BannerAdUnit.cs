using System;
using GoogleMobileAds.Api;
using PrimeTween;
using UnityEngine;

namespace Template.Core.Ads
{
    /// <summary>One adaptive anchored banner, retried with back-off when a load fails.</summary>
    public sealed class BannerAdUnit : IBannerAd, IDisposable
    {
        private readonly AdConfig config;
        private BannerView view;
        private bool visible;
        private int failedAttempts;
        private Tween retry;

        public BannerAdUnit(AdConfig config)
        {
            this.config = config;
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

        private void OnLoaded()
        {
            failedAttempts = 0;

            // A banner makes itself visible on every load, including auto-refresh.
            if (!visible) view.Hide();
        }

        private void OnLoadFailed(LoadAdError error)
        {
            Debug.LogWarning($"[Ads] Banner failed to load: {error.GetMessage()}");
            failedAttempts++;
            retry = Tween.Delay(this, config.GetRetryDelay(failedAttempts), static unit => unit.Load(), useUnscaledTime: true);
        }
    }
}
