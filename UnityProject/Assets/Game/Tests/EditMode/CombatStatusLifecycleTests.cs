using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatStatusLifecycleTests
    {
        [Test]
        public void TurnStart_ResolvesAllStatusesInFixedOrder()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            hero.ApplyStatus(StatusType.Burning, 2);
            hero.ApplyStatus(StatusType.Agility, 1, -1);
            hero.ApplyStatus(StatusType.Bound, 1);

            CombatEffectExecution execution = CombatResolver.BeginTurn(state, hero.Id);

            Assert.That(execution.Results.Select(result => result.Kind), Is.EqualTo(new[]
            {
                CombatEffectKind.TriggerStatus, CombatEffectKind.ReduceStatusDuration
            }));
            Assert.That(execution.Results.Where(result => result.Kind == CombatEffectKind.TriggerStatus).Select(result => result.Status),
                Is.EqualTo(new[] { StatusType.Bound }));
            // 燃烧只在自身回合结束触发，回合开始不增加触发次数。
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth));
            Assert.That(hero.StatusDuration(StatusType.Burning), Is.EqualTo(2));
            Assert.That(hero.StatusTriggerCount(StatusType.Burning), Is.Zero);
            Assert.That(hero.Shield, Is.EqualTo(2), "Burning preserves the existing direct-health rule.");
            Assert.That(hero.StatusDuration(StatusType.Agility), Is.EqualTo(1));
            Assert.That(hero.HasStatus(StatusType.Bound), Is.True);
            Assert.That(hero.MovementRangeThisTurn, Is.EqualTo(UnitState.HeroSlowedMovementRange));
            Assert.That(hero.EffectiveSpeed, Is.EqualTo(10));
            Assert.That(hero.EffectiveArmor, Is.EqualTo(1));

            CombatResolver.EndTurn(state, hero);
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth - CombatStatusLifecycle.BurningDamagePerTurn),
                "回合结束时结算 4 点燃烧伤害。");
            Assert.That(hero.StatusDuration(StatusType.Burning), Is.EqualTo(1), "回合结束扣一次持续量。");
            Assert.That(hero.StatusTriggerCount(StatusType.Burning), Is.EqualTo(1));
            Assert.That(hero.HasStatus(StatusType.Agility), Is.False);
        }

        [Test]
        public void ReapplyStatus_PreservesLongerDurationAndRefreshesOnlyWhenLonger()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");

            CombatEffectResult applied = CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ApplyStatus(hero.Id, StatusType.Bound, 2)).Results.Single();
            CombatEffectResult preserved = CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ApplyStatus(hero.Id, StatusType.Bound, 1)).Results.Single();
            CombatEffectResult refreshed = CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ApplyStatus(hero.Id, StatusType.Bound, 4)).Results.Single();

            Assert.That(applied.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Applied));
            Assert.That(preserved.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Preserved));
            Assert.That(preserved.ValueBefore, Is.EqualTo(3));
            Assert.That(preserved.ValueAfter, Is.EqualTo(3));
            Assert.That(refreshed.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Refreshed));
            Assert.That(refreshed.ValueAfter, Is.EqualTo(5));
        }

        [Test]
        public void DurationOneBound_BlocksTheNextOwnActionThenExpires()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            hero.ApplyStatus(StatusType.Bound, 1);

            CombatResolver.BeginTurn(state, hero.Id);
            Assert.That(hero.HasStatus(StatusType.Bound), Is.True);
            Assert.Throws<System.InvalidOperationException>(() => CombatResolver.Resolve(state,
                CombatCommand.Move(hero.Id, new GridPosition(1, 0))));
            CombatResolver.EndTurn(state, hero);
            CombatEffectExecution movement = CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(1, 0)));

            Assert.That(hero.HasStatus(StatusType.Bound), Is.False);
            Assert.That(movement.Results.Last().Kind, Is.EqualTo(CombatEffectKind.Move));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void BurningLethalTick_IsRecordedAtOwnTurnEndBeforeExpiryAndDefeat()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageHealth(hero.Id, hero.MaxHealth - 1));
            hero.ApplyStatus(StatusType.Burning, 1);
            CombatResolver.BeginTurn(state, hero.Id);

            // 燃烧在自身回合结束结算：伤害先于持续量到期，倒地结果写进公开日志。
            CombatResolver.EndTurn(state, hero);

            Assert.That(hero.IsAlive, Is.False);
            Assert.That(state.IsDefeat, Is.True);
            Assert.That(state.EventLog, Has.Some.Contains("失去行动能力"));
            Assert.That(state.EventLog, Has.None.Contains("击杀"));
            Assert.That(state.EventLog, Has.Some.Contains("燃烧触发"));
        }

        [Test]
        public void BurningUsesInstanceStrengthAndSourceSurvivesClone()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            hero.ApplyStatus(StatusType.Burning, 2, 8, "training-flame");
            CombatState copy = state.Clone();

            Assert.That(copy.GetUnit("hero").StatusSource(StatusType.Burning), Is.EqualTo("training-flame"));
            Assert.That(copy.GetUnit("hero").StatusAppliedOrder(StatusType.Burning), Is.EqualTo(hero.StatusAppliedOrder(StatusType.Burning)));
            Assert.That(CombatStatusPresentation.From(copy.GetUnit("hero"), StatusType.Burning).Detail,
                Does.Contain("失去 8 点生命"));
            CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.EndTurn(state, hero);
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth - 8));
        }

        [Test]
        public void AttributeInstances_AddValuesAndExpireIndependentlyAtOwnTurnEnd()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            hero.ApplyStatus(StatusType.Agility, 1, -2, "snare");
            hero.ApplyStatus(StatusType.Agility, 2, 1, "rally");
            Assert.That(hero.StatusStrength(StatusType.Agility), Is.EqualTo(-1));
            Assert.That(hero.StatusSource(StatusType.Agility), Does.Contain("snare"));
            Assert.That(hero.StatusSource(StatusType.Agility), Does.Contain("rally"));
            CombatResolver.BeginTurn(state, hero.Id);
            Assert.That(hero.MovementRangeThisTurn, Is.EqualTo(2));
            CombatResolver.EndTurn(state, hero);
            Assert.That(hero.StatusStrength(StatusType.Agility), Is.EqualTo(1));
            Assert.That(hero.StatusDuration(StatusType.Agility), Is.EqualTo(1));
            Assert.That(state.Clone().GetUnit(hero.Id).StatusSource(StatusType.Agility), Is.EqualTo("rally"));
        }

        [Test]
        public void AttributeFeedback_ReportsChangedValueEvenWhenDurationIsUnchanged()
        {
            CombatState state = CreateHeroState();
            CombatEffectResult first = CombatEffectExecutor.Execute(state, "hero",
                CombatEffect.ApplyStatus("hero", StatusType.Strength, 2, 1)).Results.Single();
            CombatEffectResult second = CombatEffectExecutor.Execute(state, "hero",
                CombatEffect.ApplyStatus("hero", StatusType.Strength, 2, 2)).Results.Single();

            Assert.That(first.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Applied));
            Assert.That(second.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Refreshed));
            Assert.That(second.StatusStrengthBefore, Is.EqualTo(1));
            Assert.That(second.StatusStrengthAfter, Is.EqualTo(3));
            Assert.That(second.Changed, Is.True);
        }

        [Test]
        public void SameTurnStart_ProducesIdenticalLifecycleSignature()
        {
            CombatState first = CreateHeroState();
            first.GetUnit("hero").ApplyStatus(StatusType.Burning, 2);
            first.GetUnit("hero").ApplyStatus(StatusType.Agility, 2, -1);
            CombatState second = first.Clone();

            CombatEffectExecution firstExecution = CombatResolver.BeginTurn(first, "hero");
            CombatEffectExecution secondExecution = CombatResolver.BeginTurn(second, "hero");

            Assert.That(Signature(secondExecution), Is.EqualTo(Signature(firstExecution)));
            Assert.That(second.EventLog, Is.EqualTo(first.EventLog));
        }

        [Test]
        public void ClearStatus_RecordsClearedPhaseAndOriginalDuration()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            hero.ApplyStatus(StatusType.BreakStance, 3);

            CombatEffectResult result = CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ClearStatus(hero.Id, StatusType.BreakStance)).Results.Single();

            Assert.That(result.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Cleared));
            Assert.That(result.ValueBefore, Is.EqualTo(3));
            Assert.That(result.ValueAfter, Is.EqualTo(0));
            Assert.That(result.AppliedAmount, Is.EqualTo(3));
        }

        private static string Signature(CombatEffectExecution execution) => string.Join("|", execution.Results.Select(result =>
            $"{result.Sequence}:{result.Kind}:{result.Status}:{result.StatusPhase}:{result.AppliedAmount}:{result.ValueBefore}:{result.ValueAfter}"));

        private static CombatState CreateHeroState() => new CombatState(
            new GridMap(4, 4),
            new[] { new UnitState("hero", true, new GridPosition(0, 0)) });
    }
}
