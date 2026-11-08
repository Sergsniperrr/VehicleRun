using System.Collections.Generic;
using System.Linq;
using _Project.Core.Configs;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace _Project.Tests.EditMode.Configs
{
    public class GameConfigValidatorTests
    {
        private GameConfigFixture _fixture;
        private GameConfigValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _validator = new GameConfigValidator();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void ValidConfigHasNoErrors()
        {
            Assert.That(Validate(), Is.Empty);
        }

        [Test]
        public void MissingEconomyConfigIsReported()
        {
            _fixture.Change(_fixture.Config, "_economy", property => property.objectReferenceValue = null);

            AssertReportedBy(_fixture.Config);
        }

        [Test]
        public void MissingTurboConfigIsReported()
        {
            _fixture.Change(_fixture.Config, "_turbo", property => property.objectReferenceValue = null);

            AssertReportedBy(_fixture.Config);
        }

        [TestCase(0)]
        [TestCase(13)]
        public void TurboUnlockRankOutsideCatalogIsReported(int unlockRank)
        {
            _fixture.Change(_fixture.Turbo, "_unlockRank", property => property.intValue = unlockRank);

            AssertReportedBy(_fixture.Turbo);
        }

        [TestCase(1f)]
        [TestCase(0.5f)]
        public void TurboSpeedMultiplierNotAboveOneIsReported(float speedMultiplier)
        {
            _fixture.Change(_fixture.Turbo, "_speedMultiplier", property => property.floatValue = speedMultiplier);

            AssertReportedBy(_fixture.Turbo);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void TurboDurationNotAboveZeroIsReported(float durationSeconds)
        {
            _fixture.Change(_fixture.Turbo, "_freeDurationSeconds", property => property.floatValue = durationSeconds);

            AssertReportedBy(_fixture.Turbo);
        }

        [Test]
        public void NegativeTurboCooldownIsReported()
        {
            _fixture.Change(_fixture.Turbo, "_freeCooldownSeconds", property => property.floatValue = -1f);

            AssertReportedBy(_fixture.Turbo);
        }

        [Test]
        public void RankGapIsReported()
        {
            _fixture.Change(_fixture.Vehicles, "_vehicles.Array.data[3]._rank", property => property.intValue = 6);

            AssertReportedBy(_fixture.Vehicles);
        }

        [Test]
        public void DuplicateVehicleIdIsReported()
        {
            _fixture.Change(_fixture.Vehicles, "_vehicles.Array.data[2]._id", property => property.stringValue = "test_1");

            AssertReportedBy(_fixture.Vehicles);
        }

        [Test]
        public void DecreasingIncomeIsReported()
        {
            _fixture.Change(_fixture.Vehicles, "_vehicles.Array.data[4]._coinsPerLap", property => property.longValue = 3);

            AssertReportedBy(_fixture.Vehicles);
        }

        [Test]
        public void NonPositiveTrackSpeedIsReported()
        {
            _fixture.Change(_fixture.Vehicles, "_vehicles.Array.data[2]._trackSpeed", property => property.floatValue = 0);

            AssertReportedBy(_fixture.Vehicles);
        }

        [Test]
        public void MissingTrackSpriteIsReported()
        {
            _fixture.Change(_fixture.Vehicles, "_vehicles.Array.data[0]._trackSprite", property => property.objectReferenceValue = null);

            AssertReportedBy(_fixture.Vehicles);
        }

        [Test]
        public void NameKeyWithoutLocalizationIsReported()
        {
            _fixture.Change(_fixture.Vehicles, "_vehicles.Array.data[1]._nameKey", property => property.stringValue = "vehicle.unknown.name");

            AssertReportedBy(_fixture.Vehicles);
        }

        [Test]
        public void NameKeyWithEmptyTranslationIsReported()
        {
            _fixture.Change(_fixture.Localization, "_entries.Array.data[1]._english", property => property.stringValue = string.Empty);

            AssertReportedBy(_fixture.Vehicles);
        }

        [Test]
        public void NegativeBoxRankLagIsReported()
        {
            _fixture.Change(_fixture.Economy, "_boxRankLag", property => property.intValue = -1);

            AssertReportedBy(_fixture.Economy);
        }

        [Test]
        public void FirstRankPriceGrowthBelowOneIsReported()
        {
            _fixture.Change(_fixture.Economy, "_firstRankBoxPriceGrowth", property => property.doubleValue = 0.9);

            AssertReportedBy(_fixture.Economy);
        }

        [Test]
        public void StartVehiclesBeyondOpenSlotsAreReported()
        {
            _fixture.Change(_fixture.Economy, "_startVehicleCount", property => property.intValue = 7);

            AssertReportedBy(_fixture.Economy);
        }

        [Test]
        public void GarageExpansionAboveLastRankIsReported()
        {
            _fixture.Change(_fixture.Garage, "_openSlots._steps.Array.data[2]._rank", property => property.intValue = _fixture.Vehicles.MaxRank + 1);

            AssertReportedBy(_fixture.Garage);
        }

        [Test]
        public void GarageOpenSlotsBeyondGridAreReported()
        {
            _fixture.Change(_fixture.Garage, "_rows", property => property.intValue = 4);

            AssertReportedBy(_fixture.Garage);
        }

        [Test]
        public void UnorderedTrackExpansionsAreReported()
        {
            _fixture.Change(_fixture.Track, "_capacity._steps.Array.data[1]._rank", property => property.intValue = 3);

            AssertReportedBy(_fixture.Track);
        }

        [Test]
        public void NonGrowingTrackCapacityIsReported()
        {
            _fixture.Change(_fixture.Track, "_capacity._steps.Array.data[0]._capacity", property => property.intValue = 2);

            AssertReportedBy(_fixture.Track);
        }

        [Test]
        public void StartPointOutsideLapIsReported()
        {
            _fixture.Change(_fixture.Track, "_startDistance", property => property.floatValue = _fixture.Track.LapLength);

            AssertReportedBy(_fixture.Track);
        }

        private IReadOnlyList<ConfigError> Validate()
        {
            return _validator.Validate(_fixture.Config, _fixture.Localization);
        }

        private void AssertReportedBy(Object source)
        {
            IReadOnlyList<ConfigError> errors = Validate();

            Assert.That(errors.Any(error => error.Source == source), Is.True, string.Join("\n", errors));
        }
    }
}
