using System;

namespace _Project.Core.Save
{
    [Serializable]
    public class GameSave
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion;
        public int ConfigVersion;
        public long Coins;
        public double CoinsRemainder;
        public long NextInstanceId;
        public int[] UnlockedRanks;
        public int[] BoxPurchaseCounts;
        public float FreeBoxRemainingSeconds;
        public bool HasReadyDelivery;
        public float TurboRemainingSeconds;
        public float TurboCooldownRemainingSeconds;
        public VehicleSave[] Vehicles;
        public BoxSave[] Boxes;
        public TrackRunnerSave[] TrackRunners;
        public int[] ClaimedMilestoneRanks = Array.Empty<int>();
    }
}
