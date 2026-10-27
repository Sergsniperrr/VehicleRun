using _Project.Core.Advertising;
using _Project.Core.Analytics;
using _Project.Core.Audio;
using _Project.Core.Localization;
using _Project.Core.Platform;
using _Project.Core.Purchasing;
using _Project.Core.Storage;
using _Project.Composition.Startup;
using _Project.Core.SceneManagement;
using _Project.Core.StateMachine;
using _Project.Core.Time;
using _Project.Infrastructure.Audio;
using _Project.Infrastructure.JTL;
using _Project.Infrastructure.SceneManagement;
using _Project.Infrastructure.StateMachine;
using _Project.Infrastructure.Time;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] private LocalizationConfig _localizationConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterConfigs(builder);
            RegisterServices(builder);
            RegisterPlatform(builder);
            RegisterStorage(builder);
            RegisterStateMachine(builder);
            RegisterStates(builder);
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            builder.RegisterInstance(_localizationConfig);
        }

        private void RegisterStorage(IContainerBuilder builder)
        {
            builder.Register<ExampleStorage>(Lifetime.Singleton).As<IExampleStorage>();
        }

        private void RegisterPlatform(IContainerBuilder builder)
        {
            builder.Register<JTLSDKInitializer>(Lifetime.Singleton).As<IPlatformInitializer>();
            builder.Register<JTLAnalyticsService>(Lifetime.Singleton).As<IAnalyticsService>();
            builder.Register<JTLLocalizationService>(Lifetime.Singleton).As<ILocalizationService>();
            builder.RegisterEntryPoint<JTLPurchaseService>().As<IPurchaseService>();
            builder.RegisterEntryPoint<JTLAdvertisingService>().As<IAdvertisingService>();
        }

        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();
            builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
            builder.Register<TimeService>(Lifetime.Singleton).As<ITimeService>();
        }

        private void RegisterStateMachine(IContainerBuilder builder)
        {
            builder.Register<StateFactory>(Lifetime.Singleton).As<IStateFactory>();
            builder.Register<GameStateMachine>(Lifetime.Singleton).As<IGameStateMachine>();
        }

        private void RegisterStates(IContainerBuilder builder)
        {
            builder.Register<BootstrapState>(Lifetime.Transient);
            builder.Register<LoadRunState>(Lifetime.Transient);
            builder.Register<RunState>(Lifetime.Transient);
        }
    }
}
