using _Project.Features.Ads;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Ads
{
    public class AdPresenterTests
    {
        private GameConfigFixture _fixture;
        private FakeAdvertisingService _advertising;
        private AdPresenter _presenter;

        private float Cooldown => _fixture.Economy.InterstitialCooldownSeconds;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _advertising = new FakeAdvertisingService();
            _presenter = new AdPresenter(_advertising, _fixture.Economy);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void InterstitialIsNotShownBeforeCooldownPasses()
        {
            bool shown = _presenter.TryShowInterstitial();

            Assert.That(shown, Is.False);
            Assert.That(_advertising.InterstitialCount, Is.EqualTo(0));
        }

        [Test]
        public void InterstitialIsShownAfterCooldownPasses()
        {
            _presenter.Advance(Cooldown);

            bool shown = _presenter.TryShowInterstitial();

            Assert.That(shown, Is.True);
            Assert.That(_advertising.InterstitialCount, Is.EqualTo(1));
        }

        [Test]
        public void InterstitialResetsCooldownAfterShowing()
        {
            _presenter.Advance(Cooldown);
            _presenter.TryShowInterstitial();

            bool shownAgain = _presenter.TryShowInterstitial();

            Assert.That(shownAgain, Is.False);
            Assert.That(_advertising.InterstitialCount, Is.EqualTo(1));
        }

        [Test]
        public void RewardedResetsSharedCooldown()
        {
            _presenter.Advance(Cooldown);

            _presenter.ShowRewarded("test", () => { }, null);
            bool interstitialShown = _presenter.TryShowInterstitial();

            Assert.That(_advertising.RewardedCount, Is.EqualTo(1));
            Assert.That(interstitialShown, Is.False);
        }

        [Test]
        public void RewardedInvokesRewardAndClosedCallbacks()
        {
            bool rewarded = false;
            bool closed = false;

            _presenter.ShowRewarded("test", () => rewarded = true, () => closed = true);

            Assert.That(rewarded, Is.True);
            Assert.That(closed, Is.True);
        }
    }
}
