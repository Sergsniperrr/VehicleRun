using System;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Tutorial;
using _Project.Tests.EditMode.Configs;
using _Project.Tests.EditMode.Save;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Tutorial
{
    public class TutorialServiceTests
    {
        private const int FirstRank = 1;
        private const int SecondRank = 2;

        private GameConfigFixture _fixture;
        private SaveTestGame _game;
        private TutorialService _tutorial;
        private int _stepChangedCount;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _game = new SaveTestGame(_fixture);
            _tutorial = _game.Tutorial;
            _stepChangedCount = 0;
            _tutorial.StepChanged += OnStepChanged;
        }

        [TearDown]
        public void TearDown()
        {
            _tutorial.StepChanged -= OnStepChanged;
            _game.Dispose();
            _fixture.Dispose();
        }

        [Test]
        public void NewGameBeginsAtSendToTrackStep()
        {
            _game.PlaceVehicle(0, FirstRank);
            _game.PlaceVehicle(1, FirstRank);

            _tutorial.Begin();

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.SendToTrackStep));
            Assert.That(_tutorial.IsActive, Is.True);
            Assert.That(_tutorial.CanSkip, Is.False);
        }

        [Test]
        public void BeginTwiceThrows()
        {
            _tutorial.Begin();

            Assert.Throws<InvalidOperationException>(() => _tutorial.Begin());
        }

        [Test]
        public void FullHappyPathReachesCompletion()
        {
            _game.PlaceVehicle(0, FirstRank);
            _game.PlaceVehicle(1, FirstRank);
            _tutorial.Begin();

            _game.Roster.SendToTrack(0);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.OpenBoxStep));
            int boxSlot = FindBoxSlot();

            _game.Garage.OpenBox(boxSlot);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.MergeStep));

            int left = FindVehicleSlot(FirstRank, 0);
            int right = FindVehicleSlot(FirstRank, left + 1);
            _game.Garage.Merge(left, right);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.UpgradeTrackStep));

            _game.Roster.ReturnFromTrack(0);
            int upgradeSlot = FindVehicleSlot(SecondRank, 0);
            _game.Roster.SendToTrack(upgradeSlot);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.BuyBoxStep));

            _game.Wallet.Add(_game.Delivery.BoxPrice);
            _game.Delivery.BuyBox();
            int purchasedSlot = FindBoxSlot();
            _game.Garage.OpenBox(purchasedSlot);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CatalogStep));

            _game.News.MarkSeen();
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CompletedStep));
            Assert.That(_tutorial.IsActive, Is.False);
        }

        [Test]
        public void SendToTrackGrantsTutorialBox()
        {
            _game.PlaceVehicle(0, FirstRank);
            _tutorial.Begin();

            _game.Roster.SendToTrack(0);

            Assert.That(FindBoxSlot(), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TutorialBlocksFreeBoxTimer()
        {
            _game.PlaceVehicle(0, FirstRank);
            _tutorial.Begin();

            Assert.That(_game.Tutorial.IsActive, Is.True);
        }

        [Test]
        public void RecycledTutorialBoxIsReissued()
        {
            _game.PlaceVehicle(0, FirstRank);
            _tutorial.Begin();
            _game.Roster.SendToTrack(0);
            int boxSlot = FindBoxSlot();

            _game.Garage.Recycle(boxSlot);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.OpenBoxStep));
            Assert.That(FindBoxSlot(), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OpenBoxWithoutPairGrantsAnotherBox()
        {
            _game.PlaceVehicle(0, FirstRank);
            _tutorial.Begin();
            _game.Roster.SendToTrack(0);
            int boxSlot = FindBoxSlot();

            _game.Garage.OpenBox(boxSlot);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.MergeStep));
            Assert.That(FindBoxSlot(), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void PurchasedBoxMustBeOpenedToFinishBuyStep()
        {
            ReachBuyBoxStep();
            _game.Wallet.Add(_game.Delivery.BoxPrice);

            _game.Delivery.BuyBox();

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.BuyBoxStep));
            Assert.That(_tutorial.IsPurchasedBoxWaiting, Is.True);

            _game.Garage.OpenBox(FindBoxSlot());

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CatalogStep));
            Assert.That(_tutorial.IsPurchasedBoxWaiting, Is.False);
        }

        [Test]
        public void RecycledPurchasedBoxReturnsToBuying()
        {
            ReachBuyBoxStep();
            _game.Wallet.Add(_game.Delivery.BoxPrice);
            _game.Delivery.BuyBox();

            _game.Garage.Recycle(FindBoxSlot());

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.BuyBoxStep));
            Assert.That(_tutorial.IsPurchasedBoxWaiting, Is.False);
        }

        [Test]
        public void FreeBoxDoesNotFinishBuyStep()
        {
            ReachBuyBoxStep();

            _game.Delivery.GrantRewardBox(FirstRank);
            _game.Garage.OpenBox(FindBoxSlot());

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.BuyBoxStep));
        }

        [Test]
        public void OnlyFirstStepRestrictsDropsToTrack()
        {
            Assert.That(_tutorial.IsTrackOnly, Is.False);

            _game.PlaceVehicle(0, FirstRank);
            _game.PlaceVehicle(1, FirstRank);
            _tutorial.Begin();

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.SendToTrackStep));
            Assert.That(_tutorial.IsTrackOnly, Is.True);

            _game.Roster.SendToTrack(0);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.OpenBoxStep));
            Assert.That(_tutorial.IsTrackOnly, Is.False);
        }

        [Test]
        public void SkipBeforeUpgradeStepIsRejected()
        {
            _game.PlaceVehicle(0, FirstRank);
            _game.PlaceVehicle(1, FirstRank);
            _tutorial.Begin();

            CommandResult result = _tutorial.Skip();

            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.TutorialSkipUnavailable));
            Assert.That(_tutorial.IsActive, Is.True);
        }

        [Test]
        public void SkipFromUpgradeStepCompletesTutorial()
        {
            ReachUpgradeStep();

            CommandResult result = _tutorial.Skip();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CompletedStep));
        }

        [Test]
        public void RestoreSetsStepWithoutEvent()
        {
            _tutorial.Restore(TutorialService.MergeStep);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.MergeStep));
            Assert.That(_stepChangedCount, Is.EqualTo(0));
        }

        [Test]
        public void RestoreRejectsOutOfRangeStep()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _tutorial.Restore(TutorialService.CompletedStep - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _tutorial.Restore(TutorialService.MaxStep + 1));
        }

        [Test]
        public void RestoreWhileActiveThrows()
        {
            _tutorial.Begin();

            Assert.Throws<InvalidOperationException>(() => _tutorial.Restore(TutorialService.MergeStep));
        }

        [Test]
        public void StepChangesAreCountedForSave()
        {
            _game.PlaceVehicle(0, FirstRank);
            _tutorial.Begin();

            _game.Roster.SendToTrack(0);

            Assert.That(_stepChangedCount, Is.EqualTo(2));
        }

        private void ReachBuyBoxStep()
        {
            ReachUpgradeStep();
            int upgraded = FindVehicleSlot(SecondRank, 0);
            _game.Roster.SendToTrack(upgraded);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.BuyBoxStep));
        }

        private void ReachUpgradeStep()
        {
            _game.PlaceVehicle(0, FirstRank);
            _game.PlaceVehicle(1, FirstRank);
            _tutorial.Begin();
            _game.Roster.SendToTrack(0);
            int boxSlot = FindBoxSlot();
            _game.Garage.OpenBox(boxSlot);
            int left = FindVehicleSlot(FirstRank, 0);
            int right = FindVehicleSlot(FirstRank, left + 1);
            _game.Garage.Merge(left, right);
        }

        private int FindBoxSlot()
        {
            for (int slot = 0; slot < _game.Garage.SlotCount; slot++)
            {
                if (_game.Garage.TryGetBox(slot, out _))
                {
                    return slot;
                }
            }

            return -1;
        }

        private int FindVehicleSlot(int rank, int from)
        {
            for (int slot = from; slot < _game.Garage.SlotCount; slot++)
            {
                if (_game.Garage.TryGetVehicle(slot, out var vehicle) && vehicle.Rank == rank && _game.Garage.IsOnTrack(slot) == false)
                {
                    return slot;
                }
            }

            return -1;
        }

        private void OnStepChanged()
        {
            _stepChangedCount++;
        }
    }
}
