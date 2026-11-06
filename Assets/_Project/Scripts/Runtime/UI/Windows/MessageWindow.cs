using System;
using _Project.Core.Localization;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Windows
{
    public class MessageWindow : Window
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _message;

        private ILocalizationService _localization;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public void Show(string titleKey, string messageKey)
        {
            _title.text = _localization.Get(titleKey);
            _message.text = _localization.Get(messageKey);
            Open();
        }
    }
}
