using System;
using _Project.Features.Boxes;
using _Project.Features.Vehicles;

namespace _Project.Editor.Balance
{
    public class GreedyPlayer
    {
        private const int NoSlot = -1;
        private const int FirstRank = 1;
        private const double MergeSize = 2;

        private readonly SimulatedGame _game;
        private readonly BalanceScenario _scenario;

        public GreedyPlayer(SimulatedGame game, BalanceScenario scenario)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        }

        public PlayerAction Act()
        {
            if (TryOpenBox())
            {
                return PlayerAction.OpenBox;
            }

            if (TryMerge())
            {
                return PlayerAction.Merge;
            }

            if (TryActivateTurbo())
            {
                return PlayerAction.ActivateTurbo;
            }

            if (TryChangeLineup())
            {
                return PlayerAction.ChangeLineup;
            }

            if (TryBuyBox())
            {
                return PlayerAction.BuyBox;
            }

            if (TryRecycle())
            {
                return PlayerAction.Recycle;
            }

            return IsWaitingForCoins() ? PlayerAction.WaitForCoins : PlayerAction.WaitForBox;
        }

        private bool TryOpenBox()
        {
            for (int slot = 0; slot < _game.Garage.OpenSlotCount; slot++)
            {
                if (_game.Garage.TryGetBox(slot, out Box _))
                {
                    return _game.Gestures.Tap(slot).IsSuccess;
                }
            }

            return false;
        }

        private bool TryMerge()
        {
            int bestFromSlot = NoSlot;
            int bestToSlot = NoSlot;
            int bestRank = 0;

            for (int fromSlot = 0; fromSlot < _game.Garage.OpenSlotCount; fromSlot++)
            {
                for (int toSlot = fromSlot + 1; toSlot < _game.Garage.OpenSlotCount; toSlot++)
                {
                    if (_game.Gestures.CanMerge(fromSlot, toSlot) && GetRank(fromSlot) > bestRank)
                    {
                        bestFromSlot = fromSlot;
                        bestToSlot = toSlot;
                        bestRank = GetRank(fromSlot);
                    }
                }
            }

            if (bestFromSlot == NoSlot)
            {
                return false;
            }

            return _game.Gestures.DropOnSlot(bestFromSlot, bestToSlot).IsSuccess;
        }

        private bool TryActivateTurbo()
        {
            if (_scenario.UsesTurbo == false)
            {
                return false;
            }

            return _game.Turbo.StartTurbo().IsSuccess;
        }

        private bool TryChangeLineup()
        {
            int bestGarageSlot = FindBestGarageVehicle();

            if (bestGarageSlot == NoSlot)
            {
                return false;
            }

            bool hasTrackRoom = _game.Roster.Count < _game.Roster.Capacity;
            int worstTrackSlot = FindWorstTrackVehicle();
            bool canImprove = worstTrackSlot != NoSlot && GetIncome(bestGarageSlot) > GetIncome(worstTrackSlot);

            if (hasTrackRoom == false && canImprove == false)
            {
                return false;
            }

            if (_scenario.UsesBestLineup)
            {
                return _game.Roster.ApplyBestLineup().IsSuccess;
            }

            if (hasTrackRoom)
            {
                return _game.Gestures.DropOnTrack(bestGarageSlot).IsSuccess;
            }

            return _game.Gestures.Tap(worstTrackSlot).IsSuccess;
        }

        private bool TryBuyBox()
        {
            if (_scenario.BuysBoxes == false || HasFreeSlot() == false)
            {
                return false;
            }

            int rank = ChooseBoxRank();

            if (_game.Delivery.GetMissingCoins(rank) > 0)
            {
                return false;
            }

            return _game.Delivery.BuyBox(rank).IsSuccess;
        }

        private int ChooseBoxRank()
        {
            int boxRank = _game.Progress.BoxRank;

            if (_scenario.BuysCheapestRank == false)
            {
                return boxRank;
            }

            int cheapestRank = boxRank;
            double cheapestUnitPrice = CalculateUnitPrice(boxRank);

            for (int rank = FirstRank; rank < boxRank; rank++)
            {
                double unitPrice = CalculateUnitPrice(rank);

                if (unitPrice < cheapestUnitPrice)
                {
                    cheapestRank = rank;
                    cheapestUnitPrice = unitPrice;
                }
            }

            return cheapestRank;
        }

        private double CalculateUnitPrice(int rank)
        {
            double firstRankVehicles = Math.Pow(MergeSize, rank - FirstRank);

            return _game.Delivery.GetPrice(rank) / firstRankVehicles;
        }

        private bool TryRecycle()
        {
            if (HasFreeSlot())
            {
                return false;
            }

            int worstGarageSlot = FindWorstGarageVehicle();

            if (worstGarageSlot == NoSlot)
            {
                return false;
            }

            return _game.Gestures.Recycle(worstGarageSlot).IsSuccess;
        }

        private bool IsWaitingForCoins()
        {
            return _scenario.BuysBoxes && HasFreeSlot() && _game.Delivery.GetMissingCoins(ChooseBoxRank()) > 0;
        }

        private bool HasFreeSlot()
        {
            return _game.Garage.TryFindFreeSlot(out int _);
        }

        private int FindBestGarageVehicle()
        {
            return FindVehicleSlot(false, true);
        }

        private int FindWorstGarageVehicle()
        {
            return FindVehicleSlot(false, false);
        }

        private int FindWorstTrackVehicle()
        {
            return FindVehicleSlot(true, false);
        }

        private int FindVehicleSlot(bool isOnTrack, bool isBest)
        {
            int foundSlot = NoSlot;

            for (int slot = 0; slot < _game.Garage.OpenSlotCount; slot++)
            {
                if (_game.Garage.TryGetVehicle(slot, out Vehicle _) == false || _game.Garage.IsOnTrack(slot) != isOnTrack)
                {
                    continue;
                }

                if (foundSlot == NoSlot || IsBetterCandidate(slot, foundSlot, isBest))
                {
                    foundSlot = slot;
                }
            }

            return foundSlot;
        }

        private bool IsBetterCandidate(int slot, int foundSlot, bool isBest)
        {
            double income = GetIncome(slot);
            double foundIncome = GetIncome(foundSlot);

            return isBest ? income > foundIncome : income < foundIncome;
        }

        private int GetRank(int slot)
        {
            _game.Garage.TryGetVehicle(slot, out Vehicle vehicle);

            return vehicle.Rank;
        }

        private double GetIncome(int slot)
        {
            _game.Garage.TryGetVehicle(slot, out Vehicle vehicle);

            return vehicle.Definition.CalculateAverageIncome(_game.LapLength);
        }
    }
}
