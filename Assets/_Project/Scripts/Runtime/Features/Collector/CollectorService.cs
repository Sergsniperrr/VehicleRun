using System;
using _Project.Core.Collector;
using _Project.Core.Randomness;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Vehicles;

namespace _Project.Features.Collector
{
    public class CollectorService
    {
        private readonly CollectorConfig _config;
        private readonly TrackConfig _track;
        private readonly GarageBoard _garage;
        private readonly Wallet _wallet;
        private readonly IRandomSource _random;

        private float _spawnRemainingSeconds;

        public CollectorService(
            CollectorConfig config,
            TrackConfig track,
            GarageBoard garage,
            Wallet wallet,
            IRandomSource random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _spawnRemainingSeconds = RollSpawnInterval();
        }

        public event Action Appeared;
        public event Action Disappeared;
        public event Action<long> ChargeCollected;

        public bool IsActive { get; private set; }
        public float Distance { get; private set; }
        public float RemainingSeconds { get; private set; }
        public int RemainingCharges { get; private set; }
        public long ChargeReward { get; private set; }

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (IsActive)
            {
                Drive(deltaTime);
            }
            else
            {
                CountDownSpawn(deltaTime);
            }
        }

        public CommandResult Collect()
        {
            if (IsActive == false)
            {
                return new CommandResult(FailureReason.CollectorInactive);
            }

            RemainingCharges--;
            _wallet.Add(ChargeReward);
            ChargeCollected?.Invoke(ChargeReward);

            if (RemainingCharges == 0)
            {
                Disappear();
            }

            return new CommandResult(FailureReason.None);
        }

        public bool SpawnNow()
        {
            if (TryFindChargeReward(out long reward) == false)
            {
                return false;
            }

            if (IsActive)
            {
                Disappear();
            }

            Appear(reward);

            return true;
        }

        private void Drive(float deltaTime)
        {
            Distance = (Distance + _config.TrackSpeed * deltaTime) % _track.LapLength;
            RemainingSeconds -= deltaTime;

            if (RemainingSeconds <= 0f)
            {
                Disappear();
            }
        }

        private void CountDownSpawn(float deltaTime)
        {
            _spawnRemainingSeconds = Math.Max(0f, _spawnRemainingSeconds - deltaTime);

            if (_spawnRemainingSeconds <= 0f && TryFindChargeReward(out long reward))
            {
                Appear(reward);
            }
        }

        private void Appear(long reward)
        {
            IsActive = true;
            Distance = 0f;
            RemainingSeconds = _config.LifetimeSeconds;
            RemainingCharges = _random.Range(_config.MinCharges, _config.MaxCharges);
            ChargeReward = reward;
            Appeared?.Invoke();
        }

        private void Disappear()
        {
            IsActive = false;
            RemainingSeconds = 0f;
            RemainingCharges = 0;
            _spawnRemainingSeconds = RollSpawnInterval();
            Disappeared?.Invoke();
        }

        private float RollSpawnInterval()
        {
            return _random.Range(_config.SpawnIntervalMinSeconds, _config.SpawnIntervalMaxSeconds);
        }

        private bool TryFindChargeReward(out long reward)
        {
            VehicleDefinition best = null;

            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetVehicle(slot, out Vehicle vehicle) && (best == null || vehicle.Rank > best.Rank))
                {
                    best = vehicle.Definition;
                }
            }

            reward = best == null ? 0 : (long)Math.Round(best.CoinsPerLap * (double)_config.ChargeRewardMultiplier);

            return best != null;
        }
    }
}
