using System;
using System.Threading;
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
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();

        public JTLAdvertisingService(IPlatformInitializer platformInitializer, IPurchaseService purchases)
        {
            _platformInitializer = platformInitializer ?? throw new ArgumentNullException(nameof(platformInitializer));
            _purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
        }

        public void Start()
        {
            _purchases.Changed += OnPurchasesChanged;
            ApplyBannerPolicyWhenReady(_lifetimeCancellation.Token).Forget();
        }

        public void Dispose()
        {
            _purchases.Changed -= OnPurchasesChanged;
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
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

        private async UniTaskVoid ApplyBannerPolicyWhenReady(CancellationToken cancellationToken)
        {
            await _platformInitializer.InitializeAsync(cancellationToken);

            ApplyBannerPolicy();
        }

        private void ApplyBannerPolicy()
        {
            if (_purchases.AdsDisabled)
            {
                JTLSDK.Ads.HideBanner();
            }
        }

        private void OnPurchasesChanged()
        {
            ApplyBannerPolicy();
        }
    }
}
