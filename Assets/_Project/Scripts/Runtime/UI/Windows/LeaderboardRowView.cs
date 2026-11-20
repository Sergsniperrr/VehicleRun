using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Windows
{
    public class LeaderboardRowView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Sprite _playerSprite;
        [SerializeField] private Sprite _currentPlayerSprite;
        [SerializeField] private Image _avatar;
        [SerializeField] private Sprite _defaultAvatar;
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private TMP_Text _playerName;
        [SerializeField] private TMP_Text _score;

        public void Show(int rank, string playerName, string score, bool isCurrentPlayer)
        {
            _background.sprite = isCurrentPlayer ? _currentPlayerSprite : _playerSprite;
            _avatar.sprite = _defaultAvatar;
            _rank.text = rank.ToString();
            _playerName.text = playerName;
            _score.text = score;
            gameObject.SetActive(true);
        }

        public void SetAvatar(Sprite avatar)
        {
            if (avatar == null)
            {
                return;
            }

            _avatar.sprite = avatar;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
