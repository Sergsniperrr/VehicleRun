using System;
using System.Collections.Generic;
using _Project.Core.Economy;
using _Project.Core.Vehicles;
using _Project.Features.Garage;
using _Project.Features.Vehicles;

namespace _Project.Features.Progression
{
    public class RankProgress : IDisposable
    {
        private const int FirstRank = 1;

        private readonly VehicleCatalogConfig _catalog;
        private readonly EconomyConfig _economy;
        private readonly GarageBoard _garage;
        private readonly bool[] _unlockedRanks;

        public RankProgress(VehicleCatalogConfig catalog, EconomyConfig economy, GarageBoard garage)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _unlockedRanks = new bool[catalog.MaxRank];

            _garage.VehiclePlaced += OnVehiclePlaced;
            _garage.BoxOpened += OnBoxOpened;
            _garage.VehicleMerged += OnVehicleMerged;
        }

        public event Action<int> RankUnlocked;

        public int MaxRank { get; private set; }
        public int ShopMaxRank => CalculateLaggingRank(_economy.ShopRankLag);
        public int QuickBuyRank => CalculateLaggingRank(_economy.QuickBuyRankLag);
        public int FreeBoxLowestRank => CalculateLaggingRank(_economy.FreeBoxLowestRankLag);
        public int FreeBoxHighestRank => CalculateLaggingRank(_economy.FreeBoxHighestRankLag);

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

            MaxRank = Math.Max(MaxRank, rank);

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
        }

        public void Dispose()
        {
            _garage.VehiclePlaced -= OnVehiclePlaced;
            _garage.BoxOpened -= OnBoxOpened;
            _garage.VehicleMerged -= OnVehicleMerged;
        }

        private int CalculateLaggingRank(int rankLag)
        {
            return Math.Max(FirstRank, MaxRank - rankLag);
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
