using System;

namespace Template.Core.Ads
{
    /// <summary>Player-facing ad options. Bind the "remove ads" purchase and the settings menu's privacy button to this.</summary>
    public interface IAdSettings
    {
        /// <summary>Set to true after a "remove ads" purchase. Hides the banner and blocks interstitials; rewarded ads stay available. Persisted between sessions.</summary>
        bool AdsRemoved { get; set; }

        event Action<bool> AdsRemovedChanged;

        /// <summary>True when the player's region requires a way to change consent. Show a "Privacy settings" button only when this is true.</summary>
        bool IsPrivacyOptionsRequired { get; }

        /// <summary>Opens Google's privacy options form. <paramref name="onClosed"/> is called on the main thread when it closes.</summary>
        void ShowPrivacyOptions(Action onClosed = null);
    }
}
