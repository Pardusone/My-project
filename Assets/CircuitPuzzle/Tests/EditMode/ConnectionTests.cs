using CircuitPuzzle.Circuit;
using CircuitPuzzle.Components;
using NUnit.Framework;
using UnityEngine;

namespace CircuitPuzzle.Tests
{
    public sealed class ConnectionTests : BoardTestFixture
    {
        [Test]
        public void WiresRequireExistingDistinctTerminalsAndRejectBothDuplicateDirections()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = CreateDefinition();
            board.TryAddComponent(definition, Vector2Int.zero, 0, out string first);
            board.TryAddComponent(definition, new Vector2Int(4, 0), 0, out string second);
            TerminalReference a = new TerminalReference(first, "a");
            TerminalReference b = new TerminalReference(second, "b");
            Assert.That(board.TryConnect(a, a, out _), Is.False);
            Assert.That(board.TryConnect(a, new TerminalReference(first, "missing"), out _), Is.False);
            Assert.That(board.TryConnect(a, new TerminalReference("missing", "a"), out _), Is.False);
            Assert.That(board.TryConnect(a, default, out _), Is.False);
            Assert.That(board.TryConnect(a, b, out string wire), Is.True);
            Assert.That(board.TryConnect(a, b, out _), Is.False);
            Assert.That(board.TryConnect(b, a, out _), Is.False);
            Assert.That(board.Wires.Count, Is.EqualTo(1));
            Assert.That(board.FindWire(wire).From, Is.EqualTo(a));
            Assert.That(board.FindWire(wire).To, Is.EqualTo(b));
        }

        [Test]
        public void ConnectingTwoDifferentTerminalsOnSameComponentIsEditorValid()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            board.TryAddComponent(CreateDefinition(), Vector2Int.zero, 0, out string id);
            Assert.That(board.TryConnect(new TerminalReference(id, "a"), new TerminalReference(id, "b"),
                out _), Is.True);
            Assert.That(board.Wires.Count, Is.EqualTo(1));
        }

        [Test]
        public void WiresKeepTerminalReferencesAndFollowMoveAndRotation()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = CreateDefinition();
            board.TryAddComponent(definition, Vector2Int.zero, 0, out string first);
            board.TryAddComponent(definition, new Vector2Int(4, 0), 0, out string second);
            TerminalReference from = new TerminalReference(first, "a");
            TerminalReference to = new TerminalReference(second, "b");
            board.TryConnect(from, to, out string wireId);
            WireConnection originalWire = board.FindWire(wireId);
            Assert.That(board.TryMoveComponent(first, new Vector2Int(1, 2)), Is.True);
            Assert.That(board.GetTerminalPosition(from), Is.EqualTo(new Vector2(1, 2.5f)));
            Assert.That(board.TryRotateComponent(first), Is.True);
            Assert.That(board.GetTerminalPosition(from), Is.EqualTo(new Vector2(1.5f, 2)));
            Assert.That(board.GetTerminalPosition(to), Is.EqualTo(new Vector2(6, 0.5f)));
            Assert.That(board.FindWire(wireId), Is.SameAs(originalWire));
        }

        [Test]
        public void DeletingComponentRemovesEveryAttachedWireAndLeavesUnrelatedWires()
        {
            CircuitBoard board = new CircuitBoard(10, 8);
            ComponentDefinition definition = CreateDefinition();
            board.TryAddComponent(definition, Vector2Int.zero, 0, out string first);
            board.TryAddComponent(definition, new Vector2Int(3, 0), 0, out string second);
            board.TryAddComponent(definition, new Vector2Int(6, 0), 0, out string third);
            board.TryConnect(new TerminalReference(first, "a"), new TerminalReference(second, "a"), out _);
            board.TryConnect(new TerminalReference(third, "a"), new TerminalReference(first, "b"), out _);
            board.TryConnect(new TerminalReference(first, "a"), new TerminalReference(first, "b"), out _);
            board.TryConnect(new TerminalReference(second, "b"), new TerminalReference(third, "b"), out string retained);
            int changes = 0;
            board.Changed += () => changes++;
            Assert.That(board.TryRemoveComponent(first), Is.True);
            Assert.That(board.FindComponent(first), Is.Null);
            Assert.That(board.Wires.Count, Is.EqualTo(1));
            Assert.That(board.Wires[0].Id, Is.EqualTo(retained));
            Assert.That(changes, Is.EqualTo(1), "A cascading deletion is one logical edit.");
            Assert.That(board.TryRemoveComponent(first), Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void TerminalReferenceEqualityIncludesOwnerAndTerminalId()
        {
            TerminalReference reference = new TerminalReference("first", "a");
            TerminalReference equal = new TerminalReference("first", "a");
            Assert.That(reference == equal, Is.True);
            Assert.That(reference.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(reference != new TerminalReference("second", "a"), Is.True);
            Assert.That(reference != new TerminalReference("first", "b"), Is.True);
        }
    }
}
