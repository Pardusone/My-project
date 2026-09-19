using CircuitPuzzle.Circuit;
using CircuitPuzzle.Components;
using NUnit.Framework;
using UnityEngine;

namespace CircuitPuzzle.Tests
{
    public sealed class GridAndPlacementTests : BoardTestFixture
    {
        [TestCase(0, 3, 2, 0.5f, 1.5f)]
        [TestCase(1, 2, 3, 0.5f, 0.5f)]
        [TestCase(2, 3, 2, 2.5f, 0.5f)]
        [TestCase(3, 2, 3, 1.5f, 2.5f)]
        [TestCase(-1, 2, 3, 1.5f, 2.5f)]
        [TestCase(4, 3, 2, 0.5f, 1.5f)]
        public void RotationTransformsFootprintAndLocalTerminalTogether(int rotation, int width, int height,
            float terminalX, float terminalY)
        {
            Vector2Int originalSize = new Vector2Int(3, 2);
            Assert.That(GridGeometry.RotatedSize(originalSize, rotation), Is.EqualTo(new Vector2Int(width, height)));
            Assert.That(GridGeometry.RotateLocalPoint(new Vector2(0.5f, 1.5f), originalSize, rotation),
                Is.EqualTo(new Vector2(terminalX, terminalY)));
        }

        [TestCase(3.1f, 4.2f, 2, 4)]
        [TestCase(3.9f, 4.7f, 3, 4)]
        [TestCase(-0.2f, 0.2f, -1, 0)]
        public void PointerSnapsToNearestOriginUsingFootprintCenter(float x, float y, int expectedX, int expectedY)
        {
            Assert.That(GridGeometry.NearestOrigin(new Vector2(x, y), new Vector2Int(2, 1)),
                Is.EqualTo(new Vector2Int(expectedX, expectedY)));
        }

        [Test]
        public void PlacementRejectsOutsideBoardAndOverlapButAllowsAdjacentFootprints()
        {
            CircuitBoard board = new CircuitBoard(6, 4);
            ComponentDefinition definition = CreateDefinition();
            Assert.That(board.TryAddComponent(definition, new Vector2Int(2, 1), 0, out _), Is.True);
            Assert.That(board.CanPlace(definition, new Vector2Int(3, 1), 0), Is.False);
            Assert.That(board.CanPlace(definition, new Vector2Int(4, 1), 0), Is.True);
            Assert.That(board.CanPlace(definition, new Vector2Int(2, 2), 0), Is.True);
            Assert.That(board.CanPlace(definition, new Vector2Int(4, 3), 0), Is.True);
            Assert.That(board.CanPlace(definition, new Vector2Int(5, 3), 0), Is.False);
            Assert.That(board.CanPlace(definition, new Vector2Int(0, 4), 0), Is.False);
            Assert.That(board.CanPlace(definition, new Vector2Int(-1, 0), 0), Is.False);
            Assert.That(board.CanPlace(definition, new Vector2Int(0, -1), 0), Is.False);
        }

        [Test]
        public void RejectedMoveAndNoOpPreserveStateAndEmitNoChange()
        {
            CircuitBoard board = new CircuitBoard(6, 4);
            ComponentDefinition definition = CreateDefinition();
            board.TryAddComponent(definition, Vector2Int.zero, 0, out string id);
            board.TryAddComponent(definition, new Vector2Int(2, 0), 0, out _);
            PlacedComponent original = board.FindComponent(id);
            int changes = 0;
            board.Changed += () => changes++;
            Assert.That(board.TryMoveComponent(id, new Vector2Int(1, 0)), Is.False);
            Assert.That(board.TryMoveComponent(id, new Vector2Int(-1, 0)), Is.False);
            Assert.That(board.TryMoveComponent(id, Vector2Int.zero), Is.False);
            Assert.That(board.FindComponent(id), Is.SameAs(original));
            Assert.That(changes, Is.Zero);
            Assert.That(board.TryMoveComponent(id, new Vector2Int(0, 1)), Is.True);
            Assert.That(board.FindComponent(id).Id, Is.EqualTo(id));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void RotationRejectsBoundsAndOverlapWithoutChangingPlacement()
        {
            CircuitBoard board = new CircuitBoard(6, 4);
            ComponentDefinition definition = CreateDefinition();
            board.TryAddComponent(definition, new Vector2Int(0, 3), 0, out string edge);
            board.TryAddComponent(definition, Vector2Int.zero, 0, out string blocked);
            board.TryAddComponent(definition, new Vector2Int(0, 1), 0, out _);
            int changes = 0;
            board.Changed += () => changes++;
            Assert.That(board.TryRotateComponent(edge), Is.False);
            Assert.That(board.TryRotateComponent(blocked), Is.False);
            Assert.That(board.FindComponent(edge).Rotation, Is.Zero);
            Assert.That(board.FindComponent(blocked).Rotation, Is.Zero);
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void AuthoredAllowedRotationsAreRespected()
        {
            CircuitBoard board = new CircuitBoard(6, 4);
            ComponentDefinition limited = CreateDefinition(rotations: new[] { 0, 2 });
            Assert.That(board.TryAddComponent(limited, Vector2Int.zero, 1, out _), Is.False);
            Assert.That(board.TryAddComponent(limited, Vector2Int.zero, 0, out string id), Is.True);
            Assert.That(board.TryRotateComponent(id), Is.True);
            Assert.That(board.FindComponent(id).Rotation, Is.EqualTo(2));
            Assert.That(board.TryRotateComponent(id), Is.True);
            Assert.That(board.FindComponent(id).Rotation, Is.Zero);
            ComponentDefinition fixedRotation = CreateDefinition(rotations: new[] { 0 });
            board.TryAddComponent(fixedRotation, new Vector2Int(3, 0), 0, out string fixedId);
            Assert.That(board.TryRotateComponent(fixedId), Is.False);
        }

        [Test]
        public void EveryQuarterTurnPreservesTerminalIdentityAndGridAlignment()
        {
            CircuitBoard board = new CircuitBoard(8, 8);
            board.TryAddComponent(CreateDefinition(), new Vector2Int(2, 2), 0, out string id);
            TerminalReference terminal = new TerminalReference(id, "a");
            Vector2[] expected = { new Vector2(2, 2.5f), new Vector2(2.5f, 2),
                new Vector2(4, 2.5f), new Vector2(2.5f, 4) };
            for (int rotation = 0; rotation < 4; rotation++)
            {
                Assert.That(board.GetTerminalPosition(terminal), Is.EqualTo(expected[rotation]));
                Assert.That(board.FindComponent(id).Position, Is.EqualTo(new Vector2Int(2, 2)));
                Assert.That(board.TryRotateComponent(id), Is.True);
            }
            Assert.That(board.FindComponent(id).Rotation, Is.Zero);
            Assert.That(board.GetTerminalPosition(terminal), Is.EqualTo(expected[0]));
        }
    }
}
