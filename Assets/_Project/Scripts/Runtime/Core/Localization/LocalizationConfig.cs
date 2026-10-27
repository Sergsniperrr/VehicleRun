using System;
using UnityEngine;

namespace _Project.Core.Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "_Project/Localization Config", order = 0)]
    public class LocalizationConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private string _key;
            [SerializeField] [TextArea] private string _russian;
            [SerializeField] [TextArea] private string _english;

            public string Key => _key;
            public string Russian => _russian;
            public string English => _english;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public int Count => _entries.Length;

        public Entry GetAt(int index)
        {
            return _entries[index];
        }
    }
}
