using System.Collections.Generic;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Boxes;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Garage
{
    public class RecycleTests
    {
        private const int LockedSlot = 6;
        private const int EmergencyBoxRank = 1;

        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private RankProgress _progress;
        private PlayerLevel _playerLevel;
        private Wallet _wallet;
        private BoxPricing _pricing;
        private BoxDelivery _delivery;
        private TrackRoster _roster;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _garage);
            _wallet = new Wallet(_fixture.Economy);
            _pricing = new BoxPricing(_fixture.Economy, _fixture.Vehicles);
            _delivery = new BoxDelivery(_fixture.Economy, _fixture.Vehicles, _garage, _progress, _wallet, _pricing, _instanceIds, new FakeRandomSource());
            _playerLevel = new PlayerLevel(_fixture.PlayerLevel, _fixture.Track, _garage, _wallet);
            _roster = new TrackRoster(_garage, _playerLevel, _fixture.Track, _wallet);
        }

        [TearDown]
        public void TearDown()
        {
            _playerLevel.Dispose();
            _progress.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void RecycledVehicleLeavesSlotEmpty()
        {
            Vehicle vehicle = PlaceVehicle(0, 3);
            PlaceVehicle(1, 1);
            Vehicle recycled = null;
            _garage.VehicleRecycled += (slot, removed) => recycled = removed;
            long coinsBefore = _wallet.Coins;

            CommandResult result = _garage.Recycle(0);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(0), Is.True);
            Assert.That(recycled, Is.EqualTo(vehicle));
            Assert.That(_wallet.Coins, Is.EqualTo(coinsBefore));
        }

        [Test]
        public void RecycledBoxLeavesSlotEmpty()
        {
            Box box = PlaceBox(0, 2);
            PlaceVehicle(1, 1);
            Box recycled = null;
            _garage.BoxRecycled += (slot, removed) => recycled = removed;

            CommandResult result = _garage.Recycle(0);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(0), Is.True);
            Assert.That(recycled, Is.EqualTo(box));
        }

        [Test]
        public void RecycleIsRejectedWithReason()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 2);
            _roster.SendToTrack(0);

            Assert.That(_garage.Recycle(LockedSlot).FailureReason, Is.EqualTo(FailureReason.SlotLocked));
            Assert.That(_garage.Recycle(2).FailureReason, Is.EqualTo(FailureReason.SlotEmpty));
            Assert.That(_garage.Recycle(0).FailureReason, Is.EqualTo(FailureReason.VehicleOnTrack));
            Assert.That(_garage.TryGetVehicle(0, out _) && _garage.IsOnTrack(0), Is.True);
        }

        [Test]
        public void CanRecycleMatchesRecycleResult()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 2);
            _roster.SendToTrack(0);

            Assert.That(_garage.CanRecycle(LockedSlot), Is.False);
            Assert.That(_garage.CanRecycle(2), Is.False);
            Assert.That(_garage.CanRecycle(0), Is.False);
            Assert.That(_garage.CanRecycle(1), Is.True);
            Assert.That(_garage.TryGetVehicle(1, out _), Is.True);
        }

        [Test]
        public void RecycleKeepsMaxRankAndUnlockedRank()
        {
            PlaceVehicle(0, 5);
            PlaceVehicle(1, 1);

            _garage.Recycle(0);

            Assert.That(_progress.MaxRank, Is.EqualTo(5));
            Assert.That(_progress.IsRankUnlocked(5), Is.True);
        }

        [Test]
        public void LastVehicleIsReplacedByEmergencyBox()
        {
            PlaceVehicle(3, 5);
            List<string> events = new List<string>();
            _garage.VehicleRecycled += (slot, removed) => events.Add($"recycled {slot}");
            _garage.BoxPlaced += (slot, placed) => events.Add($"box {slot}");

            CommandResult result = _garage.Recycle(3);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.TryGetBox(3, out Box box), Is.True);
            Assert.That(box.Source, Is.EqualTo(BoxSource.Emergency));
            Assert.That(box.Rank, Is.EqualTo(EmergencyBoxRank));
            Assert.That(events, Is.EqualTo(new[] { "recycled 3", "box 3" }));
        }

        [Test]
        public void LastBoxIsReplacedByEmergencyBox()
        {
            Box box = PlaceBox(2, 4);

            _garage.Recycle(2);

            Assert.That(_garage.TryGetBox(2, out Box emergencyBox), Is.True);
            Assert.That(emergencyBox.Source, Is.EqualTo(BoxSource.Emergency));
            Assert.That(emergencyBox.InstanceId, Is.Not.EqualTo(box.InstanceId));
        }

        [Test]
        public void VehicleOnTrackCountsAsRemainingItem()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 3);
            _roster.SendToTrack(0);

            _garage.Recycle(1);

            Assert.That(_garage.IsSlotEmpty(1), Is.True);
        }

        [Test]
        public void EmergencyBoxKeepsBoxRankAndPrice()
        {
            const int maxRank = 10;
            UnlockRanksUpTo(maxRank);
            PlaceVehicle(0, maxRank);
            int boxRank = _progress.QuickBuyRank;
            long boxPrice = _delivery.BoxPrice;

            _garage.Recycle(0);

            Assert.That(_progress.QuickBuyRank, Is.EqualTo(boxRank));
            Assert.That(_delivery.BoxPrice, Is.EqualTo(boxPrice));
            Assert.That(_pricing.GetPurchaseCount(EmergencyBoxRank), Is.EqualTo(0));
            Assert.That(_pricing.GetPurchaseCount(boxRank), Is.EqualTo(0));
        }

        [Test]
        public void ReadyDeliveryTakesRecycledSlot()
        {
            FillGarage(1);
            _delivery.Advance(_fixture.Economy.FreeBoxIntervalSeconds);

            Assert.That(_delivery.HasReadyDelivery, Is.True);

            _garage.Recycle(4);
            _delivery.Advance(0);

            Assert.That(_delivery.HasReadyDelivery, Is.False);
            Assert.That(_garage.TryGetBox(4, out Box box) && box.Source == BoxSource.Free, Is.True);
        }

        [Test]
        public void DeadlockedGarageStillHasFreeAction()
        {
            _fixture.Change(_fixture.Garage, "_openSlots._steps", property => property.arraySize = 0);

            for (int slot = 0; slot < _garage.OpenSlotCount; slot++)
            {
                PlaceVehicle(slot, slot + 1);
            }

            for (int slot = 0; slot < _roster.Capacity; slot++)
            {
                _roster.SendToTrack(slot);
            }

            int lastSlot = _garage.OpenSlotCount - 1;

            Assert.That(_garage.Merge(lastSlot - 1, lastSlot).FailureReason, Is.EqualTo(FailureReason.RankMismatch));
            Assert.That(_delivery.BuyBox().FailureReason, Is.EqualTo(FailureReason.NoFreeSlot));
            Assert.That(_roster.SendToTrack(lastSlot).FailureReason, Is.EqualTo(FailureReason.TrackFull));

            CommandResult result = _garage.Recycle(lastSlot);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.TryFindFreeSlot(out int freeSlot) && freeSlot == lastSlot, Is.True);
        }

        private Vehicle PlaceVehicle(int slot, int rank)
        {
            Vehicle vehicle = new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank));
            _garage.PlaceVehicle(slot, vehicle);

            return vehicle;
        }

        private Box PlaceBox(int slot, int rank)
        {
            Box box = new Box(_instanceIds.Create(), BoxSource.Free, _fixture.Vehicles.GetByRank(rank));
            _garage.PlaceBox(slot, box);

            return box;
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
    }
}
