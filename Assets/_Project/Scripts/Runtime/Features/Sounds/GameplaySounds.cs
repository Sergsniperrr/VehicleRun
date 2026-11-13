using System;
using _Project.Core.Audio;
using _Project.Core.Time;
using _Project.Core.Vehicles;
using _Project.Features.Garage;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.Features.Turbo;
using _Project.Features.Vehicles;
using VContainer.Unity;

namespace _Project.Features.Sounds
{
    public class GameplaySounds : IStartable, IDisposable
    {
        private readonly IAudioService _audio;
        private readonly GameSoundsConfig _sounds;
        private readonly ITimeService _time;
        private readonly GarageBoard _garage;
        private readonly TrackRoster _roster;
        private readonly CollectionMilestones _milestones;
        private readonly TurboService _turbo;
        private readonly VehicleCatalogConfig _catalog;
        private readonly PlayerLevel _playerLevel;

        private bool _hasMoneyPlayed;
        private float _lastMoneyTime;

        public GameplaySounds(
            IAudioService audio,
            GameSoundsConfig sounds,
            ITimeService time,
            GarageBoard garage,
            TrackRoster roster,
            CollectionMilestones milestones,
            TurboService turbo,
            VehicleCatalogConfig catalog,
            PlayerLevel playerLevel)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
        }

        public void Start()
        {
            _garage.VehicleMerged += OnVehicleMerged;
            _garage.BoxOpened += OnBoxOpened;
            _roster.FinishCrossed += OnFinishCrossed;
            _roster.VehicleSent += OnVehicleSent;
            _milestones.MilestoneClaimed += OnMilestoneClaimed;
            _turbo.TurboStarted += OnTurboStarted;
            _turbo.RunnerBoosted += OnRunnerBoosted;
            _playerLevel.RewardClaimed += OnRewardClaimed;
        }

        public void Dispose()
        {
            _garage.VehicleMerged -= OnVehicleMerged;
            _garage.BoxOpened -= OnBoxOpened;
            _roster.FinishCrossed -= OnFinishCrossed;
            _roster.VehicleSent -= OnVehicleSent;
            _milestones.MilestoneClaimed -= OnMilestoneClaimed;
            _turbo.TurboStarted -= OnTurboStarted;
            _turbo.RunnerBoosted -= OnRunnerBoosted;
            _playerLevel.RewardClaimed -= OnRewardClaimed;
        }

        private void PlayMoney()
        {
            float now = _time.Time;

            if (_hasMoneyPlayed && now - _lastMoneyTime < _sounds.MoneyMinIntervalSeconds)
            {
                return;
            }

            _hasMoneyPlayed = true;
            _lastMoneyTime = now;
            _audio.PlayOneShot(_sounds.Money);
        }

        private float CalculateStartMovingPitch(int rank)
        {
            int lastRank = _catalog.MaxRank;

            if (lastRank <= 1)
            {
                return _sounds.StartMovingFirstRankPitch;
            }

            float rankShare = (rank - 1) / (float)(lastRank - 1);
            float pitchRange = _sounds.StartMovingLastRankPitch - _sounds.StartMovingFirstRankPitch;

            return _sounds.StartMovingFirstRankPitch + pitchRange * rankShare;
        }

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            _audio.PlayOneShot(_sounds.Merge);
        }

        private void OnBoxOpened(int slot, Vehicle vehicle)
        {
            _audio.PlayOneShot(_sounds.BoxOpen);
        }

        private void OnFinishCrossed(TrackRunner runner, long payout)
        {
            PlayMoney();
        }

        private void OnMilestoneClaimed(int rank)
        {
            PlayMoney();
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            float pitch = CalculateStartMovingPitch(runner.Vehicle.Rank);
            _audio.PlayOneShot(_sounds.StartMoving, pitch);
        }

        private void OnTurboStarted()
        {
            _audio.PlayOneShot(_sounds.Boost);
        }

        private void OnRunnerBoosted(TrackRunner runner)
        {
            _audio.PlayOneShot(_sounds.Boost);
        }

        private void OnRewardClaimed(int level, long coins)
        {
            PlayMoney();
        }
    }
}
