namespace _Project.Core.Audio
{
    public interface IAudioMixerControl
    {
        void SetMusicVolume(float linearVolume);

        void SetSfxVolume(float linearVolume);
    }
}
