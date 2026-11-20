using System;
using _Project.Core.Advertising;

namespace _Project.Tests.EditMode.Ads
{
    public class FakeAdvertisingService : IAdvertisingService
    {
        public int InterstitialCount { get; private set; }
        public int RewardedCount { get; private set; }
        public bool GrantReward { get; set; } = true;

        public void ShowInterstitial()
        {
            InterstitialCount++;
        }

        public void ShowRewarded(string rewardId, Action rewarded, Action closed)
        {
            RewardedCount++;

            if (GrantReward)
            {
                rewarded?.Invoke();
            }

            closed?.Invoke();
        }
    }
}
