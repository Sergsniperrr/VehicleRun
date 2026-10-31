namespace _Project.Features.Commands
{
    public enum FailureReason
    {
        None,
        SlotLocked,
        SlotOccupied,
        SameSlot,
        NoVehicle,
        NoBox,
        TargetIsBox,
        RankMismatch,
        MaxRank,
    }
}
