using System;
using _Project.Features.Commands;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Collector
{
    public class CollectorServiceTests
    {
        private const float SpawnIntervalSeconds = 60f;
        private const float LifetimeSeconds = 10f;
        private const int MinCharges = 5;
        private const int MaxCharges = 10;
        private const float TrackSpeed = 4.4f;
        private const float ChargeRewardMultiplier = 1.5f;
        private const float TickSeconds = 1f;
        private const float DistanceTolerance = 0.01f;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private int _appearedCount;
        private int _disappearedCount;
        private long _lastCollectedAmount;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
            _appearedCount = 0;
            _disappearedCount = 0;
            _lastCollectedAmount = 0;
            _game.Collector.Appeared += OnAppeared;
            _game.Collector.Disappeared += OnDisappeared;
            _game.Collector.ChargeCollected += OnChargeCollected;
        }

        [TearDown]
        public void TearDown()
        {
            _game.Collector.Appeared -= OnAppeared;
            _game.Collector.Disappeared -= OnDisappeared;
            _game.Collector.ChargeCollected -= OnChargeCollected;
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void CollectorWaitsForSpawnInterval()
        {
            _game.PlaceVehicle(0, 1);

            _game.Collector.Advance(SpawnIntervalSeconds - TickSeconds);

            Assert.That(_game.Collector.IsActive, Is.False);

            _game.Collector.Advance(TickSeconds);

            Assert.That(_game.Collector.IsActive, Is.True);
            Assert.That(_appearedCount, Is.EqualTo(1));
            Assert.That(_game.Collector.RemainingSeconds, Is.EqualTo(LifetimeSeconds));
            Assert.That(_game.Collector.Distance, Is.Zero);
        }

        [Test]
        public void SpawnNowStartsFreshCollectorWithoutWaitingForTimer()
        {
            Assert.That(_game.Collector.SpawnNow(), Is.False);
            _game.PlaceVehicle(0, 2);

            Assert.That(_game.Collector.SpawnNow(), Is.True);
            _game.Collector.Advance(2f);
            Assert.That(_game.Collector.SpawnNow(), Is.True);

            Assert.That(_appearedCount, Is.EqualTo(2));
            Assert.That(_disappearedCount, Is.EqualTo(1));
            Assert.That(_game.Collector.Distance, Is.Zero);
            Assert.That(_game.Collector.RemainingSeconds, Is.EqualTo(LifetimeSeconds));
        }

        [Test]
        public void CollectorDoesNotAppearWithoutVehicles()
        {
            _game.Collector.Advance(SpawnIntervalSeconds);

            Assert.That(_game.Collector.IsActive, Is.False);

            _game.PlaceVehicle(0, 1);
            _game.Collector.Advance(TickSeconds);

            Assert.That(_game.Collector.IsActive, Is.True);
        }

        [Test]
        public void ChargeRewardIsTopVehicleCoinsPerLapTimesMultiplier()
        {
            _game.PlaceVehicle(0, 1);
            _game.PlaceVehicle(1, 4);

            Spawn();

            Assert.That(_game.Collector.ChargeReward, Is.EqualTo(ExpectedReward(4)));
        }

        [Test]
        public void VehicleOnTrackCountsForChargeReward()
        {
            _game.PlaceVehicle(0, 1);
            _game.PlaceVehicle(1, 4);
            _game.Roster.SendToTrack(1);

            Spawn();

            Assert.That(_game.Collector.ChargeReward, Is.EqualTo(ExpectedReward(4)));
        }

        [Test]
        public void ChargesComeFromConfigRange()
        {
            _game.PlaceVehicle(0, 1);
            _game.Random.Offset = 2;

            Spawn();

            Assert.That(_game.Collector.RemainingCharges, Is.EqualTo(MinCharges + 2));
            Assert.That(_game.Random.LastMin, Is.EqualTo(MinCharges));
            Assert.That(_game.Random.LastMax, Is.EqualTo(MaxCharges));
        }

        [Test]
        public void CollectorDrivesAndWrapsDistance()
        {
            Spawn();

            _game.Collector.Advance(TickSeconds);

            Assert.That(_game.Collector.Distance, Is.EqualTo(TrackSpeed).Within(DistanceTolerance));

            float lapLength = _fixture.Track.LapLength;
            float drivenSeconds = 9.5f;
            _game.Collector.Advance(drivenSeconds - TickSeconds);

            Assert.That(_game.Collector.IsActive, Is.True);
            Assert.That(_game.Collector.Distance, Is.EqualTo(TrackSpeed * drivenSeconds - lapLength).Within(DistanceTolerance));
        }

        [Test]
        public void CollectorDisappearsAfterLifetime()
        {
            Spawn();

            _game.Collector.Advance(LifetimeSeconds);

            Assert.That(_game.Collector.IsActive, Is.False);
            Assert.That(_disappearedCount, Is.EqualTo(1));
            Assert.That(_game.Collector.RemainingCharges, Is.Zero);
        }

        [Test]
        public void CollectAddsChargeRewardToWallet()
        {
            _game.PlaceVehicle(0, 2);
            Spawn();
            long coinsBefore = _game.Wallet.Coins;
            long reward = ExpectedReward(2);

            CommandResult result = _game.Collector.Collect();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_game.Wallet.Coins, Is.EqualTo(coinsBefore + reward));
            Assert.That(_game.Collector.RemainingCharges, Is.EqualTo(MinCharges - 1));
            Assert.That(_lastCollectedAmount, Is.EqualTo(reward));
        }

        [Test]
        public void CollectingLastChargeHidesCollector()
        {
            Spawn();

            for (int charge = 0; charge < MinCharges; charge++)
            {
                Assert.That(_game.Collector.Collect().IsSuccess, Is.True);
            }

            Assert.That(_game.Collector.IsActive, Is.False);
            Assert.That(_disappearedCount, Is.EqualTo(1));
            Assert.That(_game.Collector.Collect().FailureReason, Is.EqualTo(FailureReason.CollectorInactive));
        }

        [Test]
        public void CollectWithoutCollectorFails()
        {
            long coinsBefore = _game.Wallet.Coins;

            CommandResult result = _game.Collector.Collect();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.CollectorInactive));
            Assert.That(_game.Wallet.Coins, Is.EqualTo(coinsBefore));
        }

        [Test]
        public void ChargeRewardIsFixedAtAppear()
        {
            _game.PlaceVehicle(0, 1);
            Spawn();
            long reward = ExpectedReward(1);
            _game.PlaceVehicle(1, 5);
            long coinsBefore = _game.Wallet.Coins;

            _game.Collector.Collect();

            Assert.That(_game.Wallet.Coins, Is.EqualTo(coinsBefore + reward));
        }

        [Test]
        public void CollectorRespawnsAfterNextInterval()
        {
            Spawn();

            _game.Collector.Advance(LifetimeSeconds);

            Assert.That(_game.Collector.IsActive, Is.False);

            _game.Collector.Advance(SpawnIntervalSeconds);

            Assert.That(_game.Collector.IsActive, Is.True);
            Assert.That(_appearedCount, Is.EqualTo(2));
        }

        [Test]
        public void NegativeOrNanDeltaTimeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _game.Collector.Advance(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _game.Collector.Advance(float.NaN));
        }

        private long ExpectedReward(int rank)
        {
            return (long)Math.Round(_fixture.Vehicles.GetByRank(rank).CoinsPerLap * (double)ChargeRewardMultiplier);
        }

        private void Spawn()
        {
            if (HasAnyVehicle() == false)
            {
                _game.PlaceVehicle(0, 1);
            }

            _game.Collector.Advance(SpawnIntervalSeconds);

            Assert.That(_game.Collector.IsActive, Is.True);
        }

        private bool HasAnyVehicle()
        {
            for (int slot = 0; slot < _game.Garage.SlotCount; slot++)
            {
                if (_game.Garage.TryGetVehicle(slot, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnAppeared()
        {
            _appearedCount++;
        }

        private void OnDisappeared()
        {
            _disappearedCount++;
        }

        private void OnChargeCollected(long amount)
        {
            _lastCollectedAmount = amount;
        }
    }
}
