namespace _Project.Features.Track
{
    public interface IRunnerTimeScale
    {
        float CalculateRunnerSeconds(TrackRunner runner, float deltaTime);
    }
}
