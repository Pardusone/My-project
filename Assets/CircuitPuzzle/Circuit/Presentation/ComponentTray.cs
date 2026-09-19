using System;
using CircuitPuzzle.Circuit.Input;
using CircuitPuzzle.Components;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CircuitPuzzle.Circuit.Presentation
{
    public sealed class ComponentTray : MonoBehaviour
    {
        [SerializeField] private ComponentDefinition[] definitions;
        [SerializeField] private RectTransform itemsRoot;
        public ComponentDefinition[] Definitions => definitions;

        public void Bind(Action<ComponentDefinition, PointerEventData> begin,
            Action<PointerEventData> drag, Action<PointerEventData> end, Action<int> cancel,
            Action<ComponentDefinition> inspect)
        {
            // The available definitions are tray data; future level inventories can supply this list.
            foreach (ComponentDefinition definition in definitions)
            {
                if (definition == null) continue;
                RectTransform item = EditorViewElements.Rect(definition.StableId, itemsRoot);
                Image image = item.gameObject.AddComponent<Image>();
                image.color = new Color(.10f, .18f, .22f);
                Image accent = EditorViewElements.Rect("Accent", item).gameObject.AddComponent<Image>();
                accent.raycastTarget = false;
                accent.color = Color.Lerp(definition.Tint, Color.white, .15f);
                accent.rectTransform.anchorMin = new Vector2(0, 1);
                accent.rectTransform.anchorMax = Vector2.one;
                accent.rectTransform.pivot = new Vector2(.5f, 1);
                accent.rectTransform.sizeDelta = new Vector2(0, 4);
                accent.rectTransform.anchoredPosition = Vector2.zero;
                LayoutElement layout = item.gameObject.AddComponent<LayoutElement>();
                layout.flexibleWidth = 1;
                layout.minWidth = 0;
                Text title = EditorViewElements.Label("Name", item, 27);
                title.text = definition.DisplayName;
                title.fontStyle = FontStyle.Bold;
                EditorViewElements.Stretch(title.rectTransform);
                title.rectTransform.offsetMin = new Vector2(8, 6);
                title.rectTransform.offsetMax = new Vector2(-8, -8);
                var handle = item.gameObject.AddComponent<PointerDragHandle>();
                handle.Pressed = data => inspect(definition);
                handle.DragStarted = data => begin(definition, data);
                handle.Dragged = drag;
                handle.Released = end;
                handle.Cancelled = cancel;
                handle.Hovered = hovered => image.color = hovered
                    ? new Color(.16f, .27f, .30f) : new Color(.10f, .18f, .22f);
            }
        }
    }
}
