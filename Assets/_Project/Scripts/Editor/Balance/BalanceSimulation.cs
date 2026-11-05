using System;
using _Project.Core.Configs;
using _Project.Core.Economy;

namespace _Project.Editor.Balance
{
    public class BalanceSimulation
    {
        private readonly BalanceSimulationSettings _settings;

        public BalanceSimulation(BalanceSimulationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (settings.GameConfig == null)
            {
                throw new InvalidOperationException(nameof(settings.GameConfig));
            }

            if (settings.StepSeconds <= 0)
            {
                throw new InvalidOperationException(nameof(settings.StepSeconds));
            }
        }

        public BalanceRunStats Run(EconomyConfig economy, BalanceScenario scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (scenario.SecondsPerAction <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scenario));
            }

            GameConfig config = _settings.GameConfig;
            int lastRank = config.Vehicles.MaxRank;

            using (SimulatedGame game = new SimulatedGame(config, economy, _settings))
            using (BalanceRunStats stats = new BalanceRunStats(game))
            {
                GreedyPlayer player = new GreedyPlayer(game, scenario);
                double nextActionSeconds = 0;
                double sessionEndSeconds = scenario.SessionActiveSeconds;

                while (game.Progress.MaxRank < lastRank && game.ActiveSeconds < _settings.ActiveTimeLimitSeconds)
                {
                    if (game.ActiveSeconds >= nextActionSeconds)
                    {
                        stats.RecordAction(player.Act(), scenario.SecondsPerAction);
                        nextActionSeconds += scenario.SecondsPerAction;
                    }

                    game.Advance(_settings.StepSeconds);

                    if (scenario.HasSessions && game.ActiveSeconds >= sessionEndSeconds)
                    {
                        long offlineCoins = game.CollectOfflineIncome(scenario.SessionOfflineSeconds);
                        stats.RecordOfflineIncome(offlineCoins, scenario.SessionOfflineSeconds);
                        sessionEndSeconds += scenario.SessionActiveSeconds;
                    }
                }

                stats.Complete();

                return stats;
            }
        }
    }
}
