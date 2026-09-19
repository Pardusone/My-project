using System;
using CircuitPuzzle.Components;
using UnityEngine;

namespace CircuitPuzzle.Circuit
{
    /// <summary>A terminal is uniquely identified by its placed instance ID and authored terminal ID.</summary>
    public readonly struct TerminalReference : IEquatable<TerminalReference>
    {
        public string ComponentId { get; }
        public string TerminalId { get; }

        public TerminalReference(string componentId, string terminalId)
        {
            ComponentId = componentId;
            TerminalId = terminalId;
        }

        public bool Equals(TerminalReference other) =>
            ComponentId == other.ComponentId && TerminalId == other.TerminalId;
        public override bool Equals(object obj) => obj is TerminalReference other && Equals(other);
        public override int GetHashCode() => unchecked(
            ((ComponentId?.GetHashCode() ?? 0) * 397) ^ (TerminalId?.GetHashCode() ?? 0));
        public static bool operator ==(TerminalReference left, TerminalReference right) => left.Equals(right);
        public static bool operator !=(TerminalReference left, TerminalReference right) => !left.Equals(right);
        public override string ToString() => $"{ComponentId}/{TerminalId}";
    }

    public sealed class PlacedComponent
    {
        public string Id { get; }
        public ComponentDefinition Definition { get; }
        public Vector2Int Position { get; }
        public int Rotation { get; }

        internal PlacedComponent(string id, ComponentDefinition definition, Vector2Int position, int rotation)
        {
            Id = id;
            Definition = definition;
            Position = position;
            Rotation = GridGeometry.NormalizeRotation(rotation);
        }
    }

    public sealed class WireConnection
    {
        public string Id { get; }
        public TerminalReference From { get; }
        public TerminalReference To { get; }

        internal WireConnection(string id, TerminalReference from, TerminalReference to)
        {
            Id = id;
            From = from;
            To = to;
        }
    }
}
