using System;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class RogueSpellRuntimeTests
    {
        [Test]
        public void M2Loadout_HasEightSlotsLocksInCombatAndKeepsBasicsMastered()
        {
            RogueSpellLoadout loadout = RogueSpellLoadout.CreateStarter();
            loadout.Equip(4, "F-P-R01");
            RogueSpellLoadout snapshot = loadout.CreateCombatSnapshot();

            Assert.That(snapshot.EquippedSpellIds.Length, Is.EqualTo(8));
            Assert.That(snapshot.EquippedSpellIds.Take(4), Is.EqualTo(new[] { "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER" }));
            Assert.That(snapshot.IsCombatLocked, Is.True);
            Assert.Throws<InvalidOperationException>(() => snapshot.Equip(4, "F-P-R02"));
            Assert.That(loadout.MasteredSpellIds, Does.Contain("BASE-MANA-RECOVER"));
        }

        [Test]
        public void M2Basics_ZeroManaCanRecoverAndCooldownBlocksOneCompleteOwnTurn()
        {
            CombatState combat = BuildCombat(out UnitState hero, out UnitState enemy);
            hero.ConfigureMana(12, 0);
            RogueSpellCombatRuntime runtime = new RogueSpellCombatRuntime(combat, RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, "hero");

            RogueSpellExecution first = runtime.ExecuteSlot(3, CombatCommand.UseSkill("hero", 3, "hero"));
            Assert.That(first.Accepted, Is.True);
            Assert.That(hero.Mana, Is.EqualTo(2));
            Assert.That(runtime.IsReady("BASE-MANA-RECOVER"), Is.False);
            runtime.BeginOwnTurn("hero");
            Assert.That(runtime.IsReady("BASE-MANA-RECOVER"), Is.False);
            runtime.BeginOwnTurn("hero");
            Assert.That(runtime.IsReady("BASE-MANA-RECOVER"), Is.True);
        }

        [Test]
        public void M2Basics_MeleeRangedAndShieldUseFrozenValuesAndUniqueDamageChain()
        {
            CombatState combat = BuildCombat(out UnitState hero, out UnitState enemy);
            RogueSpellCombatRuntime runtime = new RogueSpellCombatRuntime(combat, RogueSpellLoadout.CreateStarter().CreateCombatSnapshot());
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, "hero");

            int before = enemy.Health;
            runtime.ExecuteSlot(0, CombatCommand.UseSkill("hero", 0, "enemy"));
            Assert.That(before - enemy.Health, Is.EqualTo(8));
            runtime.ExecuteSlot(2, CombatCommand.UseSkill("hero", 2, "hero"));
            Assert.That(hero.Shield, Is.EqualTo(6));
        }

        [Test]
        public void M2AllSixtyFireSpells_AreRewardEligibleAndHaveRogueInterpretableRules()
        {
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            SpellDefinition[] fire = catalog.Spells.Where(value => value.RewardEligible).ToArray();
            Assert.That(fire.Length, Is.EqualTo(60));
            foreach (SpellDefinition spell in fire)
            {
                RogueValidationResult validation = RogueSpellRuleInterpreter.Validate(spell);
                Assert.That(validation.IsValid, Is.True, spell.DefinitionId + ": " + string.Join(";", validation.Errors));
                Assert.That(spell.Rules, Has.None.Contains("ArmorBreak"));
                Assert.That(spell.Rules, Has.None.Contains("ReduceIncomingDamage"));
                Assert.That(spell.Rules, Has.None.Contains("RepairWeapon"));
            }
        }

        [Test]
        public void M2FrozenCorrections_ArePresentInAuthoritativeFireCatalog()
        {
            Assert.That(FireSpellCatalog.Get("F-P-M19").Rules.Single(value => value.Kind == FireRuleKind.LoseHealth).Amount, Is.EqualTo(8));
            Assert.That(FireSpellCatalog.Get("F-P-U11").ManaCost, Is.Zero);
            Assert.That(FireSpellCatalog.Get("F-P-U13").ManaCost, Is.EqualTo(1));
            Assert.That(FireSpellCatalog.Get("F-P-U13").Rules.Single(value => value.Kind == FireRuleKind.RestoreMana).Amount, Is.EqualTo(3));
            Assert.That(FireSpellCatalog.Get("F-P-U14").Rules.Single(value => value.Kind == FireRuleKind.Damage).Amount, Is.EqualTo(12));
            Assert.That(FireSpellCatalog.Get("F-P-U15").Rules.Any(value => value.Kind == FireRuleKind.Damage && value.Amount == 4), Is.True);
            Assert.That(FireSpellCatalog.Get("F-P-U18").Rules.Any(value => value.Kind == FireRuleKind.RepairWeapon), Is.False);
            Assert.That(FireSpellCatalog.Get("F-P-U20").ActionPointCost, Is.EqualTo(2));
            Assert.That(FireSpellCatalog.Get("F-P-R08").Rarity, Is.EqualTo(FireSpellRarity.Uncommon));
        }

        [Test]
        public void ElitePassives_OnlyAffectHeroAndCannotBeActivelyCast()
        {
            CombatState combat = BuildCombat(out UnitState hero, out UnitState enemy);
            RogueSpellLoadout loadout = LockedLoadout("PASSIVE-ELITE-03");
            RogueSpellCombatRuntime runtime = new RogueSpellCombatRuntime(combat, loadout);
            combat.AttachRogueSpellRuntime(runtime);

            CombatResolver.BeginTurn(combat, enemy.Id);
            Assert.That(enemy.Shield, Is.Zero);
            CombatResolver.BeginTurn(combat, hero.Id);
            Assert.That(hero.Shield, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() =>
                runtime.ExecuteSlot(4, CombatCommand.UseSkill(hero.Id, 4, hero.Id)));
        }

        [Test]
        public void TemperingFlow_TriggersOnceForBasicPersonalFireDamageEachHeroTurn()
        {
            CombatState combat = BuildCombat(out UnitState hero, out UnitState enemy);
            hero.ConfigureMana(12, 12);
            RogueSpellCombatRuntime runtime = new RogueSpellCombatRuntime(combat, LockedLoadout("PASSIVE-ELITE-01"));
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, hero.Id);

            runtime.ExecuteSlot(1, CombatCommand.UseSkill(hero.Id, 1, enemy.Id));
            Assert.That(hero.Mana, Is.EqualTo(11));
            runtime.ExecuteSlot(1, CombatCommand.UseSkill(hero.Id, 1, enemy.Id));
            Assert.That(hero.Mana, Is.EqualTo(9));
        }

        [Test]
        public void MomentumIgnition_TriggersOnlyForHeroAndAtMostOncePerTurn()
        {
            CombatState combat = BuildCombat(out UnitState hero, out UnitState enemy);
            RogueSpellCombatRuntime runtime = new RogueSpellCombatRuntime(combat, LockedLoadout("PASSIVE-ELITE-02"));
            combat.AttachRogueSpellRuntime(runtime);
            GridPosition[] threeCellPath =
            {
                new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0), new GridPosition(3, 0)
            };

            runtime.BeginOwnTurn(enemy.Id);
            runtime.AfterMove(enemy.Id, threeCellPath);
            int before = hero.Health;
            runtime.AfterWeaponHit(enemy.Id, hero);
            Assert.That(hero.Health, Is.EqualTo(before));

            runtime.BeginOwnTurn(hero.Id);
            runtime.AfterMove(hero.Id, threeCellPath);
            before = enemy.Health;
            runtime.AfterWeaponHit(hero.Id, enemy);
            Assert.That(before - enemy.Health, Is.EqualTo(4));
            runtime.AfterMove(hero.Id, threeCellPath);
            runtime.AfterWeaponHit(hero.Id, enemy);
            Assert.That(before - enemy.Health, Is.EqualTo(4));
        }

        [Test]
        public void AmplifySpecialization_UsesFrozenBasicAndFirstRunSpellValues()
        {
            CombatState basic = BuildCombat(out UnitState basicHero, out UnitState basicEnemy);
            RogueSpellCombatRuntime basicRuntime = new RogueSpellCombatRuntime(basic, LockedLoadout(), "BASE-FIRE-RANGED");
            basic.AttachRogueSpellRuntime(basicRuntime);
            CombatResolver.BeginTurn(basic, basicHero.Id);
            int before = basicEnemy.Health;
            basicRuntime.ExecuteSlot(1, CombatCommand.UseSkill(basicHero.Id, 1, basicEnemy.Id));
            Assert.That(before - basicEnemy.Health, Is.EqualTo(8));

            CombatState leap = TrainingRangeScenarioFactory.CreateStandard();
            leap.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState leapHero = leap.GetUnit("hero");
            UnitState leapEnemy = leap.GetUnit("range_normal");
            CombatEffectExecutor.Execute(leap, leapEnemy.Id, CombatEffect.Move(new GridPosition(5, 4)));
            leapHero.Equip(CombatCatalog.Hammer, leapHero.OffHand, leapHero.SkillOne, leapHero.SkillTwo);
            RogueSpellCombatRuntime leapRuntime = new RogueSpellCombatRuntime(leap, LockedLoadout("F-P-M03"), "F-P-M03");
            leap.AttachRogueSpellRuntime(leapRuntime);
            CombatResolver.BeginTurn(leap, leapHero.Id);
            before = leapEnemy.Health;
            leapRuntime.ExecuteSlot(4, CombatCommand.UseSkillAt(leapHero.Id, 4, new GridPosition(4, 4), CardinalDirection.East));
            Assert.That(before - leapEnemy.Health, Is.EqualTo(10));

            CombatState breach = TrainingRangeScenarioFactory.CreateStandard();
            breach.ConfigureRuleset(CombatRuleset.Roguelite);
            GridPosition objectCell = TrainingRangeScenarioFactory.ObjectTargetCell;
            breach.Map.SetTile(objectCell, new TileState { Cover = CoverType.Light, Durability = 40 });
            RogueSpellCombatRuntime breachRuntime = new RogueSpellCombatRuntime(breach, LockedLoadout("F-P-R19"), "F-P-R19");
            breach.AttachRogueSpellRuntime(breachRuntime);
            CombatResolver.BeginTurn(breach, "hero");
            breachRuntime.ExecuteSlot(4, CombatCommand.UseSkillAt("hero", 4, objectCell, CardinalDirection.East));
            Assert.That(breach.Map.GetTile(objectCell).Durability, Is.EqualTo(20));
        }

        private static RogueSpellLoadout LockedLoadout(string extraSpellId = "")
        {
            string[] ids =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                extraSpellId, string.Empty, string.Empty, string.Empty
            };
            return RogueSpellLoadout.Restore(ids.Where(value => !string.IsNullOrEmpty(value)), ids, true);
        }

        private static CombatState BuildCombat(out UnitState hero, out UnitState enemy)
        {
            GridMap map = new GridMap(5, 1);
            hero = new UnitState("hero", true, new GridPosition(0, 0));
            enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            CombatState state = new CombatState(map, new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            return state;
        }
    }
}
