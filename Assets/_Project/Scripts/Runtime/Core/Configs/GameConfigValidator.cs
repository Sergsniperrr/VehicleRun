using System;
using System.Collections.Generic;
using _Project.Core.Economy;
using _Project.Core.Garage;
using _Project.Core.Localization;
using _Project.Core.Progression;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using Object = UnityEngine.Object;

namespace _Project.Core.Configs
{
    public class GameConfigValidator
    {
        private const int FirstRank = 1;
        private const string GarageScheduleName = "Открытые места гаража";
        private const string TrackScheduleName = "Места трассы";

        public IReadOnlyList<ConfigError> Validate(GameConfig config, LocalizationConfig localization)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (localization == null)
            {
                throw new ArgumentNullException(nameof(localization));
            }

            List<ConfigError> errors = new List<ConfigError>();

            if (config.Version < 1)
            {
                errors.Add(new ConfigError(config, $"Версия конфига {config.Version}, должна быть не меньше 1"));
            }

            if (HasAllParts(config, errors) == false)
            {
                return errors;
            }

            ValidateVehicles(config.Vehicles, localization, errors);

            int maxRank = config.Vehicles.Count;

            ValidateEconomy(config.Economy, maxRank, config.Garage.OpenSlots.Initial, errors);
            ValidateGarage(config.Garage, maxRank, errors);
            ValidateTrack(config.Track, maxRank, errors);

            return errors;
        }

        private bool HasAllParts(GameConfig config, List<ConfigError> errors)
        {
            int errorCount = errors.Count;

            if (config.Vehicles == null)
            {
                errors.Add(new ConfigError(config, "Не назначен каталог машин"));
            }

            if (config.Economy == null)
            {
                errors.Add(new ConfigError(config, "Не назначен конфиг экономики"));
            }

            if (config.Garage == null)
            {
                errors.Add(new ConfigError(config, "Не назначен конфиг гаража"));
            }

            if (config.Track == null)
            {
                errors.Add(new ConfigError(config, "Не назначен конфиг трассы"));
            }

            return errors.Count == errorCount;
        }

        private void ValidateVehicles(VehicleCatalogConfig catalog, LocalizationConfig localization, List<ConfigError> errors)
        {
            if (catalog.Count == 0)
            {
                errors.Add(new ConfigError(catalog, "В каталоге нет машин"));

                return;
            }

            HashSet<string> localizedKeys = CollectLocalizedKeys(localization);
            HashSet<string> vehicleIds = new HashSet<string>();
            long previousIncome = 0;

            for (int i = 0; i < catalog.Count; i++)
            {
                VehicleDefinition vehicle = catalog.GetAt(i);
                int expectedRank = i + FirstRank;
                string label = $"Машина в элементе {i}";

                if (vehicle.Rank != expectedRank)
                {
                    errors.Add(new ConfigError(catalog, $"{label}: ранг {vehicle.Rank}, а нужен {expectedRank}. Ранги идут подряд с 1, по одной машине на ранг"));
                }

                if (string.IsNullOrWhiteSpace(vehicle.Id))
                {
                    errors.Add(new ConfigError(catalog, $"{label}: пустой id"));
                }
                else if (vehicleIds.Add(vehicle.Id) == false)
                {
                    errors.Add(new ConfigError(catalog, $"{label}: id {vehicle.Id} уже занят другой машиной"));
                }

                if (string.IsNullOrWhiteSpace(vehicle.NameKey))
                {
                    errors.Add(new ConfigError(catalog, $"{label}: пустой ключ имени"));
                }
                else if (localizedKeys.Contains(vehicle.NameKey) == false)
                {
                    errors.Add(new ConfigError(catalog, $"{label}: ключа {vehicle.NameKey} нет в LocalizationConfig или у него пустой перевод"));
                }

                if (vehicle.IncomePerSecond <= 0)
                {
                    errors.Add(new ConfigError(catalog, $"{label}: доход {vehicle.IncomePerSecond}, должен быть больше 0"));
                }
                else if (vehicle.IncomePerSecond < previousIncome)
                {
                    errors.Add(new ConfigError(catalog, $"{label}: доход {vehicle.IncomePerSecond} меньше дохода предыдущего ранга {previousIncome}"));
                }

                if (vehicle.GarageSprite == null)
                {
                    errors.Add(new ConfigError(catalog, $"{label}: нет спрайта для гаража"));
                }

                if (vehicle.TrackSprite == null)
                {
                    errors.Add(new ConfigError(catalog, $"{label}: нет спрайта для трассы"));
                }

                previousIncome = Math.Max(previousIncome, vehicle.IncomePerSecond);
            }
        }

