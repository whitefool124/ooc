using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    /// <summary>新精英底图（E02／E04／E06 已建部分）：空间契约、编成、场地接入与流程引用。</summary>
    public sealed class AcademyEliteLevelTests
    {
        private static readonly string[] BuiltLevelIds =
        {
            "calibration_lockdown", "cliff_relay_survey", "library_discipline", "sealed_vault_certification", "outer_ring_clearance"
        };

        /// <summary>总案扩展池（E04–E06）底图：可解析建图，但不进本阶段固定精英分配。</summary>
        private static readonly string[] ReserveLevelIds = { "library_discipline", "sealed_vault_certification", "outer_ring_clearance" };

        [Test]
        public void NewEliteLevels_AreRegisteredAndValidate()
        {
            Assert.That(FirstRegionLevelCatalog.NewEliteLevels.Count, Is.EqualTo(5));
            Assert.That(FirstRegionLevelCatalog.Validate(), Is.Empty);
            foreach (string id in BuiltLevelIds)
            {
                Assert.That(FirstRegionLevelCatalog.TryFor(id, out FirstRegionLevelDefinition level), Is.True, id);
                Assert.That(level.IsElite, Is.True, id);
                Assert.That(level.ObjectiveType, Is.EqualTo(CombatObjectiveType.Elimination), id);
                Assert.That(level.EnemyPlacements.Count, Is.EqualTo(3), id);
                Assert.That(level.Terrain.Count, Is.GreaterThan(0), id);
            }
        }

        [Test]
        public void NewEliteLevels_KeepTheirConfirmedEnemyCompositions()
        {
            Assert.That(FirstRegionLevelCatalog.For("cliff_relay_survey").ResolveEnemyArchetypeIds(),
                Is.EqualTo(new[] { "elder_tracker_hound", "signal_keeper", "barrier_mender" }));
            Assert.That(FirstRegionLevelCatalog.For("library_discipline").ResolveEnemyArchetypeIds(),
                Is.EqualTo(new[] { "wind_librarian", "barrier_mender", "sigil_mauler" }));
            Assert.That(FirstRegionLevelCatalog.For("outer_ring_clearance").ResolveEnemyArchetypeIds(),
                Is.EqualTo(new[] { "elite_vanguard", "lantern_revealer", "rune_arbalist" }));
            Assert.That(FirstRegionLevelCatalog.For("calibration_lockdown").ResolveEnemyArchetypeIds(),
                Is.EqualTo(new[] { "prototype_hand", "rune_arbalist", "stone_snare" }));
            Assert.That(FirstRegionLevelCatalog.For("sealed_vault_certification").ResolveEnemyArchetypeIds(),
                Is.EqualTo(new[] { "legacy_storekeeper", "lantern_revealer", "sigil_mauler" }));
        }

        [Test]
        public void NewEliteLevels_BuildRealBattlefieldsWithTheirFieldDevices()
        {
            FirstRegionLevelBuild survey = FirstRegionLevelBuilder.Build("cliff_relay_survey");
            Assert.That(survey.State.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("elder_tracker_hound"));
            Assert.That(survey.State.GetUnit("enemy_1").EnemyArchetypeId, Is.EqualTo("signal_keeper"));
            Assert.That(survey.State.Map.PositionsWith(tile => tile.IsWater).Count(), Is.EqualTo(3), "坡面积水用于冲掉气味痕。");
            Assert.That(survey.State.Map.PositionsWith(tile => tile.IsLampVine).Count(), Is.EqualTo(4), "灯藤提供藏身与搜查。");
            Assert.That(survey.State.AcademyFieldEnemy, Is.Not.Null, "有场地敌人时必须接上公开条件反应运行时。");

            FirstRegionLevelBuild library = FirstRegionLevelBuilder.Build("library_discipline");
            Assert.That(library.State.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("wind_librarian"));
            Assert.That(library.State.Map.PositionsWith(tile => tile.IsLoosePaper).Count(), Is.EqualTo(4), "散页是小铃的材料。");
            Assert.That(library.State.Map.PositionsWith(tile => tile.IsBindingMark).Count(), Is.EqualTo(1));
            Assert.That(library.State.AcademyFieldEnemy, Is.Not.Null);

            FirstRegionLevelBuild lockdown = FirstRegionLevelBuilder.Build("calibration_lockdown");
            Assert.That(lockdown.State.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("prototype_hand"));
            Assert.That(lockdown.State.Map.PositionsWith(tile => tile.IsWardGenerator).Count(), Is.EqualTo(1));
            Assert.That(lockdown.State.Map.PositionsWith(tile => tile.IsOverloadDevice).Count(), Is.EqualTo(1));
            Assert.That(lockdown.State.Map.PositionsWith(tile => tile.IsWater).Count(), Is.EqualTo(2), "冷却沟洗掉地面刻印。");
            Assert.That(lockdown.State.AcademyFieldEnemy, Is.Not.Null);

            FirstRegionLevelBuild vault = FirstRegionLevelBuilder.Build("sealed_vault_certification");
            Assert.That(vault.State.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("legacy_storekeeper"));
            Assert.That(vault.State.Map.PositionsWith(tile => tile.IsCertifierStand).Count(), Is.EqualTo(2), "两座旧检定台是优先拆除目标。");
            Assert.That(vault.State.Map.PositionsWith(tile => tile.IsLoosePaper).Count(), Is.EqualTo(2));
            Assert.That(vault.State.AcademyFieldEnemy, Is.Not.Null);

            FirstRegionLevelBuild ring = FirstRegionLevelBuilder.Build("outer_ring_clearance");
            Assert.That(ring.State.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("elite_vanguard"));
            Assert.That(ring.State.Map.PositionsWith(tile => tile.IsWardGenerator).Count(), Is.EqualTo(1));
            Assert.That(ring.State.Map.PositionsWith(tile => tile.IsCertifierStand).Count(), Is.EqualTo(1));
            Assert.That(ring.State.AcademyFieldEnemy, Is.Not.Null, "划线教官与提灯巡查的岗位机制由同一运行时结算。");
        }

        [Test]
        public void EveryEnemyOfTheNewLevels_HasReachableFormalArt()
        {
            foreach (string id in BuiltLevelIds)
                foreach (string archetypeId in FirstRegionLevelCatalog.For(id).ResolveEnemyArchetypeIds())
                    Assert.DoesNotThrow(() => FormalArtRegistry.UnitPath(archetypeId), id + ":" + archetypeId);
        }

        [Test]
        public void EveryNewEliteLevel_IsWinnableWithRealCombatCommands()
        {
            foreach (string id in BuiltLevelIds)
            {
                CombatState state = FirstRegionLevelBuilder.Build(id).State;
                UnitState hero = state.GetUnit("hero");
                // Use the always-available ranged baseline for a neutral reachability check. Elite runs
                // arrive with a developed build; testing the bare hammer would conflate level reachability
                // with one deliberately short-ranged loadout.
                hero.Equip(CombatCatalog.Rifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
                hero.ConfigureMana(RogueRuntimeConstants.MaximumPersonalMana, RogueRuntimeConstants.MaximumPersonalMana);
                int commands = 0;
                CombatResolver.BeginTurn(state, "hero");
                while (!state.IsVictory && !state.IsDefeat && commands < 400)
                {
                    UnitState unit = state.GetUnit(state.ActiveUnitId);
                    if (unit == null || !unit.IsAlive)
                    {
                        CombatResolver.AdvanceToNextTurn(state);
                        if (state.GetUnit(state.ActiveUnitId) == null) break;
                        continue;
                    }
                    if (unit.ActionPoints <= 0) { CombatResolver.EndTurn(state, unit); commands++; continue; }
                    // 束缚只禁止移动；贴身时仍应攻击，否则验收策略会被永久定身而误判为打不过。
                    if (unit.HasStatus(StatusType.Bound))
                    {
                        UnitState bound = state.Units.Values.Where(value => value.IsAlive && value.IsHero != unit.IsHero &&
                                value.Position.ManhattanDistance(unit.Position) <= 1)
                            .OrderBy(value => value.Id, StringComparer.Ordinal).FirstOrDefault();
                        if (bound == null || !unit.IsHero) { CombatResolver.EndTurn(state, unit); commands++; continue; }
                        CombatResolver.Resolve(state, CombatCommand.Attack(unit.Id, bound.Id));
                        commands++;
                        continue;
                    }
                    UnitState target = state.Units.Values.Where(value => value.IsAlive && value.IsHero != unit.IsHero)
                        .OrderByDescending(value => unit.IsHero ? ThreatPriority(value.EnemyArchetypeId) : 0)
                        .ThenBy(value => value.Position.ManhattanDistance(unit.Position))
                        .ThenBy(value => value.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (target == null) break;
                    CombatCommand command = unit.IsHero ? HeroCommand(state, unit, target)
                        : new EnemyTurnPlanBook().GetExecutionCommand(state, unit, hero);
                    if (command.Type == CombatCommandType.EndTurn) { CombatResolver.EndTurn(state, unit); commands++; continue; }
                    CombatResolver.Resolve(state, command);
                    commands++;
                }

                string unitSummary = string.Join(", ", state.Units.Values.OrderBy(value => value.Id, StringComparer.Ordinal)
                    .Select(value => value.Id + "@" + value.Position + " hp=" + value.Health + " sh=" + value.Shield));
                Assert.That(state.IsVictory, Is.True, id + " 未能在有限指令内取胜（commands=" + commands + "；" +
                    unitSummary + "；log=" + string.Join(" / ", state.EventLog) + "）");
                Assert.That(commands, Is.LessThan(400), id);
            }
        }

        /// <summary>验收用的简单主角策略：能打就打，否则走到目标的正交邻格。</summary>
        private static int ThreatPriority(string archetypeId)
        {
            if (archetypeId == "sigil_mauler" || archetypeId == "elite_vanguard" ||
                archetypeId == "prototype_hand" || archetypeId == "legacy_storekeeper") return 4;
            if (archetypeId == "barrier_mender" || archetypeId == "rune_arbalist") return 3;
            if (archetypeId == "signal_keeper" || archetypeId == "lantern_revealer") return 2;
            return 1;
        }

        private static CombatCommand HeroCommand(CombatState state, UnitState hero, UnitState target)
        {
            int distance = hero.Position.ManhattanDistance(target.Position);
            SkillDefinition skill = hero.SkillOne;
            if (skill != null && hero.Mana >= skill.ManaCost && hero.IsSkillReady(skill) &&
                distance >= skill.MinimumRange && distance <= skill.Range &&
                CombatResolver.PreviewSkillAttack(state, hero.Id, target.Id, skill).HasLineOfSight)
                return CombatCommand.UseSkill(hero.Id, 0, target.Id);
            WeaponDefinition weapon = hero.MainHand ?? CombatCatalog.Rifle;
            if (distance >= weapon.MinimumRange && distance <= weapon.Range &&
                CombatResolver.PreviewAttack(state, hero.Id, target.Id, false).HasLineOfSight)
                return CombatCommand.Attack(hero.Id, target.Id);
            GridPosition[] spots = new[]
                {
                    new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0)
                }
                .Select(offset => target.Position + offset)
                .Where(position => state.Map.IsInside(position) && !state.Map.IsBlocked(position) && !state.IsOccupied(position, hero.Id))
                .OrderBy(position => position.ManhattanDistance(hero.Position)).ToArray();
            foreach (GridPosition spot in spots)
            {
                IReadOnlyList<GridPosition> path = CombatMovementQuery.FindPath(state, hero, spot);
                if (path.Count > 1) return CombatCommand.Move(hero.Id, path[path.Count - 1]);
            }
            return CombatCommand.EndTurn(hero.Id);
        }

        [Test]
        public void EliteEncounterVariants_ResolveToTheirOwnLevels()
        {
            foreach (string id in BuiltLevelIds)
            {
                RogueliteEncounterDefinition variant = RogueliteEncounterCatalog.Packages
                    .Single(package => package.VariantKey == id + "_a");
                Assert.That(variant.LevelId, Is.EqualTo(id));
                Assert.That(variant.Tier, Is.EqualTo(RogueliteEncounterTier.Elite));
                Assert.That(variant.EnemyArchetypeIds,
                    Is.EqualTo(FirstRegionLevelCatalog.For(id).ResolveEnemyArchetypeIds()), id);
                Assert.That(variant.InFixedPool, Is.EqualTo(!ReserveLevelIds.Contains(id)), id);
            }
        }

        [Test]
        public void EliteNodes_ReferenceVariantsForBuiltLevels()
        {
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("wilds_camp").EncounterVariantId, Is.EqualTo("cliff_relay_survey_a"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("wilds_camp").ContentTableId, Is.EqualTo("E03"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("observatory_path").EncounterVariantId, Is.EqualTo("calibration_lockdown_a"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("observatory_path").ContentTableId, Is.EqualTo("E02"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("tower_foyer").EncounterVariantId, Is.EqualTo("elite_foundry_b"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("tower_foyer").ContentTableId, Is.EqualTo("E01"));
        }

        [Test]
        public void ElitePool_OffersExactlyOneVariantPerEliteNode()
        {
            int eliteNodes = RogueliteAcademyLayerCatalog.NodeIds
                .Select(RogueliteMapCatalog.Node).Count(node => node.Type == RogueliteMapNodeType.Elite);
            Assert.That(RogueliteEncounterCatalog.ElitePool.Count, Is.EqualTo(eliteNodes),
                "固定精英分配要求变体数恰好等于精英节点数。");
            Assert.That(RogueliteEncounterCatalog.ElitePool.Select(package => package.VariantKey).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(eliteNodes));
            Assert.That(RogueliteEncounterCatalog.ElitePool.Select(package => package.VariantKey),
                Is.EquivalentTo(RogueliteAcademyLayerCatalog.ContentMappings
                    .Where(mapping => mapping.Type == RogueliteMapNodeType.Elite)
                    .Select(mapping => mapping.EncounterVariantId)));
        }

        [Test]
        public void AcademyLayerAssignments_OnlyUseResolvableLevels()
        {
            foreach (RogueliteEncounterAssignment assignment in RogueliteAcademyLayerCatalog.GenerateEncounterAssignments(20260917))
            {
                RogueliteEncounterDefinition variant = RogueliteEncounterCatalog.Package(assignment.VariantKey);
                Assert.DoesNotThrow(() => FirstRegionLevelCatalog.For(variant.LevelId), assignment.NodeId + ":" + assignment.VariantKey);
            }
        }
    }
}
