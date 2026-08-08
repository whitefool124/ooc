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
            hero.ApplyStatus(StatusType.Slow, 2);
            hero.ApplyStatus(StatusType.Bound, 1);
            hero.ApplyStatus(StatusType.ArmorBreak, 2);

            CombatEffectExecution execution = CombatResolver.BeginTurn(state, hero.Id);

            Assert.That(execution.Results.Select(result => result.Kind), Is.EqualTo(new[]
            {
                CombatEffectKind.TriggerStatus, CombatEffectKind.DamageHealth, CombatEffectKind.ReduceStatusDuration,
                CombatEffectKind.TriggerStatus, CombatEffectKind.ReduceStatusDuration,
                CombatEffectKind.TriggerStatus, CombatEffectKind.ReduceStatusDuration,
                CombatEffectKind.TriggerStatus, CombatEffectKind.ReduceStatusDuration
            }));
            Assert.That(execution.Results.Where(result => result.Kind == CombatEffectKind.TriggerStatus).Select(result => result.Status),
                Is.EqualTo(new[] { StatusType.Burning, StatusType.Slow, StatusType.Bound, StatusType.ArmorBreak }));
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth - CombatStatusLifecycle.BurningDamagePerTurn));
            Assert.That(hero.Shield, Is.EqualTo(2), "Burning preserves the existing direct-health rule.");
            Assert.That(hero.StatusDuration(StatusType.Burning), Is.EqualTo(1));
            Assert.That(hero.StatusDuration(StatusType.Slow), Is.EqualTo(1));
            Assert.That(hero.HasStatus(StatusType.Bound), Is.False);
            Assert.That(hero.StatusDuration(StatusType.ArmorBreak), Is.EqualTo(1));
            Assert.That(hero.EffectiveSpeed, Is.EqualTo(7));
            Assert.That(hero.EffectiveArmor, Is.EqualTo(0));
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
            Assert.That(preserved.ValueBefore, Is.EqualTo(2));
            Assert.That(preserved.ValueAfter, Is.EqualTo(2));
            Assert.That(refreshed.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Refreshed));
            Assert.That(refreshed.ValueAfter, Is.EqualTo(4));
        }

        [Test]
        public void DurationOneBound_ExpiresBeforeActionsAndNoLongerBlocksMovement()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            hero.ApplyStatus(StatusType.Bound, 1);

            CombatEffectExecution lifecycle = CombatResolver.BeginTurn(state, hero.Id);
            CombatEffectExecution movement = CombatResolver.Resolve(state, CombatCommand.Move(hero.Id, new GridPosition(1, 0), Facing.East));

            CombatEffectResult expiry = lifecycle.Results.Single(result => result.Kind == CombatEffectKind.ReduceStatusDuration);
            Assert.That(expiry.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Expired));
            Assert.That(hero.HasStatus(StatusType.Bound), Is.False);
            Assert.That(movement.Results.Last().Kind, Is.EqualTo(CombatEffectKind.Move));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void BurningLethalTick_IsRecordedBeforeExpiryAndDefeat()
        {
            CombatState state = CreateHeroState();
            UnitState hero = state.GetUnit("hero");
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageHealth(hero.Id, hero.MaxHealth - 1));
            hero.ApplyStatus(StatusType.Burning, 1);

            CombatEffectExecution execution = CombatResolver.BeginTurn(state, hero.Id);

            CombatEffectResult damage = execution.Results.Single(result => result.Kind == CombatEffectKind.DamageHealth);
            CombatEffectResult expiry = execution.Results.Single(result => result.Kind == CombatEffectKind.ReduceStatusDuration);
            Assert.That(damage.Sequence, Is.LessThan(expiry.Sequence));
            Assert.That(damage.AppliedAmount, Is.EqualTo(1));
            Assert.That(expiry.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Expired));
            Assert.That(state.IsDefeat, Is.True);
            Assert.That(hero.ActionPoints, Is.EqualTo(0));
            Assert.That(state.EventLog[0], Does.Contain("\u6301\u7eed\u6548\u679c\u51fb\u5012"));
            Assert.That(state.EventLog, Has.Some.Contains("\u71c3\u70e7\u89e6\u53d1"));
            Assert.That(state.EventLog, Has.Some.Contains("\u71c3\u70e7\u5df2\u5230\u671f"));
        }

        [Test]
        public void SameTurnStart_ProducesIdenticalLifecycleSignature()
        {
            CombatState first = CreateHeroState();
            first.GetUnit("hero").ApplyStatus(StatusType.Burning, 2);
            first.GetUnit("hero").ApplyStatus(StatusType.Slow, 2);
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
            hero.ApplyStatus(StatusType.ArmorBreak, 3);

            CombatEffectResult result = CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.ClearStatus(hero.Id, StatusType.ArmorBreak)).Results.Single();

            Assert.That(result.StatusPhase, Is.EqualTo(CombatStatusLifecyclePhase.Cleared));
            Assert.That(result.ValueBefore, Is.EqualTo(3));
            Assert.That(result.ValueAfter, Is.EqualTo(0));
            Assert.That(result.AppliedAmount, Is.EqualTo(3));
        }

        private static string Signature(CombatEffectExecution execution) => string.Join("|", execution.Results.Select(result =>
            $"{result.Sequence}:{result.Kind}:{result.Status}:{result.StatusPhase}:{result.AppliedAmount}:{result.ValueBefore}:{result.ValueAfter}"));

        private static CombatState CreateHeroState() => new CombatState(
            new GridMap(4, 4),
            new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) });
    }
}
