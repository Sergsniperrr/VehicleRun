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
        private RankProgress _progress;

        private void Start()
        {
            _roster.VehicleSent += OnVehicleSent;
            _roster.VehicleReturned += OnVehicleReturned;
            _progress.TrackPlacesUnlocked += OnTrackPlacesUnlocked;
            ShowOccupancy();
        }

        private void OnDestroy()
        {
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;
            _progress.TrackPlacesUnlocked -= OnTrackPlacesUnlocked;
        }

        [Inject]
        public void Construct(TrackRoster roster, RankProgress progress)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
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
