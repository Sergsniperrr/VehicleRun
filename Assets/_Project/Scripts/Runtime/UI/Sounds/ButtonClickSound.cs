using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Sounds
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickSound : MonoBehaviour
    {
        private Button _button;
        private UiSounds _sounds;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        [Inject]
        public void Construct(UiSounds sounds)
        {
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        }

        private void OnClicked()
        {
            _sounds.PlayClick();
        }
    }
}
