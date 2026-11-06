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
        public long MissingCoins => Math.Max(0, BoxPrice - _wallet.Coins);

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

        public CommandResult BuyBox()
        {
            if (HasReadyDelivery)
            {
                DeliverReadyBox();
            }

            if (_garage.TryFindFreeSlot(out int slot) == false)
            {
                return new CommandResult(FailureReason.NoFreeSlot);
            }

            int rank = _progress.BoxRank;

            if (_wallet.TrySpend(_pricing.GetPrice(rank)) == false)
            {
                return new CommandResult(FailureReason.NotEnoughCoins);
            }

            _pricing.RecordPurchase(rank);
            PlaceBox(slot, BoxSource.Purchased);

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
            PlaceBox(slot, BoxSource.Free);
        }

        private void PlaceBox(int slot, BoxSource source)
        {
            VehicleDefinition content = _catalog.GetByRank(_progress.BoxRank);
            _garage.PlaceBox(slot, new Box(_instanceIds.Create(), source, content));
        }
    }
}
