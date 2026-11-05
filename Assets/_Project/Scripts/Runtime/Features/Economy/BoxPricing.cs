using System;
using _Project.Core.Economy;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using _Project.Features.Progression;

namespace _Project.Features.Economy
{
    public class BoxPricing
    {
        private const double MaxPrice = long.MaxValue;
        private const int FirstRank = 1;

        private readonly EconomyConfig _economy;
        private readonly VehicleCatalogConfig _catalog;
        private readonly TrackConfig _track;
        private readonly RankProgress _progress;
        private readonly int[] _purchasesByRank;

        public BoxPricing(EconomyConfig economy, VehicleCatalogConfig catalog, TrackConfig track, RankProgress progress)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
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
            double price = Math.Ceiling(_economy.BoxBasePriceIncomeSeconds * CalculateTopRankIncome() * growth);

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

        private double CalculateTopRankIncome()
        {
            int topRank = Math.Max(FirstRank, _progress.MaxRank);

            return _catalog.GetByRank(topRank).CalculateAverageIncome(_track.LapLength);
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
            if (rank < 1 || rank > _catalog.MaxRank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }
        }
    }
}
