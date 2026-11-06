using System;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Track;
using _Project.Features.Vehicles;

namespace _Project.Composition.Startup
{
    public class SampleStateSeeder
    {
        private static readonly (int Slot, int Rank)[] SampleVehicles = { (0, 5), (1, 4), (2, 4), (3, 3), (4, 2), (6, 1), (8, 2) };
        private static readonly (int Slot, int Rank)[] SampleBoxes = { (7, 1) };
        private static readonly int[] SampleTrackSlots = { 0, 2, 3 };

        private readonly VehicleCatalogConfig _catalog;
        private readonly GarageBoard _garage;
        private readonly TrackRoster _roster;
        private readonly InstanceIdGenerator _instanceIds;

        public SampleStateSeeder(VehicleCatalogConfig catalog, GarageBoard garage, TrackRoster roster, InstanceIdGenerator instanceIds)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _instanceIds = instanceIds ?? throw new ArgumentNullException(nameof(instanceIds));
        }

        public void Seed()
        {
            foreach ((int slot, int rank) in SampleVehicles)
            {
                _garage.PlaceVehicle(slot, new Vehicle(_instanceIds.Create(), _catalog.GetByRank(rank)));
            }

            foreach ((int slot, int rank) in SampleBoxes)
            {
                _garage.PlaceBox(slot, new Box(_instanceIds.Create(), BoxSource.Free, _catalog.GetByRank(rank)));
            }

            foreach (int slot in SampleTrackSlots)
            {
                _roster.SendToTrack(slot);
            }
        }
    }
}
