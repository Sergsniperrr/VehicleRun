using System;
using _Project.Core.Economy;

namespace _Project.Features.Economy
{
    public class Wallet
    {
        public Wallet(EconomyConfig economy)
        {
            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            Coins = economy.StartCoins;
        }

        public event Action<long> CoinsChanged;

        public long Coins { get; private set; }
        public double Remainder { get; private set; }

        public void Add(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            double total = Remainder + amount;
            double wholeCoins = Math.Floor(total);
            Remainder = total - wholeCoins;

            if (wholeCoins > 0)
            {
                AddWholeCoins(wholeCoins);
            }
        }

        public bool TrySpend(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (amount > Coins)
            {
                return false;
            }

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);

            return true;
        }

        private void AddWholeCoins(double wholeCoins)
        {
            long room = long.MaxValue - Coins;

            if (wholeCoins >= room)
            {
                Coins = long.MaxValue;
                Remainder = 0;
            }
            else
            {
                Coins += (long)wholeCoins;
            }

            CoinsChanged?.Invoke(Coins);
        }
    }
}
