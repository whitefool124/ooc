using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    /// <summary>新精英底图（E02／E04／E06 已建部分）：空间契约、编成、场地接入与流程引用。</summary>
    public sealed class AcademyEliteLevelTests
    {
        private static readonly string[] BuiltLevelIds =
        {
            "calibration_lockdown", "cliff_relay_survey", "library_discipline", "sealed_vault_certification", "outer_ring_clearance"
        };

        /// <summary>已确认为备用变体、尚未决定占用哪个精英节点的底图。</summary>
        /// <summary>总案扩展池（E04–E06）底图：可解析建图，但不进本阶段固定精英分配。</summary>
        private static readonly string[] ReserveLevelIds = { "library_discipline", "sealed_vault_certification" };

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
            Assert.That(ring.State.AcademyFieldEnemy, Is.Null, "没有场地敌人时不挂载该运行时。");
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
                    UnitState target = state.Units.Values.Where(value => value.IsAlive && value.IsHero != unit.IsHero)
                        .OrderBy(value => value.Position.ManhattanDistance(unit.Position))
                        .ThenBy(value => value.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (target == null) break;
                    CombatCommand command = unit.IsHero ? HeroCommand(state, unit, target)
                        : new EnemyTurnPlanBook().GetExecutionCommand(state, unit, hero);
                    if (command.Type == CombatCommandType.EndTurn) { CombatResolver.EndTurn(state, unit); commands++; continue; }
                    CombatResolver.Resolve(state, command);
                    commands++;
                }

                Assert.That(state.IsVictory, Is.True, id + " 未能在有限指令内取胜（commands=" + commands + "）");
                Assert.That(commands, Is.LessThan(400), id);
            }
        }

        /// <summary>验收用的简单主角策略：能打就打，否则走到目标的正交邻格。</summary>
        private static CombatCommand HeroCommand(CombatState state, UnitState hero, UnitState target)
        {
            if (hero.Position.ManhattanDistance(target.Position) <= 1) return CombatCommand.Attack(hero.Id, target.Id);
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
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("observatory_path").EncounterVariantId, Is.EqualTo("outer_ring_clearance_a"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("observatory_path").ContentTableId, Is.EqualTo("E06"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("tower_foyer").EncounterVariantId, Is.EqualTo("library_discipline_a"));
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("tower_foyer").ContentTableId, Is.EqualTo("E04"));
        }

        [Test]
        public void ElitePool_OffersExactlyOneVariantPerEliteNode()
        {
            int eliteNodes = RogueliteMapCatalog.Nodes.Count(node => node.Type == RogueliteMapNodeType.Elite);
            Assert.That(RogueliteEncounterCatalog.ElitePool.Count, Is.EqualTo(eliteNodes),
                "固定精英分配要求变体数恰好等于精英节点数。");
            Assert.That(RogueliteEncounterCatalog.ElitePool.Select(package => package.VariantKey).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(eliteNodes));
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
