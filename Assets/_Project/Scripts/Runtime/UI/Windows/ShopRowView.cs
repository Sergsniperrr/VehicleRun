using System;
using _Project.Core.Vehicles;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Windows
{
    public class ShopRowView : MonoBehaviour
    {
        private const float PulseScale = 1.12f;
        private const float PulseSeconds = 0.5f;

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
        [SerializeField] private GameObject _rewardAd;
        [SerializeField] private Button _rewardButton;
        [SerializeField] private TMP_Text _rewardLabel;

        private Tween _rewardPulse;

        public event Action<ShopRowView> BuyClicked;
        public event Action<ShopRowView> RewardClicked;

        public int Rank { get; private set; }
        public Vector3 BuyButtonPosition => _buyButton.transform.position;

        private void Awake()
        {
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
            _rewardButton.onClick.AddListener(OnRewardButtonClicked);
            _rewardAd.SetActive(false);
        }

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            _rewardButton.onClick.RemoveListener(OnRewardButtonClicked);
            _rewardPulse?.Kill();
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

        public void ShowPrice(string price, bool isAffordable)
        {
            _price.text = price;
            ShowSaleState(true);
            _buyButton.interactable = isAffordable;
            _buyButtonImage.sprite = isAffordable ? _buySprite : _lockedSprite;
        }

        public void ShowLock()
        {
            ShowSaleState(false);
        }

        public void ShowReward(string label)
        {
            _rewardLabel.text = label;
            _rewardAd.SetActive(true);
            StartPulse();
        }

        public void HideReward()
        {
            _rewardAd.SetActive(false);
            _rewardPulse?.Kill();
            _rewardPulse = null;
            _rewardButton.transform.localScale = Vector3.one;
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

        private void StartPulse()
        {
            if (_rewardPulse != null)
            {
                return;
            }

            _rewardButton.transform.localScale = Vector3.one;
            _rewardPulse = _rewardButton.transform
                .DOScale(PulseScale, PulseSeconds)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void OnBuyButtonClicked()
        {
            BuyClicked?.Invoke(this);
        }

        private void OnRewardButtonClicked()
        {
            RewardClicked?.Invoke(this);
        }
    }
}
