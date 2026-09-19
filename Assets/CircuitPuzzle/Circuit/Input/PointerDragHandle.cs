using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CircuitPuzzle.Circuit.Input
{
    /// <summary>Pointer events shared by mouse and touch. Interpretation lives in the editor session.</summary>
    public sealed class PointerDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action<PointerEventData> Pressed;
        public Action<PointerEventData> DragStarted;
        public Action<PointerEventData> Dragged;
        public Action<PointerEventData> Released;
        public Action<int> Cancelled;
        public Action<bool> Hovered;
        public bool ImmediateDrag;
        private int? pointer;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || pointer.HasValue)
                return;
            pointer = data.pointerId;
            Pressed?.Invoke(data);
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = !ImmediateDrag;
        public void OnBeginDrag(PointerEventData data)
        {
            if (pointer == data.pointerId) DragStarted?.Invoke(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (pointer == data.pointerId) Dragged?.Invoke(data);
        }
        public void OnPointerUp(PointerEventData data) => Release(data);
        public void OnEndDrag(PointerEventData data) => Release(data);
        public void OnPointerEnter(PointerEventData data) => Hovered?.Invoke(true);
        public void OnPointerExit(PointerEventData data) => Hovered?.Invoke(false);
        // Consume the click on this source. Otherwise the Input System also dispatches
        // a click to the parent board, immediately clearing the selection made on press.
        public void OnPointerClick(PointerEventData data) { }

        private void Release(PointerEventData data)
        {
            if (pointer != data.pointerId) return;
            pointer = null; // End callbacks may rebuild/destroy this view.
            Released?.Invoke(data);
        }

        private void OnDisable()
        {
            int? previous = pointer;
            pointer = null;
            if (previous.HasValue) Cancelled?.Invoke(previous.Value);
        }
    }
}
