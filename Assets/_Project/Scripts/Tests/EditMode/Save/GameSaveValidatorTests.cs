using System;
using _Project.Core.Save;
using _Project.Features.Boxes;
using _Project.Features.Save;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Save
{
    public class GameSaveValidatorTests
    {
        private const string UnknownVehicleId = "unknown";
        private const int UnknownBoxSource = 99;
        private const int ClosedSlot = 14;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private GameSaveValidator _validator;
        private long _boxInstanceId;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
            _validator = new GameSaveValidator(_fixture.Vehicles, _fixture.Garage, _fixture.Track);

            _game.PlaceVehicle(0, 5);
            _game.PlaceVehicle(1, 4);
            _game.PlaceVehicle(2, 4);
            _game.PlaceVehicle(3, 1);
            _boxInstanceId = _game.PlaceBox(4, 1, BoxSource.Free).InstanceId;
            _game.Roster.SendToTrack(0);
            _game.Roster.SendToTrack(1);
            _game.Roster.SendToTrack(2);
        }

        [TearDown]
        public void TearDown()
        {
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void SavedGameIsValid()
        {
            Assert.That(_validator.IsValid(_game.Factory.Create()), Is.True);
        }

        [Test]
        public void ClaimedMilestonesAreValid()
        {
            _game.Milestones.Claim(4);

            Assert.That(_validator.IsValid(_game.Factory.Create()), Is.True);
        }

        [Test]
        public void BrokenMilestonesAreInvalid()
        {
            AssertInvalid(save => save.ClaimedMilestoneRanks = null);
            AssertInvalid(save => save.ClaimedMilestoneRanks = new[] { 4, 4 });
            AssertInvalid(save => save.ClaimedMilestoneRanks = new[] { 0 });
            AssertInvalid(save => save.ClaimedMilestoneRanks = new[] { _fixture.Vehicles.MaxRank + 1 });
        }

        [Test]
        public void MissingSaveOrSchemaIsInvalid()
        {
            Assert.That(_validator.IsValid(null), Is.False);
            AssertInvalid(save => save.SchemaVersion = GameSave.CurrentSchemaVersion + 1);
            AssertInvalid(save => save.SchemaVersion = 0);
        }

        [Test]
        public void MissingCollectionIsInvalid()
        {
            AssertInvalid(save => save.UnlockedRanks = null);
            AssertInvalid(save => save.BoxPurchaseCounts = null);
            AssertInvalid(save => save.Vehicles = null);
            AssertInvalid(save => save.Boxes = null);
            AssertInvalid(save => save.TrackRunners = null);
        }

        [Test]
        public void BrokenCoinsAreInvalid()
        {
            AssertInvalid(save => save.Coins = -1);
            AssertInvalid(save => save.CoinsRemainder = 1.5);
            AssertInvalid(save => save.CoinsRemainder = -0.1);
            AssertInvalid(save => save.CoinsRemainder = double.NaN);
        }

        [Test]
        public void BrokenTimerIsInvalid()
        {
            AssertInvalid(save => save.FreeBoxRemainingSeconds = -1);
            AssertInvalid(save => save.FreeBoxRemainingSeconds = float.NaN);
            AssertInvalid(save => save.FreeBoxRemainingSeconds = float.PositiveInfinity);
        }

        [Test]
        public void BrokenTurboTimersAreInvalid()
        {
            AssertInvalid(save => save.TurboRemainingSeconds = -1);
            AssertInvalid(save => save.TurboRemainingSeconds = float.NaN);
            AssertInvalid(save => save.TurboRemainingSeconds = float.PositiveInfinity);
            AssertInvalid(save => save.TurboCooldownRemainingSeconds = -1);
            AssertInvalid(save => save.TurboCooldownRemainingSeconds = float.NaN);
            AssertInvalid(save => save.TurboCooldownRemainingSeconds = float.PositiveInfinity);
        }

        [Test]
        public void BrokenRanksAreInvalid()
        {
            AssertInvalid(save => save.UnlockedRanks = Array.Empty<int>());
            AssertInvalid(save => save.UnlockedRanks = new[] { 0, 1, 4, 5 });
            AssertInvalid(save => save.UnlockedRanks = new[] { 1, 4, 5, _fixture.Vehicles.MaxRank + 1 });
            AssertInvalid(save => save.UnlockedRanks = new[] { 1, 4, 4, 5 });
        }

        [Test]
        public void BrokenPurchaseCountsAreInvalid()
        {
            AssertInvalid(save => save.BoxPurchaseCounts = new int[_fixture.Vehicles.MaxRank + 1]);
            AssertInvalid(save => save.BoxPurchaseCounts[0] = -1);
        }

        [Test]
        public void UnknownVehicleIsInvalid()
        {
            AssertInvalid(save => save.Vehicles[0].VehicleId = UnknownVehicleId);
            AssertInvalid(save => save.Vehicles[0].VehicleId = null);
            AssertInvalid(save => save.Boxes[0].VehicleId = UnknownVehicleId);
        }

        [Test]
        public void VehicleOfLockedRankIsInvalid()
        {
            AssertInvalid(save => save.UnlockedRanks = new[] { 1, 5 });
        }

        [Test]
        public void BoxAboveTopRankIsInvalid()
        {
            AssertInvalid(save => save.Boxes[0].VehicleId = _fixture.Vehicles.GetByRank(6).Id);
        }

        [Test]
        public void UnknownBoxSourceIsInvalid()
        {
            AssertInvalid(save => save.Boxes[0].Source = UnknownBoxSource);
            AssertInvalid(save => save.Boxes[0].Source = -1);
        }

        [Test]
        public void BrokenInstanceIdsAreInvalid()
        {
            AssertInvalid(save => save.Boxes[0].InstanceId = save.Vehicles[0].InstanceId);
            AssertInvalid(save => save.Vehicles[1].InstanceId = save.Vehicles[0].InstanceId);
            AssertInvalid(save => save.Vehicles[0].InstanceId = 0);
            AssertInvalid(save => save.NextInstanceId = _boxInstanceId);
            AssertInvalid(save => save.NextInstanceId = 0);
        }

        [Test]
        public void BrokenSlotsAreInvalid()
        {
            AssertInvalid(save => save.Vehicles[0].Slot = -1);
            AssertInvalid(save => save.Vehicles[0].Slot = ClosedSlot);
            AssertInvalid(save => save.Boxes[0].Slot = save.Vehicles[0].Slot);
            AssertInvalid(save => save.Vehicles[1].Slot = save.Vehicles[0].Slot);
        }

        [Test]
        public void BrokenTrackRunnersAreInvalid()
        {
            AssertInvalid(save => save.TrackRunners[0].VehicleInstanceId = _boxInstanceId);
            AssertInvalid(save => save.TrackRunners[1].VehicleInstanceId = save.TrackRunners[0].VehicleInstanceId);
            AssertInvalid(save => save.TrackRunners[0].Distance = -1);
            AssertInvalid(save => save.TrackRunners[0].Distance = float.NaN);
        }

        [Test]
        public void MoreRunnersThanTrackPlacesIsInvalid()
        {
            GameSave save = _game.Factory.Create();
            string thirdRankId = _fixture.Vehicles.GetByRank(3).Id;
            save.UnlockedRanks = new[] { 1, 3 };
            save.Vehicles[0].VehicleId = thirdRankId;
            save.Vehicles[1].VehicleId = thirdRankId;
            save.Vehicles[2].VehicleId = thirdRankId;

            Assert.That(_validator.IsValid(save), Is.False);

            save.TrackRunners = new[] { save.TrackRunners[0], save.TrackRunners[1] };

            Assert.That(_validator.IsValid(save), Is.True);
        }

        [Test]
        public void ChangedConfigVersionIsValid()
        {
            GameSave save = _game.Factory.Create();
            save.ConfigVersion++;

            Assert.That(_validator.IsValid(save), Is.True);
        }

        private void AssertInvalid(Action<GameSave> breakSave)
        {
            GameSave save = _game.Factory.Create();
            breakSave(save);

            Assert.That(_validator.IsValid(save), Is.False);
        }
    }
}
