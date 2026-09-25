using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FireSpellCatalogTests
    {
        [Test]
        public void CurrentCardCopy_DoesNotGrantUndeclaredPursuitDamageOrFiregroundStatuses()
        {
            Assert.That(FireSpellCatalog.Get("F-P-M05").Rules.Select(rule => rule.Kind),
                Is.EquivalentTo(new[] { FireRuleKind.MoveSource }));
            Assert.That(FireSpellCatalog.Get("F-P-R13").Rules.Select(rule => rule.Kind),
                Is.EquivalentTo(new[] { FireRuleKind.CreateFireground }));
            Assert.That(FireSpellCatalog.Get("F-P-R20").Rules.Select(rule => rule.Kind),
                Is.EquivalentTo(new[] { FireRuleKind.Damage, FireRuleKind.CreateFireground }));
        }

        [Test]
        public void Catalog_ContainsOnlyReviewedImplementedEntriesDuringTheEightySpellMigration()
        {
            string[] expected = new[] { "M", "U", "R" }.SelectMany(prefix =>
                Enumerable.Range(1, 20).Select(index => $"F-P-{prefix}{index:00}").Concat(prefix == "M"
                    ? new[] { "F-P-M21", "F-P-M22", "F-P-M23", "F-P-M24", "F-P-M25", "F-P-M26" } : prefix == "U" ? new[] { "F-P-U21", "F-P-U22", "F-P-U23", "F-P-U24", "F-P-U25", "F-P-U26", "F-P-U27", "F-P-U28" } : new[] { "F-P-R21", "F-P-R22", "F-P-R23", "F-P-R24", "F-P-R25", "F-P-R26" })).ToArray();
            Assert.That(FireSpellCatalog.Version, Is.EqualTo("fire-personal-spells-v0.4-reviewed-migration"));
            Assert.That(FireSpellCatalog.All.Count, Is.EqualTo(80));
            Assert.That(FireSpellCatalog.All.Select(spell => spell.Id), Is.EqualTo(expected));
            Assert.That(FireSpellCatalog.All.Select(spell => spell.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(80));
            Assert.That(FireSpellCatalog.All.Count(spell => spell.CombatAffinity == FireCombatAffinity.MeleeOnly), Is.EqualTo(26));
            Assert.That(FireSpellCatalog.All.Count(spell => spell.CombatAffinity == FireCombatAffinity.WeaponUniversal), Is.EqualTo(28));
            Assert.That(FireSpellCatalog.All.Count(spell => spell.CombatAffinity == FireCombatAffinity.RangedSpell), Is.EqualTo(26));
            Assert.That(FireSpellCatalog.PersonalSpellCount(FireSpellRarity.Common), Is.EqualTo(40));
            Assert.That(FireSpellCatalog.PersonalSpellCount(FireSpellRarity.Uncommon), Is.EqualTo(28));
            Assert.That(FireSpellCatalog.PersonalSpellCount(FireSpellRarity.Rare), Is.EqualTo(12));
            Assert.That(FireSpellCatalog.All, Has.All.Matches<FireSpellDefinition>(spell =>
                !string.IsNullOrWhiteSpace(spell.DisplayName) && spell.ActionPointCost >= 0 && spell.ManaCost >= 0 &&
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
                    if (spell.Id == "F-P-U19" || spell.Id == "F-P-U20") enemy.ApplyStatus(StatusType.Burning, 2, 8);
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

        [Test]
        public void BurningAndFiregroundCashingSpells_ChargeTempoAndConsumeTheirSource()
        {
            // 总案 3.5.6.5：这三张收租术式是首批 0 行动点／0 魔力／0 冷却术式，必须消耗已存在的燃烧或火场，
            // 靠"消费掉火源"而不是费用来防止无限循环。
            string[] converters = { "F-P-M17", "F-P-U11", "F-P-R17" };
            foreach (string id in converters)
            {
                FireSpellDefinition spell = FireSpellCatalog.Get(id);
                Assert.That(spell.ActionPointCost, Is.Zero, id);
                Assert.That(spell.ManaCost, Is.Zero, id);
                Assert.That(spell.Cooldown, Is.Zero, id);
                Assert.That(spell.Rules.Any(rule => rule.Kind == FireRuleKind.ConsumeBurning || rule.Kind == FireRuleKind.ConsumeFireground), Is.True, id);
            }
            Assert.That(FireSpellCatalog.Get("F-P-M20").ActionPointCost, Is.EqualTo(2));
            Assert.That(FireSpellCatalog.Get("F-P-M20").Rules.Any(rule => rule.Kind == FireRuleKind.ConsumeBurning), Is.False);
            Assert.That(FireSpellCatalog.Get("F-P-U20").Rules.Any(rule => rule.Kind == FireRuleKind.ConsumeBurning), Is.False);
            Assert.That(FireSpellCatalog.Get("F-P-R01").ManaCost, Is.EqualTo(1));
        }

        [Test]
        public void FiregroundSpells_UseSharedBaseDamageAndResolveBothSidesStatusesDynamically()
        {
            // 总案 3.5.6.1／3.5.1.2：所有火场共用8点基础伤害，双方状态在触发时动态结算。
            foreach (string id in new[] { "F-P-M03", "F-P-R11", "F-P-R12", "F-P-R13", "F-P-R14", "F-P-R15", "F-P-R20", "F-P-U17" })
                Assert.That(FireSpellCatalog.Get(id).Rules.Where(rule => rule.Kind == FireRuleKind.CreateFireground).All(rule => rule.Amount == 8), Is.True, id);
            Assert.That(FireSpellCatalog.Get("F-P-U19").Rules.Any(rule => rule.Kind == FireRuleKind.CreateFireground), Is.False);

            GridMap map = new GridMap(6, 4);
            UnitState enemySource = new UnitState("enemy-source", false, new GridPosition(0, 0));
            UnitState targetA = new UnitState("target-a", true, new GridPosition(2, 1));
            UnitState targetB = new UnitState("target-b", true, new GridPosition(4, 1));
            enemySource.ApplyStatus(StatusType.FiregroundBoost, 3, 2);
            targetA.ApplyStatus(StatusType.FiregroundVulnerable, 3, 3);
            CombatState combat = new CombatState(map, new[] { enemySource, targetA, targetB }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            FireBattleState battle = new FireBattleState(combat);
            GridPosition cellA = targetA.Position;
            GridPosition cellB = targetB.Position;
            battle.CreateOrRefreshFireground(cellA, 12, 3, "F-P-R13", enemySource.Id);
            battle.CreateOrRefreshFireground(cellB, 8, 3, "F-P-R12", enemySource.Id);
            Assert.That(battle.Firegrounds[cellA].Damage, Is.EqualTo(FiregroundState.BaseDamage));
            Assert.That(battle.Firegrounds[cellB].Damage, Is.EqualTo(FiregroundState.BaseDamage));

            Assert.That(battle.ResolveEntry(targetA, new GridPosition(1, 1)), Is.EqualTo(13));
            Assert.That(battle.ResolveEntry(targetB, new GridPosition(3, 1)), Is.EqualTo(10));
        }

        [Test]
        public void FirstHitFracture_StopsAtFrontObject_RefluxesOnce_AndExpiresAtCastersNextTurnEnd()
        {
            GridMap map = new GridMap(6, 2);
            GridPosition coverCell = new GridPosition(2, 0);
            map.SetTile(coverCell, new TileState { Cover = CoverType.Light, Durability = 12 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 0));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Rifle, null, null);
            enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R21");
            FireSpellPreview preview = FireSpellEngine.Preview(battle, hero.Id, spell,
                FireSpellTarget.At(new GridPosition(4, 0), CardinalDirection.East));
            Assert.That(preview.CanCommit, Is.True, string.Join("；", preview.Failures));
            Assert.That(preview.Cells.Last(), Is.EqualTo(coverCell));
            Assert.That(preview.UnitIds, Is.Empty, "前方物块必须拦住后方单位");

            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.At(new GridPosition(4, 0), CardinalDirection.East));
            Assert.That(map.GetTile(coverCell).Durability, Is.EqualTo(12), "命中物件只施加裂痕，不造成直接伤害。");
            Assert.That(battle.IsFractured(coverCell), Is.True);
            int mana = hero.Mana, shield = hero.Shield;
            map.GetTile(coverCell).Durability = 0;
            battle.ResolveMarkedDestructions();
            battle.ResolveMarkedDestructions();
            Assert.That(hero.Mana, Is.EqualTo(mana + 2));
            Assert.That(hero.Shield, Is.EqualTo(shield + 4));
            Assert.That(battle.IsFractured(coverCell), Is.False);

            map.GetTile(coverCell).Durability = 12;
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U21"),
                FireSpellTarget.At(new GridPosition(4, 0), CardinalDirection.East));
            battle.EndUnitTurn(hero.Id);
            Assert.That(battle.IsFractured(coverCell), Is.True);
            battle.EndUnitTurn(hero.Id);
            Assert.That(battle.IsFractured(coverCell), Is.False);
        }

        [Test]
        public void StructureSurvey_MarksAllVisibleObjectsAndOnlyFirstUnit()
        {
            GridMap map = new GridMap(6, 2);
            GridPosition firstCover = new GridPosition(1, 0);
            GridPosition secondCover = new GridPosition(2, 0);
            map.SetTile(firstCover, new TileState { Cover = CoverType.Light, Durability = 12 });
            map.SetTile(secondCover, new TileState { Cover = CoverType.Light, Durability = 12 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState ally = new UnitState("ally", true, new GridPosition(3, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 0));
            hero.ConfigureMana(99);
            CombatState combat = new CombatState(map, new[] { hero, ally, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);

            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U21"),
                FireSpellTarget.At(new GridPosition(4, 0), CardinalDirection.East));

            Assert.That(battle.IsFractured(firstCover), Is.True);
            Assert.That(battle.IsFractured(secondCover), Is.True);
            Assert.That(ally.HasStatus(StatusType.BreakStance), Is.True);
            Assert.That(enemy.HasStatus(StatusType.BreakStance), Is.False);
        }

        [Test]
        public void FractureShield_RequiresAlliedDestruction_TriggersOnce_AndExpiresNextTurn()
        {
            GridMap map = new GridMap(6, 2);
            GridPosition coverCell = new GridPosition(2, 0);
            map.SetTile(coverCell, new TileState { Cover = CoverType.Light, Durability = 12 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 0));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Rifle, null, null);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U21"),
                FireSpellTarget.At(new GridPosition(3, 0), CardinalDirection.East));
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-R24"),
                FireSpellTarget.Unit(hero.Id));
            Assert.That(battle.HasFractureShieldArmed(hero.Id), Is.True);
            int shield = hero.Shield;
            map.GetTile(coverCell).Durability = 0;
            battle.ResolveMarkedDestructions();
            Assert.That(hero.Shield, Is.EqualTo(shield + 4), "环境自毁仅触发标准回流");
            Assert.That(battle.HasFractureShieldArmed(hero.Id), Is.True);
            GridMap alliedMap = new GridMap(6, 2);
            alliedMap.SetTile(coverCell, new TileState { Cover = CoverType.Light, Durability = 12 });
            UnitState alliedHero = new UnitState("alliedHero", true, new GridPosition(0, 0));
            UnitState alliedEnemy = new UnitState("alliedEnemy", false, new GridPosition(4, 0));
            alliedHero.ConfigureVitality(99); alliedHero.ConfigureMana(99); alliedHero.Equip(CombatCatalog.Rifle, null, null);
            CombatState alliedCombat = new CombatState(alliedMap, new[] { alliedHero, alliedEnemy }, Array.Empty<CombatObjective>());
            alliedCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(alliedCombat, alliedHero.Id);
            FireBattleState alliedBattle = new FireBattleState(alliedCombat);
            FireSpellEngine.Execute(alliedBattle, alliedHero.Id, FireSpellCatalog.Get("F-P-U21"),
                FireSpellTarget.At(new GridPosition(3, 0), CardinalDirection.East));
            FireSpellEngine.Execute(alliedBattle, alliedHero.Id, FireSpellCatalog.Get("F-P-R24"),
                FireSpellTarget.Unit(alliedHero.Id));
            alliedMap.GetTile(coverCell).Durability = 0;
            alliedBattle.ResolveMarkedDestructions(alliedHero.Id);
            alliedBattle.ResolveMarkedDestructions(alliedHero.Id);
            Assert.That(alliedHero.Shield, Is.EqualTo(8), "标准回流 4 加护持 4，只结算一次");
            Assert.That(alliedBattle.HasFractureShieldArmed(alliedHero.Id), Is.False);
            battle.BeginUnitTurn(hero.Id);
            Assert.That(battle.HasFractureShieldArmed(hero.Id), Is.False);
        }

        [Test]
        public void CounterStance_WaitsForCompletedCharge_ThenCountersOneAdjacentAttack()
        {
            GridMap map = new GridMap(5, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(99); enemy.Equip(CombatCatalog.Hammer, null, null);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-M26"), FireSpellTarget.Unit(hero.Id));
            Assert.That(battle.PendingEffects.Single(effect => effect.Spell.Id == "F-P-M26").Stage, Is.Zero);
            Assert.That(FireSpellEngine.TriggerIncomingAdjacentAttack(battle, enemy.Id, hero.Id), Is.Empty);
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-M02"),
                FireSpellTarget.At(new GridPosition(1, 0), CardinalDirection.East));
            Assert.That(battle.PendingEffects.Single(effect => effect.Spell.Id == "F-P-M26").Stage, Is.EqualTo(1));
            int before = enemy.Health;
            IReadOnlyList<FireSpellExecution> first = FireSpellEngine.TriggerIncomingAdjacentAttack(battle, enemy.Id, hero.Id);
            Assert.That(first.Single().Steps.Any(step => step.Kind == FireRuleKind.WeaponDamage && step.Applied > 0), Is.True);
            Assert.That(enemy.Health, Is.LessThan(before));
            Assert.That(FireSpellEngine.TriggerIncomingAdjacentAttack(battle, enemy.Id, hero.Id), Is.Empty);
        }

        [Test]
        public void DestroyedObjectJournal_RecordsSourceOnce_Clones_AndResetsAtNextHeroTurn()
        {
            GridMap map = new GridMap(4, 2);
            GridPosition cell = new GridPosition(2, 0);
            map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 8 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 0));
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            FireBattleState battle = new FireBattleState(combat);
            map.GetTile(cell).Durability = 0;
            battle.ResolveMarkedDestructions(hero.Id);
            battle.ResolveMarkedDestructions(hero.Id);
            Assert.That(battle.DestroyedObjectsThisHeroTurn.Count, Is.EqualTo(1));
            Assert.That(battle.DestroyedObjectsThisHeroTurn[0].Cell, Is.EqualTo(cell));
            Assert.That(battle.DestroyedObjectsThisHeroTurn[0].DestroyerUnitId, Is.EqualTo(hero.Id));
            Assert.That(battle.Clone().DestroyedObjectsThisHeroTurn.Count, Is.EqualTo(1));
            battle.BeginUnitTurn(hero.Id);
            Assert.That(battle.DestroyedObjectsThisHeroTurn, Is.Empty);
        }

        [Test]
        public void SalvageCover_RequiresAlliedFreshMaterial_AndSpendsItOnlyOnce()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U22");
            FireBattleState battle = prepared.Battle;
            GridPosition destination = prepared.RecommendedCell;
            Assert.That(prepared.Preview().CanCommit, Is.True);
            FireSpellExecution result = (FireSpellExecution)prepared.Execute().NativeResult;
            Assert.That(result.Steps.Any(step => step.Kind == FireRuleKind.CreateLightCover && step.Cell == destination), Is.True);
            Assert.That(battle.Combat.Map.GetTile(destination).Cover, Is.EqualTo(CoverType.Light));
            Assert.That(battle.Combat.Map.GetTile(destination).Durability, Is.EqualTo(8));
            Assert.That(battle.DestroyedObjectsThisHeroTurn.Single().MaterialSpent, Is.True);
            GridPosition neighbor = destination + new GridPosition(1, 0);
            Assert.That(FireSpellEngine.Preview(battle, "hero", FireSpellCatalog.Get("F-P-U22"), FireSpellTarget.At(neighbor, CardinalDirection.East)).CanCommit, Is.False);

            FireSpellTrainingRangeCase enemyMaterial = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U22");
            enemyMaterial.Battle.DestroyedObjectsThisHeroTurn.Single().SpendMaterial();
            Assert.That(enemyMaterial.Preview().CanCommit, Is.False);
        }

        [Test]
        public void BreachPierce_RequiresThisTurnBreach_AndHitsAdjacentEnemyOnce()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U24");
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-U24");
            Assert.That(prepared.Preview().CanCommit, Is.True, prepared.Preview().Summary);
            Assert.That(FireSpellEngine.Preview(prepared.Battle, "hero", spell,
                FireSpellTarget.At(prepared.RecommendedCell, CardinalDirection.South)).UnitIds, Does.Contain("range_normal"));
            FireSpellExecution result = (FireSpellExecution)prepared.Execute().NativeResult;
            Assert.That(result.Steps.Count(step => step.Kind == FireRuleKind.WeaponDamage && step.TargetId == "range_normal"), Is.EqualTo(1));
            Assert.That(prepared.Combat.GetUnit("hero").Position, Is.EqualTo(prepared.RecommendedCell));

            FireSpellTrainingRangeCase noBreach = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U24");
            noBreach.Battle.BeginUnitTurn("hero");
            Assert.That(FireSpellEngine.Preview(noBreach.Battle, "hero", spell,
                FireSpellTarget.At(noBreach.RecommendedCell, CardinalDirection.South)).CanCommit, Is.False);
        }

        [Test]
        public void BreachEntry_StopsBeforeFirstObject_ThenEntersOnlyWhenDestroyed()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U23");
            GridPosition objectCell = prepared.RecommendedCell;
            FireSpellPreview preview = FireSpellEngine.Preview(prepared.Battle, "hero", FireSpellCatalog.Get("F-P-U23"),
                FireSpellTarget.At(objectCell, CardinalDirection.East));
            Assert.That(preview.CanCommit, Is.True, string.Join(";", preview.Failures));
            Assert.That(preview.ProjectedDestroyedObjects, Does.Contain(objectCell));
            FireSpellExecution result = (FireSpellExecution)prepared.Execute().NativeResult;
            Assert.That(result.Steps.Any(step => step.Kind == FireRuleKind.AdvanceIntoBreach && step.Applied == 1), Is.True);
            Assert.That(prepared.Combat.GetUnit("hero").Position, Is.EqualTo(objectCell));

            FireSpellTrainingRangeCase sturdy = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U23");
            sturdy.Combat.Map.GetTile(sturdy.RecommendedCell).Durability = 20;
            FireSpellExecution second = (FireSpellExecution)sturdy.Execute().NativeResult;
            Assert.That(second.Steps.Any(step => step.Kind == FireRuleKind.AdvanceIntoBreach && step.Applied == 0), Is.True);
            Assert.That(sturdy.Combat.GetUnit("hero").Position.ManhattanDistance(sturdy.RecommendedCell), Is.EqualTo(1));
        }

        [Test]
        public void BreachEntry_DamagesTheFirstAllyNamedByTheCard()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState ally = new UnitState("ally", true, new GridPosition(2, 0));
            hero.ConfigureMana(99);
            hero.Equip(CombatCatalog.Rifle, null, null);
            CombatState combat = new CombatState(new GridMap(5, 2),
                new[] { hero, ally }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            int before = ally.Health + ally.Shield;

            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U23"),
                FireSpellTarget.At(ally.Position, CardinalDirection.East));

            Assert.That(hero.Position, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(ally.Health + ally.Shield, Is.EqualTo(before - 12));
        }

        [Test]
        public void SteadyBreath_ClearsExactlyTheChosenDebuff_OrTheSolePresentOne()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireBattleState battle = ((FireSpellTrainingRangeCase)provider.Prepare("F-P-U28")).Battle;
            UnitState hero = battle.Combat.GetUnit("hero");
            hero.ApplyStatus(StatusType.Slow, 2);
            FireBattleState shiftChoice = battle.Clone();
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-U28");
            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(hero.Id, CardinalDirection.East));
            Assert.That(hero.HasStatus(StatusType.Bound), Is.False);
            Assert.That(hero.HasStatus(StatusType.Slow), Is.True);

            UnitState shiftedHero = shiftChoice.Combat.GetUnit("hero");
            FireSpellEngine.Execute(shiftChoice, shiftedHero.Id, spell, FireSpellTarget.Unit(shiftedHero.Id, CardinalDirection.West));
            Assert.That(shiftedHero.HasStatus(StatusType.Bound), Is.True);
            Assert.That(shiftedHero.HasStatus(StatusType.Slow), Is.False);

            FireBattleState noStatus = ((FireSpellTrainingRangeCase)provider.Prepare("F-P-U28")).Battle;
            CombatEffectExecutor.Execute(noStatus.Combat, "hero", CombatEffect.ClearStatus("hero", StatusType.Bound));
            Assert.That(FireSpellEngine.Preview(noStatus, "hero", spell, FireSpellTarget.Unit("hero")).CanCommit, Is.False);
        }

        [Test]
        public void FoldedDash_PreviewsBothBends_AndMovesThroughTheChosenLegalRoute()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-M21");
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-M21");
            GridPosition endpoint = prepared.RecommendedCell;
            FireSpellPreview horizontal = FireSpellEngine.Preview(prepared.Battle, "hero", spell,
                FireSpellTarget.At(endpoint, CardinalDirection.East));
            FireSpellPreview vertical = FireSpellEngine.Preview(prepared.Battle, "hero", spell,
                FireSpellTarget.At(endpoint, CardinalDirection.North));
            Assert.That(horizontal.CanCommit && vertical.CanCommit, Is.True);
            Assert.That(horizontal.Cells[0], Is.Not.EqualTo(vertical.Cells[0]));
            Assert.That(horizontal.ReactionCells, Is.Not.Empty);
            prepared.Combat.Map.SetTile(horizontal.Cells[0], new TileState { Cover = CoverType.Heavy, Durability = 24 });
            Assert.That(FireSpellEngine.Preview(prepared.Battle, "hero", spell,
                FireSpellTarget.At(endpoint, CardinalDirection.East)).CanCommit, Is.False);
            FireSpellExecution result = FireSpellEngine.Execute(prepared.Battle, "hero", spell,
                FireSpellTarget.At(endpoint, CardinalDirection.North));
            Assert.That(result.Steps.Count(step => step.Kind == FireRuleKind.MoveSource && step.Detail == "folded_step"), Is.EqualTo(2));
            Assert.That(prepared.Combat.GetUnit("hero").Position, Is.EqualTo(endpoint));
        }

        [Test]
        public void StraightCharge_ResolvesIntermediateWaterBeforeTheFinalLanding()
        {
            GridMap map = new GridMap(5, 2);
            map.SetTile(new GridPosition(1, 0), new TileState { IsWater = true });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 0));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            hero.ApplyStatus(StatusType.Burning, 2, 8);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-M02"),
                FireSpellTarget.At(new GridPosition(2, 0), CardinalDirection.East));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(hero.HasStatus(StatusType.Burning), Is.False);
        }

        [Test]
        public void MomentumRetreat_ReturnsOneActualPathStepAfterChargeFinishes()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-M24");
            FireBattleState battle = prepared.Battle;
            UnitState hero = prepared.Combat.GetUnit("hero");
            GridPosition origin = hero.Position;
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-M24"), FireSpellTarget.Unit(hero.Id));
            Assert.That(battle.OptionalMoves, Is.Empty);
            MoveUnitTo(prepared.Combat, prepared.Combat.GetUnit("range_normal"), origin + new GridPosition(2, 0));
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-M02"),
                FireSpellTarget.At(origin + new GridPosition(1, 0), CardinalDirection.East));
            Assert.That(hero.Position, Is.EqualTo(origin));
            Assert.That(battle.OptionalMoves, Is.Empty);
        }

        [Test]
        public void ShatterReturn_MovesOnlyAfterOwnDestroyedObject()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-U26");
            FireBattleState battle = prepared.Battle;
            UnitState hero = prepared.Combat.GetUnit("hero");
            GridPosition cell = hero.Position + new GridPosition(0, -1);
            prepared.Combat.Map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 8 });
            battle.ResolveMarkedDestructions();
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U26"), FireSpellTarget.Unit(hero.Id));
            prepared.Combat.Map.GetTile(cell).Durability = 0;
            battle.ResolveMarkedDestructions("range_ally");
            Assert.That(battle.OptionalMoves, Is.Empty, "友方摧毁不触发自身附着");

            FireSpellTrainingRangeCase own = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-U26");
            FireBattleState ownBattle = own.Battle;
            UnitState ownHero = own.Combat.GetUnit("hero");
            GridPosition ownCell = ownHero.Position + new GridPosition(0, -1);
            own.Combat.Map.SetTile(ownCell, new TileState { Cover = CoverType.Light, Durability = 8 });
            ownBattle.ResolveMarkedDestructions();
            FireSpellEngine.Execute(ownBattle, ownHero.Id, FireSpellCatalog.Get("F-P-U26"), FireSpellTarget.Unit(ownHero.Id));
            own.Combat.Map.GetTile(ownCell).Durability = 0;
            ownBattle.ResolveMarkedDestructions(ownHero.Id);
            Assert.That(ownHero.Position, Is.EqualTo(ownCell));
            Assert.That(ownBattle.OptionalMoves, Is.Empty);
        }

        [Test]
        public void CoverRefraction_PreviewsTheCoverAndOtherSideFirstHit_AndStopsAtSecondCover()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)provider.Prepare("F-P-R25");
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R25");
            GridPosition anchor = prepared.RecommendedCell;
            FireSpellPreview preview = FireSpellEngine.Preview(prepared.Battle, "hero", spell,
                FireSpellTarget.At(anchor, CardinalDirection.South));
            Assert.That(preview.CanCommit, Is.True, string.Join(";", preview.Failures));
            Assert.That(preview.Cells, Is.EqualTo(new[] { anchor, new GridPosition(3, 1), new GridPosition(3, 0) }));
            Assert.That(preview.Destructibles.Contains(new GridPosition(3, 1)), Is.False);
            int beforeHealth = prepared.Combat.GetUnit("range_normal").Health;
            FireSpellEngine.Execute(prepared.Battle, "hero", spell, FireSpellTarget.At(anchor, CardinalDirection.South));
            Assert.That(prepared.Combat.GetUnit("range_normal").Health, Is.LessThan(beforeHealth));

            FireSpellTrainingRangeCase blocked = (FireSpellTrainingRangeCase)provider.Prepare("F-P-R25");
            MoveUnitTo(blocked.Combat, blocked.Combat.GetUnit("range_normal"), new GridPosition(4, 0));
            blocked.Combat.Map.SetTile(new GridPosition(3, 0), new TileState { Cover = CoverType.Heavy, Durability = 24 });
            FireSpellPreview secondCover = FireSpellEngine.Preview(blocked.Battle, "hero", spell,
                FireSpellTarget.At(anchor, CardinalDirection.South));
            Assert.That(secondCover.CanCommit, Is.False, "折射线被第二个重掩体截断，没有可攻击的单位。");
            Assert.That(secondCover.Cells.Last(), Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(blocked.Combat.Map.GetTile(new GridPosition(3, 0)).Durability, Is.EqualTo(24));

            FireSpellTrainingRangeCase permanent = (FireSpellTrainingRangeCase)provider.Prepare("F-P-R25");
            permanent.Combat.Map.GetTile(new GridPosition(3, 1)).IsPermanentWall = true;
            Assert.That(FireSpellEngine.Preview(permanent.Battle, "hero", spell,
                FireSpellTarget.At(anchor, CardinalDirection.South)).CanCommit, Is.False);
        }

        [Test]
        public void ReviewedPathAndBreachAdditions_ResolveTheirAdvertisedBattleGeometry()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            FireSpellTrainingRangeCase hop = (FireSpellTrainingRangeCase)provider.Prepare("F-P-M22");
            GridPosition hopDestination = hop.RecommendedCell;
            Assert.That(hop.Preview().CanCommit, Is.True);
            Assert.That(hop.Execute().NativeResult, Is.Not.Null);
            Assert.That(hop.Combat.GetUnit("hero").Position, Is.EqualTo(hopDestination),
                "越障跃步必须越过中间单位并落在后方空格。");

            FireSpellTrainingRangeCase dash = (FireSpellTrainingRangeCase)provider.Prepare("F-P-M23");
            FireSpellExecution dashResult = (FireSpellExecution)dash.Execute().NativeResult;
            Assert.That(dashResult.Steps.Any(step => step.Kind == FireRuleKind.MoveSource && step.Applied > 0), Is.True);
            Assert.That(dashResult.Steps.Any(step => step.Kind == FireRuleKind.WeaponDamage && step.TargetId == "range_normal"), Is.True);

            FireSpellTrainingRangeCase shove = (FireSpellTrainingRangeCase)provider.Prepare("F-P-M25");
            FireSpellExecution shoveResult = (FireSpellExecution)shove.Execute().NativeResult;
            Assert.That(shoveResult.Steps.Any(step => step.Kind == FireRuleKind.PushAllUnits && step.TargetId == "range_normal" && step.Applied == 1), Is.True);

            FireSpellTrainingRangeCase wedge = (FireSpellTrainingRangeCase)provider.Prepare("F-P-U25");
            wedge.Combat.Map.SetTile(new GridPosition(2, 2), new TileState { Cover = CoverType.Light, Durability = 30 });
            wedge.Execute();
            Assert.That(wedge.Combat.Map.GetTile(new GridPosition(2, 2)).Durability, Is.EqualTo(14),
                "震楔落步在落点相邻格对物件造成双倍耐久伤害。");

            FireSpellTrainingRangeCase cone = (FireSpellTrainingRangeCase)provider.Prepare("F-P-R22");
            cone.Combat.Map.SetTile(new GridPosition(5, 4), new TileState { Cover = CoverType.Light, Durability = 30 });
            cone.Execute();
            Assert.That(cone.Combat.Map.GetTile(new GridPosition(5, 4)).Durability, Is.EqualTo(14),
                "楔形震裂在锥形内对物件造成双倍耐久伤害。");
        }

        [Test]
        public void BufferVeil_ReducesExactlyTheNextForcedMoveAndIsVisibleUntilConsumed()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-U27");
            prepared.Execute();
            UnitState hero = prepared.Combat.GetUnit("hero");
            Assert.That(hero.Shield, Is.GreaterThanOrEqualTo(8));
            Assert.That(prepared.Combat.PassiveEffects.StatusBarEntriesFor(hero.Id)
                .Any(entry => entry.DisplayName == "缓冲护幕"), Is.True);
            GridPosition before = hero.Position;

            Assert.That(prepared.Combat.ResolveForcedMove(hero, new GridPosition(-1, 0), 2, "test"), Is.EqualTo(ForcedMoveResult.Moved));
            Assert.That(hero.Position, Is.EqualTo(before + new GridPosition(-1, 0)));
            Assert.That(prepared.Combat.PassiveEffects.StatusBarEntriesFor(hero.Id)
                .Any(entry => entry.DisplayName == "缓冲护幕"), Is.False);
        }

        [Test]
        public void CollapsingWave_PreviewsDestroyedObjectAndFinalSinglePushBeforeCommit()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-R23");
            UnitState enemy = prepared.Combat.GetUnit("range_normal");
            MoveUnitTo(prepared.Combat, enemy, new GridPosition(5, 5));
            GridPosition objectCell = new GridPosition(5, 4);
            FireSpellTarget target = FireSpellTarget.At(objectCell, CardinalDirection.East);
            FireSpellPreview preview = FireSpellEngine.Preview(prepared.Battle, "hero", FireSpellCatalog.Get("F-P-R23"), target);
            Assert.That(preview.CanCommit, Is.True, string.Join("；", preview.Failures));
            Assert.That(preview.ProjectedDestroyedObjects, Does.Contain(objectCell));
            Assert.That(preview.ProjectedPushedUnits[enemy.Id], Is.EqualTo(new GridPosition(5, 6)));
            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(5, 5)), "预览不能改变真实战局。");
            FireSpellExecution result = FireSpellEngine.Execute(prepared.Battle, "hero", FireSpellCatalog.Get("F-P-R23"), target);
            Assert.That(prepared.Combat.Map.GetTile(objectCell).IsDestroyed, Is.True);
            Assert.That(enemy.Position, Is.EqualTo(preview.ProjectedPushedUnits[enemy.Id]));
            Assert.That(result.Steps.Count(step => step.Kind == FireRuleKind.PushFromDestroyedObjects && step.TargetId == enemy.Id), Is.EqualTo(1));
        }

        [Test]
        public void SeveredLineBlast_StaysPublicAndResolvesAtFixedCellsEvenAfterCasterFalls()
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)new FireSpellTrainingRangeProvider().Prepare("F-P-R26");
            GridPosition center = prepared.RecommendedCell;
            UnitState enemy = prepared.Combat.GetUnit("range_normal");
            MoveUnitTo(prepared.Combat, enemy, center + new GridPosition(0, 1));
            int healthBefore = enemy.Health;
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R26");
            FireSpellTarget target = FireSpellTarget.At(center, CardinalDirection.East);
            FireSpellPreview preview = FireSpellEngine.Preview(prepared.Battle, "hero", spell, target);
            Assert.That(preview.CanCommit, Is.True, string.Join("；", preview.Failures));
            Assert.That(preview.Cells, Does.Contain(enemy.Position));
            FireSpellEngine.Execute(prepared.Battle, "hero", spell, target);
            Assert.That(enemy.Health, Is.EqualTo(healthBefore), "施放时仅放置公开标记。");
            Assert.That(prepared.Battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id && effect.MarkedCell == center), Is.True);
            UnitState hero = prepared.Combat.GetUnit("hero");
            CombatEffectExecutor.Execute(prepared.Combat, hero.Id, CombatEffect.DamageHealth(hero.Id, hero.Health));
            IReadOnlyList<FireSpellExecution> explosions = FireSpellEngine.TriggerCurrentTurnEnd(prepared.Battle, "hero");
            Assert.That(explosions.Count, Is.EqualTo(1));
            Assert.That(enemy.Health, Is.LessThan(healthBefore));
            Assert.That(prepared.Combat.Map.GetTile(center).IsDestroyed, Is.True);
            Assert.That(prepared.Battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id), Is.False);
        }

        [Test]
        public void MeltBarrierCalibration_OnlyTargetsEnemiesAsCardStates()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            GridPosition cell = TrainingRangeScenarioFactory.ObjectTargetCell;
            combat.Map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 40 });
            FireBattleState battle = new FireBattleState(combat);
            CombatResolver.BeginTurn(combat, "hero");
            FireSpellPreview preview = FireSpellEngine.Preview(battle, "hero", FireSpellCatalog.Get("F-P-U04"),
                FireSpellTarget.At(cell, CardinalDirection.East));
            Assert.That(preview.CanCommit, Is.False);
            Assert.That(combat.Map.GetTile(cell).Durability, Is.EqualTo(40));
        }

        [Test]
        public void BreakBarrierTag_DoublesObjectDurabilityDamage()
        {
            // 总案 3.5.6.1：带有破障的伤害使物块扣减双倍耐久。
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            GridPosition cell = TrainingRangeScenarioFactory.ObjectTargetCell;
            combat.Map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 40 });
            FireBattleState battle = new FireBattleState(combat);
            CombatResolver.BeginTurn(combat, "hero");
            FireSpellDefinition breach = new FireSpellDefinition("test_break_barrier", "测试破障",
                FireSpellRarity.Common, FireSpellGroup.Breach, FireCombatAffinity.RangedSpell,
                FireDeliveryMode.DetachedProjection, FireWeaponRequirement.None, FireTriggerWindow.Immediate,
                FireConsumptionRule.OnCast, 1, 2, 0, 0, 4, FireTargetKind.Hittable, FireSelectionShape.Single,
                1, true, false, new[] { new FireSpellRule(FireRuleKind.BreakBarrier), new FireSpellRule(FireRuleKind.Damage, 8) });
            FireSpellEngine.Execute(battle, "hero", breach, FireSpellTarget.At(cell, CardinalDirection.East));
            Assert.That(combat.Map.GetTile(cell).Durability, Is.EqualTo(24));
        }

        [Test]
        public void FirelineCoordination_ArmsOnlyAfterBurningHit_ThenBuffsOneAllyAttack()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState ally = new UnitState("ally", true, new GridPosition(0, 1));
            UnitState burning = new UnitState("burning", false, new GridPosition(2, 0));
            UnitState other = new UnitState("other", false, new GridPosition(2, 1));
            hero.ConfigureMana(99);
            hero.Equip(CombatCatalog.Rifle, null, null);
            ally.Equip(CombatCatalog.Rifle, null, null);
            burning.ApplyStatus(StatusType.Burning, 3, 8);
            CombatState combat = new CombatState(new GridMap(5, 3),
                new[] { hero, ally, burning, other }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-U19");

            FireSpellPreview preview = FireSpellEngine.Preview(battle, hero.Id, spell,
                FireSpellTarget.Unit(hero.Id));
            Assert.That(preview.CanCommit, Is.True);
            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(hero.Id));

            Assert.That(FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, other.Id), Is.Empty,
                "未燃烧目标不能触发协同。");
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id && effect.Stage == 0), Is.True);

            IReadOnlyList<FireSpellExecution> armed = FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, burning.Id);
            Assert.That(armed.Count, Is.EqualTo(1));
            Assert.That(armed[0].Steps.Any(step => step.Kind == FireRuleKind.ArmAllyNextAttack), Is.True);
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id && effect.Stage == 2), Is.True);
            Assert.That(battle.Firegrounds, Is.Empty);
            Assert.That(FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, burning.Id), Is.Empty,
                "施术者自己不能消费友方攻击加成。");

            int before = other.Health + other.Shield;
            IReadOnlyList<FireSpellExecution> triggers = FireSpellEngine.TriggerWeaponAttack(battle, ally.Id, other.Id);
            Assert.That(triggers.Count, Is.EqualTo(1));
            Assert.That(triggers[0].Steps.Any(step => step.Kind == FireRuleKind.Damage && step.Applied == 8), Is.True);
            Assert.That(other.Health + other.Shield, Is.EqualTo(before - 8));
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id), Is.False);
        }

        [Test]
        public void FirelineCoordination_AlsoBuffsTheNextAllySpellAttack()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState ally = new UnitState("ally", true, new GridPosition(0, 1));
            UnitState burning = new UnitState("burning", false, new GridPosition(2, 0));
            UnitState other = new UnitState("other", false, new GridPosition(2, 1));
            hero.ConfigureMana(99);
            ally.ConfigureMana(99);
            other.ConfigureVitality(99);
            hero.Equip(CombatCatalog.Rifle, null, null);
            burning.ApplyStatus(StatusType.Burning, 3, 8);
            CombatState combat = new CombatState(new GridMap(5, 3),
                new[] { hero, ally, burning, other }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U19"), FireSpellTarget.Unit(hero.Id));
            FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, burning.Id);
            CombatResolver.BeginTurn(combat, ally.Id);
            int before = other.Health + other.Shield;

            FireSpellExecution result = FireSpellEngine.Execute(battle, ally.Id, FireSpellCatalog.Get("F-P-R01"),
                FireSpellTarget.Unit(other.Id));

            Assert.That(result.Steps.Any(step => step.SpellId == "F-P-U19" && step.Detail == "ally_followup_spell" && step.Applied == 8), Is.True,
                string.Join(";", result.Steps.Select(step => step.SpellId + ":" + step.Kind + ":" + step.TargetId + ":" + step.Applied + ":" + step.Detail)));
            Assert.That(other.Health + other.Shield, Is.EqualTo(before - 20));
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-U19"), Is.False);
        }

        [Test]
        public void PressureCharge_AddsTwelveToOneWeaponHit()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            hero.ConfigureMana(99);
            hero.Equip(CombatCatalog.Rifle, null, null);
            enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(new GridMap(4, 2),
                new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U16"), FireSpellTarget.Unit(hero.Id));
            int before = enemy.Health + enemy.Shield;

            FireWeaponAttackResolution attack = FireSpellEngine.ResolveWeaponAttack(battle, hero.Id, enemy.Id);

            Assert.That(enemy.Health + enemy.Shield, Is.EqualTo(before - 16),
                "步枪基础4和术式加成12应在同一次武器攻击中结算。");
            Assert.That(attack.TriggerExecutions.Any(execution => execution.Steps.Any(step =>
                step.Detail == "included_in_base_weapon_damage")), Is.True);
        }

        [Test]
        public void BurstingCore_AddsFourToTheHitAndFourToAdjacentAlly()
        {
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState ally = new UnitState("ally", true, new GridPosition(2, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            hero.ConfigureMana(99);
            hero.Equip(CombatCatalog.Rifle, null, null);
            CombatState combat = new CombatState(new GridMap(5, 3),
                new[] { hero, ally, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellEngine.Execute(battle, hero.Id, FireSpellCatalog.Get("F-P-U05"), FireSpellTarget.Unit(hero.Id));
            int enemyBefore = enemy.Health + enemy.Shield;
            int allyBefore = ally.Health + ally.Shield;

            IReadOnlyList<FireSpellExecution> triggers = FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, enemy.Id);

            Assert.That(triggers.Single().Steps.Count(step => step.Kind == FireRuleKind.Damage && step.Applied == 4),
                Is.GreaterThanOrEqualTo(2));
            Assert.That(enemy.Health + enemy.Shield, Is.EqualTo(enemyBefore - 4));
            Assert.That(ally.Health + ally.Shield, Is.EqualTo(allyBefore - 4));
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
        public void HeatPulseBoost_SeparatesImmediateMovementFromPendingMeleeDamage()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, "hero");
            FireBattleState battle = new FireBattleState(combat);
            UnitState hero = combat.GetUnit("hero");
            UnitState enemy = combat.GetUnit("range_normal");
            hero.Equip(CombatCatalog.Hammer, null, null);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-M01");
            int movementBefore = hero.MovementRangeThisTurn;
            int healthBefore = enemy.Health;

            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(hero.Id));

            Assert.That(hero.MovementRangeThisTurn, Is.EqualTo(movementBefore + 2));
            Assert.That(enemy.Health, Is.EqualTo(healthBefore), "追加伤害不能在施放时提前结算。 ");
            Assert.That(battle.PendingEffects.Count(effect => effect.Spell.Id == spell.Id), Is.EqualTo(1));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Is.EqualTo("本次行动可多移动2格。你下一次近战武器攻击额外造成8点火焰伤害。"));

            FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, enemy.Id);
            Assert.That(enemy.Health, Is.LessThan(healthBefore));
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id), Is.False);
        }

        [Test]
        public void BlastPursuit_FollowsIntoVacatedCellAndPublishesEveryBlockingRule()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, "hero");
            FireBattleState battle = new FireBattleState(combat);
            UnitState hero = combat.GetUnit("hero");
            UnitState enemy = combat.GetUnit("range_normal");
            hero.Equip(CombatCatalog.Hammer, null, null);
            GridPosition vacated = enemy.Position;
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-M05");

            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(enemy.Id));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Is.EqualTo("标记相邻敌人。它首次主动移动后，你移至它离开的格子。"));
            MoveUnitTo(combat, enemy, new GridPosition(6, 4));
            FirePendingEffect pending = battle.PendingEffects.Single(effect => effect.Spell.Id == spell.Id);

            FirePursuitPreview preview = FireSpellEngine.PreviewMarkedTargetMove(battle, pending, vacated);
            IReadOnlyList<FireSpellExecution> triggered = FireSpellEngine.TriggerMarkedTargetMove(battle, enemy.Id, vacated);

            Assert.That(preview.WillMove, Is.True);
            Assert.That(preview.Destination, Is.EqualTo(vacated));
            Assert.That(hero.Position, Is.EqualTo(vacated));
            Assert.That(triggered.Single().Steps.Single(step => step.Kind == FireRuleKind.MoveSource).Detail,
                Is.EqualTo("pursuit_to_vacated_cell"));
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id), Is.False);

            CombatState blockedCombat = TrainingRangeScenarioFactory.CreateStandard();
            blockedCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(blockedCombat, "hero");
            FireBattleState blockedBattle = new FireBattleState(blockedCombat);
            UnitState blockedHero = blockedCombat.GetUnit("hero");
            UnitState blockedEnemy = blockedCombat.GetUnit("range_normal");
            blockedHero.Equip(CombatCatalog.Hammer, null, null);
            FireSpellEngine.Execute(blockedBattle, blockedHero.Id, spell, FireSpellTarget.Unit(blockedEnemy.Id));
            GridPosition blockedVacated = blockedEnemy.Position;
            MoveUnitTo(blockedCombat, blockedEnemy, new GridPosition(6, 4));
            pending = blockedBattle.PendingEffects.Single(effect => effect.Spell.Id == spell.Id);
            blockedHero.ApplyStatus(StatusType.Bound, 1);

            FirePursuitPreview blocked = FireSpellEngine.PreviewMarkedTargetMove(blockedBattle, pending, blockedVacated);

            Assert.That(blocked.WillMove, Is.False);
            Assert.That(blocked.Reason, Does.Contain("束缚"));
            Assert.That(blocked.DetailCode, Is.EqualTo("pursuit_bound"));
        }

        [Test]
        public void CorePierce_HitsAdjacentEnemyWithoutConsumingBurning()
        {
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, "hero");
            FireBattleState battle = new FireBattleState(combat);
            UnitState hero = combat.GetUnit("hero");
            UnitState enemy = combat.GetUnit("range_normal");
            hero.Equip(CombatCatalog.Hammer, null, null);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-M07");

            Assert.That(FireSpellEngine.Preview(battle, hero.Id, spell, FireSpellTarget.Unit(enemy.Id)).CanCommit, Is.True);
            enemy.ApplyStatus(StatusType.Burning, 2, 8);
            combat.TryGrantRogueliteShield(enemy.Id, "core-pierce-test", 16);
            int vitalityBefore = enemy.Health + enemy.Shield;

            FireSpellExecution execution = FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(enemy.Id));

            Assert.That(execution.Steps.Any(step => step.Kind == FireRuleKind.WeaponDamage && step.Requested == 20), Is.True);
            Assert.That(execution.Steps.Any(step => step.Kind == FireRuleKind.Damage && step.Requested == 8), Is.True);
            Assert.That(enemy.HasStatus(StatusType.BreakStance), Is.True);
            Assert.That(enemy.HasStatus(StatusType.Burning), Is.True);
            Assert.That(enemy.Shield, Is.Zero);
            Assert.That(enemy.Health + enemy.Shield, Is.LessThan(vitalityBefore));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Does.StartWith("对相邻敌人").And.Contain("施加破势"));
        }

        [Test]
        public void FurnaceSweep_PreviewsThreeConeLayersAndAppliesVisibleFriendlyFire()
        {
            GridMap map = new GridMap(9, 7);
            UnitState hero = new UnitState("hero", true, new GridPosition(3, 3));
            UnitState ally = new UnitState("ally", true, new GridPosition(5, 4));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 3));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            ally.ConfigureVitality(99); enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, ally, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-M08");
            FireSpellTarget target = FireSpellTarget.Unit(enemy.Id, CardinalDirection.East);
            int allyBefore = ally.Health;
            int enemyBefore = enemy.Health;

            FireSpellPreview preview = FireSpellEngine.Preview(battle, hero.Id, spell, target);

            Assert.That(preview.CanCommit, Is.True);
            Assert.That(preview.Cells.Count, Is.EqualTo(9));
            Assert.That(preview.Cells, Does.Contain(new GridPosition(4, 3)));
            Assert.That(preview.Cells, Does.Contain(new GridPosition(5, 4)));
            Assert.That(preview.Cells, Does.Contain(new GridPosition(6, 1)));
            Assert.That(preview.FriendlyFireRisk, Is.True);
            FireSpellEngine.Execute(battle, hero.Id, spell, target);
            Assert.That(enemy.Health, Is.LessThan(enemyBefore));
            Assert.That(ally.Health, Is.LessThan(allyBefore));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Is.EqualTo("扇形内的敌人和友方各受12点武器伤害和4点火焰伤害。破障。"));
        }

        [Test]
        public void HeatBarrier_PreviewsAndAbsorbsTheFirstRangedSkillBeforeDamage()
        {
            GridMap map = new GridMap(6, 1);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(4, 0));
            hero.ConfigureVitality(40); hero.ConfigureMana(12);
            hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(40); enemy.ConfigureMana(12);
            enemy.Equip(EnemyAbilityCatalog.HeavyCrossbow, null, EnemyAbilityCatalog.WindlassBolt, null);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            string[] ids =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                "F-P-M11", string.Empty, string.Empty, string.Empty
            };
            var loadout = OCC.Combat.Roguelite.RogueSpellLoadout.Restore(
                ids.Where(value => !string.IsNullOrEmpty(value)), ids, true);
            var runtime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(combat, loadout);
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, hero.Id);
            runtime.ExecuteSlot(4, CombatCommand.UseSkill(hero.Id, 4, hero.Id));
            CombatCommand attack = CombatCommand.UseSkill(enemy.Id, 0, hero.Id);

            EnemyIntentPresentation intent = CombatInformationPresenter.BuildEnemyIntent(combat, enemy, attack);

            Assert.That(intent.ResultSummary, Does.StartWith("热障架势先获得 8 点护盾（0→8）")
                .And.Contain("预计造成 5 点护盾伤害").And.Contain("结算后护盾 3"));
            Assert.That(intent.ExpectedDamage, Is.EqualTo(5));
            Assert.That(runtime.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-M11"), Is.True,
                "只读意图预览不得消耗架势。");

            CombatResolver.BeginTurn(combat, enemy.Id);
            CombatResolver.Resolve(combat, attack);

            Assert.That(hero.Shield, Is.EqualTo(3));
            Assert.That(hero.Health, Is.EqualTo(40));
            Assert.That(runtime.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-M11"), Is.False);
        }

        [Test]
        public void EmberBlock_GrantsImmediateShieldThenRebuildsAfterTheFirstAdjacentSkillHit()
        {
            GridMap map = new GridMap(3, 1);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            hero.ConfigureVitality(40); hero.ConfigureMana(12); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(40); enemy.ConfigureMana(12);
            enemy.Equip(CombatCatalog.Hammer, null, EnemyAbilityCatalog.HookingStrike, null);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            string[] ids =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                "F-P-M12", string.Empty, string.Empty, string.Empty
            };
            var loadout = OCC.Combat.Roguelite.RogueSpellLoadout.Restore(
                ids.Where(value => !string.IsNullOrEmpty(value)), ids, true);
            var runtime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(combat, loadout);
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, hero.Id);

            runtime.ExecuteSlot(4, CombatCommand.UseSkill(hero.Id, 4, hero.Id));

            Assert.That(hero.Shield, Is.EqualTo(12));
            Assert.That(runtime.FireBattle.PendingEffects.Single().Spell.TriggerWindow,
                Is.EqualTo(FireTriggerWindow.FirstAdjacentAttack));
            CombatResolver.BeginTurn(combat, enemy.Id);
            CombatCommandExecutionResult result = new CombatCommandExecutionService().Execute(
                combat, runtime.FireBattle, CombatCommand.UseSkill(enemy.Id, 0, hero.Id));

            Assert.That(result.Accepted, Is.True);
            Assert.That(hero.Health, Is.EqualTo(40));
            Assert.That(hero.Shield, Is.EqualTo(13), "先承受 3 点护盾伤害，再由独立触发源补回 4 点。");
            Assert.That(result.AttackFireExecutions.Single().Steps.Any(step =>
                step.Kind == FireRuleKind.RestoreShield && step.Applied == 4), Is.True);
            Assert.That(runtime.FireBattle.PendingEffects, Is.Empty);
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(FireSpellCatalog.Get("F-P-M12")),
                Does.Contain("首次相邻攻击结算后"));
        }

        [Test]
        public void CoreCounter_ShowsLethalReactionInIntentAndTriggersAfterAdjacentSkill()
        {
            GridMap map = new GridMap(3, 1);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            hero.ConfigureVitality(40); hero.ConfigureMana(12); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(12); enemy.ConfigureMana(12);
            enemy.Equip(CombatCatalog.Hammer, null, EnemyAbilityCatalog.HookingStrike, null);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            string[] ids =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                "F-P-M13", string.Empty, string.Empty, string.Empty
            };
            var loadout = OCC.Combat.Roguelite.RogueSpellLoadout.Restore(
                ids.Where(value => !string.IsNullOrEmpty(value)), ids, true);
            var runtime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(combat, loadout);
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, hero.Id);
            runtime.ExecuteSlot(4, CombatCommand.UseSkill(hero.Id, 4, hero.Id));
            CombatCommand command = CombatCommand.UseSkill(enemy.Id, 0, hero.Id);

            EnemyIntentPresentation intent = CombatInformationPresenter.BuildEnemyIntent(combat, enemy, command);

            Assert.That(intent.ResultSummary, Does.Contain("公开反应：炉心反击")
                .And.Contain("生命 -12").And.Contain("击倒攻击者"));
            Assert.That(enemy.Health, Is.EqualTo(12), "只读意图不得提前结算反击。");

            CombatResolver.BeginTurn(combat, enemy.Id);
            CombatCommandExecutionResult result = new CombatCommandExecutionService().Execute(
                combat, runtime.FireBattle, command);

            Assert.That(result.Accepted, Is.True);
            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(result.AttackFireExecutions.Single().Steps.Any(step =>
                step.Kind == FireRuleKind.WeaponDamage && step.Requested == 12), Is.True);
            Assert.That(runtime.FireBattle.PendingEffects, Is.Empty);
        }

        [Test]
        public void MeleeUtilityBranch_M14ToM17KeepsDistinctControlAndBurningRoles()
        {
            CombatState releaseCombat = TrainingRangeScenarioFactory.CreateStandard();
            releaseCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(releaseCombat, "hero");
            UnitState releaseHero = releaseCombat.GetUnit("hero");
            UnitState releaseEnemy = releaseCombat.GetUnit("range_normal");
            releaseHero.Equip(CombatCatalog.Hammer, null, null);
            releaseHero.ApplyStatus(StatusType.Bound, 2);
            int releaseHealth = releaseEnemy.Health;
            FireSpellExecution release = FireSpellEngine.Execute(new FireBattleState(releaseCombat), releaseHero.Id,
                FireSpellCatalog.Get("F-P-M14"), FireSpellTarget.Unit(releaseEnemy.Id));
            Assert.That(releaseHero.HasStatus(StatusType.Bound), Is.False);
            Assert.That(releaseEnemy.Health, Is.LessThan(releaseHealth));
            Assert.That(release.Steps.Any(step => step.Kind == FireRuleKind.ClearStatus && step.Detail == StatusType.Bound.ToString()), Is.True);

            CombatState pushCombat = TrainingRangeScenarioFactory.CreateStandard();
            pushCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(pushCombat, "hero");
            UnitState pushHero = pushCombat.GetUnit("hero");
            UnitState pushEnemy = pushCombat.GetUnit("range_normal");
            pushHero.Equip(CombatCatalog.Hammer, null, null);
            GridPosition pushBefore = pushEnemy.Position;
            FireSpellExecution push = FireSpellEngine.Execute(new FireBattleState(pushCombat), pushHero.Id,
                FireSpellCatalog.Get("F-P-M15"), FireSpellTarget.Unit(pushEnemy.Id));
            Assert.That(pushEnemy.Position.ManhattanDistance(pushBefore), Is.EqualTo(1));
            Assert.That(push.Steps.Single(step => step.Kind == FireRuleKind.Push).Detail, Is.EqualTo("push"));

            CombatState harvestCombat = TrainingRangeScenarioFactory.CreateStandard();
            harvestCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(harvestCombat, "hero");
            UnitState harvestHero = harvestCombat.GetUnit("hero");
            UnitState harvestEnemy = harvestCombat.GetUnit("range_normal");
            harvestHero.Equip(CombatCatalog.Hammer, null, null);
            harvestEnemy.ApplyStatus(StatusType.Burning, 2, 8);
            FireSpellExecution harvest = FireSpellEngine.Execute(new FireBattleState(harvestCombat), harvestHero.Id,
                FireSpellCatalog.Get("F-P-M16"), FireSpellTarget.Unit(harvestEnemy.Id));
            Assert.That(harvestEnemy.HasStatus(StatusType.Burning), Is.True, "收割分支保留燃烧供后续术式继续利用。");
            Assert.That(harvest.Steps.Any(step => step.Kind == FireRuleKind.WeaponDamage && step.Requested == 16), Is.True);

            CombatState absorbCombat = TrainingRangeScenarioFactory.CreateStandard();
            absorbCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(absorbCombat, "hero");
            UnitState absorbHero = absorbCombat.GetUnit("hero");
            UnitState absorbEnemy = absorbCombat.GetUnit("range_normal");
            absorbHero.Equip(CombatCatalog.Hammer, null, null);
            absorbEnemy.ApplyStatus(StatusType.Burning, 2, 8);
            FireSpellExecution absorb = FireSpellEngine.Execute(new FireBattleState(absorbCombat), absorbHero.Id,
                FireSpellCatalog.Get("F-P-M17"), FireSpellTarget.Unit(absorbEnemy.Id));
            Assert.That(absorbEnemy.HasStatus(StatusType.Burning), Is.False, "吸热分支必须消费燃烧。");
            Assert.That(absorbHero.Shield, Is.EqualTo(12));
            Assert.That(absorb.Steps.Any(step => step.Kind == FireRuleKind.ConsumeBurning), Is.True);

            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(FireSpellCatalog.Get("F-P-M14")), Does.Contain("解除自身束缚"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(FireSpellCatalog.Get("F-P-M15")), Does.Contain("推开1格。破障"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(FireSpellCatalog.Get("F-P-M16")), Does.Contain("16点武器伤害和8点火焰伤害"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(FireSpellCatalog.Get("F-P-M17")), Does.Contain("移除其燃烧"));
        }

        [Test]
        public void CoreOverlimit_UsesAPublicStraightChargeLandingAndUnshieldedSelfCost()
        {
            GridMap map = new GridMap(5, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 0));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-M19");
            int heroHealth = hero.Health;
            int enemyVitality = enemy.Health + enemy.Shield;

            FireSpellPreview preview = FireSpellEngine.Preview(battle, hero.Id, spell, FireSpellTarget.Unit(enemy.Id));
            Assert.That(preview.CanCommit, Is.True);
            Assert.That(preview.Cells, Is.EqualTo(new[] { new GridPosition(1, 0), new GridPosition(2, 0), new GridPosition(3, 0) }));

            FireSpellExecution execution = FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(enemy.Id));

            Assert.That(hero.Position, Is.EqualTo(new GridPosition(2, 0)), "落点固定为目标前一格，不从目标周围任意挑选。");
            Assert.That(execution.Steps.Single(step => step.Kind == FireRuleKind.MoveSource).Detail, Is.EqualTo("charge_to_front_cell"));
            Assert.That(enemy.Health + enemy.Shield, Is.LessThan(enemyVitality));
            Assert.That(hero.Health, Is.EqualTo(heroHealth - 8));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Does.Contain("突进至首个敌人前一格").And.Contain("失去8点生命（无视护盾）"));

            UnitState diagonalHero = new UnitState("hero2", true, new GridPosition(0, 0));
            UnitState diagonalEnemy = new UnitState("diagonal", false, new GridPosition(2, 1));
            diagonalHero.ConfigureVitality(99); diagonalHero.ConfigureMana(99); diagonalHero.Equip(CombatCatalog.Hammer, null, null);
            CombatState diagonalCombat = new CombatState(map.Clone(), new[] { diagonalHero, diagonalEnemy }, Array.Empty<CombatObjective>());
            diagonalCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(diagonalCombat, diagonalHero.Id);
            Assert.That(FireSpellEngine.Preview(new FireBattleState(diagonalCombat), diagonalHero.Id, spell,
                FireSpellTarget.Unit(diagonalEnemy.Id)).Failures, Does.Contain("目标必须位于同一横线或竖线"));
        }

        [TestCase("F-P-U12")]
        [TestCase("F-P-U13")]
        [TestCase("F-P-U14")]
        [TestCase("F-P-U15")]
        [TestCase("F-P-U20")]
        public void ConditionalWeaponAttachments_WaitForAnActuallyLegalStatusTarget(string spellId)
        {
            GridMap map = new GridMap(3, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 1));
            UnitState wrong = new UnitState("wrong", false, new GridPosition(1, 2));
            UnitState legal = new UnitState("legal", false, new GridPosition(2, 1));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            wrong.ConfigureVitality(99); legal.ConfigureVitality(99); legal.ApplyStatus(StatusType.Burning, 2, 8);
            CombatState combat = new CombatState(map, new[] { hero, wrong, legal }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get(spellId);
            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(hero.Id));

            IReadOnlyList<FireSpellExecution> wrongResult = FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, wrong.Id);

            Assert.That(wrongResult, Is.Empty, "不满足公开条件的攻击不得浪费待触发窗口：" + spellId);
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spellId), Is.True);

            IReadOnlyList<FireSpellExecution> legalResult = FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, legal.Id);

            Assert.That(legalResult.Count, Is.EqualTo(1));
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spellId), Is.False);
            if (spellId == "F-P-U12")
            {
                Assert.That(hero.Shield, Is.EqualTo(12), "余热必须先转化为护盾，再消耗燃烧状态。 ");
                Assert.That(legal.HasStatus(StatusType.Burning), Is.False);
            }
        }

        [Test]
        public void FurnacePressureCharge_WaitsForCurrentWeaponMaximumRange()
        {
            int maximumRange = CombatCatalog.Rifle.Range;
            GridMap map = new GridMap(maximumRange + 2, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState near = new UnitState("near", false, new GridPosition(1, 0));
            UnitState far = new UnitState("far", false, new GridPosition(maximumRange, 0));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Rifle, null, null);
            near.ConfigureVitality(99); far.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, near, far }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-U16");
            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(hero.Id));

            IReadOnlyList<FireSpellExecution> result = FireSpellEngine.TriggerWeaponAttack(battle, hero.Id, near.Id);

            Assert.That(result.Single().Steps.Any(step => step.Kind == FireRuleKind.WeaponDamage && step.Requested == 12), Is.True);
            Assert.That(battle.PendingEffects.Any(effect => effect.Spell.Id == spell.Id), Is.False);
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Does.Contain("下一次武器攻击基础伤害+12"));
        }

        [Test]
        public void WeaponPreview_ShowsPendingSplashFriendlyFireWithoutConsumingTheAttachment()
        {
            GridMap map = new GridMap(3, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 1));
            UnitState ally = new UnitState("ally", true, new GridPosition(1, 2));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(99); ally.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, enemy, ally }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            string[] ids =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                "F-P-U05", string.Empty, string.Empty, string.Empty
            };
            var loadout = OCC.Combat.Roguelite.RogueSpellLoadout.Restore(
                ids.Where(value => !string.IsNullOrEmpty(value)), ids, true);
            var runtime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(combat, loadout);
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, hero.Id);
            runtime.ExecuteSlot(4, CombatCommand.UseSkill(hero.Id, 4, hero.Id));
            int heroHealth = hero.Health, allyHealth = ally.Health, enemyHealth = enemy.Health;

            CombatActionPreview preview = new BattlefieldPresentationAdapter().BuildPreview(combat, "攻击", enemy.Id);

            Assert.That(preview.ExpectedResult, Does.Contain("将触发：爆燃弹芯"));
            Assert.That(preview.StatusResults, Does.Contain("爆燃弹芯"));
            Assert.That(preview.FriendlyFireRisk, Is.True);
            Assert.That(preview.AffectedCellCount, Is.EqualTo(3));
            Assert.That(preview.DamageBreakdown, Does.StartWith("武器与附着合计"));
            Assert.That(hero.Health, Is.EqualTo(heroHealth));
            Assert.That(ally.Health, Is.EqualTo(allyHealth));
            Assert.That(enemy.Health, Is.EqualTo(enemyHealth));
            Assert.That(runtime.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-U05"), Is.True,
                "预览不得消耗真实待触发窗口。 ");
        }

        [TestCase("F-P-U02", StatusType.Burning, "烙痕传递", "目标获得燃烧")]
        [TestCase("F-P-U03", StatusType.BreakStance, "灼蚀校准", "目标进入破势")]
        public void WeaponStatusAttachments_PreviewAndApplyTheExactSelectedTarget(string spellId, StatusType status,
            string displayName, string resultText)
        {
            GridMap map = new GridMap(4, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(1, 0));
            hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            string[] ids =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                spellId, string.Empty, string.Empty, string.Empty
            };
            var loadout = OCC.Combat.Roguelite.RogueSpellLoadout.Restore(
                ids.Where(value => !string.IsNullOrEmpty(value)), ids, true);
            var runtime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(combat, loadout);
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, hero.Id);
            runtime.ExecuteSlot(4, CombatCommand.UseSkill(hero.Id, 4, hero.Id));

            CombatActionPreview preview = new BattlefieldPresentationAdapter().BuildPreview(combat, "攻击", enemy.Id);

            Assert.That(preview.ExpectedResult, Does.Contain("将触发：" + displayName).And.Contain(resultText));
            Assert.That(preview.StatusResults, Does.Contain(resultText));
            Assert.That(enemy.HasStatus(status), Is.False, "只读预览不能提前施加状态。 ");
            Assert.That(runtime.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == spellId), Is.True);

            FireSpellEngine.ResolveWeaponAttack(runtime.FireBattle, hero.Id, enemy.Id);
            Assert.That(enemy.HasStatus(status), Is.True);
            Assert.That(runtime.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == spellId), Is.False);
        }

        [Test]
        public void EmberFireball_DealsBaseDamageAndRaisesEligibleBurningToAtLeastTwoTurns()
        {
            GridMap map = new GridMap(6, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState burning = new UnitState("burning", false, new GridPosition(3, 0));
            UnitState plain = new UnitState("plain", false, new GridPosition(3, 1));
            hero.ConfigureMana(99); burning.ConfigureVitality(99); plain.ConfigureVitality(99);
            burning.ApplyStatus(StatusType.Burning, 1, 8);
            CombatState combat = new CombatState(map, new[] { hero, burning, plain }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R05");
            int burningVitality = burning.Health + burning.Shield;

            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(burning.Id));

            Assert.That(burning.Health + burning.Shield, Is.LessThan(burningVitality));
            Assert.That(burning.StatusDuration(StatusType.Burning), Is.EqualTo(2),
                "正式数据定义为延长至至少 2 回合，而不是额外增加 2 回合。 ");
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Does.Contain("燃烧至少持续2回合"));

            CombatState plainCombat = new CombatState(new GridMap(6, 2), new[]
            {
                new UnitState("hero", true, new GridPosition(0, 0)), plain = new UnitState("plain", false, new GridPosition(3, 1))
            }, Array.Empty<CombatObjective>());
            plainCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            plainCombat.GetUnit("hero").ConfigureMana(99); plain.ConfigureVitality(99);
            CombatResolver.BeginTurn(plainCombat, "hero"); int plainVitality = plain.Health + plain.Shield;
            FireSpellEngine.Execute(new FireBattleState(plainCombat), "hero", spell, FireSpellTarget.Unit(plain.Id));
            Assert.That(plain.Health + plain.Shield, Is.LessThan(plainVitality));
            Assert.That(plain.HasStatus(StatusType.Burning), Is.False,
                "未燃烧目标仍受基础伤害，但不能凭空获得余烬延长。 ");
        }

        [Test]
        public void RangedBasics_PreserveRangeDamageAndBurnEstablishmentTradeoffs()
        {
            GridMap directMap = new GridMap(7, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState close = new UnitState("close", false, new GridPosition(3, 0));
            UnitState far = new UnitState("far", false, new GridPosition(5, 0));
            hero.ConfigureMana(99); close.ConfigureVitality(99); far.ConfigureVitality(99);
            CombatState direct = new CombatState(directMap, new[] { hero, close, far }, Array.Empty<CombatObjective>());
            direct.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(direct, hero.Id);
            FireBattleState directBattle = new FireBattleState(direct);
            FireSpellDefinition fireball = FireSpellCatalog.Get("F-P-R01");
            FireSpellDefinition arrow = FireSpellCatalog.Get("F-P-R02");

            Assert.That(FireSpellEngine.Preview(directBattle, hero.Id, fireball, FireSpellTarget.Unit(close.Id)).CanCommit, Is.True);
            Assert.That(FireSpellEngine.Preview(directBattle, hero.Id, fireball, FireSpellTarget.Unit(far.Id)).CanCommit, Is.False,
                "高伤火弹必须实际受到 3 格短射程限制。 ");
            Assert.That(FireSpellEngine.Preview(directBattle, hero.Id, arrow, FireSpellTarget.Unit(far.Id)).CanCommit, Is.True,
                "低伤火矢必须真实换得 5 格射程。 ");
            int closeBefore = close.Health + close.Shield;
            int farBefore = far.Health + far.Shield;
            FireSpellEngine.Execute(directBattle, hero.Id, fireball, FireSpellTarget.Unit(close.Id));
            FireSpellEngine.Execute(directBattle, hero.Id, arrow, FireSpellTarget.Unit(far.Id));
            Assert.That(closeBefore - (close.Health + close.Shield), Is.EqualTo(12));
            Assert.That(farBefore - (far.Health + far.Shield), Is.EqualTo(8));

            GridMap burnMap = new GridMap(6, 2);
            hero = new UnitState("hero2", true, new GridPosition(0, 0));
            UnitState branded = new UnitState("branded", false, new GridPosition(3, 0));
            UnitState seeded = new UnitState("seeded", false, new GridPosition(4, 0));
            hero.ConfigureMana(99); branded.ConfigureVitality(99); seeded.ConfigureVitality(99);
            CombatState burnCombat = new CombatState(burnMap, new[] { hero, branded, seeded }, Array.Empty<CombatObjective>());
            burnCombat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(burnCombat, hero.Id);
            FireBattleState burnBattle = new FireBattleState(burnCombat);
            int brandedBefore = branded.Health + branded.Shield;
            int seededBefore = seeded.Health + seeded.Shield;
            FireSpellEngine.Execute(burnBattle, hero.Id, FireSpellCatalog.Get("F-P-R03"), FireSpellTarget.Unit(branded.Id));
            FireSpellEngine.Execute(burnBattle, hero.Id, FireSpellCatalog.Get("F-P-R04"), FireSpellTarget.Unit(seeded.Id));

            Assert.That(brandedBefore - (branded.Health + branded.Shield), Is.EqualTo(0));  // 总案 3.5.6.5 伤害类别分区：R03 烙印改为纯点燃起手，不再造成直伤。
            Assert.That(branded.StatusDuration(StatusType.Burning), Is.EqualTo(1));
            Assert.That(branded.StatusStrength(StatusType.Burning), Is.EqualTo(8));
            Assert.That(seeded.Health + seeded.Shield, Is.EqualTo(seededBefore), "火种只建立燃烧，不附带直伤。 ");
            Assert.That(seeded.StatusDuration(StatusType.Burning), Is.EqualTo(2));
            Assert.That(seeded.StatusStrength(StatusType.Burning), Is.EqualTo(8));
        }

        [Test]
        public void DelayedRangedAttacks_ApplyDamageThenPublishTheirOwnTimelineCost()
        {
            GridMap impactMap = new GridMap(5, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 0));
            hero.ConfigureMana(99); enemy.ConfigureVitality(99);
            CombatState impactCombat = new CombatState(impactMap, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            impactCombat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(impactCombat, hero.Id);
            FireBattleState impactBattle = new FireBattleState(impactCombat);
            int heroTimelineBefore = hero.ActionValue;
            int enemyBefore = enemy.Health + enemy.Shield;
            FireSpellEngine.Execute(impactBattle, hero.Id, FireSpellCatalog.Get("F-P-R09"), FireSpellTarget.Unit(enemy.Id));

            Assert.That(enemyBefore - (enemy.Health + enemy.Shield), Is.EqualTo(20));
            Assert.That(hero.ActionValue, Is.EqualTo(heroTimelineBefore - 4));

            GridMap breachMap = new GridMap(5, 2);
            hero = new UnitState("hero2", true, new GridPosition(0, 0));
            enemy = new UnitState("enemy2", false, new GridPosition(3, 0));
            hero.ConfigureMana(99); enemy.ConfigureVitality(99);
            CombatState breachCombat = new CombatState(breachMap, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            breachCombat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(breachCombat, hero.Id);
            FireBattleState breachBattle = new FireBattleState(breachCombat);
            heroTimelineBefore = hero.ActionValue;
            enemyBefore = enemy.Health + enemy.Shield;
            FireSpellDefinition breach = FireSpellCatalog.Get("F-P-R10");
            FireSpellEngine.Execute(breachBattle, hero.Id, breach, FireSpellTarget.Unit(enemy.Id));

            Assert.That(enemyBefore - (enemy.Health + enemy.Shield), Is.EqualTo(12), "破势只影响后续伤害，不能回溯本次命中。 ");
            Assert.That(enemy.HasStatus(StatusType.BreakStance), Is.True);
            Assert.That(hero.ActionValue, Is.EqualTo(heroTimelineBefore - 4));
            Assert.That(breach.InitiativeDelay, Is.EqualTo(4));
        }

        [Test]
        public void PressureStrike_MatchesTheOfficialRangeContractWithoutADeadZone()
        {
            GridMap map = new GridMap(6, 2);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState adjacent = new UnitState("adjacent", false, new GridPosition(1, 0));
            UnitState band = new UnitState("band", false, new GridPosition(2, 0));
            UnitState edge = new UnitState("edge", false, new GridPosition(3, 0));
            UnitState far = new UnitState("far", false, new GridPosition(4, 0));
            hero.ConfigureMana(99);
            foreach (UnitState enemy in new[] { adjacent, band, edge, far }) enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, adjacent, band, edge, far }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R09");

            // The official table defines 焰击术 as a plain three-cell spell. The 2–3 test-arena band was
            // never part of it, so an adjacent target is legal here; the dead-zone presentation keeps its
            // own coverage in CombatBattlefieldCellPresenterTests.
            Assert.That(spell.DisplayName, Is.EqualTo("焰击术"));
            Assert.That(spell.MinimumRange, Is.Zero);
            Assert.That(spell.Range, Is.EqualTo(3));
            Assert.That(spell.InitiativeDelay, Is.EqualTo(4));
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, spell, FireSpellTarget.Unit(adjacent.Id)).CanCommit, Is.True);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, spell, FireSpellTarget.Unit(edge.Id)).CanCommit, Is.True);
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, spell, FireSpellTarget.Unit(far.Id)).Failures,
                Does.Contain("超出射程"));
            // 总案 3.5.1.1: range is written as 形状＋距离, never as a bare reach number, and 选取范围 and
            // 作用范围 are separate fields.
            Assert.That(OCC.Combat.Presentation.RogueliteSettlementPresentation.FireSpellTargetSummary(spell),
                Does.Contain("选取：单点 3 格内").And.Contain("；作用：目标格"));

            int before = band.Health + band.Shield;
            FireSpellEngine.Execute(battle, hero.Id, spell, FireSpellTarget.Unit(band.Id));
            Assert.That(before - (band.Health + band.Shield), Is.EqualTo(20));
        }

        [Test]
        public void RangedLineSpells_UseTheirFullFourCellLineAndRejectAnOffAxisUnitAnchor()
        {
            GridMap map = new GridMap(8, 5);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 2));
            UnitState target = new UnitState("target", false, new GridPosition(3, 2));
            UnitState far = new UnitState("far", false, new GridPosition(5, 2));
            UnitState ally = new UnitState("ally", true, new GridPosition(4, 2));
            UnitState offAxis = new UnitState("off_axis", false, new GridPosition(3, 4));
            hero.ConfigureMana(99); target.ConfigureVitality(99); far.ConfigureVitality(99);
            ally.ConfigureVitality(99); offAxis.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, target, far, ally, offAxis }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, hero.Id);
            GridPosition objectCell = new GridPosition(5, 2);
            combat.Map.SetTile(objectCell, new TileState { Cover = CoverType.Light, Durability = 30 });
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition line = FireSpellCatalog.Get("F-P-R06");

            FireSpellPreview preview = FireSpellEngine.Preview(battle, hero.Id, line,
                FireSpellTarget.Unit(target.Id, CardinalDirection.East));
            Assert.That(preview.CanCommit, Is.True);
            Assert.That(preview.Cells, Is.EqualTo(new[]
            {
                new GridPosition(2, 2), new GridPosition(3, 2), new GridPosition(4, 2), new GridPosition(5, 2)
            }));
            Assert.That(preview.FriendlyFireRisk, Is.True);
            int farBefore = far.Health + far.Shield, allyBefore = ally.Health + ally.Shield;
            FireSpellEngine.Execute(battle, hero.Id, line, FireSpellTarget.Unit(target.Id, CardinalDirection.East));
            Assert.That(far.Health + far.Shield, Is.LessThan(farBefore));
            Assert.That(ally.Health + ally.Shield, Is.LessThan(allyBefore));
            Assert.That(combat.Map.GetTile(objectCell).Durability, Is.EqualTo(14),
                "焰线的破障使物件受到双倍耐久伤害。");

            CombatState offAxisCombat = new CombatState(new GridMap(8, 5), new[]
            {
                hero = new UnitState("hero2", true, new GridPosition(1, 2)),
                offAxis = new UnitState("off_axis2", false, new GridPosition(3, 4))
            }, Array.Empty<CombatObjective>());
            hero.ConfigureMana(99); offAxisCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(offAxisCombat, hero.Id);
            Assert.That(FireSpellEngine.Preview(new FireBattleState(offAxisCombat), hero.Id, line,
                FireSpellTarget.Unit(offAxis.Id, CardinalDirection.East)).Failures, Does.Contain("所选单位不在作用范围内"));

            CombatState roadCombat = new CombatState(new GridMap(8, 5), new[]
            {
                hero = new UnitState("hero3", true, new GridPosition(1, 2)),
                new UnitState("enemy3", false, new GridPosition(7, 4))
            }, Array.Empty<CombatObjective>());
            hero.ConfigureMana(99); roadCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(roadCombat, hero.Id);
            FireBattleState roadBattle = new FireBattleState(roadCombat);
            FireSpellEngine.Execute(roadBattle, hero.Id, FireSpellCatalog.Get("F-P-R12"),
                FireSpellTarget.At(new GridPosition(2, 2), CardinalDirection.East));
            Assert.That(roadBattle.Firegrounds.Keys, Is.EquivalentTo(new[]
            {
                new GridPosition(2, 2), new GridPosition(3, 2), new GridPosition(4, 2), new GridPosition(5, 2)
            }));
        }

        [Test]
        public void RangedCones_HitBothSidesButDoNotPassThroughHeavyCover()
        {
            GridMap map = new GridMap(8, 7);
            map.SetTile(new GridPosition(3, 3), new TileState { Cover = CoverType.Heavy, Durability = 24 });
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 3));
            UnitState primary = new UnitState("primary", false, new GridPosition(2, 3));
            UnitState visibleAlly = new UnitState("visible_ally", true, new GridPosition(3, 2));
            UnitState screenedEnemy = new UnitState("screened_enemy", false, new GridPosition(4, 3));
            hero.ConfigureMana(99); primary.ConfigureVitality(99); visibleAlly.ConfigureVitality(99); screenedEnemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, primary, visibleAlly, screenedEnemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spray = FireSpellCatalog.Get("F-P-R07");
            FireSpellTarget target = FireSpellTarget.Unit(primary.Id, CardinalDirection.East);
            FireSpellPreview preview = FireSpellEngine.Preview(battle, hero.Id, spray, target);
            int primaryBefore = primary.Health + primary.Shield;
            int allyBefore = visibleAlly.Health + visibleAlly.Shield;
            int screenedBefore = screenedEnemy.Health + screenedEnemy.Shield;

            Assert.That(preview.CanCommit, Is.True);
            Assert.That(preview.Cells.Contains(primary.Position), Is.True);
            Assert.That(preview.Cells.Contains(visibleAlly.Position), Is.True);
            Assert.That(preview.Cells.Contains(screenedEnemy.Position), Is.False, "重型遮挡后的锥形格不能显示为将受影响。 ");
            Assert.That(preview.FriendlyFireRisk, Is.True);
            FireSpellEngine.Execute(battle, hero.Id, spray, target);

            Assert.That(primary.Health + primary.Shield, Is.LessThan(primaryBefore));
            Assert.That(visibleAlly.Health + visibleAlly.Shield, Is.LessThan(allyBefore));
            Assert.That(screenedEnemy.Health + screenedEnemy.Shield, Is.EqualTo(screenedBefore));

            GridMap igniteMap = new GridMap(7, 7);
            hero = new UnitState("hero2", true, new GridPosition(1, 3));
            primary = new UnitState("primary2", false, new GridPosition(2, 3));
            visibleAlly = new UnitState("visible_ally2", true, new GridPosition(3, 2));
            hero.ConfigureMana(99); primary.ConfigureVitality(99); visibleAlly.ConfigureVitality(99);
            CombatState igniteCombat = new CombatState(igniteMap, new[] { hero, primary, visibleAlly }, Array.Empty<CombatObjective>());
            igniteCombat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(igniteCombat, hero.Id);
            FireBattleState igniteBattle = new FireBattleState(igniteCombat);
            allyBefore = visibleAlly.Health + visibleAlly.Shield;
            FireSpellEngine.Execute(igniteBattle, hero.Id, FireSpellCatalog.Get("F-P-R08"),
                FireSpellTarget.Unit(primary.Id, CardinalDirection.East));

            Assert.That(primary.HasStatus(StatusType.Burning), Is.True, "点燃喷射只给锥形内敌人施加燃烧。 ");
            Assert.That(visibleAlly.Health + visibleAlly.Shield, Is.EqualTo(allyBefore));  // 总案 3.5.6.5：R08 改为纯点燃起手，友军不再承受直伤。
            Assert.That(visibleAlly.HasStatus(StatusType.Burning), Is.False, "友军不承受直伤，也不被点燃。 ");
        }

        [Test]
        public void GroundDetonation_RequiresAndConsumesTheTargetUnitFireground()
        {
            GridMap map = new GridMap(6, 3);
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 1));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 1));
            hero.ConfigureMana(99); enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R17");
            FireSpellTarget target = FireSpellTarget.Unit(enemy.Id);

            Assert.That(FireSpellEngine.Preview(battle, hero.Id, spell, target).CanCommit, Is.False,
                "没有通用火场前置时不能空耗术式。 ");
            battle.CreateOrRefreshFireground(enemy.Position, 8, 2, "test-ground");
            Assert.That(FireSpellEngine.Preview(battle, hero.Id, spell, target).CanCommit, Is.True);
            int vitalityBefore = enemy.Health + enemy.Shield;

            FireSpellEngine.Execute(battle, hero.Id, spell, target);

            Assert.That(enemy.Health + enemy.Shield, Is.LessThan(vitalityBefore));
            Assert.That(battle.HasFireground(enemy.Position), Is.False, "地火抽爆必须消费实际承载目标的火场。 ");
        }

        [Test]
        public void RangedFinishers_RestrictDetonationToEnemiesAndCreateFireOnlyOnEmptyCells()
        {
            GridMap detonationMap = new GridMap(6, 5);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 2));
            UnitState ally = new UnitState("ally", true, new GridPosition(2, 2));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 2));
            hero.ConfigureMana(99); ally.ApplyStatus(StatusType.Burning, 2, 8); enemy.ApplyStatus(StatusType.Burning, 2, 8);
            CombatState detonation = new CombatState(detonationMap, new[] { hero, ally, enemy }, Array.Empty<CombatObjective>());
            detonation.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(detonation, hero.Id);
            FireBattleState detonationBattle = new FireBattleState(detonation);
            FireSpellDefinition r16 = FireSpellCatalog.Get("F-P-R16");
            Assert.That(FireSpellEngine.Preview(detonationBattle, hero.Id, r16, FireSpellTarget.Unit(ally.Id)).CanCommit, Is.False);
            Assert.That(FireSpellEngine.Preview(detonationBattle, hero.Id, r16, FireSpellTarget.Unit(enemy.Id)).CanCommit, Is.True);

            GridMap boundaryMap = new GridMap(7, 7);
            boundaryMap.SetTile(new GridPosition(4, 3), new TileState { Cover = CoverType.Heavy, Durability = 99 });
            hero = new UnitState("hero2", true, new GridPosition(1, 3));
            enemy = new UnitState("enemy2", false, new GridPosition(3, 3));
            ally = new UnitState("ally2", true, new GridPosition(3, 4));
            hero.ConfigureMana(99); enemy.ConfigureVitality(99); ally.ConfigureVitality(99);
            CombatState boundary = new CombatState(boundaryMap, new[] { hero, enemy, ally }, Array.Empty<CombatObjective>());
            boundary.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(boundary, hero.Id);
            FireBattleState boundaryBattle = new FireBattleState(boundary);
            FireSpellEngine.Execute(boundaryBattle, hero.Id, FireSpellCatalog.Get("F-P-R20"), FireSpellTarget.Unit(enemy.Id));

            Assert.That(enemy.IsAlive, Is.True);
            Assert.That(boundaryBattle.HasFireground(enemy.Position), Is.False, "卡面要求只在空格生成火场。");
            Assert.That(boundaryBattle.HasFireground(ally.Position), Is.False, "友方占用格也不是空格。");
            Assert.That(boundaryBattle.HasFireground(new GridPosition(3, 2)), Is.True);
            Assert.That(boundaryBattle.HasFireground(new GridPosition(4, 3)), Is.False, "重物块不是可燃地面。 ");
        }

        [Test]
        public void RangedFiregroundSpells_StopAtHeavyCoverAndKeepOccupiedFloorInTheArea()
        {
            GridMap lineMap = new GridMap(8, 5);
            lineMap.SetTile(new GridPosition(3, 2), new TileState { Cover = CoverType.Heavy, Durability = 24 });
            UnitState hero = new UnitState("hero", true, new GridPosition(0, 2)); hero.ConfigureMana(99);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(7, 4));
            CombatState lineCombat = new CombatState(lineMap, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            lineCombat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(lineCombat, hero.Id);
            FireBattleState lineBattle = new FireBattleState(lineCombat);
            FireSpellEngine.Execute(lineBattle, hero.Id, FireSpellCatalog.Get("F-P-R11"),
                FireSpellTarget.At(new GridPosition(2, 2), CardinalDirection.East));
            Assert.That(lineBattle.HasFireground(new GridPosition(2, 2)), Is.True);
            Assert.That(lineBattle.HasFireground(new GridPosition(3, 2)), Is.False);
            Assert.That(lineBattle.HasFireground(new GridPosition(4, 2)), Is.False, "火带不能越过重物块继续生成。 ");

            GridMap areaMap = new GridMap(7, 7);
            hero = new UnitState("hero2", true, new GridPosition(0, 3)); hero.ConfigureMana(99);
            enemy = new UnitState("enemy2", false, new GridPosition(3, 3)); enemy.ConfigureVitality(99);
            CombatState areaCombat = new CombatState(areaMap, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            areaCombat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(areaCombat, hero.Id);
            FireBattleState areaBattle = new FireBattleState(areaCombat);
            int vitalityBefore = enemy.Health + enemy.Shield;
            FireSpellEngine.Execute(areaBattle, hero.Id, FireSpellCatalog.Get("F-P-R15"),
                FireSpellTarget.At(new GridPosition(3, 2), CardinalDirection.East));
            Assert.That(areaBattle.HasFireground(enemy.Position), Is.True, "区域内单位占位不应在地面火场中挖出空洞。 ");
            Assert.That(enemy.Health + enemy.Shield, Is.EqualTo(vitalityBefore), "脚下生成火场时不立即追加进入伤害。 ");
        }

        [Test]
        public void RangedBurnSweep_OnlyDamagesAndConsumesBurningUnitsInItsVisibleCone()
        {
            GridMap map = new GridMap(7, 5);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 2)); hero.ConfigureMana(99);
            UnitState burningEnemy = new UnitState("burning_enemy", false, new GridPosition(2, 2));
            UnitState plainEnemy = new UnitState("plain_enemy", false, new GridPosition(3, 3));
            UnitState burningAlly = new UnitState("burning_ally", true, new GridPosition(3, 1));
            burningEnemy.ConfigureVitality(99); plainEnemy.ConfigureVitality(99); burningAlly.ConfigureVitality(99);
            burningEnemy.ApplyStatus(StatusType.Burning, 2, 8); burningAlly.ApplyStatus(StatusType.Burning, 2, 8);
            CombatState combat = new CombatState(map, new[] { hero, burningEnemy, plainEnemy, burningAlly }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, hero.Id);
            FireBattleState battle = new FireBattleState(combat);
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-R18");
            // 技能配置表：燃烧单位各受到 12 点火焰伤害并消费燃烧。
            Assert.That(spell.Rules.Single(rule => rule.Kind == FireRuleKind.Damage).Amount, Is.EqualTo(12));
            FireSpellTarget target = FireSpellTarget.Unit(burningEnemy.Id, CardinalDirection.East);
            FireSpellPreview preview = FireSpellEngine.Preview(battle, hero.Id, spell, target);
            int burningBefore = burningEnemy.Health + burningEnemy.Shield;
            int plainBefore = plainEnemy.Health + plainEnemy.Shield;
            int allyBefore = burningAlly.Health + burningAlly.Shield;
            Assert.That(preview.FriendlyFireRisk, Is.True);

            FireSpellEngine.Execute(battle, hero.Id, spell, target);

            Assert.That(burningEnemy.Health + burningEnemy.Shield, Is.LessThan(burningBefore));
            Assert.That(burningAlly.Health + burningAlly.Shield, Is.LessThan(allyBefore));
            Assert.That(plainEnemy.Health + plainEnemy.Shield, Is.EqualTo(plainBefore));
            Assert.That(burningEnemy.HasStatus(StatusType.Burning), Is.False);
            Assert.That(burningAlly.HasStatus(StatusType.Burning), Is.False);
        }

        [Test]
        public void HeatPressureFollowup_AutomaticallyRetreatsAwayFromTheWeaponTarget()
        {
            GridMap map = new GridMap(4, 1);
            UnitState hero = new UnitState("hero", true, new GridPosition(1, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(2, 0));
            hero.ConfigureVitality(99); hero.ConfigureMana(99); hero.Equip(CombatCatalog.Hammer, null, null);
            enemy.ConfigureVitality(99);
            CombatState combat = new CombatState(map, new[] { hero, enemy }, Array.Empty<CombatObjective>());
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            string[] ids =
            {
                "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
                "F-P-U06", string.Empty, string.Empty, string.Empty
            };
            var loadout = OCC.Combat.Roguelite.RogueSpellLoadout.Restore(
                ids.Where(value => !string.IsNullOrEmpty(value)), ids, true);
            var runtime = new OCC.Combat.Roguelite.RogueSpellCombatRuntime(combat, loadout);
            combat.AttachRogueSpellRuntime(runtime);
            CombatResolver.BeginTurn(combat, hero.Id);
            runtime.ExecuteSlot(4, CombatCommand.UseSkill(hero.Id, 4, hero.Id));

            CombatCommandExecutionResult result = new CombatCommandExecutionService().Execute(
                combat, runtime.FireBattle, CombatCommand.Attack(hero.Id, enemy.Id));

            Assert.That(result.Accepted, Is.True);
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(result.AttackFireExecutions.Single().Steps.Single(step => step.Kind == FireRuleKind.MoveAfterAttack).Detail,
                Is.EqualTo("post_attack_retreat"));
            Assert.That(runtime.FireBattle.PendingEffects.Any(effect => effect.Spell.Id == "F-P-U06"), Is.False);
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(FireSpellCatalog.Get("F-P-U06")),
                Does.Contain("向远离目标的方向后退1格"));
        }

        [Test]
        public void EmberArmor_UsesExactlyOneOfItsBurningAndNonBurningShieldBranches()
        {
            FireSpellDefinition spell = FireSpellCatalog.Get("F-P-U08");
            CombatState burningCombat = TrainingRangeScenarioFactory.CreateStandard();
            burningCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState burningHero = burningCombat.GetUnit("hero");
            burningHero.ConfigureMana(99); burningHero.ApplyStatus(StatusType.Burning, 2, 8);
            CombatResolver.BeginTurn(burningCombat, burningHero.Id);

            FireSpellExecution burningResult = FireSpellEngine.Execute(new FireBattleState(burningCombat), burningHero.Id,
                spell, FireSpellTarget.Unit(burningHero.Id));

            Assert.That(burningHero.Shield, Is.EqualTo(20));
            Assert.That(burningHero.HasStatus(StatusType.Burning), Is.False);
            Assert.That(burningResult.Steps.Count(step => step.Kind == FireRuleKind.RestoreShield && step.Applied > 0), Is.EqualTo(1));

            CombatState coolCombat = TrainingRangeScenarioFactory.CreateStandard();
            coolCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState coolHero = coolCombat.GetUnit("hero");
            coolHero.ConfigureMana(99);
            CombatResolver.BeginTurn(coolCombat, coolHero.Id);

            FireSpellExecution coolResult = FireSpellEngine.Execute(new FireBattleState(coolCombat), coolHero.Id,
                spell, FireSpellTarget.Unit(coolHero.Id));

            Assert.That(coolHero.Shield, Is.EqualTo(12));
            Assert.That(coolResult.Steps.Count(step => step.Kind == FireRuleKind.RestoreShield && step.Applied > 0), Is.EqualTo(1));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(spell),
                Does.Contain("否则获得12点护盾"));
        }

        [Test]
        public void UniversalSupport_U07U09U11UsesAllyMovementAndExclusiveFieldStateCorrectly()
        {
            CombatState shieldCombat = TrainingRangeScenarioFactory.CreateStandard();
            shieldCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState shieldHero = shieldCombat.GetUnit("hero"), ally = shieldCombat.GetUnit("range_ally");
            shieldHero.ConfigureMana(99);
            CombatResolver.BeginTurn(shieldCombat, shieldHero.Id);
            FireSpellEngine.Execute(new FireBattleState(shieldCombat), shieldHero.Id, FireSpellCatalog.Get("F-P-U07"),
                FireSpellTarget.Unit(ally.Id));
            Assert.That(ally.Shield, Is.EqualTo(12));

            CombatState wakeCombat = TrainingRangeScenarioFactory.CreateStandard();
            wakeCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState wakeHero = wakeCombat.GetUnit("hero");
            wakeHero.ConfigureMana(99); wakeHero.ApplyStatus(StatusType.Slow, 2);
            CombatResolver.BeginTurn(wakeCombat, wakeHero.Id);
            Assert.That(wakeHero.MovementRangeThisTurn, Is.EqualTo(UnitState.HeroSlowedMovementRange));
            FireSpellEngine.Execute(new FireBattleState(wakeCombat), wakeHero.Id, FireSpellCatalog.Get("F-P-U09"),
                FireSpellTarget.Unit(wakeHero.Id));
            Assert.That(wakeHero.HasStatus(StatusType.Slow), Is.False);
            Assert.That(wakeHero.MovementRangeThisTurn, Is.EqualTo(UnitState.HeroBaseMovementRange));

            CombatState recycleCombat = TrainingRangeScenarioFactory.CreateStandard();
            recycleCombat.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState recycleHero = recycleCombat.GetUnit("hero");
            recycleHero.ConfigureMana(99, 10);
            CombatResolver.BeginTurn(recycleCombat, recycleHero.Id);
            FireBattleState recycleBattle = new FireBattleState(recycleCombat);
            GridPosition field = new GridPosition(3, 2);
            recycleBattle.CreateOrRefreshFireground(field, 8, 2, "test");
            recycleBattle.CreateOrRefreshShallowWater(field);
            Assert.That(FireSpellEngine.Preview(recycleBattle, recycleHero.Id, FireSpellCatalog.Get("F-P-U11"),
                FireSpellTarget.At(field, CardinalDirection.South)).CanCommit, Is.False,
                "后来生成的浅水已经覆盖火场，不能重复回收。 ");
            recycleBattle.CreateOrRefreshFireground(field, 8, 2, "test-later-fire");
            int manaBefore = recycleHero.Mana;
            FireSpellEngine.Execute(recycleBattle, recycleHero.Id, FireSpellCatalog.Get("F-P-U11"),
                FireSpellTarget.At(field, CardinalDirection.South));
            Assert.That(recycleHero.Mana, Is.EqualTo(manaBefore + 2));
            Assert.That(recycleBattle.HasFireground(field), Is.False);
            Assert.That(recycleCombat.Map.GetTile(field).IsWater, Is.False);
        }

        [Test]
        public void FiregroundDurations_UseTwoThreeAndFourPublicTurns()
        {
            var expected = new Dictionary<string, int>
            {
                { "F-P-M03", 2 }, { "F-P-U17", 2 }, { "F-P-R11", 3 },
                { "F-P-R12", 2 }, { "F-P-R13", 4 }, { "F-P-R14", 4 },
                { "F-P-R15", 3 }, { "F-P-R20", 3 }
            };

            foreach (var pair in expected)
            {
                FireSpellRule rule = FireSpellCatalog.Get(pair.Key).Rules.Single(value => value.Kind == FireRuleKind.CreateFireground);
                Assert.That(rule.Duration, Is.EqualTo(pair.Value), pair.Key);
            }
            Assert.That(ItemAbilityCatalog.FirelineScroll.Rules.Single(value => value.Kind == FireRuleKind.CreateFireground).Duration,
                Is.EqualTo(2), "F-S01");
        }

        [Test]
        public void Fireground_DurationAdvancesOnlyOnHeroTurnsAndDoesNotShrinkWithEnemyCount()
        {
            GridPosition burningCell = new GridPosition(1, 1);
            UnitState first = new UnitState("first", true, burningCell);
            UnitState second = new UnitState("second", false, new GridPosition(2, 1));
            CombatState combat = new CombatState(new GridMap(4, 4), new[] { first, second }, Array.Empty<CombatObjective>());
            FireBattleState battle = new FireBattleState(combat);
            battle.CreateOrRefreshFireground(burningCell, 8, 2, "test-fireground");
            int before = first.Health + first.Shield;

            battle.BeginUnitTurn(first.Id);

            Assert.That(before - first.Health - first.Shield, Is.GreaterThan(0));
            Assert.That(battle.Firegrounds[burningCell].RemainingTurns, Is.EqualTo(1));

            battle.BeginUnitTurn(second.Id);

            Assert.That(battle.HasFireground(burningCell), Is.True,
                "An enemy turn must not consume a protagonist-round duration.");
            Assert.That(battle.Firegrounds[burningCell].RemainingTurns, Is.EqualTo(1));

            int afterFirstWindow = first.Health + first.Shield;
            battle.BeginUnitTurn(first.Id);

            Assert.That(battle.HasFireground(burningCell), Is.False);
            Assert.That(first.Health + first.Shield, Is.EqualTo(afterFirstWindow),
                "Expiry is resolved before turn-start damage in the final window.");
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
            Assert.That(combat.PassiveEffects.StatusBarEntriesFor(hero.Id).Single(value => value.DisplayName == "脱线疾行").TimingText,
                Is.EqualTo("下次自己回合开始"));
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
            int neighborBefore = neighbor.Health + neighbor.Shield;
            int centerDurabilityBefore = combat.Map.GetTile(center).Durability;
            FireSpellEngine.Execute(battle, "hero", FireSpellCatalog.Get("F-P-R19"), FireSpellTarget.At(center, CardinalDirection.East));

            // 技能配置表：对中心单位或物件造成 16 点伤害，并对正交相邻单位与物件各造成 8 点伤害。
            // Asserted as deltas so the expectation tracks the factory's current vitality values.
            Assert.That(centerDurabilityBefore - combat.Map.GetTile(center).Durability, Is.EqualTo(16),
                "The centre deals its full value to whatever stands on the target cell.");
            Assert.That(neighborBefore - (neighbor.Health + neighbor.Shield), Is.EqualTo(8),
                "Each orthogonal neighbour takes the neighbour value, unit or object alike.");
        }

        [Test]
        public void RewardPool_MakesAllImplementedPersonalSpellsReachableAndCanFilterForCurrentWeapon()
        {
            HashSet<string> reachable = new HashSet<string>(StringComparer.Ordinal);
            for (int seed = 0; seed < 4000 && reachable.Count < FireSpellCatalog.All.Count; seed++)
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
