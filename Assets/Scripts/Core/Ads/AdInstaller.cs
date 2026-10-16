using System;
using VContainer;
using VContainer.Unity;

namespace Template.Core.Ads
{
    public sealed class AdInstaller : IInstaller
    {
        private readonly AdConfig config;

        public AdInstaller(AdConfig config)
        {
            this.config = config;
        }

        public void Install(IContainerBuilder builder)
        {
            if (config == null)
            {
                throw new InvalidOperationException("No AdConfig is assigned on the ProjectLifetimeScope prefab.");
            }

            builder.RegisterInstance(config);
            builder.Register<IAdSettings, AdSettings>(Lifetime.Singleton);
            builder.RegisterEntryPoint<AdsInitializer>().As<IAdsInitializer>();
            builder.Register<IBannerAd, BannerAdUnit>(Lifetime.Singleton);
            builder.Register<IInterstitialAd, InterstitialAdUnit>(Lifetime.Singleton);
            builder.Register<IRewardedAd, RewardedAdUnit>(Lifetime.Singleton);
            builder.RegisterEntryPoint<AdController>().As<IAdController>();
        }
    }
}
