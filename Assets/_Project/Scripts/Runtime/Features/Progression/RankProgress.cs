using System;
using System.Collections.Generic;
using _Project.Core.Economy;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using _Project.Features.Garage;
using _Project.Features.Vehicles;

namespace _Project.Features.Progression
{
    public class RankProgress : IDisposable
    {
        private readonly VehicleCatalogConfig _catalog;
        private readonly EconomyConfig _economy;
        private readonly TrackConfig _track;
        private readonly GarageBoard _garage;
        private readonly bool[] _unlockedRanks;

        public RankProgress(VehicleCatalogConfig catalog, EconomyConfig economy, TrackConfig track, GarageBoard garage)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _unlockedRanks = new bool[catalog.MaxRank];

            _garage.VehiclePlaced += OnVehiclePlaced;
            _garage.BoxOpened += OnBoxOpened;
            _garage.VehicleMerged += OnVehicleMerged;
        }

        public event Action<int> RankUnlocked;
        public event Action<int> TrackPlacesUnlocked;

        public int MaxRank { get; private set; }
        public int BoxRank => Math.Max(1, MaxRank - _economy.BoxRankLag);
        public int TrackCapacity => _track.Capacity.GetCapacity(MaxRank);

        public bool IsRankUnlocked(int rank)
        {
            CheckRank(rank);

            return _unlockedRanks[rank - 1];
        }

        public void UnlockRank(int rank)
        {
            if (IsRankUnlocked(rank))
            {
                return;
            }

            _unlockedRanks[rank - 1] = true;

            if (rank > MaxRank)
            {
                IncreaseMaxRank(rank);
            }

            RankUnlocked?.Invoke(rank);
        }

        public void RestoreUnlockedRanks(IReadOnlyList<int> ranks)
        {
            if (ranks == null)
            {
                throw new ArgumentNullException(nameof(ranks));
            }

            if (MaxRank != 0)
            {
                throw new InvalidOperationException(nameof(MaxRank));
            }

            int maxRank = 0;

            foreach (int rank in ranks)
            {
                CheckRank(rank);
                _unlockedRanks[rank - 1] = true;
                maxRank = Math.Max(maxRank, rank);
            }

            MaxRank = maxRank;
            _garage.UnlockSlots(maxRank);
        }

        public void Dispose()
        {
            _garage.VehiclePlaced -= OnVehiclePlaced;
            _garage.BoxOpened -= OnBoxOpened;
            _garage.VehicleMerged -= OnVehicleMerged;
        }

        private void IncreaseMaxRank(int maxRank)
        {
            int previousTrackCapacity = TrackCapacity;
            MaxRank = maxRank;
            _garage.UnlockSlots(maxRank);

            if (TrackCapacity > previousTrackCapacity)
            {
                TrackPlacesUnlocked?.Invoke(TrackCapacity);
            }
        }

        private void CheckRank(int rank)
        {
            if (rank < 1 || rank > _catalog.MaxRank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }
        }

        private void OnVehiclePlaced(int slot, Vehicle vehicle)
        {
            UnlockRank(vehicle.Rank);
        }

        private void OnBoxOpened(int slot, Vehicle vehicle)
        {
            UnlockRank(vehicle.Rank);
        }

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            UnlockRank(result.Rank);
        }
    }
}
