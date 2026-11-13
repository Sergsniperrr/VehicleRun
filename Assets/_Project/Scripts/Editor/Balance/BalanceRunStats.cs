using System;
using System.Collections.Generic;
using _Project.Features.Boxes;
using _Project.Features.Track;
using _Project.Features.Vehicles;

namespace _Project.Editor.Balance
{
    public class BalanceRunStats : IDisposable
    {
        private readonly SimulatedGame _game;
        private readonly Dictionary<int, double> _rankSeconds = new Dictionary<int, double>();

        public BalanceRunStats(SimulatedGame game)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));

            for (int rank = 1; rank <= game.Progress.MaxRank; rank++)
            {
                _rankSeconds[rank] = game.ActiveSeconds;
            }

            _game.Garage.VehicleMerged += OnVehicleMerged;
            _game.Garage.BoxPlaced += OnBoxPlaced;
            _game.Garage.VehicleRecycled += OnVehicleRecycled;
            _game.Roster.FinishCrossed += OnFinishCrossed;
            _game.Progress.RankUnlocked += OnRankUnlocked;
            _game.Garage.SlotsUnlocked += OnSlotsUnlocked;
        }

        public double? FirstMergeSeconds { get; private set; }
        public double? FirstIncomeSeconds { get; private set; }
        public double? AllSlotsSeconds { get; private set; }
        public int FreeBoxCount { get; private set; }
        public int BoughtBoxCount { get; private set; }
        public int RecycledVehicleCount { get; private set; }
        public double CoinsWaitSeconds { get; private set; }
        public double BoxWaitSeconds { get; private set; }
        public int SessionCount { get; private set; }
        public double OfflineSeconds { get; private set; }
        public long OfflineCoins { get; private set; }
        public double ActiveSeconds { get; private set; }
        public long FinalCoins { get; private set; }
        public int FinalMaxRank { get; private set; }
        public int FinalLevel { get; private set; }

        public bool TryGetRankSeconds(int rank, out double seconds)
        {
            return _rankSeconds.TryGetValue(rank, out seconds);
        }

        public void RecordAction(PlayerAction action, float seconds)
        {
            if (action == PlayerAction.WaitForCoins)
            {
                CoinsWaitSeconds += seconds;
            }
            else if (action == PlayerAction.WaitForBox)
            {
                BoxWaitSeconds += seconds;
            }
        }

        public void RecordOfflineIncome(long coins, float awaySeconds)
        {
            SessionCount++;
            OfflineSeconds += awaySeconds;
            OfflineCoins += coins;
        }

        public void Complete()
        {
            ActiveSeconds = _game.ActiveSeconds;
            FinalCoins = _game.Wallet.Coins;
            FinalMaxRank = _game.Progress.MaxRank;
            FinalLevel = _game.PlayerLevel.Level;
        }

        public void Dispose()
        {
            _game.Garage.VehicleMerged -= OnVehicleMerged;
            _game.Garage.BoxPlaced -= OnBoxPlaced;
            _game.Garage.VehicleRecycled -= OnVehicleRecycled;
            _game.Roster.FinishCrossed -= OnFinishCrossed;
            _game.Progress.RankUnlocked -= OnRankUnlocked;
            _game.Garage.SlotsUnlocked -= OnSlotsUnlocked;
        }

        private void OnVehicleMerged(int fromSlot, int toSlot, Vehicle result)
        {
            FirstMergeSeconds = FirstMergeSeconds ?? _game.ActiveSeconds;
        }

        private void OnBoxPlaced(int slot, Box box)
        {
            if (box.Source == BoxSource.Free)
            {
                FreeBoxCount++;
            }
            else if (box.Source == BoxSource.Purchased)
            {
                BoughtBoxCount++;
            }
        }

        private void OnVehicleRecycled(int slot, Vehicle vehicle)
        {
            RecycledVehicleCount++;
        }

        private void OnFinishCrossed(TrackRunner runner, long payout)
        {
            FirstIncomeSeconds = FirstIncomeSeconds ?? _game.ActiveSeconds;
        }

        private void OnSlotsUnlocked(int openSlotCount)
        {
            if (openSlotCount == _game.Garage.SlotCount)
            {
                AllSlotsSeconds = AllSlotsSeconds ?? _game.ActiveSeconds;
            }
        }

        private void OnRankUnlocked(int rank)
        {
            if (_rankSeconds.ContainsKey(rank) == false)
            {
                _rankSeconds[rank] = _game.ActiveSeconds;
            }
        }
    }
}
