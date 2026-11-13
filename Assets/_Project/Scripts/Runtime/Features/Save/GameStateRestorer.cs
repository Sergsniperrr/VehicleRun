using System;
using _Project.Core.Save;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.Features.Turbo;
using _Project.Features.Vehicles;

namespace _Project.Features.Save
{
    public class GameStateRestorer
    {
        private readonly VehicleCatalogConfig _catalog;
        private readonly InstanceIdGenerator _instanceIds;
        private readonly GarageBoard _garage;
        private readonly RankProgress _progress;
        private readonly Wallet _wallet;
        private readonly BoxPricing _pricing;
        private readonly BoxDelivery _delivery;
        private readonly TrackRoster _roster;
        private readonly TurboService _turbo;
        private readonly CollectionMilestones _milestones;
        private readonly PlayerLevel _playerLevel;
        private readonly SaveLevelResolver _levels;

        public GameStateRestorer(
            VehicleCatalogConfig catalog,
            InstanceIdGenerator instanceIds,
            GarageBoard garage,
            RankProgress progress,
            Wallet wallet,
            BoxPricing pricing,
            BoxDelivery delivery,
            TrackRoster roster,
            TurboService turbo,
            CollectionMilestones milestones,
            PlayerLevel playerLevel,
            SaveLevelResolver levels)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _instanceIds = instanceIds ?? throw new ArgumentNullException(nameof(instanceIds));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
        }

        public void Restore(GameSave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            _instanceIds.Restore(save.NextInstanceId);
            _progress.RestoreUnlockedRanks(save.UnlockedRanks);
            _playerLevel.Restore(
                _levels.ResolveLevel(save),
                _levels.ResolveExperience(save),
                _levels.ResolveClaimedRewardLevel(save));

            foreach (VehicleSave vehicle in save.Vehicles)
            {
                _garage.RestoreVehicle(vehicle.Slot, new Vehicle(vehicle.InstanceId, GetDefinition(vehicle.VehicleId)));
            }

            foreach (BoxSave box in save.Boxes)
            {
                _garage.RestoreBox(box.Slot, new Box(box.InstanceId, (BoxSource)box.Source, GetDefinition(box.VehicleId)));
            }

            foreach (TrackRunnerSave runner in save.TrackRunners)
            {
                _roster.RestoreRunner(FindVehicleSlot(save.Vehicles, runner.VehicleInstanceId), runner.Distance);
            }

            _wallet.Restore(save.Coins, save.CoinsRemainder);
            _pricing.RestorePurchaseCounts(save.BoxPurchaseCounts);
            _delivery.Restore(save.FreeBoxRemainingSeconds, save.HasReadyDelivery);
            _turbo.Restore(save.TurboRemainingSeconds, save.TurboCooldownRemainingSeconds);
            _milestones.RestoreClaimed(save.ClaimedMilestoneRanks);
        }

        private VehicleDefinition GetDefinition(string vehicleId)
        {
            if (_catalog.TryGetById(vehicleId, out VehicleDefinition definition) == false)
            {
                throw new InvalidOperationException(nameof(vehicleId));
            }

            return definition;
        }

        private int FindVehicleSlot(VehicleSave[] vehicles, long instanceId)
        {
            foreach (VehicleSave vehicle in vehicles)
            {
                if (vehicle.InstanceId == instanceId)
                {
                    return vehicle.Slot;
                }
            }

            throw new InvalidOperationException(nameof(instanceId));
        }
    }
}
