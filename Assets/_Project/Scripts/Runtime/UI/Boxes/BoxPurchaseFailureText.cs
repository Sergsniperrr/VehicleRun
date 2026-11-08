using System;
using _Project.Core.Localization;
using _Project.Features.Commands;
using _Project.Features.Economy;

namespace _Project.UI.Boxes
{
    public class BoxPurchaseFailureText
    {
        private const string NoFreeSlotKey = "ui.hud.no_free_slot";
        private const string NotEnoughCoinsKey = "ui.hud.not_enough_coins";

        private readonly ILocalizationService _localization;
        private readonly CoinsFormatter _formatter;

        public BoxPurchaseFailureText(ILocalizationService localization, CoinsFormatter formatter)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public string Create(FailureReason reason, long missingCoins)
        {
            switch (reason)
            {
                case FailureReason.NoFreeSlot:
                    return _localization.Get(NoFreeSlotKey);
                case FailureReason.NotEnoughCoins:
                    string missingCoinsText = _formatter.FormatExact(missingCoins);
                    return string.Format(_localization.Get(NotEnoughCoinsKey), missingCoinsText);
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason));
            }
        }
    }
}
