using Template.Core.Ads;
using Template.Core.Haptic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Template.Core
{
    /// <summary>Root scope for the whole app. Lives across scene loads and is the parent of every scene's LifetimeScope. Register app-wide services here.</summary>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private AdConfig adConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            new HapticInstaller().Install(builder);
            new AdInstaller(adConfig).Install(builder);
        }
    }
}
