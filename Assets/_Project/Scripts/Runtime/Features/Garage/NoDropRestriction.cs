namespace _Project.Features.Garage
{
    public class NoDropRestriction : IDropRestriction
    {
        public bool IsTrackOnly => false;
        public bool IsRecycleOnly => false;

        public bool IsMergeAllowed(int fromSlot, int toSlot)
        {
            return true;
        }

        public bool IsRecycleAllowed(int slot)
        {
            return true;
        }
    }
}
