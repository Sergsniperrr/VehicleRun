using System.Collections.Generic;
using _Project.Core.Configs;
using UnityEngine;

namespace _Project.Editor.Balance
{
    [CreateAssetMenu(fileName = "BalanceSimulationSettings", menuName = "_Project/Editor/Balance Simulation Settings")]
    public class BalanceSimulationSettings : ScriptableObject
    {
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private string _reportPath;
        [SerializeField] private float _stepSeconds;
        [SerializeField] private float _activeTimeLimitSeconds;
        [SerializeField] private int _decorRankStep;
        [SerializeField] private double _decorIncomeBonus;
        [SerializeField] private float _firstMergeTargetSeconds;
        [SerializeField] private float _firstIncomeTargetSeconds;
        [SerializeField] private List<PaceTarget> _paceTargets = new List<PaceTarget>();
        [SerializeField] private List<BoxEconomyVariant> _economyVariants = new List<BoxEconomyVariant>();
        [SerializeField] private List<BalanceScenario> _scenarios = new List<BalanceScenario>();

        public GameConfig GameConfig => _gameConfig;
        public string ReportPath => _reportPath;
        public float StepSeconds => _stepSeconds;
        public float ActiveTimeLimitSeconds => _activeTimeLimitSeconds;
        public int DecorRankStep => _decorRankStep;
        public double DecorIncomeBonus => _decorIncomeBonus;
        public float FirstMergeTargetSeconds => _firstMergeTargetSeconds;
        public float FirstIncomeTargetSeconds => _firstIncomeTargetSeconds;
        public IReadOnlyList<PaceTarget> PaceTargets => _paceTargets;
        public IReadOnlyList<BoxEconomyVariant> EconomyVariants => _economyVariants;
        public IReadOnlyList<BalanceScenario> Scenarios => _scenarios;
    }
}
