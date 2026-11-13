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
        [SerializeField] private Vector2 _layoutAreaCenter = new Vector2(0f, 0.95f);
        [SerializeField] private Vector2 _layoutAreaSize = new Vector2(6.3f, 10.25f);
        [SerializeField] private Vector2 _layoutBaseCellSize = new Vector2(2.1f, 2.05f);
        [SerializeField] private Vector2 _slotCenterOffset = new Vector2(0f, 0.45f);
        [SerializeField] [Min(0.1f)] private float _layoutMaxScale = 1.5f;
        [SerializeField] [Min(1)] private int _layoutMaxColumns = 3;

        private GarageBoard _garage;
        private TrackRoster _roster;
        private VehicleCatalogConfig _vehicles;
        private GarageSlotLayout _layout;

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

            _layout = new GarageSlotLayout(
                _layoutAreaCenter,
                _layoutAreaSize,
                _layoutBaseCellSize,
                _slotCenterOffset,
                _layoutMaxScale,
                _layoutMaxColumns);
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
            ArrangeSlots();
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

        private void ArrangeSlots()
        {
            int openSlotCount = _garage.OpenSlotCount;

            for (int slot = 0; slot < _slots.Length; slot++)
            {
                GarageSlotView view = _slots[slot];
                bool isOpen = slot < openSlotCount;

                view.gameObject.SetActive(isOpen);

                if (isOpen)
                {
                    GarageSlotPlacement placement = _layout.Calculate(slot, openSlotCount);
                    view.transform.localPosition = placement.Position;
                    view.transform.localScale = Vector3.one * placement.Scale;
                }
            }
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
            ArrangeSlots();
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
