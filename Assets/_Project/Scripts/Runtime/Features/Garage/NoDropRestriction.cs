namespace _Project.Features.Garage
{
    public class NoDropRestriction : IDropRestriction
    {
        public bool IsTrackOnly => false;
    }
}
