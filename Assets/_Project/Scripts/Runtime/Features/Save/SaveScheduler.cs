using System;
using _Project.Core.Save;
using _Project.Core.Time;
using _Project.Features.Boxes;
using _Project.Features.Garage;
using _Project.Features.Track;
using _Project.Features.Vehicles;
using VContainer.Unity;

namespace _Project.Features.Save
{
    public class SaveScheduler : ILateTickable, IDisposable
    {
        private const float PeriodicWriteIntervalSeconds = 1f;

        private readonly IProgressStorage _storage;
        private readonly GameSaveFactory _factory;
        private readonly GarageBoard _garage;
        private readonly TrackRoster _roster;
        private readonly ITimeService _time;

        private bool _isWriting;
        private bool _hasImportantChanges;
        private float _secondsSinceWrite;

        public SaveScheduler(IProgressStorage storage, GameSaveFactory factory, GarageBoard garage, TrackRoster roster, ITimeService time)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        public void StartWriting()
        {
            if (_isWriting)
            {
                throw new InvalidOperationException(nameof(_isWriting));
            }

            _isWriting = true;
            _garage.VehiclePlaced += OnVehiclePlaced;
            _garage.BoxPlaced += OnBoxPlaced;
            _garage.BoxOpened += OnBoxOpened;
            _garage.VehicleMoved += OnVehicleMoved;
            _garage.VehicleMerged += OnVehicleMerged;
            _garage.VehicleRecycled += OnVehicleRecycled;
            _garage.BoxRecycled += OnBoxRecycled;
            _roster.VehicleSent += OnVehicleSent;
            _roster.VehicleReturned += OnVehicleReturned;
        }

        public void LateTick()
        {
            if (_isWriting == false)
            {
                return;
            }

            _secondsSinceWrite += _time.DeltaTime;

            if (_hasImportantChanges)
            {
                Write(true);
            }
            else if (_secondsSinceWrite >= PeriodicWriteIntervalSeconds)
            {
                Write(false);
            }
        }

        public void Dispose()
        {
            if (_isWriting == false)
            {
                return;
            }

            _isWriting = false;
            _garage.VehiclePlaced -= OnVehiclePlaced;
            _garage.BoxPlaced -= OnBoxPlaced;
            _garage.BoxOpened -= OnBoxOpened;
            _garage.VehicleMoved -= OnVehicleMoved;
            _garage.VehicleMerged -= OnVehicleMerged;
            _garage.VehicleRecycled -= OnVehicleRecycled;
            _garage.BoxRecycled -= OnBoxRecycled;
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;
        }

        private void Write(bool isImportant)
        {
            _storage.WriteState(_factory.Create(), isImportant);
            _hasImportantChanges = false;
            _secondsSinceWrite = 0;
        }

        private void MarkImportantChange()
        {
            _hasImportantChanges = true;
        }

        private void OnVehiclePlaced(int slot, Vehicle vehicle)
        {
            MarkImportantChange();
        }

        private void OnBoxPlaced(int slot, Box box)
        {
            MarkImportantChange();
        }

        private void OnBoxOpened(int slot, Vehicle vehicle)
        {
            MarkImportantChange();
        }

        private void OnVehicleMoved(int fromSlot, int toSlot)
        {
            MarkImportantChange();
        }

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            MarkImportantChange();
        }

        private void OnVehicleRecycled(int slot, Vehicle vehicle)
        {
            MarkImportantChange();
        }

        private void OnBoxRecycled(int slot, Box box)
        {
            MarkImportantChange();
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            MarkImportantChange();
        }

        private void OnVehicleReturned(TrackRunner runner)
        {
            MarkImportantChange();
        }
    }
}
