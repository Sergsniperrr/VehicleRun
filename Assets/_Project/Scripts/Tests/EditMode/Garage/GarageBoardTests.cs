using System.Collections.Generic;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Garage
{
    public class GarageBoardTests
    {
        private const int InitialOpenSlots = 6;
        private const int LastRank = 8;

        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void TwoRankFiveVehiclesMergeIntoOneRankSix()
        {
            Vehicle first = PlaceVehicle(0, 5);
            Vehicle second = PlaceVehicle(1, 5);

            CommandResult result = _garage.Merge(0, 1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(0), Is.True);
            Assert.That(_garage.TryGetVehicle(1, out Vehicle merged), Is.True);
            Assert.That(merged.Rank, Is.EqualTo(6));
            Assert.That(merged.InstanceId, Is.Not.EqualTo(first.InstanceId).And.Not.EqualTo(second.InstanceId));
        }

        [Test]
        public void RankFiveOnRankFourIsRejectedWithoutChanges()
        {
            Vehicle rankFive = PlaceVehicle(0, 5);
            Vehicle rankFour = PlaceVehicle(1, 4);

            CommandResult result = _garage.Merge(0, 1);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.RankMismatch));
            Assert.That(_garage.TryGetVehicle(0, out Vehicle source) && source == rankFive, Is.True);
            Assert.That(_garage.TryGetVehicle(1, out Vehicle target) && target == rankFour, Is.True);
        }

        [Test]
        public void BoxOpensInItsSlotWhenGarageIsFull()
        {
            const int boxSlot = 2;

            for (int slot = 0; slot < InitialOpenSlots; slot++)
            {
                if (slot != boxSlot)
                {
                    PlaceVehicle(slot, 1);
                }
            }

            _garage.PlaceBox(boxSlot, CreateBox(3));

            CommandResult result = _garage.OpenBox(boxSlot);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.TryGetBox(boxSlot, out _), Is.False);
            Assert.That(_garage.TryGetVehicle(boxSlot, out Vehicle vehicle) && vehicle.Rank == 3, Is.True);
            Assert.That(_garage.TryFindFreeSlot(out _), Is.False);
        }

        [Test]
        public void MergeKeepsVehicleMass()
        {
            PlaceVehicle(0, 3);
            PlaceVehicle(1, 3);
            PlaceVehicle(2, 1);
            long massBefore = CalculateMass();

            _garage.Merge(0, 1);

            Assert.That(CalculateMass(), Is.EqualTo(massBefore));
        }

        [Test]
        public void LastRankVehiclesDoNotMerge()
        {
            PlaceVehicle(0, LastRank);
            PlaceVehicle(1, LastRank);

            CommandResult result = _garage.Merge(0, 1);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.MaxRank));
            Assert.That(_garage.TryGetVehicle(0, out _) && _garage.TryGetVehicle(1, out _), Is.True);
        }

        [Test]
        public void VehicleDoesNotMergeIntoBox()
        {
            PlaceVehicle(0, 2);
            _garage.PlaceBox(1, CreateBox(2));

            CommandResult result = _garage.Merge(0, 1);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.TargetIsBox));
        }

        [Test]
        public void MergeRaisesEventWithResult()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 2);
            Vehicle merged = null;
            _garage.VehicleMerged += (fromSlot, toSlot, result) => merged = result;

            _garage.Merge(0, 1);

            Assert.That(merged, Is.Not.Null);
            Assert.That(merged.Rank, Is.EqualTo(3));
        }

        [Test]
        public void MoveToEmptySlotKeepsVehicle()
        {
            Vehicle vehicle = PlaceVehicle(0, 4);

            CommandResult result = _garage.Move(0, 3);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(0), Is.True);
            Assert.That(_garage.TryGetVehicle(3, out Vehicle moved) && moved == vehicle, Is.True);
        }

        [Test]
        public void MoveToOccupiedSlotIsRejected()
        {
            PlaceVehicle(0, 4);
            PlaceVehicle(1, 2);

            CommandResult result = _garage.Move(0, 1);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.SlotOccupied));
        }

        [Test]
        public void MoveToLockedSlotIsRejected()
        {
            PlaceVehicle(0, 4);

            CommandResult result = _garage.Move(0, InitialOpenSlots);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.SlotLocked));
        }

        [Test]
        public void OpenBoxOnSlotWithoutBoxIsRejected()
        {
            PlaceVehicle(0, 1);

            CommandResult result = _garage.OpenBox(0);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NoBox));
        }

        [Test]
        public void SlotsUnlockByMaxRankAndNeverLock()
        {
            List<int> unlocked = new List<int>();
            _garage.SlotsUnlocked += openSlotCount => unlocked.Add(openSlotCount);

            _garage.UnlockSlots(2);
            Assert.That(_garage.OpenSlotCount, Is.EqualTo(InitialOpenSlots));

            _garage.UnlockSlots(3);
            Assert.That(_garage.OpenSlotCount, Is.EqualTo(9));

            _garage.UnlockSlots(7);
            Assert.That(_garage.OpenSlotCount, Is.EqualTo(15));

            _garage.UnlockSlots(4);
            Assert.That(_garage.OpenSlotCount, Is.EqualTo(15));
            Assert.That(unlocked, Is.EqualTo(new[] { 9, 15 }));
        }

        private Vehicle PlaceVehicle(int slot, int rank)
        {
            Vehicle vehicle = new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank));
            _garage.PlaceVehicle(slot, vehicle);

            return vehicle;
        }

        private Box CreateBox(int rank)
        {
            return new Box(_instanceIds.Create(), BoxSource.Free, _fixture.Vehicles.GetByRank(rank));
        }

        private long CalculateMass()
        {
            long mass = 0;

            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetVehicle(slot, out Vehicle vehicle))
                {
                    mass += 1L << (vehicle.Rank - 1);
                }
            }

            return mass;
        }
    }
}
