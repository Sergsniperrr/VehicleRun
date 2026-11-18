using System;
using _Project.Core.Track;
using _Project.Features.Progression;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.Features.Track
{
    public class StartSignView : MonoBehaviour
    {
        private const string BonusFormat = "+{0}%";

        [SerializeField] private TMP_Text _value;
        [SerializeField] private TMP_Text _fullTrackBonus;

        private TrackRoster _roster;
        private PlayerLevel _playerLevel;
        private TrackConfig _track;

        private void Start()
        {
            _roster.VehicleSent += OnVehicleSent;
            _roster.VehicleReturned += OnVehicleReturned;
            _playerLevel.TrackPlacesUnlocked += OnTrackPlacesUnlocked;
            ShowOccupancy();
        }

        private void OnDestroy()
        {
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;
            _playerLevel.TrackPlacesUnlocked -= OnTrackPlacesUnlocked;
        }

        [Inject]
        public void Construct(TrackRoster roster, PlayerLevel playerLevel, TrackConfig track)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        private void ShowOccupancy()
        {
            _value.text = $"{_roster.Count}/{_roster.Capacity}";
            ShowFullTrackBonus();
        }

        private void ShowFullTrackBonus()
        {
            bool hasBonus = _roster.IsFull && _track.FullTrackBonusPercent > 0;
            _fullTrackBonus.gameObject.SetActive(hasBonus);

            if (hasBonus)
            {
                _fullTrackBonus.text = string.Format(BonusFormat, _track.FullTrackBonusPercent);
            }
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            ShowOccupancy();
        }

        private void OnVehicleReturned(TrackRunner runner)
        {
            ShowOccupancy();
        }

        private void OnTrackPlacesUnlocked(int capacity)
        {
            ShowOccupancy();
        }
    }
}
