using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FirstRegionLevelSystemTests
    {
        private static readonly string[] PackIds =
        {
            "shieldguard", "pyromancer", "raider", "elite_vanguard", "sigil_mauler",
            "barrier_mender", "tether_hound", "stone_snare", "lantern_revealer", "rune_arbalist"
        };

        [Test]
        public void Catalog_HasNineValidEraCorrectLevels()
        {
            Assert.That(FirstRegionLevelCatalog.All.Count, Is.EqualTo(9));
            Assert.That(FirstRegionLevelCatalog.All.Select(level => level.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
            Assert.That(FirstRegionLevelCatalog.All.Select(level => level.DisplayName).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
            Assert.That(FirstRegionLevelCatalog.Validate(), Is.Empty);

            string visibleSurface = string.Join("|", FirstRegionLevelCatalog.All.SelectMany(level => new[]
            {
                level.DisplayName, level.ObjectiveSummary, level.EnemySummary()
            }));
            foreach (string forbidden in new[] { "步枪", "狙击", "枪械", "爆破", "铁路", "货场", "铸造厂", "精炼厂", "传输" })
                Assert.That(visibleSurface, Does.Not.Contain(forbidden), forbidden);
        }

        [Test]
        public void EveryLevel_BuildsExactMapObjectiveTerrainAndEnemyPlacements()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(level, "purifier_overseer");
                CombatState state = build.State;
                Assert.That(state.Map.Width, Is.EqualTo(level.Width), level.Id);
                Assert.That(state.Map.Height, Is.EqualTo(level.Height), level.Id);
                Assert.That(state.Map.Width * state.Map.Height, Is.InRange(25, 50), level.Id);
                Assert.That(state.GetUnit("hero").Position, Is.EqualTo(level.HeroSpawn), level.Id);
                Assert.That(state.GetUnit("hero").MainHand, Is.SameAs(CombatCatalog.Hammer), level.Id);
                Assert.That(state.Units.Values.Count(unit => !unit.IsHero), Is.EqualTo(level.EnemyPlacements.Count), level.Id);
                Assert.That(state.Units.Values.Select(unit => unit.Position).Distinct().Count(), Is.EqualTo(state.Units.Count), level.Id);
                Assert.That(state.Objectives.Single().Type, Is.EqualTo(level.ObjectiveType), level.Id);

                IReadOnlyList<string> resolved = level.ResolveEnemyArchetypeIds("purifier_overseer");
                for (int index = 0; index < resolved.Count; index++)
                {
                    UnitState enemy = state.GetUnit("enemy_" + index);
                    Assert.That(enemy, Is.Not.Null, level.Id + ":enemy_" + index);
                    Assert.That(enemy.EnemyArchetypeId, Is.EqualTo(resolved[index]), level.Id);
                    Assert.That(enemy.Position, Is.EqualTo(level.EnemyPlacements[index].Position), level.Id);
                    Assert.That(enemy.SkillOne, Is.Not.Null, level.Id);
                    Assert.DoesNotThrow(() => FormalArtRegistry.UnitPath(enemy.EnemyArchetypeId), level.Id);
                }

                foreach (LevelTerrainPlacement placement in level.Terrain)
                {
                    TileState tile = state.Map.GetTile(placement.Position);
                    if (placement.Kind == LevelTerrainKind.LightCover) Assert.That(tile.Cover, Is.EqualTo(CoverType.Light), level.Id);
                    if (placement.Kind == LevelTerrainKind.HeavyCover) Assert.That(tile.Cover, Is.EqualTo(CoverType.Heavy), level.Id);
                    if (placement.Kind == LevelTerrainKind.AetherObjective) Assert.That(tile.IsObjective && tile.IsDevice, Is.True, level.Id);
                }
                foreach (GridPosition blocked in level.BlockedPositions)
                    Assert.That(state.Map.IsBlocked(blocked), Is.True, level.Id + ":" + blocked);
            }
        }

        [Test]
        public void NineLevels_HaveDistinctTacticalLayoutSignatures()
        {
            string[] signatures = FirstRegionLevelCatalog.All.Select(level => string.Join(";",
                level.EnemyPlacements.Select(enemy => "E:" + enemy.ArchetypeId + "@" + enemy.Position.X + "," + enemy.Position.Y)
                    .Concat(level.Terrain.Select(tile => "T:" + tile.Kind + "@" + tile.Position.X + "," + tile.Position.Y))
                    .Concat(level.BlockedPositions.Select(tile => "B:@" + tile.X + "," + tile.Y))
                    .OrderBy(value => value, StringComparer.Ordinal))).ToArray();
            Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
        }

        [Test]
        public void NineLevels_UseDistinctSpaceGrammarsAndNoLegacyUniformPlacementTemplate()
        {
            Assert.That(FirstRegionLevelCatalog.All.Select(level => level.SpaceContract.Grammar)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
            Assert.That(FirstRegionLevelCatalog.All.Select(level => level.HeroSpawn).Distinct().Count(), Is.GreaterThanOrEqualTo(6));
            Assert.That(FirstRegionLevelCatalog.All.All(level => level.HeroSpawn != new GridPosition(1, 4)), Is.True);

            GridPosition[] objectives = FirstRegionLevelCatalog.All
                .SelectMany(level => level.Terrain.Where(tile => tile.Kind == LevelTerrainKind.AetherObjective).Select(tile => tile.Position)).ToArray();
            Assert.That(objectives, Has.Length.EqualTo(3));
            Assert.That(objectives.Distinct().Count(), Is.EqualTo(3));
            Assert.That(objectives.Contains(new GridPosition(10, 4)), Is.False);

            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                // 塔内机关与敌人单位共同构成 B01 的对抗布点，按同一套空间规则检查。
                GridPosition[] opposing = level.EnemyPlacements.Select(enemy => enemy.Position)
                    .Concat(level.Terrain.Where(tile => tile.Kind == LevelTerrainKind.TowerMechanism).Select(tile => tile.Position))
                    .ToArray();
                bool surroundsSpawn = opposing.Any(position => position.X <= level.HeroSpawn.X);
                int rightCenter = level.Width / 2;
                bool crossesMapCenter = opposing.Any(position => position.X < rightCenter) &&
                    opposing.Any(position => position.X >= rightCenter);
                Assert.That(surroundsSpawn || crossesMapCenter, Is.True, level.Id + " must not reduce to a far-right enemy wall");
            }
        }

        [Test]
        public void EveryLevel_HasConnectedWalkableSpaceForMeleeRangedAndGeneralistOpenings()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(level);
                foreach (LevelOpeningProfile profile in Enum.GetValues(typeof(LevelOpeningProfile)))
                    Assert.That(level.SpaceContract.SupportedOpenings, Does.Contain(profile), level.Id + ":" + profile);

                GridPosition[] firstSteps = WalkableNeighbors(build.State.Map, level.HeroSpawn).ToArray();
                Assert.That(firstSteps, Has.Length.GreaterThanOrEqualTo(2), level.Id + " has a spawn soft lock");
                GridPosition[] walkable = AllPositions(build.State.Map).Where(position => !build.State.Map.IsBlocked(position)).ToArray();
                Assert.That(ReachablePositions(build.State.Map, level.HeroSpawn), Is.EquivalentTo(walkable),
                    level.Id + " contains walkable cells disconnected from the actual playable area");
            }
        }

        [Test]
        public void LootChest_PlacementMarksBlockingPositionWithoutDefiningContents()
        {
            FirstRegionLevelDefinition source = FirstRegionLevelCatalog.RainLanternCourt;
            GridPosition chest = new GridPosition(3, 3);
            FirstRegionLevelDefinition level = new FirstRegionLevelDefinition("loot_chest_probe", "宝箱位置测试", "", CombatObjectiveType.Elimination,
                1, source.HeroSpawn, source.FloorTheme, false, false, Array.Empty<string>(), source.EnemyPlacements,
                source.Terrain.Concat(new[] { new LevelTerrainPlacement(chest.X, chest.Y, LevelTerrainKind.LootChest) }),
                source.SpaceContract, source.Width, source.Height, source.BlockedPositions);

            CombatState state = FirstRegionLevelBuilder.Build(level).State;
            Assert.That(state.Map.GetTile(chest).IsLootChest, Is.True);
            Assert.That(state.Map.IsBlocked(chest), Is.True);
            Assert.That(state.LootSource, Is.Null, "地图物件只定义位置，不在关卡配置中定义宝箱内容。");
        }

        [Test]
        public void FirstElite_PressureCrystalUsesHeavyDurabilityAndSharedBurst()
        {
            FirstRegionLevelDefinition elite = FirstRegionLevelCatalog.ThreeMaterialPressure;
            GridPosition position = elite.Terrain.Single(tile => tile.Kind == LevelTerrainKind.PressureCrystal).Position;
            CombatState state = FirstRegionLevelBuilder.Build(elite).State;
            TileState crystal = state.Map.GetTile(position);
            Assert.That(crystal.IsAetherCrystal && crystal.IsPressureCrystal, Is.True);
            Assert.That(crystal.Durability, Is.EqualTo(24));
            Assert.That(crystal.ObjectName(), Is.EqualTo("精英稳压晶簇"));
            Assert.That(crystal.BlocksMovement, Is.True);

            crystal.Durability = 0;
            Assert.That(state.ResolveAetherCrystalDamage(position, 24), Is.True);
            Assert.That(crystal.IsAetherCrystal || crystal.IsPressureCrystal, Is.False);
            Assert.That(state.Map.GetTile(position).IsCrystalShard, Is.True);
            Assert.That(state.Map.GetTile(position + new GridPosition(1, 0)).IsCrystalShard, Is.True);

            GridPosition ordinaryPosition = FirstRegionLevelCatalog.GreenhouseCollectionRoom.Terrain
                .First(tile => tile.Kind == LevelTerrainKind.AetherCrystal).Position;
            TileState ordinary = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.GreenhouseCollectionRoom)
                .State.Map.GetTile(ordinaryPosition);
            Assert.That(ordinary.Durability, Is.EqualTo(16));
            Assert.That(ordinary.IsPressureCrystal, Is.False);
            Assert.That(ordinary.ObjectName(), Is.EqualTo("蓄能晶簇"));
        }

        [Test]
        public void MapDevices_UseTheirConfiguredDurability()
        {
            FirstRegionLevelDefinition source = GeneratedBattleMapCatalog.For("calibration_lockdown");
            Assert.That(source.Terrain.Where(tile => tile.Kind == LevelTerrainKind.OverloadDevice ||
                tile.Kind == LevelTerrainKind.WardGenerator).All(tile => tile.Durability == 16), Is.True);
            LevelTerrainPlacement[] customized = source.Terrain.Select(tile =>
                tile.Kind == LevelTerrainKind.WardGenerator
                    ? new LevelTerrainPlacement(tile.Position.X, tile.Position.Y, tile.Kind, 0, 0, 11)
                    : tile.Kind == LevelTerrainKind.OverloadDevice
                        ? new LevelTerrainPlacement(tile.Position.X, tile.Position.Y, tile.Kind, 0, 0, 7)
                        : tile).ToArray();
            var level = new FirstRegionLevelDefinition(source.Id, source.DisplayName, source.ObjectiveSummary,
                source.ObjectiveType, source.Tier, source.HeroSpawn, source.FloorTheme, source.IsElite,
                source.IsBoss, source.PrerequisiteLevelIds, source.EnemyPlacements, customized,
                source.SpaceContract, source.Width, source.Height, source.BlockedPositions);
            CombatState state = FirstRegionLevelBuilder.Build(level).State;

            Assert.That(state.Map.GetTile(new GridPosition(1, 1)).Durability, Is.EqualTo(11));
            Assert.That(state.Map.GetTile(new GridPosition(1, 4)).Durability, Is.EqualTo(7));
        }

        [Test]
        public void MapTraceAndBindingMark_ExpireByTheirConfiguredHeroTurnDurations()
        {
            FirstRegionLevelDefinition source = GeneratedBattleMapCatalog.For("library_discipline");
            LevelTerrainPlacement mark = source.Terrain.Single(tile => tile.Kind == LevelTerrainKind.BindingMark);
            Assert.That(mark.Duration, Is.EqualTo(3));
            CombatState baseState = FirstRegionLevelBuilder.Build(source).State;
            GridPosition trace = baseState.Map.PositionsWith(tile => !tile.HasEffectLayer && !tile.BlocksMovement)
                .First(cell => cell != source.HeroSpawn &&
                    source.EnemyPlacements.All(enemy => enemy.Position != cell));
            var level = new FirstRegionLevelDefinition(source.Id, source.DisplayName, source.ObjectiveSummary,
                source.ObjectiveType, source.Tier, source.HeroSpawn, source.FloorTheme, source.IsElite,
                source.IsBoss, source.PrerequisiteLevelIds, source.EnemyPlacements,
                source.Terrain.Concat(new[] { new LevelTerrainPlacement(trace.X, trace.Y, LevelTerrainKind.Trace, 0, 2) }),
                source.SpaceContract, source.Width, source.Height, source.BlockedPositions);
            CombatState state = FirstRegionLevelBuilder.Build(level).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            FirstRegionLevelBuilder.ApplyTimedTerrain(level, state);

            Assert.That(state.Map.GetTile(mark.Position).IsBindingMark, Is.True);
            Assert.That(state.Map.GetTile(trace).HasTrace, Is.True);
            state.AcademyEnemyArea.HeroTurnEnded(state);
            Assert.That(state.Map.GetTile(mark.Position).IsBindingMark, Is.True);
            Assert.That(state.Map.GetTile(trace).HasTrace, Is.True);
            CombatState clone = state.Clone();
            clone.AcademyEnemyArea.HeroTurnEnded(clone);
            Assert.That(clone.Map.GetTile(trace).HasTrace, Is.False);
            Assert.That(clone.Map.GetTile(mark.Position).IsBindingMark, Is.True);
            clone.AcademyEnemyArea.HeroTurnEnded(clone);
            Assert.That(clone.Map.GetTile(mark.Position).IsBindingMark, Is.False);
        }

        [Test]
        public void MapBindingMarkExpiry_DoesNotClearAnotherSourcesReplacement()
        {
            FirstRegionLevelDefinition level = GeneratedBattleMapCatalog.For("library_discipline");
            GridPosition cell = level.Terrain.Single(tile => tile.Kind == LevelTerrainKind.BindingMark).Position;
            CombatState state = FirstRegionLevelBuilder.Build(level).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyEnemyArea(new AcademyEnemyAreaRuntime());
            FirstRegionLevelBuilder.ApplyTimedTerrain(level, state);
            TileState replaced = state.Map.GetTile(cell).Clone();
            replaced.ClearEffectLayers();
            replaced.IsBindingMark = true;
            replaced.EffectSourceId = "skill:SK-SUP-08";
            state.Map.SetTile(cell, replaced);

            for (int turn = 0; turn < 3; turn++) state.AcademyEnemyArea.HeroTurnEnded(state);

            Assert.That(state.Map.GetTile(cell).IsBindingMark, Is.True);
            Assert.That(state.Map.GetTile(cell).EffectSourceId, Is.EqualTo("skill:SK-SUP-08"));
        }

        [Test]
        public void EveryLevel_HasNoSingleCellSoftLockOrUnavoidableSpawnCrossfire()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                CombatState state = FirstRegionLevelBuilder.Build(level).State;
                foreach (GridPosition candidate in AllPositions(state.Map).Where(position => position != level.HeroSpawn))
                {
                    if (state.Map.IsBlocked(candidate)) continue;
                    Assert.That(ReachablePositions(state.Map, level.HeroSpawn, candidate).Count, Is.GreaterThan(1),
                        level.Id + " can be soft-locked at spawn by one cell at " + candidate);
                }

                UnitState hero = state.GetUnit("hero");
                int immediateThreats = state.Units.Values.Where(unit => !unit.IsHero)
                    .Select(enemy => EnemyTactics.Choose(state, enemy, hero))
                    .Count(command => command.TargetUnitId == hero.Id &&
                        (command.Type == CombatCommandType.Attack || command.Type == CombatCommandType.UseSkill));
                Assert.That(immediateThreats, Is.LessThanOrEqualTo(1), level.Id + " starts inside unavoidable overlapping attacks");
            }
        }

        [Test]
        public void DestructionLevels_CompleteImmediatelyWhenTheirPublicTargetIsDestroyed()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All.Where(level => level.ObjectiveType == CombatObjectiveType.Destruction))
            {
                CombatState state = FirstRegionLevelBuilder.Build(level).State;
                Assert.That(state.Units.Values.Any(unit => !unit.IsHero && unit.IsAlive), Is.True, level.Id);
                foreach (GridPosition position in state.Map.PositionsWith(tile => tile.IsObjective))
                    state.Map.GetTile(position).Durability = 0;
                state.ConfigureObjectives(state.Objectives.ToArray());
                Assert.That(state.IsVictory, Is.True, level.Id + " must finish without a hidden post-objective cleanup phase");
            }
        }

        [Test]
        public void AllTenExpansionEnemies_AreReachableThroughRealLevelBuilds()
        {
            string[] active = FirstRegionLevelCatalog.All.SelectMany(level => FirstRegionLevelBuilder.Build(level).State.Units.Values)
                .Where(unit => !unit.IsHero).Select(unit => unit.EnemyArchetypeId).Distinct(StringComparer.Ordinal).ToArray();
            Assert.That(PackIds.Except(active), Is.Empty);
        }

        [Test]
        public void LegacyEncounterApi_IsDerivedFromLevelCatalog()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.For(level.Id, "purifier_overseer");
                Assert.That(encounter.NodeId, Is.EqualTo(level.Id));
                Assert.That(encounter.IsElite, Is.EqualTo(level.IsElite));
                Assert.That(encounter.IsBoss, Is.EqualTo(level.IsBoss));
                Assert.That(encounter.EnemyArchetypeIds, Is.EqualTo(level.ResolveEnemyArchetypeIds("purifier_overseer")));
            }
        }

        [Test]
        public void Finale_AlwaysResolvesFixedAcademyCoreBossRegardlessOfLegacyOverride()
        {
            FirstRegionLevelDefinition finale = FirstRegionLevelCatalog.For("core_finale");
            CombatState core = FirstRegionLevelBuilder.Build(finale, "core_overseer").State;
            CombatState purifier = FirstRegionLevelBuilder.Build(finale, "purifier_overseer").State;
            Assert.That(core.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("core_overseer"));
            Assert.That(purifier.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("core_overseer"));
            for (int index = 1; index < finale.EnemyPlacements.Count; index++)
                Assert.That(core.GetUnit("enemy_" + index).EnemyArchetypeId, Is.EqualTo(purifier.GetUnit("enemy_" + index).EnemyArchetypeId));
        }

        [Test]
        public void LevelDefinitions_HaveNoCountdownOrTimePressureContract()
        {
            string[] memberNames = typeof(FirstRegionLevelDefinition).GetProperties().Select(property => property.Name)
                .Concat(typeof(FirstRegionLevelDefinition).GetFields().Select(field => field.Name)).ToArray();
            foreach (string forbidden in new[] { "Timer", "Countdown", "Deadline", "TimeLimit" })
                Assert.That(memberNames.Any(name => name.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0), Is.False, forbidden);
        }

        [Test]
        public void ActiveMapNodeLabels_MatchEraCorrectLevelNames()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
                Assert.That(RogueliteMapCatalog.Node(level.Id).DisplayName, Is.EqualTo(level.DisplayName), level.Id);
        }

        [Test]
        public void EveryPlacedEnemy_CanProduceDeterministicTacticalCommand()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                CombatState state = FirstRegionLevelBuilder.Build(level).State;
                UnitState hero = state.GetUnit("hero");
                foreach (UnitState enemy in state.Units.Values.Where(unit => !unit.IsHero))
                {
                    CombatCommand first = EnemyTactics.Choose(state, enemy, hero);
                    CombatCommand second = EnemyTactics.Choose(state, enemy, hero);
                    Assert.That(first.Type, Is.EqualTo(second.Type), level.Id + ":" + enemy.EnemyArchetypeId);
                    Assert.That(first.UnitId, Is.EqualTo(enemy.Id), level.Id + ":" + enemy.EnemyArchetypeId);
                }
            }
        }

        [Test]
        public void FirstPhaseFollowupBattles_AreWinnableWithNormalCommands()
        {
            foreach (string levelId in new[]
            {
                FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id,
                FirstRegionLevelCatalog.RainPrismCourt.Id,
                FirstRegionLevelCatalog.ThreeMaterialPressure.Id
            })
            {
                CombatState state = FirstRegionLevelBuilder.Build(levelId).State;
                UnitState hero = state.GetUnit("hero");
                hero.Equip(CombatCatalog.Rifle, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
                hero.ConfigureMana(12, 12);
                int commands = 0;
                CombatResolver.BeginTurn(state, hero.Id);

                while (!state.IsVictory && !state.IsDefeat && commands < 200)
                {
                    UnitState unit = state.GetUnit(state.ActiveUnitId);
                    if (unit == null || !unit.IsAlive)
                    {
                        CombatResolver.AdvanceToNextTurn(state);
                        continue;
                    }
                    if (unit.ActionPoints <= 0)
                    {
                        CombatResolver.EndTurn(state, unit);
                        commands++;
                        continue;
                    }

                    CombatCommand command = unit.IsHero
                        ? FirstPhaseHeroCommand(state, unit)
                        : new EnemyTurnPlanBook().GetExecutionCommand(state, unit, hero);
                    if (command.Type == CombatCommandType.EndTurn) CombatResolver.EndTurn(state, unit);
                    else CombatResolver.Resolve(state, command);
                    commands++;
                }

                Assert.That(state.IsVictory, Is.True, levelId + " should be winnable with normal movement, interaction, skills, and attacks.");
                Assert.That(commands, Is.LessThan(200), levelId);
            }
        }

        private static CombatCommand FirstPhaseHeroCommand(CombatState state, UnitState hero)
        {
            UnitState enemy = state.Units.Values.Where(unit => !unit.IsHero && unit.IsAlive)
                .OrderBy(unit => unit.Position.ManhattanDistance(hero.Position)).ThenBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();
            if (enemy != null)
            {
                int distance = hero.Position.ManhattanDistance(enemy.Position);
                SkillDefinition skill = hero.SkillOne;
                if (skill != null && hero.Mana >= skill.ManaCost && hero.IsSkillReady(skill) &&
                    distance >= skill.MinimumRange && distance <= skill.Range &&
                    CombatResolver.PreviewSkillAttack(state, hero.Id, enemy.Id, skill).HasLineOfSight)
                    return CombatCommand.UseSkill(hero.Id, 0, enemy.Id);
                WeaponDefinition weapon = hero.MainHand ?? CombatCatalog.Rifle;
                if (distance >= weapon.MinimumRange && distance <= weapon.Range &&
                    CombatResolver.PreviewAttack(state, hero.Id, enemy.Id, false).HasLineOfSight)
                    return CombatCommand.Attack(hero.Id, enemy.Id);
            }

            GridPosition crystal = state.Map.PositionsWith(tile => tile.Durability > 0 && tile.IsAetherCrystal)
                .OrderBy(position => position.ManhattanDistance(hero.Position)).FirstOrDefault();
            if (state.Map.IsInside(crystal))
            {
                if (hero.Position.ManhattanDistance(crystal) == 1) return CombatCommand.Interact(hero.Id, crystal);
                foreach (GridPosition approach in WalkableNeighbors(state.Map, crystal).Where(position => !state.IsOccupied(position, hero.Id)))
                {
                    CombatCommand? move = MoveToward(state, hero, approach);
                    if (move.HasValue) return move.Value;
                }
            }

            if (enemy != null)
                foreach (GridPosition approach in WalkableNeighbors(state.Map, enemy.Position).Where(position => !state.IsOccupied(position, hero.Id)))
                {
                    CombatCommand? move = MoveToward(state, hero, approach);
                    if (move.HasValue) return move.Value;
                }
            return CombatCommand.EndTurn(hero.Id);
        }

        private static CombatCommand? MoveToward(CombatState state, UnitState hero, GridPosition destination)
        {
            IReadOnlyList<GridPosition> wholePath = state.Map.FindLowestCostPath(hero.Position, destination, 100,
                position => CombatMovementQuery.EntryCost(state, hero, position), position => state.IsOccupied(position, hero.Id));
            foreach (GridPosition position in wholePath.Reverse())
                if (CombatMovementQuery.FindPath(state, hero, position).Count > 1)
                    return CombatCommand.Move(hero.Id, position);
            return null;
        }

        private static HashSet<GridPosition> ReachablePositions(GridMap map, GridPosition start, GridPosition? additionallyBlocked = null)
        {
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            HashSet<GridPosition> visited = new HashSet<GridPosition> { start };
            frontier.Enqueue(start);
            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                foreach (GridPosition next in WalkableNeighbors(map, current))
                    if ((!additionallyBlocked.HasValue || next != additionallyBlocked.Value) && visited.Add(next)) frontier.Enqueue(next);
            }
            return visited;
        }

        private static IEnumerable<GridPosition> WalkableNeighbors(GridMap map, GridPosition position)
        {
            foreach (GridPosition direction in new[]
            {
                new GridPosition(1, 0), new GridPosition(-1, 0), new GridPosition(0, 1), new GridPosition(0, -1)
            })
            {
                GridPosition next = position + direction;
                if (map.IsInside(next) && !map.IsBlocked(next)) yield return next;
            }
        }

        private static IEnumerable<GridPosition> AllPositions(GridMap map)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    yield return new GridPosition(x, y);
        }
    }
}
