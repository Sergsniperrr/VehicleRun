namespace _Project.Features.Garage
{
    public interface IDropRestriction
    {
        bool IsTrackOnly { get; }
        bool IsRecycleOnly { get; }
        bool IsMergeAllowed(int fromSlot, int toSlot);
        bool IsRecycleAllowed(int slot);
    }
}
