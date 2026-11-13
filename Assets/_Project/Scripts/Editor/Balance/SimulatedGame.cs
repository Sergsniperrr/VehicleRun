using System;
using _Project.Core.Configs;
using _Project.Core.Economy;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Session;
using _Project.Features.Track;
using _Project.Features.Turbo;

namespace _Project.Editor.Balance
{
    public class SimulatedGame : IDisposable
    {
        private const int RandomSeed = 20261113;

        private readonly EconomyConfig _economy;
        private readonly BalanceSimulationSettings _settings;

        public SimulatedGame(GameConfig config, EconomyConfig economy, BalanceSimulationSettings settings)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Catalog = config.Vehicles;
            LapLength = config.Track.LapLength;

            InstanceIdGenerator instanceIds = new InstanceIdGenerator();
            Garage = new GarageBoard(config.Garage, config.Vehicles, instanceIds);
            Progress = new RankProgress(config.Vehicles, economy, Garage);
            BoxPricing pricing = new BoxPricing(economy, config.Vehicles);
            Wallet = new Wallet(economy);
            PlayerLevel = new PlayerLevel(config.PlayerLevel, config.Track, Garage, Wallet);
            Delivery = new BoxDelivery(
                economy,
                config.Vehicles,
                Garage,
                Progress,
                Wallet,
                pricing,
                instanceIds,
                new SeededRandomSource(RandomSeed));
            Roster = new TrackRoster(Garage, PlayerLevel, config.Track, Wallet);
            Turbo = new TurboService(config.Turbo, Progress, Roster);
            Gestures = new GarageGestures(Garage, Roster);
            Income = new IncomeService(Roster, config.Track, Turbo);

            Roster.FinishCrossed += OnFinishCrossed;
            PlayerLevel.LevelReached += OnLevelReached;
            new NewGameSetup(economy, config.Vehicles, Garage, Progress, instanceIds).Apply();
        }

        public VehicleCatalogConfig Catalog { get; }
        public float LapLength { get; }
        public GarageBoard Garage { get; }
        public RankProgress Progress { get; }
        public Wallet Wallet { get; }
        public PlayerLevel PlayerLevel { get; }
        public BoxDelivery Delivery { get; }
        public TrackRoster Roster { get; }
        public TurboService Turbo { get; }
        public GarageGestures Gestures { get; }
        public IncomeService Income { get; }
        public double ActiveSeconds { get; private set; }
        public double DecorMultiplier => 1 + _settings.DecorIncomeBonus * CountDecorObjects();

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            Roster.Advance(deltaTime, Turbo);
            Turbo.Advance(deltaTime);
            Delivery.Advance(deltaTime);
            ActiveSeconds += deltaTime;
        }

        public long CollectOfflineIncome(float awaySeconds)
        {
            if (float.IsNaN(awaySeconds) || awaySeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(awaySeconds));
            }

            double countedSeconds = Math.Min(awaySeconds, _economy.OfflineMaxAwaySeconds);
            double incomePerSecond = DecorMultiplier * Income.CalculateIncomePerSecondWithoutTurbo();
            double offlineIncome = Math.Floor(countedSeconds * _economy.OfflineIncomeRate * incomePerSecond);
            Wallet.Add(offlineIncome);

            return (long)offlineIncome;
        }

        public void Dispose()
        {
            Roster.FinishCrossed -= OnFinishCrossed;
            PlayerLevel.LevelReached -= OnLevelReached;
            PlayerLevel.Dispose();
            Progress.Dispose();
        }

        private int CountDecorObjects()
        {
            return Progress.MaxRank / _settings.DecorRankStep;
        }

        private void OnLevelReached(int level)
        {
            PlayerLevel.ClaimReward();
        }

        private void OnFinishCrossed(TrackRunner runner, long payout)
        {
            double decorBonus = payout * (DecorMultiplier - 1);

            if (decorBonus > 0)
            {
                Wallet.Add(decorBonus);
            }
        }
    }
}
