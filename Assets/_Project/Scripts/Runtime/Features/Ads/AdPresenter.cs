using System;
using _Project.Core.Advertising;
using _Project.Core.Economy;

namespace _Project.Features.Ads
{
    public class AdPresenter
    {
        private readonly IAdvertisingService _advertising;
        private readonly EconomyConfig _economy;

        private float _secondsSinceAd;

        public AdPresenter(IAdvertisingService advertising, EconomyConfig economy)
        {
            _advertising = advertising ?? throw new ArgumentNullException(nameof(advertising));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        public bool IsInterstitialReady => _secondsSinceAd >= _economy.InterstitialCooldownSeconds;

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _secondsSinceAd = Math.Min(_economy.InterstitialCooldownSeconds, _secondsSinceAd + deltaTime);
        }

        public void ShowRewarded(string rewardId, Action rewarded, Action closed)
        {
            _secondsSinceAd = 0f;
            _advertising.ShowRewarded(rewardId, rewarded, closed);
        }

        public bool TryShowInterstitial()
        {
            if (IsInterstitialReady == false)
            {
                return false;
            }

            _secondsSinceAd = 0f;
            _advertising.ShowInterstitial();

            return true;
        }
    }
}
