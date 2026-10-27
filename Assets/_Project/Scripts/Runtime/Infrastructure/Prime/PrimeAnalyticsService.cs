using System;
using System.Collections.Generic;
using _Project.Core.Analytics;
using _Project.Infrastructure.Analytics;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class PrimeAnalyticsService : IAnalyticsService, IAnalyticsReporter
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
            PrimeSDK.Analytics.GameIsReady();
        }

        public void ReportGameplayStart()
        {
            PrimeSDK.Analytics.GameplayStart();
        }

        public void ReportGameplayStop()
        {
            PrimeSDK.Analytics.GameplayStop();
        }
    }
}
