using System.Collections.Generic;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Garage
{
    public class GarageGesturesTests
    {
        private const int LockedSlot = 6;

        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private RankProgress _progress;
        private TrackRoster _roster;
        private GarageGestures _gestures;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _fixture.Track, _garage);
            _roster = new TrackRoster(_garage, _progress, _fixture.Track, new Wallet(_fixture.Economy));
            _gestures = new GarageGestures(_garage, _roster);
        }

        [TearDown]
        public void TearDown()
        {
            _progress.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void TapOpensBox()
        {
            PlaceBox(0, 2);

            CommandResult result = _gestures.Tap(0);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.TryGetVehicle(0, out Vehicle vehicle) && vehicle.Rank == 2, Is.True);
        }

        [Test]
        public void TapReturnsVehicleFromTrack()
        {
            PlaceVehicle(0, 2);
            _roster.SendToTrack(0);

            CommandResult result = _gestures.Tap(0);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsOnTrack(0), Is.False);
            Assert.That(_roster.Count, Is.EqualTo(0));
        }

        [Test]
        public void TapOnGarageVehicleChangesNothing()
        {
            Vehicle vehicle = PlaceVehicle(0, 2);

            CommandResult result = _gestures.Tap(0);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NotOnTrack));
            Assert.That(_garage.TryGetVehicle(0, out Vehicle kept) && kept == vehicle, Is.True);
            Assert.That(_garage.IsOnTrack(0), Is.False);
        }

        [Test]
        public void GarageVehicleOpensCard()
        {
            Vehicle vehicle = PlaceVehicle(0, 2);

            Assert.That(_gestures.TryGetCardVehicle(0, out Vehicle cardVehicle), Is.True);
            Assert.That(cardVehicle, Is.SameAs(vehicle));
        }

        [Test]
        public void TrackVehicleDoesNotOpenCard()
        {
            PlaceVehicle(0, 2);
            _roster.SendToTrack(0);

            Assert.That(_gestures.TryGetCardVehicle(0, out Vehicle cardVehicle), Is.False);
            Assert.That(cardVehicle, Is.Null);
        }

        [Test]
        public void BoxDoesNotOpenCard()
        {
            PlaceBox(0, 2);

            Assert.That(_gestures.TryGetCardVehicle(0, out Vehicle _), Is.False);
        }

        [Test]
        public void EmptySlotDoesNotOpenCard()
        {
            Assert.That(_gestures.TryGetCardVehicle(0, out Vehicle _), Is.False);
        }

        [Test]
        public void DropOnEmptySlotMovesVehicle()
        {
            Vehicle vehicle = PlaceVehicle(0, 3);

            CommandResult result = _gestures.DropOnSlot(0, 4);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(0), Is.True);
            Assert.That(_garage.TryGetVehicle(4, out Vehicle moved) && moved == vehicle, Is.True);
        }

        [Test]
        public void DropOnSameRankMergesVehicles()
        {
            PlaceVehicle(0, 3);
            PlaceVehicle(1, 3);

            CommandResult result = _gestures.DropOnSlot(0, 1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(0), Is.True);
            Assert.That(_garage.TryGetVehicle(1, out Vehicle merged) && merged.Rank == 4, Is.True);
        }

        [Test]
        public void DropOnOtherRankKeepsBothVehicles()
        {
            Vehicle source = PlaceVehicle(0, 5);
            Vehicle target = PlaceVehicle(1, 4);

            CommandResult result = _gestures.DropOnSlot(0, 1);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.RankMismatch));
            Assert.That(_garage.TryGetVehicle(0, out Vehicle keptSource) && keptSource == source, Is.True);
            Assert.That(_garage.TryGetVehicle(1, out Vehicle keptTarget) && keptTarget == target, Is.True);
        }

        [Test]
        public void DropOnVehicleOnTrackMergesAndTakesItOffTrack()
        {
            PlaceVehicle(0, 3);
            PlaceVehicle(1, 3);
            _roster.SendToTrack(1);
            List<string> events = new List<string>();
            _roster.VehicleReturned += runner => events.Add("returned " + runner.Slot);
            _garage.VehicleMerged += (fromSlot, toSlot, merged) => events.Add("merged " + toSlot);

            CommandResult result = _gestures.DropOnSlot(0, 1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(0), Is.True);
            Assert.That(_garage.TryGetVehicle(1, out Vehicle vehicle) && vehicle.Rank == 4, Is.True);
            Assert.That(_garage.IsOnTrack(1), Is.False);
            Assert.That(_roster.Count, Is.EqualTo(0));
            Assert.That(events, Is.EqualTo(new[] { "returned 1", "merged 1" }));
        }

        [Test]
        public void DragOfVehicleOnTrackMergesAndTakesItOffTrack()
        {
            PlaceVehicle(0, 3);
            PlaceVehicle(1, 3);
            PlaceVehicle(2, 2);
            _roster.SendToTrack(0);
            _roster.SendToTrack(2);

            CommandResult result = _gestures.DropOnSlot(0, 1);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.TryGetVehicle(1, out Vehicle vehicle) && vehicle.Rank == 4, Is.True);
            Assert.That(_garage.IsOnTrack(1), Is.False);
            Assert.That(_roster.Count, Is.EqualTo(1));
            Assert.That(_roster.Runners[0].Slot, Is.EqualTo(2));
        }

        [Test]
        public void MergeOfTwoVehiclesOnTrackTakesBothOffTrack()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 2);
            _roster.SendToTrack(0);
            _roster.SendToTrack(1);

            CommandResult result = _gestures.DropOnSlot(1, 0);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.TryGetVehicle(0, out Vehicle vehicle) && vehicle.Rank == 3, Is.True);
            Assert.That(_garage.IsOnTrack(0), Is.False);
            Assert.That(_roster.Count, Is.EqualTo(0));
        }

        [Test]
        public void VehicleOnTrackStaysOnTrackAfterOtherDrops()
        {
            Vehicle vehicle = PlaceVehicle(0, 3);
            PlaceVehicle(1, 4);
            _roster.SendToTrack(0);

            Assert.That(_gestures.DropOnSlot(0, 1).FailureReason, Is.EqualTo(FailureReason.RankMismatch));
            Assert.That(_gestures.DropOnSlot(0, 5).FailureReason, Is.EqualTo(FailureReason.VehicleOnTrack));
            Assert.That(_gestures.DropOnTrack(0).FailureReason, Is.EqualTo(FailureReason.AlreadyOnTrack));
            Assert.That(_gestures.Recycle(0).FailureReason, Is.EqualTo(FailureReason.VehicleOnTrack));
            Assert.That(_garage.TryGetVehicle(0, out Vehicle kept) && kept == vehicle, Is.True);
            Assert.That(_garage.IsOnTrack(0), Is.True);
            Assert.That(_roster.Count, Is.EqualTo(1));
        }

        [Test]
        public void MaxRankPairOnTrackStaysOnTrack()
        {
            int maxRank = _fixture.Vehicles.MaxRank;
            PlaceVehicle(0, maxRank);
            PlaceVehicle(1, maxRank);
            _roster.SendToTrack(1);

            CommandResult result = _gestures.DropOnSlot(0, 1);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.MaxRank));
            Assert.That(_garage.IsOnTrack(1), Is.True);
            Assert.That(_roster.Count, Is.EqualTo(1));
        }

        [Test]
        public void VehicleOnTrackTargetsOnlyPairs()
        {
            PlaceVehicle(0, 3);
            PlaceVehicle(1, 3);
            PlaceVehicle(2, 3);
            _roster.SendToTrack(1);

            Assert.That(_gestures.CanMerge(1, 0), Is.True);
            Assert.That(_gestures.CanMerge(0, 1), Is.True);
            Assert.That(_gestures.CanDropOnTrack(1), Is.False);
            Assert.That(_gestures.CanDropOnTrack(2), Is.True);
            Assert.That(_gestures.CanRecycle(1), Is.False);
            Assert.That(_gestures.CanRecycle(2), Is.True);
        }

        [Test]
        public void BoxIsNotMovedOrMerged()
        {
            Box box = PlaceBox(0, 2);
            PlaceVehicle(1, 2);

            Assert.That(_gestures.DropOnSlot(0, 3).FailureReason, Is.EqualTo(FailureReason.NoVehicle));
            Assert.That(_gestures.DropOnSlot(0, 1).FailureReason, Is.EqualTo(FailureReason.NoVehicle));
            Assert.That(_gestures.DropOnSlot(1, 0).FailureReason, Is.EqualTo(FailureReason.SlotOccupied));
            Assert.That(_garage.TryGetBox(0, out Box kept) && kept == box, Is.True);
        }

        [Test]
        public void DropOnTrackSendsVehicleUntilTrackIsFull()
        {
            for (int slot = 0; slot <= _roster.Capacity; slot++)
            {
                PlaceVehicle(slot, 1);
            }

            for (int slot = 0; slot < _roster.Capacity; slot++)
            {
                Assert.That(_gestures.DropOnTrack(slot).IsSuccess, Is.True);
            }

            Assert.That(_gestures.DropOnTrack(_roster.Capacity).FailureReason, Is.EqualTo(FailureReason.TrackFull));
            Assert.That(_garage.IsOnTrack(_roster.Capacity), Is.False);
        }

        [Test]
        public void ItemsOfOpenSlotsCanBeDragged()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 2);
            PlaceBox(2, 1);
            _roster.SendToTrack(1);

            Assert.That(_gestures.CanDrag(0), Is.True);
            Assert.That(_gestures.CanDrag(1), Is.True);
            Assert.That(_gestures.CanDrag(2), Is.True);
            Assert.That(_gestures.CanDrag(3), Is.False);
            Assert.That(_gestures.CanDrag(LockedSlot), Is.False);
        }

        [Test]
        public void RecycleRemovesBox()
        {
            PlaceVehicle(0, 2);
            PlaceBox(1, 1);

            Assert.That(_gestures.Recycle(1).IsSuccess, Is.True);
            Assert.That(_garage.IsSlotEmpty(1), Is.True);
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
    }
}
