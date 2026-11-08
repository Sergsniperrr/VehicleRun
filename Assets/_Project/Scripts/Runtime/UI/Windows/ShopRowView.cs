using System;
using _Project.Core.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Windows
{
    public class ShopRowView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _rankBadge;
        [SerializeField] private TMP_Text _rankValue;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Image _buyButtonImage;
        [SerializeField] private Sprite _buySprite;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private GameObject _coin;
        [SerializeField] private TMP_Text _price;
        [SerializeField] private GameObject _lock;
        [SerializeField] private TMP_Text _lockRankValue;

        public event Action<ShopRowView> BuyClicked;

        public int Rank { get; private set; }
        public Vector3 BuyButtonPosition => _buyButton.transform.position;

        private void Awake()
        {
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }

        public void ShowVehicle(VehicleDefinition definition, string vehicleName)
        {
            ShowRank(definition);
            _icon.color = Color.white;
            _rankBadge.gameObject.SetActive(true);
            _name.text = vehicleName;
        }

        public void ShowSilhouette(VehicleDefinition definition)
        {
            ShowRank(definition);
            _icon.color = Color.black;
            _rankBadge.gameObject.SetActive(false);
            _name.text = string.Empty;
        }

        public void ShowPrice(string price)
        {
            _price.text = price;
            ShowSaleState(true);
        }

        public void ShowLock()
        {
            ShowSaleState(false);
        }

        private void ShowRank(VehicleDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            Rank = definition.Rank;
            _icon.sprite = definition.GarageSprite;
            _rankBadge.color = definition.BadgeColor;
            _rankValue.text = definition.Rank.ToString();
            _lockRankValue.text = _rankValue.text;
        }

        private void ShowSaleState(bool isForSale)
        {
            _buyButton.interactable = isForSale;
            _buyButtonImage.sprite = isForSale ? _buySprite : _lockedSprite;
            _coin.SetActive(isForSale);
            _price.gameObject.SetActive(isForSale);
            _lock.SetActive(isForSale == false);
        }

        private void OnBuyButtonClicked()
        {
            BuyClicked?.Invoke(this);
        }
    }
}
