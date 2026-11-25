using System;
using _Project.Core.Time;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Collector;
using _Project.Features.Garage;
using _Project.Features.Track;
using _Project.Features.Turbo;
using _Project.Features.Vehicles;

namespace _Project.Features.Tutorial
{
    public class TutorialService : IDropRestriction, IDisposable
    {
        public const int CompletedStep = 0;
        public const int SendToTrackStep = 1;
        public const int OpenBoxStep = 2;
        public const int MergeStep = 3;
        public const int UpgradeTrackStep = 4;
        public const int BuyBoxStep = 5;
        public const int RecycleStep = 6;
        public const int RecycleConfirmStep = 7;
        public const int TrackTapStep = 8;
        public const int CollectorStep = 9;
        public const int MaxStep = CollectorStep;

        private const int TutorialBoxRank = 1;
        private const int UpgradeRank = 2;
        private const int BottomRowFirstSlot = 2;
        private const int NoSlot = -1;
        private const float CollectorPauseDelaySeconds = 1f;

        private readonly GarageBoard _garage;
        private readonly TrackRoster _roster;
        private readonly BoxDelivery _delivery;
        private readonly IRecycleConfirmation _recycleConfirmation;
        private readonly TurboService _turbo;
        private readonly CollectorService _collector;
        private readonly ITimeService _time;

        private bool _hasPurchasedBox;
        private int _recycleSlot = NoSlot;
        private float _collectorPauseRemainingSeconds;
        private float _scaleBeforeCollectorPause;

        public TutorialService(GarageBoard garage, TrackRoster roster, BoxDelivery delivery, IRecycleConfirmation recycleConfirmation, TurboService turbo, CollectorService collector, ITimeService time)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _recycleConfirmation = recycleConfirmation ?? throw new ArgumentNullException(nameof(recycleConfirmation));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _garage.BoxPlaced += OnBoxPlaced;
            _garage.BoxOpened += OnBoxOpened;
            _garage.VehicleMerged += OnVehicleMerged;
            _garage.BoxRecycled += OnBoxRecycled;
            _roster.VehicleSent += OnVehicleSent;
            _recycleConfirmation.PromptOpened += OnRecyclePromptOpened;
            _recycleConfirmation.PromptAnswered += OnRecyclePromptAnswered;
            _turbo.RunnerBoosted += OnRunnerBoosted;
            _collector.ChargeCollected += OnChargeCollected;
        }

        public event Action StepChanged;

        public int Step { get; private set; } = CompletedStep;
        public bool IsActive => Step != CompletedStep;
        public bool CanSkip => IsActive && Step >= UpgradeTrackStep;
        public bool IsPurchasedBoxWaiting => Step == BuyBoxStep && _hasPurchasedBox;
        public bool IsTrackOnly => Step == SendToTrackStep;
        public bool IsRecycleOnly => Step == RecycleStep;
        public bool IsCollectorPaused { get; private set; }

        public bool IsRecycleAllowed(int slot)
        {
            return Step != RecycleStep || TryGetRecycleSlot(out int recycleSlot) && slot == recycleSlot;
        }

        public bool TryGetRecycleSlot(out int slot)
        {
            if (_recycleSlot < 0 || _garage.CanRecycle(_recycleSlot) == false)
            {
                _recycleSlot = FindRecycleSlot();
            }

            slot = _recycleSlot;

            return slot >= 0;
        }

        public bool IsMergeAllowed(int fromSlot, int toSlot)
        {
            if (Step != MergeStep)
            {
                return true;
            }

            return TryGetMergePair(out int sourceSlot, out int targetSlot)
                && fromSlot == sourceSlot && toSlot == targetSlot;
        }

        public bool TryGetMergePair(out int sourceSlot, out int targetSlot)
        {
            // In the opening layout the first pair occupies the upper row.
            if (TryGetPair(1, 0, out sourceSlot, out targetSlot))
            {
                return true;
            }

            for (int left = 0; left < _garage.OpenSlotCount; left++)
            {
                for (int right = left + 1; right < _garage.OpenSlotCount; right++)
                {
                    if (TryGetPair(right, left, out sourceSlot, out targetSlot))
                    {
                        return true;
                    }
                }
            }

            sourceSlot = -1;
            targetSlot = -1;

            return false;
        }

        public void Begin()
        {
            if (IsActive)
            {
                throw new InvalidOperationException(nameof(IsActive));
            }

            SetStep(SendToTrackStep);
        }

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (Step != CollectorStep || IsCollectorPaused)
            {
                return;
            }

            _collectorPauseRemainingSeconds = Math.Max(0f, _collectorPauseRemainingSeconds - deltaTime);

            if (_collectorPauseRemainingSeconds > 0f)
            {
                return;
            }

