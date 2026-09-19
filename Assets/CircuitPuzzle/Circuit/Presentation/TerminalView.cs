using CircuitPuzzle.Circuit.Input;
using CircuitPuzzle.Components;
using UnityEngine;
using UnityEngine.UI;

namespace CircuitPuzzle.Circuit.Presentation
{
    public sealed class TerminalView : MonoBehaviour
    {
        public TerminalReference Reference { get; private set; }
        public PointerDragHandle Handle { get; private set; }
        public RectTransform Rect { get; private set; }
        private Image marker;
        private Outline outline;
        private Text nameLabel;
        private bool highlighted;
        private bool hovered;
        private bool detailsVisible;

        public void Initialize(TerminalReference reference, TerminalDefinition definition)
        {
            Reference = reference;
            Rect = (RectTransform)transform;
            // The connector is deliberately smaller than its touch target.
            Rect.sizeDelta = new Vector2(120, 120);
            Image hit = gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Handle = gameObject.AddComponent<PointerDragHandle>();
            Handle.ImmediateDrag = true;
            Handle.Hovered = value => { hovered = value; Refresh(); };

            marker = EditorViewElements.Rect("Connector", transform).gameObject.AddComponent<Image>();
            marker.raycastTarget = false;
            marker.rectTransform.sizeDelta = new Vector2(36, 36);
            outline = marker.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2, -2);
            Text symbol = EditorViewElements.Label("Terminal Symbol", transform, 23);
            symbol.rectTransform.sizeDelta = new Vector2(42, 38);
            symbol.text = definition.ShortLabel;
            nameLabel = EditorViewElements.Label("Terminal Name", transform, 19);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0, -46);
            nameLabel.rectTransform.sizeDelta = new Vector2(108, 28);
            nameLabel.color = new Color(.74f, .84f, .87f);
            nameLabel.text = definition.DisplayName == definition.ShortLabel ? string.Empty : definition.DisplayName;
            Refresh();
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            Refresh();
        }

        public void SetDetailsVisible(bool value)
        {
            detailsVisible = value;
            Refresh();
        }

        public void SetCaptionLayout(bool vertical)
        {
            // The name remains upright and outside the component title after rotation.
            nameLabel.rectTransform.anchoredPosition = vertical ? new Vector2(78, 0) : new Vector2(0, -46);
        }

        private void Refresh()
        {
            if (marker == null) return;
            bool active = highlighted || hovered;
            marker.color = active ? new Color(.16f, .38f, .38f) : new Color(.055f, .12f, .15f);
            outline.effectColor = active ? new Color(1f, .82f, .3f) : new Color(.54f, .74f, .77f);
            marker.rectTransform.sizeDelta = active ? new Vector2(42, 42) : new Vector2(36, 36);
            nameLabel.enabled = detailsVisible || active;
        }
    }
}
