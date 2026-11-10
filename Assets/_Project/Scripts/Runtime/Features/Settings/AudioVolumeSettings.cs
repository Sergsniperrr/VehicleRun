using System;
using _Project.Core.Audio;
using _Project.Core.Save;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Features.Settings
{
    public class AudioVolumeSettings : IInitializable
    {
        private const float DefaultVolume = 1f;

        private readonly ISettingsStorage _storage;
        private readonly IAudioMixerControl _mixer;

        private float _musicVolume = DefaultVolume;
        private float _sfxVolume = DefaultVolume;

        public AudioVolumeSettings(ISettingsStorage storage, IAudioMixerControl mixer)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
        }

        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;

        public void Initialize()
        {
            if (_storage.TryRead(out SettingsSave settings))
            {
                _musicVolume = Mathf.Clamp01(settings.MusicVolume);
                _sfxVolume = Mathf.Clamp01(settings.SfxVolume);
            }

            _mixer.SetMusicVolume(_musicVolume);
            _mixer.SetSfxVolume(_sfxVolume);
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            _mixer.SetMusicVolume(_musicVolume);
            Save();
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            _mixer.SetSfxVolume(_sfxVolume);
            Save();
        }

        private void Save()
        {
            _storage.Write(new SettingsSave { MusicVolume = _musicVolume, SfxVolume = _sfxVolume });
        }
    }
}
