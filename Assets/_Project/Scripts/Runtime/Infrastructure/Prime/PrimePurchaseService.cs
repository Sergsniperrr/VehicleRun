using System;
using _Project.Core.Purchasing;
using PrimeGames.SDK;
using PrimeGames.SDK.Common;

namespace _Project.Infrastructure.Prime
{
    public class PrimePurchaseService : IPurchaseService
    {
        private const string NoAdsProductId = "no_ads";
        private const string NoAdsOwnedKey = "no_ads.owned";

        public event Action Changed;

        public bool AdsDisabled =>
            PrimeSDK.Data.GetBool(NoAdsOwnedKey, false) ||
            PrimeSDK.Payments.IsAlreadyPurchased(NoAdsProductId);

        public string RemoveAdsPrice
        {
            get
            {
                ProductData data = PrimeSDK.Payments.GetProductData(NoAdsProductId);
                return data == null ? string.Empty : data.GetFullPriceInteger();
            }
        }

        public void BuyRemoveAds()
        {
            PrimeSDK.Payments.Purchase(NoAdsProductId, OnRemoveAdsPurchased);
        }

        private void OnRemoveAdsPurchased()
        {
            PrimeSDK.Data.SetBool(NoAdsOwnedKey, true, true);
            PrimeSDK.Data.Save();
            Changed?.Invoke();
        }
    }
}
