using System;
using GoogleMobileAds.Api;
using PrimeTween;
using UnityEngine;

namespace Template.Core.Ads
{
    /// <summary>Keeps one interstitial preloaded and reloads it after every show.</summary>
    public sealed class InterstitialAdUnit : IInterstitialAd, IDisposable
    {
        private readonly AdConfig config;
        private InterstitialAd ad;
        private bool isLoading;
        private bool disposed;
        private int failedAttempts;
        private Tween retry;
        private Action onClosed;

        public InterstitialAdUnit(AdConfig config)
        {
            this.config = config;
        }

        public bool IsReady => ad != null && ad.CanShowAd();

        public void Load()
        {
            if (disposed || ad != null || isLoading) return;

            var unitId = config.ActiveUnitIds.Interstitial;
            if (string.IsNullOrEmpty(unitId)) return;

            isLoading = true;
            InterstitialAd.Load(unitId, new AdRequest(), OnLoaded);
        }

        public bool Show(Action onClosed)
        {
            if (!IsReady) return false;

            this.onClosed = onClosed;
            ad.Show();
            return true;
        }

        public void Dispose()
        {
            disposed = true;
            retry.Stop();
            ReleaseAd();
        }

        private void OnLoaded(InterstitialAd loaded, LoadAdError error)
        {
            isLoading = false;
            if (disposed)
            {
                loaded?.Destroy();
                return;
            }

            if (error != null || loaded == null)
            {
                Debug.LogWarning($"[Ads] Interstitial failed to load: {error?.GetMessage()}");
                failedAttempts++;
                retry = Tween.Delay(this, config.GetRetryDelay(failedAttempts), static unit => unit.Load(), useUnscaledTime: true);
                return;
            }

            failedAttempts = 0;
            ad = loaded;
            ad.OnAdFullScreenContentClosed += OnClosed;
            ad.OnAdFullScreenContentFailed += OnShowFailed;
        }

        private void OnClosed()
        {
            Finish();
        }

        private void OnShowFailed(AdError error)
        {
            Debug.LogWarning($"[Ads] Interstitial failed to show: {error.GetMessage()}");
            Finish();
        }

        private void Finish()
        {
            ReleaseAd();
            Load();

            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private void ReleaseAd()
        {
            if (ad == null) return;

            ad.OnAdFullScreenContentClosed -= OnClosed;
            ad.OnAdFullScreenContentFailed -= OnShowFailed;
            ad.Destroy();
            ad = null;
        }
    }
}
