using NUnit.Framework;
using System.Collections.Generic;
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

            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, available), Is.EqualTo("可以直接前往"));
            Assert.That(RogueliteMapVisualPresentation.ConnectionSummary(run, available), Does.Contain("从这里还能去："));
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, unknown), Is.EqualTo("还看不清这里"));
            Assert.That(RogueliteMapVisualPresentation.ConnectionSummary(run, unknown), Is.EqualTo("附近的路还看不清"));
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
            Assert.That(availability.Status, Is.EqualTo("行囊放不下"));
            Assert.That(RogueliteEconomyPresentation.RewardComparison(run, itemReward), Is.EqualTo("行囊已经装不下了"));
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
                new UnitState("hero", true, new GridPosition(0, 0))
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
                new UnitState("hero", true, new GridPosition(0, 0))
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
            var hero = new UnitState("hero", true, new GridPosition(0, 0));
            var earlyEnemy = new UnitState("enemy_early", false, new GridPosition(1, 0));
            var lateEnemy = new UnitState("enemy_late", false, new GridPosition(2, 0));
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
                .Select(index => new UnitState("unit_" + index, index == 0, new GridPosition(index, 0)))
                .ToArray();
            var state = new CombatState(new GridMap(8, 2), units);

            Assert.That(CombatTurnTrackPresentation.Build(state, 5).Count, Is.EqualTo(5));
            Assert.That(CombatTurnTrackPresentation.Build(state, 0), Is.Empty);
        }

        [Test]
        public void ActionTimeline_UsesZeroTo199GaugeAndEffectiveSpeed()
        {
            var hero = new UnitState("hero", true, new GridPosition(0, 0)) { Speed = 12 };
            var enemy = new UnitState("enemy", false, new GridPosition(1, 0)) { Speed = 8 };
            var state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });

            CombatResolver.AdvanceToNextTurn(state);
            Assert.That(state.ActiveUnitId, Is.EqualTo("hero"));
            Assert.That(hero.ActionValue, Is.EqualTo(108));
            Assert.That(enemy.ActionValue, Is.EqualTo(72));

            CombatResolver.EndTurn(state, hero);
            Assert.That(state.ActiveUnitId, Is.EqualTo("enemy"));
            Assert.That(hero.ActionValue, Is.EqualTo(56));
            Assert.That(enemy.ActionValue, Is.EqualTo(104));
            Assert.That(state.Units.Values.All(unit => unit.ActionValue >= 0 && unit.ActionValue <= 199), Is.True);
        }

        [Test]
        public void ActionTimeline_TiesUseSpeedThenBattleStartOrder()
        {
            var first = new UnitState("z_first", true, new GridPosition(0, 0)) { Speed = 10 };
            var second = new UnitState("a_second", false, new GridPosition(1, 0)) { Speed = 10 };
            var state = new CombatState(new GridMap(3, 2), new[] { first, second });

            CombatResolver.AdvanceToNextTurn(state);

            Assert.That(state.ActiveUnitId, Is.EqualTo("z_first"));
            Assert.That(CombatTurnTrackPresentation.Build(state, 2).Select(entry => entry.UnitId),
                Is.EqualTo(new[] { "z_first", "a_second" }));

            CombatResolver.EndTurn(state, first);

            Assert.That(state.ActiveUnitId, Is.EqualTo("a_second"),
                "An equal-speed first actor must not win the same fixed-order tie forever.");
        }

        [Test]
        public void ActionTimeline_DelayPreviewClampsAndDoesNotRevokeCurrentAction()
        {
            var hero = new UnitState("hero", true, new GridPosition(0, 0));
            var state = new CombatState(new GridMap(2, 2), new[] { hero });
            CombatResolver.BeginTurn(state, hero.Id);

            Assert.That(CombatActionTimeline.PreviewDelayedValue(hero, 24), Is.EqualTo(76));
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DelayInitiative(24));

            Assert.That(hero.ActionValue, Is.EqualTo(76));
            Assert.That(state.ActiveUnitId, Is.EqualTo(hero.Id));
            Assert.That(CombatActionTimeline.PreviewDelayedValue(hero, 500), Is.Zero);
        }

        [Test]
        public void ActionTimeline_DelayPreviewReportsEveryChangedPostTurnRank()
        {
            var hero = new UnitState("hero", true, new GridPosition(0, 0)) { Speed = 12 };
            var fast = new UnitState("fast", false, new GridPosition(1, 0)) { Speed = 11 };
            var slow = new UnitState("slow", false, new GridPosition(2, 0)) { Speed = 9 };
            var state = new CombatState(new GridMap(4, 2), new[] { hero, fast, slow });
            CombatResolver.AdvanceToNextTurn(state);

            IReadOnlyDictionary<string, int> changes = CombatTurnTrackPresentation.PreviewOrderChanges(state, fast.Id, 30);

            Assert.That(changes[slow.Id], Is.EqualTo(1));
            Assert.That(changes[fast.Id], Is.EqualTo(-1));
            Assert.That(changes.ContainsKey(hero.Id), Is.False);
        }

        [Test]
        public void CombatState_TurnSequenceIncrementsAndSurvivesPreviewClone()
        {
            var state = new CombatState(new GridMap(2, 2), new[] { new UnitState("hero", true, new GridPosition(0, 0)) });
            CombatResolver.BeginTurn(state, "hero");
            Assert.That(state.TurnSequence, Is.EqualTo(1));
            Assert.That(state.Clone().TurnSequence, Is.EqualTo(1));
        }
    }
}
