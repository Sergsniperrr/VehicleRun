using System;
using _Project.Core.Advertising;
using _Project.Core.Purchasing;
using PrimeGames.SDK;
using VContainer.Unity;

namespace _Project.Infrastructure.Prime
{
    public class PrimeAdvertisingService : IAdvertisingService, IStartable, IDisposable
    {
        private readonly IPurchaseService _purchases;

        public PrimeAdvertisingService(IPurchaseService purchases)
        {
            _purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
        }

        public void Start()
        {
            PrimeSDK.WaitForProviders(ApplyBannerPolicy);
            _purchases.Changed += ApplyBannerPolicy;
        }

        public void Dispose()
        {
            _purchases.Changed -= ApplyBannerPolicy;
        }

        public void ShowInterstitial()
        {
            if (_purchases.AdsDisabled)
            {
                return;
            }

            PrimeSDK.Ads.InvokeInterstitial();
        }

        public void ShowRewarded(string rewardId, Action rewarded, Action closed)
        {
            if (rewarded == null)
            {
                throw new ArgumentNullException(nameof(rewarded));
            }

            PrimeSDK.Ads.InvokeRewarded(
                onClose: isSuccess =>
                {
                    if (isSuccess)
                    {
                        rewarded();
                    }

                    closed?.Invoke();
                },
                rewardTag: rewardId);
        }

        private void ApplyBannerPolicy()
        {
            if (_purchases.AdsDisabled)
            {
                PrimeSDK.Ads.DisableBanner();
            }
        }
    }
}
