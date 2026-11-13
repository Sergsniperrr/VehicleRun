namespace _Project.Core.Randomness
{
    public interface IRandomSource
    {
        int Range(int minInclusive, int maxInclusive);
    }
}
