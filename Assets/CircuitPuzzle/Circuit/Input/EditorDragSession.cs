using System;
using CircuitPuzzle.Circuit.Presentation;
using CircuitPuzzle.Components;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CircuitPuzzle.Circuit.Input
{
    /// <summary>One captured pointer, one transient edit. Model changes only on a valid release.</summary>
    public sealed class EditorDragSession
    {
        private enum DragKind { None, Component, Wire }
        private readonly CircuitBoard board;
        private readonly EditorHistory history;
        private readonly GridBoardView grid;
        private readonly BoardPresentation presentation;
        private readonly Action<string> status;
        private readonly Action<string> selectComponent;
        private readonly Action<string> selectWire;
        private DragKind kind;
        private int pointer;
        private Camera camera;
        private ComponentDefinition definition;
        private string movingId;
        private int rotation;
        private Vector2Int position;
        private Vector2 pointerOffset;
        private TerminalReference from;
        private TerminalReference? to;
        private bool valid;

        public bool IsActive => kind != DragKind.None;
        public event Action Changed;

        public EditorDragSession(CircuitBoard board, EditorHistory history, GridBoardView grid,
            BoardPresentation presentation, Action<string> status,
            Action<string> selectComponent, Action<string> selectWire)
        {
            this.board = board;
            this.history = history;
            this.grid = grid;
            this.presentation = presentation;
            this.status = status;
            this.selectComponent = selectComponent;
            this.selectWire = selectWire;
        }

        public void BeginPlacement(ComponentDefinition component, PointerEventData data)
        {
            if (IsActive) return;
            definition = component;
            movingId = null;
            rotation = definition.AllowedRotations[0];
            pointerOffset = Vector2.zero;
            Capture(DragKind.Component, data);
            Update(data);
        }

        public void BeginMove(string componentId, PointerEventData data)
        {
            if (IsActive) return;
            PlacedComponent component = board.FindComponent(componentId);
            if (component == null) return;
            definition = component.Definition;
            movingId = component.Id;
            rotation = component.Rotation;
            Vector2 center = (Vector2)component.Position +
                (Vector2)GridGeometry.RotatedSize(definition.Footprint, rotation) * .5f;
            // Preserve where the pointer grabbed the part; no jump when dragging a corner.
            pointerOffset = grid.ScreenToGrid(data.pressPosition, data.pressEventCamera) - center;
            Capture(DragKind.Component, data);
            Update(data);
        }

        public void BeginWire(TerminalReference terminal, PointerEventData data)
        {
            if (IsActive || !board.HasTerminal(terminal)) return;
            from = terminal;
            Capture(DragKind.Wire, data);
            status("editor.wire.ready");
            Update(data);
        }

        private void Capture(DragKind dragKind, PointerEventData data)
        {
            kind = dragKind;
            pointer = data.pointerId;
            camera = data.pressEventCamera;
            Changed?.Invoke();
        }

        public void Update(PointerEventData data)
        {
            if (!IsActive || pointer != data.pointerId) return;
            Vector2 point = grid.ScreenToGrid(data.position, camera);
            if (kind == DragKind.Component)
            {
                position = GridGeometry.NearestOrigin(point - pointerOffset,
                    GridGeometry.RotatedSize(definition.Footprint, rotation));
                valid = grid.ContainsScreenPoint(data.position, camera) &&
                    board.CanPlace(definition, position, rotation, movingId);
                presentation.ShowComponentPreview(definition, position, rotation, valid, movingId);
                status(valid ? "editor.placement.valid" : "editor.placement.invalid");
            }
            else
            {
                to = presentation.FindTerminal(data.position, camera, from);
                valid = to.HasValue && board.CanConnect(from, to.Value);
                Vector2 end = to.HasValue ? board.GetTerminalPosition(to.Value) : point;
                presentation.ShowWirePreview(from, end, valid, to);
                status(valid ? "editor.wire.valid" : "editor.wire.invalid");
            }
        }

        public void End(PointerEventData data)
        {
            if (!IsActive || pointer != data.pointerId) return;
            Update(data); // Release location, including releases outside the source view, is authoritative.
            DragKind completed = kind;
            bool accepted = valid;
            string moved = movingId;
            Clear(); // Rebuilding views during the edit must not cancel a still-captured gesture.
            if (!accepted)
            {
                status(completed == DragKind.Wire ? "editor.cancelled" : "editor.placement.invalid");
                return;
            }
            if (completed == DragKind.Component)
            {
                string id = moved;
                bool changed = history.Execute(() => moved == null
                    ? board.TryAddComponent(definition, position, rotation, out id)
                    : board.TryMoveComponent(moved, position));
                selectComponent(id);
                if (changed) status(moved == null ? "editor.placed" : "editor.moved");
            }
            else
            {
                string id = null;
                if (history.Execute(() => board.TryConnect(from, to.Value, out id)))
                {
                    selectWire(id);
                    status("editor.connected");
                }
            }
        }

        public void CancelForPointer(int pointerId)
        {
            if (IsActive && pointer == pointerId) Cancel();
        }

        public void Cancel()
        {
            if (!IsActive) return;
            Clear();
            status("editor.cancelled");
        }

        private void Clear()
        {
            kind = DragKind.None;
            presentation.ClearPreviews();
            Changed?.Invoke();
        }
    }
}
