using System;
using _Project.Core.Garage;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Instances;
using _Project.Features.Vehicles;

namespace _Project.Features.Garage
{
    public class GarageBoard
    {
        private const int NoSlot = -1;

        private readonly GarageConfig _config;
        private readonly VehicleCatalogConfig _catalog;
        private readonly InstanceIdGenerator _instanceIds;
        private readonly Vehicle[] _vehicles;
        private readonly Box[] _boxes;

        public GarageBoard(GarageConfig config, VehicleCatalogConfig catalog, InstanceIdGenerator instanceIds)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _instanceIds = instanceIds ?? throw new ArgumentNullException(nameof(instanceIds));
            _vehicles = new Vehicle[config.SlotCount];
            _boxes = new Box[config.SlotCount];
            OpenSlotCount = Math.Min(config.OpenSlots.Initial, config.SlotCount);
        }

        public event Action<int, Vehicle> VehiclePlaced;
        public event Action<int, Box> BoxPlaced;
        public event Action<int, Vehicle> BoxOpened;
        public event Action<int, int> VehicleMoved;
        public event Action<int, int, Vehicle> VehicleMerged;
        public event Action<int> SlotsUnlocked;

        public int SlotCount => _vehicles.Length;
        public int OpenSlotCount { get; private set; }

        public bool IsSlotOpen(int slot)
        {
            CheckSlotIndex(slot);

            return slot < OpenSlotCount;
        }

        public bool IsSlotEmpty(int slot)
        {
            CheckSlotIndex(slot);

            return _vehicles[slot] == null && _boxes[slot] == null;
        }

        public bool TryGetVehicle(int slot, out Vehicle vehicle)
        {
            CheckSlotIndex(slot);
            vehicle = _vehicles[slot];

            return vehicle != null;
        }

        public bool TryGetBox(int slot, out Box box)
        {
            CheckSlotIndex(slot);
            box = _boxes[slot];

            return box != null;
        }

        public bool TryFindFreeSlot(out int slot)
        {
            for (int i = 0; i < OpenSlotCount; i++)
            {
                if (IsSlotEmpty(i))
                {
                    slot = i;

                    return true;
                }
            }

            slot = NoSlot;

            return false;
        }

        public void PlaceVehicle(int slot, Vehicle vehicle)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            CheckSlotAvailable(slot);
            _vehicles[slot] = vehicle;
            VehiclePlaced?.Invoke(slot, vehicle);
        }

        public void PlaceBox(int slot, Box box)
        {
            if (box == null)
            {
                throw new ArgumentNullException(nameof(box));
            }

            CheckSlotAvailable(slot);
            _boxes[slot] = box;
            BoxPlaced?.Invoke(slot, box);
        }

        public void UnlockSlots(int maxRank)
        {
            int openSlotCount = Math.Min(_config.OpenSlots.GetCapacity(maxRank), SlotCount);

            if (openSlotCount <= OpenSlotCount)
            {
                return;
            }

            OpenSlotCount = openSlotCount;
            SlotsUnlocked?.Invoke(OpenSlotCount);
        }

        public CommandResult OpenBox(int slot)
        {
            if (IsSlotOpen(slot) == false)
            {
                return Fail(FailureReason.SlotLocked);
            }

            Box box = _boxes[slot];

            if (box == null)
            {
                return Fail(FailureReason.NoBox);
            }

            Vehicle vehicle = new Vehicle(_instanceIds.Create(), box.Content);
            _boxes[slot] = null;
            _vehicles[slot] = vehicle;
            BoxOpened?.Invoke(slot, vehicle);

            return Succeed();
        }

        public CommandResult Move(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot)
            {
                return Fail(FailureReason.SameSlot);
            }

            if (IsSlotOpen(fromSlot) == false || IsSlotOpen(toSlot) == false)
            {
                return Fail(FailureReason.SlotLocked);
            }

            Vehicle vehicle = _vehicles[fromSlot];

            if (vehicle == null)
            {
                return Fail(FailureReason.NoVehicle);
            }

            if (IsSlotEmpty(toSlot) == false)
            {
                return Fail(FailureReason.SlotOccupied);
            }

            _vehicles[fromSlot] = null;
            _vehicles[toSlot] = vehicle;
            VehicleMoved?.Invoke(fromSlot, toSlot);

            return Succeed();
        }

        public CommandResult Merge(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot)
            {
                return Fail(FailureReason.SameSlot);
            }

            if (IsSlotOpen(fromSlot) == false || IsSlotOpen(toSlot) == false)
            {
                return Fail(FailureReason.SlotLocked);
            }

            Vehicle source = _vehicles[fromSlot];

            if (source == null)
            {
                return Fail(FailureReason.NoVehicle);
            }

            if (_boxes[toSlot] != null)
            {
                return Fail(FailureReason.TargetIsBox);
            }

            Vehicle target = _vehicles[toSlot];

            if (target == null)
            {
                return Fail(FailureReason.NoVehicle);
            }

            if (source.Rank != target.Rank)
            {
                return Fail(FailureReason.RankMismatch);
            }

            if (source.Rank >= _catalog.MaxRank)
            {
                return Fail(FailureReason.MaxRank);
            }

            Vehicle result = new Vehicle(_instanceIds.Create(), _catalog.GetByRank(source.Rank + 1));
            _vehicles[fromSlot] = null;
            _vehicles[toSlot] = result;
            VehicleMerged?.Invoke(fromSlot, toSlot, result);

            return Succeed();
        }

        private void CheckSlotIndex(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        private void CheckSlotAvailable(int slot)
        {
            if (IsSlotOpen(slot) == false || IsSlotEmpty(slot) == false)
            {
                throw new InvalidOperationException(nameof(slot));
            }
        }

        private CommandResult Succeed()
        {
            return new CommandResult(FailureReason.None);
        }

        private CommandResult Fail(FailureReason reason)
        {
            return new CommandResult(reason);
        }
    }
}
