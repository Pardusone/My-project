using System;
using System.Collections.Generic;
using CircuitPuzzle.Components;
using UnityEngine;

namespace CircuitPuzzle.Circuit.Presentation
{
    /// <summary>Renders committed board state and transient previews without modifying that state.</summary>
    public sealed class BoardPresentation : MonoBehaviour
    {
        [SerializeField] private RectTransform componentLayer;
        [SerializeField] private RectTransform wireLayer;
        [SerializeField] private RectTransform previewLayer;
        private readonly Dictionary<string, ComponentView> components = new Dictionary<string, ComponentView>();
        private readonly Dictionary<string, WireView> wires = new Dictionary<string, WireView>();
        private CircuitBoard board;
        private GridBoardView grid;
        private Action<ComponentView> bindComponent;
        private Action<string> selectWire;
        private ComponentView componentPreview;
        private WireView wirePreview;
        private ComponentDefinition previewDefinition;
        private Vector2Int previewPosition;
        private int previewRotation;
        private string movingId;

        public void Bind(CircuitBoard state, GridBoardView boardGrid,
            Action<ComponentView> onComponent, Action<string> onWire)
        {
            board = state;
            grid = boardGrid;
            bindComponent = onComponent;
            selectWire = onWire;
            // Keep even same-component connections visible; wires yield hits near terminals.
            wireLayer.SetAsLastSibling();
            previewLayer.SetAsLastSibling();
            Rebuild();
        }

        public void Rebuild()
        {
            ClearPreviews();
            foreach (ComponentView view in components.Values) Remove(view.gameObject);
            foreach (WireView view in wires.Values) Remove(view.gameObject);
            components.Clear();
            wires.Clear();
            foreach (PlacedComponent component in board.Components)
            {
                var view = EditorViewElements.Rect(component.Definition.DisplayName, componentLayer)
                    .gameObject.AddComponent<ComponentView>();
                view.Initialize(component.Id, component.Definition);
                components.Add(component.Id, view);
                bindComponent(view);
            }
            foreach (WireConnection wire in board.Wires)
            {
                WireView view = CreateWire(wireLayer);
                view.Bind(wire.Id, selectWire);
                view.IsOverTerminal = IsOverTerminal;
                wires.Add(wire.Id, view);
            }
            RefreshGeometry();
        }

        public void RefreshGeometry()
        {
            if (board == null) return;
            foreach (PlacedComponent component in board.Components)
                components[component.Id].SetGeometry(component.Position, component.Rotation, grid);
            if (componentPreview != null)
                componentPreview.SetGeometry(previewPosition, previewRotation, grid);
            RefreshWires();
        }

        private void RefreshWires()
        {
            foreach (WireConnection wire in board.Wires)
                if (wires.TryGetValue(wire.Id, out WireView view))
                    view.SetEndpoints(grid.GridToLocal(DisplayTerminalPosition(wire.From)),
                        grid.GridToLocal(DisplayTerminalPosition(wire.To)));
        }

        private bool IsOverTerminal(Vector2 local)
        {
            foreach (PlacedComponent component in board.Components)
                foreach (TerminalDefinition terminal in component.Definition.Terminals)
                {
                    Vector2 point = grid.GridToLocal(board.GetTerminalPosition(
                        new TerminalReference(component.Id, terminal.Id)));
                    if ((point - local).sqrMagnitude < 60 * 60) return true;
                }
            return false;
        }

        private Vector2 DisplayTerminalPosition(TerminalReference reference)
        {
            if (movingId == reference.ComponentId && previewDefinition != null)
            {
                TerminalDefinition terminal = previewDefinition.FindTerminal(reference.TerminalId);
                return (Vector2)previewPosition + GridGeometry.RotateLocalPoint(terminal.LocalPosition,
                    previewDefinition.Footprint, previewRotation);
            }
            return board.GetTerminalPosition(reference);
        }

        public void ShowComponentPreview(ComponentDefinition definition, Vector2Int position,
            int rotation, bool valid, string movingComponentId)
        {
            if (componentPreview == null)
            {
                componentPreview = EditorViewElements.Rect("Placement Preview", previewLayer)
                    .gameObject.AddComponent<ComponentView>();
                componentPreview.Initialize("preview", definition, true);
            }
            previewDefinition = definition;
            previewPosition = position;
            previewRotation = rotation;
            movingId = movingComponentId;
            if (movingId != null && components.TryGetValue(movingId, out ComponentView moving))
                moving.SetFaded(true);
            componentPreview.SetGeometry(position, rotation, grid);
            componentPreview.SetPreviewState(valid);
            RefreshWires();
        }

        public void ShowWirePreview(TerminalReference from, Vector2 endGrid, bool valid,
            TerminalReference? target)
        {
            if (wirePreview == null) wirePreview = CreateWire(previewLayer);
            wirePreview.SetPreview(valid);
            wirePreview.SetEndpoints(grid.GridToLocal(board.GetTerminalPosition(from)), grid.GridToLocal(endGrid));
            foreach (ComponentView component in components.Values)
                foreach (TerminalView terminal in component.Terminals)
                    terminal.SetHighlighted(terminal.Reference == from || terminal.Reference == target);
        }

        // Geometric target lookup ignores the dragged preview and works for mouse and touch.
        public TerminalReference? FindTerminal(Vector2 screen, Camera eventCamera, TerminalReference? ignore = null)
        {
            if (!grid.ContainsScreenPoint(screen, eventCamera)) return null;
            Vector2 local = grid.GridToLocal(grid.ScreenToGrid(screen, eventCamera));
            float bestDistance = float.PositiveInfinity;
            TerminalReference? best = null;
            foreach (ComponentView component in components.Values)
            {
                foreach (TerminalView terminal in component.Terminals)
                {
                    if (terminal.Reference == ignore) continue;
                    Vector2 point = grid.GridToLocal(board.GetTerminalPosition(terminal.Reference));
                    float distance = (point - local).sqrMagnitude;
                    if (distance <= 65 * 65 && distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = terminal.Reference;
                    }
                }
            }
            return best;
        }

        public void SetSelection(string componentId, string wireId)
        {
            foreach (ComponentView view in components.Values) view.SetSelected(view.ComponentId == componentId);
            foreach (WireView view in wires.Values) view.SetSelected(view.WireId == wireId);
        }

        public void ClearPreviews()
        {
            if (componentPreview != null) Remove(componentPreview.gameObject);
            if (wirePreview != null) Remove(wirePreview.gameObject);
            componentPreview = null;
            wirePreview = null;
            previewDefinition = null;
            movingId = null;
            foreach (ComponentView component in components.Values)
            {
                if (component == null) continue;
                component.SetFaded(false);
                foreach (TerminalView terminal in component.Terminals) terminal.SetHighlighted(false);
            }
            if (board != null && wires.Count == board.Wires.Count) RefreshWires();
        }

        private static WireView CreateWire(Transform parent)
        {
            RectTransform rect = EditorViewElements.Rect("Wire", parent);
            EditorViewElements.Stretch(rect);
            return rect.gameObject.AddComponent<WireView>();
        }

        private static void Remove(GameObject target)
        {
            target.SetActive(false); // Don't leave old raycast targets alive until end of frame.
            Destroy(target);
        }
    }
}
