using NUnit.Framework;
using System.Linq;

namespace OCC.Combat.Tests
{
    public sealed class UiPresentationModelsTests
    {
        [Test]
        public void Versions_AdvanceOnlyMarkedArea()
        {
            var versions = new UiPresentationVersions();
            UiPresentationChange received = default;
            versions.Changed += change => received = change;

            versions.Mark(UiPresentationArea.MapResources);

            Assert.That(versions.Version(UiPresentationArea.MapResources), Is.EqualTo(1));
            Assert.That(versions.Version(UiPresentationArea.MapStructure), Is.EqualTo(0));
            Assert.That(received.Area, Is.EqualTo(UiPresentationArea.MapResources));
            Assert.That(received.Version, Is.EqualTo(1));
        }

        [Test]
        public void MapModel_IsSnapshotNotLiveRunReference()
        {
            var run = new RogueliteMapRun(123);
            RogueliteMapPresentationModel before = RogueliteMapPresentationModel.From(run);

            run.SelectNode("rail_patrol");
            RogueliteMapPresentationModel after = RogueliteMapPresentationModel.From(run);

            Assert.That(before.CurrentNodeId, Is.EqualTo("start"));
            Assert.That(after.CurrentNodeId, Is.EqualTo("rail_patrol"));
            Assert.That(before.Equals(after), Is.False);
        }

        [Test]
        public void MapVisualSemantics_DoNotDependOnColorAndUseStableNodeFocusKeys()
        {
            Assert.That(RogueliteMapVisualPresentation.FocusKey("rail_patrol"), Is.EqualTo("map.node.rail_patrol"));
            RogueliteMapNodeVisualState[] states = (RogueliteMapNodeVisualState[])System.Enum.GetValues(typeof(RogueliteMapNodeVisualState));
            Assert.That(states.Select(RogueliteMapVisualPresentation.StateLabel).Distinct().Count(), Is.EqualTo(states.Length));
            Assert.That(states.Select(RogueliteMapVisualPresentation.StateGlyph).Distinct().Count(), Is.EqualTo(states.Length));
        }

        [Test]
        public void MapRouteSemantics_DistinguishAvailableSafeLockedAndUnknownConnections()
        {
            Assert.That(RogueliteMapVisualPresentation.RouteState(RogueliteMapNodeVisualState.Current, RogueliteMapNodeVisualState.Available), Is.EqualTo(RogueliteMapRouteVisualState.Available));
            Assert.That(RogueliteMapVisualPresentation.RouteState(RogueliteMapNodeVisualState.Current, RogueliteMapNodeVisualState.Cleared), Is.EqualTo(RogueliteMapRouteVisualState.Safe));
            Assert.That(RogueliteMapVisualPresentation.RouteState(RogueliteMapNodeVisualState.Current, RogueliteMapNodeVisualState.Locked), Is.EqualTo(RogueliteMapRouteVisualState.Locked));
            Assert.That(RogueliteMapVisualPresentation.RouteState(RogueliteMapNodeVisualState.Known, RogueliteMapNodeVisualState.Unknown), Is.EqualTo(RogueliteMapRouteVisualState.Unknown));
        }

