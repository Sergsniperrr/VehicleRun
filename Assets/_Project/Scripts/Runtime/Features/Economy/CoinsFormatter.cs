using System;
using System.Globalization;

namespace _Project.Features.Economy
{
    public class CoinsFormatter
    {
        private const string GroupSeparator = " ";
        private const string DecimalSeparator = ",";
        private const string WholeNumberFormat = "N0";
        private const string FractionalIncomeFormat = "0.#";
        private const double FractionalIncomeLimit = 10;

        private readonly NumberFormatInfo _numberFormat;

        public CoinsFormatter()
        {
            _numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            _numberFormat.NumberGroupSeparator = GroupSeparator;
            _numberFormat.NumberDecimalSeparator = DecimalSeparator;
        }

        public string Format(long coins)
        {
            return coins.ToString(WholeNumberFormat, _numberFormat);
        }

        public string FormatIncome(double coinsPerSecond)
        {
            if (coinsPerSecond < FractionalIncomeLimit)
            {
                return coinsPerSecond.ToString(FractionalIncomeFormat, _numberFormat);
            }

            long wholeCoinsPerSecond = (long)Math.Round(coinsPerSecond, MidpointRounding.AwayFromZero);

            return Format(wholeCoinsPerSecond);
        }
    }
}
