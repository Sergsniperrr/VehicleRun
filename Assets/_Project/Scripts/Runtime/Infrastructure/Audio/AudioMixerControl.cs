using System;
using _Project.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace _Project.Infrastructure.Audio
{
    public class AudioMixerControl : IAudioMixerControl
    {
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SFXVolume";
        private const float MinLinearVolume = 0.0001f;
        private const float DecibelsPerDecade = 20f;

        private readonly AudioMixer _mixer;

        public AudioMixerControl(AudioMixer mixer)
        {
            if (mixer == null)
            {
                throw new ArgumentNullException(nameof(mixer));
            }

            _mixer = mixer;
        }

        public void SetMusicVolume(float linearVolume)
        {
            _mixer.SetFloat(MusicVolumeParameter, ToDecibels(linearVolume));
        }

        public void SetSfxVolume(float linearVolume)
        {
            _mixer.SetFloat(SfxVolumeParameter, ToDecibels(linearVolume));
        }

        private float ToDecibels(float linearVolume)
        {
            return Mathf.Log10(Mathf.Max(linearVolume, MinLinearVolume)) * DecibelsPerDecade;
        }
    }
}
