using _Project.Core.Audio;

namespace _Project.Tests.EditMode.Settings
{
    public class FakeAudioMixerControl : IAudioMixerControl
    {
        public float MusicVolume { get; private set; } = -1f;
        public float SfxVolume { get; private set; } = -1f;

        public void SetMusicVolume(float linearVolume)
        {
            MusicVolume = linearVolume;
        }

        public void SetSfxVolume(float linearVolume)
        {
            SfxVolume = linearVolume;
        }
    }
}
