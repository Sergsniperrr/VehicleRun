using _Project.Core.Storage;
using JTLStudio.SDK;

namespace _Project.Infrastructure.JTL
{
    public class ExampleStorage : IExampleStorage
    {
        private const string CoinsKey = "coins";

        public int Coins
        {
            get => JTLSDK.Data.GetInt(CoinsKey);
            set => JTLSDK.Data.SetInt(CoinsKey, value);
        }

        public void Save()
        {
            JTLSDK.Data.Save();
        }
    }
}
