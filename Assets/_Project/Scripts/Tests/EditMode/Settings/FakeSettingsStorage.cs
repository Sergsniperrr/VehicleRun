using _Project.Core.Save;

namespace _Project.Tests.EditMode.Settings
{
    public class FakeSettingsStorage : ISettingsStorage
    {
        public SettingsSave Stored { get; set; }
        public SettingsSave LastWritten { get; private set; }
        public int WriteCount { get; private set; }

        public bool TryRead(out SettingsSave settings)
        {
            settings = Stored;

            return settings != null;
        }

        public void Write(SettingsSave settings)
        {
            Stored = settings;
            LastWritten = settings;
            WriteCount++;
        }
    }
}
