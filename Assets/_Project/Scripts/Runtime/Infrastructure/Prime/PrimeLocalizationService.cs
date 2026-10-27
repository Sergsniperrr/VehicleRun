using System;
using System.Collections.Generic;
using _Project.Core.Localization;
using PrimeGames.SDK;
using PrimeGames.SDK.Common;

namespace _Project.Infrastructure.Prime
{
    public class PrimeLocalizationService : ILocalizationService
    {
        private readonly LocalizationConfig _config;

        private Dictionary<string, string> _texts;
        private bool _russian;
        private bool _built;

        public PrimeLocalizationService(LocalizationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string Get(string key)
        {
            EnsureBuilt();

            return _texts.TryGetValue(key, out string value) ? value : key;
        }

        public string Localize(string russian, string english)
        {
            EnsureBuilt();

            return _russian ? russian : english;
        }

        private void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            _russian = PrimeSDK.Language.Current == LanguageType.Russian;
            _texts = new Dictionary<string, string>();

            for (int i = 0; i < _config.Count; i++)
            {
                LocalizationConfig.Entry entry = _config.GetAt(i);
                _texts[entry.Key] = _russian ? entry.Russian : entry.English;
            }

            _built = true;
        }
    }
}
