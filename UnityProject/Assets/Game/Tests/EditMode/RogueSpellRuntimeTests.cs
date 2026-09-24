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
        public void AllReviewedFireSpells_AreRewardEligibleAndHaveRogueInterpretableRules()
        {
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            SpellDefinition[] fire = catalog.Spells.Where(value => value.RewardEligible).ToArray();
            Assert.That(fire.Length, Is.EqualTo(FireSpellCatalog.All.Count));
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
            Assert.That(combat.PassiveEffects.StatusBarEntriesFor(hero.Id),
                Has.Some.Matches<CombatStatusBarEntry>(value => value.DisplayName == "动势点火" && value.TimingText == "下次武器命中"));
            before = enemy.Health;
            runtime.AfterWeaponHit(hero.Id, enemy);
            Assert.That(before - enemy.Health, Is.EqualTo(4));
            Assert.That(combat.PassiveEffects.OngoingEffectsFor(hero.Id), Is.Empty);
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
            // R19 熔障爆点带破障标记（总案 3.5.6.1）：物块承受双倍耐久伤害，故 40 耐久的轻掩体被打空。
            Assert.That(breach.Map.GetTile(objectCell).Durability, Is.EqualTo(0));
        }

        [Test]
        public void AmplifySpecialization_CoversTheDocumentedTwelveAndKeepsBalancedCosts()
        {
            string[] documented =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                "F-P-M01", "F-P-M03", "F-P-M06", "F-P-U01", "F-P-U04", "F-P-U18", "F-P-R01", "F-P-R19"
            };
            foreach (string id in documented) Assert.That(RogueSpellCombatRuntime.SupportsSpecialization(id), Is.True, id);
            Assert.That(RogueSpellCombatRuntime.SupportsSpecialization("F-P-U07"), Is.False);
            Assert.That(RogueSpellCombatRuntime.SupportsSpecialization("F-P-R02"), Is.False);

            FireSpellDefinition m01 = RogueSpellCombatRuntime.ApplyAmplifySpecialization(FireSpellCatalog.Get("F-P-M01"));
            Assert.That(m01.Rules.Where(rule => rule.Kind == FireRuleKind.Damage && rule.Timing == FireRuleTiming.OnTrigger).Max(rule => rule.Amount), Is.EqualTo(10));

            FireSpellDefinition m06 = RogueSpellCombatRuntime.ApplyAmplifySpecialization(FireSpellCatalog.Get("F-P-M06"));
            Assert.That(m06.Rules.Any(rule => rule.Kind == FireRuleKind.Damage && rule.Amount == 2 && rule.Timing == FireRuleTiming.OnTrigger), Is.True);

            FireSpellDefinition r01 = RogueSpellCombatRuntime.ApplyAmplifySpecialization(FireSpellCatalog.Get("F-P-R01"));
            Assert.That(r01.Rules.Single(rule => rule.Kind == FireRuleKind.Damage).Amount, Is.EqualTo(14));

            // 变体不得二次减费：行动点、魔力与冷却必须与目录中的平衡值一致。
            foreach (string id in new[] { "F-P-M01", "F-P-M03", "F-P-M06", "F-P-U01", "F-P-U04", "F-P-U18", "F-P-R01", "F-P-R19" })
            {
                FireSpellDefinition origin = FireSpellCatalog.Get(id);
                FireSpellDefinition variant = RogueSpellCombatRuntime.ApplyAmplifySpecialization(origin);
                Assert.That(variant.ActionPointCost, Is.EqualTo(origin.ActionPointCost), id);
                Assert.That(variant.ManaCost, Is.EqualTo(origin.ManaCost), id);
                Assert.That(variant.Cooldown, Is.EqualTo(origin.Cooldown), id);
            }

            // 总案 3.5.6.5：只有普通与罕见下调 1 点魔力，稀有不下调（与技能配置表一致）。
            // 三链落表后 U17 焦土警戒为普通（落表魔力 4 → 结算 3），R19 熔障爆点为稀有不参与下调。
            Assert.That(FireSpellCatalog.Get("F-P-M07").ManaCost, Is.EqualTo(5));
            Assert.That(FireSpellCatalog.Get("F-P-U17").ManaCost, Is.EqualTo(3));
            Assert.That(FireSpellCatalog.Get("F-P-R19").ManaCost, Is.EqualTo(4));

            // 未登记专精的术式原样返回，不产生空结果变体。
            Assert.That(RogueSpellCombatRuntime.ApplyAmplifySpecialization(FireSpellCatalog.Get("F-P-U07")).Rules.Count,
                Is.EqualTo(FireSpellCatalog.Get("F-P-U07").Rules.Count));

            // 节流刻墨（总案 4.2.2.1）：在已生效费用上再降 1 点魔力（最低 0）；回路调息的节流降的是冷却。
            string[] throttled = { "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER", "F-P-M01", "F-P-M06", "F-P-U01", "F-P-R01" };
            foreach (string id in throttled) Assert.That(RogueSpellCombatRuntime.SupportsThrottleSpecialization(id), Is.True, id);
            Assert.That(RogueSpellCombatRuntime.SupportsThrottleSpecialization("F-P-R02"), Is.False);
            Assert.That(RogueSpellCombatRuntime.ApplyThrottleSpecialization(FireSpellCatalog.Get("F-P-M01")).ManaCost,
                Is.EqualTo(FireSpellCatalog.Get("F-P-M01").ManaCost - 1));
            Assert.That(RogueSpellCombatRuntime.ApplyThrottleSpecialization(FireSpellCatalog.Get("F-P-R01")).ManaCost,
                Is.EqualTo(0));
            // 基础四式不在 FireSpellCatalog 内（属另一套 SpellDefinition），其节流由 ExecuteBasic 路径处理。
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
