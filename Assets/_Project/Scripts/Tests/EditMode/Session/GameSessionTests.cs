using System;
using _Project.Core.Save;
using _Project.Features.Save;
using _Project.Features.Session;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Session
{
    public class GameSessionTests
    {
        private const float FrameSeconds = 0.02f;
        private const int SavedRank = 3;
        private const int SavedSlot = 4;

        private GameConfigFixture _fixture;
        private SaveTestGame _source;
        private SaveTestGame _game;
        private FakeProgressStorage _storage;
        private SaveScheduler _scheduler;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _source = new SaveTestGame(_fixture);
            _game = new SaveTestGame(_fixture);
            _storage = new FakeProgressStorage();
            FakeTimeService time = new FakeTimeService { DeltaTime = FrameSeconds };
            GameSaveValidator validator = new GameSaveValidator(_fixture.Vehicles, _fixture.Garage, _fixture.Track);
            ProgressLoader loader = new ProgressLoader(_storage, validator, _game.Restorer);
            _scheduler = new SaveScheduler(_storage, _game.Factory, _game.Garage, _game.Roster, _game.Turbo, _game.Milestones, time);
            NewGameSetup newGameSetup = new NewGameSetup(_fixture.Economy, _fixture.Vehicles, _game.Garage, _game.Progress, _game.InstanceIds);
            _session = new GameSession(loader, _scheduler, newGameSetup);
        }

        [TearDown]
        public void TearDown()
        {
            _scheduler.Dispose();
            _source.Dispose();
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void NewGameStartsWithoutSaveAndIsWritten()
        {
            _session.Open();
            _scheduler.LateTick();

            Assert.That(_session.IsOpen, Is.True);
            Assert.That(_session.LoadOutcome, Is.EqualTo(LoadOutcome.NotFound));
            Assert.That(_game.Garage.TryGetVehicle(0, out Vehicle vehicle), Is.True);
            Assert.That(vehicle.Rank, Is.EqualTo(_fixture.Economy.StartVehicleRank));
            Assert.That(_storage.StateWriteCount, Is.EqualTo(1));
            Assert.That(_storage.IsLastStateWriteImportant, Is.True);
            Assert.That(_storage.State.Vehicles.Length, Is.EqualTo(_fixture.Economy.StartVehicleCount));
        }

        [Test]
        public void SavedGameIsLoadedWithoutStartVehicles()
        {
            Vehicle saved = _source.PlaceVehicle(SavedSlot, SavedRank);
            _storage.State = _source.Factory.Create();

            _session.Open();
            _scheduler.LateTick();

            Assert.That(_session.LoadOutcome, Is.EqualTo(LoadOutcome.Loaded));
            Assert.That(_game.Progress.MaxRank, Is.EqualTo(SavedRank));
            Assert.That(_game.Garage.TryGetVehicle(SavedSlot, out Vehicle loaded) && loaded.InstanceId == saved.InstanceId, Is.True);
            Assert.That(_game.Garage.IsSlotEmpty(0), Is.True);
            Assert.That(_storage.StateWriteCount, Is.EqualTo(0));
        }

        [Test]
        public void BrokenSaveStartsNewGame()
        {
            _source.PlaceVehicle(SavedSlot, SavedRank);
            GameSave broken = _source.Factory.Create();
            broken.Vehicles = null;
            _storage.State = broken;

            _session.Open();

            Assert.That(_session.LoadOutcome, Is.EqualTo(LoadOutcome.Broken));
            Assert.That(_game.Progress.MaxRank, Is.EqualTo(_fixture.Economy.StartVehicleRank));
            Assert.That(_game.Garage.TryGetVehicle(0, out Vehicle vehicle), Is.True);
        }

        [Test]
        public void BackupIsLoadedWithoutStartVehicles()
        {
            _source.PlaceVehicle(SavedSlot, SavedRank);
            _storage.Backup = _source.Factory.Create();

            _session.Open();

            Assert.That(_session.LoadOutcome, Is.EqualTo(LoadOutcome.RestoredFromBackup));
            Assert.That(_session.IsNewGame, Is.False);
            Assert.That(_game.Garage.IsSlotEmpty(0), Is.True);
        }

        [Test]
        public void SessionOpensOnce()
        {
            _session.Open();

            Assert.Throws<InvalidOperationException>(() => _session.Open());
        }
    }
}
