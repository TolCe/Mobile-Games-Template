using System;

namespace Template.Core.Ads
{
    /// <summary>
    /// Shows ads. Inject this from gameplay and UI. It waits for the SDK and consent, honours the "remove ads"
    /// purchase and the interstitial cooldown, so callers only decide where an ad fits.
    /// </summary>
    public interface IAdController
    {
        /// <summary>Raised when an interstitial or rewarded ad takes over the screen. Pause gameplay and mute audio here; Android keeps the game running behind the ad.</summary>
        event Action FullscreenAdOpened;

        /// <summary>Raised when a fullscreen ad closes or fails to show.</summary>
        event Action FullscreenAdClosed;

        /// <summary>Raised when a rewarded ad becomes ready or stops being ready. Use it to enable or disable "watch ad" buttons.</summary>
        event Action<bool> RewardedReadyChanged;

        bool IsInterstitialReady { get; }
        bool IsRewardedReady { get; }

        /// <summary>Shows the banner as soon as the SDK is ready and keeps it shown until <see cref="HideBanner"/>. Has no effect while ads are removed.</summary>
        void ShowBanner();

        void HideBanner();

        /// <summary>
        /// Shows an interstitial if one is loaded, ads are not removed and the cooldown has passed.
        /// <paramref name="onFinished"/> is always called: when the ad closes, or immediately if nothing was shown.
        /// </summary>
        /// <returns>True if an ad was shown.</returns>
        bool ShowInterstitial(Action onFinished = null);

        /// <summary>
        /// Shows a rewarded ad if one is loaded. <paramref name="onFinished"/> is always called, with true only if the
        /// player earned the reward: when the ad closes, or immediately with false if nothing was shown.
        /// </summary>
        /// <returns>True if an ad was shown.</returns>
        bool ShowRewarded(Action<bool> onFinished);
    }
}
