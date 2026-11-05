using System;
using System.Collections.Generic;
using System.Globalization;
using _Project.Core.Economy;
using Object = UnityEngine.Object;

namespace _Project.Editor.Balance
{
    public class BalanceSimulationRunner
    {
        private const string CurrentConfigVariantName = "Текущий конфиг";

        private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("ru-RU");

        public IReadOnlyList<BalanceRun> Run(BalanceSimulationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            List<BalanceRun> runs = new List<BalanceRun>();
            BalanceSimulation simulation = new BalanceSimulation(settings);
            EconomyConfig economy = settings.GameConfig.Economy;

            RunScenarios(simulation, settings.Scenarios, CurrentConfigVariantName, economy, runs);

            foreach (BoxEconomyVariant variant in settings.EconomyVariants)
            {
                EconomyConfig variantEconomy = variant.CreateEconomy(economy);

                try
                {
                    RunScenarios(simulation, settings.Scenarios, variant.Name, variantEconomy, runs);
                }
                finally
                {
                    Object.DestroyImmediate(variantEconomy);
                }
            }

            return runs;
        }

        private void RunScenarios(
            BalanceSimulation simulation,
            IReadOnlyList<BalanceScenario> scenarios,
            string variantName,
            EconomyConfig economy,
            List<BalanceRun> runs)
        {
            string economyDescription = DescribeEconomy(economy);

            foreach (BalanceScenario scenario in scenarios)
            {
                BalanceRunStats stats = simulation.Run(economy, scenario);
                runs.Add(new BalanceRun(variantName, economyDescription, scenario, stats));
            }
        }

        private string DescribeEconomy(EconomyConfig economy)
        {
            string interval = economy.FreeBoxIntervalSeconds.ToString(_culture);
            string basePrice = economy.BoxBasePriceIncomeSeconds.ToString(_culture);
            string growth = economy.BoxPriceGrowth.ToString(_culture);
            string firstRankGrowth = economy.FirstRankBoxPriceGrowth.ToString(_culture);
            string limit = economy.IsBoxPriceGrowthLimited
                ? $"потолок {economy.BoxPriceGrowthPurchaseLimit} покупок"
                : "без потолка";

            return $"отставание ранга коробки {economy.BoxRankLag}, бесплатная коробка раз в {interval} с, цена {basePrice} с дохода старшего ранга, рост {growth}, ранг 1 — {firstRankGrowth}, {limit}";
        }
    }
}
