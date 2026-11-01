using System;
using System.Collections.Generic;
using _Project.Features.Boxes;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Progression
{
    public class RankProgressTests
    {
        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private RankProgress _progress;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _fixture.Track, _garage);
        }

        [TearDown]
        public void TearDown()
        {
            _progress.Dispose();
            _fixture.Dispose();
        }

        [TestCase(2, 6)]
        [TestCase(3, 9)]
        [TestCase(4, 9)]
        [TestCase(5, 12)]
        [TestCase(6, 12)]
        [TestCase(7, 15)]
        public void GarageSlotsOpenAtRanksThreeFiveSeven(int maxRank, int openSlotCount)
        {
            UnlockRanksUpTo(maxRank);

            Assert.That(_garage.OpenSlotCount, Is.EqualTo(openSlotCount));
        }

        [TestCase(3, 2)]
        [TestCase(4, 4)]
        [TestCase(7, 4)]
        [TestCase(8, 6)]
        [TestCase(11, 6)]
        [TestCase(12, 8)]
        public void TrackPlacesOpenAtRanksFourEightTwelve(int maxRank, int trackCapacity)
        {
            UnlockRanksUpTo(maxRank);

            Assert.That(_progress.TrackCapacity, Is.EqualTo(trackCapacity));
        }

        [TestCase(1, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(12, 8)]
        public void BoxRankLagsBehindMaxRank(int maxRank, int boxRank)
        {
            UnlockRanksUpTo(maxRank);

            Assert.That(_progress.BoxRank, Is.EqualTo(boxRank));
        }

        [Test]
        public void TrackPlacesUnlockOnlyWhenCapacityGrows()
        {
            List<int> capacities = new List<int>();
            _progress.TrackPlacesUnlocked += capacity => capacities.Add(capacity);

            UnlockRanksUpTo(_fixture.Vehicles.MaxRank);

            Assert.That(capacities, Is.EqualTo(new[] { 4, 6, 8 }));
        }

        [Test]
        public void PlacedVehicleUnlocksItsRank()
        {
            PlaceVehicle(0, 1);

            Assert.That(_progress.MaxRank, Is.EqualTo(1));
            Assert.That(_progress.IsRankUnlocked(1), Is.True);
        }

        [Test]
        public void MergeIntoNewRankRaisesMaxRankAndOpensSlots()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 2);
            List<int> unlockedRanks = new List<int>();
            _progress.RankUnlocked += rank => unlockedRanks.Add(rank);

            _garage.Merge(0, 1);

            Assert.That(_progress.MaxRank, Is.EqualTo(3));
            Assert.That(unlockedRanks, Is.EqualTo(new[] { 3 }));
            Assert.That(_garage.OpenSlotCount, Is.EqualTo(9));
        }

        [Test]
        public void OpenedBoxUnlocksVehicleRank()
        {
            _garage.PlaceBox(0, new Box(_instanceIds.Create(), BoxSource.Tutorial, _fixture.Vehicles.GetByRank(4)));

            _garage.OpenBox(0);

            Assert.That(_progress.MaxRank, Is.EqualTo(4));
            Assert.That(_progress.TrackCapacity, Is.EqualTo(4));
        }

        [Test]
        public void RankUnlockedIsRaisedAfterProgressUpdates()
        {
            int maxRankOnEvent = 0;
            int openSlotCountOnEvent = 0;
            _progress.RankUnlocked += rank =>
            {
                maxRankOnEvent = _progress.MaxRank;
                openSlotCountOnEvent = _garage.OpenSlotCount;
            };

            _progress.UnlockRank(5);

            Assert.That(maxRankOnEvent, Is.EqualTo(5));
            Assert.That(openSlotCountOnEvent, Is.EqualTo(12));
        }

        [Test]
        public void UnlockedRankIsReportedOnce()
        {
            List<int> unlockedRanks = new List<int>();
            _progress.RankUnlocked += rank => unlockedRanks.Add(rank);

            _progress.UnlockRank(1);
            _progress.UnlockRank(2);
            _progress.UnlockRank(1);

            Assert.That(unlockedRanks, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void LowerRankDoesNotLowerMaxRank()
        {
            _progress.UnlockRank(5);
            _progress.UnlockRank(2);

            Assert.That(_progress.MaxRank, Is.EqualTo(5));
            Assert.That(_progress.IsRankUnlocked(2), Is.True);
            Assert.That(_progress.IsRankUnlocked(3), Is.False);
        }

        [Test]
        public void RankOutsideCatalogIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _progress.UnlockRank(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _progress.UnlockRank(_fixture.Vehicles.MaxRank + 1));
        }

        [Test]
        public void DisposedProgressIgnoresGarage()
        {
            _progress.Dispose();

            PlaceVehicle(0, 3);

            Assert.That(_progress.MaxRank, Is.EqualTo(0));
            Assert.That(_garage.OpenSlotCount, Is.EqualTo(6));
        }

        private void UnlockRanksUpTo(int maxRank)
        {
            for (int rank = 1; rank <= maxRank; rank++)
            {
                _progress.UnlockRank(rank);
            }
        }

        private void PlaceVehicle(int slot, int rank)
        {
            Vehicle vehicle = new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank));
            _garage.PlaceVehicle(slot, vehicle);
        }
    }
}
