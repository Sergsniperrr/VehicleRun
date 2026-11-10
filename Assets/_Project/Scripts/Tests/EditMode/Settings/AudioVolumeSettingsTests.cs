using _Project.Core.Save;
using _Project.Features.Settings;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Settings
{
    public class AudioVolumeSettingsTests
    {
        private const float Tolerance = 1e-6f;

        private FakeSettingsStorage _storage;
        private FakeAudioMixerControl _mixer;
        private AudioVolumeSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _storage = new FakeSettingsStorage();
            _mixer = new FakeAudioMixerControl();
            _settings = new AudioVolumeSettings(_storage, _mixer);
        }

        [Test]
        public void InitializeWithoutSaveUsesFullVolume()
        {
            _settings.Initialize();

            Assert.That(_settings.MusicVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_settings.SfxVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_mixer.MusicVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_mixer.SfxVolume, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void InitializeLoadsSavedVolumes()
        {
            _storage.Stored = new SettingsSave { MusicVolume = 0.3f, SfxVolume = 0.7f };

            _settings.Initialize();

            Assert.That(_settings.MusicVolume, Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(_settings.SfxVolume, Is.EqualTo(0.7f).Within(Tolerance));
            Assert.That(_mixer.MusicVolume, Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(_mixer.SfxVolume, Is.EqualTo(0.7f).Within(Tolerance));
        }

        [Test]
        public void InitializeClampsSavedVolumes()
        {
            _storage.Stored = new SettingsSave { MusicVolume = 2f, SfxVolume = -1f };

            _settings.Initialize();

            Assert.That(_settings.MusicVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_settings.SfxVolume, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void InitializeDoesNotWriteStorage()
        {
            _settings.Initialize();

            Assert.That(_storage.WriteCount, Is.EqualTo(0));
        }

        [Test]
        public void SetMusicVolumeAppliesAndSaves()
        {
            _settings.SetMusicVolume(0.5f);

            Assert.That(_settings.MusicVolume, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(_mixer.MusicVolume, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(_storage.WriteCount, Is.EqualTo(1));
            Assert.That(_storage.LastWritten.MusicVolume, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void SetSfxVolumeAppliesAndSaves()
        {
            _settings.SetSfxVolume(0.25f);

            Assert.That(_settings.SfxVolume, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(_mixer.SfxVolume, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(_storage.WriteCount, Is.EqualTo(1));
            Assert.That(_storage.LastWritten.SfxVolume, Is.EqualTo(0.25f).Within(Tolerance));
        }

        [TestCase(2f, 1f)]
        [TestCase(-1f, 0f)]
        public void SetMusicVolumeClampsToUnitRange(float value, float expected)
        {
            _settings.SetMusicVolume(value);

            Assert.That(_settings.MusicVolume, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void SetVolumeSavesBothVolumes()
        {
            _settings.SetMusicVolume(0.2f);
            _settings.SetSfxVolume(0.8f);

            Assert.That(_storage.LastWritten.MusicVolume, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(_storage.LastWritten.SfxVolume, Is.EqualTo(0.8f).Within(Tolerance));
        }
    }
}
