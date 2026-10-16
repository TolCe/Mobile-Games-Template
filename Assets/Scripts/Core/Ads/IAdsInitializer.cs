using System;

namespace Template.Core.Ads
{
    public interface IAdsInitializer
    {
        /// <summary>True once the SDK is initialized and consent allows requesting ads.</summary>
        bool IsInitialized { get; }

        /// <summary>Raised once, on the main thread, when <see cref="IsInitialized"/> becomes true. Never raised if ads are disabled in <see cref="AdConfig"/> or consent is refused.</summary>
        event Action Initialized;
    }
}
