using System;
using UnityEngine;

namespace _Project.Core.Progression
{
    [Serializable]
    public class CapacityStep
    {
        [SerializeField] private int _rank;
        [SerializeField] private int _capacity;

        public int Rank => _rank;
        public int Capacity => _capacity;
    }
}
