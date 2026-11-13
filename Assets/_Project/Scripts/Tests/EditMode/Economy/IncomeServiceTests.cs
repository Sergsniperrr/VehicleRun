using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Track;
using _Project.Features.Turbo;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Economy
{
    public class IncomeServiceTests
    {
        private const double Tolerance = 0.01;

        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private RankProgress _progress;
        private PlayerLevel _playerLevel;
        private TrackRoster _roster;
        private TurboService _turbo;
        private IncomeService _income;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _garage);
            Wallet wallet = new Wallet(_fixture.Economy);
            _playerLevel = new PlayerLevel(_fixture.PlayerLevel, _fixture.Track, _garage, wallet);
            _roster = new TrackRoster(_garage, _playerLevel, _fixture.Track, wallet);
            _turbo = new TurboService(_fixture.Turbo, _progress, _roster);
            _income = new IncomeService(_roster, _fixture.Track, _turbo);
        }

        [TearDown]
        public void TearDown()
        {
            _playerLevel.Dispose();
            _progress.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void EmptyTrackHasNoIncome()
        {
            PlaceVehicle(0, 4);

            Assert.That(_income.CalculateIncomePerSecond(), Is.EqualTo(0));
        }

        [Test]
        public void TwoRankFourVehiclesEarnAboutEightAndHalf()
        {
            PlaceVehicle(0, 4);
            PlaceVehicle(1, 4);
            _roster.SendToTrack(0);
            _roster.SendToTrack(1);

            Assert.That(_income.CalculateIncomePerSecond(), Is.EqualTo(8.52).Within(Tolerance));
        }

        [Test]
        public void ReturnedVehicleStopsEarning()
        {
            PlaceVehicle(0, 4);
            PlaceVehicle(1, 4);
            _roster.SendToTrack(0);
            _roster.SendToTrack(1);

            _roster.ReturnFromTrack(1);

            Assert.That(_income.CalculateIncomePerSecond(), Is.EqualTo(4.26).Within(Tolerance));
        }

        [Test]
        public void TurboMultipliesShownIncomeOnly()
        {
            PlaceVehicle(0, 4);
            PlaceVehicle(1, 4);
            _roster.SendToTrack(0);
            _roster.SendToTrack(1);

            _turbo.StartTurbo();

            double expectedIncome = 8.52 * _fixture.Turbo.SpeedMultiplier;
            Assert.That(_income.CalculateIncomePerSecond(), Is.EqualTo(expectedIncome).Within(Tolerance));
            Assert.That(_income.CalculateIncomePerSecondWithoutTurbo(), Is.EqualTo(8.52).Within(Tolerance));
        }

        private void PlaceVehicle(int slot, int rank)
        {
            _garage.PlaceVehicle(slot, new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank)));
        }
    }
}
