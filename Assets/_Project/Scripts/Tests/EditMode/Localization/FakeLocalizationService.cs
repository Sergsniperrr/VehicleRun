using System.Collections.Generic;
using _Project.Core.Localization;

namespace _Project.Tests.EditMode.Localization
{
    public class FakeLocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, string> _texts = new Dictionary<string, string>();
        private readonly bool _isEnglish;

        public FakeLocalizationService(bool isEnglish)
        {
            _isEnglish = isEnglish;
        }

        public void Set(string key, string text)
        {
            _texts[key] = text;
        }

        public string Get(string key)
        {
            return _texts.TryGetValue(key, out string text) ? text : key;
        }

        public string Localize(string russian, string english)
        {
            return _isEnglish ? english : russian;
        }
    }
}
