using System;
using UnityEngine;

namespace _Project.Core.Progression
{
    [Serializable]
    public class CapacitySchedule
    {
        [SerializeField] private int _initial;
        [SerializeField] private CapacityStep[] _steps = Array.Empty<CapacityStep>();

        public int Initial => _initial;
        public int StepCount => _steps.Length;
        public int LastLevel => CalculateLastLevel();

        public CapacityStep GetStepAt(int index)
        {
            return _steps[index];
        }

        public int GetCapacity(int level)
        {
            int capacity = _initial;

            foreach (CapacityStep step in _steps)
            {
                if (step.Level <= level)
                {
                    capacity = Math.Max(capacity, step.Capacity);
                }
            }

            return capacity;
        }

        private int CalculateLastLevel()
        {
            int lastLevel = PlayerLevelConfig.FirstLevel;

            foreach (CapacityStep step in _steps)
            {
                lastLevel = Math.Max(lastLevel, step.Level);
            }

            return lastLevel;
        }
    }
}
