using System;
using _Project.Features.Settings;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Windows
{
    public class SettingsWindow : Window
    {
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        private AudioVolumeSettings _audio;
        private bool _isApplying;

        protected override void Awake()
        {
            base.Awake();
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }

        [Inject]
        public void Construct(AudioVolumeSettings audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        public override void Show()
        {
            _isApplying = true;
            _musicSlider.value = _audio.MusicVolume;
            _sfxSlider.value = _audio.SfxVolume;
            _isApplying = false;

            base.Show();
        }

        private void OnMusicChanged(float value)
        {
            if (_isApplying)
            {
                return;
            }

            _audio.SetMusicVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            if (_isApplying)
            {
                return;
            }

            _audio.SetSfxVolume(value);
        }
    }
}
