using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatCommandExecutionServiceTests
    {
        [Test]
        public void Execute_MovesHeroAndReturnsPresentationContext()
        {
            CombatState state = State(out UnitState hero, out _);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatCommandExecutionResult result = new CombatCommandExecutionService().Execute(state, null,
                CombatCommand.Move(hero.Id, new GridPosition(1, 0), Facing.East));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Execution, Is.Not.Null);
            Assert.That(result.HeroMoved, Is.True);
            Assert.That(result.DeliverySource, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(result.MovementFireExecutions, Is.Empty);
        }

        [Test]
        public void Execute_AttackUsesFireWeaponResolutionAndRetainsBattle()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            FireBattleState fireBattle = new FireBattleState(state);
            CombatCommandExecutionResult result = new CombatCommandExecutionService().Execute(state, fireBattle,
                CombatCommand.Attack(hero.Id, enemy.Id));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.FireBattle, Is.SameAs(fireBattle));
            Assert.That(result.Execution, Is.Not.Null);
            Assert.That(result.AttackFireExecutions, Is.Not.Null);
        }

        [Test]
        public void Execute_SkillReportsDeliverySourceTargetAndDefinition()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatResolver.BeginTurn(state, hero.Id);
            CombatCommandExecutionResult result = new CombatCommandExecutionService().Execute(state, null,
                CombatCommand.UseSkill(hero.Id, 0, enemy.Id));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.DeliveredSkill, Is.SameAs(hero.SkillOne));
            Assert.That(result.DeliverySource, Is.EqualTo(hero.Position));
            Assert.That(result.DeliveryTarget, Is.EqualTo(enemy.Position));
        }

        [Test]
        public void Execute_ReturnsRejectionForImplicitHeroEndTurnAndRuleFailure()
        {
            CombatState state = State(out UnitState hero, out UnitState enemy);
            CombatCommandExecutionService service = new CombatCommandExecutionService();

            CombatCommandExecutionResult implicitEnd = service.Execute(state, null,
                CombatCommand.EndTurn(hero.Id));
            CombatCommandExecutionResult inactiveAttack = service.Execute(state, null,
                CombatCommand.Attack(hero.Id, enemy.Id));

            Assert.That(implicitEnd.Accepted, Is.False);
            Assert.That(implicitEnd.RejectionReason, Is.EqualTo(CombatCommandExecutionService.ExplicitHeroEndTurnReason));
            Assert.That(inactiveAttack.Accepted, Is.False);
            Assert.That(inactiveAttack.RejectionReason, Is.Not.Empty);
        }

        private static CombatState State(out UnitState hero, out UnitState enemy)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            enemy = new UnitState("enemy", false, new GridPosition(2, 0), Facing.West);
            return new CombatState(new GridMap(4, 2), new[] { hero, enemy });
        }
    }
}
