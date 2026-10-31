namespace _Project.Features.Instances
{
    public class InstanceIdGenerator
    {
        private const long FirstId = 1;

        private long _nextId = FirstId;

        public long Create()
        {
            long id = _nextId;
            _nextId++;

            return id;
        }
    }
}
