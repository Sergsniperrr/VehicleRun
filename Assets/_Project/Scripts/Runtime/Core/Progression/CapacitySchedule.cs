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

        public CapacityStep GetStepAt(int index)
        {
            return _steps[index];
        }

        public int GetCapacity(int maxRank)
        {
            int capacity = _initial;

            foreach (CapacityStep step in _steps)
            {
                if (step.Rank <= maxRank)
                {
                    capacity = Math.Max(capacity, step.Capacity);
                }
            }

            return capacity;
        }
    }
}
