using System;
using System.Collections.Generic;
using _Project.Core.Economy;
using _Project.Core.Randomness;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;

namespace _Project.Features.Shop
{
    public class ShopRewards
    {
        private const int NoRank = 0;

        private readonly EconomyConfig _economy;
        private readonly Wallet _wallet;
        private readonly BoxDelivery _delivery;
        private readonly IRandomSource _random;

        private float _cooldownRemainingSeconds;

        public ShopRewards(EconomyConfig economy, Wallet wallet, BoxDelivery delivery, IRandomSource random)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool IsReady => _cooldownRemainingSeconds <= 0f;
        public float CooldownRemainingSeconds => _cooldownRemainingSeconds;
        public int DiscountPercent => _economy.RewardAdDiscountPercent;

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _cooldownRemainingSeconds = Math.Max(0f, _cooldownRemainingSeconds - deltaTime);
        }

        public long GetDiscountedPrice(int rank)
        {
            return _delivery.GetDiscountedPrice(rank, DiscountPercent);
        }

        public RewardOffer SelectOffer(IReadOnlyList<int> openedRanks)
        {
            if (openedRanks == null)
            {
                throw new ArgumentNullException(nameof(openedRanks));
            }

            if (openedRanks.Count == 0)
            {
                return new RewardOffer(NoRank, NoRank);
            }

            if (openedRanks.Count == 1)
            {
                return new RewardOffer(openedRanks[0], NoRank);
            }

            return SelectForManyRows(openedRanks);
        }

        public CommandResult ClaimFree(int rank)
        {
            CommandResult result = _delivery.GrantRewardBox(rank);
            StartCooldownOnSuccess(result);

            return result;
        }

        public CommandResult ClaimDiscount(int rank)
        {
            CommandResult result = _delivery.BuyDiscountedBox(rank, DiscountPercent);
            StartCooldownOnSuccess(result);

            return result;
        }

        private RewardOffer SelectForManyRows(IReadOnlyList<int> openedRanks)
        {
            int lastIndex = openedRanks.Count - 1;
            List<int> discountIndices = new List<int>();

            for (int i = 1; i <= lastIndex; i++)
            {
                if (_wallet.Coins >= GetDiscountedPrice(openedRanks[i]))
                {
                    discountIndices.Add(i);
                }
            }

            if (discountIndices.Count == 0)
            {
                int freeIndex = _random.Range(0, lastIndex - 1);

                return new RewardOffer(openedRanks[freeIndex], NoRank);
            }

            int discountIndex = discountIndices[_random.Range(0, discountIndices.Count - 1)];
            int freeIndexAbove = _random.Range(0, discountIndex - 1);

            return new RewardOffer(openedRanks[freeIndexAbove], openedRanks[discountIndex]);
        }

        private void StartCooldownOnSuccess(CommandResult result)
        {
            if (result.IsSuccess)
            {
                _cooldownRemainingSeconds = _economy.RewardAdCooldownSeconds;
            }
        }
    }
}
