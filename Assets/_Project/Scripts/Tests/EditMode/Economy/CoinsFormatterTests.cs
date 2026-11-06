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

        private CoinsFormatter _formatter;

        [SetUp]
        public void SetUp()
        {
            FakeLocalizationService localization = new FakeLocalizationService(false);
            localization.Set(DecimalSeparatorKey, ",");
            localization.Set(ThousandKey, "{0} тыс.");
            localization.Set(MillionKey, "{0} млн");
            localization.Set(BillionKey, "{0} млрд");
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

        [TestCase(1000, "1 тыс.")]
        [TestCase(1234, "1,2 тыс.")]
        [TestCase(1999, "1,9 тыс.")]
        [TestCase(12345, "12,3 тыс.")]
        [TestCase(99999, "99,9 тыс.")]
        [TestCase(123456, "123 тыс.")]
        [TestCase(999999, "999 тыс.")]
        public void ShortThousandsDropTheRest(long coins, string expected)
        {
            Assert.That(_formatter.FormatShort(coins), Is.EqualTo(expected));
        }

        [TestCase(1234567, "1,2 млн")]
        [TestCase(35912345, "35,9 млн")]
        [TestCase(1299999999, "1,2 млрд")]
        [TestCase(1234567890123, "1 234 млрд")]
        public void ShortMillionsAndBillionsDropTheRest(long coins, string expected)
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

        [TestCase(1282598.6, "1,2 млн")]
        [TestCase(4560.2, "4,5 тыс.")]
        public void IncomeFromThousandIsShort(double coinsPerSecond, string expected)
        {
            Assert.That(_formatter.FormatIncome(coinsPerSecond), Is.EqualTo(expected));
        }
    }
}
