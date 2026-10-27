namespace _Project.Core.Storage
{
    public interface IExampleStorage
    {
        int Coins { get; set; }

        void Save();
    }
}
