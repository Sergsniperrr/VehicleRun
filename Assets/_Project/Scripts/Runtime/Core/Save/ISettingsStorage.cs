namespace _Project.Core.Save
{
    public interface ISettingsStorage
    {
        bool TryRead(out SettingsSave settings);

        void Write(SettingsSave settings);
    }
}
