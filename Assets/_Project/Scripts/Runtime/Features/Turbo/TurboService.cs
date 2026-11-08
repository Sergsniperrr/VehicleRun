using System;
using _Project.Core.Turbo;
using _Project.Features.Commands;
using _Project.Features.Progression;
using _Project.Features.Track;

namespace _Project.Features.Turbo
{
    public class TurboService
    {
        private readonly TurboConfig _config;
        private readonly RankProgress _progress;
        private readonly TrackRoster _roster;

        public TurboService(TurboConfig config, RankProgress progress, TrackRoster roster)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        public event Action TurboStarted;
        public event Action TurboEnded;

        public float RemainingSeconds { get; private set; }
        public float CooldownRemainingSeconds { get; private set; }
        public bool IsUnlocked => _progress.MaxRank >= _config.UnlockRank;
        public bool IsActive => RemainingSeconds > 0;
        public bool IsCoolingDown => CooldownRemainingSeconds > 0;
        public float SpeedMultiplier => IsActive ? _config.SpeedMultiplier : 1f;
        public float RemainingProgress => RemainingSeconds / _config.FreeDurationSeconds;
        public float CooldownProgress => IsCoolingDown ? 1f - CooldownRemainingSeconds / _config.FreeCooldownSeconds : 1f;

        public bool CanStartTurbo()
        {
            return CheckStart() == FailureReason.None;
        }

        public CommandResult StartTurbo()
        {
            FailureReason failureReason = CheckStart();

            if (failureReason != FailureReason.None)
            {
                return new CommandResult(failureReason);
            }

            RemainingSeconds = _config.FreeDurationSeconds;
            TurboStarted?.Invoke();

            return new CommandResult(FailureReason.None);
        }

        public float CalculateTrackSeconds(float deltaTime)
        {
            ValidateDeltaTime(deltaTime);

            float boostedSeconds = Math.Min(deltaTime, RemainingSeconds);

            return deltaTime + boostedSeconds * (_config.SpeedMultiplier - 1f);
        }

        public void Advance(float deltaTime)
        {
            ValidateDeltaTime(deltaTime);

            if (IsActive)
            {
                AdvanceEffect(deltaTime);

                return;
            }

            CooldownRemainingSeconds = Math.Max(0f, CooldownRemainingSeconds - deltaTime);
        }

        public void Restore(float remainingSeconds, float cooldownRemainingSeconds)
        {
            if (IsValidSeconds(remainingSeconds) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingSeconds));
            }

            if (IsValidSeconds(cooldownRemainingSeconds) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownRemainingSeconds));
            }

            RemainingSeconds = Math.Min(remainingSeconds, _config.FreeDurationSeconds);
            CooldownRemainingSeconds = Math.Min(cooldownRemainingSeconds, _config.FreeCooldownSeconds);
        }

        private FailureReason CheckStart()
        {
            if (IsUnlocked == false)
            {
                return FailureReason.TurboLocked;
            }

            if (IsActive)
            {
                return FailureReason.TurboActive;
            }

            if (IsCoolingDown)
            {
                return FailureReason.TurboCooldown;
            }

            if (_roster.Count == 0)
            {
                return FailureReason.TrackEmpty;
            }

            return FailureReason.None;
        }

        private void AdvanceEffect(float deltaTime)
        {
            if (deltaTime < RemainingSeconds)
            {
                RemainingSeconds -= deltaTime;

                return;
            }

            float secondsAfterEffect = deltaTime - RemainingSeconds;
            RemainingSeconds = 0f;
            CooldownRemainingSeconds = Math.Max(0f, _config.FreeCooldownSeconds - secondsAfterEffect);
            TurboEnded?.Invoke();
        }

        private void ValidateDeltaTime(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }
        }

        private bool IsValidSeconds(float seconds)
        {
            return float.IsNaN(seconds) == false && float.IsInfinity(seconds) == false && seconds >= 0;
        }
    }
}
