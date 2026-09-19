using System;
using UnityEngine;

namespace CircuitPuzzle.UI
{
    /// <summary>Central text data with stable keys for a future localization migration.</summary>
    [CreateAssetMenu(menuName = "Circuit Puzzle/UI Text Catalog")]
    public sealed class UiTextCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public string key;
            [TextArea(1, 5)] public string value;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public string Get(string key)
        {
            foreach (Entry entry in entries)
            {
                if (entry.key == key)
                    return entry.value;
            }

            Debug.LogWarning($"UI text key is missing: {key}", this);
            return $"[{key}]";
        }
    }
}
