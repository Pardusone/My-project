using System;
using System.Collections.Generic;
using CircuitPuzzle.Circuit;
using CircuitPuzzle.Components;
using NUnit.Framework;
using UnityEngine;

namespace CircuitPuzzle.Tests
{
    public sealed class EditorHistoryTests : BoardTestFixture
    {
        [TestCase("place")]
        [TestCase("move")]
        [TestCase("rotate")]
        [TestCase("delete component")]
        [TestCase("create wire")]
        [TestCase("delete wire")]
        public void UndoRestoresCompleteBoardForEachSupportedEdit(string action)
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = CreateDefinition();
            board.TryAddComponent(definition, Vector2Int.zero, 0, out string first);
            board.TryAddComponent(definition, new Vector2Int(4, 0), 0, out string second);
            board.TryConnect(new TerminalReference(first, "a"), new TerminalReference(second, "a"), out string wire);
            BoardSnapshot before = board.Capture();
            EditorHistory history = new EditorHistory(board);
            Func<bool> edit;
            switch (action)
            {
                case "place":
                    edit = () => board.TryAddComponent(definition, new Vector2Int(0, 4), 0, out _);
                    break;
                case "move":
                    edit = () => board.TryMoveComponent(first, new Vector2Int(1, 2));
                    break;
                case "rotate":
                    edit = () => board.TryRotateComponent(first);
                    break;
                case "delete component":
                    edit = () => board.TryRemoveComponent(first);
                    break;
                case "create wire":
                    edit = () => board.TryConnect(new TerminalReference(first, "b"),
                        new TerminalReference(second, "b"), out _);
                    break;
                default:
                    edit = () => board.TryRemoveWire(wire);
                    break;
            }

            Assert.That(history.Execute(edit), Is.True);
            Assert.That(history.CanUndo, Is.True);
            Assert.That(history.Undo(), Is.True);
            AssertBoardMatches(board, before);
            Assert.That(history.CanUndo, Is.False);
            Assert.That(history.Undo(), Is.False);
        }

