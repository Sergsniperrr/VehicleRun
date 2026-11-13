using System;
using _Project.Core.Garage;
using _Project.Core.Progression;
using _Project.Core.Save;
using _Project.Core.Track;

namespace _Project.Features.Save
{
    public class SaveLevelResolver
    {
        private const int LegacyLevel = 0;

        private readonly GarageConfig _garage;
        private readonly TrackConfig _track;

        public SaveLevelResolver(GarageConfig garage, TrackConfig track)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        public bool IsLegacy(GameSave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            return save.PlayerLevel == LegacyLevel;
        }

        public int ResolveLevel(GameSave save)
        {
            if (IsLegacy(save) == false)
            {
                return save.PlayerLevel;
            }

            int usedSlotCount = CountUsedSlots(save);
            int runnerCount = save.TrackRunners.Length;
            int lastLevel = Math.Max(_garage.OpenSlots.LastLevel, _track.Capacity.LastLevel);
            int level = PlayerLevelConfig.FirstLevel;

            while (level < lastLevel && IsEnough(level, usedSlotCount, runnerCount) == false)
            {
                level++;
            }

            return level;
        }

        public long ResolveExperience(GameSave save)
        {
            return IsLegacy(save) ? 0 : save.PlayerExperience;
        }

        public int ResolveClaimedRewardLevel(GameSave save)
        {
            return IsLegacy(save) ? ResolveLevel(save) : save.ClaimedRewardLevel;
        }

        private bool IsEnough(int level, int usedSlotCount, int runnerCount)
        {
            return _garage.OpenSlots.GetCapacity(level) >= usedSlotCount && _track.Capacity.GetCapacity(level) >= runnerCount;
        }

        private int CountUsedSlots(GameSave save)
        {
            int usedSlotCount = 0;

            foreach (VehicleSave vehicle in save.Vehicles)
            {
                if (vehicle != null)
                {
                    usedSlotCount = Math.Max(usedSlotCount, vehicle.Slot + 1);
                }
            }

            foreach (BoxSave box in save.Boxes)
            {
                if (box != null)
                {
                    usedSlotCount = Math.Max(usedSlotCount, box.Slot + 1);
                }
            }

            return usedSlotCount;
        }
    }
}
