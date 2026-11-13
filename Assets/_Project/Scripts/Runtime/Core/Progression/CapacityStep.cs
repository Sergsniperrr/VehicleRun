using System;
using UnityEngine;

namespace _Project.Core.Progression
{
    [Serializable]
    public class CapacityStep
    {
        [SerializeField] private int _level;
        [SerializeField] private int _capacity;

        public int Level => _level;
        public int Capacity => _capacity;
    }
}
