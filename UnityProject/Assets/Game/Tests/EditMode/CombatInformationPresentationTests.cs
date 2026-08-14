using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatInformationPresentationTests
    {
        [Test]
        public void EnemyIntent_UsesAuthoritativeTacticsCommandAndExposesExactSignature()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(2, 0), Facing.West);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(0, 0), Facing.East);
            EnemyArchetypes.Get("pyromancer").Apply(enemy);
            CombatState state = new CombatState(new GridMap(4, 2), new[] { hero, enemy });

            CombatCommand authoritative = EnemyTactics.Choose(state, enemy, hero);
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            EnemyIntentPresentation intent = plans.GetPublicIntent(state, enemy, hero);

            Assert.That(intent.Signature, Is.EqualTo(CombatInformationPresenter.CommandSignature(authoritative)));
            Assert.That(intent.ActionName, Is.EqualTo(enemy.SkillOne.DisplayName));
            Assert.That(intent.TargetSummary, Does.Contain(hero.DisplayName));
        }

        [Test]
        public void MoveIntent_ExposesTheAuthoritativeDestinationForHoverHighlight()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(4, 1), Facing.West);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(0, 1), Facing.East);
            CombatState state = new CombatState(new GridMap(6, 3), new[] { hero, enemy });
            CombatCommand command = CombatCommand.Move(enemy.Id, new GridPosition(2, 1), Facing.East);

            EnemyIntentPresentation intent = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);

            Assert.That(intent.IconId, Is.EqualTo("move"));
            Assert.That(intent.HasDestination, Is.True);
            Assert.That(intent.Destination, Is.EqualTo(command.Destination));
            Assert.That(intent.ExpectedDamage, Is.Zero);
        }

        [Test]
        public void DamageIntent_RecalculatesAgainstTheCurrentDefenses()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 0), Facing.West);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(0, 0), Facing.East);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            CombatCommand command = CombatCommand.Attack(enemy.Id, hero.Id);

            EnemyIntentPresentation before = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            hero.ApplyStatus(StatusType.ArmorBreak, 2, 2);
            EnemyIntentPresentation after = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);

            Assert.That(before.IconId, Is.EqualTo("attack"));
            Assert.That(before.ExpectedDamage, Is.GreaterThan(0));
            Assert.That(after.ExpectedDamage, Is.GreaterThan(before.ExpectedDamage));
            Assert.That(after.ResultSummary, Does.Contain(after.ExpectedDamage + " 点"));
        }

        [Test]
        public void EnemyProfile_ContainsVitalsWeaponSkillsCooldownsAndStatuses()
        {
            UnitState enemy = new UnitState("enemy", false, new GridPosition(0, 0), Facing.East);
            EnemyArchetypes.Get("shieldguard").Apply(enemy);
            enemy.ApplyStatus(StatusType.Burning, 2);

            EnemyInformationPresentation profile = CombatInformationPresenter.BuildEnemyInformation(enemy);

            Assert.That(profile.Vitals, Does.Contain(enemy.Health + "/" + enemy.MaxHealth));
            Assert.That(profile.Defenses, Does.Contain("护甲"));
            Assert.That(profile.Weapon, Does.Contain(enemy.MainHand.DisplayName));
            Assert.That(profile.Skills, Does.Contain(enemy.SkillOne.DisplayName));
            Assert.That(profile.Statuses, Does.Contain("燃烧"));
        }

        [Test]
        public void AttackPreview_ReportsBeforeAfterAndDamageBreakdown()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);

            CombatActionPreview preview = new BattlefieldPresentationAdapter().BuildPreview(state, "攻击", enemy.Id);

            Assert.That(preview.CanSubmit, Is.True);
            Assert.That(preview.TargetBefore, Does.Contain("生命 " + enemy.Health));
            Assert.That(preview.TargetAfter, Does.Contain("生命 "));
            Assert.That(preview.DamageBreakdown, Does.Contain("基础"));
            Assert.That(preview.DamageBreakdown, Does.Contain("护盾吸收"));
        }

        [Test]
        public void DefeatSettlement_DoesNotCapturePostCombatInventoryIntoMapRun()
        {
            RogueliteMapRun run = new RogueliteMapRun(90210);
            string before = run.ToJson();
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState combat = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            combat.ConfigureItemInventory(new InventoryContainerState(), Enumerable.Empty<string>());
            combat.ResolveDebugOutcome(false);

            bool settled = RogueliteCombatSettlement.TrySettleVictory(run, combat);

            Assert.That(settled, Is.False, "a live combat is not a victory and must not be persisted");
            Assert.That(run.ToJson(), Is.EqualTo(before));
        }

        [Test]
        public void DefeatOutcome_ExplainsReasonConsequencesAndRecentEvents()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            for (int i = 0; i < 7; i++) state.AddLog("事件" + i);
            state.ResolveDebugOutcome(false);

            CombatOutcomePresentation outcome = CombatInformationPresenter.BuildOutcome(state, true);

            Assert.That(outcome.Title, Is.EqualTo("战斗失败"));
            Assert.That(outcome.Reason, Does.Contain("英雄倒下"));
            Assert.That(outcome.Consequence, Does.Contain("从战斗前继续"));
            Assert.That(outcome.RecentEvents.Count, Is.EqualTo(5));
            Assert.That(outcome.RemainingEnemyCount, Is.EqualTo(1));
        }

        [Test]
        public void ActionResult_UsesExecutionValuesForSingleStructuredRecord()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(new GridMap(3, 2), new[] { hero, enemy });
            CombatResolver.BeginTurn(state, hero.Id);
            CombatCommand command = CombatCommand.Attack(hero.Id, enemy.Id);

            CombatEffectExecution execution = CombatResolver.Resolve(state, command);
            string record = CombatInformationPresenter.BuildActionResult(state, command, execution);

            Assert.That(record, Does.Contain(hero.DisplayName));
            Assert.That(record, Does.Contain(enemy.DisplayName));
            Assert.That(record, Does.Match("(生命|护盾) [0-9]+→[0-9]+"));
            Assert.That(execution.Results.Count(result => result.Kind == CombatEffectKind.AbsorbShield), Is.LessThanOrEqualTo(1));
            Assert.That(execution.Results.Count(result => result.Kind == CombatEffectKind.DamageHealth), Is.LessThanOrEqualTo(1));
        }
    }
}
