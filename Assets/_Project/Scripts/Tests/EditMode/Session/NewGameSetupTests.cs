using _Project.Features.Session;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Session
{
    public class NewGameSetupTests
    {
        private const string StartVehicleRankPath = "_startVehicleRank";
        private const string StartVehicleCountPath = "_startVehicleCount";
        private const int ChangedStartRank = 3;

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
        public void StartVehiclesTakeFirstSlots()
        {
            CreateSetup().Apply();

            for (int slot = 0; slot < _fixture.Economy.StartVehicleCount; slot++)
            {
                Assert.That(_game.Garage.TryGetVehicle(slot, out Vehicle vehicle), Is.True);
                Assert.That(vehicle.Rank, Is.EqualTo(_fixture.Economy.StartVehicleRank));
            }

            Assert.That(_game.Garage.IsSlotEmpty(_fixture.Economy.StartVehicleCount), Is.True);
            Assert.That(_game.Roster.Count, Is.EqualTo(0));
        }

        [Test]
        public void StartVehiclesGetNewIds()
        {
            CreateSetup().Apply();

            _game.Garage.TryGetVehicle(0, out Vehicle first);
            _game.Garage.TryGetVehicle(1, out Vehicle second);

            Assert.That(first.InstanceId, Is.Not.EqualTo(second.InstanceId));
            Assert.That(_game.InstanceIds.NextId, Is.EqualTo(second.InstanceId + 1));
        }

        [Test]
        public void StartRankOpensOnce()
        {
            int unlockedCount = 0;
            _game.Progress.RankUnlocked += rank => unlockedCount++;

            CreateSetup().Apply();

            Assert.That(_game.Progress.MaxRank, Is.EqualTo(_fixture.Economy.StartVehicleRank));
            Assert.That(unlockedCount, Is.EqualTo(1));
        }

        [Test]
        public void StartRankComesFromConfig()
        {
            _fixture.Change(_fixture.Economy, StartVehicleRankPath, property => property.intValue = ChangedStartRank);

            CreateSetup().Apply();

            Assert.That(_game.Garage.TryGetVehicle(0, out Vehicle vehicle) && vehicle.Rank == ChangedStartRank, Is.True);
            Assert.That(_game.Progress.MaxRank, Is.EqualTo(ChangedStartRank));
            Assert.That(_game.Garage.OpenSlotCount, Is.EqualTo(_fixture.Garage.OpenSlots.GetCapacity(ChangedStartRank)));
        }

        [Test]
        public void StartRankOpensWithoutStartVehicles()
        {
            _fixture.Change(_fixture.Economy, StartVehicleCountPath, property => property.intValue = 0);

            CreateSetup().Apply();

            Assert.That(_game.Progress.MaxRank, Is.EqualTo(_fixture.Economy.StartVehicleRank));
            Assert.That(_game.Garage.IsSlotEmpty(0), Is.True);
        }

        private NewGameSetup CreateSetup()
        {
            return new NewGameSetup(_fixture.Economy, _fixture.Vehicles, _game.Garage, _game.Progress, _game.InstanceIds);
        }
    }
}
