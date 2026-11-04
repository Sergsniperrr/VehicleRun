using System.Globalization;

namespace _Project.Features.Economy
{
    public class CoinsFormatter
    {
        private const string GroupSeparator = " ";
        private const string WholeNumberFormat = "N0";

        private readonly NumberFormatInfo _numberFormat;

        public CoinsFormatter()
        {
            _numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            _numberFormat.NumberGroupSeparator = GroupSeparator;
        }

        public string Format(long coins)
        {
            return coins.ToString(WholeNumberFormat, _numberFormat);
        }
    }
}
