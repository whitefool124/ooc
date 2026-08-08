using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FireSpellCatalogTests
    {
        [Test]
        public void Catalog_IsV02WithTwentyMeleeUniversalAndRangedEntries()
        {
            string[] expected = new[] { "M", "U", "R" }.SelectMany(prefix =>
                Enumerable.Range(1, 20).Select(index => $"F-P-{prefix}{index:00}")).ToArray();
            Assert.That(FireSpellCatalog.Version, Is.EqualTo("fire-personal-spells-v0.2"));
            Assert.That(FireSpellCatalog.All.Count, Is.EqualTo(60));
            Assert.That(FireSpellCatalog.All.Select(spell => spell.Id), Is.EqualTo(expected));
            Assert.That(FireSpellCatalog.All.Select(spell => spell.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(60));
            Assert.That(FireSpellCatalog.All.Count(spell => spell.CombatAffinity == FireCombatAffinity.MeleeOnly), Is.EqualTo(20));
            Assert.That(FireSpellCatalog.All.Count(spell => spell.CombatAffinity == FireCombatAffinity.WeaponUniversal), Is.EqualTo(20));
            Assert.That(FireSpellCatalog.All.Count(spell => spell.CombatAffinity == FireCombatAffinity.RangedSpell), Is.EqualTo(20));
            Assert.That(FireSpellCatalog.All, Has.All.Matches<FireSpellDefinition>(spell =>
                !string.IsNullOrWhiteSpace(spell.DisplayName) && spell.ActionPointCost >= 1 && spell.ManaCost >= 2 &&
                spell.Rules.Count > 0 && spell.PresentationModules.Count > 0 &&
                Enum.IsDefined(typeof(FireDeliveryMode), spell.DeliveryMode) &&
                Enum.IsDefined(typeof(FireWeaponRequirement), spell.WeaponRequirement) &&
                Enum.IsDefined(typeof(FireTriggerWindow), spell.TriggerWindow) &&
                Enum.IsDefined(typeof(FireConsumptionRule), spell.ConsumptionRule)));
            Assert.That(FireSpellCatalog.Get("F-P-U05").DisplayName, Is.EqualTo("爆燃弹芯"));
        }

        [Test]
        public void Catalog_UsesOnlyExplicitReviewedExistingIconAndVfxPaths()
        {
            Assert.That(FireSpellCatalog.All.Select(spell => spell.IconPath).Distinct(StringComparer.Ordinal).Count(), Is.LessThanOrEqualTo(50));
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                Assert.That(Resources.Load<Sprite>(spell.IconPath), Is.Not.Null, spell.Id + " icon => " + spell.IconPath);
                foreach (string module in spell.PresentationModules)
                    Assert.That(Resources.Load<Sprite>($"Art/FormalVfx32/{module}/frame_00"), Is.Not.Null, spell.Id + " => " + module);
            }
        }

        [Test]
        public void EverySpell_HasLegalIllegalAndDeterministicPreparedCases()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                FireSpellTrainingRangeCase first = (FireSpellTrainingRangeCase)provider.Prepare(spell.Id);
                FireSpellTrainingRangeCase second = (FireSpellTrainingRangeCase)provider.Prepare(spell.Id);
                Assert.That(first.Preview().CanCommit, Is.True, spell.Id + ": " + first.Preview().Summary);
                Assert.That(provider.PrepareIllegal(spell.Id).Preview().CanCommit, Is.False, spell.Id + " illegal");
                Assert.That(first.Execute().Signature(), Is.EqualTo(second.Execute().Signature()), spell.Id + " deterministic cast");
            }
        }

        [Test]
        public void TriggerWindows_UseWeaponMovementReactionAndMitigationPathsWithoutIdDispatch()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            foreach (FireSpellDefinition spell in FireSpellCatalog.All.Where(value => value.TriggerWindow != FireTriggerWindow.Immediate))
            {
                FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare(spell.Id);
                prepared.Execute();
                FireBattleState battle = prepared.Battle; UnitState hero = battle.Combat.GetUnit("hero");
                UnitState enemy = battle.Combat.GetUnit("range_normal");
                if (spell.TriggerWindow == FireTriggerWindow.NextLegalWeaponAttack || spell.TriggerWindow == FireTriggerWindow.AfterNextWeaponAttack)
                {
                    if (spell.Id == "F-P-U04")
                    {
                        GridPosition cell = new GridPosition(4, 3); battle.Combat.Map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 24 });
                        Assert.That(FireSpellEngine.TriggerWeaponAttackAt(battle, hero.Id, cell), Is.Not.Empty, spell.Id);
                    }
                    else Assert.That(FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, enemy.Id, new GridPosition(3, 3)), Is.Not.Empty, spell.Id);
                }
                else if (spell.TriggerWindow == FireTriggerWindow.FirstAdjacentAttack)
                    Assert.That(FireSpellEngine.TriggerIncomingAdjacentAttack(battle, enemy.Id, hero.Id), Is.Not.Empty, spell.Id);
                else if (spell.TriggerWindow == FireTriggerWindow.FirstMarkedTargetMove)
                {
                    GridPosition previous = enemy.Position; MoveUnitTo(battle.Combat, enemy, new GridPosition(5, 4));
                    Assert.That(FireSpellEngine.TriggerMarkedTargetMove(battle, enemy.Id, previous), Is.Not.Empty, spell.Id);
                }
                else if (spell.TriggerWindow == FireTriggerWindow.FirstEnemyEntry)
                {
                    MoveUnitTo(battle.Combat, enemy, prepared.RecommendedCell);
                    Assert.That(FireSpellEngine.TriggerEnemyEntry(battle, enemy.Id), Is.Not.Empty, spell.Id);
                }
                else if (spell.TriggerWindow == FireTriggerWindow.UntilNextAction)
                {
                    int reduced = FireSpellEngine.ReduceIncomingDamage(battle, hero.Id, "range_armored", 20);
                    Assert.That(reduced, Is.LessThanOrEqualTo(20), spell.Id);
                }
            }
        }

        [Test]
        public void WeaponAttackPipeline_AppliesAttachmentAndConsumesItOnTheRealAttackPath()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U16");
            prepared.Execute();

            FireWeaponAttackResolution resolution = FireSpellEngine.ResolveWeaponAttack(prepared.Battle, "hero", "range_normal");

            Assert.That(resolution.WeaponExecution.Results, Is.Not.Empty);
            Assert.That(resolution.TriggerExecutions.Count, Is.EqualTo(1));
            Assert.That(resolution.TriggerExecutions[0].Steps.Any(step => step.Kind == FireRuleKind.WeaponDamage), Is.True);
            Assert.That(prepared.Battle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-U16"), Is.False);
        }

        [Test]
        public void WeaponAttackPipeline_AppliesPreArmorStanceReduction()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare("F-P-M11");
            prepared.Execute();
            CombatResolver.BeginTurn(prepared.Battle.Combat, "range_armored");

            FireWeaponAttackResolution resolution = FireSpellEngine.ResolveWeaponAttack(prepared.Battle,
                "range_armored", "hero");

            Assert.That(resolution.IncomingDamageReduction, Is.GreaterThan(0));
        }

        private static void MoveUnitTo(CombatState combat, UnitState unit, GridPosition destination)
        {
            while (unit.Position != destination)
            {
                CombatResolver.BeginTurn(combat, unit.Id);
                int dx = destination.X - unit.Position.X;
                int dy = destination.Y - unit.Position.Y;
                GridPosition step = Math.Abs(dx) >= Math.Abs(dy)
                    ? new GridPosition(unit.Position.X + Math.Sign(dx), unit.Position.Y)
                    : new GridPosition(unit.Position.X, unit.Position.Y + Math.Sign(dy));
                Facing facing = dx > 0 ? Facing.East : dx < 0 ? Facing.West : dy > 0 ? Facing.North : Facing.South;
                CombatResolver.Resolve(combat, CombatCommand.Move(unit.Id, step, facing));
            }
        }

        [Test]
        public void WeaponContracts_RejectMeleeOnRifleAndAllowUniversalOnBothWeaponClasses()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard(); CombatResolver.BeginTurn(combat, "hero");
            UnitState hero = combat.GetUnit("hero"); FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition melee = FireSpellCatalog.Get("F-P-M01"), universal = FireSpellCatalog.Get("F-P-U01");
            hero.Equip(CombatCatalog.Rifle, hero.OffHand, hero.SkillOne, hero.SkillTwo);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, melee, FireSpellTarget.Unit(hero.Id)).Failures, Does.Contain("武器要求不符"));
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, universal, FireSpellTarget.Unit(hero.Id)).CanCommit, Is.True);
            hero.Equip(CombatCatalog.Hammer, hero.OffHand, hero.SkillOne, hero.SkillTwo);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, melee, FireSpellTarget.Unit(hero.Id)).CanCommit, Is.True);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, universal, FireSpellTarget.Unit(hero.Id)).CanCommit, Is.True);
        }

        [Test]
        public void RewardPool_MakesAllSixtyReachableAndCanFilterForCurrentWeapon()
        {
            HashSet<string> reachable = new HashSet<string>(StringComparer.Ordinal);
            for (int seed = 0; seed < 4000 && reachable.Count < 60; seed++)
            {
                foreach (FireSpellDefinition spell in FireSpellRewardPool.RollPersonalChoices(seed, seed % 17, RogueliteMapNodeType.Combat, Array.Empty<string>())) reachable.Add(spell.Id);
                foreach (FireSpellDefinition spell in FireSpellRewardPool.RollPersonalChoices(seed, seed % 17, RogueliteMapNodeType.Elite, Array.Empty<string>())) reachable.Add(spell.Id);
            }
            Assert.That(reachable, Is.EquivalentTo(FireSpellCatalog.All.Select(spell => spell.Id)));
            Assert.That(FireSpellRewardPool.RollPersonalChoices(17, 2, RogueliteMapNodeType.Combat, Array.Empty<string>(), CombatCatalog.Rifle),
                Has.All.Matches<FireSpellDefinition>(spell => spell.CombatAffinity != FireCombatAffinity.MeleeOnly));
        }
    }
}
