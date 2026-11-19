using System;
using System.Collections.Generic;
using _Project.Features.Commands;
using _Project.Features.Shop;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Shop
{
    public class ShopRewardsTests
    {
        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private ShopRewards _rewards;

        private float Cooldown => _fixture.Economy.RewardAdCooldownSeconds;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
            _rewards = _game.ShopRewards;
        }

        [TearDown]
        public void TearDown()
        {
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void NoOpenedRowsGivesNoOffer()
        {
            RewardOffer offer = _rewards.SelectOffer(new List<int>());

            Assert.That(offer.HasFree, Is.False);
            Assert.That(offer.HasDiscount, Is.False);
        }

        [Test]
        public void SingleOpenedRowGivesOnlyFreeOffer()
        {
            RewardOffer offer = _rewards.SelectOffer(new List<int> { 3 });

            Assert.That(offer.FreeRank, Is.EqualTo(3));
            Assert.That(offer.HasDiscount, Is.False);
        }

        [Test]
        public void AffordableDiscountPutsFreeAboveDiscount()
        {
            _game.Wallet.Add(1000);

            RewardOffer offer = _rewards.SelectOffer(new List<int> { 1, 2 });

            Assert.That(offer.FreeRank, Is.EqualTo(1));
            Assert.That(offer.DiscountRank, Is.EqualTo(2));
        }

        [Test]
        public void UnaffordableDiscountGivesOnlyFreeOnNonBottomRow()
        {
            RewardOffer offer = _rewards.SelectOffer(new List<int> { 1, 2 });

            Assert.That(offer.FreeRank, Is.EqualTo(1));
            Assert.That(offer.HasDiscount, Is.False);
        }

        [Test]
        public void DiscountSkipsTopmostAndFreeStaysAbove()
        {
            _game.Wallet.Add(1_000_000);
            _game.Random.Offset = 1;

            RewardOffer offer = _rewards.SelectOffer(new List<int> { 1, 2, 3 });

            Assert.That(offer.DiscountRank, Is.EqualTo(3));
            Assert.That(offer.FreeRank, Is.EqualTo(2));
            Assert.That(offer.FreeRank, Is.LessThan(offer.DiscountRank));
        }

        [Test]
        public void ReadyUntilClaimThenCooldownBlocksUntilElapsed()
        {
            Assert.That(_rewards.IsReady, Is.True);

            CommandResult result = _rewards.ClaimFree(2);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_rewards.IsReady, Is.False);

            _rewards.Advance(Cooldown - 1f);

            Assert.That(_rewards.IsReady, Is.False);

            _rewards.Advance(1f);

            Assert.That(_rewards.IsReady, Is.True);
        }

        [Test]
        public void ClaimDiscountSpendsDiscountedPriceAndStartsCooldown()
        {
            _game.Wallet.Add(10000);
            long coins = _game.Wallet.Coins;

            CommandResult result = _rewards.ClaimDiscount(3);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_game.Wallet.Coins, Is.EqualTo(coins - 4560));
            Assert.That(_rewards.IsReady, Is.False);
        }

        [Test]
        public void FailedClaimKeepsReady()
        {
            FillGarage();

            CommandResult result = _rewards.ClaimFree(2);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NoFreeSlot));
            Assert.That(_rewards.IsReady, Is.True);
        }

        [Test]
        public void NegativeTimeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _rewards.Advance(-1f));
        }

        private void FillGarage()
        {
            for (int slot = 0; slot < _game.Garage.OpenSlotCount; slot++)
            {
                _game.PlaceVehicle(slot, 1);
            }
        }
    }
}
