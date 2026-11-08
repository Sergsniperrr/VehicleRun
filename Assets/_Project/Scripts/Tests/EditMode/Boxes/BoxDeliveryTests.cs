using System;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Boxes
{
    public class BoxDeliveryTests
    {
        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private RankProgress _progress;
        private Wallet _wallet;
        private BoxPricing _pricing;
        private BoxDelivery _delivery;

        private float FreeBoxInterval => _fixture.Economy.FreeBoxIntervalSeconds;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _fixture.Track, _garage);
            _wallet = new Wallet(_fixture.Economy);
            _pricing = new BoxPricing(_fixture.Economy, _fixture.Vehicles);
            _delivery = new BoxDelivery(_fixture.Economy, _fixture.Vehicles, _garage, _progress, _wallet, _pricing, _instanceIds);
        }

        [TearDown]
        public void TearDown()
        {
            _progress.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void FreeBoxAppearsInFirstFreeSlotAfterInterval()
        {
            PlaceVehicle(0, 1);
            PlaceVehicle(1, 1);

            _delivery.Advance(FreeBoxInterval - 1);

            Assert.That(_garage.IsSlotEmpty(2), Is.True);

            _delivery.Advance(1);

            Assert.That(_garage.TryGetBox(2, out Box box), Is.True);
            Assert.That(box.Source, Is.EqualTo(BoxSource.Free));
            Assert.That(box.Rank, Is.EqualTo(_progress.BoxRank));
            Assert.That(_delivery.FreeBoxRemainingSeconds, Is.EqualTo(FreeBoxInterval));
        }

        [Test]
        public void FreeBoxTimerStartsOverAfterDelivery()
        {
            _delivery.Advance(FreeBoxInterval);
            _delivery.Advance(FreeBoxInterval / 2);

            Assert.That(CountBoxes(), Is.EqualTo(1));

            _delivery.Advance(FreeBoxInterval / 2);

            Assert.That(CountBoxes(), Is.EqualTo(2));
        }

        [Test]
        public void FreeBoxInFullGarageWaitsAsSingleReadyDelivery()
        {
            FillGarage(1);

            _delivery.Advance(FreeBoxInterval);
            _delivery.Advance(FreeBoxInterval * 3);

            Assert.That(_delivery.HasReadyDelivery, Is.True);
            Assert.That(_delivery.FreeBoxRemainingSeconds, Is.EqualTo(0));
            Assert.That(CountBoxes(), Is.EqualTo(0));

            _garage.Merge(0, 1);
            _delivery.Advance(0);

            Assert.That(_garage.TryGetBox(0, out Box box), Is.True);
            Assert.That(box.Source, Is.EqualTo(BoxSource.Free));
            Assert.That(CountBoxes(), Is.EqualTo(1));
            Assert.That(_delivery.HasReadyDelivery, Is.False);
            Assert.That(_delivery.FreeBoxRemainingSeconds, Is.EqualTo(FreeBoxInterval));
        }

        [Test]
        public void ReadyDeliveryUsesBoxRankAtDeliveryTime()
        {
            int firstUnlockedSlot = _fixture.Garage.OpenSlots.Initial;
            FillGarage(1);
            _delivery.Advance(FreeBoxInterval);
            UnlockRanksUpTo(7);

            _delivery.Advance(0);

            Assert.That(_garage.TryGetBox(firstUnlockedSlot, out Box box), Is.True);
            Assert.That(box.Rank, Is.EqualTo(2));
        }

        [Test]
        public void BoughtBoxCostsPriceAndRaisesNextPrice()
        {
            _wallet.Add(100);
            long startCoins = _wallet.Coins;
            PlaceVehicle(0, 1);
            PlaceVehicle(1, 1);

            CommandResult result = _delivery.BuyBox();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_wallet.Coins, Is.EqualTo(startCoins - 80));
            Assert.That(_garage.TryGetBox(2, out Box box), Is.True);
            Assert.That(box.Source, Is.EqualTo(BoxSource.Purchased));
            Assert.That(box.Rank, Is.EqualTo(1));
            Assert.That(_pricing.GetPurchaseCount(1), Is.EqualTo(1));
            Assert.That(_delivery.BoxPrice, Is.EqualTo(86));
        }

        [Test]
        public void FullGarageAndPurchaseLeaveCoinsAndBoxesUnchanged()
        {
            long startCoins = _wallet.Coins;
            FillGarage(1);

            CommandResult result = _delivery.BuyBox();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NoFreeSlot));
            Assert.That(_wallet.Coins, Is.EqualTo(startCoins));
            Assert.That(CountBoxes(), Is.EqualTo(0));
            Assert.That(_pricing.GetPurchaseCount(1), Is.EqualTo(0));
        }

        [Test]
        public void PurchaseWithoutEnoughCoinsIsRejectedWithoutChanges()
        {
            _wallet.TrySpend(_wallet.Coins - 5);
            PlaceVehicle(0, 1);

            CommandResult result = _delivery.BuyBox();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NotEnoughCoins));
            Assert.That(_wallet.Coins, Is.EqualTo(5));
            Assert.That(CountBoxes(), Is.EqualTo(0));
            Assert.That(_pricing.GetPurchaseCount(1), Is.EqualTo(0));
        }

        [Test]
        public void MissingCoinsIsPriceMinusCoins()
        {
            _wallet.Add(100);

            Assert.That(_delivery.MissingCoins, Is.EqualTo(0));

            _wallet.TrySpend(_wallet.Coins - 5);

            Assert.That(_delivery.BoxPrice, Is.EqualTo(80));
            Assert.That(_delivery.MissingCoins, Is.EqualTo(75));
        }

        [Test]
        public void FreeBoxProgressFollowsTimerAndIsFullForReadyDelivery()
        {
            Assert.That(_delivery.FreeBoxProgress, Is.EqualTo(0f));

            _delivery.Advance(FreeBoxInterval / 4);

            Assert.That(_delivery.FreeBoxProgress, Is.EqualTo(0.25f).Within(0.0001f));

            FillGarage(1);
            _delivery.Advance(FreeBoxInterval);

            Assert.That(_delivery.HasReadyDelivery, Is.True);
            Assert.That(_delivery.FreeBoxProgress, Is.EqualTo(1f));
        }

        [Test]
        public void ReadyDeliveryTakesLastSlotBeforePurchase()
        {
            long startCoins = _wallet.Coins;
            FillGarage(1);
            _delivery.Advance(FreeBoxInterval);
            _garage.Merge(0, 1);

            CommandResult result = _delivery.BuyBox();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NoFreeSlot));
            Assert.That(_garage.TryGetBox(0, out Box box), Is.True);
            Assert.That(box.Source, Is.EqualTo(BoxSource.Free));
            Assert.That(_delivery.HasReadyDelivery, Is.False);
            Assert.That(_wallet.Coins, Is.EqualTo(startCoins));
        }

        [Test]
        public void FreeBoxDoesNotRaisePrice()
        {
            _delivery.Advance(FreeBoxInterval);

            Assert.That(CountBoxes(), Is.EqualTo(1));
            Assert.That(_pricing.GetPurchaseCount(1), Is.EqualTo(0));
            Assert.That(_delivery.BoxPrice, Is.EqualTo(80));
        }

        [Test]
        public void BoxAtMaxRankTenIsRankFiveForFortyThreeThousandCoins()
        {
            UnlockRanksUpTo(10);
            _wallet.Add(50000);
            long coins = _wallet.Coins;

            CommandResult result = _delivery.BuyBox();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_wallet.Coins, Is.EqualTo(coins - 43000));
            Assert.That(_garage.TryGetBox(0, out Box box), Is.True);
            Assert.That(box.Rank, Is.EqualTo(5));
        }

        [Test]
        public void LowerRankBoxCostsItsPriceAndRaisesOnlyItsPrice()
        {
            UnlockRanksUpTo(10);
            _wallet.Add(10000);
            long coins = _wallet.Coins;

            CommandResult result = _delivery.BuyBox(3);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_wallet.Coins, Is.EqualTo(coins - 5700));
            Assert.That(_garage.TryGetBox(0, out Box box), Is.True);
            Assert.That(box.Rank, Is.EqualTo(3));
            Assert.That(box.Source, Is.EqualTo(BoxSource.Purchased));
            Assert.That(_delivery.GetPrice(3), Is.EqualTo(6698));
            Assert.That(_delivery.BoxPrice, Is.EqualTo(43000));
        }

        [Test]
        public void RankAboveBoxRankIsNotSold()
        {
            UnlockRanksUpTo(10);
            _wallet.Add(1000000);
            long coins = _wallet.Coins;

            CommandResult result = _delivery.BuyBox(6);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.BoxRankLocked));
            Assert.That(_wallet.Coins, Is.EqualTo(coins));
            Assert.That(CountBoxes(), Is.EqualTo(0));
            Assert.That(_pricing.GetPurchaseCount(6), Is.EqualTo(0));
        }

        [Test]
        public void RanksUpToBoxRankAreForSale()
        {
            UnlockRanksUpTo(10);

            Assert.That(_delivery.IsRankForSale(1), Is.True);
            Assert.That(_delivery.IsRankForSale(5), Is.True);
            Assert.That(_delivery.IsRankForSale(6), Is.False);
            Assert.That(_delivery.IsRankForSale(_fixture.Vehicles.MaxRank), Is.False);
        }

        [Test]
        public void MissingCoinsForRankUseThatRankPrice()
        {
            UnlockRanksUpTo(10);
            _wallet.TrySpend(_wallet.Coins);
            _wallet.Add(1000);

            Assert.That(_delivery.GetMissingCoins(1), Is.EqualTo(0));
            Assert.That(_delivery.GetMissingCoins(2), Is.EqualTo(200));
            Assert.That(_delivery.GetMissingCoins(5), Is.EqualTo(42000));
        }

        [Test]
        public void RankOutsideCatalogThrows()
        {
            int outsideRank = _fixture.Vehicles.MaxRank + 1;

            Assert.Throws<ArgumentOutOfRangeException>(() => _delivery.BuyBox(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _delivery.BuyBox(outsideRank));
            Assert.Throws<ArgumentOutOfRangeException>(() => _delivery.IsRankForSale(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _delivery.IsRankForSale(outsideRank));
        }

        [Test]
        public void NegativeTimeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _delivery.Advance(-1f));
        }

        private void PlaceVehicle(int slot, int rank)
        {
            Vehicle vehicle = new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank));
            _garage.PlaceVehicle(slot, vehicle);
        }

        private void FillGarage(int rank)
        {
            for (int slot = 0; slot < _garage.OpenSlotCount; slot++)
            {
                PlaceVehicle(slot, rank);
            }
        }

        private void UnlockRanksUpTo(int maxRank)
        {
            for (int rank = 1; rank <= maxRank; rank++)
            {
                _progress.UnlockRank(rank);
            }
        }

        private int CountBoxes()
        {
            int boxCount = 0;

            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetBox(slot, out Box _))
                {
                    boxCount++;
                }
            }

            return boxCount;
        }
    }
}
