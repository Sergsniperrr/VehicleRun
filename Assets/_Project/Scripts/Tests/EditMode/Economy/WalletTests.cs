using System;
using System.Collections.Generic;
using _Project.Features.Economy;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Economy
{
    public class WalletTests
    {
        private const double RemainderTolerance = 1e-9;

        private GameConfigFixture _fixture;
        private Wallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _wallet = new Wallet(_fixture.Economy);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void NewWalletHoldsStartCoins()
        {
            Assert.That(_wallet.Coins, Is.EqualTo(_fixture.Economy.StartCoins));
            Assert.That(_wallet.Remainder, Is.EqualTo(0));
        }

        [Test]
        public void FractionalIncomeAccumulatesIntoWholeCoins()
        {
            long startCoins = _wallet.Coins;

            _wallet.Add(0.4);
            _wallet.Add(0.4);

            Assert.That(_wallet.Coins, Is.EqualTo(startCoins));

            _wallet.Add(0.4);

            Assert.That(_wallet.Coins, Is.EqualTo(startCoins + 1));
            Assert.That(_wallet.Remainder, Is.EqualTo(0.2).Within(RemainderTolerance));
        }

        [Test]
        public void CoinsChangedIsRaisedOnlyWhenWholeCoinsChange()
        {
            long startCoins = _wallet.Coins;
            List<long> balances = new List<long>();
            _wallet.CoinsChanged += balance => balances.Add(balance);

            _wallet.Add(0.5);
            _wallet.Add(0.5);
            _wallet.Add(22);

            Assert.That(balances, Is.EqualTo(new[] { startCoins + 1, startCoins + 23 }));
        }

        [Test]
        public void SpendingRemovesCoins()
        {
            long startCoins = _wallet.Coins;
            List<long> balances = new List<long>();
            _wallet.CoinsChanged += balance => balances.Add(balance);

            bool isSpent = _wallet.TrySpend(12);

            Assert.That(isSpent, Is.True);
            Assert.That(_wallet.Coins, Is.EqualTo(startCoins - 12));
            Assert.That(balances, Is.EqualTo(new[] { startCoins - 12 }));
        }

        [Test]
        public void SpendingWholeBalanceIsAllowed()
        {
            bool isSpent = _wallet.TrySpend(_wallet.Coins);

            Assert.That(isSpent, Is.True);
            Assert.That(_wallet.Coins, Is.EqualTo(0));
        }

        [Test]
        public void SpendingMoreThanBalanceIsRejectedWithoutChanges()
        {
            long startCoins = _wallet.Coins;
            List<long> balances = new List<long>();
            _wallet.CoinsChanged += balance => balances.Add(balance);

            bool isSpent = _wallet.TrySpend(startCoins + 1);

            Assert.That(isSpent, Is.False);
            Assert.That(_wallet.Coins, Is.EqualTo(startCoins));
            Assert.That(balances, Is.Empty);
        }

        [Test]
        public void BalanceStopsAtMaximumInsteadOfOverflow()
        {
            _wallet.Add(long.MaxValue);
            _wallet.Add(1000.5);

            Assert.That(_wallet.Coins, Is.EqualTo(long.MaxValue));
            Assert.That(_wallet.Remainder, Is.EqualTo(0));
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidIncomeThrows(double amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.Add(amount));
        }

        [Test]
        public void NegativeSpendingThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.TrySpend(-1));
        }
    }
}
