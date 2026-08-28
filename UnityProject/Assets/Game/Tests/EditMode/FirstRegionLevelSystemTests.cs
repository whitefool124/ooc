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
                Assert.That(state.Map.Width, Is.EqualTo(12), level.Id);
                Assert.That(state.Map.Height, Is.EqualTo(9), level.Id);
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
                bool surroundsSpawn = level.EnemyPlacements.Any(enemy => enemy.Position.X <= level.HeroSpawn.X);
                bool crossesMapCenter = level.EnemyPlacements.Any(enemy => enemy.Position.X <= 5) &&
                    level.EnemyPlacements.Any(enemy => enemy.Position.X >= 6);
                Assert.That(surroundsSpawn || crossesMapCenter, Is.True, level.Id + " must not reduce to a far-right enemy wall");
            }
        }

        [Test]
        public void EveryLevel_HasTwoReachableRoutesForMeleeRangedAndGeneralistOpenings()
        {
            foreach (FirstRegionLevelDefinition level in FirstRegionLevelCatalog.All)
            {
                FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(level);
                Assert.That(level.SpaceContract.RouteAnchors.Count, Is.GreaterThanOrEqualTo(2), level.Id);
                foreach (LevelOpeningProfile profile in Enum.GetValues(typeof(LevelOpeningProfile)))
                {
                    Assert.That(level.SpaceContract.SupportedOpenings, Does.Contain(profile), level.Id + ":" + profile);
                    foreach (GridPosition anchor in level.SpaceContract.RouteAnchors)
                        Assert.That(IsReachable(build.State.Map, level.HeroSpawn, anchor), Is.True, level.Id + ":" + profile + ":" + anchor);
                }

                GridPosition[] firstSteps = WalkableNeighbors(build.State.Map, level.HeroSpawn).ToArray();
                Assert.That(firstSteps, Has.Length.GreaterThanOrEqualTo(2), level.Id + " has a spawn soft lock");
                bool distinctEntrances = firstSteps.Any(first => IsReachable(build.State.Map, first, level.SpaceContract.RouteAnchors[0], level.HeroSpawn) &&
                    firstSteps.Any(second => second != first && IsReachable(build.State.Map, second, level.SpaceContract.RouteAnchors[1], level.HeroSpawn)));
                Assert.That(distinctEntrances, Is.True, level.Id + " routes collapse into one opening cell");
            }
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
                    Assert.That(level.SpaceContract.RouteAnchors.Any(anchor => anchor != candidate &&
                        IsReachable(state.Map, level.HeroSpawn, anchor, candidate)), Is.True,
                        level.Id + " can be soft-locked by one cell at " + candidate);
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

        private static bool IsReachable(GridMap map, GridPosition start, GridPosition destination, GridPosition? additionallyBlocked = null)
        {
            if (additionallyBlocked.HasValue && (start == additionallyBlocked.Value || destination == additionallyBlocked.Value)) return false;
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            HashSet<GridPosition> visited = new HashSet<GridPosition> { start };
            frontier.Enqueue(start);
            GridPosition[] directions =
            {
                new GridPosition(1, 0), new GridPosition(-1, 0), new GridPosition(0, 1), new GridPosition(0, -1)
            };
            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                if (current == destination) return true;
                foreach (GridPosition direction in directions)
                {
                    GridPosition next = current + direction;
                    if (map.IsInside(next) && !map.IsBlocked(next) && (!additionallyBlocked.HasValue || next != additionallyBlocked.Value) && visited.Add(next))
                        frontier.Enqueue(next);
                }
            }
            return false;
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
