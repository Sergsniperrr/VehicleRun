using System.Collections.Generic;
using _Project.Core.Audio;

namespace _Project.Tests.EditMode.Sounds
{
    public class FakeAudioService : IAudioService
    {
        private readonly List<AudioAsset> _played = new List<AudioAsset>();
        private readonly List<float> _pitchScales = new List<float>();

        public IReadOnlyList<AudioAsset> Played => _played;
        public IReadOnlyList<float> PitchScales => _pitchScales;

        public void PlayOneShot(AudioAsset asset)
        {
            PlayOneShot(asset, 1f);
        }

        public void PlayOneShot(AudioAsset asset, float pitchScale)
        {
            _played.Add(asset);
            _pitchScales.Add(pitchScale);
        }

        public void PlayMusic(AudioAsset asset)
        {
            _played.Add(asset);
        }

        public void Clear()
        {
            _played.Clear();
            _pitchScales.Clear();
        }
    }
}
