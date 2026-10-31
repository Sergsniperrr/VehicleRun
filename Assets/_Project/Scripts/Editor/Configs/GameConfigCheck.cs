using System.Collections.Generic;
using _Project.Core.Configs;
using _Project.Core.Localization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Editor.Configs
{
    public class GameConfigCheck
    {
        private const string LogPrefix = "[GameConfig]";

        private readonly GameConfigValidator _validator = new GameConfigValidator();

        public IReadOnlyList<ConfigError> Run()
        {
            List<ConfigError> errors = new List<ConfigError>();
            List<LocalizationConfig> localizations = LoadAssets<LocalizationConfig>();
            List<GameConfig> configs = LoadAssets<GameConfig>();

            if (localizations.Count != 1)
            {
                errors.Add(new ConfigError(null, $"Нужен ровно один LocalizationConfig, в проекте их {localizations.Count}"));

                return errors;
            }

            if (configs.Count == 0)
            {
                errors.Add(new ConfigError(null, "В проекте нет ни одного GameConfig"));

                return errors;
            }

            foreach (GameConfig config in configs)
            {
                errors.AddRange(_validator.Validate(config, localizations[0]));
            }

            return errors;
        }

        public void Report(IReadOnlyList<ConfigError> errors)
        {
            if (errors.Count == 0)
            {
                Debug.Log($"{LogPrefix} Конфиги в порядке");

                return;
            }

            foreach (ConfigError error in errors)
            {
                Debug.LogError($"{LogPrefix} {error.Message}", error.Source);
            }
        }

        private List<T> LoadAssets<T>() where T : Object
        {
            List<T> assets = new List<T>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                assets.Add(AssetDatabase.LoadAssetAtPath<T>(path));
            }

            return assets;
        }
    }
}
