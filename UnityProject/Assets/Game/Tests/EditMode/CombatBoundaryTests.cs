using NUnit.Framework;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatBoundaryTests
    {
        [Test]
        public void AvailabilityQuery_DelegatesPreviewToTheAuthoritativeRuleAdapter()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero });
            CombatResolver.BeginTurn(state, hero.Id);
            CombatActionPreview query = new CombatAvailabilityQuery().Preview(state, "移动", null);
            CombatActionPreview authority = new BattlefieldPresentationAdapter().BuildPreview(state, "移动", null);
            Assert.That(query.FailureReason, Is.EqualTo(authority.FailureReason));
            Assert.That(query.ValidCellCount, Is.EqualTo(authority.ValidCellCount));
        }

        [Test]
        public void EnemyPlan_UsesTheSameCommandForPublicIntentAndExecutionUntilInvalidated()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(2, 0), Facing.West);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(0, 0), Facing.East);
            EnemyArchetypes.Get("pyromancer").Apply(enemy);
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            EnemyIntentPresentation intent = plans.GetPublicIntent(state, enemy, hero);
            CombatCommand execution = plans.GetExecutionCommand(state, enemy, hero);
            Assert.That(intent.Signature, Is.EqualTo(CombatInformationPresenter.CommandSignature(execution)));
            Assert.That(plans.HasPlanFor(enemy.Id), Is.True);
            plans.Invalidate();
            Assert.That(plans.HasPlanFor(enemy.Id), Is.False);
        }

        [Test]
        public void DefaultConfiguration_DoesNotEnableDeveloperEntrypoints()
        {
            Assert.That(DeveloperBuildGate.IsEnabled, Is.False);
        }

        [Test]
        public void PublicIntent_DoesNotExposeCombatCommand()
        {
            Assert.That(typeof(EnemyIntentPresentation).GetProperty("Command"), Is.Null);
        }
    }
}
