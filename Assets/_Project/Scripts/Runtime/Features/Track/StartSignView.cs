using System;
using _Project.Features.Progression;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.Features.Track
{
    public class StartSignView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _value;

        private TrackRoster _roster;
        private PlayerLevel _playerLevel;

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
        public void Construct(TrackRoster roster, PlayerLevel playerLevel)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
        }

        private void ShowOccupancy()
        {
            _value.text = $"{_roster.Count}/{_roster.Capacity}";
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
