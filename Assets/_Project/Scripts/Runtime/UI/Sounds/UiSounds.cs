using System;
using _Project.Core.Audio;

namespace _Project.UI.Sounds
{
    public class UiSounds
    {
        private readonly IAudioService _audio;
        private readonly GameSoundsConfig _sounds;

        public UiSounds(IAudioService audio, GameSoundsConfig sounds)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        }

        public void PlayClick()
        {
            _audio.PlayOneShot(_sounds.Click);
        }

        public void PlayWindowOpened()
        {
            _audio.PlayOneShot(_sounds.Window);
        }

        public void PlayLevelUp()
        {
            _audio.PlayOneShot(_sounds.LevelUp);
        }
    }
}
