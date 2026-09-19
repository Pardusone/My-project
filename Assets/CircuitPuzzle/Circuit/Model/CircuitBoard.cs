using System;
using System.Collections.Generic;
using CircuitPuzzle.Components;
using UnityEngine;

namespace CircuitPuzzle.Circuit
{
    /// <summary>Owns construction state and placement rules without depending on scene or input objects.</summary>
    public sealed class CircuitBoard
    {
        private readonly List<PlacedComponent> components = new List<PlacedComponent>();
        private readonly List<WireConnection> wires = new List<WireConnection>();

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<PlacedComponent> Components { get; }
        public IReadOnlyList<WireConnection> Wires { get; }
        internal int Revision { get; private set; }
        public event Action Changed;

        public CircuitBoard(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Board dimensions must be positive.");
            Width = width;
            Height = height;
            Components = components.AsReadOnly();
            Wires = wires.AsReadOnly();
        }

        public PlacedComponent FindComponent(string id) => components.Find(component => component.Id == id);
        public WireConnection FindWire(string id) => wires.Find(wire => wire.Id == id);

        public bool CanPlace(ComponentDefinition definition, Vector2Int position, int rotation,
            string ignoreComponentId = null)
        {
            if (!HasValidDefinition(definition) || !definition.AllowsRotation(rotation))
                return false;
            Vector2Int size = GridGeometry.RotatedSize(definition.Footprint, rotation);
            if (position.x < 0 || position.y < 0 || size.x > Width || size.y > Height ||
                position.x > Width - size.x || position.y > Height - size.y)
                return false;
            foreach (PlacedComponent other in components)
            {
                if (other.Id == ignoreComponentId)
                    continue;
                Vector2Int otherSize = GridGeometry.RotatedSize(other.Definition.Footprint, other.Rotation);
                if (position.x < other.Position.x + otherSize.x && position.x + size.x > other.Position.x &&
                    position.y < other.Position.y + otherSize.y && position.y + size.y > other.Position.y)
                    return false;
            }
            return true;
        }

        public bool TryAddComponent(ComponentDefinition definition, Vector2Int position, int rotation,
            out string id)
        {
            id = null;
            if (!CanPlace(definition, position, rotation))
                return false;
            id = Guid.NewGuid().ToString("N");
            components.Add(new PlacedComponent(id, definition, position, rotation));
            NotifyChanged();
            return true;
        }

        public bool TryMoveComponent(string id, Vector2Int position)
        {
            PlacedComponent component = FindComponent(id);
            if (component == null || component.Position == position ||
                !CanPlace(component.Definition, position, component.Rotation, id))
                return false;
            components[components.IndexOf(component)] =
                new PlacedComponent(id, component.Definition, position, component.Rotation);
            NotifyChanged();
            return true;
        }

        public bool TryRotateComponent(string id)
        {
            PlacedComponent component = FindComponent(id);
            if (component == null)
                return false;
            for (int step = 1; step < 4; step++)
            {
                int rotation = GridGeometry.NormalizeRotation(component.Rotation + step);
                if (!component.Definition.AllowsRotation(rotation))
                    continue;
                if (!CanPlace(component.Definition, component.Position, rotation, id))
                    return false;
                components[components.IndexOf(component)] =
                    new PlacedComponent(id, component.Definition, component.Position, rotation);
                NotifyChanged();
                return true;
            }
            return false;
        }

        public bool TryRemoveComponent(string id)
        {
            PlacedComponent component = FindComponent(id);
            if (component == null)
                return false;
            wires.RemoveAll(wire => wire.From.ComponentId == id || wire.To.ComponentId == id);
            components.Remove(component);
            NotifyChanged();
            return true;
        }

        public bool CanConnect(TerminalReference from, TerminalReference to)
        {
            if (from == to || !HasTerminal(from) || !HasTerminal(to))
                return false;
            foreach (WireConnection wire in wires)
                if ((wire.From == from && wire.To == to) || (wire.From == to && wire.To == from))
                    return false;
            // Terminals on the same component are deliberately allowed: this is construction only.
            return true;
        }

