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

        public GarageGestures(GarageBoard garage, TrackRoster roster)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        public bool CanDrag(int slot)
        {
            return _garage.IsSlotOpen(slot) && _garage.IsSlotEmpty(slot) == false;
        }

        public bool CanMerge(int fromSlot, int toSlot)
        {
            return _garage.IsMergePair(fromSlot, toSlot);
        }

        public bool CanDropOnTrack(int slot)
        {
            return _roster.CanSendToTrack(slot);
        }

        public bool CanRecycle(int slot)
        {
            return _garage.CanRecycle(slot);
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
            if (_garage.TryGetVehicle(toSlot, out Vehicle _) == false)
            {
                return _garage.Move(fromSlot, toSlot);
            }

            if (_garage.IsMergePair(fromSlot, toSlot))
            {
                TakeOffTrack(fromSlot);
                TakeOffTrack(toSlot);
            }

            return _garage.Merge(fromSlot, toSlot);
        }

        public CommandResult DropOnTrack(int fromSlot)
        {
            return _roster.SendToTrack(fromSlot);
        }

        public CommandResult Recycle(int slot)
        {
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
