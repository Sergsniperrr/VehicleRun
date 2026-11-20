using _Project.Features.Economy;
using _Project.Tests.EditMode.Localization;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Economy
{
    public class CoinsFormatterTests
    {
        private const string DecimalSeparatorKey = "ui.number.decimal_separator";
        private const string ThousandKey = "ui.number.thousand";
        private const string MillionKey = "ui.number.million";
        private const string BillionKey = "ui.number.billion";
        private const string TrillionKey = "ui.number.trillion";
        private const string QuadrillionKey = "ui.number.quadrillion";

        private CoinsFormatter _formatter;

        [SetUp]
        public void SetUp()
        {
            FakeLocalizationService localization = new FakeLocalizationService(false);
            localization.Set(DecimalSeparatorKey, ",");
            localization.Set(ThousandKey, "{0}K");
            localization.Set(MillionKey, "{0}M");
            localization.Set(BillionKey, "{0}B");
            localization.Set(TrillionKey, "{0}T");
            localization.Set(QuadrillionKey, "{0}Qa");
            _formatter = new CoinsFormatter(localization);
        }

        [Test]
        public void ExactCoinsAreGroupedBySpaces()
        {
            Assert.That(_formatter.FormatExact(1234567), Is.EqualTo("1 234 567"));
        }

        [TestCase(0, "0")]
        [TestCase(7, "7")]
        [TestCase(999, "999")]
        public void ShortCoinsBelowThousandAreExact(long coins, string expected)
        {
            Assert.That(_formatter.FormatShort(coins), Is.EqualTo(expected));
        }

        [TestCase(1000, "1K")]
        [TestCase(1234, "1,2K")]
        [TestCase(1999, "1,9K")]
        [TestCase(12345, "12,3K")]
        [TestCase(99999, "99,9K")]
        [TestCase(123456, "123K")]
        [TestCase(999999, "999K")]
        public void ShortThousandsDropTheRest(long coins, string expected)
        {
            Assert.That(_formatter.FormatShort(coins), Is.EqualTo(expected));
        }

        [TestCase(1234567, "1,2M")]
        [TestCase(35912345, "35,9M")]
        [TestCase(1299999999, "1,2B")]
        [TestCase(999999999999, "999B")]
        [TestCase(1234567890123, "1,2T")]
        [TestCase(123456789012345, "123T")]
        [TestCase(1200000000000000, "1,2Qa")]
        [TestCase(1234567890123456, "1,2Qa")]
        [TestCase(1234567890123456789, "1 234Qa")]
        public void ShortMillionsBillionsTrillionsAndQuadrillionsDropTheRest(long coins, string expected)
        {
            Assert.That(_formatter.FormatShort(coins), Is.EqualTo(expected));
        }

        [Test]
        public void ShortCoinsUseLanguageFormat()
        {
            FakeLocalizationService english = new FakeLocalizationService(true);
            english.Set(DecimalSeparatorKey, ".");
            english.Set(ThousandKey, "{0}K");
            CoinsFormatter formatter = new CoinsFormatter(english);

            Assert.That(formatter.FormatShort(1234), Is.EqualTo("1.2K"));
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
        [TestCase(999.4, "999")]
        public void IncomeFromTenShowsWholeCoins(double coinsPerSecond, string expected)
        {
            Assert.That(_formatter.FormatIncome(coinsPerSecond), Is.EqualTo(expected));
        }

        [TestCase(1282598.6, "1,2M")]
        [TestCase(4560.2, "4,5K")]
        public void IncomeFromThousandIsShort(double coinsPerSecond, string expected)
        {
            Assert.That(_formatter.FormatIncome(coinsPerSecond), Is.EqualTo(expected));
        }
    }
}
