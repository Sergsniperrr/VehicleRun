using System;
using _Project.Core.Vehicles;
using _Project.Features.Progression;
using UnityEngine;
using VContainer;

namespace _Project.UI.Windows
{
    public class LevelUpNotice : MonoBehaviour
    {
        [SerializeField] private LevelUpWindow _levelUpWindow;

        private RankProgress _progress;
        private VehicleCatalogConfig _vehicles;

        private void Start()
        {
            _progress.RankUnlocked += OnRankUnlocked;
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
            _levelUpWindow.Show(_vehicles.GetByRank(rank));
        }
    }
}
