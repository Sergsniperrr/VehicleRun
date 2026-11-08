using System;
using _Project.Features.Economy;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Economy
{
    public class BoxPricingTests
    {
        private GameConfigFixture _fixture;
        private BoxPricing _pricing;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _pricing = new BoxPricing(_fixture.Economy, _fixture.Vehicles);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void FirstBoxCostsRankOneBasePrice()
        {
            Assert.That(_pricing.GetPrice(1), Is.EqualTo(80));
        }

        [Test]
        public void EachRankStartsFromItsBasePrice()
        {
            Assert.That(_pricing.GetPrice(2), Is.EqualTo(1200));
            Assert.That(_pricing.GetPrice(5), Is.EqualTo(43000));
            Assert.That(_pricing.GetPrice(10), Is.EqualTo(5000000));
        }

        [Test]
        public void RankOneBoxGrowsByFirstRankGrowth()
        {
            _pricing.RecordPurchase(1);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(86));
        }

        [Test]
        public void OtherRanksGrowByBoxPriceGrowth()
        {
            _pricing.RecordPurchase(3);

            Assert.That(_pricing.GetPrice(3), Is.EqualTo(6698));

            _pricing.RecordPurchase(3);

            Assert.That(_pricing.GetPrice(3), Is.EqualTo(7870));
        }

        [Test]
        public void WholePriceIsNotRaisedByRoundingError()
        {
            RecordPurchases(4, 2);

            Assert.That(_pricing.GetPrice(4), Is.EqualTo(22090));
        }

        [Test]
        public void UnlimitedGrowthKeepsRaisingPriceAfterPurchaseLimit()
        {
            RecordPurchases(1, 25);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(435));
        }

        [TestCase(20)]
        [TestCase(25)]
        public void LimitedGrowthStopsAfterPurchaseLimit(int purchaseCount)
        {
            _fixture.Change(_fixture.Economy, "_isBoxPriceGrowthLimited", property => property.boolValue = true);
            RecordPurchases(1, purchaseCount);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(310));
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
            Assert.That(_pricing.GetPrice(2), Is.EqualTo(1200));
        }

        [Test]
        public void RankOutsideCatalogThrows()
        {
            int outsideRank = _fixture.Vehicles.MaxRank + 1;

            Assert.Throws<ArgumentOutOfRangeException>(() => _pricing.GetPrice(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _pricing.GetPrice(outsideRank));
            Assert.Throws<ArgumentOutOfRangeException>(() => _pricing.RecordPurchase(outsideRank));
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
