using _Project.Features.Commands;
using _Project.Features.Progression;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Progression
{
    public class CollectionMilestonesTests
    {
        private const int MilestoneRank = 4;
        private const int HigherMilestoneRank = 8;
        private const int NonMilestoneRank = 5;
        private const long MilestoneReward = 1000;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
        }

        [TearDown]
        public void TearDown()
        {
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void MilestoneIsLockedBeforeRankIsReached()
        {
            Assert.That(_game.Milestones.GetState(MilestoneRank), Is.EqualTo(MilestoneState.Locked));
        }

        [Test]
        public void MilestoneIsClaimableWhenRankIsReached()
        {
            _game.PlaceVehicle(0, MilestoneRank);

            Assert.That(_game.Milestones.GetState(MilestoneRank), Is.EqualTo(MilestoneState.Claimable));
        }

        [Test]
        public void NonMilestoneRankIsLockedEvenWhenReached()
        {
            _game.PlaceVehicle(0, NonMilestoneRank);

            Assert.That(_game.Milestones.GetState(NonMilestoneRank), Is.EqualTo(MilestoneState.Locked));
        }

        [Test]
        public void ClaimAddsCoinsAndMarksClaimed()
        {
            _game.PlaceVehicle(0, MilestoneRank);
            long coinsBefore = _game.Wallet.Coins;

            CommandResult result = _game.Milestones.Claim(MilestoneRank);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_game.Wallet.Coins, Is.EqualTo(coinsBefore + MilestoneReward));
            Assert.That(_game.Milestones.GetState(MilestoneRank), Is.EqualTo(MilestoneState.Claimed));
        }

        [Test]
        public void ClaimRaisesEvent()
        {
            _game.PlaceVehicle(0, MilestoneRank);
            int claimedRank = 0;
            _game.Milestones.MilestoneClaimed += rank => claimedRank = rank;

            _game.Milestones.Claim(MilestoneRank);

            Assert.That(claimedRank, Is.EqualTo(MilestoneRank));
        }

        [Test]
        public void SecondClaimFailsAndDoesNotPay()
        {
            _game.PlaceVehicle(0, MilestoneRank);
            _game.Milestones.Claim(MilestoneRank);
            long coinsAfterFirst = _game.Wallet.Coins;

            CommandResult result = _game.Milestones.Claim(MilestoneRank);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.MilestoneUnavailable));
            Assert.That(_game.Wallet.Coins, Is.EqualTo(coinsAfterFirst));
        }

        [Test]
        public void ClaimingLockedMilestoneFails()
        {
            long coinsBefore = _game.Wallet.Coins;

            CommandResult result = _game.Milestones.Claim(HigherMilestoneRank);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.MilestoneUnavailable));
            Assert.That(_game.Wallet.Coins, Is.EqualTo(coinsBefore));
        }

        [Test]
        public void GetRewardReturnsConfiguredCoins()
        {
            Assert.That(_game.Milestones.GetReward(MilestoneRank), Is.EqualTo(MilestoneReward));
            Assert.That(_game.Milestones.GetReward(NonMilestoneRank), Is.EqualTo(0));
        }

        [Test]
        public void RestoreMarksReachedMilestoneClaimed()
        {
            _game.PlaceVehicle(0, MilestoneRank);

            _game.Milestones.RestoreClaimed(new[] { MilestoneRank });

            Assert.That(_game.Milestones.GetState(MilestoneRank), Is.EqualTo(MilestoneState.Claimed));
        }

        [Test]
        public void RestoreIgnoresNonMilestoneRanks()
        {
            _game.Milestones.RestoreClaimed(new[] { NonMilestoneRank });

            Assert.That(_game.Milestones.ClaimedRanks, Is.Empty);
        }
    }
}
