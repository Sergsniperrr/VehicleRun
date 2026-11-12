using System;
using _Project.Core.Vehicles;
using _Project.Features.Garage;
using _Project.Features.Progression;
using UnityEngine;
using VContainer;

namespace _Project.UI.Windows
{
    public class LevelUpNotice : MonoBehaviour
    {
        [SerializeField] private LevelUpWindow _levelUpWindow;
        [SerializeField] private GarageView _garageView;

        private RankProgress _progress;
        private VehicleCatalogConfig _vehicles;
        private VehicleDefinition _pendingVehicle;

        private void Start()
        {
            _progress.RankUnlocked += OnRankUnlocked;
        }

        private void Update()
        {
            if (_pendingVehicle == null || _garageView.IsMergePlaying)
            {
                return;
            }

            VehicleDefinition vehicle = _pendingVehicle;
            _pendingVehicle = null;
            _levelUpWindow.Show(vehicle);
        }

        private void OnDestroy()
        {
            _progress.RankUnlocked -= OnRankUnlocked;
        }

        [Inject]
        public void Construct(RankProgress progress, VehicleCatalogConfig vehicles)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _vehicles = vehicles ?? throw new ArgumentNullException(nameof(vehicles));
        }

        private void OnRankUnlocked(int rank)
        {
            _pendingVehicle = _vehicles.GetByRank(rank);
        }
    }
}
