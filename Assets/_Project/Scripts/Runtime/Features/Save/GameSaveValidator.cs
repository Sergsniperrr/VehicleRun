using System;
using System.Collections.Generic;
using _Project.Core.Garage;
using _Project.Core.Save;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Instances;

namespace _Project.Features.Save
{
    public class GameSaveValidator
    {
        private readonly VehicleCatalogConfig _catalog;
        private readonly GarageConfig _garage;
        private readonly TrackConfig _track;

        public GameSaveValidator(VehicleCatalogConfig catalog, GarageConfig garage, TrackConfig track)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        public bool IsValid(GameSave save)
        {
            if (save == null || save.SchemaVersion != GameSave.CurrentSchemaVersion)
            {
                return false;
            }

            if (HasAllCollections(save) == false || AreCountersValid(save) == false || AreRanksValid(save.UnlockedRanks) == false)
            {
                return false;
            }

            if (AreMilestonesValid(save.ClaimedMilestoneRanks) == false)
            {
                return false;
            }

            HashSet<int> unlockedRanks = new HashSet<int>(save.UnlockedRanks);
            int maxRank = FindMaxRank(save.UnlockedRanks);
            int openSlotCount = Math.Min(_garage.OpenSlots.GetCapacity(maxRank), _garage.SlotCount);
            HashSet<long> instanceIds = new HashSet<long>();
            HashSet<int> occupiedSlots = new HashSet<int>();

            foreach (VehicleSave vehicle in save.Vehicles)
            {
                if (IsVehicleValid(vehicle, unlockedRanks) == false)
                {
                    return false;
                }

                if (TryOccupy(vehicle.InstanceId, vehicle.Slot, save.NextInstanceId, openSlotCount, instanceIds, occupiedSlots) == false)
                {
                    return false;
                }
            }

            foreach (BoxSave box in save.Boxes)
            {
                if (IsBoxValid(box, maxRank) == false)
                {
                    return false;
                }

                if (TryOccupy(box.InstanceId, box.Slot, save.NextInstanceId, openSlotCount, instanceIds, occupiedSlots) == false)
                {
                    return false;
                }
            }

            return AreTrackRunnersValid(save, _track.Capacity.GetCapacity(maxRank));
        }

        private bool HasAllCollections(GameSave save)
        {
            return save.UnlockedRanks != null
                && save.BoxPurchaseCounts != null
                && save.Vehicles != null
                && save.Boxes != null
                && save.TrackRunners != null
                && save.ClaimedMilestoneRanks != null;
        }

        private bool AreMilestonesValid(int[] ranks)
        {
            HashSet<int> uniqueRanks = new HashSet<int>();

            foreach (int rank in ranks)
            {
                if (rank < 1 || rank > _catalog.MaxRank || uniqueRanks.Add(rank) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreCountersValid(GameSave save)
        {
            if (save.Coins < 0 || save.NextInstanceId < InstanceIdGenerator.FirstId)
            {
                return false;
            }

            if (double.IsNaN(save.CoinsRemainder) || save.CoinsRemainder < 0 || save.CoinsRemainder > 1)
            {
                return false;
            }

            if (IsNonNegativeNumber(save.FreeBoxRemainingSeconds) == false)
            {
                return false;
            }

            if (IsNonNegativeNumber(save.TurboRemainingSeconds) == false || IsNonNegativeNumber(save.TurboCooldownRemainingSeconds) == false)
            {
                return false;
            }

            if (save.BoxPurchaseCounts.Length > _catalog.MaxRank)
            {
                return false;
            }

            foreach (int purchaseCount in save.BoxPurchaseCounts)
            {
                if (purchaseCount < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreRanksValid(int[] ranks)
        {
            if (ranks.Length == 0)
            {
                return false;
            }

            HashSet<int> uniqueRanks = new HashSet<int>();

            foreach (int rank in ranks)
            {
                if (rank < 1 || rank > _catalog.MaxRank || uniqueRanks.Add(rank) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private int FindMaxRank(int[] ranks)
        {
            int maxRank = 0;

            foreach (int rank in ranks)
            {
                maxRank = Math.Max(maxRank, rank);
            }

            return maxRank;
        }

        private bool IsVehicleValid(VehicleSave vehicle, HashSet<int> unlockedRanks)
        {
            if (vehicle == null || _catalog.TryGetById(vehicle.VehicleId, out VehicleDefinition definition) == false)
            {
                return false;
            }

            return unlockedRanks.Contains(definition.Rank);
        }

        private bool IsBoxValid(BoxSave box, int maxRank)
        {
            if (box == null || _catalog.TryGetById(box.VehicleId, out VehicleDefinition definition) == false)
            {
                return false;
            }

            return definition.Rank <= maxRank && Enum.IsDefined(typeof(BoxSource), box.Source);
        }

        private bool TryOccupy(long instanceId, int slot, long nextInstanceId, int openSlotCount, HashSet<long> instanceIds, HashSet<int> occupiedSlots)
        {
            if (instanceId < InstanceIdGenerator.FirstId || instanceId >= nextInstanceId)
            {
                return false;
            }

            if (slot < 0 || slot >= openSlotCount)
            {
                return false;
            }

            return instanceIds.Add(instanceId) && occupiedSlots.Add(slot);
        }

        private bool AreTrackRunnersValid(GameSave save, int trackCapacity)
        {
            if (save.TrackRunners.Length > trackCapacity)
            {
                return false;
            }

            HashSet<long> vehicleIds = new HashSet<long>();

            foreach (VehicleSave vehicle in save.Vehicles)
            {
                vehicleIds.Add(vehicle.InstanceId);
            }

            HashSet<long> runnerIds = new HashSet<long>();

            foreach (TrackRunnerSave runner in save.TrackRunners)
            {
                if (runner == null || IsNonNegativeNumber(runner.Distance) == false)
                {
                    return false;
                }

                if (vehicleIds.Contains(runner.VehicleInstanceId) == false || runnerIds.Add(runner.VehicleInstanceId) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsNonNegativeNumber(float value)
        {
            return float.IsNaN(value) == false && float.IsInfinity(value) == false && value >= 0;
        }
    }
}
