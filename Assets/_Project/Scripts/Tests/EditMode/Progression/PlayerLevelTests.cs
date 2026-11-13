using System;
using System.Collections.Generic;
using _Project.Core.Progression;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Progression
{
    public class PlayerLevelTests
    {
        private const long FirstLevelExperience = 4;
        private const long SecondLevelExperience = 12;
        private const long SecondLevelReward = 100;
        private const long ThirdLevelReward = 170;
        private const long FifthLevelReward = 490;

        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private Wallet _wallet;
        private PlayerLevel _playerLevel;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _wallet = new Wallet(_fixture.Economy);
            _playerLevel = new PlayerLevel(_fixture.PlayerLevel, _fixture.Track, _garage, _wallet);
        }

        [TearDown]
        public void TearDown()
        {
            _playerLevel.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void NewPlayerStartsAtFirstLevelWithoutExperience()
        {
            Assert.That(_playerLevel.Level, Is.EqualTo(PlayerLevelConfig.FirstLevel));
            Assert.That(_playerLevel.Experience, Is.EqualTo(0));
            Assert.That(_playerLevel.ExperienceToNextLevel, Is.EqualTo(FirstLevelExperience));
            Assert.That(_playerLevel.HasPendingReward, Is.False);
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(5, 5)]
        public void MergeGivesExperienceOfMergedRank(int mergedRank, long experience)
        {
            _playerLevel.Restore(10, 0, 10);
            PlaceVehicle(0, mergedRank);
            PlaceVehicle(1, mergedRank);

            _garage.Merge(0, 1);

            Assert.That(_playerLevel.Experience, Is.EqualTo(experience));
        }

        [Test]
        public void FailedMergeGivesNoExperience()
        {
            PlaceVehicle(0, 1);
            PlaceVehicle(1, 2);

            _garage.Merge(0, 1);

            Assert.That(_playerLevel.Experience, Is.EqualTo(0));
        }

        [Test]
        public void OpenedBoxAndPlacedVehicleGiveNoExperience()
        {
            PlaceVehicle(0, 3);

            Assert.That(_playerLevel.Experience, Is.EqualTo(0));
            Assert.That(_playerLevel.Level, Is.EqualTo(PlayerLevelConfig.FirstLevel));
        }

        [Test]
        public void EnoughExperienceRaisesLevelAndKeepsRest()
        {
            _playerLevel.AddExperience(FirstLevelExperience + 1);

            Assert.That(_playerLevel.Level, Is.EqualTo(2));
            Assert.That(_playerLevel.Experience, Is.EqualTo(1));
            Assert.That(_playerLevel.ExperienceToNextLevel, Is.EqualTo(SecondLevelExperience));
        }

        [Test]
        public void BigExperienceRaisesSeveralLevels()
        {
            List<int> levels = new List<int>();
            _playerLevel.LevelReached += level => levels.Add(level);

            _playerLevel.AddExperience(FirstLevelExperience + SecondLevelExperience);

            Assert.That(levels, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(_playerLevel.Experience, Is.EqualTo(0));
        }

        [Test]
        public void LevelProgressIsShareOfCurrentLevel()
        {
            _playerLevel.AddExperience(1);

            Assert.That(_playerLevel.LevelProgress, Is.EqualTo(1f / FirstLevelExperience).Within(0.0001f));
        }

        [Test]
        public void ExperienceChangedIsRaisedAfterLevelReached()
        {
            List<string> events = new List<string>();
            _playerLevel.LevelReached += level => events.Add(nameof(_playerLevel.LevelReached));
            _playerLevel.ExperienceChanged += () => events.Add(nameof(_playerLevel.ExperienceChanged));

            _playerLevel.AddExperience(FirstLevelExperience);

            Assert.That(events, Is.EqualTo(new[] { nameof(_playerLevel.LevelReached), nameof(_playerLevel.ExperienceChanged) }));
        }

        [TestCase(1, 6)]
        [TestCase(2, 6)]
        [TestCase(3, 9)]
        [TestCase(4, 9)]
        [TestCase(5, 12)]
        [TestCase(7, 15)]
        [TestCase(20, 15)]
        public void GarageSlotsOpenByLevel(int level, int openSlotCount)
        {
            ReachLevel(level);

            Assert.That(_garage.OpenSlotCount, Is.EqualTo(openSlotCount));
        }

        [TestCase(1, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 4)]
        [TestCase(8, 6)]
        [TestCase(12, 8)]
        [TestCase(20, 8)]
        public void TrackPlacesOpenByLevel(int level, int trackCapacity)
        {
            ReachLevel(level);

            Assert.That(_playerLevel.TrackCapacity, Is.EqualTo(trackCapacity));
        }

        [Test]
        public void TrackPlacesUnlockOnlyWhenCapacityGrows()
        {
            List<int> capacities = new List<int>();
            _playerLevel.TrackPlacesUnlocked += capacity => capacities.Add(capacity);

            ReachLevel(13);

            Assert.That(capacities, Is.EqualTo(new[] { 4, 6, 8 }));
        }

        [Test]
        public void LevelReachedIsRaisedAfterSlotsOpen()
        {
            int openSlotCountOnEvent = 0;
            _playerLevel.LevelReached += level => openSlotCountOnEvent = _garage.OpenSlotCount;

            ReachLevel(3);

            Assert.That(openSlotCountOnEvent, Is.EqualTo(9));
        }

        [Test]
        public void ReachedLevelLeavesRewardToClaim()
        {
            ReachLevel(2);

            Assert.That(_playerLevel.HasPendingReward, Is.True);
            Assert.That(_playerLevel.PendingRewardLevel, Is.EqualTo(2));
            Assert.That(_wallet.Coins, Is.EqualTo(_fixture.Economy.StartCoins));
        }

        [Test]
        public void ClaimGivesCoinsOfLevelOnce()
        {
            ReachLevel(2);
            long coins = _wallet.Coins;

            CommandResult first = _playerLevel.ClaimReward();
            CommandResult second = _playerLevel.ClaimReward();

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.FailureReason, Is.EqualTo(FailureReason.NoLevelReward));
            Assert.That(_wallet.Coins, Is.EqualTo(coins + SecondLevelReward));
            Assert.That(_playerLevel.HasPendingReward, Is.False);
        }

        [Test]
        public void RewardsAreClaimedLevelByLevel()
        {
            List<long> rewards = new List<long>();
            _playerLevel.RewardClaimed += (level, coins) => rewards.Add(coins);
            ReachLevel(3);

            _playerLevel.ClaimReward();
            _playerLevel.ClaimReward();

            Assert.That(rewards, Is.EqualTo(new[] { SecondLevelReward, ThirdLevelReward }));
        }

        [Test]
        public void ClaimWithoutRewardIsRejected()
        {
            CommandResult result = _playerLevel.ClaimReward();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NoLevelReward));
        }

        [TestCase(1, 0)]
        [TestCase(2, SecondLevelReward)]
        [TestCase(3, ThirdLevelReward)]
        [TestCase(5, FifthLevelReward)]
        public void RewardGrowsWithLevelAndIsRounded(int level, long reward)
        {
            Assert.That(_playerLevel.GetReward(level), Is.EqualTo(reward));
        }

        [Test]
        public void HugeLevelRewardStopsAtLongLimit()
        {
            Assert.That(_playerLevel.GetReward(1000), Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void RestoreSetsLevelAndOpensSlotsWithoutEvents()
        {
            int eventCount = 0;
            _playerLevel.LevelReached += level => eventCount++;
            _playerLevel.ExperienceChanged += () => eventCount++;
            _playerLevel.TrackPlacesUnlocked += capacity => eventCount++;

            _playerLevel.Restore(5, 7, 4);

            Assert.That(_playerLevel.Level, Is.EqualTo(5));
            Assert.That(_playerLevel.Experience, Is.EqualTo(7));
            Assert.That(_playerLevel.PendingRewardLevel, Is.EqualTo(5));
            Assert.That(_garage.OpenSlotCount, Is.EqualTo(12));
            Assert.That(eventCount, Is.EqualTo(0));
        }

        [Test]
        public void RestoreCutsExperienceBelowNextLevel()
        {
            _playerLevel.Restore(1, FirstLevelExperience + 10, 1);

            Assert.That(_playerLevel.Experience, Is.EqualTo(FirstLevelExperience - 1));
        }

        [Test]
        public void RestoreRejectsBrokenValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _playerLevel.Restore(0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _playerLevel.Restore(2, -1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => _playerLevel.Restore(2, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _playerLevel.Restore(2, 0, 3));
        }

        [Test]
        public void NegativeExperienceIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _playerLevel.AddExperience(-1));
        }

        [Test]
        public void DisposedLevelIgnoresMerges()
        {
            PlaceVehicle(0, 1);
            PlaceVehicle(1, 1);
            _playerLevel.Dispose();

            _garage.Merge(0, 1);

            Assert.That(_playerLevel.Experience, Is.EqualTo(0));
        }

        private void ReachLevel(int level)
        {
            while (_playerLevel.Level < level)
            {
                _playerLevel.AddExperience(_playerLevel.ExperienceToNextLevel - _playerLevel.Experience);
            }
        }

        private void PlaceVehicle(int slot, int rank)
        {
            Vehicle vehicle = new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank));
            _garage.PlaceVehicle(slot, vehicle);
        }
    }
}
