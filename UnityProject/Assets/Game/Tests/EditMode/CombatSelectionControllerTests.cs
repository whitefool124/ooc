using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatSelectionControllerTests
    {
        [Test]
        public void SelectActionAndReset_ClearTargetAndKeyboardCursor()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatSelectionController selection = new CombatSelectionController();
            selection.SetTarget(state, enemy.Id);
            selection.BeginKeyboardTargeting(state);

            selection.SelectAction("攻击");

            Assert.That(selection.Action, Is.EqualTo("攻击"));
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.IsKeyboardTargeting, Is.False);
            selection.SetKnownTarget(enemy.Id);
            selection.Reset();
            Assert.That(selection.Action, Is.EqualTo("移动"));
            Assert.That(selection.TargetId, Is.Null);
        }

        [Test]
        public void KeyboardTargeting_StartsAtSelectionMovesAndTracksEnemyOnly()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatSelectionController selection = new CombatSelectionController();
            selection.SetTarget(state, enemy.Id);

            Assert.That(selection.BeginKeyboardTargeting(state), Is.True);
            Assert.That(selection.KeyboardPosition, Is.EqualTo(enemy.Position));
            Assert.That(selection.MoveKeyboardTarget(state, -1, 0), Is.True);
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.MoveKeyboardTarget(state, 1, 0), Is.True);
            Assert.That(selection.TargetId, Is.EqualTo(enemy.Id));
        }

        [Test]
        public void CommitAndCancel_EndCursorWithExistingTargetSemantics()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatSelectionController selection = new CombatSelectionController();
            selection.SetTarget(state, enemy.Id);
            selection.BeginKeyboardTargeting(state);

            Assert.That(selection.TryCommitKeyboardTarget(out GridPosition committed), Is.True);
            Assert.That(committed, Is.EqualTo(enemy.Position));
            Assert.That(selection.TargetId, Is.EqualTo(enemy.Id), "commit retains the inspected target until command handling decides otherwise");

            selection.BeginKeyboardTargeting(state);
            Assert.That(selection.CancelKeyboardTargeting(), Is.True);
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.IsKeyboardTargeting, Is.False);
        }

        [Test]
        public void TargetValidationRejectsUnknownUnitsAndInactiveHeroCannotStartCursor()
        {
            CombatState state = State(out _, out _);
            CombatSelectionController selection = new CombatSelectionController();

            Assert.That(selection.SetTarget(state, "missing"), Is.False);
            Assert.That(selection.TargetId, Is.Null);
            Assert.That(selection.BeginKeyboardTargeting(state), Is.False);
        }

        private static CombatState State(out UnitState hero, out UnitState enemy)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            enemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
            return new CombatState(new GridMap(4, 2), new[] { hero, enemy });
        }
    }
}
