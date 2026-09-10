using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>Pixel-aligned UV partition; keeps the original texture and its authored alpha.</summary>
    public readonly struct CombatObjectLayerLayout
    {
        public const int LampVineFrontRows = 10;
        public Rect BackUv { get; }
        public Rect FrontUv { get; }
        public Rect BackRect { get; }
        public Rect FrontRect { get; }
        public bool HasFront => FrontRect.height > 0;

        public CombatObjectLayerLayout(float cellSize, int sourceHeight, int frontRows)
        {
            int rows = Mathf.Clamp(frontRows, 0, Mathf.Max(0, sourceHeight - 1));
            float fraction = sourceHeight > 0 ? (float)rows / sourceHeight : 0;
            float frontHeight = cellSize * fraction;
            BackUv = new Rect(0, fraction, 1, 1 - fraction);
            FrontUv = new Rect(0, 0, 1, fraction);
            BackRect = new Rect(0, 0, cellSize, cellSize - frontHeight);
            FrontRect = new Rect(0, cellSize - frontHeight, cellSize, frontHeight);
        }

        public static int CompareDepth(Vector2 firstFoot, bool firstFront, Vector2 secondFoot, bool secondFront)
        {
            int row = firstFoot.y.CompareTo(secondFoot.y);
            if (row != 0) return row;
            // All front leaves on a row cover feet on that row, including sideways overhang.
            int layer = firstFront.CompareTo(secondFront);
            return layer != 0 ? layer : firstFoot.x.CompareTo(secondFoot.x);
        }
    }
}
