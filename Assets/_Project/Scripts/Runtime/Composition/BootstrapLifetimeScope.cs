using _Project.Composition.Startup;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class BootstrapLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            RegisterEntryPoint(builder);
        }

        private void RegisterEntryPoint(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<Bootstrapper>();
        }
    }
}
