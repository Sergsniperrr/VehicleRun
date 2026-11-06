using System;
using _Project.Features.Economy;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Hud
{
    [RequireComponent(typeof(TMP_Text))]
    public class CoinsView : MonoBehaviour
    {
        private TMP_Text _label;
        private Wallet _wallet;
        private CoinsFormatter _formatter;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _wallet.CoinsChanged += OnCoinsChanged;
            ShowCoins(_wallet.Coins);
        }

        private void OnDestroy()
        {
            _wallet.CoinsChanged -= OnCoinsChanged;
        }

        [Inject]
        public void Construct(Wallet wallet, CoinsFormatter formatter)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        private void ShowCoins(long coins)
        {
            _label.text = _formatter.FormatShort(coins);
        }

        private void OnCoinsChanged(long coins)
        {
            ShowCoins(coins);
        }
    }
}
