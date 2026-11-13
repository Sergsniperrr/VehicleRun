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
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _garage);
        }

        [TearDown]
        public void TearDown()
        {
            _progress.Dispose();
            _fixture.Dispose();
        }

        [TestCase(1, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(12, 8)]
        public void ShopSellsRanksFourBelowMaxRank(int maxRank, int shopMaxRank)
        {
            UnlockRanksUpTo(maxRank);

            Assert.That(_progress.ShopMaxRank, Is.EqualTo(shopMaxRank));
        }

        [TestCase(1, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 2)]
        [TestCase(12, 4)]
        public void QuickBuyRankIsEightBelowMaxRank(int maxRank, int quickBuyRank)
        {
            UnlockRanksUpTo(maxRank);

            Assert.That(_progress.QuickBuyRank, Is.EqualTo(quickBuyRank));
        }

        [TestCase(1, 1, 1)]
        [TestCase(8, 1, 1)]
        [TestCase(9, 1, 2)]
        [TestCase(11, 1, 4)]
        [TestCase(12, 2, 5)]
        public void FreeBoxRanksAreTenToSevenBelowMaxRank(int maxRank, int lowestRank, int highestRank)
        {
            UnlockRanksUpTo(maxRank);

            Assert.That(_progress.FreeBoxLowestRank, Is.EqualTo(lowestRank));
            Assert.That(_progress.FreeBoxHighestRank, Is.EqualTo(highestRank));
        }

        [Test]
        public void NewRankDoesNotOpenGarageSlots()
        {
            int openSlotCount = _garage.OpenSlotCount;

            UnlockRanksUpTo(_fixture.Vehicles.MaxRank);

            Assert.That(_garage.OpenSlotCount, Is.EqualTo(openSlotCount));
        }

        [Test]
        public void PlacedVehicleUnlocksItsRank()
        {
            PlaceVehicle(0, 1);

            Assert.That(_progress.MaxRank, Is.EqualTo(1));
            Assert.That(_progress.IsRankUnlocked(1), Is.True);
        }

        [Test]
        public void MergeIntoNewRankRaisesMaxRank()
        {
            PlaceVehicle(0, 2);
            PlaceVehicle(1, 2);
            List<int> unlockedRanks = new List<int>();
            _progress.RankUnlocked += rank => unlockedRanks.Add(rank);

            _garage.Merge(0, 1);

            Assert.That(_progress.MaxRank, Is.EqualTo(3));
            Assert.That(unlockedRanks, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void OpenedBoxUnlocksVehicleRank()
        {
            _garage.PlaceBox(0, new Box(_instanceIds.Create(), BoxSource.Tutorial, _fixture.Vehicles.GetByRank(4)));

            _garage.OpenBox(0);

            Assert.That(_progress.MaxRank, Is.EqualTo(4));
        }

        [Test]
        public void RankUnlockedIsRaisedAfterProgressUpdates()
        {
            int maxRankOnEvent = 0;
            _progress.RankUnlocked += rank => maxRankOnEvent = _progress.MaxRank;

            _progress.UnlockRank(5);

            Assert.That(maxRankOnEvent, Is.EqualTo(5));
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
