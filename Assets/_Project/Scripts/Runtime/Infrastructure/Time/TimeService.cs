using _Project.Core.Time;

namespace _Project.Infrastructure.Time
{
    public class TimeService : ITimeService
    {
        public float DeltaTime => UnityEngine.Time.deltaTime;

        public float Time => UnityEngine.Time.time;
    }
}
