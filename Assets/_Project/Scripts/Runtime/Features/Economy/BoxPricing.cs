using System;
using System.Collections.Generic;
using _Project.Core.Economy;
using _Project.Core.Vehicles;

namespace _Project.Features.Economy
{
    public class BoxPricing
    {
        private const double MaxPrice = long.MaxValue;
        private const int FirstRank = 1;
        private const double RoundingTolerance = 1e-9;

        private readonly EconomyConfig _economy;
        private readonly VehicleCatalogConfig _catalog;
        private readonly int[] _purchasesByRank;

        public BoxPricing(EconomyConfig economy, VehicleCatalogConfig catalog)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _purchasesByRank = new int[catalog.MaxRank];
        }

        public int GetPurchaseCount(int rank)
        {
            CheckRank(rank);

            return _purchasesByRank[rank - 1];
        }

        public long GetPrice(int rank)
        {
            double growth = Math.Pow(GetGrowth(rank), CountGrowthSteps(rank));
            double rawPrice = _economy.BoxBasePrices[rank - 1] * growth;
            double price = Math.Ceiling(rawPrice - rawPrice * RoundingTolerance);

            if (price >= MaxPrice)
            {
                return long.MaxValue;
            }

            return (long)price;
        }

        public void RecordPurchase(int rank)
        {
            CheckRank(rank);
            _purchasesByRank[rank - 1]++;
        }

        public void RestorePurchaseCounts(IReadOnlyList<int> purchaseCounts)
        {
            if (purchaseCounts == null)
            {
                throw new ArgumentNullException(nameof(purchaseCounts));
            }

            if (purchaseCounts.Count > _purchasesByRank.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(purchaseCounts));
            }

            for (int i = 0; i < purchaseCounts.Count; i++)
            {
                if (purchaseCounts[i] < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(purchaseCounts));
                }

                _purchasesByRank[i] = purchaseCounts[i];
            }
        }

        private double GetGrowth(int rank)
        {
            if (rank == FirstRank)
            {
                return _economy.FirstRankBoxPriceGrowth;
            }

            return _economy.BoxPriceGrowth;
        }

        private int CountGrowthSteps(int rank)
        {
            int purchaseCount = GetPurchaseCount(rank);

            if (_economy.IsBoxPriceGrowthLimited)
            {
                return Math.Min(purchaseCount, _economy.BoxPriceGrowthPurchaseLimit);
            }

            return purchaseCount;
        }

        private void CheckRank(int rank)
        {
            if (rank < FirstRank || rank > _catalog.MaxRank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }
        }
    }
}
