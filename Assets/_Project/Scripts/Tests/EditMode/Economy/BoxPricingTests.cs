using System;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Economy
{
    public class BoxPricingTests
    {
        private GameConfigFixture _fixture;
        private RankProgress _progress;
        private BoxPricing _pricing;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            GarageBoard garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, new InstanceIdGenerator());
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _fixture.Track, garage);
            _pricing = new BoxPricing(_fixture.Economy, _fixture.Vehicles, _fixture.Track, _progress);
        }

        [TearDown]
        public void TearDown()
        {
            _progress.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void FirstBoxCostsTwentyCoins()
        {
            UnlockRanksUpTo(1);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(20));
        }

        [Test]
        public void PriceWithoutVehiclesUsesRankOneIncome()
        {
            Assert.That(_pricing.GetPrice(1), Is.EqualTo(20));
        }

        [Test]
        public void PriceFollowsTopRankIncome()
        {
            UnlockRanksUpTo(2);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(43));
        }

        [Test]
        public void RankOneBoxGrowsByFirstRankGrowth()
        {
            UnlockRanksUpTo(1);
            _pricing.RecordPurchase(1);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(21));
        }

        [Test]
        public void RankFiveBoxAtMaxRankTenGrowsByBoxPriceGrowth()
        {
            UnlockRanksUpTo(10);

            Assert.That(_pricing.GetPrice(5), Is.EqualTo(23181));

            _pricing.RecordPurchase(5);

            Assert.That(_pricing.GetPrice(5), Is.EqualTo(30135));
        }

        [Test]
        public void UnlimitedGrowthKeepsRaisingPriceAfterPurchaseLimit()
        {
            RecordPurchases(1, 25);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(105));
        }

        [TestCase(20)]
        [TestCase(25)]
        public void LimitedGrowthStopsAfterPurchaseLimit(int purchaseCount)
        {
            _fixture.Change(_fixture.Economy, "_isBoxPriceGrowthLimited", property => property.boolValue = true);
            RecordPurchases(1, purchaseCount);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(75));
        }

        [Test]
        public void PriceBeyondLongRangeIsCappedAtMaxLong()
        {
            RecordPurchases(1, 2000);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void PurchasesAreCountedPerRank()
        {
            RecordPurchases(1, 3);

            Assert.That(_pricing.GetPurchaseCount(1), Is.EqualTo(3));
            Assert.That(_pricing.GetPurchaseCount(2), Is.EqualTo(0));
            Assert.That(_pricing.GetPrice(2), Is.EqualTo(20));
        }

        [Test]
        public void RankOutsideCatalogThrows()
        {
            int outsideRank = _fixture.Vehicles.MaxRank + 1;

            Assert.Throws<ArgumentOutOfRangeException>(() => _pricing.GetPrice(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _pricing.GetPrice(outsideRank));
            Assert.Throws<ArgumentOutOfRangeException>(() => _pricing.RecordPurchase(outsideRank));
        }

        private void UnlockRanksUpTo(int maxRank)
        {
            for (int rank = 1; rank <= maxRank; rank++)
            {
                _progress.UnlockRank(rank);
            }
        }

        private void RecordPurchases(int rank, int purchaseCount)
        {
            for (int i = 0; i < purchaseCount; i++)
            {
                _pricing.RecordPurchase(rank);
            }
        }
    }
}
