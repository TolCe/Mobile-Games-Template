using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using PrimeTween;
using UnityEngine;

namespace Template.Core.Ads
{
    /// <summary>Keeps one rewarded ad preloaded and reloads it after every show.</summary>
    public sealed class RewardedAdUnit : IRewardedAd, IDisposable
    {
        private readonly AdConfig config;
        private readonly Action<RewardedAd, LoadAdError> onLoaded;
        private readonly Action<Reward> onUserEarnedReward;
        private readonly Action markEarned;
        private readonly Action finish;
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
            onLoaded = OnLoaded;
            onUserEarnedReward = OnUserEarnedReward;
            markEarned = MarkEarned;
            finish = Finish;
        }

        public event Action<bool> ReadyChanged;

        public bool IsReady => ad != null && ad.CanShowAd();

        public void Load()
        {
            if (disposed || ad != null || isLoading) return;

            var unitId = config.ActiveUnitIds.Rewarded;
            if (string.IsNullOrEmpty(unitId)) return;

            isLoading = true;
            RewardedAd.Load(unitId, new AdRequest(), (loaded, error) =>
                MobileAdsEventExecutor.ExecuteInUpdate(() => onLoaded(loaded, error)));
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

        // SDK events can arrive on a background thread. They share one queue, so a reward
        // is always recorded before the close that follows it is handled.
        private void OnUserEarnedReward(Reward reward)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(markEarned);
        }

        private void OnClosed()
        {
            MobileAdsEventExecutor.ExecuteInUpdate(finish);
        }

        private void OnShowFailed(AdError error)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.LogWarning($"[Ads] Rewarded ad failed to show: {error.GetMessage()}");
                Finish();
            });
        }

        private void MarkEarned()
        {
            earned = true;
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
