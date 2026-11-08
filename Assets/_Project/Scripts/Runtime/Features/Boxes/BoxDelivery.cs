using System;
using _Project.Core.Economy;
using _Project.Core.Vehicles;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;

namespace _Project.Features.Boxes
{
    public class BoxDelivery
    {
        private const int FirstRank = 1;

        private readonly EconomyConfig _economy;
        private readonly VehicleCatalogConfig _catalog;
        private readonly GarageBoard _garage;
        private readonly RankProgress _progress;
        private readonly Wallet _wallet;
        private readonly BoxPricing _pricing;
        private readonly InstanceIdGenerator _instanceIds;

        public BoxDelivery(
            EconomyConfig economy,
            VehicleCatalogConfig catalog,
            GarageBoard garage,
            RankProgress progress,
            Wallet wallet,
            BoxPricing pricing,
            InstanceIdGenerator instanceIds)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
            _instanceIds = instanceIds ?? throw new ArgumentNullException(nameof(instanceIds));
            FreeBoxRemainingSeconds = economy.FreeBoxIntervalSeconds;
        }

        public float FreeBoxRemainingSeconds { get; private set; }
        public float FreeBoxProgress => 1f - FreeBoxRemainingSeconds / _economy.FreeBoxIntervalSeconds;
        public bool HasReadyDelivery { get; private set; }
        public long BoxPrice => _pricing.GetPrice(_progress.BoxRank);
        public long MissingCoins => GetMissingCoins(_progress.BoxRank);

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (HasReadyDelivery)
            {
                DeliverReadyBox();

                return;
            }

            FreeBoxRemainingSeconds = Math.Max(0, FreeBoxRemainingSeconds - deltaTime);

            if (FreeBoxRemainingSeconds > 0)
            {
                return;
            }

            HasReadyDelivery = true;
            DeliverReadyBox();
        }

        public long GetPrice(int rank)
        {
            return _pricing.GetPrice(rank);
        }

        public long GetMissingCoins(int rank)
        {
            return Math.Max(0, _pricing.GetPrice(rank) - _wallet.Coins);
        }

        public bool IsRankForSale(int rank)
        {
            if (rank < FirstRank || rank > _catalog.MaxRank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            return rank <= _progress.BoxRank;
        }

        public CommandResult BuyBox()
        {
            return BuyBox(_progress.BoxRank);
        }

        public CommandResult BuyBox(int rank)
        {
            if (IsRankForSale(rank) == false)
            {
                return new CommandResult(FailureReason.BoxRankLocked);
            }

            if (HasReadyDelivery)
            {
                DeliverReadyBox();
            }

            if (_garage.TryFindFreeSlot(out int slot) == false)
            {
                return new CommandResult(FailureReason.NoFreeSlot);
            }

            if (_wallet.TrySpend(_pricing.GetPrice(rank)) == false)
            {
                return new CommandResult(FailureReason.NotEnoughCoins);
            }

            _pricing.RecordPurchase(rank);
            PlaceBox(slot, BoxSource.Purchased, rank);

            return new CommandResult(FailureReason.None);
        }

        public void Restore(float freeBoxRemainingSeconds, bool hasReadyDelivery)
        {
            if (float.IsNaN(freeBoxRemainingSeconds) || freeBoxRemainingSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(freeBoxRemainingSeconds));
            }

            FreeBoxRemainingSeconds = Math.Min(freeBoxRemainingSeconds, _economy.FreeBoxIntervalSeconds);
            HasReadyDelivery = hasReadyDelivery;
        }

        private void DeliverReadyBox()
        {
            if (_garage.TryFindFreeSlot(out int slot) == false)
            {
                return;
            }

            HasReadyDelivery = false;
            FreeBoxRemainingSeconds = _economy.FreeBoxIntervalSeconds;
            PlaceBox(slot, BoxSource.Free, _progress.BoxRank);
        }

        private void PlaceBox(int slot, BoxSource source, int rank)
        {
            VehicleDefinition content = _catalog.GetByRank(rank);
            _garage.PlaceBox(slot, new Box(_instanceIds.Create(), source, content));
        }
    }
}
