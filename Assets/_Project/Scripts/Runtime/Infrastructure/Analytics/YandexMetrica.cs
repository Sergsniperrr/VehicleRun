using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Infrastructure.Analytics
{
    public class YandexMetrica : IInitializable
    {
        private readonly string _counterId;

        public YandexMetrica(string counterId)
        {
            _counterId = counterId ?? throw new ArgumentNullException(nameof(counterId));
        }

        public void Initialize()
        {
            if (string.IsNullOrEmpty(_counterId))
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            InitYandexMetrica(_counterId);
#else
            Debug.Log($"[YandexMetrica] (sim) init: ym({_counterId}, \"init\", {{...}})");
#endif
        }

        public void ReachGoal(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexMetricaSend_js(eventName);
#else
            Debug.Log($"[YandexMetrica] (sim) ym('{_counterId}', 'reachGoal', '{eventName}')");
#endif
        }

        public void ReachGoal(string eventName, Dictionary<string, string> eventData)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            string json = ToJson(eventData);

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexMetricaSend2_js(eventName, json);
#else
            Debug.Log($"[YandexMetrica] (sim) ym('{_counterId}', 'reachGoal', '{eventName}', {json})");
#endif
        }

        private string ToJson(Dictionary<string, string> eventData)
        {
            if (eventData == null || eventData.Count == 0)
            {
                return "{}";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('{');
            bool first = true;

            foreach (KeyValuePair<string, string> pair in eventData)
            {
                if (first == false)
                {
                    builder.Append(',');
                }

                first = false;
                AppendString(builder, pair.Key);
                builder.Append(':');
                AppendString(builder, pair.Value);
            }

            builder.Append('}');
            return builder.ToString();
        }

        private void AppendString(StringBuilder builder, string value)
        {
            builder.Append('"');

            if (value != null)
            {
                foreach (char symbol in value)
                {
                    AppendEscaped(builder, symbol);
                }
            }

            builder.Append('"');
        }

        private void AppendEscaped(StringBuilder builder, char symbol)
        {
            switch (symbol)
            {
                case '"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append("\\\\");
                    break;

                case '\n':
                    builder.Append("\\n");
                    break;

                case '\r':
                    builder.Append("\\r");
                    break;

                case '\t':
                    builder.Append("\\t");
                    break;

                default:
                    builder.Append(symbol);
                    break;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void InitYandexMetrica(string counterId);

        [DllImport("__Internal")]
        private static extern void YandexMetricaSend_js(string eventName);

        [DllImport("__Internal")]
        private static extern void YandexMetricaSend2_js(string eventName, string eventDataJson);
#endif
    }
}
