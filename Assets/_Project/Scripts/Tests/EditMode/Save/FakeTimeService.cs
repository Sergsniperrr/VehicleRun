using _Project.Core.Time;

namespace _Project.Tests.EditMode.Save
{
    public class FakeTimeService : ITimeService
    {
        public float DeltaTime { get; set; }
        public float Time { get; set; }

        public float Scale { get; set; } = 1f;

        public bool CanSetScale { get; set; } = true;
    }
}
