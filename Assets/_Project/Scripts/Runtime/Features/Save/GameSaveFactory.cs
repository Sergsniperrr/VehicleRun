using System;
using System.Collections.Generic;
using _Project.Core.Configs;
using _Project.Core.Save;
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
    public class GameSaveFactory
    {
        private readonly GameConfig _config;
        private readonly InstanceIdGenerator _instanceIds;
        private readonly GarageBoard _garage;
        private readonly RankProgress _progress;
        private readonly Wallet _wallet;
        private readonly BoxPricing _pricing;
        private readonly BoxDelivery _delivery;
        private readonly TrackRoster _roster;
        private readonly TurboService _turbo;
        private readonly CollectionMilestones _milestones;

        public GameSaveFactory(
            GameConfig config,
            InstanceIdGenerator instanceIds,
            GarageBoard garage,
            RankProgress progress,
            Wallet wallet,
            BoxPricing pricing,
            BoxDelivery delivery,
            TrackRoster roster,
            TurboService turbo,
            CollectionMilestones milestones)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _instanceIds = instanceIds ?? throw new ArgumentNullException(nameof(instanceIds));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
        }

        public GameSave Create()
        {
            return new GameSave
            {
                SchemaVersion = GameSave.CurrentSchemaVersion,
                ConfigVersion = _config.Version,
                Coins = _wallet.Coins,
                CoinsRemainder = _wallet.Remainder,
                NextInstanceId = _instanceIds.NextId,
                UnlockedRanks = CollectUnlockedRanks(),
                BoxPurchaseCounts = CollectBoxPurchaseCounts(),
                FreeBoxRemainingSeconds = _delivery.FreeBoxRemainingSeconds,
                HasReadyDelivery = _delivery.HasReadyDelivery,
                TurboRemainingSeconds = _turbo.RemainingSeconds,
                TurboCooldownRemainingSeconds = _turbo.CooldownRemainingSeconds,
                Vehicles = CollectVehicles(),
                Boxes = CollectBoxes(),
                TrackRunners = CollectTrackRunners(),
                ClaimedMilestoneRanks = CollectClaimedMilestones(),
            };
        }

        private int[] CollectClaimedMilestones()
        {
            List<int> ranks = new List<int>(_milestones.ClaimedRanks);
            ranks.Sort();

            return ranks.ToArray();
        }

        private int[] CollectUnlockedRanks()
        {
            List<int> ranks = new List<int>();

            for (int rank = 1; rank <= _config.Vehicles.MaxRank; rank++)
            {
                if (_progress.IsRankUnlocked(rank))
                {
                    ranks.Add(rank);
                }
            }

            return ranks.ToArray();
        }

        private int[] CollectBoxPurchaseCounts()
        {
            int[] purchaseCounts = new int[_config.Vehicles.MaxRank];

            for (int rank = 1; rank <= purchaseCounts.Length; rank++)
            {
                purchaseCounts[rank - 1] = _pricing.GetPurchaseCount(rank);
            }

            return purchaseCounts;
        }

        private VehicleSave[] CollectVehicles()
        {
            List<VehicleSave> vehicles = new List<VehicleSave>();

            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetVehicle(slot, out Vehicle vehicle))
                {
                    vehicles.Add(new VehicleSave
                    {
                        InstanceId = vehicle.InstanceId,
                        Slot = slot,
                        VehicleId = vehicle.Definition.Id,
                    });
                }
            }

            return vehicles.ToArray();
        }

        private BoxSave[] CollectBoxes()
        {
            List<BoxSave> boxes = new List<BoxSave>();

            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetBox(slot, out Box box))
                {
                    boxes.Add(new BoxSave
                    {
                        InstanceId = box.InstanceId,
                        Slot = slot,
                        VehicleId = box.Content.Id,
                        Source = (int)box.Source,
                    });
                }
            }

            return boxes.ToArray();
        }

        private TrackRunnerSave[] CollectTrackRunners()
        {
            TrackRunnerSave[] runners = new TrackRunnerSave[_roster.Count];

            for (int i = 0; i < runners.Length; i++)
            {
                TrackRunner runner = _roster.Runners[i];
                runners[i] = new TrackRunnerSave
                {
                    VehicleInstanceId = runner.Vehicle.InstanceId,
                    Distance = runner.Distance,
                };
            }

            return runners;
        }
    }
}
