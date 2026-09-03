using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class InventoryDragState
    {
        public string InstanceId { get; }
        public bool Rotated { get; private set; }
        public Vector2Int GrabOffset { get; private set; }

        public InventoryDragState(string instanceId, bool rotated, int grabX, int grabY)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("Dragged item identity is required.", nameof(instanceId));
            InstanceId = instanceId;
            Rotated = rotated;
            GrabOffset = new Vector2Int(Math.Max(0, grabX), Math.Max(0, grabY));
        }

        public void ToggleRotation(ItemDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            Vector2Int previous = GrabOffset;
            if (!Rotated)
                GrabOffset = new Vector2Int(definition.Height - 1 - previous.y, previous.x);
            else
                GrabOffset = new Vector2Int(previous.y, definition.Height - 1 - previous.x);
            Rotated = !Rotated;
        }

        public Vector2Int AnchorFor(Vector2Int pointerCell) => pointerCell - GrabOffset;

        public InventoryResult Preview(InventoryContainerState inventory, Vector2Int pointerCell)
        {
            if (inventory == null) return new InventoryResult(InventoryError.InvalidItem, InstanceId);
            ItemInstance item = inventory.Get(InstanceId);
            Vector2Int anchor = AnchorFor(pointerCell);
            return inventory.CanPlace(item, anchor.x, anchor.y, InstanceId, Rotated);
        }

        public InventoryResult Commit(InventoryContainerState inventory, Vector2Int pointerCell)
        {
            if (inventory == null) return new InventoryResult(InventoryError.InvalidItem, InstanceId);
            Vector2Int anchor = AnchorFor(pointerCell);
            return inventory.Move(InstanceId, anchor.x, anchor.y, Rotated);
        }
    }

    public static class InventoryInteractionPresentation
    {
        public const float CellSize = 52f;
        public static readonly Vector2Int OutsideGrid = new Vector2Int(-1, -1);

        public static Rect GridRect(Rect panelRect) => new Rect(panelRect.x + 26f, panelRect.y + 68f,
            InventoryContainerState.BaseWidth * CellSize, InventoryContainerState.BaseHeight * CellSize);

        public static Vector2Int GridCellAt(Rect panelRect, Vector2 pointer)
        {
            Rect grid = GridRect(panelRect);
            if (!grid.Contains(pointer)) return OutsideGrid;
            int x = Mathf.FloorToInt((pointer.x - grid.x) / CellSize);
            int y = Mathf.FloorToInt((pointer.y - grid.y) / CellSize);
            return x >= 0 && x < InventoryContainerState.BaseWidth && y >= 0 && y < InventoryContainerState.BaseHeight
                ? new Vector2Int(x, y)
                : OutsideGrid;
        }

        public static Rect PlacementRect(Rect panelRect, InventoryPlacement placement, ItemDefinition definition)
        {
            Rect grid = GridRect(panelRect);
            int width = placement.Rotated ? definition.Height : definition.Width;
            int height = placement.Rotated ? definition.Width : definition.Height;
            return new Rect(grid.x + placement.X * CellSize + 3f, grid.y + placement.Y * CellSize + 3f,
                width * CellSize - 9f, height * CellSize - 9f);
        }

        public static Rect TooltipRect(Vector2 pointer)
        {
            const float width = 520f;
            const float height = 320f;
            float x = Mathf.Clamp(pointer.x + 22f, 48f, 1872f - width);
            float y = Mathf.Clamp(pointer.y + 22f, 48f, 1032f - height);
            return new Rect(x, y, width, height);
        }

        public static string BuildHoverText(ItemInstance item, InventoryPlacement placement)
        {
            if (item == null) return string.Empty;
            ItemDefinition definition = ItemCatalog.Get(item.DefinitionId);
            ArtifactDefinition artifact = ArtifactCatalog.All.FirstOrDefault(candidate => candidate.Id == definition.Id);
            int width = placement.Rotated ? definition.Height : definition.Width;
            int height = placement.Rotated ? definition.Width : definition.Height;
            StringBuilder text = new StringBuilder();
            text.AppendLine(definition.DisplayName + " · " + CategoryName(definition.Category) + " · " + RarityName(definition.Rarity));
            text.Append("大小：").Append(width).Append('×').Append(height).Append(placement.Rotated ? "（横放）" : "（竖放）")
                .Append("  重量：").Append(definition.Weight);
            if (definition.MaximumUses > 0) text.Append("  还能用：").Append(item.RemainingUses).Append('/').Append(definition.MaximumUses);
            text.AppendLine();
            if (!string.IsNullOrEmpty(definition.Element)) text.AppendLine("元素：" + definition.Element);
            text.AppendLine("来源：" + (string.IsNullOrEmpty(definition.Provenance) ? "未知" : definition.Provenance));
            if (artifact == null)
                text.AppendLine(definition.Description);
            else
            {
                text.AppendLine("怎么用：" + artifact.PublicCost);
                text.AppendLine("能对谁用：" + artifact.TargetSummary);
                text.AppendLine("会怎样：" + artifact.EffectSummary);
                text.AppendLine("要小心：" + artifact.RiskSummary);
                text.AppendLine("适合：" + artifact.BuildUse);
            }
            text.Append("左键拖拽移动 · 拖拽中右键旋转 · 松开左键放置");
            return text.ToString();
        }

        public static string ErrorName(InventoryError error)
        {
            switch (error)
            {
                case InventoryError.OutOfBounds: return "这件东西放出行囊了";
                case InventoryError.Occupied: return "那里已经放了别的东西";
                case InventoryError.Overweight: return "超过负重限制";
                case InventoryError.MissingInstance: return "物品不存在";
                default: return error == InventoryError.None ? "可放置" : "当前位置不可放置";
            }
        }

        public static string NextSelection(InventoryContainerState inventory, string currentId, int directionX, int directionY)
        {
            if (inventory == null || inventory.Placements.Count == 0) return null;
            InventoryPlacement? current = string.IsNullOrEmpty(currentId) ? null : inventory.PlacementOf(currentId);
            if (!current.HasValue || directionX == 0 && directionY == 0)
                return inventory.Placements.OrderBy(item => item.Y).ThenBy(item => item.X).Select(item => item.InstanceId).FirstOrDefault();
            Vector2 origin = PlacementCenter(inventory, current.Value);
            return inventory.Placements.Where(item => item.InstanceId != currentId)
                .Select(item => new { item.InstanceId, Delta = PlacementCenter(inventory, item) - origin })
                .Where(item => item.Delta.x * directionX + item.Delta.y * directionY > 0f)
                .OrderBy(item => Mathf.Abs(item.Delta.x * directionY - item.Delta.y * directionX) * 2f + item.Delta.magnitude)
                .ThenBy(item => item.InstanceId, StringComparer.Ordinal)
                .Select(item => item.InstanceId).FirstOrDefault() ?? currentId;
        }

        public static UiOperationAvailability LootTakeAvailability(InventoryContainerState inventory, ItemInstance item)
        {
            if (inventory == null || item == null) return new UiOperationAvailability(false, "不可拿取", "战利品不可用");
            InventoryResult fit = inventory.FindFirstFit(item);
            return fit.Success
                ? new UiOperationAvailability(true, "可拿取", "将自动放入背包 " + fit.X + "," + fit.Y + (fit.Rotated ? "（旋转）" : string.Empty))
                : new UiOperationAvailability(false, "行囊放不下", ErrorName(fit.Error));
        }

        public static string LootSearchReason(bool adjacent, int actionPoints, bool complete)
        {
            if (complete) return "容器已清空";
            if (!adjacent) return "需要移动到容器相邻格";
            if (actionPoints < 1) return PlayerFacingCopy.ResourceShortage("行动点", 1, actionPoints);
            return "继续搜索会消耗 1 行动点";
        }

        private static Vector2 PlacementCenter(InventoryContainerState inventory, InventoryPlacement placement)
        {
            ItemDefinition definition = ItemCatalog.Get(inventory.Get(placement.InstanceId).DefinitionId);
            int width = placement.Rotated ? definition.Height : definition.Width;
            int height = placement.Rotated ? definition.Width : definition.Height;
            return new Vector2(placement.X + width * .5f, placement.Y + height * .5f);
        }

        private static string RarityName(ItemRarity rarity) => rarity == ItemRarity.Common ? "普通" : rarity == ItemRarity.Uncommon ? "少见" : rarity == ItemRarity.Rare ? "稀有" : "珍奇";
        private static string CategoryName(ItemCategory category) => category == ItemCategory.Consumable ? "消耗品" : category == ItemCategory.Weapon ? "武器" : category == ItemCategory.Armor ? "护具" : category == ItemCategory.Scroll ? "卷轴" : category == ItemCategory.Artifact ? "法宝" : category == ItemCategory.Material ? "材料" : category == ItemCategory.Quest ? "任务物品" : "容器";
    }
}
