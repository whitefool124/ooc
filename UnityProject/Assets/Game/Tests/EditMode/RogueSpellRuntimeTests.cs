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

        private static CombatState BuildCombat(out UnitState hero, out UnitState enemy)
        {
            GridMap map = new GridMap(5, 1);
            hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            CombatState state = new CombatState(map, new[] { hero, enemy });
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            return state;
        }
    }
}