        public bool TryConnect(TerminalReference from, TerminalReference to, out string wireId)
        {
            wireId = null;
            if (!CanConnect(from, to))
                return false;
            wireId = Guid.NewGuid().ToString("N");
            wires.Add(new WireConnection(wireId, from, to));
            NotifyChanged();
            return true;
        }

        public bool TryRemoveWire(string id)
        {
            WireConnection wire = FindWire(id);
            if (wire == null)
                return false;
            wires.Remove(wire);
            NotifyChanged();
            return true;
        }

        public bool HasTerminal(TerminalReference terminal) =>
            FindComponent(terminal.ComponentId)?.Definition.FindTerminal(terminal.TerminalId) != null;

        public Vector2 GetTerminalPosition(TerminalReference reference)
        {
            PlacedComponent component = FindComponent(reference.ComponentId);
            TerminalDefinition terminal = component?.Definition.FindTerminal(reference.TerminalId);
            if (terminal == null)
                throw new ArgumentException("The referenced terminal does not exist on this board.", nameof(reference));
            return (Vector2)component.Position + GridGeometry.RotateLocalPoint(
                terminal.LocalPosition, component.Definition.Footprint, component.Rotation);
        }

        public BoardSnapshot Capture() => new BoardSnapshot(Width, Height, components, wires);

        public void Restore(BoardSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Width != Width || snapshot.Height != Height)
                throw new ArgumentException("Snapshot dimensions must match this board.", nameof(snapshot));
            if (Matches(snapshot))
                return;

            // Validate completely before changing live state; an invalid restore cannot partially apply.
            CircuitBoard check = new CircuitBoard(Width, Height);
            foreach (PlacedComponent component in snapshot.Components)
            {
                if (component == null || string.IsNullOrEmpty(component.Id) ||
                    check.FindComponent(component.Id) != null ||
                    !check.CanPlace(component.Definition, component.Position, component.Rotation))
                    throw new ArgumentException("Snapshot contains invalid component placement.", nameof(snapshot));
                check.components.Add(component);
            }
            foreach (WireConnection wire in snapshot.Wires)
            {
                if (wire == null || string.IsNullOrEmpty(wire.Id) || check.FindWire(wire.Id) != null ||
                    !check.CanConnect(wire.From, wire.To))
                    throw new ArgumentException("Snapshot contains an invalid connection.", nameof(snapshot));
                check.wires.Add(wire);
            }
            components.Clear();
            components.AddRange(check.components);
            wires.Clear();
            wires.AddRange(check.wires);
            NotifyChanged();
        }

        public void Reset(BoardSnapshot initialState = null)
        {
            if (initialState != null)
            {
                Restore(initialState);
                return;
            }
            if (components.Count == 0 && wires.Count == 0)
                return;
            components.Clear();
            wires.Clear();
            NotifyChanged();
        }

        private bool Matches(BoardSnapshot snapshot)
        {
            if (components.Count != snapshot.Components.Count || wires.Count != snapshot.Wires.Count)
                return false;
            for (int index = 0; index < components.Count; index++)
            {
                PlacedComponent a = components[index];
                PlacedComponent b = snapshot.Components[index];
                if (a.Id != b.Id || a.Definition != b.Definition || a.Position != b.Position || a.Rotation != b.Rotation)
                    return false;
            }
            for (int index = 0; index < wires.Count; index++)
            {
                WireConnection a = wires[index];
                WireConnection b = snapshot.Wires[index];
                if (a.Id != b.Id || a.From != b.From || a.To != b.To)
                    return false;
            }
            return true;
        }

        private static bool HasValidDefinition(ComponentDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.StableId) ||
                definition.Footprint.x <= 0 || definition.Footprint.y <= 0)
                return false;
            HashSet<string> terminalIds = new HashSet<string>();
            foreach (TerminalDefinition terminal in definition.Terminals)
            {
                if (terminal == null || string.IsNullOrWhiteSpace(terminal.Id) || !terminalIds.Add(terminal.Id))
                    return false;
                Vector2 point = terminal.LocalPosition;
                if (float.IsNaN(point.x) || float.IsNaN(point.y) || point.x < 0 || point.y < 0 ||
                    point.x > definition.Footprint.x || point.y > definition.Footprint.y)
                    return false;
            }
            return true;
        }

        private void NotifyChanged()
        {
            Revision++;
            Changed?.Invoke();
        }
    }
}
