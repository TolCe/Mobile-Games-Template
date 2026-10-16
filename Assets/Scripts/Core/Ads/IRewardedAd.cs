using System;

namespace Template.Core.Ads
{
    public interface IRewardedAd
    {
        bool IsReady { get; }

        /// <summary>Raised when an ad finishes loading (true) or is used up (false).</summary>
        event Action<bool> ReadyChanged;

        /// <summary>Starts loading unless an ad is already loaded or loading. A new ad loads automatically after each show.</summary>
        void Load();

        /// <summary>
        /// Shows the loaded ad and calls <paramref name="onFinished"/> when it closes or fails to show, with true if
        /// the reward was earned. Returns false and does nothing if no ad is ready.
        /// </summary>
        bool Show(Action<bool> onFinished);
    }
}
