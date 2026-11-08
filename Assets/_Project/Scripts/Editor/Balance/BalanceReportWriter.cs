using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace _Project.Editor.Balance
{
    public class BalanceReportWriter
    {
        private const double Thousand = 1e3;
        private const double Million = 1e6;
        private const double Billion = 1e9;
        private const double ShortCoinsThreshold = 1e4;
        private const string NotReached = "—";
        private const string InTarget = "✓";
        private const string FasterThanTarget = "↓";
        private const string SlowerThanTarget = "↑";

        private readonly BalanceSimulationSettings _settings;
        private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("ru-RU");

        public BalanceReportWriter(BalanceSimulationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string Write(IReadOnlyList<BalanceRun> runs, DateTime createdAt)
        {
            if (runs == null)
            {
                throw new ArgumentNullException(nameof(runs));
            }

            StringBuilder report = new StringBuilder();
            WriteHeader(report, createdAt);
            string currentVariant = null;

            foreach (BalanceRun run in runs)
            {
                if (run.VariantName != currentVariant)
                {
                    currentVariant = run.VariantName;
                    WriteVariantHeader(report, run);
                }

                WriteRow(report, run);
            }

            return report.ToString();
        }

        private void WriteHeader(StringBuilder report, DateTime createdAt)
        {
            report.AppendLine("# Отчёт симулятора баланса");
            report.AppendLine();
            report.AppendLine($"Создан {createdAt.ToString("dd.MM.yyyy HH:mm", _culture)} меню `_Project › Balance › Run Simulation` по конфигу `{_settings.GameConfig.name}` версии {_settings.GameConfig.Version}. Настройки симуляции лежат в `BalanceSimulationSettings.asset`.");
            report.AppendLine();
            report.AppendLine("Время считается по активной игре в формате ч:мм:сс, офлайн в него не входит. Жадный игрок раз в «секунд на действие» делает одно действие: открывает коробку, сливает старшую пару, включает турбо, меняет состав трассы, покупает коробку или перерабатывает младшую машину в полном гараже. Покупает он коробку ранга b, как кнопка коробки, а в сценарии «с магазином» — ранг до b, у которого ниже цена в пересчёте на машину ранга 1 (ранг r — это 2^(r−1) машин ранга 1). Машины трассы занимают слоты гаража.");
            report.AppendLine();
            report.AppendLine($"Учтены правила ГДД, которых ещё нет в логике игры: декор D = 1 + {Format(_settings.DecorIncomeBonus)} × floor(K / {_settings.DecorRankStep}) и офлайн-доход по ГДД 16. Турбо берётся из логики игры и `TurboConfig`. Обучения нет: игра начинается с монет и машин из `EconomyConfig`. Предел симуляции {FormatTime(_settings.ActiveTimeLimitSeconds)} активной игры.");
            report.AppendLine();
            report.AppendLine($"Метки: {InTarget} в цели ГДД 10, {FasterThanTarget} быстрее цели, {SlowerThanTarget} медленнее цели, {NotReached} не достигнуто до предела.");
            report.AppendLine();
            WriteTargets(report);
        }

        private void WriteTargets(StringBuilder report)
        {
            StringBuilder targets = new StringBuilder();
            targets.Append($"Цели: первое слияние до {FormatTime(_settings.FirstMergeTargetSeconds)}, первый доход до {FormatTime(_settings.FirstIncomeTargetSeconds)}");

            foreach (PaceTarget target in _settings.PaceTargets)
            {
                targets.Append($", K = {target.Rank} за {FormatTime(target.MinSeconds)}–{FormatTime(target.MaxSeconds)}");
            }

            targets.Append('.');
            report.AppendLine(targets.ToString());
        }

        private void WriteVariantHeader(StringBuilder report, BalanceRun run)
        {
            report.AppendLine();
            report.AppendLine($"## {run.VariantName}: {run.EconomyDescription}");
            report.AppendLine();

            StringBuilder header = new StringBuilder("| Сценарий | Секунд на действие | Первое слияние | Первый доход |");
            StringBuilder separator = new StringBuilder("|---|---|---|---|");

            foreach (PaceTarget target in _settings.PaceTargets)
            {
                header.Append($" K = {target.Rank} |");
                separator.Append("---|");
            }

            header.Append(" Коробки бесплатные / купленные | Переработано машин | Ожидание монет | Монет в конце | Сессий и офлайн-монет |");
            separator.Append("---|---|---|---|---|");
            report.AppendLine(header.ToString());
            report.AppendLine(separator.ToString());
        }

        private void WriteRow(StringBuilder report, BalanceRun run)
        {
            BalanceRunStats stats = run.Stats;
            StringBuilder row = new StringBuilder();
            row.Append($"| {run.Scenario.Name} | {Format(run.Scenario.SecondsPerAction)} | ");
            row.Append($"{FormatFirstTime(stats.FirstMergeSeconds, _settings.FirstMergeTargetSeconds)} | ");
            row.Append($"{FormatFirstTime(stats.FirstIncomeSeconds, _settings.FirstIncomeTargetSeconds)} |");

            foreach (PaceTarget target in _settings.PaceTargets)
            {
                row.Append($" {FormatRankTime(stats, target)} |");
            }

            row.Append($" {stats.FreeBoxCount} / {stats.BoughtBoxCount} | {stats.RecycledVehicleCount} | {FormatTime(stats.CoinsWaitSeconds)} | {FormatCoins(stats.FinalCoins)} | {FormatSessions(stats)} |");
            report.AppendLine(row.ToString());
        }

        private string FormatFirstTime(double? seconds, float targetSeconds)
        {
            if (seconds.HasValue == false)
            {
                return NotReached;
            }

            string mark = seconds.Value <= targetSeconds ? InTarget : SlowerThanTarget;

            return $"{FormatTime(seconds.Value)} {mark}";
        }

        private string FormatRankTime(BalanceRunStats stats, PaceTarget target)
        {
            if (stats.TryGetRankSeconds(target.Rank, out double seconds) == false)
            {
                return NotReached;
            }

            string mark = InTarget;

            if (seconds < target.MinSeconds)
            {
                mark = FasterThanTarget;
            }
            else if (seconds > target.MaxSeconds)
            {
                mark = SlowerThanTarget;
            }

            return $"{FormatTime(seconds)} {mark}";
        }

        private string FormatSessions(BalanceRunStats stats)
        {
            if (stats.SessionCount == 0)
            {
                return NotReached;
            }

            return $"{stats.SessionCount}, {FormatCoins(stats.OfflineCoins)}";
        }

        private string FormatTime(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Math.Round(seconds));

            return $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
        }

        private string FormatCoins(long coins)
        {
            if (coins >= Billion)
            {
                return $"{Format(coins / Billion)} млрд";
            }

            if (coins >= Million)
            {
                return $"{Format(coins / Million)} млн";
            }

            if (coins >= ShortCoinsThreshold)
            {
                return $"{Format(coins / Thousand)} тыс.";
            }

            return coins.ToString(_culture);
        }

        private string Format(double value)
        {
            return value.ToString("0.#", _culture);
        }
    }
}
