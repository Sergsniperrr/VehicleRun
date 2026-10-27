using System;

namespace _Project.Core.Purchasing
{
    public interface IPurchaseService
    {
        bool AdsDisabled { get; }

        string RemoveAdsPrice { get; }

        event Action Changed;

        void BuyRemoveAds();
    }
}
