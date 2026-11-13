using System;
using _Project.Core.Progression;
using _Project.Core.Track;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Vehicles;

namespace _Project.Features.Progression
{
    public class PlayerLevel : IDisposable
    {
        private readonly PlayerLevelConfig _config;
        private readonly TrackConfig _track;
        private readonly GarageBoard _garage;
        private readonly Wallet _wallet;

        public PlayerLevel(PlayerLevelConfig config, TrackConfig track, GarageBoard garage, Wallet wallet)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Level = PlayerLevelConfig.FirstLevel;
            ClaimedRewardLevel = PlayerLevelConfig.FirstLevel;

            _garage.VehicleMerged += OnVehicleMerged;
        }

        public event Action ExperienceChanged;
        public event Action<int> LevelReached;
        public event Action<int, long> RewardClaimed;
        public event Action<int> TrackPlacesUnlocked;

        public int Level { get; private set; }
        public long Experience { get; private set; }
        public int ClaimedRewardLevel { get; private set; }
        public long ExperienceToNextLevel => _config.GetExperienceToNextLevel(Level);
        public float LevelProgress => (float)Experience / ExperienceToNextLevel;
        public int TrackCapacity => _track.Capacity.GetCapacity(Level);
        public bool HasPendingReward => ClaimedRewardLevel < Level;
        public int PendingRewardLevel => ClaimedRewardLevel + 1;

        public long GetReward(int level)
        {
            return _config.GetReward(level);
        }

        public void AddExperience(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Experience += amount;

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                RaiseLevel();
            }

            ExperienceChanged?.Invoke();
        }

        public CommandResult ClaimReward()
        {
            if (HasPendingReward == false)
            {
                return new CommandResult(FailureReason.NoLevelReward);
            }

            int level = PendingRewardLevel;
            long coins = _config.GetReward(level);

            ClaimedRewardLevel = level;
            _wallet.Add(coins);
            RewardClaimed?.Invoke(level, coins);

            return new CommandResult(FailureReason.None);
        }

        public void Restore(int level, long experience, int claimedRewardLevel)
        {
            if (level < PlayerLevelConfig.FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (experience < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(experience));
            }

            if (claimedRewardLevel < PlayerLevelConfig.FirstLevel || claimedRewardLevel > level)
            {
                throw new ArgumentOutOfRangeException(nameof(claimedRewardLevel));
            }

            Level = level;
            Experience = Math.Min(experience, ExperienceToNextLevel - 1);
            ClaimedRewardLevel = claimedRewardLevel;
            _garage.UnlockSlots(level);
        }

        public void Dispose()
        {
            _garage.VehicleMerged -= OnVehicleMerged;
        }

        private void RaiseLevel()
        {
            int previousTrackCapacity = TrackCapacity;

            Level++;
            _garage.UnlockSlots(Level);

            if (TrackCapacity > previousTrackCapacity)
            {
                TrackPlacesUnlocked?.Invoke(TrackCapacity);
            }

            LevelReached?.Invoke(Level);
        }

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            int mergedRank = result.Rank - 1;

            AddExperience(_config.GetMergeExperience(mergedRank));
        }
    }
}
