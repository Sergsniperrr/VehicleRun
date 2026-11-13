using _Project.Core.Save;
using _Project.Features.Boxes;
using _Project.Features.Progression;
using _Project.Features.Save;
using _Project.Features.Track;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.EditMode.Save
{
    public class GameSaveRoundTripTests
    {
        private const int SourceLevel = 5;
        private const long SourceExperience = 7;
        private const int LegacyLevel = 0;
        private const int LegacyFittingLevel = 4;
        private const float DrivingSeconds = 13.7f;
        private const double BonusCoins = 2000.1;
        private const float PartialDeliverySeconds = 7.5f;
        private const float PartialTurboSeconds = 12.5f;
        private const float PartialCooldownSeconds = 40f;
        private const double AlmostWholeRemainder = 0.99999999999999989;
        private const float OversizedTimerSeconds = 1000f;
        private const float DistanceTolerance = 0.001f;
        private const double RemainderTolerance = 1e-12;

        private GameConfigFixture _fixture;
        private SaveTestGame _source;
        private SaveTestGame _target;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _source = new SaveTestGame(_fixture);
            _target = new SaveTestGame(_fixture);
        }

        [TearDown]
        public void TearDown()
        {
            _source.Dispose();
            _target.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void RestoredGameSavesTheSameState()
        {
            PlaySourceGame();
            GameSave saved = _source.Factory.Create();

            _target.Restorer.Restore(PassThroughJson(saved));

            GameSave restored = _target.Factory.Create();
            Assert.That(restored.CoinsRemainder, Is.EqualTo(saved.CoinsRemainder).Within(RemainderTolerance));
            saved.CoinsRemainder = 0;
            restored.CoinsRemainder = 0;
            Assert.That(JsonUtility.ToJson(restored), Is.EqualTo(JsonUtility.ToJson(saved)));
        }

        [Test]
        public void JsonKeepsClaimedMilestones()
        {
            _source.PlaceVehicle(0, 4);
            _source.Milestones.Claim(4);

            _target.Restorer.Restore(PassThroughJson(_source.Factory.Create()));

            Assert.That(_target.Milestones.ClaimedRanks, Does.Contain(4));
            Assert.That(_target.Milestones.GetState(4), Is.EqualTo(MilestoneState.Claimed));
        }

        [Test]
        public void JsonKeepsCoinsAndExactDistances()
        {
            PlaySourceGame();

            _target.Restorer.Restore(PassThroughJson(_source.Factory.Create()));

            Assert.That(_target.Wallet.Coins, Is.EqualTo(_source.Wallet.Coins));
            Assert.That(_target.Wallet.Remainder, Is.EqualTo(_source.Wallet.Remainder).Within(RemainderTolerance));
            Assert.That(_target.Roster.Count, Is.EqualTo(_source.Roster.Count));

            for (int i = 0; i < _source.Roster.Count; i++)
            {
                Assert.That(_target.Roster.Runners[i].Distance, Is.EqualTo(_source.Roster.Runners[i].Distance));
            }
        }

        [Test]
        public void JsonKeepsLargestCoinsAndIds()
        {
            GameSave save = new GameSave
            {
                Coins = long.MaxValue,
                NextInstanceId = long.MaxValue,
            };

            GameSave restored = PassThroughJson(save);

            Assert.That(restored.Coins, Is.EqualTo(long.MaxValue));
            Assert.That(restored.NextInstanceId, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void RemainderRoundedToOneByJsonBecomesCoin()
        {
            PlaySourceGame();
            GameSave save = _source.Factory.Create();
            save.CoinsRemainder = AlmostWholeRemainder;

            GameSave restored = PassThroughJson(save);
            GameSaveValidator validator = new GameSaveValidator(_fixture.Vehicles, _fixture.Garage, _fixture.Track, new SaveLevelResolver(_fixture.Garage, _fixture.Track));

            Assert.That(validator.IsValid(restored), Is.True);

            _target.Restorer.Restore(restored);

            Assert.That(_target.Wallet.Coins, Is.EqualTo(save.Coins + 1));
            Assert.That(_target.Wallet.Remainder, Is.EqualTo(0));
        }

        [Test]
        public void RestoreRaisesNoEvents()
        {
            PlaySourceGame();
            int eventCount = 0;
            _target.Garage.VehiclePlaced += (slot, vehicle) => eventCount++;
            _target.Garage.BoxPlaced += (slot, box) => eventCount++;
            _target.Progress.RankUnlocked += rank => eventCount++;
            _target.Level.TrackPlacesUnlocked += capacity => eventCount++;
            _target.Level.LevelReached += level => eventCount++;
            _target.Level.ExperienceChanged += () => eventCount++;
            _target.Roster.VehicleSent += runner => eventCount++;
            _target.Wallet.CoinsChanged += coins => eventCount++;

            _target.Restorer.Restore(_source.Factory.Create());

            Assert.That(eventCount, Is.EqualTo(0));
        }

        [Test]
        public void RestoredProgressOpensSlotsAndTrackPlaces()
        {
            PlaySourceGame();

            _target.Restorer.Restore(_source.Factory.Create());

            Assert.That(_target.Progress.MaxRank, Is.EqualTo(5));
            Assert.That(_target.Progress.IsRankUnlocked(3), Is.True);
            Assert.That(_target.Progress.IsRankUnlocked(2), Is.False);
            Assert.That(_target.Garage.OpenSlotCount, Is.EqualTo(_source.Garage.OpenSlotCount));
            Assert.That(_target.Roster.Capacity, Is.EqualTo(_source.Roster.Capacity));
            Assert.That(_target.Progress.ShopMaxRank, Is.EqualTo(_source.Progress.ShopMaxRank));
        }

        [Test]
        public void RestoredRunnersKeepOrderAndSlots()
        {
            PlaySourceGame();

            _target.Restorer.Restore(_source.Factory.Create());

            int[] slots = new int[_target.Roster.Count];

            for (int i = 0; i < slots.Length; i++)
            {
                TrackRunner runner = _target.Roster.Runners[i];
                slots[i] = runner.Slot;
                Assert.That(_target.Garage.IsOnTrack(runner.Slot), Is.True);
            }

            Assert.That(slots, Is.EqualTo(new[] { 2, 0, 3 }));
            Assert.That(_target.Garage.IsOnTrack(1), Is.False);
        }

        [Test]
        public void RestoredGameContinuesInstanceIds()
        {
            PlaySourceGame();
            long nextId = _source.InstanceIds.NextId;

            _target.Restorer.Restore(_source.Factory.Create());

            Assert.That(_target.InstanceIds.Create(), Is.EqualTo(nextId));
        }

        [Test]
        public void RestoredPricingAndDeliveryContinue()
        {
            PlaySourceGame();

            _target.Restorer.Restore(_source.Factory.Create());

            Assert.That(_target.Pricing.GetPurchaseCount(1), Is.EqualTo(2));
            Assert.That(_target.Delivery.BoxPrice, Is.EqualTo(_source.Delivery.BoxPrice));
            Assert.That(_target.Delivery.FreeBoxRemainingSeconds, Is.EqualTo(_source.Delivery.FreeBoxRemainingSeconds));
            Assert.That(_target.Delivery.HasReadyDelivery, Is.False);
        }

        [Test]
        public void RestoredReadyDeliveryWaitsForFreeSlot()
        {
            for (int slot = 0; slot < _source.Garage.OpenSlotCount; slot++)
            {
                _source.PlaceVehicle(slot, 1);
            }

            _source.Delivery.Advance(_fixture.Economy.FreeBoxIntervalSeconds);
            _target.Restorer.Restore(_source.Factory.Create());

            Assert.That(_target.Delivery.HasReadyDelivery, Is.True);

            _target.Garage.Recycle(5);
            _target.Delivery.Advance(0);

            Assert.That(_target.Delivery.HasReadyDelivery, Is.False);
            Assert.That(_target.Garage.TryGetBox(5, out Box box) && box.Source == BoxSource.Free, Is.True);
        }

        [Test]
        public void RestoredTurboContinuesWithoutEvents()
        {
            PlaySourceGame();
            int eventCount = 0;
            _target.Turbo.TurboStarted += () => eventCount++;
            _target.Turbo.TurboEnded += () => eventCount++;

            _target.Restorer.Restore(PassThroughJson(_source.Factory.Create()));

            Assert.That(_target.Turbo.RemainingSeconds, Is.EqualTo(_source.Turbo.RemainingSeconds));
            Assert.That(_target.Turbo.SpeedMultiplier, Is.EqualTo(_fixture.Turbo.SpeedMultiplier));
            Assert.That(eventCount, Is.EqualTo(0));
        }

        [Test]
        public void RestoredTurboCooldownContinues()
        {
            PlaySourceGame();
            _source.Turbo.Advance(_source.Turbo.RemainingSeconds + PartialCooldownSeconds);

            _target.Restorer.Restore(PassThroughJson(_source.Factory.Create()));

            Assert.That(_target.Turbo.IsActive, Is.False);
            Assert.That(_target.Turbo.CooldownRemainingSeconds, Is.EqualTo(_source.Turbo.CooldownRemainingSeconds));
            Assert.That(_target.Turbo.CanStartTurbo(), Is.False);
        }

        [Test]
        public void RestoreWrapsDistanceAndLimitsTimer()
        {
            PlaySourceGame();
            GameSave save = _source.Factory.Create();
            float distance = save.TrackRunners[0].Distance;
            save.TrackRunners[0].Distance += _fixture.Track.LapLength;
            save.FreeBoxRemainingSeconds = OversizedTimerSeconds;
            save.TurboRemainingSeconds = OversizedTimerSeconds;
            save.TurboCooldownRemainingSeconds = OversizedTimerSeconds;

            _target.Restorer.Restore(save);

            Assert.That(_target.Roster.Runners[0].Distance, Is.EqualTo(distance).Within(DistanceTolerance));
            Assert.That(_target.Delivery.FreeBoxRemainingSeconds, Is.EqualTo(_fixture.Economy.FreeBoxIntervalSeconds));
            Assert.That(_target.Turbo.RemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeDurationSeconds));
            Assert.That(_target.Turbo.CooldownRemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeCooldownSeconds));
        }

        [Test]
        public void PlayerLevelSurvivesJson()
        {
            PlaySourceGame();
            _source.Level.AddExperience(_source.Level.ExperienceToNextLevel + SourceExperience);

            _target.Restorer.Restore(PassThroughJson(_source.Factory.Create()));

            Assert.That(_target.Level.Level, Is.EqualTo(SourceLevel + 1));
            Assert.That(_target.Level.Experience, Is.EqualTo(SourceExperience));
            Assert.That(_target.Level.HasPendingReward, Is.True);
            Assert.That(_target.Level.PendingRewardLevel, Is.EqualTo(SourceLevel + 1));
        }

        [Test]
        public void ClaimedRewardIsNotOfferedAfterRestore()
        {
            PlaySourceGame();
            _source.Level.AddExperience(_source.Level.ExperienceToNextLevel);
            _source.Level.ClaimReward();

            _target.Restorer.Restore(PassThroughJson(_source.Factory.Create()));

            Assert.That(_target.Level.HasPendingReward, Is.False);
        }

        [Test]
        public void SaveWithoutLevelGetsLevelThatFitsItsSlotsAndRunners()
        {
            PlaySourceGame();
            GameSave save = _source.Factory.Create();
            save.PlayerLevel = LegacyLevel;
            save.PlayerExperience = 0;
            save.ClaimedRewardLevel = 0;

            GameSave restored = PassThroughJson(save);
            GameSaveValidator validator = new GameSaveValidator(_fixture.Vehicles, _fixture.Garage, _fixture.Track, _target.Levels);

            Assert.That(validator.IsValid(restored), Is.True);

            _target.Restorer.Restore(restored);

            Assert.That(_target.Level.Level, Is.EqualTo(LegacyFittingLevel));
            Assert.That(_target.Level.Experience, Is.EqualTo(0));
            Assert.That(_target.Level.HasPendingReward, Is.False);
            Assert.That(_target.Garage.TryGetVehicle(6, out Vehicle lastVehicle), Is.True);
            Assert.That(_target.Roster.Count, Is.EqualTo(_source.Roster.Count));
        }

        private GameSave PassThroughJson(GameSave save)
        {
            return JsonUtility.FromJson<GameSave>(JsonUtility.ToJson(save));
        }

        private void PlaySourceGame()
        {
            _source.ReachLevel(SourceLevel);
            _source.PlaceVehicle(0, 5);
            _source.PlaceVehicle(1, 4);
            _source.PlaceVehicle(2, 4);
            _source.PlaceVehicle(3, 3);
            _source.PlaceVehicle(4, 1);
            _source.PlaceBox(5, 1, BoxSource.Tutorial);
            _source.Roster.SendToTrack(2);
            _source.Roster.SendToTrack(0);
            _source.Roster.SendToTrack(3);
            _source.Roster.Advance(DrivingSeconds);
            _source.Wallet.Add(BonusCoins);
            _source.Delivery.BuyBox();
            _source.Delivery.BuyBox();
            _source.Garage.OpenBox(6);
            _source.Delivery.Advance(PartialDeliverySeconds);
            _source.Turbo.StartTurbo();
            _source.Turbo.Advance(PartialTurboSeconds);
        }
    }
}
