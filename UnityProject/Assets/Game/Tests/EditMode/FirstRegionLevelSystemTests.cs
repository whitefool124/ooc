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
            }
        }

        [Test]
        public void NineLevels_HaveDistinctTacticalLayoutSignatures()
        {
            string[] signatures = FirstRegionLevelCatalog.All.Select(level => string.Join(";",
                level.EnemyPlacements.Select(enemy => "E:" + enemy.ArchetypeId + "@" + enemy.Position.X + "," + enemy.Position.Y)
                    .Concat(level.Terrain.Select(tile => "T:" + tile.Kind + "@" + tile.Position.X + "," + tile.Position.Y))
                    .OrderBy(value => value, StringComparer.Ordinal))).ToArray();
            Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
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
        public void Finale_ResolvesSeedSelectedBossWithoutChangingOtherPlacements()
        {
            FirstRegionLevelDefinition finale = FirstRegionLevelCatalog.For("core_finale");
            CombatState core = FirstRegionLevelBuilder.Build(finale, "core_overseer").State;
            CombatState purifier = FirstRegionLevelBuilder.Build(finale, "purifier_overseer").State;
            Assert.That(core.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("core_overseer"));
            Assert.That(purifier.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("purifier_overseer"));
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
    }
}
