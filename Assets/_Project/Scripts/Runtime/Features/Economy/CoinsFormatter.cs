using System;
using System.Globalization;
using _Project.Core.Localization;

namespace _Project.Features.Economy
{
    public class CoinsFormatter
    {
        private const string GroupSeparator = " ";
        private const string DecimalSeparatorKey = "ui.number.decimal_separator";
        private const string ThousandKey = "ui.number.thousand";
        private const string MillionKey = "ui.number.million";
        private const string BillionKey = "ui.number.billion";
        private const string WholeNumberFormat = "N0";
        private const string FractionalFormat = "0.#";
        private const double FractionalIncomeLimit = 10;
        private const long Thousand = 1000;
        private const long Million = 1000000;
        private const long Billion = 1000000000;
        private const long FractionalUnitLimit = 100;
        private const long TenthsPerUnit = 10;
        private const double TenthsPerUnitAsDouble = 10;

        private readonly ILocalizationService _localization;
        private readonly NumberFormatInfo _numberFormat;

        public CoinsFormatter(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            _numberFormat.NumberGroupSeparator = GroupSeparator;
            _numberFormat.NumberDecimalSeparator = _localization.Get(DecimalSeparatorKey);
        }

        public string FormatExact(long coins)
        {
            return coins.ToString(WholeNumberFormat, _numberFormat);
        }

        public string FormatShort(long coins)
        {
            if (coins >= Billion)
            {
                return FormatInUnits(coins, Billion, BillionKey);
            }

            if (coins >= Million)
            {
                return FormatInUnits(coins, Million, MillionKey);
            }

            if (coins >= Thousand)
            {
                return FormatInUnits(coins, Thousand, ThousandKey);
            }

            return FormatExact(coins);
        }

        public string FormatIncome(double coinsPerSecond)
        {
            if (coinsPerSecond < FractionalIncomeLimit)
            {
                return coinsPerSecond.ToString(FractionalFormat, _numberFormat);
            }

            long wholeCoinsPerSecond = (long)Math.Round(coinsPerSecond, MidpointRounding.AwayFromZero);

            return FormatShort(wholeCoinsPerSecond);
        }

        private string FormatInUnits(long coins, long unit, string unitKey)
        {
            long wholeUnits = coins / unit;
            string amount;

            if (wholeUnits >= FractionalUnitLimit)
            {
                amount = FormatExact(wholeUnits);
            }
            else
            {
                long tenths = coins / (unit / TenthsPerUnit);
                amount = (tenths / TenthsPerUnitAsDouble).ToString(FractionalFormat, _numberFormat);
            }

            return string.Format(_localization.Get(unitKey), amount);
        }
    }
}
