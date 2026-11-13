namespace _Project.Core.Audio
{
    public interface IAudioService
    {
        void PlayOneShot(AudioAsset asset);
        void PlayOneShot(AudioAsset asset, float pitchScale);
        void PlayMusic(AudioAsset asset);
    }
}
