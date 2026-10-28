using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatSessionLifecycleControllerTests
    {
        [Test]
        public void ReturnToMapRun_DoesNotBypassAnUnsavedMapTransaction()
        {
            GameObject host = new GameObject("unsaved-map-return-test");
            try
            {
                CombatPrototypeBootstrap bootstrap = host.AddComponent<CombatPrototypeBootstrap>();
                RogueliteFlowCoordinator flow = (RogueliteFlowCoordinator)typeof(CombatPrototypeBootstrap)
                    .GetField("rogueliteFlow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
                RogueliteMapSaveCoordinator saves = (RogueliteMapSaveCoordinator)typeof(CombatPrototypeBootstrap)
                    .GetField("mapSaves", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
                flow.BeginMapRun(RogueliteMapRun.CreateFirstRunV1(7647));
                flow.SetMapMenuOpen(false);
                Assert.That(saves.Save(null), Is.False);

                bootstrap.ReturnToMapRun();

                Assert.That(flow.IsMapMenuOpen, Is.False);
                Assert.That(flow.MapRun, Is.Not.Null);
                Assert.That(bootstrap.IsMapRunSaved, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void Begin_ActivatesHeroTurnAndResetsSessionScopedCoordinators()
        {
            CombatFlowController flow = Flow(out CombatState state);
            flow.OpenBriefing();
            EnemyTurnCoordinator enemyTurn = new EnemyTurnCoordinator();
            CombatOutcomeSettlementCoordinator outcome = new CombatOutcomeSettlementCoordinator();
            outcome.Process(CombatFlowPhase.Defeat, Defeat(), null, null);

            CombatSessionActivation activation = new CombatSessionLifecycleController().Begin(flow, enemyTurn, outcome);

            Assert.That(flow.Phase, Is.EqualTo(CombatFlowPhase.Active));
            Assert.That(activation.State, Is.SameAs(state));
            Assert.That(activation.FireBattle.Combat, Is.SameAs(state));
            Assert.That(state.ActiveUnitId, Is.EqualTo("hero"));
            Assert.That(state.GetUnit("hero").ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn));
            Assert.That(outcome.IsHandled, Is.False);
            Assert.That(enemyTurn.IsRunning, Is.False);
        }

        [Test]
        public void Restart_RestoresSnapshotAndReturnsDirectlyToActivePlay()
        {
            CombatFlowController flow = Flow(out CombatState state);
            flow.OpenBriefing();
            CombatSessionLifecycleController controller = new CombatSessionLifecycleController();
            EnemyTurnCoordinator enemyTurn = new EnemyTurnCoordinator();
            CombatOutcomeSettlementCoordinator outcome = new CombatOutcomeSettlementCoordinator();
            controller.Begin(flow, enemyTurn, outcome);
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(1, 0)));

            CombatSessionActivation restarted = controller.Restart(flow, enemyTurn, outcome);

            Assert.That(flow.Phase, Is.EqualTo(CombatFlowPhase.Active));
            Assert.That(restarted.State, Is.Not.SameAs(state));
            Assert.That(restarted.State.GetUnit("hero").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(restarted.State.GetUnit("hero").ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn));
        }

        [Test]
        public void Restart_RestoresRogueliteSpellsAndEquipmentBoundToTheNewCombat()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(1973);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(
                run, null, Array.Empty<CombatSceneMarker>());
            CombatFlowController flow = new CombatFlowController();
            flow.Configure(build.Preparation, build.State);
            flow.OpenBriefing();
            CombatSessionLifecycleController controller = new CombatSessionLifecycleController();
            EnemyTurnCoordinator enemyTurn = new EnemyTurnCoordinator();
            CombatOutcomeSettlementCoordinator outcome = new CombatOutcomeSettlementCoordinator();
            controller.Begin(flow, enemyTurn, outcome);

            CombatSessionActivation restarted = controller.Restart(flow, enemyTurn, outcome);

            Assert.That(restarted.State.RogueSpells, Is.Not.Null);
            Assert.That(restarted.State.RogueSpells.Combat, Is.SameAs(restarted.State));
            Assert.That(restarted.State.RogueSpells.FireBattle.Combat, Is.SameAs(restarted.State));
            Assert.That(restarted.State.RogueSpells.Loadout.EquippedSpellIds,
                Is.EqualTo(build.State.RogueSpells.Loadout.EquippedSpellIds));
            Assert.That(restarted.State.RogueEquipment, Is.Not.Null);
            Assert.That(restarted.State.RogueEquipment, Is.Not.SameAs(build.State.RogueEquipment));
            Assert.That(restarted.State.RogueEquipment.Equipped,
                Is.EquivalentTo(build.State.RogueEquipment.Equipped));
        }

        [TestCase(false, CombatFlowPhase.Active, false, false)]
        [TestCase(false, CombatFlowPhase.Victory, false, false)]
        [TestCase(false, CombatFlowPhase.Defeat, false, true)]
        [TestCase(false, CombatFlowPhase.Defeat, true, false)]
        [TestCase(true, CombatFlowPhase.Active, false, true)]
        [TestCase(true, CombatFlowPhase.Defeat, true, false)]
        public void RetryEligibility_OnlyAddsUnabandonedRegularDefeats(bool tutorial,
            CombatFlowPhase phase, bool abandoned, bool expected)
        {
            GameObject host = new GameObject("retry-eligibility-test");
            try
            {
                var bootstrap = host.AddComponent<CombatPrototypeBootstrap>();
                var run = tutorial ? RogueliteMapRun.CreateFirstRunV1(303) : RegularCombat(RogueliteMapNodeType.Combat);
                if (tutorial) { run.AcknowledgeFirstRunOrigin(); run.SelectNode("B1"); }
                run.BeginCombatJournal();
                var mapFlow = (RogueliteFlowCoordinator)typeof(CombatPrototypeBootstrap)
                    .GetField("rogueliteFlow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
                mapFlow.BeginMapRun(run);
                CombatFlowController flow = Flow(out CombatState state);
                flow.OpenBriefing(); flow.BeginCombat();
                if (phase != CombatFlowPhase.Active) { state.ResolveDebugOutcome(phase == CombatFlowPhase.Victory); flow.RefreshOutcome(); }
                SetPrivate(bootstrap, "developerFlow", flow); SetPrivate(bootstrap, "state", state);
                SetPrivate(bootstrap, "pendingMapRunAbandon", abandoned);

                Assert.That(bootstrap.CanRetryCurrentCombat, Is.EqualTo(expected));
                if (!tutorial && phase == CombatFlowPhase.Defeat && !abandoned)
                {
                    bootstrap.ReturnFromCombatOutcome(); // Without confirmation, the callback must not execute.
                    Assert.That(run.IsComplete, Is.False, "战败页结束本轮必须先确认。");
                    Assert.That(run.HasActiveCombat, Is.True);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void AbandonConfirmation_PreservesOutcomeReachedWhileDialogWasOpen(bool victory, bool settled)
        {
            GameObject host = new GameObject("abandon-confirmation-outcome-test");
            try
            {
                var bootstrap = host.AddComponent<CombatPrototypeBootstrap>();
                var run = RegularCombat(RogueliteMapNodeType.Combat);
                run.BeginCombatJournal();
                var mapFlow = (RogueliteFlowCoordinator)typeof(CombatPrototypeBootstrap)
                    .GetField("rogueliteFlow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
                mapFlow.BeginMapRun(run);
                CombatFlowController flow = Flow(out CombatState state);
                flow.OpenBriefing(); flow.BeginCombat();
                SetPrivate(bootstrap, "developerFlow", flow); SetPrivate(bootstrap, "state", state);

                // Simulate an already-started action resolving after the confirmation opened.
                state.ResolveDebugOutcome(victory);
                if (settled)
                {
                    flow.RefreshOutcome();
                    new CombatOutcomeSettlementCoordinator().Process(flow.Phase, state, run, null);
                }
                string before = Rogue11Serializer.Serialize(ExportForTest(run));

                typeof(CombatPrototypeBootstrap).GetMethod("ConfirmMapRunAbandon", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(bootstrap, null);

                Assert.That(bootstrap.IsMapRunAbandonPending, Is.False);
                Assert.That(Rogue11Serializer.Serialize(ExportForTest(run)), Is.EqualTo(before));
                Assert.That(run.RunEndReason, Is.Not.EqualTo("abandon"));
                if (victory && settled) Assert.That(run.AwaitingReward, Is.True);
                if (!victory && settled) Assert.That(bootstrap.CanRetryCurrentCombat, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [TestCase(RogueliteMapNodeType.Combat)]
        [TestCase(RogueliteMapNodeType.Elite)]
        [TestCase(RogueliteMapNodeType.Event)]
        [TestCase(RogueliteMapNodeType.Finale)]
        public void RegularRetry_PreservesEncounterResourcesAndDoesNotCompleteOrReward(RogueliteMapNodeType type)
        {
            var run = RegularCombat(type);
            run.BeginCombatJournal();
            string before = Rogue11Serializer.Serialize(ExportForTest(run));
            run.AppendCombatJournal(CombatJournalEntry.Accepted(CombatCommand.EndTurn("hero")));
            run = RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(ExportForTest(run))));

            run.RestartCombatJournalPreservingBalance();

            Assert.That(Rogue11Serializer.Serialize(ExportForTest(run)), Is.EqualTo(before));
            Assert.That(run.HasActiveCombat, Is.True);
            Assert.That(run.IsComplete, Is.False);
            Assert.That(run.AwaitingReward, Is.False);
            Assert.That(run.CompletedNodes, Does.Not.Contain(run.CurrentNodeId));
            run.CloseAsFailure(true);
            Assert.Throws<InvalidOperationException>(() => run.RestartCombatJournalPreservingBalance());
        }

        [Test]
        public void RegularRetry_RestoresHealthBackpackQuickbarAndChargesWithoutRetainingBattleLoot()
        {
            var run = RegularCombat(RogueliteMapNodeType.Combat);
            var equipment = RogueEquipmentRuntime.FromDto(run.RogueRunState);
            var tool = equipment.CreateTacticalItem("retry-tool", "G-T01", 0, "test-entry");
            Assert.That(equipment.AddTacticalToBackpack(tool), Is.True);
            Assert.That(equipment.AssignQuickbar(0, tool.InstanceId), Is.True);
            equipment.WriteToDto(run.RogueRunState);
            run.BeginCombatJournal();
            string roundBefore = Rogue11Serializer.Serialize(ExportForTest(run));
            var build = new CombatSceneSessionBuilder().Build(run, null, Array.Empty<CombatSceneMarker>());
            var flow = new CombatFlowController(); flow.Configure(build.Preparation, build.State); flow.OpenBriefing();
            var controller = new CombatSessionLifecycleController(); var enemies = new EnemyTurnCoordinator();
            var settlement = new CombatOutcomeSettlementCoordinator(); controller.Begin(flow, enemies, settlement);
            int health = build.State.GetUnit("hero").Health;
            int charges = build.State.RogueEquipment.TacticalItem(tool.InstanceId).ChargesCurrent;
            var placement = build.State.RogueEquipment.Backpack[tool.InstanceId];
            Assert.That(build.State.RogueEquipment.TacticalItem(tool.InstanceId).Consume(), Is.True);
            Assert.That(build.State.RogueEquipment.DiscardBackpackItem(tool.InstanceId), Is.True);
            var loot = build.State.RogueEquipment.CreateTacticalItem("retry-battle-loot", "G-T06", 1, "battlefield-container");
            Assert.That(build.State.RogueEquipment.AddTacticalToBackpack(loot), Is.True);
            build.State.ResolveDebugOutcome(false); flow.RefreshOutcome();
            settlement.Process(flow.Phase, build.State, run, null);

            run.RestartCombatJournalPreservingBalance();
            CombatState restored = controller.Restart(flow, enemies, settlement).State;

            Assert.That(restored.GetUnit("hero").Health, Is.EqualTo(health));
            Assert.That(restored.IsDefeat, Is.False);
            Assert.That(restored.RogueEquipment.TacticalItem(tool.InstanceId).ChargesCurrent, Is.EqualTo(charges));
            Assert.That(restored.RogueEquipment.ItemQuickbarInstanceIds[0], Is.EqualTo(tool.InstanceId));
            Assert.That(restored.RogueEquipment.Backpack[tool.InstanceId].X, Is.EqualTo(placement.X));
            Assert.That(restored.RogueEquipment.Backpack[tool.InstanceId].Y, Is.EqualTo(placement.Y));
            Assert.That(restored.RogueEquipment.Backpack[tool.InstanceId].Rotated, Is.EqualTo(placement.Rotated));
            Assert.That(restored.RogueEquipment.TacticalItem(loot.InstanceId), Is.Null);
            Assert.That(Rogue11Serializer.Serialize(ExportForTest(run)), Is.EqualTo(roundBefore));
            Assert.That(settlement.IsHandled, Is.False);
        }

        private static RogueliteMapRun RegularCombat(RogueliteMapNodeType type)
        {
            var source = RogueliteMapRun.CreateSubsequentAcademyRun(303, includeOpeningGift: false);
            var node = source.MapNodes.First(value => value.Type == type);
            var dto = ExportForTest(source); dto.DeparturePending = false; dto.CurrentNodeId = node.Id;
            if (!dto.VisitedNodeIds.Contains(node.Id)) dto.VisitedNodeIds.Add(node.Id);
            if (type == RogueliteMapNodeType.Event)
            {
                dto.NodeContentAssignments.RemoveAll(value => value.StartsWith(node.Id + "=", StringComparison.Ordinal));
                dto.NodeContentAssignments.Add(node.Id + "=EV01");
            }
            var run = RogueliteMapRun.FromRogue11(dto);
            if (type == RogueliteMapNodeType.Event) run.ChooseCurrentNodeContent(run.CurrentContentChoices.First(value => value.RequiresCombat).Id);
            return run;
        }

        private static RogueRunDto ExportForTest(RogueliteMapRun run) =>
            (RogueRunDto)typeof(RogueliteMapRun).GetMethod("ExportRogue11", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(run, new object[] { null, string.Empty });

        private static void SetPrivate(CombatPrototypeBootstrap host, string name, object value) =>
            typeof(CombatPrototypeBootstrap).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, value);

        [Test]
        public void ObserveActiveUnit_OnlySignalsActualBoundaries()
        {
            CombatSessionLifecycleController controller = new CombatSessionLifecycleController();

            CombatUnitLifecycleAdvance first = controller.ObserveActiveUnit("hero");
            CombatUnitLifecycleAdvance duplicate = controller.ObserveActiveUnit("hero");
            CombatUnitLifecycleAdvance enemy = controller.ObserveActiveUnit("enemy");

            Assert.That(first.Changed, Is.True);
            Assert.That(first.UnitId, Is.EqualTo("hero"));
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(enemy.Changed, Is.True);
            Assert.That(enemy.UnitId, Is.EqualTo("enemy"));
        }

        private static CombatFlowController Flow(out CombatState state)
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            state = new CombatState(new GridMap(4, 2), new[] { hero, enemy },
                new CombatObjective[] { new EliminationObjective() });
            CombatFlowController flow = new CombatFlowController();
            flow.Configure(new MissionPreparation().Configure("test", "test", "test"), state);
            return flow;
        }

        private static CombatState Defeat()
        {
            CombatFlowController flow = Flow(out CombatState state);
            state.ResolveDebugOutcome(false);
            return state;
        }
    }
}
