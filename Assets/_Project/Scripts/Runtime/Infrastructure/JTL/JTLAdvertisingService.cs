using System;
using _Project.Core.Advertising;
using _Project.Core.Platform;
using _Project.Core.Purchasing;
using Cysharp.Threading.Tasks;
using JTLStudio.SDK;
using VContainer.Unity;

namespace _Project.Infrastructure.JTL
{
    public class JTLAdvertisingService : IAdvertisingService, IStartable, IDisposable
    {
        private readonly IPlatformInitializer _platformInitializer;
        private readonly IPurchaseService _purchases;

        public JTLAdvertisingService(IPlatformInitializer platformInitializer, IPurchaseService purchases)
        {
            _platformInitializer = platformInitializer ?? throw new ArgumentNullException(nameof(platformInitializer));
            _purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
        }

        public void Start()
        {
            _purchases.Changed += ApplyBannerPolicy;
            ApplyBannerPolicyWhenReady().Forget();
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

            JTLSDK.Ads.ShowInterstitial();
        }

        public void ShowRewarded(string rewardId, Action rewarded, Action closed)
        {
            if (rewarded == null)
            {
                throw new ArgumentNullException(nameof(rewarded));
            }

            JTLSDK.Ads.ShowRewarded(rewardId, result =>
            {
                if (result == AdResult.Rewarded)
                {
                    rewarded();
                }

                closed?.Invoke();
            });
        }

        private async UniTaskVoid ApplyBannerPolicyWhenReady()
        {
            await _platformInitializer.InitializeAsync();

            ApplyBannerPolicy();
        }

        private void ApplyBannerPolicy()
        {
            if (_purchases.AdsDisabled)
            {
                JTLSDK.Ads.HideBanner();
            }
        }
    }
}
