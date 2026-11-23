using System;
using _Project.Features.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Windows
{
    public class CatalogMilestoneView : MonoBehaviour
    {
        private static readonly Color ReachedCircleColor = new Color(0.38f, 0.69f, 0f);
        private static readonly Color LockedCircleColor = new Color(0.27f, 0.3f, 0.22f);
        private static readonly Color ClaimedGiftColor = new Color(0.55f, 0.55f, 0.55f);

        [SerializeField] private Image _circle;
        [SerializeField] private Image _gift;
        [SerializeField] private GameObject _lock;
        [SerializeField] private TMP_Text _number;
        [SerializeField] private GameObject _claimedMark;
        [SerializeField] private TMP_Text _rewardLabel;
        [SerializeField] private Button _button;

        public event Action<CatalogMilestoneView> Clicked;

        public int Rank { get; private set; }
        public Vector3 Position => transform.position;

        private void Awake()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }

        public void Show(int rank, MilestoneState state, string rewardText)
        {
            bool isLocked = state == MilestoneState.Locked;
            bool isClaimable = state == MilestoneState.Claimable;
            bool isClaimed = state == MilestoneState.Claimed;

            Rank = rank;
            _number.text = rank.ToString();
            _circle.color = isLocked ? LockedCircleColor : ReachedCircleColor;
            _lock.SetActive(isLocked);
            _number.gameObject.SetActive(isLocked);
            _gift.gameObject.SetActive(isLocked == false);
            _gift.color = isClaimed ? ClaimedGiftColor : Color.white;
            _claimedMark.SetActive(isClaimed);
            _rewardLabel.text = rewardText;
            _rewardLabel.gameObject.SetActive(isClaimable);
            _button.interactable = isClaimable;
        }

        private void OnButtonClicked()
        {
            Clicked?.Invoke(this);
        }
    }
}
