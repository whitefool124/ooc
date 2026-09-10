using System;
using System.Collections.Generic;
using System.Linq;

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

        public IReadOnlyList<GridPosition> FindShortestPath(GridPosition from, GridPosition to, int maximumSteps,
            Func<GridPosition, bool> isOccupied = null)
        {
            if (!IsInside(from) || !IsInside(to) || maximumSteps < 0 || IsBlocked(to) || (isOccupied != null && isOccupied(to)))
                return Array.Empty<GridPosition>();
            Queue<GridPosition> open = new Queue<GridPosition>();
            Dictionary<GridPosition, GridPosition?> previous = new Dictionary<GridPosition, GridPosition?>();
            Dictionary<GridPosition, int> distance = new Dictionary<GridPosition, int>();
            open.Enqueue(from); previous[from] = null; distance[from] = 0;
            GridPosition[] offsets = { new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0) };
            while (open.Count > 0)
            {
                GridPosition current = open.Dequeue();
                if (current == to) break;
                if (distance[current] >= maximumSteps) continue;
                foreach (GridPosition offset in offsets)
                {
                    GridPosition next = current + offset;
                    if (previous.ContainsKey(next) || !IsInside(next) || IsBlocked(next) || (isOccupied != null && isOccupied(next))) continue;
                    previous[next] = current; distance[next] = distance[current] + 1; open.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(to)) return Array.Empty<GridPosition>();
            List<GridPosition> path = new List<GridPosition>();
            for (GridPosition? step = to; step.HasValue; step = previous[step.Value]) path.Add(step.Value);
            path.Reverse();
            return path;
        }

        public IReadOnlyList<GridPosition> FindLowestCostPath(GridPosition from, GridPosition to, int maximumCost,
            Func<GridPosition, int> entryCost, Func<GridPosition, bool> isOccupied = null)
        {
            if (!IsInside(from) || !IsInside(to) || maximumCost < 0 || IsBlocked(to) ||
                (isOccupied != null && isOccupied(to))) return Array.Empty<GridPosition>();
            Func<GridPosition, int> cost = entryCost ?? (_ => 1);
            List<GridPosition> open = new List<GridPosition> { from };
            Dictionary<GridPosition, GridPosition?> previous = new Dictionary<GridPosition, GridPosition?> { [from] = null };
            Dictionary<GridPosition, int> distance = new Dictionary<GridPosition, int> { [from] = 0 };
            Dictionary<GridPosition, int> sequence = new Dictionary<GridPosition, int> { [from] = 0 };
            int nextSequence = 1;
            GridPosition[] offsets = { new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0) };
            while (open.Count > 0)
            {
                GridPosition current = open.OrderBy(value => distance[value]).ThenBy(value => sequence[value]).First();
                open.Remove(current);
                if (current == to) break;
                foreach (GridPosition offset in offsets)
                {
                    GridPosition next = current + offset;
                    if (!IsInside(next) || IsBlocked(next) || (isOccupied != null && isOccupied(next))) continue;
                    int stepCost = Math.Max(1, cost(next));
                    int candidate = distance[current] + stepCost;
                    if (candidate > maximumCost || distance.TryGetValue(next, out int known) && known <= candidate) continue;
                    distance[next] = candidate;
                    previous[next] = current;
                    if (!sequence.ContainsKey(next)) sequence[next] = nextSequence++;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            if (!previous.ContainsKey(to)) return Array.Empty<GridPosition>();
            List<GridPosition> path = new List<GridPosition>();
            for (GridPosition? step = to; step.HasValue; step = previous[step.Value]) path.Add(step.Value);
            path.Reverse();
            return path;
        }

        public bool HasLineOfSight(GridPosition from, GridPosition to)
        {
            if (from.ManhattanDistance(to) > 1 && (GetTile(from).IsLampVine || GetTile(to).IsLampVine)) return false;
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
