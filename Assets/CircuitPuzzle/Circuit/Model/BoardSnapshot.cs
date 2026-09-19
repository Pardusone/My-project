using System.Collections.Generic;

namespace CircuitPuzzle.Circuit
{
    /// <summary>An immutable session snapshot. It contains construction data, never view objects.</summary>
    public sealed class BoardSnapshot
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<PlacedComponent> Components { get; }
        public IReadOnlyList<WireConnection> Wires { get; }

        internal BoardSnapshot(int width, int height, List<PlacedComponent> components,
            List<WireConnection> wires)
        {
            Width = width;
            Height = height;
            Components = new List<PlacedComponent>(components).AsReadOnly();
            Wires = new List<WireConnection>(wires).AsReadOnly();
        }
    }
}
