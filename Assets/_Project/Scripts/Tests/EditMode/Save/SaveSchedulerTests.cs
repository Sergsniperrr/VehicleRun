using _Project.Features.Boxes;
using _Project.Features.Save;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.EditMode.Save
{
    public class SaveSchedulerTests
    {
        private const float FrameSeconds = 0.02f;
        private const float LongFrameSeconds = 0.4f;
        private const int FramesBeforePeriodicWrite = 2;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private FakeProgressStorage _storage;
        private FakeTimeService _time;
        private SaveScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
            _storage = new FakeProgressStorage();
            _time = new FakeTimeService { DeltaTime = FrameSeconds };
            _scheduler = new SaveScheduler(_storage, _game.Factory, _game.Garage, _game.Roster, _game.Turbo, _game.Milestones, _game.Level, _time);
        }

        [TearDown]
        public void TearDown()
        {
            _scheduler.Dispose();
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void NothingIsWrittenBeforeStart()
        {
            _game.PlaceVehicle(0, 1);

            _scheduler.LateTick();

            Assert.That(_storage.StateWriteCount, Is.EqualTo(0));
        }

        [Test]
        public void CommandsOfOneFrameAreWrittenOnceAsImportant()
        {
            _scheduler.StartWriting();
            _game.PlaceVehicle(0, 2);
            _game.PlaceVehicle(1, 1);
            _game.Roster.SendToTrack(0);

            _scheduler.LateTick();
            _scheduler.LateTick();

            Assert.That(_storage.StateWriteCount, Is.EqualTo(1));
            Assert.That(_storage.IsLastStateWriteImportant, Is.True);
            Assert.That(JsonUtility.ToJson(_storage.State), Is.EqualTo(JsonUtility.ToJson(_game.Factory.Create())));
        }

        [Test]
        public void EveryCommandKindIsWritten()
        {
            _scheduler.StartWriting();
            _game.PlaceVehicle(0, 1);
            AssertWrittenAfterTick(1);

            _game.PlaceVehicle(1, 1);
            AssertWrittenAfterTick(2);

            _game.Garage.Merge(0, 1);
            AssertWrittenAfterTick(3);

            _game.Garage.Move(1, 2);
            AssertWrittenAfterTick(4);

            _game.Roster.SendToTrack(2);
            AssertWrittenAfterTick(5);

            _game.Roster.ReturnFromTrack(2);
            AssertWrittenAfterTick(6);

            _game.PlaceBox(3, 1, BoxSource.Free);
            AssertWrittenAfterTick(7);

            _game.Garage.OpenBox(3);
            AssertWrittenAfterTick(8);

            _game.Garage.Recycle(3);
            AssertWrittenAfterTick(9);
        }

        [Test]
        public void ClaimedLevelRewardIsWrittenAsImportant()
        {
            _scheduler.StartWriting();
            _game.Level.AddExperience(_game.Level.ExperienceToNextLevel);

            _game.Level.ClaimReward();

            AssertWrittenAfterTick(1);
            Assert.That(_storage.State.ClaimedRewardLevel, Is.EqualTo(_game.Level.Level));
        }

        [Test]
        public void TurboStartIsWrittenAsImportant()
        {
            _scheduler.StartWriting();
            _game.PlaceVehicle(0, _fixture.Turbo.UnlockRank);
            _game.Roster.SendToTrack(0);
            AssertWrittenAfterTick(1);

            _game.Turbo.StartTurbo();

            AssertWrittenAfterTick(2);
            Assert.That(_storage.State.TurboRemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeDurationSeconds));
        }

        [Test]
        public void StateIsWrittenPeriodicallyWithoutCommands()
        {
            _scheduler.StartWriting();
            _time.DeltaTime = LongFrameSeconds;

            for (int i = 0; i < FramesBeforePeriodicWrite; i++)
            {
                _scheduler.LateTick();
            }

            Assert.That(_storage.StateWriteCount, Is.EqualTo(0));

            _scheduler.LateTick();

            Assert.That(_storage.StateWriteCount, Is.EqualTo(1));
            Assert.That(_storage.IsLastStateWriteImportant, Is.False);
        }

        [Test]
        public void NothingIsWrittenAfterDispose()
        {
            _scheduler.StartWriting();
            _scheduler.Dispose();
            _game.PlaceVehicle(0, 1);

            _scheduler.LateTick();

            Assert.That(_storage.StateWriteCount, Is.EqualTo(0));
        }

        private void AssertWrittenAfterTick(int expectedWriteCount)
        {
            _scheduler.LateTick();

            Assert.That(_storage.StateWriteCount, Is.EqualTo(expectedWriteCount));
            Assert.That(_storage.IsLastStateWriteImportant, Is.True);
        }
    }
}
