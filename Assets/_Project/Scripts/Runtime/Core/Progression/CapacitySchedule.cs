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
    }
}
