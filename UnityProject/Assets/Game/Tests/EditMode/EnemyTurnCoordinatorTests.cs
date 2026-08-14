using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class EnemyTurnCoordinatorTests
    {
        [Test]
        public void Advance_OwnsCommandFromFocusThroughResolutionAndActorGap()
        {
            UnitState enemy = Enemy("enemy-a", 2);
            CombatCommand command = CombatCommand.Move(enemy.Id, new GridPosition(1, 0), Facing.East);
            EnemyTurnCoordinator coordinator = new EnemyTurnCoordinator();

            EnemyTurnAdvance begin = coordinator.Advance(enemy, 10f, _ => command);
            Assert.That(begin.Kind, Is.EqualTo(EnemyTurnAdvanceKind.BeginAction));
            Assert.That(begin.CommandType, Is.EqualTo(CombatCommandType.Move));
            Assert.That(coordinator.Phase, Is.EqualTo(EnemyTurnSequencePhase.Focus));

            EnemyTurnAdvance resolve = coordinator.Advance(enemy, 10f + EnemyTurnSequence.FocusSeconds, _ => command);
            Assert.That(resolve.Kind, Is.EqualTo(EnemyTurnAdvanceKind.ResolveCommand));
            Assert.That(resolve.Command, Is.EqualTo(command));

            float resultAt = 10f + EnemyTurnSequence.FocusSeconds + EnemyTurnSequence.ResultHoldFor(command.Type);
            EnemyTurnAdvance end = coordinator.Advance(enemy, resultAt, _ => command);
            Assert.That(end.Kind, Is.EqualTo(EnemyTurnAdvanceKind.EndAction));
            Assert.That(coordinator.Phase, Is.EqualTo(EnemyTurnSequencePhase.ActorGap));

            EnemyTurnAdvance ready = coordinator.Advance(enemy, resultAt + EnemyTurnSequence.ActorGapSeconds, _ => command);
            Assert.That(ready.Kind, Is.EqualTo(EnemyTurnAdvanceKind.ReadyForNext));
            Assert.That(coordinator.IsRunning, Is.False);
        }

        [Test]
        public void Advance_UsesEndTurnPresentationWithoutChoosingACommandWhenActorHasNoActions()
        {
            UnitState enemy = Enemy("enemy-a", 0);
            EnemyTurnCoordinator coordinator = new EnemyTurnCoordinator();
            bool factoryCalled = false;

            EnemyTurnAdvance begin = coordinator.Advance(enemy, 0f, _ =>
            {
                factoryCalled = true;
                return CombatCommand.EndTurn(enemy.Id);
            });

            Assert.That(factoryCalled, Is.False);
            Assert.That(begin.Kind, Is.EqualTo(EnemyTurnAdvanceKind.BeginAction));
            Assert.That(begin.CommandType, Is.EqualTo(CombatCommandType.EndTurn));
        }

        [Test]
        public void Advance_CancelsStaleSequenceWhenTheActiveActorChanges()
        {
            EnemyTurnCoordinator coordinator = new EnemyTurnCoordinator();
            UnitState first = Enemy("enemy-a", 2);
            UnitState second = Enemy("enemy-b", 2);
            coordinator.Advance(first, 0f, unit => CombatCommand.EndTurn(unit.Id));

            EnemyTurnAdvance result = coordinator.Advance(second, .1f,
                unit => CombatCommand.EndTurn(unit.Id));

            Assert.That(result.Kind, Is.EqualTo(EnemyTurnAdvanceKind.ActorChanged));
            Assert.That(result.UnitId, Is.EqualTo(first.Id));
            Assert.That(coordinator.IsRunning, Is.False);
        }

        [Test]
        public void Reset_DropsPendingCommandAndPresentationClock()
        {
            EnemyTurnCoordinator coordinator = new EnemyTurnCoordinator();
            UnitState enemy = Enemy("enemy-a", 2);
            coordinator.Advance(enemy, 0f, unit => CombatCommand.EndTurn(unit.Id));

            coordinator.Reset();

            Assert.That(coordinator.Phase, Is.EqualTo(EnemyTurnSequencePhase.Idle));
            Assert.That(coordinator.UnitId, Is.Null);
            Assert.That(coordinator.IsRunning, Is.False);
        }

        private static UnitState Enemy(string id, int actionPoints)
        {
            UnitState enemy = new UnitState(id, false, new GridPosition(0, 0), Facing.East);
            if (actionPoints > 0)
            {
                CombatState state = new CombatState(new GridMap(2, 1), new[] { enemy });
                CombatResolver.BeginTurn(state, enemy.Id);
            }
            return enemy;
        }
    }
}
