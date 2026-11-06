using _Project.Core.Save;
using _Project.Features.Save;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Save
{
    public class ProgressLoaderTests
    {
        private const int BrokenSchemaVersion = 99;

        private GameConfigFixture _fixture;
        private SaveTestGame _source;
        private SaveTestGame _target;
        private FakeProgressStorage _storage;
        private ProgressLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _source = new SaveTestGame(_fixture);
            _target = new SaveTestGame(_fixture);
            _storage = new FakeProgressStorage();
            GameSaveValidator validator = new GameSaveValidator(_fixture.Vehicles, _fixture.Garage, _fixture.Track);
            _loader = new ProgressLoader(_storage, validator, _target.Restorer);
        }

        [TearDown]
        public void TearDown()
        {
            _source.Dispose();
            _target.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void ValidStateIsLoadedAndBackedUp()
        {
            GameSave state = CreateSave(3);
            _storage.State = state;
            _storage.Backup = CreateSave(2);

            Assert.That(_loader.Load(), Is.EqualTo(LoadOutcome.Loaded));
            Assert.That(_target.Progress.MaxRank, Is.EqualTo(3));
            Assert.That(_storage.Backup, Is.SameAs(state));
        }

        [Test]
        public void InvalidStateFallsBackToBackup()
        {
            GameSave backup = CreateSave(2);
            _storage.State = CreateSave(3);
            _storage.State.SchemaVersion = BrokenSchemaVersion;
            _storage.Backup = backup;

            Assert.That(_loader.Load(), Is.EqualTo(LoadOutcome.RestoredFromBackup));
            Assert.That(_target.Progress.MaxRank, Is.EqualTo(2));
            Assert.That(_storage.Backup, Is.SameAs(backup));
        }

        [Test]
        public void MissingStateFallsBackToBackup()
        {
            _storage.Backup = CreateSave(2);

            Assert.That(_loader.Load(), Is.EqualTo(LoadOutcome.RestoredFromBackup));
            Assert.That(_target.Progress.MaxRank, Is.EqualTo(2));
        }

        [Test]
        public void NothingIsLoadedWhenStateAndBackupAreInvalid()
        {
            _storage.State = CreateSave(3);
            _storage.State.SchemaVersion = BrokenSchemaVersion;
            _storage.Backup = CreateSave(2);
            _storage.Backup.Vehicles = null;

            Assert.That(_loader.Load(), Is.EqualTo(LoadOutcome.Broken));
            Assert.That(_target.Progress.MaxRank, Is.EqualTo(0));
            Assert.That(_target.Garage.IsSlotEmpty(0), Is.True);
            Assert.That(_storage.BackupWriteCount, Is.EqualTo(0));
        }

        [Test]
        public void InvalidStateWithoutBackupIsBroken()
        {
            _storage.State = CreateSave(3);
            _storage.State.SchemaVersion = BrokenSchemaVersion;

            Assert.That(_loader.Load(), Is.EqualTo(LoadOutcome.Broken));
            Assert.That(_target.Progress.MaxRank, Is.EqualTo(0));
        }

        [Test]
        public void NothingIsLoadedFromEmptyStorage()
        {
            Assert.That(_loader.Load(), Is.EqualTo(LoadOutcome.NotFound));
            Assert.That(_storage.BackupWriteCount, Is.EqualTo(0));
            Assert.That(_storage.StateWriteCount, Is.EqualTo(0));
        }

        private GameSave CreateSave(int rank)
        {
            using (SaveTestGame game = new SaveTestGame(_fixture))
            {
                game.PlaceVehicle(0, rank);
                game.PlaceVehicle(1, 1);

                return game.Factory.Create();
            }
        }
    }
}
