using System;
using _Project.Core.Economy;
using _Project.Core.Vehicles;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Vehicles;

namespace _Project.Features.Session
{
    public class NewGameSetup
    {
        private readonly EconomyConfig _economy;
        private readonly VehicleCatalogConfig _catalog;
        private readonly GarageBoard _garage;
        private readonly RankProgress _progress;
        private readonly InstanceIdGenerator _instanceIds;

        public NewGameSetup(
            EconomyConfig economy,
            VehicleCatalogConfig catalog,
            GarageBoard garage,
            RankProgress progress,
            InstanceIdGenerator instanceIds)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _instanceIds = instanceIds ?? throw new ArgumentNullException(nameof(instanceIds));
        }

        public void Apply()
        {
            VehicleDefinition definition = _catalog.GetByRank(_economy.StartVehicleRank);
            _progress.UnlockRank(definition.Rank);

            for (int slot = 0; slot < _economy.StartVehicleCount; slot++)
            {
                _garage.PlaceVehicle(slot, new Vehicle(_instanceIds.Create(), definition));
            }
        }
    }
}
