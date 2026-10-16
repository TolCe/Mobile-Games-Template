using System;
using GoogleMobileAds.Api;
using PrimeTween;
using UnityEngine;

namespace Template.Core.Ads
{
    /// <summary>Keeps one rewarded ad preloaded and reloads it after every show.</summary>
    public sealed class RewardedAdUnit : IRewardedAd, IDisposable
    {
        private readonly AdConfig config;
        private readonly Action<Reward> onUserEarnedReward;
        private RewardedAd ad;
        private bool isLoading;
        private bool disposed;
        private bool earned;
        private int failedAttempts;
        private Tween retry;
        private Action<bool> onFinished;

        public RewardedAdUnit(AdConfig config)
        {
            this.config = config;
            onUserEarnedReward = OnUserEarnedReward;
        }

        public event Action<bool> ReadyChanged;

        public bool IsReady => ad != null && ad.CanShowAd();

        public void Load()
        {
            if (disposed || ad != null || isLoading) return;

            var unitId = config.ActiveUnitIds.Rewarded;
            if (string.IsNullOrEmpty(unitId)) return;

            isLoading = true;
            RewardedAd.Load(unitId, new AdRequest(), OnLoaded);
        }

        public bool Show(Action<bool> onFinished)
        {
            if (!IsReady) return false;

            this.onFinished = onFinished;
            earned = false;
            ad.Show(onUserEarnedReward);
            return true;
        }

        public void Dispose()
        {
            disposed = true;
            retry.Stop();
            ReleaseAd();
        }

        private void OnLoaded(RewardedAd loaded, LoadAdError error)
        {
            isLoading = false;
            if (disposed)
            {
                loaded?.Destroy();
                return;
            }

            if (error != null || loaded == null)
            {
                Debug.LogWarning($"[Ads] Rewarded ad failed to load: {error?.GetMessage()}");
                failedAttempts++;
                retry = Tween.Delay(this, config.GetRetryDelay(failedAttempts), static unit => unit.Load(), useUnscaledTime: true);
                return;
            }

            failedAttempts = 0;
            ad = loaded;
            ad.OnAdFullScreenContentClosed += OnClosed;
            ad.OnAdFullScreenContentFailed += OnShowFailed;
            ReadyChanged?.Invoke(true);
        }

        private void OnUserEarnedReward(Reward reward)
        {
            earned = true;
        }

        private void OnClosed()
        {
            Finish();
        }

        private void OnShowFailed(AdError error)
        {
            Debug.LogWarning($"[Ads] Rewarded ad failed to show: {error.GetMessage()}");
            Finish();
        }

        private void Finish()
        {
            ReleaseAd();
            ReadyChanged?.Invoke(false);
            Load();

            var callback = onFinished;
            onFinished = null;
            callback?.Invoke(earned);
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
