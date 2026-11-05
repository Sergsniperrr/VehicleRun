using System;
using UnityEngine;

namespace _Project.Editor.Balance
{
    [Serializable]
    public class PaceTarget
    {
        [SerializeField] private int _rank;
        [SerializeField] private float _minSeconds;
        [SerializeField] private float _maxSeconds;

        public int Rank => _rank;
        public float MinSeconds => _minSeconds;
        public float MaxSeconds => _maxSeconds;
    }
}
