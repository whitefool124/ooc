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
                !string.IsNullOrWhiteSpace(spell.DisplayName) && spell.ActionPointCost >= 1 && spell.ManaCost >= 0 &&
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
                GridPosition origin = hero.Position;
                if (spell.TriggerWindow == FireTriggerWindow.NextLegalWeaponAttack || spell.TriggerWindow == FireTriggerWindow.AfterNextWeaponAttack)
                {
                    if (spell.Id == "F-P-U04")
                    {
                        GridPosition cell = new GridPosition(4, 3); battle.Combat.Map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 24 });
                        AssertTriggerOrigin(FireSpellEngine.TriggerWeaponAttackAt(battle, hero.Id, cell), hero.Id, origin, spell.Id);
                    }
                    else AssertTriggerOrigin(FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, enemy.Id, new GridPosition(3, 3)), hero.Id, origin, spell.Id);
                }
                else if (spell.TriggerWindow == FireTriggerWindow.FirstAdjacentAttack)
                    AssertTriggerOrigin(FireSpellEngine.TriggerIncomingAdjacentAttack(battle, enemy.Id, hero.Id), hero.Id, origin, spell.Id);
                else if (spell.TriggerWindow == FireTriggerWindow.FirstMarkedTargetMove)
                {
                    GridPosition previous = enemy.Position; MoveUnitTo(battle.Combat, enemy, new GridPosition(5, 4));
                    AssertTriggerOrigin(FireSpellEngine.TriggerMarkedTargetMove(battle, enemy.Id, previous), hero.Id, origin, spell.Id);
                }
                else if (spell.TriggerWindow == FireTriggerWindow.FirstEnemyEntry)
                {
                    MoveUnitTo(battle.Combat, enemy, prepared.RecommendedCell);
                    AssertTriggerOrigin(FireSpellEngine.TriggerEnemyEntry(battle, enemy.Id), hero.Id, origin, spell.Id);
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

        private static void AssertTriggerOrigin(IReadOnlyList<FireSpellExecution> executions,
            string sourceId, GridPosition origin, string spellId)
        {
            Assert.That(executions, Is.Not.Empty, spellId);
            foreach (FireSpellExecution execution in executions)
            {
                Assert.That(execution.SourceUnitId, Is.EqualTo(sourceId), spellId);
                Assert.That(execution.SourcePosition, Is.EqualTo(origin), spellId);
                Assert.That(execution.IsTriggered, Is.True, spellId);
            }
        }

        [Test]
        public void EverySpell_RecordsCastOriginBeforeAnyMovement()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            var targetField = typeof(FireSpellTrainingRangeCase).GetField("target",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(targetField, Is.Not.Null);
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                var prepared = (FireSpellTrainingRangeCase)provider.Prepare(spell.Id);
                UnitState source = prepared.Battle.Combat.GetUnit("hero");
                GridPosition origin = source.Position;
                var target = (FireSpellTarget)targetField.GetValue(prepared);
                FireSpellExecution execution = FireSpellEngine.Execute(prepared.Battle, source.Id, spell, target);
                Assert.That(execution.SourceUnitId, Is.EqualTo(source.Id), spell.Id);
                Assert.That(execution.SourcePosition, Is.EqualTo(origin), spell.Id);
                Assert.That(execution.IsTriggered, Is.False, spell.Id);
            }
        }

        [Test]
        public void WeaponAttackPipeline_GrantsPreHitShieldWithoutFixedReduction()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare("F-P-M11");
            prepared.Execute();
            CombatResolver.BeginTurn(prepared.Battle.Combat, "range_armored");

            FireWeaponAttackResolution resolution = FireSpellEngine.ResolveWeaponAttack(prepared.Battle,
                "range_armored", "hero");

            Assert.That(resolution.IncomingDamageReduction, Is.Zero);
            Assert.That(prepared.Battle.Combat.GetUnit("hero").Shield, Is.GreaterThan(0));
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
                CombatResolver.Resolve(combat, CombatCommand.Move(unit.Id, step));
            }
        }

        [Test]
        public void WeaponContracts_RejectMeleeOnRifleAndAllowUniversalOnBothWeaponClasses()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard(); CombatResolver.BeginTurn(combat, "hero");
            UnitState hero = combat.GetUnit("hero"); FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition melee = FireSpellCatalog.Get("F-P-M01"), universal = FireSpellCatalog.Get("F-P-U01");
            FireSpellTarget dashTarget = FireSpellTarget.At(new GridPosition(2, 4), CardinalDirection.South);
            hero.Equip(CombatCatalog.Rifle, hero.OffHand, hero.SkillOne, hero.SkillTwo);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, melee, FireSpellTarget.Unit(hero.Id)).Failures, Does.Contain("武器要求不符"));
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, universal, dashTarget).CanCommit, Is.True);
            hero.Equip(CombatCatalog.Hammer, hero.OffHand, hero.SkillOne, hero.SkillTwo);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, melee, FireSpellTarget.Unit(hero.Id)).CanCommit, Is.True);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, universal, dashTarget).CanCommit, Is.True);
        }

        [Test]
        public void MeltBarrierCalibration_MarksDamagesAndPaysDestructionReward()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, "hero");
            UnitState hero = combat.GetUnit("hero"), enemy = combat.GetUnit("range_normal");
            enemy.ConfigureVitality(8);
            FireBattleState battle = new FireBattleState(combat);

            FireSpellExecution result = FireSpellEngine.Execute(battle, hero.Id,
                FireSpellCatalog.Get("F-P-U04"), FireSpellTarget.Unit(enemy.Id));

            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(hero.Mana, Is.EqualTo(hero.MaxMana));
            Assert.That(hero.Shield, Is.EqualTo(4));
            Assert.That(battle.MeltBarrierMarks, Is.Empty);
            Assert.That(result.Steps.Any(step => step.Kind == FireRuleKind.ApplyMeltBarrierMark), Is.True);
        }

        [Test]
        public void OffLineDash_ReservesExactlyOneActionForNextOwnTurnWhenEscapingThreat()
        {
            GridMap map = new GridMap(8, 8);
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 4)); hero.ConfigureMana(20);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 4));
            SkillDefinition inactive = new SkillDefinition("inactive", "无", DamageType.Physical, 0, 0, 0, 0);
            enemy.Equip(CombatCatalog.Hammer, enemy.OffHand, inactive, inactive);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);

            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U01"),
                FireSpellTarget.At(new GridPosition(3, 1), CardinalDirection.South));

            Assert.That(hero.Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(battle.HasReservedNextTurnAction(hero.Id), Is.True);
            CombatResolver.BeginTurn(combat, hero.Id); battle.BeginUnitTurn(hero.Id);
            Assert.That(hero.ActionPoints, Is.EqualTo(4));
            Assert.That(battle.HasReservedNextTurnAction(hero.Id), Is.False);
        }

        [Test]
        public void FurnacePressureStep_DamagesObjectsAndPushesLivingUnitsFromLandingCell()
        {
            GridMap map = new GridMap(8, 8);
            GridPosition landing = new GridPosition(3, 2), objectCell = new GridPosition(2, 2);
            map.SetTile(objectCell, new TileState { Cover = CoverType.Light, Durability = 8 });
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 4)); hero.ConfigureMana(20);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 2)); enemy.ConfigureVitality(20);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);

            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U18"),
                FireSpellTarget.At(landing, CardinalDirection.South));

            Assert.That(hero.Position, Is.EqualTo(landing));
            Assert.That(map.GetTile(objectCell).Durability, Is.Zero);
            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(5, 2)));
            Assert.That(enemy.Health + enemy.Shield, Is.LessThan(22));
        }

        [Test]
        public void MeltBarrierBurst_UsesUnifiedCenterAndNeighborDamageForUnitsAndObjects()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, "hero");
            GridPosition center = TrainingRangeScenarioFactory.ObjectTargetCell;
            combat.Map.SetTile(center, new TileState { Cover = CoverType.Light, Durability = 16 });
            FireBattleState battle = new FireBattleState(combat);
            UnitState neighbor = combat.GetUnit("range_armored");
            int before = neighbor.Health + neighbor.Shield;

            FireSpellEngine.Execute(battle, "hero", FireSpellCatalog.Get("F-P-R19"), FireSpellTarget.At(center, CardinalDirection.East));

            Assert.That(combat.Map.GetTile(center).Durability, Is.Zero);
            Assert.That(before - (neighbor.Health + neighbor.Shield), Is.EqualTo(8));
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
