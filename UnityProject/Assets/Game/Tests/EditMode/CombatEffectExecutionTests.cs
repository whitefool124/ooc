using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatEffectExecutionTests
    {
        [Test]
        public void AttackPreviewAndExecution_UseTheSameFinalDamage()
        {
            AssertPreviewMatchesExecution(false);
            AssertPreviewMatchesExecution(true);
        }

        private static void AssertPreviewMatchesExecution(bool isCast)
        {
            CombatState state = CreateDuelState();
            CombatResolver.BeginTurn(state, "hero");
            CombatResolver.AttackPreview preview = CombatResolver.PreviewAttack(state, "hero", "enemy", isCast);

            CombatEffectExecution execution = CombatResolver.Resolve(state, isCast ? CombatCommand.Cast("hero", "enemy") : CombatCommand.Attack("hero", "enemy"));

            CombatEffectResult damage = execution.Results.Single(result => result.Kind == CombatEffectKind.DamageHealth);
            Assert.That(damage.AppliedAmount, Is.EqualTo(preview.FinalDamage));
            Assert.That(damage.ValueBefore - damage.ValueAfter, Is.EqualTo(preview.FinalDamage));
        }

        [Test]
        public void SameStateAndCommand_ProduceIdenticalOrderedEffectResults()
        {
            CombatState first = CreateDuelState();
            CombatState second = first.Clone();
            CombatResolver.BeginTurn(first, "hero");
            CombatResolver.BeginTurn(second, "hero");

            CombatEffectExecution firstExecution = CombatResolver.Resolve(first, CombatCommand.UseSkill("hero", 0, "enemy"));
            CombatEffectExecution secondExecution = CombatResolver.Resolve(second, CombatCommand.UseSkill("hero", 0, "enemy"));

            Assert.That(Signature(secondExecution), Is.EqualTo(Signature(firstExecution)));
            Assert.That(firstExecution.Results.Select(result => result.Sequence), Is.EqualTo(Enumerable.Range(0, firstExecution.Results.Count)));
        }

        [Test]
        public void CoreEffects_ExposeCostsRecoveryStatusMovementAndObjectDamageInOrder()
        {
            GridPosition objectPosition = new GridPosition(2, 1);
            GridMap map = new GridMap(5, 3);
            map.SetTile(objectPosition, new TileState { IsObjective = true, Durability = 5 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1), Facing.East);
            CombatState state = new CombatState(map, new[] { hero });
            CombatResolver.BeginTurn(state, hero.Id);
            hero.ApplyStatus(StatusType.Burning, 2);

            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, hero.Id,
                CombatEffect.SpendActionPoints(1),
                CombatEffect.SpendMana(1),
                CombatEffect.AbsorbShield(hero.Id, 1),
                CombatEffect.DamageHealth(hero.Id, 3),
                CombatEffect.RestoreHealth(hero.Id, 2),
                CombatEffect.RestoreShield(hero.Id, 2),
                CombatEffect.RestoreMana(hero.Id, 1),
                CombatEffect.ClearStatus(hero.Id, StatusType.Burning),
                CombatEffect.ApplyStatus(hero.Id, StatusType.Slow, 2),
                CombatEffect.Move(new GridPosition(1, 1), Facing.East),
                CombatEffect.DamageObject(objectPosition, 3),
                CombatEffect.DelayInitiative(4));

            Assert.That(execution.Results.Select(result => result.Kind), Is.EqualTo(new[]
            {
                CombatEffectKind.SpendActionPoints, CombatEffectKind.SpendMana, CombatEffectKind.AbsorbShield, CombatEffectKind.DamageHealth,
                CombatEffectKind.RestoreHealth, CombatEffectKind.RestoreShield, CombatEffectKind.RestoreMana,
                CombatEffectKind.ClearStatus, CombatEffectKind.ApplyStatus, CombatEffectKind.Move,
                CombatEffectKind.DamageObject, CombatEffectKind.DelayInitiative
            }));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(hero.HasStatus(StatusType.Burning), Is.False);
            Assert.That(hero.StatusDuration(StatusType.Slow), Is.EqualTo(2));
            Assert.That(state.Map.GetTile(objectPosition).Durability, Is.EqualTo(2));
        }

        [Test]
        public void QuickbarRecovery_ReturnsAppliedShieldAndStatusClearResults()
        {
            CombatState state = CreateDuelState();
            UnitState hero = state.GetUnit("hero");
            InventoryContainerState inventory = new InventoryContainerState();
            Assert.That(inventory.AddFirstFit(new ItemInstance("shield-cell", "shield_cell", 0)).Success, Is.True);
            state.ConfigureItemInventory(inventory, new[] { "shield-cell" });
            CombatResolver.BeginTurn(state, hero.Id);
            hero.ApplyStatus(StatusType.Burning, 2);

            CombatEffectExecution execution = CombatResolver.Resolve(state, CombatCommand.UseQuickbar(hero.Id, 0));

            Assert.That(execution.Results.Single(result => result.Kind == CombatEffectKind.RestoreShield).AppliedAmount, Is.EqualTo(4));
            Assert.That(execution.Results.Single(result => result.Kind == CombatEffectKind.ClearStatus).AppliedAmount, Is.EqualTo(2));
            Assert.That(hero.HasStatus(StatusType.Burning), Is.False);
            Assert.That(state.ItemInventory.Get("shield-cell"), Is.Null);
            Assert.That(state.ItemQuickbar[0], Is.Null);
        }

        [Test]
        public void InvalidLateEffect_IsRejectedBeforeEarlierCostsMutateState()
        {
            CombatState state = CreateDuelState();
            UnitState hero = state.GetUnit("hero");
            CombatResolver.BeginTurn(state, hero.Id);
            int actionPoints = hero.ActionPoints;
            int mana = hero.Mana;

            Assert.Throws<InvalidOperationException>(() => CombatEffectExecutor.Execute(state, hero.Id,
                CombatEffect.SpendActionPoints(1),
                CombatEffect.SpendMana(1),
                CombatEffect.DamageHealth("missing-target", 5)));

            Assert.That(hero.ActionPoints, Is.EqualTo(actionPoints));
            Assert.That(hero.Mana, Is.EqualTo(mana));
        }

        private static string Signature(CombatEffectExecution execution) => string.Join("|", execution.Results.Select(result =>
            $"{result.Sequence}:{result.Kind}:{result.SourceUnitId}:{result.TargetUnitId}:{result.RequestedAmount}:{result.AppliedAmount}:{result.ValueBefore}:{result.ValueAfter}:{result.Status}:{result.Duration}:{result.PositionBefore}:{result.PositionAfter}"));

        private static CombatState CreateDuelState() => new CombatState(
            new GridMap(6, 3),
            new[]
            {
                new UnitState("hero", true, new GridPosition(0, 1), Facing.East),
                new UnitState("enemy", false, new GridPosition(3, 1), Facing.East)
            });
    }
}
