namespace _Project.Core.Audio
{
    public interface IAudioService
    {
        void PlayOneShot(AudioAsset asset);
        void PlayMusic(AudioAsset asset);
    }
}
