using System;
using _Project.Core.Audio;
using _Project.Core.Time;
using _Project.Features.Garage;
using _Project.Features.Progression;
using _Project.Features.Track;
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

        private bool _hasMoneyPlayed;
        private float _lastMoneyTime;

        public GameplaySounds(
            IAudioService audio,
            GameSoundsConfig sounds,
            ITimeService time,
            GarageBoard garage,
            TrackRoster roster,
            CollectionMilestones milestones)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
        }

        public void Start()
        {
            _garage.VehicleMerged += OnVehicleMerged;
            _roster.FinishCrossed += OnFinishCrossed;
            _milestones.MilestoneClaimed += OnMilestoneClaimed;
        }

        public void Dispose()
        {
            _garage.VehicleMerged -= OnVehicleMerged;
            _roster.FinishCrossed -= OnFinishCrossed;
            _milestones.MilestoneClaimed -= OnMilestoneClaimed;
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

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            _audio.PlayOneShot(_sounds.Merge);
        }

        private void OnFinishCrossed(TrackRunner runner, long payout)
        {
            PlayMoney();
        }

        private void OnMilestoneClaimed(int rank)
        {
            PlayMoney();
        }
    }
}
