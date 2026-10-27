using System.Collections.Generic;

namespace _Project.Core.Analytics
{
    public interface IAnalyticsReporter
    {
        void ReportDesignEvent(string eventName);
        void ReportDesignEvent(string eventName, Dictionary<string, string> eventData);
        void ReportGameReady();
        void ReportGameplayStart();
        void ReportGameplayStop();
    }
}
