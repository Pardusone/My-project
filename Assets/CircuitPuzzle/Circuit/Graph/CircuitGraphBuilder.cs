using System;
using System.Collections.Generic;
using CircuitPuzzle.Components;

namespace CircuitPuzzle.Circuit
{
    /// <summary>Normalizes only ideal wires. Components remain branches, including shorted branches.</summary>
    public static class CircuitGraphBuilder
    {
        public static CircuitGraph Build(BoardSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            List<PlacedComponent> components = ValidateComponents(snapshot);
            List<TerminalReference> terminals = CollectTerminals(components);
            Dictionary<TerminalReference, int> terminalIndices = new Dictionary<TerminalReference, int>();
            for (int index = 0; index < terminals.Count; index++)
                terminalIndices.Add(terminals[index], index);

            List<WireConnection> wires = ValidateWires(snapshot, terminalIndices);
            DisjointSet sets = new DisjointSet(terminals.Count);
            foreach (WireConnection wire in wires)
                sets.Union(terminalIndices[wire.From], terminalIndices[wire.To]);

            Dictionary<int, int> rootNodes = new Dictionary<int, int>();
            Dictionary<TerminalReference, int> terminalNodes = new Dictionary<TerminalReference, int>();
            List<List<TerminalReference>> nodeTerminals = new List<List<TerminalReference>>();
            for (int index = 0; index < terminals.Count; index++)
            {
                int root = sets.Find(index);
                if (!rootNodes.TryGetValue(root, out int node))
                {
                    node = nodeTerminals.Count;
                    rootNodes.Add(root, node);
                    nodeTerminals.Add(new List<TerminalReference>());
                }
                nodeTerminals[node].Add(terminals[index]);
                terminalNodes.Add(terminals[index], node);
            }

            List<CircuitNode> nodes = new List<CircuitNode>();
            for (int index = 0; index < nodeTerminals.Count; index++)
                nodes.Add(new CircuitNode(index, nodeTerminals[index]));

            List<ElectricalComponent> electricalComponents = new List<ElectricalComponent>();
            foreach (PlacedComponent placed in components)
            {
                ComponentDefinition definition = placed.Definition;
                List<ElectricalTerminal> componentTerminals = new List<ElectricalTerminal>();
                foreach (TerminalDefinition terminal in definition.Terminals)
                    componentTerminals.Add(new ElectricalTerminal(terminal.Id,
                        terminalNodes[new TerminalReference(placed.Id, terminal.Id)]));
                componentTerminals.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
                ElectricalParameters parameters = definition.Electrical;
                electricalComponents.Add(new ElectricalComponent(placed.Id, definition.StableId,
                    definition.ComponentType, parameters, componentTerminals,
                    terminalNodes[new TerminalReference(placed.Id, parameters.FromTerminalId)],
                    terminalNodes[new TerminalReference(placed.Id, parameters.ToTerminalId)]));
            }

            List<CircuitConnection> connections = new List<CircuitConnection>();
            foreach (WireConnection wire in wires)
                connections.Add(new CircuitConnection(wire.Id, wire.From, wire.To, terminalNodes[wire.From]));
            return new CircuitGraph(nodes, electricalComponents, connections, terminalNodes);
        }

        private static List<PlacedComponent> ValidateComponents(BoardSnapshot snapshot)
        {
            List<PlacedComponent> components = new List<PlacedComponent>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlacedComponent component in snapshot.Components)
            {
                if (component == null || string.IsNullOrWhiteSpace(component.Id) || !ids.Add(component.Id))
                    throw Invalid("A placed component is missing or has a duplicate instance ID.");
                ComponentDefinition definition = component.Definition;
                if (definition == null || string.IsNullOrWhiteSpace(definition.StableId))
                    throw Invalid($"Component '{component.Id}' has no valid definition.");
                ElectricalParameters electrical = definition.Electrical;
                if (electrical == null)
                    throw Invalid($"Component '{definition.StableId}' has no electrical parameters.");

                HashSet<string> terminalIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (TerminalDefinition terminal in definition.Terminals)
                    if (terminal == null || string.IsNullOrWhiteSpace(terminal.Id) || !terminalIds.Add(terminal.Id))
                        throw Invalid($"Component '{definition.StableId}' has invalid or duplicate terminal IDs.");
                if (string.IsNullOrWhiteSpace(electrical.FromTerminalId) ||
                    string.IsNullOrWhiteSpace(electrical.ToTerminalId) ||
                    electrical.FromTerminalId == electrical.ToTerminalId ||
                    !terminalIds.Contains(electrical.FromTerminalId) || !terminalIds.Contains(electrical.ToTerminalId))
                    throw Invalid($"Component '{definition.StableId}' needs two distinct existing electrical terminal IDs.");
                components.Add(component);
            }
            components.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            return components;
        }

        private static List<TerminalReference> CollectTerminals(List<PlacedComponent> components)
        {
            List<TerminalReference> terminals = new List<TerminalReference>();
            foreach (PlacedComponent component in components)
                foreach (TerminalDefinition terminal in component.Definition.Terminals)
                    terminals.Add(new TerminalReference(component.Id, terminal.Id));
            terminals.Sort((left, right) =>
            {
                int owner = StringComparer.Ordinal.Compare(left.ComponentId, right.ComponentId);
                return owner != 0 ? owner : StringComparer.Ordinal.Compare(left.TerminalId, right.TerminalId);
            });
            return terminals;
        }

        private static List<WireConnection> ValidateWires(BoardSnapshot snapshot,
            Dictionary<TerminalReference, int> terminalIndices)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            List<WireConnection> wires = new List<WireConnection>();
            foreach (WireConnection wire in snapshot.Wires)
            {
                if (wire == null || string.IsNullOrWhiteSpace(wire.Id) || !ids.Add(wire.Id) ||
                    wire.From == wire.To || !terminalIndices.ContainsKey(wire.From) ||
                    !terminalIndices.ContainsKey(wire.To))
                    throw Invalid("A wire has an invalid ID or references a missing or identical terminal.");
                wires.Add(wire);
            }
            wires.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            return wires;
        }

        private static ArgumentException Invalid(string message) => new ArgumentException(message, "snapshot");

        private sealed class DisjointSet
        {
            private readonly int[] parents;
            private readonly byte[] ranks;

            public DisjointSet(int count)
            {
                parents = new int[count];
                ranks = new byte[count];
                for (int index = 0; index < count; index++)
                    parents[index] = index;
            }

            public int Find(int index)
            {
                while (parents[index] != index)
                {
                    parents[index] = parents[parents[index]];
                    index = parents[index];
                }
                return index;
            }

            public void Union(int first, int second)
            {
                int firstRoot = Find(first);
                int secondRoot = Find(second);
                if (firstRoot == secondRoot)
                    return;
                if (ranks[firstRoot] < ranks[secondRoot])
                    parents[firstRoot] = secondRoot;
                else
                {
                    parents[secondRoot] = firstRoot;
                    if (ranks[firstRoot] == ranks[secondRoot])
                        ranks[firstRoot]++;
                }
            }
        }
    }
}
