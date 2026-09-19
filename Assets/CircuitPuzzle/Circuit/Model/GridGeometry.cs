using UnityEngine;

namespace CircuitPuzzle.Circuit
{
    /// <summary>Grid geometry uses lower-left origins and positive counterclockwise quarter turns.</summary>
    public static class GridGeometry
    {
        public static int NormalizeRotation(int quarterTurns) => ((quarterTurns % 4) + 4) % 4;

        public static Vector2Int RotatedSize(Vector2Int originalSize, int quarterTurns)
        {
            return NormalizeRotation(quarterTurns) % 2 == 0
                ? originalSize : new Vector2Int(originalSize.y, originalSize.x);
        }

        public static Vector2 RotateLocalPoint(Vector2 point, Vector2Int originalSize, int quarterTurns)
        {
            switch (NormalizeRotation(quarterTurns))
            {
                case 1: return new Vector2(originalSize.y - point.y, point.x);
                case 2: return new Vector2(originalSize.x - point.x, originalSize.y - point.y);
                case 3: return new Vector2(point.y, originalSize.x - point.x);
                default: return point;
            }
        }

        /// <summary>Place the footprint around the pointer, snapping its origin to the nearest cell.</summary>
        public static Vector2Int NearestOrigin(Vector2 gridPoint, Vector2Int rotatedSize)
        {
            return new Vector2Int(Mathf.RoundToInt(gridPoint.x - rotatedSize.x * 0.5f),
                Mathf.RoundToInt(gridPoint.y - rotatedSize.y * 0.5f));
        }
    }
}
