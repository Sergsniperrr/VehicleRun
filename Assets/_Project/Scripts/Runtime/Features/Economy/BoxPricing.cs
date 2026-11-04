using System;
using _Project.Core.Economy;
using _Project.Core.Track;
using _Project.Core.Vehicles;

namespace _Project.Features.Economy
{
    public class BoxPricing
    {
        private const double MaxPrice = long.MaxValue;

        private readonly EconomyConfig _economy;
        private readonly VehicleCatalogConfig _catalog;
        private readonly TrackConfig _track;
        private readonly int[] _purchasesByRank;

        public BoxPricing(EconomyConfig economy, VehicleCatalogConfig catalog, TrackConfig track)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _purchasesByRank = new int[catalog.MaxRank];
        }

        public int GetPurchaseCount(int rank)
        {
            CheckRank(rank);

            return _purchasesByRank[rank - 1];
        }

        public long GetPrice(int rank)
        {
            double income = _catalog.GetByRank(rank).CalculateAverageIncome(_track.LapLength);
            int growthSteps = Math.Min(GetPurchaseCount(rank), _economy.BoxPriceGrowthPurchaseLimit);
            double growth = Math.Pow(_economy.BoxPriceGrowth, growthSteps);
            double price = Math.Ceiling(_economy.BoxBasePriceIncomeSeconds * income * growth);

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

        private void CheckRank(int rank)
        {
            if (rank < 1 || rank > _catalog.MaxRank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }
        }
    }
}
