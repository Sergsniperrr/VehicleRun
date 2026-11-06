using _Project.UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Hud
{
    public class WindowButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Window _window;

        private void Awake()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            _window.Open();
        }
    }
}
