using System;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Track;
using _Project.Features.Vehicles;

namespace _Project.Features.Garage
{
    public class GarageGestures
    {
        private readonly GarageBoard _garage;
        private readonly TrackRoster _roster;
        private readonly IDropRestriction _dropRestriction;

        public GarageGestures(GarageBoard garage, TrackRoster roster, IDropRestriction dropRestriction)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _dropRestriction = dropRestriction ?? throw new ArgumentNullException(nameof(dropRestriction));
        }

        public bool CanDrag(int slot)
        {
            return _garage.IsSlotOpen(slot) && _garage.IsSlotEmpty(slot) == false;
        }

        public bool CanMerge(int fromSlot, int toSlot)
        {
            return _dropRestriction.IsTrackOnly == false
                && _dropRestriction.IsRecycleOnly == false
                && _dropRestriction.IsMergeAllowed(fromSlot, toSlot)
                && _garage.IsMergePair(fromSlot, toSlot);
        }

        public bool CanDropOnTrack(int slot)
        {
            return _dropRestriction.IsRecycleOnly == false && _roster.CanSendToTrack(slot);
        }

        public bool CanRecycle(int slot)
        {
            return _dropRestriction.IsTrackOnly == false
                && _dropRestriction.IsRecycleAllowed(slot)
                && _garage.CanRecycle(slot);
        }

        public bool TryGetCardVehicle(int slot, out Vehicle vehicle)
        {
            if (_garage.TryGetVehicle(slot, out vehicle) && _garage.IsOnTrack(slot) == false)
            {
                return true;
            }

            vehicle = null;

            return false;
        }

        public CommandResult Tap(int slot)
        {
            if (_garage.TryGetBox(slot, out Box _))
            {
                return _garage.OpenBox(slot);
            }

            return _roster.ReturnFromTrack(slot);
        }

        public CommandResult DropOnSlot(int fromSlot, int toSlot)
        {
            if (_dropRestriction.IsTrackOnly || _dropRestriction.IsRecycleOnly)
            {
                return new CommandResult(FailureReason.TutorialDropRestricted);
            }

            if (_garage.TryGetVehicle(toSlot, out Vehicle _) == false)
            {
                return _garage.Move(fromSlot, toSlot);
            }

            if (_garage.IsMergePair(fromSlot, toSlot))
            {
                if (_dropRestriction.IsMergeAllowed(fromSlot, toSlot) == false)
                {
                    return new CommandResult(FailureReason.TutorialDropRestricted);
                }

                TakeOffTrack(fromSlot);
                TakeOffTrack(toSlot);
            }

            return _garage.Merge(fromSlot, toSlot);
        }

        public CommandResult DropOnTrack(int fromSlot)
        {
            if (_dropRestriction.IsRecycleOnly)
            {
                return new CommandResult(FailureReason.TutorialDropRestricted);
            }

            return _roster.SendToTrack(fromSlot);
        }

        public CommandResult Recycle(int slot)
        {
            if (_dropRestriction.IsTrackOnly || _dropRestriction.IsRecycleAllowed(slot) == false)
            {
                return new CommandResult(FailureReason.TutorialDropRestricted);
            }

            return _garage.Recycle(slot);
        }

        private void TakeOffTrack(int slot)
        {
            if (_garage.IsOnTrack(slot))
            {
                _roster.ReturnFromTrack(slot);
            }
        }
    }
}
