using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatSessionLifecycleControllerTests
    {
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
            CombatResolver.Resolve(state, CombatCommand.Move("hero", new GridPosition(1, 0), Facing.East));

            CombatSessionActivation restarted = controller.Restart(flow, enemyTurn, outcome);

            Assert.That(flow.Phase, Is.EqualTo(CombatFlowPhase.Active));
            Assert.That(restarted.State, Is.Not.SameAs(state));
            Assert.That(restarted.State.GetUnit("hero").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(restarted.State.GetUnit("hero").ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn));
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
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
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
