using System;
using _Project.Core.Configs;
using _Project.Core.Economy;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.Features.Vehicles;

namespace _Project.Editor.Balance
{
    public class SimulatedGame : IDisposable
    {
        private readonly EconomyConfig _economy;
        private readonly BalanceSimulationSettings _settings;
        private float _turboRemainingSeconds;
        private float _turboCooldownRemainingSeconds;

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
            Progress = new RankProgress(config.Vehicles, economy, config.Track, Garage);
            BoxPricing pricing = new BoxPricing(economy, config.Vehicles, config.Track, Progress);
            Wallet = new Wallet(economy);
            Delivery = new BoxDelivery(economy, config.Vehicles, Garage, Progress, Wallet, pricing, instanceIds);
            Roster = new TrackRoster(Garage, Progress, config.Track, Wallet);
            Gestures = new GarageGestures(Garage, Roster);
            Income = new IncomeService(Roster, config.Track);

            Roster.FinishCrossed += OnFinishCrossed;
            PlaceStartVehicles(instanceIds);
        }

        public VehicleCatalogConfig Catalog { get; }
        public float LapLength { get; }
        public GarageBoard Garage { get; }
        public RankProgress Progress { get; }
        public Wallet Wallet { get; }
        public BoxDelivery Delivery { get; }
        public TrackRoster Roster { get; }
        public GarageGestures Gestures { get; }
        public IncomeService Income { get; }
        public double ActiveSeconds { get; private set; }
        public bool IsTurboActive => _turboRemainingSeconds > 0;
        public double DecorMultiplier => 1 + _settings.DecorIncomeBonus * CountDecorObjects();

        public bool CanActivateTurbo =>
            Progress.MaxRank >= _settings.TurboUnlockRank
            && IsTurboActive == false
            && _turboCooldownRemainingSeconds <= 0
            && Roster.Count > 0;

        public void ActivateTurbo()
        {
            if (CanActivateTurbo == false)
            {
                throw new InvalidOperationException(nameof(CanActivateTurbo));
            }

            _turboRemainingSeconds = _settings.TurboDurationSeconds;
        }

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            float speedMultiplier = IsTurboActive ? _settings.TurboSpeedMultiplier : 1f;
            Roster.Advance(deltaTime * speedMultiplier);
            Delivery.Advance(deltaTime);
            AdvanceTurbo(deltaTime);
            ActiveSeconds += deltaTime;
        }

        public long CollectOfflineIncome(float awaySeconds)
        {
            if (float.IsNaN(awaySeconds) || awaySeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(awaySeconds));
            }

            double countedSeconds = Math.Min(awaySeconds, _economy.OfflineMaxAwaySeconds);
            double incomePerSecond = DecorMultiplier * Income.CalculateIncomePerSecond();
            double offlineIncome = Math.Floor(countedSeconds * _economy.OfflineIncomeRate * incomePerSecond);
            Wallet.Add(offlineIncome);

            return (long)offlineIncome;
        }

        public void Dispose()
        {
            Roster.FinishCrossed -= OnFinishCrossed;
            Progress.Dispose();
        }

        private void PlaceStartVehicles(InstanceIdGenerator instanceIds)
        {
            VehicleDefinition definition = Catalog.GetByRank(_economy.StartVehicleRank);

            for (int slot = 0; slot < _economy.StartVehicleCount; slot++)
            {
                Garage.PlaceVehicle(slot, new Vehicle(instanceIds.Create(), definition));
            }
        }

        private void AdvanceTurbo(float deltaTime)
        {
            if (IsTurboActive)
            {
                _turboRemainingSeconds -= deltaTime;

                if (IsTurboActive == false)
                {
                    _turboRemainingSeconds = 0;
                    _turboCooldownRemainingSeconds = _settings.TurboCooldownSeconds;
                }

                return;
            }

            _turboCooldownRemainingSeconds = Math.Max(0, _turboCooldownRemainingSeconds - deltaTime);
        }

        private int CountDecorObjects()
        {
            return Progress.MaxRank / _settings.DecorRankStep;
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
