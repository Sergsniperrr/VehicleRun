using System;
using UnityEngine;

namespace _Project.Editor.Balance
{
    [Serializable]
    public class BalanceScenario
    {
        [SerializeField] private string _name;
        [SerializeField] private float _secondsPerAction;
        [SerializeField] private bool _buysBoxes;
        [SerializeField] private bool _buysCheapestRank;
        [SerializeField] private bool _usesBestLineup;
        [SerializeField] private bool _usesTurbo;
        [SerializeField] private float _sessionActiveSeconds;
        [SerializeField] private float _sessionOfflineSeconds;

        public string Name => _name;
        public float SecondsPerAction => _secondsPerAction;
        public bool BuysBoxes => _buysBoxes;
        public bool BuysCheapestRank => _buysCheapestRank;
        public bool UsesBestLineup => _usesBestLineup;
        public bool UsesTurbo => _usesTurbo;
        public float SessionActiveSeconds => _sessionActiveSeconds;
        public float SessionOfflineSeconds => _sessionOfflineSeconds;
        public bool HasSessions => _sessionActiveSeconds > 0;
    }
}
