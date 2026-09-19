using System;
using System.Collections.Generic;
using CircuitPuzzle.Circuit;
using CircuitPuzzle.Components;
using NUnit.Framework;
using UnityEngine;

namespace CircuitPuzzle.Tests
{
    public sealed class CircuitGraphTests : BoardTestFixture
    {
        [Test]
        public void EmptyBoardBuildsAnEmptyGraphAndUnknownTerminalLookupIsExplicit()
        {
            CircuitGraph graph = CircuitGraphBuilder.Build(new CircuitBoard(6, 8).Capture());
            Assert.That(graph.Nodes, Is.Empty);
            Assert.That(graph.Components, Is.Empty);
            Assert.That(graph.Connections, Is.Empty);
            Assert.That(graph.TryGetNode(default, out _), Is.False);
            Assert.Throws<ArgumentException>(() => graph.GetNode(default));
            Assert.Throws<ArgumentNullException>(() => CircuitGraphBuilder.Build(null));
        }

        [Test]
        public void UnconnectedTerminalsAreSeparateNodesAndComponentsDoNotMergeThem()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = ElectricalDefinition();
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            CircuitGraph graph = CircuitGraphBuilder.Build(board.Capture());
            Assert.That(graph.Nodes.Count, Is.EqualTo(4));
            Assert.That(graph.Components.Count, Is.EqualTo(2));
            HashSet<int> indices = new HashSet<int>
            {
                graph.GetNode(Port(first, "a")), graph.GetNode(Port(first, "b")),
                graph.GetNode(Port(second, "a")), graph.GetNode(Port(second, "b"))
            };
            Assert.That(indices.Count, Is.EqualTo(4));
            foreach (CircuitNode node in graph.Nodes)
            {
                Assert.That(node.Terminals.Count, Is.EqualTo(1));
                Assert.That(graph.GetNode(node.Terminals[0]), Is.EqualTo(node.Index));
            }
        }

        [Test]
        public void TransitiveWireChainAndCycleShareOnePotentialButOtherPortsStaySeparate()
        {
            CircuitBoard board = new CircuitBoard(10, 8);
            ComponentDefinition definition = ElectricalDefinition();
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            string third = Place(board, definition, 6);
            Connect(board, Port(first, "a"), Port(second, "a"));
            Connect(board, Port(second, "a"), Port(third, "a"));
            Connect(board, Port(third, "a"), Port(first, "a"));
            CircuitGraph graph = CircuitGraphBuilder.Build(board.Capture());
            int net = graph.GetNode(Port(first, "a"));
            Assert.That(graph.GetNode(Port(second, "a")), Is.EqualTo(net));
            Assert.That(graph.GetNode(Port(third, "a")), Is.EqualTo(net));
            Assert.That(graph.Nodes[net].Terminals.Count, Is.EqualTo(3));
            Assert.That(graph.Nodes.Count, Is.EqualTo(4));
            Assert.That(graph.Connections.Count, Is.EqualTo(3), "Cycle wires keep their editor identities.");
            foreach (CircuitConnection wire in graph.Connections)
            {
                Assert.That(wire.NodeIndex, Is.EqualTo(net));
                Assert.That(graph.GetNode(wire.From), Is.EqualTo(wire.NodeIndex));
                Assert.That(graph.GetNode(wire.To), Is.EqualTo(wire.NodeIndex));
                Assert.That(board.FindWire(wire.Id), Is.Not.Null);
            }
        }

        [Test]
        public void SameComponentWireRetainsAShortedBatteryBranchForFutureDiagnosis()
        {
            CircuitBoard board = new CircuitBoard(6, 8);
            string id = Place(board, ElectricalDefinition(ComponentType.Battery, 5), 0);
            Connect(board, Port(id, "a"), Port(id, "b"));
            CircuitGraph graph = CircuitGraphBuilder.Build(board.Capture());
            Assert.That(graph.Nodes.Count, Is.EqualTo(1));
            Assert.That(graph.Components.Count, Is.EqualTo(1));
            Assert.That(graph.Components[0].FromNode, Is.EqualTo(graph.Components[0].ToNode));
            Assert.That(graph.Components[0].ComponentType, Is.EqualTo(ComponentType.Battery));
            Assert.That(graph.Components[0].Parameters.VoltageVolts, Is.EqualTo(5));
        }