        private void ValidateEconomy(EconomyConfig economy, int maxRank, int initialOpenSlots, List<ConfigError> errors)
        {
            if (economy.StartCoins < 0)
            {
                errors.Add(new ConfigError(economy, $"Стартовые монеты {economy.StartCoins}, не могут быть отрицательными"));
            }

            if (economy.StartVehicleRank < FirstRank || economy.StartVehicleRank > maxRank)
            {
                errors.Add(new ConfigError(economy, $"Ранг стартовых машин {economy.StartVehicleRank} вне каталога 1..{maxRank}"));
            }

            if (economy.StartVehicleCount < 0 || economy.StartVehicleCount > initialOpenSlots)
            {
                errors.Add(new ConfigError(economy, $"Стартовых машин {economy.StartVehicleCount}, а открытых мест гаража в начале {initialOpenSlots}"));
            }

            if (economy.BoxRankLag < 0)
            {
                errors.Add(new ConfigError(economy, $"Отставание ранга коробки {economy.BoxRankLag} отрицательное: ранг коробки b = max(1, K − отставание) выйдет за K"));
            }

            if (economy.BoxBasePriceIncomeSeconds <= 0)
            {
                errors.Add(new ConfigError(economy, $"Базовая цена коробки {economy.BoxBasePriceIncomeSeconds} секунд дохода, должна быть больше 0"));
            }

            if (economy.BoxPriceGrowth < 1)
            {
                errors.Add(new ConfigError(economy, $"Рост цены коробки {economy.BoxPriceGrowth}, должен быть не меньше 1"));
            }

            if (economy.BoxPriceGrowthPurchaseLimit < 0)
            {
                errors.Add(new ConfigError(economy, $"Предел покупок для роста цены {economy.BoxPriceGrowthPurchaseLimit}, не может быть отрицательным"));
            }

            if (economy.FreeBoxIntervalSeconds <= 0)
            {
                errors.Add(new ConfigError(economy, $"Интервал бесплатной поставки {economy.FreeBoxIntervalSeconds} секунд, должен быть больше 0"));
            }

            if (economy.OfflineMinAwaySeconds < 0 || economy.OfflineMinAwaySeconds > economy.OfflineMaxAwaySeconds)
            {
                errors.Add(new ConfigError(economy, $"Порог офлайна {economy.OfflineMinAwaySeconds} секунд должен быть от 0 до предела {economy.OfflineMaxAwaySeconds}"));
            }

            if (economy.OfflineMaxAwaySeconds <= 0)
            {
                errors.Add(new ConfigError(economy, $"Предел офлайна {economy.OfflineMaxAwaySeconds} секунд, должен быть больше 0"));
            }

            if (economy.OfflineIncomeRate <= 0 || economy.OfflineIncomeRate > 1)
            {
                errors.Add(new ConfigError(economy, $"Доля офлайн-дохода {economy.OfflineIncomeRate}, должна быть больше 0 и не больше 1"));
            }
        }

        private void ValidateGarage(GarageConfig garage, int maxRank, List<ConfigError> errors)
        {
            if (garage.Columns < 1 || garage.Rows < 1)
            {
                errors.Add(new ConfigError(garage, $"Сетка гаража {garage.Columns} × {garage.Rows}, в ней должен быть хотя бы один слот"));
            }

            int maxOpenSlots = ValidateSchedule(garage, garage.OpenSlots, GarageScheduleName, maxRank, errors);

            if (maxOpenSlots > garage.SlotCount)
            {
                errors.Add(new ConfigError(garage, $"{GarageScheduleName}: открывается {maxOpenSlots}, а слотов в сетке {garage.SlotCount}"));
            }
        }

        private void ValidateTrack(TrackConfig track, int maxRank, List<ConfigError> errors)
        {
            if (track.LapDurationSeconds <= 0)
            {
                errors.Add(new ConfigError(track, $"Круг {track.LapDurationSeconds} секунд, должен быть больше 0"));
            }

            if (track.StraightHalfLength < 0)
            {
                errors.Add(new ConfigError(track, $"Полудлина прямой {track.StraightHalfLength}, не может быть отрицательной"));
            }

            if (track.TurnRadius <= 0)
            {
                errors.Add(new ConfigError(track, $"Радиус поворота {track.TurnRadius}, должен быть больше 0"));
            }

            ValidateSchedule(track, track.Capacity, TrackScheduleName, maxRank, errors);
        }

        private int ValidateSchedule(Object source, CapacitySchedule schedule, string scheduleName, int maxRank, List<ConfigError> errors)
        {
            if (schedule.Initial < 1)
            {
                errors.Add(new ConfigError(source, $"{scheduleName}: в начале {schedule.Initial}, нужно хотя бы одно"));
            }

            int previousRank = FirstRank;
            int previousCapacity = schedule.Initial;

            for (int i = 0; i < schedule.StepCount; i++)
            {
                CapacityStep step = schedule.GetStepAt(i);
                string label = $"{scheduleName}, шаг {i}";

                if (step.Rank <= previousRank)
                {
                    errors.Add(new ConfigError(source, $"{label}: ранг {step.Rank} должен быть больше {previousRank}"));
                }

                if (step.Rank > maxRank)
                {
                    errors.Add(new ConfigError(source, $"{label}: ранг {step.Rank} больше последнего ранга каталога {maxRank}"));
                }

                if (step.Capacity <= previousCapacity)
                {
                    errors.Add(new ConfigError(source, $"{label}: мест {step.Capacity}, должно быть больше {previousCapacity}"));
                }

                previousRank = Math.Max(previousRank, step.Rank);
                previousCapacity = Math.Max(previousCapacity, step.Capacity);
            }

            return previousCapacity;
        }

        private HashSet<string> CollectLocalizedKeys(LocalizationConfig localization)
        {
            HashSet<string> keys = new HashSet<string>();

            for (int i = 0; i < localization.Count; i++)
            {
                LocalizationConfig.Entry entry = localization.GetAt(i);

                if (string.IsNullOrWhiteSpace(entry.Russian) == false && string.IsNullOrWhiteSpace(entry.English) == false)
                {
                    keys.Add(entry.Key);
                }
            }

            return keys;
        }
    }
}
