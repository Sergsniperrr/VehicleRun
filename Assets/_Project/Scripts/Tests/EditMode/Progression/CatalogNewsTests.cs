using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Instances;
using _Project.Features.Progression;
using _Project.Features.Vehicles;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Progression
{
    public class CatalogNewsTests
    {
        private const int StartRank = 1;
        private const int NewRank = 2;
        private const int MilestoneRank = 4;
        private const int SavedBeforeNewsRank = 0;

        private GameConfigFixture _fixture;
        private InstanceIdGenerator _instanceIds;
        private GarageBoard _garage;
        private RankProgress _progress;
        private CollectionMilestones _milestones;
        private CatalogNews _news;
        private int _changeCount;
        private int _seenCount;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _instanceIds = new InstanceIdGenerator();
            _garage = new GarageBoard(_fixture.Garage, _fixture.Vehicles, _instanceIds);
            _progress = new RankProgress(_fixture.Vehicles, _fixture.Economy, _garage);
            _milestones = new CollectionMilestones(_progress, new Wallet(_fixture.Economy), _fixture.Economy);
            _news = new CatalogNews(_progress, _milestones, _fixture.Economy);
            _changeCount = 0;
            _seenCount = 0;
            _news.Changed += () => _changeCount++;
            _news.Seen += () => _seenCount++;
        }

        [TearDown]
        public void TearDown()
        {
            _news.Dispose();
            _progress.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void NewGameHasNoNews()
        {
            _progress.UnlockRank(StartRank);

            Assert.That(_news.HasNews, Is.False);
        }

        [Test]
        public void UnlockedRankIsNews()
        {
            _progress.UnlockRank(StartRank);
            _changeCount = 0;

            PlaceVehicle(0, NewRank);

            Assert.That(_news.HasNews, Is.True);
            Assert.That(_changeCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkSeenClearsVehicleNews()
        {
            _progress.UnlockRank(NewRank);

            _news.MarkSeen();

            Assert.That(_news.HasNews, Is.False);
            Assert.That(_news.SeenRank, Is.EqualTo(NewRank));
            Assert.That(_seenCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkSeenWithoutNewsRaisesNothing()
        {
            _progress.UnlockRank(StartRank);
            _changeCount = 0;

            _news.MarkSeen();

            Assert.That(_seenCount, Is.EqualTo(0));
            Assert.That(_changeCount, Is.EqualTo(0));
        }

        [Test]
        public void ClaimableMilestoneStaysNewsUntilClaimed()
        {
            _progress.UnlockRank(MilestoneRank);
            _news.MarkSeen();

            Assert.That(_news.HasNews, Is.True);

            _milestones.Claim(MilestoneRank);

            Assert.That(_news.HasNews, Is.False);
        }

        [Test]
        public void RestoreKeepsSeenRank()
        {
            _progress.RestoreUnlockedRanks(new[] { StartRank, NewRank });

            _news.Restore(StartRank);

            Assert.That(_news.HasUnseenVehicle, Is.True);
        }

        [Test]
        public void SaveWithoutSeenRankHasNoVehicleNews()
        {
            _progress.RestoreUnlockedRanks(new[] { StartRank, NewRank });

            _news.Restore(SavedBeforeNewsRank);

            Assert.That(_news.HasUnseenVehicle, Is.False);
            Assert.That(_news.SeenRank, Is.EqualTo(NewRank));
        }

        [Test]
        public void NegativeSeenRankIsRejected()
        {
            Assert.That(() => _news.Restore(-1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        private void PlaceVehicle(int slot, int rank)
        {
            _garage.PlaceVehicle(slot, new Vehicle(_instanceIds.Create(), _fixture.Vehicles.GetByRank(rank)));
        }
    }
}
