using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatPassiveRuntimeTests
    {
        [Test]
        public void PassiveSources_RemainDistinctFromTemporaryStatusBarEffects()
        {
            CombatState combat = BuildCombat(out UnitState hero);
            combat.PassiveEffects.RegisterPassive(hero.Id, new CombatPassiveDefinition(
                "origin:test", "测试出身", CombatPassiveSourceKind.OriginTalent, "ORIGIN-TEST",
                CombatPassiveTrigger.AfterActiveMove, "移动后触发。", 30));
            combat.PassiveEffects.RegisterPassive(hero.Id, new CombatPassiveDefinition(
                "equipment:test", "测试装备", CombatPassiveSourceKind.Equipment, "EQUIPMENT-TEST",
                CombatPassiveTrigger.OwnTurnStart, "回合开始触发。", 10));

            combat.PassiveEffects.ArmNextWeaponDamage("ready:test", hero.Id, "蓄势",
                "下次武器命中额外造成 3 点火焰伤害。", CombatPassiveSourceKind.OriginTalent,
                "ORIGIN-TEST", 3, DamageType.Fire);

            CombatStatusBarEntry[] entries = combat.PassiveEffects.StatusBarEntriesFor(hero.Id).ToArray();
            Assert.That(entries.Count(value => value.Kind == CombatStatusBarEntryKind.Passive), Is.EqualTo(2));
            Assert.That(entries.Single(value => value.Kind == CombatStatusBarEntryKind.OngoingEffect).TimingText,
                Is.EqualTo("下次武器命中"));
            Assert.That(hero.Statuses, Is.Empty, "A visible build effect must not become a dispellable unit status.");
            string details = CombatInformationPresenter.BuildRogueliteHeroDetails(combat, hero);
            Assert.That(details, Does.Contain("待触发：蓄势"));
            Assert.That(details, Does.Contain("被动来源：测试出身"));
        }

        [Test]
        public void NextWeaponDamage_IsVisibleThenConsumedExactlyOnce()
        {
            CombatState combat = BuildCombat(out UnitState hero);
            combat.PassiveEffects.ArmNextWeaponDamage("ready:test", hero.Id, "蓄势", "下次命中 +4。",
                CombatPassiveSourceKind.PassiveSpell, "PASSIVE-TEST", 4, DamageType.Fire);

            Assert.That(combat.PassiveEffects.OngoingEffectsFor(hero.Id).Count, Is.EqualTo(1));
            CombatDamageBonus bonus = combat.PassiveEffects.ConsumeNextWeaponDamage(hero.Id).Single();
            Assert.That(bonus.Amount, Is.EqualTo(4));
            Assert.That(bonus.DamageType, Is.EqualTo(DamageType.Fire));
            Assert.That(combat.PassiveEffects.ConsumeNextWeaponDamage(hero.Id), Is.Empty);
            Assert.That(combat.PassiveEffects.OngoingEffectsFor(hero.Id), Is.Empty);
        }

        [Test]
        public void NextTurnActionPoint_IsVisibleThenAppliedAfterBaseTurnReset()
        {
            CombatState combat = BuildCombat(out UnitState hero);
            combat.PassiveEffects.ScheduleNextTurn("next-ap:test", hero.Id, "余势",
                "下次自己回合获得 1 点额外行动力。", CombatPassiveSourceKind.ActiveSpell, "SPELL-TEST",
                CombatOngoingEffectKind.NextTurnActionPoints, 1);

            Assert.That(combat.PassiveEffects.StatusBarEntriesFor(hero.Id).Single().TimingText,
                Is.EqualTo("下次自己回合开始"));
            CombatResolver.BeginTurn(combat, hero.Id);

            Assert.That(hero.ActionPoints, Is.EqualTo(CombatResolver.HeroActionPointsPerTurn + 1));
            Assert.That(combat.PassiveEffects.OngoingEffectsFor(hero.Id), Is.Empty);
            Assert.That(combat.EventLog, Has.Some.Contains("余势触发"));
        }

        [Test]
        public void Clone_PreservesPassiveAndPendingEffectWithoutSharingCollections()
        {
            CombatState combat = BuildCombat(out UnitState hero);
            combat.PassiveEffects.RegisterPassive(hero.Id, new CombatPassiveDefinition(
                "origin:test", "测试出身", CombatPassiveSourceKind.OriginTalent, "ORIGIN-TEST",
                CombatPassiveTrigger.BattleStart, "持续生效。"));
            combat.PassiveEffects.ScheduleNextTurn("next-ap:test", hero.Id, "余势", "下回合 +1 行动力。",
                CombatPassiveSourceKind.ActiveSpell, "SPELL-TEST", CombatOngoingEffectKind.NextTurnActionPoints, 1);

            CombatState clone = combat.Clone();
            clone.PassiveEffects.ConsumeNextWeaponDamage(hero.Id);
            CombatResolver.BeginTurn(clone, hero.Id);

            Assert.That(clone.PassiveEffects.PassivesFor(hero.Id).Count, Is.EqualTo(1));
            Assert.That(clone.GetUnit(hero.Id).ActionPoints, Is.EqualTo(4));
            Assert.That(combat.PassiveEffects.OngoingEffectsFor(hero.Id).Count, Is.EqualTo(1));
        }

        private static CombatState BuildCombat(out UnitState hero)
        {
            hero = new UnitState("hero", true, new GridPosition(0, 0));
            CombatState combat = new CombatState(new GridMap(3, 3), new[] { hero });
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            return combat;
        }
    }
}
