using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class InventoryInteractionPresentationTests
    {
        [Test]
        public void HoverText_ContainsInstanceDefinitionPlacementAndInteractionHints()
        {
            ItemInstance item = new ItemInstance("artifact", "F-T01", 0, 1);
            InventoryPlacement placement = new InventoryPlacement(item.InstanceId, 2, 3, true);

            string text = InventoryInteractionPresentation.BuildHoverText(item, placement);

            Assert.That(text, Does.Contain(ItemCatalog.Get(item.DefinitionId).DisplayName));
            Assert.That(text, Does.Contain("重量"));
            Assert.That(text, Does.Contain("1/2"));
            Assert.That(text, Does.Contain("横放"));
            Assert.That(text, Does.Contain("来源："));
            Assert.That(text, Does.Contain("会怎样："));
            Assert.That(text, Does.Contain("左键拖拽"));
            Assert.That(text, Does.Contain("拖拽中右键旋转"));
        }

        [Test]
        public void GridCellAt_MapsOnlyTheSixByTenInteractiveGrid()
        {
            Rect panel = new Rect(100f, 160f, 600f, 720f);
            Rect grid = InventoryInteractionPresentation.GridRect(panel);

            Assert.That(InventoryInteractionPresentation.GridCellAt(panel, new Vector2(grid.x + 1f, grid.y + 1f)), Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(InventoryInteractionPresentation.GridCellAt(panel, new Vector2(grid.x + 5f * 52f + 2f, grid.y + 9f * 52f + 2f)), Is.EqualTo(new Vector2Int(5, 9)));
            Assert.That(InventoryInteractionPresentation.GridCellAt(panel, new Vector2(grid.xMax + 1f, grid.yMax + 1f)), Is.EqualTo(new Vector2Int(-1, -1)));
        }

        [Test]
        public void DragRotation_PreservesGrabbedCellAndRoundTripsOffset()
        {
            ItemDefinition definition = ItemCatalog.Get("F-T01");
            InventoryDragState drag = new InventoryDragState("artifact", false, 1, 0);

            drag.ToggleRotation(definition);
            Assert.That(drag.Rotated, Is.True);
            Assert.That(drag.GrabOffset, Is.EqualTo(new Vector2Int(definition.Height - 1, 1)));

            drag.ToggleRotation(definition);
            Assert.That(drag.Rotated, Is.False);
            Assert.That(drag.GrabOffset, Is.EqualTo(new Vector2Int(1, 0)));
        }

        [Test]
        public void DropPreviewAndCommit_UseSamePlacementRuleAndKeepInvalidDropAtomic()
        {
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.Place(new ItemInstance("drag", "F-T01", 0), 0, 0).Success, Is.True);
            Assert.That(inventory.Place(new ItemInstance("block", "medkit", 1), 4, 4).Success, Is.True);
            InventoryDragState drag = new InventoryDragState("drag", false, 0, 0);

            InventoryResult valid = drag.Preview(inventory, new Vector2Int(2, 2));
            Assert.That(valid.Success, Is.True);
            Assert.That(drag.Commit(inventory, new Vector2Int(2, 2)).Success, Is.True);
            Assert.That(inventory.PlacementOf("drag").Value.X, Is.EqualTo(2));

            InventoryPlacement before = inventory.PlacementOf("drag").Value;
            Assert.That(drag.Preview(inventory, new Vector2Int(4, 4)).Error, Is.EqualTo(InventoryError.Occupied));
            Assert.That(drag.Commit(inventory, new Vector2Int(4, 4)).Success, Is.False);
            InventoryPlacement after = inventory.PlacementOf("drag").Value;
            Assert.That(after.X, Is.EqualTo(before.X));
            Assert.That(after.Y, Is.EqualTo(before.Y));
            Assert.That(after.Rotated, Is.EqualTo(before.Rotated));
        }

        [Test]
        public void DragRotation_CommitsOrientationAndSurvivesInventoryRoundTrip()
        {
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.Place(new ItemInstance("drag", "F-T01", 0), 0, 0).Success, Is.True);
            InventoryDragState drag = new InventoryDragState("drag", false, 0, 0);
            drag.ToggleRotation(ItemCatalog.Get("F-T01"));

            Assert.That(drag.Commit(inventory, new Vector2Int(3, 3)).Success, Is.True);
            Assert.That(inventory.PlacementOf("drag").Value.Rotated, Is.True);

            InventoryContainerState restored = InventoryContainerState.FromDataString(inventory.ToDataString());
            InventoryPlacement placement = restored.PlacementOf("drag").Value;
            Assert.That(placement.X, Is.EqualTo(3));
            Assert.That(placement.Y, Is.EqualTo(3));
            Assert.That(placement.Rotated, Is.True);
        }

        [Test]
        public void TooltipRect_RemainsInsideReferenceCanvas()
        {
            Rect tooltip = InventoryInteractionPresentation.TooltipRect(new Vector2(1900f, 1060f));

            Assert.That(tooltip.xMin, Is.GreaterThanOrEqualTo(48f));
            Assert.That(tooltip.yMin, Is.GreaterThanOrEqualTo(48f));
            Assert.That(tooltip.xMax, Is.LessThanOrEqualTo(1872f));
            Assert.That(tooltip.yMax, Is.LessThanOrEqualTo(1032f));
        }

        [Test]
        public void KeyboardSelection_UsesSpatialDirectionAndKeepsCurrentAtDeadEnd()
        {
            var inventory = new InventoryContainerState();
            Assert.That(inventory.Place(new ItemInstance("left", "medkit", 0), 0, 0).Success, Is.True);
            Assert.That(inventory.Place(new ItemInstance("right", "shield_cell", 1), 4, 0).Success, Is.True);

            Assert.That(InventoryInteractionPresentation.NextSelection(inventory, null, 0, 0), Is.EqualTo("left"));
            Assert.That(InventoryInteractionPresentation.NextSelection(inventory, "left", 1, 0), Is.EqualTo("right"));
            Assert.That(InventoryInteractionPresentation.NextSelection(inventory, "left", -1, 0), Is.EqualTo("left"));
        }

        [Test]
        public void LootTakePreview_UsesTheSameFirstFitRuleAsCommit()
        {
            var inventory = new InventoryContainerState();
            var loot = new ItemInstance("loot", "medkit", 0);
            UiOperationAvailability availability = InventoryInteractionPresentation.LootTakeAvailability(inventory, loot);

            Assert.That(availability.CanExecute, Is.True);
            Assert.That(availability.Reason, Does.Contain("0,0"));
            Assert.That(inventory.AddFirstFit(loot).Success, Is.True);
        }

        [TestCase(false, 2, false, "需要移动到容器相邻格")]
        [TestCase(true, 0, false, "行动点不足：需要 1，当前 0")]
        [TestCase(true, 2, true, "容器已清空")]
        public void LootSearchReason_ExplainsEveryBlockedState(bool adjacent, int actionPoints, bool complete, string expected)
        {
            Assert.That(InventoryInteractionPresentation.LootSearchReason(adjacent, actionPoints, complete), Is.EqualTo(expected));
        }
    }
}
