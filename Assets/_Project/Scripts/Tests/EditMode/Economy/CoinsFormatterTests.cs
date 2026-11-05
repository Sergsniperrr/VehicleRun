using _Project.Features.Economy;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Economy
{
    public class CoinsFormatterTests
    {
        private CoinsFormatter _formatter;

        [SetUp]
        public void SetUp()
        {
            _formatter = new CoinsFormatter();
        }

        [Test]
        public void CoinsAreGroupedBySpaces()
        {
            Assert.That(_formatter.Format(1234567), Is.EqualTo("1 234 567"));
        }

        [TestCase(0, "0")]
        [TestCase(0.4, "0,4")]
        [TestCase(1, "1")]
        [TestCase(8.52, "8,5")]
        public void IncomeBelowTenShowsTenths(double coinsPerSecond, string expected)
        {
            Assert.That(_formatter.FormatIncome(coinsPerSecond), Is.EqualTo(expected));
        }

        [TestCase(16.4, "16")]
        [TestCase(1282598.6, "1 282 599")]
        public void IncomeFromTenShowsWholeCoins(double coinsPerSecond, string expected)
        {
            Assert.That(_formatter.FormatIncome(coinsPerSecond), Is.EqualTo(expected));
        }
    }
}
