using System;
using _Project.Composition.Startup;
using _Project.Core.Configs;
using _Project.Features.Boxes;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.UI.Hud;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Composition
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameConfig _gameConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterConfigs(builder);
            RegisterGameplay(builder);
            RegisterUI(builder);
            builder.RegisterEntryPoint<SampleStateSeeder>();
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            if (_gameConfig == null)
            {
                throw new InvalidOperationException(nameof(_gameConfig));
            }

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
            builder.Register<IncomeService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<EconomyTicker>();
        }

        private void RegisterUI(IContainerBuilder builder)
        {
            builder.Register<CoinsFormatter>(Lifetime.Singleton);
        }
    }
}
