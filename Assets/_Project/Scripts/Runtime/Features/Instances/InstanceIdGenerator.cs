using System;

namespace _Project.Features.Instances
{
    public class InstanceIdGenerator
    {
        public const long FirstId = 1;

        private long _nextId = FirstId;

        public long NextId => _nextId;

        public long Create()
        {
            long id = _nextId;
            _nextId++;

            return id;
        }

        public void Restore(long nextId)
        {
            if (nextId < _nextId)
            {
                throw new ArgumentOutOfRangeException(nameof(nextId));
            }

            _nextId = nextId;
        }
    }
}
