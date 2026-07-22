using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class InventoryItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Width { get; }
        public int Height { get; }

        public InventoryItem(string id, string displayName, int width = 1, int height = 1)
        {
            Id = id; DisplayName = displayName; Width = width; Height = height;
        }
    }

    public sealed class InventoryGrid
    {
        private readonly InventoryItem[,] cells;
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<InventoryItem> Items => cells.Cast<InventoryItem>().Where(item => item != null).Distinct().ToList();

        public InventoryGrid(int width, int height)
        {
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width; Height = height; cells = new InventoryItem[width, height];
        }

        public InventoryItem GetAt(int x, int y) => x < 0 || x >= Width || y < 0 || y >= Height ? null : cells[x, y];
        public bool CanPlace(InventoryItem item, int x, int y)
        {
            if (item == null || x < 0 || y < 0 || x + item.Width > Width || y + item.Height > Height) return false;
            for (int iy = y; iy < y + item.Height; iy++) for (int ix = x; ix < x + item.Width; ix++) if (cells[ix, iy] != null) return false;
            return true;
        }

        public bool TryAdd(InventoryItem item)
        {
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) if (CanPlace(item, x, y))
            {
                for (int iy = y; iy < y + item.Height; iy++) for (int ix = x; ix < x + item.Width; ix++) cells[ix, iy] = item;
                return true;
            }
            return false;
        }

        public bool CanAdd(InventoryItem item)
        {
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) if (CanPlace(item, x, y)) return true;
            return false;
        }

        public InventoryGrid Clone()
        {
            InventoryGrid clone = new InventoryGrid(Width, Height);
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) clone.cells[x, y] = cells[x, y];
            return clone;
        }
    }

    public sealed class LootContainer
    {
        public GridPosition Position { get; }
        public InventoryItem Item { get; }
        public bool IsLooted { get; private set; }
        public LootContainer(GridPosition position, InventoryItem item) { Position = position; Item = item; }
        public void MarkLooted() => IsLooted = true;
        public LootContainer Clone() { LootContainer clone = new LootContainer(Position, Item); if (IsLooted) clone.MarkLooted(); return clone; }
    }
}
