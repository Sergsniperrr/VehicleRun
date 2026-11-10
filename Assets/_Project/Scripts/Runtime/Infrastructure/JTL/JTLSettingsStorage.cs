using System;
using _Project.Core.Save;
using JTLStudio.SDK;

namespace _Project.Infrastructure.JTL
{
    public class JTLSettingsStorage : ISettingsStorage
    {
        private const string SettingsKey = "settings.audio";

        public bool TryRead(out SettingsSave settings)
        {
            settings = JTLSDK.Data.GetObject<SettingsSave>(SettingsKey);

            return settings != null;
        }

        public void Write(SettingsSave settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            JTLSDK.Data.SetObject(SettingsKey, settings, false);
        }
    }
}
