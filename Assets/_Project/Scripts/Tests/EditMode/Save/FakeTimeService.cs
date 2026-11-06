using _Project.Core.Time;

namespace _Project.Tests.EditMode.Save
{
    public class FakeTimeService : ITimeService
    {
        public float DeltaTime { get; set; }
        public float Time { get; set; }
    }
}