        [Test]
        public void ExplicitElectricalDirectionDoesNotDependOnTerminalArrayOrAlphabeticalOrder()
        {
            CircuitBoard board = new CircuitBoard(6, 8);
            ComponentDefinition definition = ElectricalDefinition(ComponentType.Battery, 9);
            definition.ConfigureElectrical(new ElectricalParameters("b", "a", 9));
            string id = Place(board, definition, 0);
            CircuitGraph graph = CircuitGraphBuilder.Build(board.Capture());
            ElectricalComponent component = graph.Components[0];
            Assert.That(component.FromNode, Is.EqualTo(graph.GetNode(Port(id, "b"))));
            Assert.That(component.ToNode, Is.EqualTo(graph.GetNode(Port(id, "a"))));
            Assert.That(component.Terminals[0].Id, Is.EqualTo("a"));
            Assert.That(component.Parameters.FromTerminalId, Is.EqualTo("b"));
        }

        [Test]
        public void NodeNumberingIsDeterministicAcrossRepeatedBuildsAndWireInsertionDirections()
        {
            CircuitBoard board = new CircuitBoard(10, 8);
            ComponentDefinition definition = ElectricalDefinition();
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            string third = Place(board, definition, 6);
            string wireA = Connect(board, Port(first, "a"), Port(second, "b"));
            string wireB = Connect(board, Port(second, "b"), Port(third, "a"));
            CircuitGraph before = CircuitGraphBuilder.Build(board.Capture());
            AssertSameNodeMapping(before, CircuitGraphBuilder.Build(board.Capture()));
            board.TryRemoveWire(wireA);
            board.TryRemoveWire(wireB);
            Connect(board, Port(third, "a"), Port(second, "b"));
            Connect(board, Port(second, "b"), Port(first, "a"));
            AssertSameNodeMapping(before, CircuitGraphBuilder.Build(board.Capture()));
        }

