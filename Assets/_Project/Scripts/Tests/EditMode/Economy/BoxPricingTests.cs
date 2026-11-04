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
            _pricing = new BoxPricing(_fixture.Economy, _fixture.Vehicles, _fixture.Track);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void FirstRankOneBoxCostsTwelveCoins()
        {
            Assert.That(_pricing.GetPrice(1), Is.EqualTo(12));
        }

        [Test]
        public void SecondRankOneBoxPriceIsRoundedUp()
        {
            _pricing.RecordPurchase(1);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(13));
        }

        [TestCase(20)]
        [TestCase(25)]
        public void RankOneBoxPriceStopsGrowingAfterTwentyPurchases(int purchaseCount)
        {
            RecordPurchases(1, purchaseCount);

            Assert.That(_pricing.GetPrice(1), Is.EqualTo(27));
        }

        [Test]
        public void FirstRankSixBoxCostsSixHundredTwentyFourCoins()
        {
            Assert.That(_pricing.GetPrice(6), Is.EqualTo(624));
        }

        [Test]
        public void PurchasesAreCountedPerRank()
        {
            RecordPurchases(1, 3);

            Assert.That(_pricing.GetPurchaseCount(1), Is.EqualTo(3));
            Assert.That(_pricing.GetPurchaseCount(2), Is.EqualTo(0));
            Assert.That(_pricing.GetPrice(2), Is.EqualTo(24));
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
