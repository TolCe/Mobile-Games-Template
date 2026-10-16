using VContainer;
using VContainer.Unity;

namespace Template.Core.Haptic
{
    public sealed class HapticInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(RichHaptic.Haptics.Service);
            builder.RegisterEntryPoint<HapticSettings>().As<IHapticSettings>();
            builder.Register<IHapticPlayer, HapticPlayer>(Lifetime.Singleton);
        }
    }
}
