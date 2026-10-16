using System;

namespace Template.Core.Ads
{
    public interface IInterstitialAd
    {
        bool IsReady { get; }

        /// <summary>Starts loading unless an ad is already loaded or loading. A new ad loads automatically after each show.</summary>
        void Load();

        /// <summary>Shows the loaded ad and calls <paramref name="onClosed"/> when it closes or fails to show. Returns false and does nothing if no ad is ready.</summary>
        bool Show(Action onClosed);
    }
}
