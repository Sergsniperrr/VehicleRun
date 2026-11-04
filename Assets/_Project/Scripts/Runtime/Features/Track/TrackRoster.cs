using System;
using System.Collections.Generic;
using _Project.Core.Track;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Progression;
using _Project.Features.Vehicles;

namespace _Project.Features.Track
{
    public class TrackRoster
    {
        private readonly GarageBoard _garage;
        private readonly RankProgress _progress;
        private readonly TrackConfig _track;
        private readonly Wallet _wallet;
        private readonly List<TrackRunner> _runners = new List<TrackRunner>();

        public TrackRoster(GarageBoard garage, RankProgress progress, TrackConfig track, Wallet wallet)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public event Action<TrackRunner> VehicleSent;
        public event Action<TrackRunner> VehicleReturned;
        public event Action<TrackRunner, long> FinishCrossed;

        public IReadOnlyList<TrackRunner> Runners => _runners;
        public int Count => _runners.Count;
        public int Capacity => _progress.TrackCapacity;

        public CommandResult SendToTrack(int slot)
        {
            if (_garage.IsSlotOpen(slot) == false)
            {
                return Fail(FailureReason.SlotLocked);
            }

            if (_garage.TryGetVehicle(slot, out Vehicle vehicle) == false)
            {
                return Fail(FailureReason.NoVehicle);
            }

            if (_garage.IsOnTrack(slot))
            {
                return Fail(FailureReason.AlreadyOnTrack);
            }

            if (Count >= Capacity)
            {
                return Fail(FailureReason.TrackFull);
            }

            Send(slot, vehicle);

            return Succeed();
        }

        public CommandResult ReturnFromTrack(int slot)
        {
            TrackRunner runner = FindRunner(slot);

            if (runner == null)
            {
                return Fail(FailureReason.NotOnTrack);
            }

            Return(runner);

            return Succeed();
        }

        public CommandResult ApplyBestLineup()
        {
            List<int> bestSlots = SelectBestSlots();

            for (int i = _runners.Count - 1; i >= 0; i--)
            {
                if (bestSlots.Contains(_runners[i].Slot) == false)
                {
                    Return(_runners[i]);
                }
            }

            foreach (int slot in bestSlots)
            {
                if (_garage.IsOnTrack(slot) == false && _garage.TryGetVehicle(slot, out Vehicle vehicle))
                {
                    Send(slot, vehicle);
                }
            }

            return Succeed();
        }

        public void Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            float lapLength = _track.LapLength;

            for (int i = 0; i < _runners.Count; i++)
            {
                TrackRunner runner = _runners[i];
                int crossings = runner.Drive(runner.Vehicle.Definition.TrackSpeed * deltaTime, lapLength);

                if (crossings > 0)
                {
                    PayForLaps(runner, crossings);
                }
            }
        }

        private void Send(int slot, Vehicle vehicle)
        {
            _garage.MarkOnTrack(slot);
            TrackRunner runner = new TrackRunner(slot, vehicle);
            _runners.Add(runner);
            VehicleSent?.Invoke(runner);
        }

        private void Return(TrackRunner runner)
        {
            _garage.MarkInGarage(runner.Slot);
            _runners.Remove(runner);
            VehicleReturned?.Invoke(runner);
        }

        private void PayForLaps(TrackRunner runner, int crossings)
        {
            long payout = runner.Vehicle.Definition.CoinsPerLap * crossings;
            _wallet.Add(payout);
            FinishCrossed?.Invoke(runner, payout);
        }

        private TrackRunner FindRunner(int slot)
        {
            foreach (TrackRunner runner in _runners)
            {
                if (runner.Slot == slot)
                {
                    return runner;
                }
            }

            return null;
        }

        private List<int> SelectBestSlots()
        {
            List<int> candidates = new List<int>();

            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetVehicle(slot, out Vehicle _))
                {
                    candidates.Add(slot);
                }
            }

            candidates.Sort(CompareByIncome);

            if (candidates.Count > Capacity)
            {
                candidates.RemoveRange(Capacity, candidates.Count - Capacity);
            }

            return candidates;
        }

        private int CompareByIncome(int firstSlot, int secondSlot)
        {
            double firstIncome = CalculateIncome(firstSlot);
            double secondIncome = CalculateIncome(secondSlot);

            if (firstIncome != secondIncome)
            {
                return secondIncome.CompareTo(firstIncome);
            }

            bool isFirstOnTrack = _garage.IsOnTrack(firstSlot);

            if (isFirstOnTrack != _garage.IsOnTrack(secondSlot))
            {
                return isFirstOnTrack ? -1 : 1;
            }

            return firstSlot.CompareTo(secondSlot);
        }

        private double CalculateIncome(int slot)
        {
            _garage.TryGetVehicle(slot, out Vehicle vehicle);

            return vehicle.Definition.CalculateAverageIncome(_track.LapLength);
        }

        private CommandResult Succeed()
        {
            return new CommandResult(FailureReason.None);
        }

        private CommandResult Fail(FailureReason reason)
        {
            return new CommandResult(reason);
        }
    }
}
