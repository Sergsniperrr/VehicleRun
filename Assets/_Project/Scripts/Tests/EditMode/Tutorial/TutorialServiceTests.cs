using System;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Garage;
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

            GarageGestures gestures = new GarageGestures(_game.Garage, _game.Roster, _tutorial);
            Assert.That(gestures.DropOnSlot(1, 0).IsSuccess, Is.True);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.UpgradeTrackStep));

            int upgradeSlot = FindVehicleSlot(SecondRank, 0);
            _game.Roster.SendToTrack(upgradeSlot);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.BuyBoxStep));

            _game.Wallet.Add(_game.Delivery.BoxPrice);
            _game.Delivery.BuyBox();
            int purchasedSlot = FindBoxSlot();
            _game.Garage.OpenBox(purchasedSlot);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleStep));
            Assert.That(_tutorial.TryGetRecycleSlot(out int recycleSlot), Is.True);
            Assert.That(recycleSlot, Is.EqualTo(2));

            _game.News.MarkSeen();
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleStep));
            _game.Confirmation.OpenPrompt();
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleConfirmStep));
            _game.Confirmation.AnswerPrompt(false, true);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.TrackTapStep));
            Assert.That(_game.Garage.TryGetVehicle(recycleSlot, out var kept) && kept.Rank == FirstRank, Is.True);

            Assert.That(_game.Turbo.BoostRunner(_game.Roster.Runners[0]).IsSuccess, Is.True);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CollectorStep));
            Assert.That(_game.Collector.IsActive, Is.True);
            _tutorial.Advance(1f);
            Assert.That(_tutorial.IsCollectorPaused, Is.True);
            Assert.That(_game.TimeService.Scale, Is.Zero);
            Assert.That(_game.Collector.Collect().IsSuccess, Is.True);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CompletedStep));
            Assert.That(_tutorial.IsActive, Is.False);
            Assert.That(_game.TimeService.Scale, Is.EqualTo(1f));
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
        public void OpenBoxCanMergeWithVehicleOnTrack()
        {
            _game.PlaceVehicle(0, FirstRank);
            _tutorial.Begin();
            _game.Roster.SendToTrack(0);
            int boxSlot = FindBoxSlot();

            _game.Garage.OpenBox(boxSlot);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.MergeStep));
            Assert.That(_tutorial.TryGetMergePair(out int sourceSlot, out int targetSlot), Is.True);
            Assert.That(sourceSlot, Is.EqualTo(boxSlot));
            Assert.That(targetSlot, Is.EqualTo(0));
            Assert.That(FindBoxSlot(), Is.EqualTo(-1));
        }

        [Test]
        public void MergeStepHighlightsLeftPairAndRejectsLowerVehicle()
        {
            _game.PlaceVehicle(0, FirstRank);
            _game.PlaceVehicle(1, FirstRank);
            _tutorial.Begin();
            _game.Roster.SendToTrack(0);
            _game.Garage.OpenBox(FindBoxSlot());
            GarageGestures gestures = new GarageGestures(_game.Garage, _game.Roster, _tutorial);

            Assert.That(_tutorial.TryGetMergePair(out int sourceSlot, out int targetSlot), Is.True);
            Assert.That(sourceSlot, Is.EqualTo(1));
            Assert.That(targetSlot, Is.EqualTo(0));
            Assert.That(gestures.CanMerge(1, 0), Is.True);
            Assert.That(gestures.CanMerge(1, 2), Is.False);
            Assert.That(gestures.CanMerge(0, 1), Is.False);
            Assert.That(gestures.DropOnSlot(1, 2).FailureReason, Is.EqualTo(FailureReason.TutorialDropRestricted));
            Assert.That(gestures.DropOnSlot(0, 1).FailureReason, Is.EqualTo(FailureReason.TutorialDropRestricted));
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.MergeStep));
            Assert.That(_game.Garage.IsOnTrack(0), Is.True);

            Assert.That(gestures.DropOnSlot(1, 0).IsSuccess, Is.True);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.UpgradeTrackStep));
            Assert.That(_game.Garage.IsSlotEmpty(1), Is.True);
            Assert.That(_game.Garage.TryGetVehicle(0, out var merged) && merged.Rank == SecondRank, Is.True);
            Assert.That(_game.Garage.TryGetVehicle(2, out var lower) && lower.Rank == FirstRank, Is.True);
            Assert.That(_game.Garage.IsOnTrack(0), Is.False);
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

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleStep));
            Assert.That(_tutorial.IsPurchasedBoxWaiting, Is.False);
        }

        [Test]
        public void RecycleStepAcceptsOnlyLowerFirstRankVehicle()
        {
            ReachBuyBoxStep();
            _game.Wallet.Add(_game.Delivery.BoxPrice);
            _game.Delivery.BuyBox();
            _game.Garage.OpenBox(FindBoxSlot());
            GarageGestures gestures = new GarageGestures(_game.Garage, _game.Roster, _tutorial);

            Assert.That(_tutorial.TryGetRecycleSlot(out int slot), Is.True);
            Assert.That(slot, Is.EqualTo(2));
            Assert.That(gestures.CanRecycle(slot), Is.True);
            Assert.That(gestures.CanRecycle(1), Is.False);
            Assert.That(gestures.CanDropOnTrack(slot), Is.False);
            Assert.That(gestures.CanMerge(1, 2), Is.False);
            Assert.That(gestures.DropOnSlot(2, 3).FailureReason, Is.EqualTo(FailureReason.TutorialDropRestricted));
            Assert.That(gestures.DropOnTrack(slot).FailureReason, Is.EqualTo(FailureReason.TutorialDropRestricted));
            Assert.That(gestures.Recycle(1).FailureReason, Is.EqualTo(FailureReason.TutorialDropRestricted));
        }

        [Test]
        public void RecyclePromptAdvancesOnlyOnNoButton()
        {
            ReachBuyBoxStep();
            _game.Wallet.Add(_game.Delivery.BoxPrice);
            _game.Delivery.BuyBox();
            _game.Garage.OpenBox(FindBoxSlot());

            _game.Confirmation.OpenPrompt();
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleConfirmStep));
            _game.Confirmation.AnswerPrompt(false, false);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleStep));

            _game.Confirmation.OpenPrompt();
            _game.Confirmation.AnswerPrompt(true, false);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleStep));

            _game.Confirmation.OpenPrompt();
            _game.Confirmation.AnswerPrompt(false, true);
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.TrackTapStep));
        }

        [Test]
        public void TrackTapStepWaitsForRunnerBoost()
        {
            _game.PlaceVehicle(0, SecondRank);
            _game.Roster.SendToTrack(0);
            _tutorial.Restore(TutorialService.TrackTapStep);

            _game.News.MarkSeen();
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.TrackTapStep));

            _game.Turbo.BoostRunner(_game.Roster.Runners[0]);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CollectorStep));
            Assert.That(_game.Collector.IsActive, Is.True);
        }

        [Test]
        public void CollectorStepPausesAfterOneSecondAndResumesOnFirstCharge()
        {
            _game.PlaceVehicle(0, SecondRank);
            _game.Roster.SendToTrack(0);
            _tutorial.Restore(TutorialService.TrackTapStep);
            _game.Turbo.BoostRunner(_game.Roster.Runners[0]);
            long coinsBefore = _game.Wallet.Coins;

            _tutorial.Advance(0.5f);
            Assert.That(_tutorial.IsCollectorPaused, Is.False);
            Assert.That(_game.TimeService.Scale, Is.EqualTo(1f));

            _tutorial.Advance(0.5f);
            Assert.That(_tutorial.IsCollectorPaused, Is.True);
            Assert.That(_game.TimeService.Scale, Is.Zero);

            _game.Collector.Collect();

            Assert.That(_game.Wallet.Coins, Is.GreaterThan(coinsBefore));
            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.CompletedStep));
            Assert.That(_game.TimeService.Scale, Is.EqualTo(1f));
            Assert.That(_game.Collector.IsActive, Is.True);
        }

        [Test]
        public void SkippingPausedCollectorStepRestoresTimeScale()
        {
            _game.PlaceVehicle(0, SecondRank);
            _tutorial.Restore(TutorialService.CollectorStep);
            _game.TimeService.Scale = 0.75f;
            _tutorial.Advance(1f);

            Assert.That(_tutorial.Skip().IsSuccess, Is.True);
            Assert.That(_game.TimeService.Scale, Is.EqualTo(0.75f));
        }

        [Test]
        public void DisposingPausedCollectorAfterTimeServiceShutdownDoesNotThrow()
        {
            _game.PlaceVehicle(0, SecondRank);
            _tutorial.Restore(TutorialService.CollectorStep);
            _tutorial.Advance(1f);
            _game.TimeService.CanSetScale = false;

            Assert.DoesNotThrow(() => _tutorial.Dispose());
            Assert.That(_tutorial.IsCollectorPaused, Is.False);
        }

        [Test]
        public void RestoredRecyclePromptRepeatsDrag()
        {
            _game.PlaceVehicle(2, FirstRank);

            _tutorial.Restore(TutorialService.RecycleConfirmStep);

            Assert.That(_tutorial.Step, Is.EqualTo(TutorialService.RecycleStep));
            Assert.That(_tutorial.TryGetRecycleSlot(out int slot), Is.True);
            Assert.That(slot, Is.EqualTo(2));
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
            GarageGestures gestures = new GarageGestures(_game.Garage, _game.Roster, _tutorial);
            gestures.DropOnSlot(1, 0);
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
