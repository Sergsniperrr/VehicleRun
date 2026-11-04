using System;
using System.Collections.Generic;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Track
{
    public class TrackRosterTests
    {
        private const int LockedSlot = 6;
        private const float AlmostWholeLap = 0.99f;
        private const float HalfLap = 0.5f;
        private const float LongStepLaps = 3.5f;
        private const int LongStepCrossings = 3;

        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private RankProgress _progress;
        private Wallet _wallet;
        private TrackRoster _roster;
        private long _startCoins;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _fixture.Track, _garage);
            _wallet = new Wallet(_fixture.Economy);
            _roster = new TrackRoster(_garage, _progress, _fixture.Track, _wallet);
            _startCoins = _wallet.Coins;
        }

        [TearDown]
        public void TearDown()
        {
            _progress.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void SentVehicleStaysInItsSlot()
        {
            Vehicle vehicle = PlaceVehicle(0, 2);
            TrackRunner sent = null;
            _roster.VehicleSent += runner => sent = runner;

            CommandResult result = _roster.SendToTrack(0);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsOnTrack(0), Is.True);
            Assert.That(_garage.TryGetVehicle(0, out Vehicle inSlot) && inSlot == vehicle, Is.True);
            Assert.That(_roster.Count, Is.EqualTo(1));
            Assert.That(sent.Vehicle, Is.EqualTo(vehicle));
            Assert.That(sent.Distance, Is.EqualTo(0));
        }

        [Test]
        public void SendIsRejectedWithReason()
        {
            PlaceVehicle(0, 1);
            PlaceVehicle(1, 1);
            PlaceVehicle(2, 1);
            _roster.SendToTrack(0);
            _roster.SendToTrack(1);

            Assert.That(_roster.SendToTrack(LockedSlot).FailureReason, Is.EqualTo(FailureReason.SlotLocked));
            Assert.That(_roster.SendToTrack(3).FailureReason, Is.EqualTo(FailureReason.NoVehicle));
            Assert.That(_roster.SendToTrack(0).FailureReason, Is.EqualTo(FailureReason.AlreadyOnTrack));
            Assert.That(_roster.SendToTrack(2).FailureReason, Is.EqualTo(FailureReason.TrackFull));
        }

        [Test]
        public void CapacityGrowsWithMaxRank()
        {
            PlaceVehicle(0, 3);

            Assert.That(_roster.Capacity, Is.EqualTo(2));

            PlaceVehicle(1, 4);

            Assert.That(_roster.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void ReturnBringsVehicleBackToItsSlot()
        {
            Vehicle vehicle = PlaceVehicle(0, 2);
            _roster.SendToTrack(0);
            TrackRunner returned = null;
            _roster.VehicleReturned += runner => returned = runner;

            CommandResult result = _roster.ReturnFromTrack(0);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsOnTrack(0), Is.False);
            Assert.That(_garage.TryGetVehicle(0, out Vehicle inSlot) && inSlot == vehicle, Is.True);
            Assert.That(_roster.Count, Is.EqualTo(0));
            Assert.That(returned.Vehicle, Is.EqualTo(vehicle));
        }

        [Test]
        public void ReturnOfVehicleNotOnTrackIsRejected()
        {
            PlaceVehicle(0, 2);

            CommandResult result = _roster.ReturnFromTrack(0);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NotOnTrack));
        }

        [Test]
        public void FinishPaysCoinsPerLap()
        {
            Vehicle vehicle = PlaceVehicle(0, 1);
            _roster.SendToTrack(0);
            float lapTime = CalculateLapTime(vehicle);

            _roster.Advance(lapTime * AlmostWholeLap);

            Assert.That(_wallet.Coins, Is.EqualTo(_startCoins));

            _roster.Advance(lapTime * (1 - AlmostWholeLap) * 2);

            Assert.That(_wallet.Coins, Is.EqualTo(_startCoins + vehicle.Definition.CoinsPerLap));
        }

        [Test]
        public void LongStepPaysEveryCrossing()
        {
            Vehicle vehicle = PlaceVehicle(0, 1);
            _roster.SendToTrack(0);
            List<long> payouts = new List<long>();
            _roster.FinishCrossed += (runner, payout) => payouts.Add(payout);

            _roster.Advance(CalculateLapTime(vehicle) * LongStepLaps);

            long expectedPayout = vehicle.Definition.CoinsPerLap * LongStepCrossings;
            Assert.That(payouts, Is.EqualTo(new[] { expectedPayout }));
            Assert.That(_wallet.Coins, Is.EqualTo(_startCoins + expectedPayout));
        }

        [Test]
        public void ReturnBeforeFinishLosesLap()
        {
            Vehicle vehicle = PlaceVehicle(0, 1);
            _roster.SendToTrack(0);
            float lapTime = CalculateLapTime(vehicle);
            _roster.Advance(lapTime * AlmostWholeLap);

            _roster.ReturnFromTrack(0);
            _roster.SendToTrack(0);
            _roster.Advance(lapTime * HalfLap);

            Assert.That(_wallet.Coins, Is.EqualTo(_startCoins));
        }

        [Test]
        public void NegativeStepIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _roster.Advance(-1));
        }

        [Test]
        public void BestLineupKeepsRunnersOnIncomeTie()
        {
            PlaceVehicle(0, 1);
            PlaceVehicle(1, 2);
            PlaceVehicle(2, 2);
            PlaceVehicle(3, 3);
            _roster.SendToTrack(0);
            _roster.SendToTrack(2);

            CommandResult result = _roster.ApplyBestLineup();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_garage.IsOnTrack(0), Is.False);
            Assert.That(_garage.IsOnTrack(1), Is.False);
            Assert.That(_garage.IsOnTrack(2), Is.True);
            Assert.That(_garage.IsOnTrack(3), Is.True);
            Assert.That(_roster.Count, Is.EqualTo(_roster.Capacity));
        }

        [Test]
        public void BestLineupPrefersLowerSlotOnIncomeTie()
        {
            PlaceVehicle(0, 1);
            PlaceVehicle(2, 2);
            PlaceVehicle(4, 2);
            PlaceVehicle(5, 2);

            _roster.ApplyBestLineup();

            Assert.That(_garage.IsOnTrack(2) && _garage.IsOnTrack(4), Is.True);
            Assert.That(_garage.IsOnTrack(0) || _garage.IsOnTrack(5), Is.False);
        }

        [Test]
        public void CanSendToTrackMatchesSendResult()
        {
            for (int slot = 0; slot <= _roster.Capacity; slot++)
            {
                PlaceVehicle(slot, 1);
            }

            Assert.That(_roster.CanSendToTrack(0), Is.True);
            Assert.That(_roster.CanSendToTrack(_roster.Capacity + 1), Is.False);

            for (int slot = 0; slot < _roster.Capacity; slot++)
            {
                _roster.SendToTrack(slot);
            }

            Assert.That(_roster.CanSendToTrack(0), Is.False);
            Assert.That(_roster.CanSendToTrack(_roster.Capacity), Is.False);
            Assert.That(_roster.Count, Is.EqualTo(_roster.Capacity));
        }

        private Vehicle PlaceVehicle(int slot, int rank)
        {
            Vehicle vehicle = new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank));
            _garage.PlaceVehicle(slot, vehicle);

            return vehicle;
        }

        private float CalculateLapTime(Vehicle vehicle)
        {
            return _fixture.Track.LapLength / vehicle.Definition.TrackSpeed;
        }
    }
}
