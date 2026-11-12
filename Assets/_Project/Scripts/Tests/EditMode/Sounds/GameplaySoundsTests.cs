using _Project.Core.Audio;
using _Project.Features.Boxes;
using _Project.Features.Sounds;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _Project.Tests.EditMode.Sounds
{
    public class GameplaySoundsTests
    {
        private const float MoneyMinIntervalSeconds = 0.5f;
        private const int MilestoneRank = 4;
        private const float LapMargin = 1.01f;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private FakeAudioService _audio;
        private FakeTimeService _time;
        private GameSoundsConfig _config;
        private AudioAsset _merge;
        private AudioAsset _money;
        private AudioAsset _boxOpen;
        private GameplaySounds _sounds;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
            _audio = new FakeAudioService();
            _time = new FakeTimeService();
            _merge = ScriptableObject.CreateInstance<AudioAsset>();
            _money = ScriptableObject.CreateInstance<AudioAsset>();
            _boxOpen = ScriptableObject.CreateInstance<AudioAsset>();
            _config = ScriptableObject.CreateInstance<GameSoundsConfig>();

            SerializedObject serializedConfig = new SerializedObject(_config);
            serializedConfig.FindProperty("_merge").objectReferenceValue = _merge;
            serializedConfig.FindProperty("_money").objectReferenceValue = _money;
            serializedConfig.FindProperty("_boxOpen").objectReferenceValue = _boxOpen;
            serializedConfig.FindProperty("_moneyMinIntervalSeconds").floatValue = MoneyMinIntervalSeconds;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            _sounds = new GameplaySounds(_audio, _config, _time, _game.Garage, _game.Roster, _game.Milestones);
            _sounds.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _sounds.Dispose();
            _game.Dispose();
            _fixture.Dispose();
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_merge);
            Object.DestroyImmediate(_money);
            Object.DestroyImmediate(_boxOpen);
        }

        [Test]
        public void MergePlaysMergeSound()
        {
            _game.PlaceVehicle(0, 1);
            _game.PlaceVehicle(1, 1);

            _game.Garage.Merge(0, 1);

            Assert.That(_audio.Played, Is.EqualTo(new[] { _merge }));
        }

        [Test]
        public void OpenBoxPlaysBoxOpenSound()
        {
            _game.PlaceBox(0, 1, BoxSource.Free);

            _game.Garage.OpenBox(0);

            Assert.That(_audio.Played, Is.EqualTo(new[] { _boxOpen }));
        }

        [Test]
        public void FailedMergeIsSilent()
        {
            _game.PlaceVehicle(0, 1);
            _game.PlaceVehicle(1, 2);

            _game.Garage.Merge(0, 1);

            Assert.That(_audio.Played, Is.Empty);
        }

        [Test]
        public void FinishPayoutPlaysMoneySound()
        {
            SendVehicleToTrack();

            CrossFinish();

            Assert.That(_audio.Played, Is.EqualTo(new[] { _money }));
        }

        [Test]
        public void PayoutsInsideIntervalPlayMoneySoundOnce()
        {
            SendVehicleToTrack();

            CrossFinish();
            _time.Time += MoneyMinIntervalSeconds / 2f;
            CrossFinish();

            Assert.That(_audio.Played.Count, Is.EqualTo(1));
        }

        [Test]
        public void PayoutAfterIntervalPlaysMoneySoundAgain()
        {
            SendVehicleToTrack();

            CrossFinish();
            _time.Time += MoneyMinIntervalSeconds;
            CrossFinish();

            Assert.That(_audio.Played, Is.EqualTo(new[] { _money, _money }));
        }

        [Test]
        public void MilestoneClaimPlaysMoneySound()
        {
            _game.PlaceVehicle(0, MilestoneRank);

            _game.Milestones.Claim(MilestoneRank);

            Assert.That(_audio.Played, Is.EqualTo(new[] { _money }));
        }

        [Test]
        public void DisposedSoundsAreSilent()
        {
            _game.PlaceVehicle(0, 1);
            _game.PlaceVehicle(1, 1);
            _sounds.Dispose();

            _game.Garage.Merge(0, 1);

            Assert.That(_audio.Played, Is.Empty);
        }

        private void SendVehicleToTrack()
        {
            _game.PlaceVehicle(0, 1);
            _game.Roster.SendToTrack(0);
        }

        private void CrossFinish()
        {
            float lapSeconds = _fixture.Track.LapLength / _fixture.Vehicles.GetByRank(1).TrackSpeed;
            _game.Roster.Advance(lapSeconds * LapMargin);
        }
    }
}
