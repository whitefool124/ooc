using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public sealed class GridMap
    {
        private readonly HashSet<GridPosition> blockedPositions;
        private readonly Dictionary<GridPosition, TileState> tiles = new Dictionary<GridPosition, TileState>();

        public int Width { get; }
        public int Height { get; }

        public GridMap(int width, int height, IEnumerable<GridPosition> blockedPositions = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            this.blockedPositions = blockedPositions == null
                ? new HashSet<GridPosition>()
                : new HashSet<GridPosition>(blockedPositions);
        }

        public bool IsInside(GridPosition position) =>
            position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

        public bool IsBlocked(GridPosition position) => blockedPositions.Contains(position) || GetTile(position).BlocksMovement;

        public bool HasLineOfSight(GridPosition from, GridPosition to)
        {
            int x = from.X;
            int y = from.Y;
            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);
            int sx = from.X < to.X ? 1 : -1;
            int sy = from.Y < to.Y ? 1 : -1;
            int error = dx - dy;
            while (x != to.X || y != to.Y)
            {
                if (!(x == from.X && y == from.Y) && GetTile(new GridPosition(x, y)).BlocksLineOfSight) return false;
                int twice = 2 * error;
                if (twice > -dy) { error -= dy; x += sx; }
                if (twice < dx) { error += dx; y += sy; }
            }
            return true;
        }

        public TileState GetTile(GridPosition position) => tiles.TryGetValue(position, out TileState tile) ? tile : TileState.Empty;

        public IEnumerable<GridPosition> PositionsWith(Func<TileState, bool> predicate)
        {
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
            {
                GridPosition position = new GridPosition(x, y);
                if (predicate(GetTile(position))) yield return position;
            }
        }

        public void SetTile(GridPosition position, TileState tile)
        {
            if (!IsInside(position)) throw new ArgumentOutOfRangeException(nameof(position));
            tiles[position] = tile ?? TileState.Empty;
        }

        public GridMap Clone()
        {
            GridMap clone = new GridMap(Width, Height, blockedPositions);
            foreach (KeyValuePair<GridPosition, TileState> pair in tiles) clone.tiles[pair.Key] = pair.Value.Clone();
            return clone;
        }
    }
}
