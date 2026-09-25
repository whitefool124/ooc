using System;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
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
