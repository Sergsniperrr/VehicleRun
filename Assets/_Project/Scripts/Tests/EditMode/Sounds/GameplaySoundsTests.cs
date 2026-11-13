using _Project.Core.Audio;
using _Project.Features.Boxes;
using _Project.Features.Sounds;
using _Project.Features.Track;
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
        private const float FirstRankPitch = 1.3f;
        private const float LastRankPitch = 0.7f;
        private const float PitchTolerance = 0.0001f;
        private const int TurboUnlockRank = 3;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private FakeAudioService _audio;
        private FakeTimeService _time;
        private GameSoundsConfig _config;
        private AudioAsset _merge;
        private AudioAsset _money;
        private AudioAsset _boxOpen;
        private AudioAsset _startMoving;
        private AudioAsset _boost;
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
            _startMoving = ScriptableObject.CreateInstance<AudioAsset>();
            _boost = ScriptableObject.CreateInstance<AudioAsset>();
            _config = ScriptableObject.CreateInstance<GameSoundsConfig>();

            SerializedObject serializedConfig = new SerializedObject(_config);
            serializedConfig.FindProperty("_merge").objectReferenceValue = _merge;
            serializedConfig.FindProperty("_money").objectReferenceValue = _money;
            serializedConfig.FindProperty("_boxOpen").objectReferenceValue = _boxOpen;
            serializedConfig.FindProperty("_startMoving").objectReferenceValue = _startMoving;
            serializedConfig.FindProperty("_boost").objectReferenceValue = _boost;
            serializedConfig.FindProperty("_startMovingFirstRankPitch").floatValue = FirstRankPitch;
            serializedConfig.FindProperty("_startMovingLastRankPitch").floatValue = LastRankPitch;
            serializedConfig.FindProperty("_moneyMinIntervalSeconds").floatValue = MoneyMinIntervalSeconds;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            _sounds = new GameplaySounds(
                _audio,
                _config,
                _time,
                _game.Garage,
                _game.Roster,
                _game.Milestones,
                _game.Turbo,
                _fixture.Vehicles,
                _game.Level);
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
            Object.DestroyImmediate(_startMoving);
            Object.DestroyImmediate(_boost);
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
        public void LevelRewardClaimPlaysMoneySound()
        {
            _game.Level.AddExperience(_game.Level.ExperienceToNextLevel);

            _game.Level.ClaimReward();

            Assert.That(_audio.Played, Is.EqualTo(new[] { _money }));
        }

        [Test]
        public void MilestoneClaimPlaysMoneySound()
        {
            _game.PlaceVehicle(0, MilestoneRank);

            _game.Milestones.Claim(MilestoneRank);

            Assert.That(_audio.Played, Is.EqualTo(new[] { _money }));
        }

        [Test]
        public void SendToTrackPlaysStartMovingSound()
        {
            _game.PlaceVehicle(0, 1);

            _game.Roster.SendToTrack(0);

            Assert.That(_audio.Played, Is.EqualTo(new[] { _startMoving }));
        }

        [Test]
        public void FirstRankStartsWithFirstRankPitch()
        {
            _game.PlaceVehicle(0, 1);

            _game.Roster.SendToTrack(0);

            Assert.That(_audio.PitchScales[0], Is.EqualTo(FirstRankPitch).Within(PitchTolerance));
        }

        [Test]
        public void LastRankStartsWithLastRankPitch()
        {
            _game.PlaceVehicle(0, _fixture.Vehicles.MaxRank);

            _game.Roster.SendToTrack(0);

            Assert.That(_audio.PitchScales[0], Is.EqualTo(LastRankPitch).Within(PitchTolerance));
        }

        [Test]
        public void StartPitchFallsWithRank()
        {
            int lastRank = _fixture.Vehicles.MaxRank;
            int middleRank = (1 + lastRank) / 2;
            float rankShare = (middleRank - 1) / (float)(lastRank - 1);
            float expectedPitch = FirstRankPitch + (LastRankPitch - FirstRankPitch) * rankShare;
            _game.PlaceVehicle(0, middleRank);

            _game.Roster.SendToTrack(0);

            Assert.That(_audio.PitchScales[0], Is.EqualTo(expectedPitch).Within(PitchTolerance));
        }

        [Test]
        public void FailedSendIsSilent()
        {
            _game.Roster.SendToTrack(0);

            Assert.That(_audio.Played, Is.Empty);
        }

        [Test]
        public void TapBoostPlaysBoostSound()
        {
            SendVehicleToTrack();

            _game.Turbo.BoostRunner(_game.Roster.Runners[0]);

            Assert.That(_audio.Played, Is.EqualTo(new[] { _boost }));
        }

        [Test]
        public void TapBoostOfReturnedVehicleIsSilent()
        {
            SendVehicleToTrack();
            TrackRunner runner = _game.Roster.Runners[0];
            _game.Roster.ReturnFromTrack(0);

            _game.Turbo.BoostRunner(runner);

            Assert.That(_audio.Played, Is.Empty);
        }

        [Test]
        public void TurboStartPlaysBoostSound()
        {
            _game.PlaceVehicle(1, TurboUnlockRank);
            SendVehicleToTrack();

            _game.Turbo.StartTurbo();

            Assert.That(_audio.Played, Is.EqualTo(new[] { _boost }));
        }

        [Test]
        public void FailedTurboStartIsSilent()
        {
            SendVehicleToTrack();

            _game.Turbo.StartTurbo();

            Assert.That(_audio.Played, Is.Empty);
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
            _audio.Clear();
        }

        private void CrossFinish()
        {
            float lapSeconds = _fixture.Track.LapLength / _fixture.Vehicles.GetByRank(1).TrackSpeed;
            _game.Roster.Advance(lapSeconds * LapMargin);
        }
    }
}
