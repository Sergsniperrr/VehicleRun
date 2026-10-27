using System;
using _Project.Core.Platform;
using _Project.Core.Purchasing;
using Cysharp.Threading.Tasks;
using JTLStudio.SDK;
using VContainer.Unity;

namespace _Project.Infrastructure.JTL
{
    public class JTLPurchaseService : IPurchaseService, IStartable
    {
        private const string NoAdsProductId = "no_ads";
        private const string NoAdsOwnedKey = "no_ads.owned";

        private readonly IPlatformInitializer _platformInitializer;

        public event Action Changed;

        public JTLPurchaseService(IPlatformInitializer platformInitializer)
        {
            _platformInitializer = platformInitializer ?? throw new ArgumentNullException(nameof(platformInitializer));
        }

        public bool AdsDisabled =>
            JTLSDK.Data.GetBool(NoAdsOwnedKey) ||
            JTLSDK.Payments.IsAlreadyPurchased(NoAdsProductId);

        public string RemoveAdsPrice
        {
            get
            {
                ProductData data = JTLSDK.Payments.GetProductData(NoAdsProductId);
                return data.IsValid ? data.Formatted : string.Empty;
            }
        }

        public void Start()
        {
            RestorePurchasesWhenReady().Forget();
        }

        public void BuyRemoveAds()
        {
            JTLSDK.Payments.Purchase(NoAdsProductId, GrantRemoveAds);
        }

        private async UniTaskVoid RestorePurchasesWhenReady()
        {
            await _platformInitializer.InitializeAsync();

            if (JTLSDK.Payments.IsSupported == false)
            {
                return;
            }

            JTLSDK.Payments.RestorePurchases(OnRestoreData);
        }

        private void OnRestoreData(IRestoreData restoreData)
        {
            if (restoreData == null)
            {
                return;
            }

            foreach (string productId in restoreData.PendingProducts)
            {
                if (productId == NoAdsProductId)
                {
                    restoreData.RestoreProduct(productId, GrantRemoveAds);
                }
            }
        }

        private void GrantRemoveAds()
        {
            JTLSDK.Data.SetBool(NoAdsOwnedKey, true);
            Changed?.Invoke();
        }
    }
}