            _scaleBeforeCollectorPause = _time.Scale;
            _time.Scale = 0f;
            IsCollectorPaused = true;
            StepChanged?.Invoke();
        }

        public CommandResult Skip()
        {
            if (CanSkip == false)
            {
                return new CommandResult(FailureReason.TutorialSkipUnavailable);
            }

            SetStep(CompletedStep);

            return new CommandResult(FailureReason.None);
        }

        public void Restore(int step)
        {
            if (step < CompletedStep || step > MaxStep)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }

            if (IsActive)
            {
                throw new InvalidOperationException(nameof(IsActive));
            }

            Step = step == RecycleConfirmStep ? RecycleStep : step;
            _recycleSlot = Step == RecycleStep ? FindRecycleSlot() : NoSlot;

            if (Step == CollectorStep)
            {
                StartCollectorStep();
            }
        }

        public void Dispose()
        {
            _garage.BoxPlaced -= OnBoxPlaced;
            _garage.BoxOpened -= OnBoxOpened;
            _garage.VehicleMerged -= OnVehicleMerged;
            _garage.BoxRecycled -= OnBoxRecycled;
            _roster.VehicleSent -= OnVehicleSent;
            _recycleConfirmation.PromptOpened -= OnRecyclePromptOpened;
            _recycleConfirmation.PromptAnswered -= OnRecyclePromptAnswered;
            _turbo.RunnerBoosted -= OnRunnerBoosted;
            _collector.ChargeCollected -= OnChargeCollected;
            ResumeCollectorTime();
        }

        private void SetStep(int step)
        {
            if (Step == CollectorStep && step != CollectorStep)
            {
                ResumeCollectorTime();
            }

            Step = step;
            _recycleSlot = step == RecycleStep ? FindRecycleSlot() : NoSlot;
            StepChanged?.Invoke();
        }

        private void GrantTutorialBox()
        {
            _delivery.GrantTutorialBox(TutorialBoxRank);
        }

        private bool HasMergeablePair()
        {
            return TryGetMergePair(out int _, out int _);
        }

        private bool TryGetPair(int sourceSlot, int targetSlot, out int foundSource, out int foundTarget)
        {
            foundSource = -1;
            foundTarget = -1;

            if (sourceSlot >= _garage.OpenSlotCount || targetSlot >= _garage.OpenSlotCount
                || IsGarageVehicle(sourceSlot, out Vehicle _) == false
                || _garage.IsMergePair(sourceSlot, targetSlot) == false)
            {
                return false;
            }

            foundSource = sourceSlot;
            foundTarget = targetSlot;

            return true;
        }

        private bool IsGarageVehicle(int slot, out Vehicle vehicle)
        {
            return _garage.TryGetVehicle(slot, out vehicle) && _garage.IsOnTrack(slot) == false;
        }

        private int FindRecycleSlot()
        {
            for (int slot = BottomRowFirstSlot; slot < _garage.OpenSlotCount; slot++)
            {
                if (IsFirstRankVehicle(slot))
                {
                    return slot;
                }
            }

            for (int slot = 0; slot < _garage.OpenSlotCount; slot++)
            {
                if (IsFirstRankVehicle(slot))
                {
                    return slot;
                }
            }

            for (int slot = 0; slot < _garage.OpenSlotCount; slot++)
            {
                if (_garage.CanRecycle(slot))
                {
                    return slot;
                }
            }

            return NoSlot;
        }

        private bool IsFirstRankVehicle(int slot)
        {
            return IsGarageVehicle(slot, out Vehicle vehicle) && vehicle.Rank == TutorialBoxRank && _garage.CanRecycle(slot);
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            if (Step == SendToTrackStep)
            {
                SetStep(OpenBoxStep);
                GrantTutorialBox();
            }
            else if (Step == UpgradeTrackStep && runner.Vehicle.Rank >= UpgradeRank)
            {
                SetStep(BuyBoxStep);
            }
        }

        private void OnBoxPlaced(int slot, Box box)
        {
            if (Step == BuyBoxStep && box.Source == BoxSource.Purchased)
            {
                _hasPurchasedBox = true;
                StepChanged?.Invoke();
            }
        }

        private void OnBoxOpened(int slot, Vehicle vehicle)
        {
            if (Step == OpenBoxStep)
            {
                SetStep(MergeStep);

                if (HasMergeablePair() == false)
                {
                    GrantTutorialBox();
                }
            }
            else if (Step == BuyBoxStep && _hasPurchasedBox)
            {
                _hasPurchasedBox = false;
                SetStep(RecycleStep);
            }
        }

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            if (Step == MergeStep)
            {
                SetStep(UpgradeTrackStep);
            }
        }

        private void OnBoxRecycled(int slot, Box box)
        {
            if (Step == OpenBoxStep)
            {
                GrantTutorialBox();
            }
            else if (Step == BuyBoxStep && _hasPurchasedBox)
            {
                _hasPurchasedBox = false;
                StepChanged?.Invoke();
            }
        }

        private void OnRecyclePromptOpened()
        {
            if (Step == RecycleStep)
            {
                SetStep(RecycleConfirmStep);
            }
        }

        private void OnRecyclePromptAnswered(bool isConfirmed, bool isNoButton)
        {
            if (Step == RecycleConfirmStep)
            {
                SetStep(isConfirmed == false && isNoButton ? TrackTapStep : RecycleStep);
            }
        }

        private void OnRunnerBoosted(TrackRunner runner)
        {
            if (Step == TrackTapStep)
            {
                if (_collector.SpawnNow())
                {
                    _collectorPauseRemainingSeconds = CollectorPauseDelaySeconds;
                    SetStep(CollectorStep);
                }
            }
        }

        private void OnChargeCollected(long amount)
        {
            if (Step == CollectorStep && IsCollectorPaused)
            {
                SetStep(CompletedStep);
            }
        }

        private void StartCollectorStep()
        {
            if (_collector.SpawnNow())
            {
                _collectorPauseRemainingSeconds = CollectorPauseDelaySeconds;
            }
        }

        private void ResumeCollectorTime()
        {
            if (IsCollectorPaused == false)
            {
                return;
            }

            if (_time.CanSetScale)
            {
                _time.Scale = _scaleBeforeCollectorPause;
            }

            IsCollectorPaused = false;
        }
    }
}
