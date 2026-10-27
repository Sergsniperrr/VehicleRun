using System;
using _Project.Core.Localization;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _label;
        private ILocalizationService _localization;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _label.text = _localization.Get(_key);
        }
    }
}
