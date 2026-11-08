using System;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Track;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Turbo
{
    public class TurboServiceTests
    {
        private const float PartialEffectSeconds = 25f;
        private const float StepOverEffectEndSeconds = 10f;
        private const float LastEffectSecond = 1f;
        private const float StepAcrossEffectEndSeconds = 3f;
        private const float OversizedTimerSeconds = 1000f;
        private const float TickSeconds = 1f;
        private const float HalfTapBoostShare = 0.5f;
        private const int FirstRank = 1;
        private const float DistanceTolerance = 0.001f;
        private const int PausedTickCount = 10;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private FakeTimeService _time;
        private EconomyTicker _ticker;
        private int _startedCount;
        private int _endedCount;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
            _time = new FakeTimeService();
            _ticker = new EconomyTicker(_time, _game.Roster, _game.Turbo, _game.Delivery);
            _startedCount = 0;
            _endedCount = 0;
            _game.Turbo.TurboStarted += OnTurboStarted;
            _game.Turbo.TurboEnded += OnTurboEnded;
        }

        [TearDown]
        public void TearDown()
        {
            _game.Turbo.TurboStarted -= OnTurboStarted;
            _game.Turbo.TurboEnded -= OnTurboEnded;
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void TurboIsLockedBelowUnlockRank()
        {
            _game.PlaceVehicle(0, _fixture.Turbo.UnlockRank - 1);
            _game.Roster.SendToTrack(0);

            CommandResult result = _game.Turbo.StartTurbo();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.TurboLocked));
            Assert.That(_game.Turbo.IsUnlocked, Is.False);
            Assert.That(_game.Turbo.IsActive, Is.False);
        }

        [Test]
        public void TurboNeedsVehicleOnTrack()
        {
            _game.PlaceVehicle(0, _fixture.Turbo.UnlockRank);

            CommandResult result = _game.Turbo.StartTurbo();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.TrackEmpty));
            Assert.That(_game.Turbo.IsUnlocked, Is.True);
            Assert.That(_startedCount, Is.EqualTo(0));
        }

        [Test]
        public void StartedTurboSpeedsUpForFreeDuration()
        {
            PrepareUnlockedTrack();

            CommandResult result = _game.Turbo.StartTurbo();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_game.Turbo.IsActive, Is.True);
            Assert.That(_game.Turbo.SpeedMultiplier, Is.EqualTo(_fixture.Turbo.SpeedMultiplier));
            Assert.That(_game.Turbo.RemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeDurationSeconds));
            Assert.That(_game.Turbo.RemainingProgress, Is.EqualTo(1f));
            Assert.That(_startedCount, Is.EqualTo(1));
        }

        [Test]
        public void ActiveTurboCannotStartAgain()
        {
            PrepareUnlockedTrack();
            _game.Turbo.StartTurbo();

            CommandResult result = _game.Turbo.StartTurbo();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.TurboActive));
            Assert.That(_startedCount, Is.EqualTo(1));
        }

        [Test]
        public void EndedTurboStartsCooldown()
        {
            PrepareUnlockedTrack();
            _game.Turbo.StartTurbo();

            _game.Turbo.Advance(_fixture.Turbo.FreeDurationSeconds);

            Assert.That(_game.Turbo.IsActive, Is.False);
            Assert.That(_game.Turbo.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(_game.Turbo.CooldownRemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeCooldownSeconds));
            Assert.That(_game.Turbo.CooldownProgress, Is.EqualTo(0f));
            Assert.That(_game.Turbo.StartTurbo().FailureReason, Is.EqualTo(FailureReason.TurboCooldown));
            Assert.That(_endedCount, Is.EqualTo(1));
        }

        [Test]
        public void TimeAfterEffectGoesToCooldown()
        {
            PrepareUnlockedTrack();
            _game.Turbo.StartTurbo();
            _game.Turbo.Advance(PartialEffectSeconds);

            _game.Turbo.Advance(StepOverEffectEndSeconds);

            float secondsAfterEffect = PartialEffectSeconds + StepOverEffectEndSeconds - _fixture.Turbo.FreeDurationSeconds;
            float expectedCooldown = _fixture.Turbo.FreeCooldownSeconds - secondsAfterEffect;
            Assert.That(_game.Turbo.CooldownRemainingSeconds, Is.EqualTo(expectedCooldown));
        }

        [Test]
        public void TurboIsReadyAgainAfterCooldown()
        {
            PrepareUnlockedTrack();
            _game.Turbo.StartTurbo();
            _game.Turbo.Advance(_fixture.Turbo.FreeDurationSeconds);

            _game.Turbo.Advance(_fixture.Turbo.FreeCooldownSeconds);

            Assert.That(_game.Turbo.IsCoolingDown, Is.False);
            Assert.That(_game.Turbo.CooldownProgress, Is.EqualTo(1f));
            Assert.That(_game.Turbo.StartTurbo().IsSuccess, Is.True);
            Assert.That(_startedCount, Is.EqualTo(2));
        }

        [Test]
        public void RunnerTimeIsBoostedOnlyUntilEffectEnds()
        {
            TrackRunner runner = PrepareUnlockedTrack();
            _game.Turbo.StartTurbo();
            _game.Turbo.Advance(_fixture.Turbo.FreeDurationSeconds - LastEffectSecond);

            float runnerSeconds = _game.Turbo.CalculateRunnerSeconds(runner, StepAcrossEffectEndSeconds);

            float expectedSeconds = StepAcrossEffectEndSeconds + LastEffectSecond * (_fixture.Turbo.SpeedMultiplier - 1f);
            Assert.That(runnerSeconds, Is.EqualTo(expectedSeconds));
        }

        [Test]
        public void RunnerTimeIsNotBoostedWithoutTurbo()
        {
            TrackRunner runner = PrepareUnlockedTrack();

            Assert.That(_game.Turbo.CalculateRunnerSeconds(runner, StepAcrossEffectEndSeconds), Is.EqualTo(StepAcrossEffectEndSeconds));
            Assert.That(_game.Turbo.IsRunnerBoosted(runner), Is.False);
        }

        [Test]
        public void ActiveTurboBoostsEveryRunner()
        {
            TrackRunner runner = PrepareUnlockedTrack();

            _game.Turbo.StartTurbo();

            Assert.That(_game.Turbo.IsRunnerBoosted(runner), Is.True);
            Assert.That(runner.TapBoostRemainingSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void TapBoostsRunnerBelowTurboUnlockRank()
        {
            TrackRunner runner = SendVehicle(0, FirstRank);

            CommandResult result = _game.Turbo.BoostRunner(runner);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_game.Turbo.IsUnlocked, Is.False);
            Assert.That(_game.Turbo.IsRunnerBoosted(runner), Is.True);
            Assert.That(runner.TapBoostRemainingSeconds, Is.EqualTo(_fixture.Turbo.TapBoostSeconds));
            Assert.That(_game.Turbo.IsActive, Is.False);
            Assert.That(_startedCount, Is.EqualTo(0));
        }

        [Test]
        public void TapBoostEndsAfterConfiguredTime()
        {
            TrackRunner runner = SendVehicle(0, FirstRank);
            _game.Turbo.BoostRunner(runner);

            _game.Turbo.Advance(_fixture.Turbo.TapBoostSeconds);

            Assert.That(_game.Turbo.IsRunnerBoosted(runner), Is.False);
            Assert.That(runner.TapBoostRemainingSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void TapBoostSpeedsUpOnlyItsTimeInStep()
        {
            TrackRunner runner = SendVehicle(0, FirstRank);
            _game.Turbo.BoostRunner(runner);

            float runnerSeconds = _game.Turbo.CalculateRunnerSeconds(runner, StepAcrossEffectEndSeconds);

            float expectedSeconds = StepAcrossEffectEndSeconds + _fixture.Turbo.TapBoostSeconds * (_fixture.Turbo.SpeedMultiplier - 1f);
            Assert.That(runnerSeconds, Is.EqualTo(expectedSeconds));
        }

        [Test]
        public void TickerDrivesOnlyTappedRunnerFaster()
        {
            TrackRunner tappedRunner = SendVehicle(0, FirstRank);
            TrackRunner otherRunner = SendVehicle(1, FirstRank);
            float tappedStart = tappedRunner.Distance;
            float otherStart = otherRunner.Distance;
            _game.Turbo.BoostRunner(tappedRunner);
            _time.DeltaTime = _fixture.Turbo.TapBoostSeconds;

            _ticker.Tick();

            float plainDistance = tappedRunner.Vehicle.Definition.TrackSpeed * _fixture.Turbo.TapBoostSeconds;
            float boostedDistance = plainDistance * _fixture.Turbo.SpeedMultiplier;
            Assert.That(tappedRunner.Distance - tappedStart, Is.EqualTo(boostedDistance).Within(DistanceTolerance));
            Assert.That(otherRunner.Distance - otherStart, Is.EqualTo(plainDistance).Within(DistanceTolerance));
            Assert.That(_game.Turbo.IsRunnerBoosted(tappedRunner), Is.False);
        }

        [Test]
        public void RepeatedTapRestartsBoost()
        {
            TrackRunner runner = SendVehicle(0, FirstRank);
            _game.Turbo.BoostRunner(runner);
            _game.Turbo.Advance(_fixture.Turbo.TapBoostSeconds * HalfTapBoostShare);

            _game.Turbo.BoostRunner(runner);

            Assert.That(runner.TapBoostRemainingSeconds, Is.EqualTo(_fixture.Turbo.TapBoostSeconds));
        }

        [Test]
        public void TapBoostDoesNotStackWithTurbo()
        {
            TrackRunner runner = PrepareUnlockedTrack();
            _game.Turbo.StartTurbo();
            _game.Turbo.BoostRunner(runner);

            float runnerSeconds = _game.Turbo.CalculateRunnerSeconds(runner, TickSeconds);

            Assert.That(runnerSeconds, Is.EqualTo(TickSeconds * _fixture.Turbo.SpeedMultiplier));
        }

        [Test]
        public void ReturnedRunnerCannotBeBoosted()
        {
            TrackRunner runner = SendVehicle(0, FirstRank);
            _game.Roster.ReturnFromTrack(0);

            CommandResult result = _game.Turbo.BoostRunner(runner);

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.NotOnTrack));
            Assert.That(runner.TapBoostRemainingSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void PausedTickerKeepsTapBoost()
        {
            TrackRunner runner = SendVehicle(0, FirstRank);
            _game.Turbo.BoostRunner(runner);
            _time.DeltaTime = 0f;

            TickPaused();

            Assert.That(runner.TapBoostRemainingSeconds, Is.EqualTo(_fixture.Turbo.TapBoostSeconds));
        }

        [Test]
        public void MissingRunnerIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => _game.Turbo.BoostRunner(null));
            Assert.Throws<ArgumentNullException>(() => _game.Turbo.IsRunnerBoosted(null));
            Assert.Throws<ArgumentNullException>(() => _game.Turbo.CalculateRunnerSeconds(null, TickSeconds));
        }

        [Test]
        public void TickerDrivesRunnersFasterDuringTurbo()
        {
            TrackRunner runner = PrepareUnlockedTrack();
            float startDistance = runner.Distance;
            _game.Turbo.StartTurbo();
            _time.DeltaTime = TickSeconds;

            _ticker.Tick();

            float expectedDistance = runner.Vehicle.Definition.TrackSpeed * TickSeconds * _fixture.Turbo.SpeedMultiplier;
            Assert.That(runner.Distance - startDistance, Is.EqualTo(expectedDistance).Within(DistanceTolerance));
            Assert.That(_game.Turbo.RemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeDurationSeconds - TickSeconds));
        }

        [Test]
        public void PausedTickerKeepsTurboTimers()
        {
            PrepareUnlockedTrack();
            _game.Turbo.StartTurbo();
            _time.DeltaTime = 0f;

            TickPaused();

            Assert.That(_game.Turbo.RemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeDurationSeconds));

            _game.Turbo.Advance(_fixture.Turbo.FreeDurationSeconds);
            TickPaused();

            Assert.That(_game.Turbo.CooldownRemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeCooldownSeconds));
        }

        [Test]
        public void RestoreLimitsTimersByConfig()
        {
            _game.Turbo.Restore(OversizedTimerSeconds, OversizedTimerSeconds);

            Assert.That(_game.Turbo.RemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeDurationSeconds));
            Assert.That(_game.Turbo.CooldownRemainingSeconds, Is.EqualTo(_fixture.Turbo.FreeCooldownSeconds));
            Assert.That(_startedCount, Is.EqualTo(0));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void RestoreRejectsBrokenTimers(float seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _game.Turbo.Restore(seconds, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _game.Turbo.Restore(0f, seconds));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void BrokenDeltaTimeIsRejected(float deltaTime)
        {
            TrackRunner runner = PrepareUnlockedTrack();

            Assert.Throws<ArgumentOutOfRangeException>(() => _game.Turbo.Advance(deltaTime));
            Assert.Throws<ArgumentOutOfRangeException>(() => _game.Turbo.CalculateRunnerSeconds(runner, deltaTime));
        }

        private TrackRunner PrepareUnlockedTrack()
        {
            return SendVehicle(0, _fixture.Turbo.UnlockRank);
        }

        private TrackRunner SendVehicle(int slot, int rank)
        {
            _game.PlaceVehicle(slot, rank);
            _game.Roster.SendToTrack(slot);

            return _game.Roster.Runners[_game.Roster.Count - 1];
        }

        private void TickPaused()
        {
            for (int i = 0; i < PausedTickCount; i++)
            {
                _ticker.Tick();
            }
        }

        private void OnTurboStarted()
        {
            _startedCount++;
        }

        private void OnTurboEnded()
        {
            _endedCount++;
        }
    }
}
