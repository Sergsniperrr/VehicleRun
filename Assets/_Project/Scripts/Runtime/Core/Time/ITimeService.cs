namespace _Project.Core.Time
{
    public interface ITimeService
    {
        float DeltaTime { get; }

        float Time { get; }

        float Scale { get; set; }

        bool CanSetScale { get; }
    }
}
