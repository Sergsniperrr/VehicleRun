using System;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Track;
using _Project.Features.Vehicles;
using UnityEngine;
using VContainer;

namespace _Project.Features.Garage
{
    public class GarageView : MonoBehaviour
    {
        [SerializeField] private GarageSlotView[] _slots = Array.Empty<GarageSlotView>();

        private GarageBoard _garage;
        private TrackRoster _roster;
        private VehicleCatalogConfig _vehicles;

        public int SlotCount => _slots.Length;

        public bool IsMergePlaying
        {
            get
            {
                for (int slot = 0; slot < _slots.Length; slot++)
                {
                    if (_slots[slot].IsMerging)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Awake()
        {
            for (int slot = 0; slot < _slots.Length; slot++)
            {
                _slots[slot].Bind(slot);
            }
        }

        private void Start()
        {
            if (_slots.Length != _garage.SlotCount)
            {
                throw new InvalidOperationException(nameof(_slots));
            }

            _garage.VehiclePlaced += OnVehiclePlaced;
            _garage.BoxPlaced += OnBoxPlaced;
            _garage.BoxOpened += OnBoxOpened;
            _garage.VehicleMoved += OnVehicleMoved;
            _garage.VehicleMerged += OnVehicleMerged;
            _garage.VehicleRecycled += OnVehicleRecycled;
            _garage.BoxRecycled += OnBoxRecycled;
            _garage.SlotsUnlocked += OnSlotsUnlocked;
            _roster.VehicleSent += OnVehicleSent;
            _roster.VehicleReturned += OnVehicleReturned;
            RefreshAll();
        }

        private void OnDestroy()
        {
            _garage.VehiclePlaced -= OnVehiclePlaced;
            _garage.BoxPlaced -= OnBoxPlaced;
            _garage.BoxOpened -= OnBoxOpened;
            _garage.VehicleMoved -= OnVehicleMoved;
            _garage.VehicleMerged -= OnVehicleMerged;
            _garage.VehicleRecycled -= OnVehicleRecycled;
            _garage.BoxRecycled -= OnBoxRecycled;
            _garage.SlotsUnlocked -= OnSlotsUnlocked;
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;
        }

        [Inject]
        public void Construct(GarageBoard garage, TrackRoster roster, VehicleCatalogConfig vehicles)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _vehicles = vehicles ?? throw new ArgumentNullException(nameof(vehicles));
        }

        public GarageSlotView GetSlot(int slot)
        {
            return _slots[slot];
        }

        private void RefreshAll()
        {
            for (int slot = 0; slot < _slots.Length; slot++)
            {
                Refresh(slot);
            }
        }

        private void Refresh(int slot)
        {
            GarageSlotView view = _slots[slot];

            if (_garage.IsSlotOpen(slot) == false)
            {
                view.ShowLocked();
            }
            else if (_garage.TryGetVehicle(slot, out Vehicle vehicle))
            {
                view.ShowVehicle(vehicle, _garage.IsOnTrack(slot));
            }
            else if (_garage.TryGetBox(slot, out Box box))
            {
                view.ShowBox(box);
            }
            else
            {
                view.ShowEmpty();
            }
        }

        private void OnVehiclePlaced(int slot, Vehicle vehicle)
        {
            Refresh(slot);
        }

        private void OnBoxPlaced(int slot, Box box)
        {
            Refresh(slot);
        }

        private void OnBoxOpened(int slot, Vehicle vehicle)
        {
            Refresh(slot);
            _slots[slot].PlayBoxOpened();
        }

        private void OnVehicleMoved(int fromSlot, int toSlot)
        {
            Refresh(fromSlot);
            Refresh(toSlot);
        }

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            VehicleDefinition merged = _vehicles.GetByRank(result.Rank - 1);

            Refresh(fromSlot);
            Refresh(toSlot);
            _slots[toSlot].PlayMerge(merged);
        }

        private void OnVehicleRecycled(int slot, Vehicle vehicle)
        {
            Refresh(slot);
        }

        private void OnBoxRecycled(int slot, Box box)
        {
            Refresh(slot);
        }

        private void OnSlotsUnlocked(int openSlotCount)
        {
            RefreshAll();
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            Refresh(runner.Slot);
        }

        private void OnVehicleReturned(TrackRunner runner)
        {
            Refresh(runner.Slot);
        }
    }
}