        [Test]
        public void MoveAndRotationLeaveTheElectricalGraphUnchanged()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = ElectricalDefinition();
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            Connect(board, Port(first, "a"), Port(second, "b"));
            CircuitGraph before = CircuitGraphBuilder.Build(board.Capture());
            Assert.That(board.TryMoveComponent(first, new Vector2Int(0, 3)), Is.True);
            Assert.That(board.TryRotateComponent(first), Is.True);
            CircuitGraph after = CircuitGraphBuilder.Build(board.Capture());
            AssertSameNodeMapping(before, after);
            Assert.That(after.Connections[0].Id, Is.EqualTo(before.Connections[0].Id));
        }

        [Test]
        public void WireDeletionAndUndoRebuildWithoutStaleNodeMappings()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = ElectricalDefinition();
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            string wire = Connect(board, Port(first, "a"), Port(second, "b"));
            CircuitGraph connected = CircuitGraphBuilder.Build(board.Capture());
            EditorHistory history = new EditorHistory(board);
            Assert.That(history.Execute(() => board.TryRemoveWire(wire)), Is.True);
            CircuitGraph disconnected = CircuitGraphBuilder.Build(board.Capture());
            Assert.That(disconnected.Nodes.Count, Is.EqualTo(4));
            Assert.That(disconnected.GetNode(Port(first, "a")),
                Is.Not.EqualTo(disconnected.GetNode(Port(second, "b"))));
            Assert.That(history.Undo(), Is.True);
            AssertSameNodeMapping(connected, CircuitGraphBuilder.Build(board.Capture()));
        }

        [Test]
        public void SnapshotCopiesElectricalValuesAndSurvivesDefinitionChangesAndBoardDeletion()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = ElectricalDefinition(ComponentType.Battery, 5);
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            Connect(board, Port(first, "a"), Port(second, "b"));
            CircuitGraph graph = CircuitGraphBuilder.Build(board.Capture());
            Assert.That(graph.Components[0].Parameters, Is.Not.SameAs(definition.Electrical));
            Assert.That(graph.Components[0].Parameters, Is.Not.SameAs(graph.Components[1].Parameters));
            definition.ConfigureElectrical(new ElectricalParameters("b", "a", 12, 100));
            board.TryRemoveComponent(first);
            Assert.That(graph.Components.Count, Is.EqualTo(2));
            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            Assert.That(graph.Nodes.Count, Is.EqualTo(3));
            foreach (ElectricalComponent component in graph.Components)
            {
                Assert.That(component.Parameters.VoltageVolts, Is.EqualTo(5));
                Assert.That(component.Parameters.FromTerminalId, Is.EqualTo("a"));
                Assert.That(component.DefinitionId, Is.EqualTo(definition.StableId));
            }
            CircuitGraph latest = CircuitGraphBuilder.Build(board.Capture());
            Assert.That(latest.Components.Count, Is.EqualTo(1));
            Assert.That(latest.TryGetNode(Port(first, "a"), out _), Is.False);
            Assert.That(latest.Components[0].Parameters.VoltageVolts, Is.EqualTo(12));
        }

        [Test]
        public void SharedDefinitionsRetainDistinctInstanceIdsAndNodes()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = ElectricalDefinition();
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            CircuitGraph graph = CircuitGraphBuilder.Build(board.Capture());
            Assert.That(graph.Components[0].Id, Is.Not.EqualTo(graph.Components[1].Id));
            Assert.That(graph.Components[0].DefinitionId, Is.EqualTo(graph.Components[1].DefinitionId));
            Assert.That(graph.GetNode(Port(first, "a")), Is.Not.EqualTo(graph.GetNode(Port(second, "a"))));
        }

        [Test]
        public void MissingElectricalDataFailsWithAnAuthoringExplanation()
        {
            CircuitBoard board = new CircuitBoard(6, 8);
            Place(board, CreateDefinition(), 0);
            ArgumentException error = Assert.Throws<ArgumentException>(() => CircuitGraphBuilder.Build(board.Capture()));
            Assert.That(error.Message, Does.Contain("electrical parameters"));
        }

        [TestCase(null, "b")]
        [TestCase("", "b")]
        [TestCase("a", " ")]
        [TestCase("a", "a")]
        [TestCase("missing", "b")]
        [TestCase("A", "b")]
        public void InvalidElectricalPortMappingsFailClearly(string from, string to)
        {
            CircuitBoard board = new CircuitBoard(6, 8);
            ComponentDefinition definition = ElectricalDefinition();
            definition.ConfigureElectrical(new ElectricalParameters(from, to, resistanceOhms: 330));
            Place(board, definition, 0);
            ArgumentException error = Assert.Throws<ArgumentException>(() => CircuitGraphBuilder.Build(board.Capture()));
            Assert.That(error.Message, Does.Contain("electrical terminal IDs"));
        }

        [Test]
        public void AuthoringChangesThatRemoveAnExistingWireTerminalAreRejected()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = ElectricalDefinition();
            string first = Place(board, definition, 0);
            string second = Place(board, definition, 3);
            Connect(board, Port(first, "a"), Port(second, "b"));
            ConfigureTerminals(definition, "a", "c");
            definition.ConfigureElectrical(new ElectricalParameters("a", "c", resistanceOhms: 330));
            ArgumentException error = Assert.Throws<ArgumentException>(() => CircuitGraphBuilder.Build(board.Capture()));
            Assert.That(error.Message, Does.Contain("wire"));
        }

        [Test]
        public void DuplicateTerminalIdsIntroducedAfterPlacementAreRejected()
        {
            CircuitBoard board = new CircuitBoard(6, 8);
            ComponentDefinition definition = ElectricalDefinition();
            Place(board, definition, 0);
            ConfigureTerminals(definition, "a", "a");
            ArgumentException error = Assert.Throws<ArgumentException>(() => CircuitGraphBuilder.Build(board.Capture()));
            Assert.That(error.Message, Does.Contain("duplicate terminal IDs"));
        }

        private ComponentDefinition ElectricalDefinition(ComponentType type = ComponentType.Resistor, double voltage = 0)
        {
            ComponentDefinition definition = CreateDefinition();
            definition.Configure("test." + type, type.ToString(), type, "", "", null, Color.white,
                new Vector2Int(2, 1), new[] { 0, 1, 2, 3 }, new[]
                {
                    new TerminalDefinition("a", "Terminal A", "A", new Vector2(0, 0.5f)),
                    new TerminalDefinition("b", "Terminal B", "B", new Vector2(2, 0.5f))
                });
            definition.ConfigureElectrical(new ElectricalParameters("a", "b", voltage, 330));
            return definition;
        }

        private static void ConfigureTerminals(ComponentDefinition definition, string first, string second)
        {
            definition.Configure(definition.StableId, definition.DisplayName, definition.ComponentType,
                "", "", null, Color.white, new Vector2Int(2, 1), new[] { 0, 1, 2, 3 }, new[]
                {
                    new TerminalDefinition(first, first, first, new Vector2(0, 0.5f)),
                    new TerminalDefinition(second, second, second, new Vector2(2, 0.5f))
                });
        }

        private static string Place(CircuitBoard board, ComponentDefinition definition, int x)
        {
            Assert.That(board.TryAddComponent(definition, new Vector2Int(x, 0), 0, out string id), Is.True);
            return id;
        }

        private static string Connect(CircuitBoard board, TerminalReference from, TerminalReference to)
        {
            Assert.That(board.TryConnect(from, to, out string id), Is.True);
            return id;
        }

        private static TerminalReference Port(string owner, string terminal) => new TerminalReference(owner, terminal);

        private static void AssertSameNodeMapping(CircuitGraph expected, CircuitGraph actual)
        {
            Assert.That(actual.Nodes.Count, Is.EqualTo(expected.Nodes.Count));
            foreach (CircuitNode node in expected.Nodes)
                foreach (TerminalReference terminal in node.Terminals)
                    Assert.That(actual.GetNode(terminal), Is.EqualTo(node.Index));
        }
    }
}
