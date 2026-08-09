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
            text.AppendLine(definition.DisplayName + "  //  " + CategoryName(definition.Category) + " · " + RarityName(definition.Rarity));
            text.Append("占格：").Append(width).Append('×').Append(height).Append(placement.Rotated ? "（已旋转）" : "（标准朝向）")
                .Append("  重量：").Append(definition.Weight);
            if (definition.MaximumUses > 0) text.Append("  次数：").Append(item.RemainingUses).Append('/').Append(definition.MaximumUses);
            text.AppendLine();
            if (!string.IsNullOrEmpty(definition.Element)) text.AppendLine("元素：" + definition.Element);
            text.AppendLine("来源：" + (string.IsNullOrEmpty(definition.Provenance) ? "未知" : definition.Provenance));
            if (artifact == null)
                text.AppendLine(definition.Description);
            else
            {
                text.AppendLine("代价：" + artifact.PublicCost);
                text.AppendLine("目标：" + artifact.TargetSummary);
                text.AppendLine("效果：" + artifact.EffectSummary);
                text.AppendLine("风险/反制：" + artifact.RiskSummary);
                text.AppendLine("构筑用途：" + artifact.BuildUse);
            }
            text.Append("左键拖拽移动 · 拖拽中右键旋转 · 松开左键放置");
            return text.ToString();
        }

        public static string ErrorName(InventoryError error)
        {
            switch (error)
            {
                case InventoryError.OutOfBounds: return "超出背包边界";
                case InventoryError.Occupied: return "目标位置已被占用";
                case InventoryError.Overweight: return "超过负重限制";
                case InventoryError.MissingInstance: return "物品不存在";
                default: return error == InventoryError.None ? "可放置" : "当前位置不可放置";
            }
        }

        private static string RarityName(ItemRarity rarity) => rarity == ItemRarity.Common ? "普通" : rarity == ItemRarity.Uncommon ? "少见" : rarity == ItemRarity.Rare ? "稀有" : "珍奇";
        private static string CategoryName(ItemCategory category) => category == ItemCategory.Consumable ? "消耗品" : category == ItemCategory.Weapon ? "武器" : category == ItemCategory.Armor ? "护具" : category == ItemCategory.Scroll ? "卷轴" : category == ItemCategory.Artifact ? "法宝" : category == ItemCategory.Material ? "材料" : category == ItemCategory.Quest ? "任务物品" : "容器";
    }
}
