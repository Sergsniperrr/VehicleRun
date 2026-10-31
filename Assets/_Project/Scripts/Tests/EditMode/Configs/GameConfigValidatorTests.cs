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
            _fixture.Change(_fixture.Vehicles, "_vehicles.Array.data[4]._incomePerSecond", property => property.longValue = 3);

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
        public void StartVehiclesBeyondOpenSlotsAreReported()
        {
            _fixture.Change(_fixture.Economy, "_startVehicleCount", property => property.intValue = 7);

            AssertReportedBy(_fixture.Economy);
        }

        [Test]
        public void GarageExpansionAboveLastRankIsReported()
        {
            _fixture.Change(_fixture.Garage, "_openSlots._steps.Array.data[2]._rank", property => property.intValue = 9);

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
