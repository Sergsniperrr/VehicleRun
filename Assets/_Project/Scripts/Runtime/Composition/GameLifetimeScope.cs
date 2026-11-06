using System;
using _Project.Composition.Startup;
using _Project.Core.Configs;
using _Project.Features.Boxes;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Save;
using _Project.Features.Session;
using _Project.Features.Track;
using _Project.UI.Windows;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private RecycleConfirmWindow _recycleConfirmWindow;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterConfigs(builder);
            RegisterGameplay(builder);
            RegisterSave(builder);
            RegisterUI(builder);
            RegisterStartup(builder);
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            if (_gameConfig == null)
            {
                throw new InvalidOperationException(nameof(_gameConfig));
            }

            builder.RegisterInstance(_gameConfig);
            builder.RegisterInstance(_gameConfig.Vehicles);
            builder.RegisterInstance(_gameConfig.Economy);
            builder.RegisterInstance(_gameConfig.Garage);
            builder.RegisterInstance(_gameConfig.Track);
        }

        private void RegisterGameplay(IContainerBuilder builder)
        {
            builder.Register<InstanceIdGenerator>(Lifetime.Singleton);
            builder.Register<GarageBoard>(Lifetime.Singleton);
            builder.Register<RankProgress>(Lifetime.Singleton);
            builder.Register<Wallet>(Lifetime.Singleton);
            builder.Register<BoxPricing>(Lifetime.Singleton);
            builder.Register<BoxDelivery>(Lifetime.Singleton);
            builder.Register<TrackPath>(Lifetime.Singleton);
            builder.Register<TrackRoster>(Lifetime.Singleton);
            builder.Register<GarageGestures>(Lifetime.Singleton);
            builder.Register<IncomeService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<EconomyTicker>();
        }

        private void RegisterSave(IContainerBuilder builder)
        {
            builder.Register<GameSaveFactory>(Lifetime.Singleton);
            builder.Register<GameSaveValidator>(Lifetime.Singleton);
            builder.Register<GameStateRestorer>(Lifetime.Singleton);
            builder.Register<ProgressLoader>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SaveScheduler>().AsSelf();
        }

        private void RegisterStartup(IContainerBuilder builder)
        {
            builder.Register<NewGameSetup>(Lifetime.Singleton);
            builder.Register<GameSession>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameStartup>();
        }

        private void RegisterUI(IContainerBuilder builder)
        {
            if (_recycleConfirmWindow == null)
            {
                throw new InvalidOperationException(nameof(_recycleConfirmWindow));
            }

            builder.Register<CoinsFormatter>(Lifetime.Singleton);
            builder.RegisterInstance(_recycleConfirmWindow).As<IRecycleConfirmation>();
        }
    }
}
