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
            : this(cellSize, sourceHeight, sourceHeight, frontRows)
        {
        }

        public CombatObjectLayerLayout(float cellSize, int sourceWidth, int sourceHeight, int frontRows)
        {
            int rows = Mathf.Clamp(frontRows, 0, Mathf.Max(0, sourceHeight - 1));
            // Objects retain their authored native-32 canvas: a 64px prop is a visual
            // two-cell-wide overhang, not a two-cell gameplay footprint.
            float scale = sourceWidth > 0 ? cellSize / 32f : 1f;
            float fullWidth = sourceWidth > 0 ? sourceWidth * scale : cellSize;
            float fullHeight = sourceHeight > 0 ? sourceHeight * scale : cellSize;
            float fraction = sourceHeight > 0 ? (float)rows / sourceHeight : 0;
            float frontHeight = fullHeight * fraction;
            BackUv = new Rect(0, fraction, 1, 1 - fraction);
            FrontUv = new Rect(0, 0, 1, fraction);
            float left = (cellSize - fullWidth) * .5f;
            float top = cellSize - fullHeight;
            BackRect = new Rect(left, top, fullWidth, fullHeight - frontHeight);
            FrontRect = new Rect(left, top + fullHeight - frontHeight, fullWidth, frontHeight);
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