        [Test]
        public void SequentialUndoRestoresDeletedComponentAndWireIdsThenReversesEarlierEdits()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            EditorHistory history = new EditorHistory(board);
            ComponentDefinition definition = CreateDefinition();
            string first = null;
            string second = null;
            string wire = null;
            history.Execute(() => board.TryAddComponent(definition, Vector2Int.zero, 0, out first));
            history.Execute(() => board.TryAddComponent(definition, new Vector2Int(4, 0), 0, out second));
            TerminalReference from = new TerminalReference(first, "a");
            TerminalReference to = new TerminalReference(second, "b");
            history.Execute(() => board.TryConnect(from, to, out wire));
            history.Execute(() => board.TryMoveComponent(first, new Vector2Int(1, 2)));
            history.Execute(() => board.TryRotateComponent(first));
            BoardSnapshot beforeDelete = board.Capture();
            history.Execute(() => board.TryRemoveComponent(first));
            Assert.That(board.Wires, Is.Empty);
            Assert.That(history.Undo(), Is.True);
            AssertBoardMatches(board, beforeDelete);
            Assert.That(board.FindWire(wire).From, Is.EqualTo(from));
            Assert.That(board.FindWire(wire).To, Is.EqualTo(to));
            history.Undo();
            Assert.That(board.FindComponent(first).Rotation, Is.Zero);
            history.Undo();
            Assert.That(board.FindComponent(first).Position, Is.EqualTo(Vector2Int.zero));
            history.Undo();
            Assert.That(board.Wires, Is.Empty);
            history.Undo();
            Assert.That(board.Components.Count, Is.EqualTo(1));
            history.Undo();
            Assert.That(board.Components, Is.Empty);
            Assert.That(history.CanUndo, Is.False);
        }

        [Test]
        public void RejectedAndUnchangedEditsDoNotConsumeOrEvictHistory()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            EditorHistory history = new EditorHistory(board, 1);
            ComponentDefinition definition = CreateDefinition();
            string id = null;
            Assert.That(history.Execute(() => board.TryAddComponent(definition, Vector2Int.zero, 0, out id)), Is.True);
            Assert.That(history.Execute(() => board.TryMoveComponent(id, new Vector2Int(-1, 0))), Is.False);
            Assert.That(history.Execute(() => board.TryMoveComponent(id, Vector2Int.zero)), Is.False);
            Assert.That(history.Execute(() => true), Is.False, "An unchanged board must not get an undo entry.");
            Assert.That(history.Undo(), Is.True);
            Assert.That(board.Components, Is.Empty);
        }

        [Test]
        public void CapacityDiscardsOnlyOldestUndoStates()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            EditorHistory history = new EditorHistory(board, 2);
            ComponentDefinition definition = CreateDefinition();
            string id = null;
            history.Execute(() => board.TryAddComponent(definition, Vector2Int.zero, 0, out id));
            history.Execute(() => board.TryMoveComponent(id, new Vector2Int(1, 2)));
            history.Execute(() => board.TryRotateComponent(id));
            Assert.That(history.Undo(), Is.True);
            Assert.That(history.Undo(), Is.True);
            Assert.That(history.Undo(), Is.False);
            Assert.That(board.FindComponent(id).Position, Is.EqualTo(Vector2Int.zero));
            Assert.That(board.FindComponent(id).Rotation, Is.Zero);
            Assert.That(board.Components.Count, Is.EqualTo(1), "The evicted placement edit cannot be undone.");
        }

        [Test]
        public void ResetRestoresEmptyInitialStateAndClearsUndo()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            BoardSnapshot initial = board.Capture();
            EditorHistory history = new EditorHistory(board);
            ComponentDefinition definition = CreateDefinition();
            string id = null;
            history.Execute(() => board.TryAddComponent(definition, Vector2Int.zero, 0, out id));
            history.Execute(() => board.TryConnect(new TerminalReference(id, "a"), new TerminalReference(id, "b"), out _));
            history.ResetTo(initial);
            Assert.That(board.Components, Is.Empty);
            Assert.That(board.Wires, Is.Empty);
            Assert.That(history.CanUndo, Is.False);
            Assert.That(history.Undo(), Is.False);
            history.Execute(() => board.TryAddComponent(definition, Vector2Int.zero, 0, out _));
            Assert.That(history.Undo(), Is.True, "Editing can continue after a reset.");
            Assert.That(board.Components, Is.Empty);
        }

        [Test]
        public void ResetCanRestoreNonEmptyInitialConstructionWithoutChangingIds()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            ComponentDefinition definition = CreateDefinition();
            board.TryAddComponent(definition, Vector2Int.zero, 0, out string id);
            board.TryConnect(new TerminalReference(id, "a"), new TerminalReference(id, "b"), out _);
            BoardSnapshot initial = board.Capture();
            EditorHistory history = new EditorHistory(board);
            history.Execute(() => board.TryRemoveComponent(id));
            history.ResetTo(initial);
            AssertBoardMatches(board, initial);
            Assert.That(history.CanUndo, Is.False);
        }

        [Test]
        public void SnapshotAndExposedCollectionsCannotMutateTheBoard()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            board.TryAddComponent(CreateDefinition(), Vector2Int.zero, 0, out string id);
            BoardSnapshot snapshot = board.Capture();
            Assert.Throws<NotSupportedException>(() => ((IList<PlacedComponent>)board.Components).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<WireConnection>)board.Wires).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<PlacedComponent>)snapshot.Components).Clear());
            board.TryMoveComponent(id, new Vector2Int(2, 2));
            Assert.That(snapshot.Components[0].Position, Is.EqualTo(Vector2Int.zero));
            Assert.That(board.FindComponent(id).Position, Is.EqualTo(new Vector2Int(2, 2)));
        }

        [Test]
        public void RestoreRejectsWrongDimensionsWithoutModifyingLiveBoard()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            board.TryAddComponent(CreateDefinition(), Vector2Int.zero, 0, out _);
            BoardSnapshot before = board.Capture();
            Assert.Throws<ArgumentException>(() => board.Restore(new CircuitBoard(4, 4).Capture()));
            AssertBoardMatches(board, before);
        }

        [Test]
        public void RestoreAndResetEmitExactlyOneEventOnlyWhenBoardChanges()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            int changes = 0;
            board.Changed += () => changes++;
            board.Reset();
            Assert.That(changes, Is.Zero);
            board.TryAddComponent(CreateDefinition(), Vector2Int.zero, 0, out _);
            BoardSnapshot snapshot = board.Capture();
            Assert.That(changes, Is.EqualTo(1));
            board.Restore(snapshot);
            Assert.That(changes, Is.EqualTo(1));
            board.Reset();
            Assert.That(changes, Is.EqualTo(2));
            board.Restore(snapshot);
            Assert.That(changes, Is.EqualTo(3));
            board.Restore(snapshot);
            Assert.That(changes, Is.EqualTo(3));
        }

        private static void AssertBoardMatches(CircuitBoard board, BoardSnapshot expected)
        {
            Assert.That(board.Components.Count, Is.EqualTo(expected.Components.Count));
            Assert.That(board.Wires.Count, Is.EqualTo(expected.Wires.Count));
            foreach (PlacedComponent component in expected.Components)
            {
                PlacedComponent actual = board.FindComponent(component.Id);
                Assert.That(actual, Is.Not.Null);
                Assert.That(actual.Definition, Is.SameAs(component.Definition));
                Assert.That(actual.Position, Is.EqualTo(component.Position));
                Assert.That(actual.Rotation, Is.EqualTo(component.Rotation));
            }
            foreach (WireConnection wire in expected.Wires)
            {
                WireConnection actual = board.FindWire(wire.Id);
                Assert.That(actual, Is.Not.Null);
                Assert.That(actual.From, Is.EqualTo(wire.From));
                Assert.That(actual.To, Is.EqualTo(wire.To));
            }
        }
    }
}
