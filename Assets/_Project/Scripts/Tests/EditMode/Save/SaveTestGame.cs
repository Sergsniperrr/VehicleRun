using System;
using _Project.Features.Boxes;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Save;
using _Project.Features.Track;
using _Project.Features.Turbo;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;

namespace _Project.Tests.EditMode.Save
{
    public class SaveTestGame : IDisposable
    {
        private readonly GameConfigFixture _fixture;

        public SaveTestGame(GameConfigFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            InstanceIds = new InstanceIdGenerator();
            Garage = new GarageBoard(fixture.Garage, fixture.Vehicles, InstanceIds);
            Progress = new RankProgress(fixture.Vehicles, fixture.Economy, fixture.Track, Garage);
            Wallet = new Wallet(fixture.Economy);
            Pricing = new BoxPricing(fixture.Economy, fixture.Vehicles);
            Delivery = new BoxDelivery(fixture.Economy, fixture.Vehicles, Garage, Progress, Wallet, Pricing, InstanceIds);
            Roster = new TrackRoster(Garage, Progress, fixture.Track, Wallet);
            Turbo = new TurboService(fixture.Turbo, Progress, Roster);
            Milestones = new CollectionMilestones(Progress, Wallet, fixture.Economy);
            Factory = new GameSaveFactory(fixture.Config, InstanceIds, Garage, Progress, Wallet, Pricing, Delivery, Roster, Turbo, Milestones);
            Restorer = new GameStateRestorer(fixture.Vehicles, InstanceIds, Garage, Progress, Wallet, Pricing, Delivery, Roster, Turbo, Milestones);
        }

        public InstanceIdGenerator InstanceIds { get; }
        public GarageBoard Garage { get; }
        public RankProgress Progress { get; }
        public Wallet Wallet { get; }
        public BoxPricing Pricing { get; }
        public BoxDelivery Delivery { get; }
        public TrackRoster Roster { get; }
        public TurboService Turbo { get; }
        public CollectionMilestones Milestones { get; }
        public GameSaveFactory Factory { get; }
        public GameStateRestorer Restorer { get; }

        public Vehicle PlaceVehicle(int slot, int rank)
        {
            Vehicle vehicle = new Vehicle(InstanceIds.Create(), _fixture.Vehicles.GetByRank(rank));
            Garage.PlaceVehicle(slot, vehicle);

            return vehicle;
        }

        public Box PlaceBox(int slot, int rank, BoxSource source)
        {
            Box box = new Box(InstanceIds.Create(), source, _fixture.Vehicles.GetByRank(rank));
            Garage.PlaceBox(slot, box);

            return box;
        }

        public void Dispose()
        {
            Progress.Dispose();
        }
    }
}
