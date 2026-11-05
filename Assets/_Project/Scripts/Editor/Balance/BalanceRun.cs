using System;

namespace _Project.Editor.Balance
{
    public class BalanceRun
    {
        public BalanceRun(string variantName, string economyDescription, BalanceScenario scenario, BalanceRunStats stats)
        {
            VariantName = variantName ?? throw new ArgumentNullException(nameof(variantName));
            EconomyDescription = economyDescription ?? throw new ArgumentNullException(nameof(economyDescription));
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public string VariantName { get; }
        public string EconomyDescription { get; }
        public BalanceScenario Scenario { get; }
        public BalanceRunStats Stats { get; }
    }
}
