using _Project.Core.Time;
using JTLStudio.SDK;

namespace _Project.Infrastructure.Time
{
    public class TimeService : ITimeService
    {
        public float DeltaTime => UnityEngine.Time.deltaTime;

        public float Time => UnityEngine.Time.time;

        public float Scale
        {
            get => JTLSDK.Time.Scale;
            set => JTLSDK.Time.Scale = value;
        }

        public bool CanSetScale => JTLSDK.IsCreated;
    }
}
