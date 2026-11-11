using System.Collections.Generic;
using _Project.Core.Audio;

namespace _Project.Tests.EditMode.Sounds
{
    public class FakeAudioService : IAudioService
    {
        private readonly List<AudioAsset> _played = new List<AudioAsset>();

        public IReadOnlyList<AudioAsset> Played => _played;

        public void PlayOneShot(AudioAsset asset)
        {
            _played.Add(asset);
        }

        public void PlayMusic(AudioAsset asset)
        {
            _played.Add(asset);
        }
    }
}
