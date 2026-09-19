using UnityEngine;
using UnityEngine.UI;

namespace CircuitPuzzle.Circuit.Presentation
{
    /// <summary>Small, replaceable uGUI placeholders; no rendering packages or scene searches.</summary>
    internal static class EditorViewElements
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static Text Label(string name, Transform parent, int size)
        {
            var label = Rect(name, parent).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.95f, .98f, 1f);
            label.raycastTarget = false;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }
    }
}
