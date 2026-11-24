using System;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Garage;
using _Project.Features.Progression;
using _Project.Features.Track;
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
        public const int CatalogStep = 6;
        public const int MaxStep = CatalogStep;

        private const int TutorialBoxRank = 1;
        private const int UpgradeRank = 2;

        private readonly GarageBoard _garage;
        private readonly TrackRoster _roster;
        private readonly BoxDelivery _delivery;
        private readonly CatalogNews _catalogNews;

        private bool _hasPurchasedBox;

        public TutorialService(GarageBoard garage, TrackRoster roster, BoxDelivery delivery, CatalogNews catalogNews)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _catalogNews = catalogNews ?? throw new ArgumentNullException(nameof(catalogNews));
            _garage.BoxPlaced += OnBoxPlaced;
            _garage.BoxOpened += OnBoxOpened;
            _garage.VehicleMerged += OnVehicleMerged;
            _garage.BoxRecycled += OnBoxRecycled;
            _roster.VehicleSent += OnVehicleSent;
            _catalogNews.Seen += OnCatalogSeen;
        }

        public event Action StepChanged;

        public int Step { get; private set; } = CompletedStep;
        public bool IsActive => Step != CompletedStep;
        public bool CanSkip => IsActive && Step >= UpgradeTrackStep;
        public bool IsPurchasedBoxWaiting => Step == BuyBoxStep && _hasPurchasedBox;
        public bool IsTrackOnly => Step == SendToTrackStep;

        public void Begin()
        {
            if (IsActive)
            {
                throw new InvalidOperationException(nameof(IsActive));
            }

            SetStep(SendToTrackStep);
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

            Step = step;
        }

        public void Dispose()
        {
            _garage.BoxPlaced -= OnBoxPlaced;
            _garage.BoxOpened -= OnBoxOpened;
            _garage.VehicleMerged -= OnVehicleMerged;
            _garage.BoxRecycled -= OnBoxRecycled;
            _roster.VehicleSent -= OnVehicleSent;
            _catalogNews.Seen -= OnCatalogSeen;
        }

        private void SetStep(int step)
        {
            Step = step;
            StepChanged?.Invoke();
        }

        private void GrantTutorialBox()
        {
            _delivery.GrantTutorialBox(TutorialBoxRank);
        }

        private bool HasMergeablePair()
        {
            for (int left = 0; left < _garage.SlotCount; left++)
            {
                if (IsGarageVehicle(left, out Vehicle leftVehicle) == false)
                {
                    continue;
                }

                for (int right = left + 1; right < _garage.SlotCount; right++)
                {
                    if (IsGarageVehicle(right, out Vehicle rightVehicle) && rightVehicle.Rank == leftVehicle.Rank)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsGarageVehicle(int slot, out Vehicle vehicle)
        {
            return _garage.TryGetVehicle(slot, out vehicle) && _garage.IsOnTrack(slot) == false;
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
                SetStep(CatalogStep);
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

        private void OnCatalogSeen()
        {
            if (Step == CatalogStep)
            {
                SetStep(CompletedStep);
            }
        }
    }
}
