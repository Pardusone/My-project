using System;
using System.Collections.Generic;
using CircuitPuzzle.Components;

namespace CircuitPuzzle.Circuit
{
    /// <summary>One immutable electrical snapshot. Node indices belong to this snapshot only.</summary>
    public sealed class CircuitGraph
    {
        private readonly Dictionary<TerminalReference, int> terminalNodes;

        public IReadOnlyList<CircuitNode> Nodes { get; }
        public IReadOnlyList<ElectricalComponent> Components { get; }
        public IReadOnlyList<CircuitConnection> Connections { get; }

        internal CircuitGraph(List<CircuitNode> nodes, List<ElectricalComponent> components,
            List<CircuitConnection> connections, Dictionary<TerminalReference, int> terminalNodes)
        {
            Nodes = nodes.AsReadOnly();
            Components = components.AsReadOnly();
            Connections = connections.AsReadOnly();
            this.terminalNodes = terminalNodes;
        }

        public bool TryGetNode(TerminalReference terminal, out int nodeIndex) =>
            terminalNodes.TryGetValue(terminal, out nodeIndex);

        public int GetNode(TerminalReference terminal)
        {
            if (!TryGetNode(terminal, out int nodeIndex))
                throw new ArgumentException("The terminal is not part of this graph.", nameof(terminal));
            return nodeIndex;
        }
    }

    /// <summary>All terminals joined by ideal wires share this potential. This does not choose ground.</summary>
    public sealed class CircuitNode
    {
        public int Index { get; }
        public IReadOnlyList<TerminalReference> Terminals { get; }

        internal CircuitNode(int index, List<TerminalReference> terminals)
        {
            Index = index;
            Terminals = terminals.AsReadOnly();
        }
    }

    public readonly struct ElectricalTerminal
    {
        public string Id { get; }
        public int NodeIndex { get; }

        internal ElectricalTerminal(string id, int nodeIndex)
        {
            Id = id;
            NodeIndex = nodeIndex;
        }
    }

    /// <summary>Copied definition values plus instance terminals; no authoring asset or view reference.</summary>
    public sealed class ElectricalComponent
    {
        public string Id { get; }
        public string DefinitionId { get; }
        public ComponentType ComponentType { get; }
        public ElectricalParameters Parameters { get; }
        public IReadOnlyList<ElectricalTerminal> Terminals { get; }
        public int FromNode { get; }
        public int ToNode { get; }

        internal ElectricalComponent(string id, string definitionId, ComponentType componentType,
            ElectricalParameters parameters, List<ElectricalTerminal> terminals, int fromNode, int toNode)
        {
            Id = id;
            DefinitionId = definitionId;
            ComponentType = componentType;
            Parameters = parameters.Copy();
            Terminals = terminals.AsReadOnly();
            FromNode = fromNode;
            ToNode = toNode;
        }
    }

    /// <summary>Retains the authored wire identity for presentation; both endpoints share NodeIndex.</summary>
    public sealed class CircuitConnection
    {
        public string Id { get; }
        public TerminalReference From { get; }
        public TerminalReference To { get; }
        public int NodeIndex { get; }

        internal CircuitConnection(string id, TerminalReference from, TerminalReference to, int nodeIndex)
        {
            Id = id;
            From = from;
            To = to;
            NodeIndex = nodeIndex;
        }
    }
}
