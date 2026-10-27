using _Project.Core.Storage;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class ExampleStorage : IExampleStorage
    {
        private const string CoinsKey = "coins";

        public int Coins
        {
            get => PrimeSDK.Data.GetInt(CoinsKey, 0);
            set => PrimeSDK.Data.SetInt(CoinsKey, value, important: false);
        }

        public void Save()
        {
            PrimeSDK.Data.Save();
        }
    }
}