        [Test]
        public void MapDetail_ExposesRestrictionAndKnownConnectionsWithoutChangingTravelRules()
        {
            var run = new RogueliteMapRun(123);
            RogueliteMapNode available = RogueliteMapCatalog.Node("rail_patrol");
            RogueliteMapNode unknown = RogueliteMapCatalog.Node("core_finale");

            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, available), Is.EqualTo("路径可用"));
            Assert.That(RogueliteMapVisualPresentation.ConnectionSummary(run, available), Does.Contain("连接："));
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, unknown), Is.EqualTo("尚未侦测"));
            Assert.That(RogueliteMapVisualPresentation.ConnectionSummary(run, unknown), Is.EqualTo("连接信息尚未公开"));
        }

        [Test]
        public void SettlementModel_ChangesWhenRewardStateOpens()
        {
            var run = new RogueliteMapRun(321);
            run.SelectNode("rail_patrol");
            SettlementPresentationModel before = SettlementPresentationModel.From(run);

            run.CompleteCurrentCombat();
            SettlementPresentationModel after = SettlementPresentationModel.From(run);

            Assert.That(before.Visible, Is.False);
            Assert.That(after.Visible, Is.True);
            Assert.That(after.RewardKey, Is.Not.Empty);
        }

        [Test]
        public void EconomyChoice_ExplainsCurrencyFailureBeforeSubmission()
        {
            var run = new RogueliteMapRun(123);
            var shopChoice = new RogueliteNodeContentChoice("ui-cost-preview", "成本预览", "不执行", RogueliteNodeContentEffect.Supplies, partsCost: 9);

            UiOperationAvailability availability = RogueliteEconomyPresentation.ForNodeChoice(run, shopChoice);

            Assert.That(availability.CanExecute, Is.False);
            Assert.That(availability.Status, Is.EqualTo("零件不足"));
            Assert.That(availability.Reason, Does.Contain("当前 4"));
        }

        [Test]
        public void EconomyReward_ExplainsBackpackCapacityBeforeClaiming()
        {
            var run = new RogueliteMapRun(124);
            run.SelectNode("rail_patrol");
            run.CompleteCurrentCombat();
            RogueliteReward itemReward = run.CurrentRewards.Single(reward => reward.Kind == RogueliteRewardKind.Item);
            for (int index = 0; ; index++)
            {
                InventoryResult result = run.Inventory.AddFirstFit(new ItemInstance("fill-" + index, "medkit", 1000 + index));
                if (!result.Success) break;
            }

            UiOperationAvailability availability = RogueliteEconomyPresentation.ForReward(run, itemReward);

            Assert.That(availability.CanExecute, Is.False);
            Assert.That(availability.Status, Is.EqualTo("背包空间不足"));
            Assert.That(RogueliteEconomyPresentation.RewardComparison(run, itemReward), Is.EqualTo("背包：空间不足"));
        }

        [Test]
        public void EconomyWorkshop_BlocksIncompatibleWeaponAndExplainsRequiredRecovery()
        {
            var run = new RogueliteMapRun(8404, FireRogueliteStarterCatalog.Melee);
            run.SelectNode("supply_checkpoint");
            run.SelectNode("field_workshop");
            run.ChooseCurrentNodeContent("wand_calibration");
            RogueliteReward wand = RogueliteMapCatalog.Rewards.Single(reward => reward.Id == "arcane_wand");

            UiOperationAvailability availability = RogueliteEconomyPresentation.ForEquipment(run, wand);

            Assert.That(availability.CanExecute, Is.False);
            Assert.That(availability.Status, Is.EqualTo("术式不兼容"));
            Assert.That(availability.Reason, Does.Contain("调整"));
        }

        [Test]
        public void CombatHudModel_ChangesWhenQuickbarInstanceUsesChange()
        {
            CombatState state = new CombatState(new GridMap(4, 4), new[]
            {
                new UnitState("hero", true, new GridPosition(0, 0), Facing.East)
            });
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.AddFirstFit(new ItemInstance("artifact", "F-T01", 0, 2)).Success, Is.True);
            state.ConfigureItemInventory(inventory, new[] { "artifact" });
            CombatResolver.BeginTurn(state, "hero");
            CombatHudPresentationModel before = CombatHudPresentationModel.From(state, "技能1", null, false);

            Assert.That(state.ConsumeInventoryItem("artifact"), Is.True);
            CombatHudPresentationModel after = CombatHudPresentationModel.From(state, "技能1", null, false);

            Assert.That(before.Equals(after), Is.False);
        }

        [Test]
        public void CombatHudModel_IsStableBeforeTheFirstTurnBegins()
        {
            var state = new CombatState(new GridMap(4, 4), new[]
            {
                new UnitState("hero", true, new GridPosition(0, 0), Facing.East)
            });

            CombatHudPresentationModel model = default;
            Assert.DoesNotThrow(() => model = CombatHudPresentationModel.From(state, null, null, false));
            Assert.That(model.ActiveUnitId, Is.Empty);
            Assert.That(model.ActiveActionPoints, Is.EqualTo(-1));
            Assert.That(model.Health, Is.GreaterThan(0));
        }

        [Test]
        public void CombatTurnTrack_OrdersLivingUnitsAndMarksTheCurrentActor()
        {
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            var earlyEnemy = new UnitState("enemy_early", false, new GridPosition(1, 0), Facing.West);
            var lateEnemy = new UnitState("enemy_late", false, new GridPosition(2, 0), Facing.West);
            var state = new CombatState(new GridMap(4, 4), new[] { hero, earlyEnemy, lateEnemy });
            CombatResolver.BeginTurn(state, "hero");

            CombatTurnTrackEntry[] track = CombatTurnTrackPresentation.Build(state, 5).ToArray();

            Assert.That(track.Select(entry => entry.UnitId), Is.EqualTo(new[] { "hero", "enemy_early", "enemy_late" }));
            Assert.That(track.Select(entry => entry.Order), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(track.Single(entry => entry.IsActive).UnitId, Is.EqualTo("hero"));
            Assert.That(track.Single(entry => entry.UnitId == "hero").IsHero, Is.True);
            Assert.That(track.All(entry => entry.VitalityText.Contains("生命")), Is.True);
        }

        [Test]
        public void CombatTurnTrack_RespectsVisibleSlotLimit()
        {
            UnitState[] units = Enumerable.Range(0, 7)
                .Select(index => new UnitState("unit_" + index, index == 0, new GridPosition(index, 0), Facing.East))
                .ToArray();
            var state = new CombatState(new GridMap(8, 2), units);

            Assert.That(CombatTurnTrackPresentation.Build(state, 5).Count, Is.EqualTo(5));
            Assert.That(CombatTurnTrackPresentation.Build(state, 0), Is.Empty);
        }
    }
}
