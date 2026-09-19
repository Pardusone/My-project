using UnityEngine;
using UnityEngine.UI;

namespace CircuitPuzzle.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class UiLabel : MonoBehaviour
    {
        [SerializeField] private UiTextCatalog catalog;
        [SerializeField] private string key;

        private void OnEnable()
        {
            Refresh();
        }

        [ContextMenu("Refresh Text")]
        public void Refresh()
        {
            if (catalog != null && !string.IsNullOrEmpty(key))
                GetComponent<Text>().text = catalog.Get(key);
        }
    }
}
