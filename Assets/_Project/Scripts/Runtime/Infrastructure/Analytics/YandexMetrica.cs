using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace _Project.Infrastructure.Analytics
{
    public static class YandexMetrica
    {
        private static string _counterId;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void InitYandexMetrica(string counterId);

        [DllImport("__Internal")]
        private static extern void YandexMetricaSend_js(string eventName);

        [DllImport("__Internal")]
        private static extern void YandexMetricaSend2_js(string eventName, string eventDataJson);
#endif

        public static void Init(string counterId)
        {
            _counterId = counterId;

            if (string.IsNullOrEmpty(counterId))
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            InitYandexMetrica(counterId);
#else
            Debug.Log($"[YandexMetrica] (sim) init: ym({counterId}, \"init\", {{...}})");
#endif
        }

        public static void ReachGoal(string eventName)
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

        public static void ReachGoal(string eventName, Dictionary<string, string> eventData)
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

        private static string ToJson(Dictionary<string, string> data)
        {
            if (data == null || data.Count == 0)
            {
                return "{}";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('{');
            bool first = true;

            foreach (KeyValuePair<string, string> pair in data)
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

        private static void AppendString(StringBuilder builder, string value)
        {
            builder.Append('"');

            if (value != null)
            {
                foreach (char symbol in value)
                {
                    switch (symbol)
                    {
                        case '"': builder.Append("\\\""); break;
                        case '\\': builder.Append("\\\\"); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default: builder.Append(symbol); break;
                    }
                }
            }

            builder.Append('"');
        }
    }
}
