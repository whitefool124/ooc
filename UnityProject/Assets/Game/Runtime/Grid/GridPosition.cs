using System;

namespace OCC.Combat
{
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X}, {Y})";

        public int ManhattanDistance(GridPosition other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public static GridPosition operator +(GridPosition left, GridPosition right) => new GridPosition(left.X + right.X, left.Y + right.Y);

        public static GridPosition operator *(GridPosition value, int scalar) => new GridPosition(value.X * scalar, value.Y * scalar);

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);

        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);
    }
}
