using System;
using System.Collections.Generic;
using _Project.Core.Analytics;
using _Project.Infrastructure.Analytics;
using JTLStudio.SDK;

namespace _Project.Infrastructure.JTL
{
    public class JTLAnalyticsService : IAnalyticsService, IAnalyticsReporter
    {
        public void ReportEvent(IAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
            {
                throw new ArgumentNullException(nameof(analyticsEvent));
            }

            analyticsEvent.Report(this);
        }

        public void ReportDesignEvent(string eventName)
        {
            YandexMetrica.ReachGoal(eventName);
        }

        public void ReportDesignEvent(string eventName, Dictionary<string, string> eventData)
        {
            YandexMetrica.ReachGoal(eventName, eventData);
        }

        public void ReportGameReady()
        {
            JTLSDK.GameEvents.GameReady();
        }

        public void ReportGameplayStart()
        {
            JTLSDK.GameEvents.GameplayStarted();
        }

        public void ReportGameplayStop()
        {
            JTLSDK.GameEvents.GameplayStopped();
        }
    }
}
