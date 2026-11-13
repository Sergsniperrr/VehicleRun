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
        private readonly PlayerLevel _playerLevel;
        private readonly TrackConfig _track;
        private readonly Wallet _wallet;
        private readonly List<TrackRunner> _runners = new List<TrackRunner>();

        public TrackRoster(GarageBoard garage, PlayerLevel playerLevel, TrackConfig track, Wallet wallet)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public event Action<TrackRunner> VehicleSent;
        public event Action<TrackRunner> VehicleReturned;
        public event Action<TrackRunner, long> FinishCrossed;

        public IReadOnlyList<TrackRunner> Runners => _runners;
        public int Count => _runners.Count;
        public int Capacity => _playerLevel.TrackCapacity;

        public bool CanSendToTrack(int slot)
        {
            return CheckSend(slot) == FailureReason.None;
        }

        public CommandResult SendToTrack(int slot)
        {
            FailureReason failureReason = CheckSend(slot);

            if (failureReason != FailureReason.None)
            {
                return Fail(failureReason);
            }

            _garage.TryGetVehicle(slot, out Vehicle vehicle);
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

        public void RestoreRunner(int slot, float distance)
        {
            if (CheckSend(slot) != FailureReason.None)
            {
                throw new InvalidOperationException(nameof(slot));
            }

            _garage.TryGetVehicle(slot, out Vehicle vehicle);
            AddRunner(slot, vehicle, distance % _track.LapLength);
        }

        public bool HasRunner(TrackRunner runner)
        {
            return _runners.Contains(runner);
        }

        public void Advance(float deltaTime)
        {
            ValidateDeltaTime(deltaTime);

            for (int i = 0; i < _runners.Count; i++)
            {
                Drive(_runners[i], deltaTime);
            }
        }

        public void Advance(float deltaTime, IRunnerTimeScale timeScale)
        {
            if (timeScale == null)
            {
                throw new ArgumentNullException(nameof(timeScale));
            }

            ValidateDeltaTime(deltaTime);

            for (int i = 0; i < _runners.Count; i++)
            {
                TrackRunner runner = _runners[i];
                Drive(runner, timeScale.CalculateRunnerSeconds(runner, deltaTime));
            }
        }

        private FailureReason CheckSend(int slot)
        {
            if (_garage.IsSlotOpen(slot) == false)
            {
                return FailureReason.SlotLocked;
            }

            if (_garage.TryGetVehicle(slot, out Vehicle _) == false)
            {
                return FailureReason.NoVehicle;
            }

            if (_garage.IsOnTrack(slot))
            {
                return FailureReason.AlreadyOnTrack;
            }

            if (Count >= Capacity)
            {
                return FailureReason.TrackFull;
            }

            return FailureReason.None;
        }

        private void Send(int slot, Vehicle vehicle)
        {
            TrackRunner runner = AddRunner(slot, vehicle, 0f);
            VehicleSent?.Invoke(runner);
        }

        private TrackRunner AddRunner(int slot, Vehicle vehicle, float distance)
        {
            TrackRunner runner = new TrackRunner(slot, vehicle, distance);
            _garage.MarkOnTrack(slot);
            _runners.Add(runner);

            return runner;
        }

        private void Return(TrackRunner runner)
        {
            _garage.MarkInGarage(runner.Slot);
            _runners.Remove(runner);
            VehicleReturned?.Invoke(runner);
        }

        private void Drive(TrackRunner runner, float seconds)
        {
            int crossings = runner.Drive(runner.Vehicle.Definition.TrackSpeed * seconds, _track.LapLength);

            if (crossings > 0)
            {
                PayForLaps(runner, crossings);
            }
        }

        private void ValidateDeltaTime(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }
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
