using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteEncounterPoolTests
    {
        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }

        [Test]
        public void Catalog_ContainsSixTwoUnitWeakTwelveStrongSixEliteAndFixedBoss()
        {
            Assert.That(RogueliteEncounterCatalog.WeakPool.Count, Is.EqualTo(6));
            Assert.That(RogueliteEncounterCatalog.WeakPool.All(value => value.EnemyArchetypeIds.Count == 2), Is.True);
            Assert.That(RogueliteEncounterCatalog.StrongPool.Count, Is.EqualTo(12));
            Assert.That(RogueliteEncounterCatalog.StrongPool.All(value => value.EnemyArchetypeIds.Count == 3), Is.True);
            Assert.That(RogueliteEncounterCatalog.ElitePool.Count, Is.EqualTo(6));
            Assert.That(RogueliteEncounterCatalog.FixedBoss.VariantKey, Is.EqualTo("boss_academy_sealed_core"));
            Assert.That(RogueliteEncounterCatalog.FixedBoss.EnemyArchetypeIds[0], Is.EqualTo("core_overseer"));
        }

        [Test]
        public void WeakLayouts_AreFormalTwelveByNineAndCoverW1ThroughW4()
        {
            string[] signatures = RogueliteEncounterCatalog.WeakPool.Select(value => value.Layout.Signature).Distinct().OrderBy(value => value).ToArray();
            Assert.That(signatures, Is.EqualTo(new[] { "W1", "W2", "W3", "W4" }));
            foreach (RogueliteEncounterDefinition package in RogueliteEncounterCatalog.WeakPool)
            {
                Assert.That(package.Layout.Width, Is.EqualTo(12), package.VariantKey);
                Assert.That(package.Layout.Height, Is.EqualTo(9), package.VariantKey);
                Assert.That(package.Layout.EnemySpawns.Count, Is.EqualTo(2), package.VariantKey);
                Assert.That(package.MaximumOpeningThreatOverlap, Is.LessThanOrEqualTo(1), package.VariantKey);
                Assert.That(package.Layout.Terrain.Any(value => value.Kind == LevelTerrainKind.AetherObjective), Is.False, package.VariantKey);
            }
        }

        [TestCase(1)]
        [TestCase(37)]
        [TestCase(701)]
        public void FixedSeedAssignment_IsValidDeterministicAndHasTwoOpeningWeakEncounters(int seed)
        {
            RogueliteMapRun first = new RogueliteMapRun(seed);
            RogueliteMapRun second = new RogueliteMapRun(seed);
            CollectionAssert.AreEqual(first.EncounterAssignments.OrderBy(value => value.Key).Select(value => value.Key + "=" + value.Value),
                second.EncounterAssignments.OrderBy(value => value.Key).Select(value => value.Key + "=" + value.Value));
            Assert.That(RogueliteMapRunValidator.Validate(first).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(first).Summary);
            int openingWeak = first.EncounterAssignments.Count(value => RogueliteEncounterCatalog.IsAdjacent(value.Key, "start") &&
                RogueliteEncounterCatalog.Package(value.Value).Tier == RogueliteEncounterTier.Weak);
            Assert.That(openingWeak, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Rogue11Save_RoundTripsExactAssignmentsWithoutReroll()
        {
            RogueliteMapRun source = new RogueliteMapRun(812);
            MemoryStore store = new MemoryStore(); RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(gateway.SaveMapRun(source), Is.True, gateway.LastError);
            string written = store.Values[RogueliteSaveGateway.MapRunKey];
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun restored), Is.True, gateway.LastError);
            CollectionAssert.AreEqual(source.EncounterAssignments.OrderBy(value => value.Key).Select(value => value.Key + "=" + value.Value),
                restored.EncounterAssignments.OrderBy(value => value.Key).Select(value => value.Key + "=" + value.Value));
            Assert.That(restored.RegionBossId, Is.EqualTo("core_overseer"));
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(written));
        }

        [Test]
        public void WeakAndStrongPackages_BuildExactEnemyCountsAndConsistentObjectives()
        {
            RogueliteEncounterDefinition weak = RogueliteEncounterCatalog.Package("weak_fire_drill");
            FirstRegionLevelDefinition weakLevel = CombatSceneSessionBuilder.BindEncounterToLevel(FirstRegionLevelCatalog.For(weak.LevelId), weak);
            FirstRegionLevelBuild weakBuild = FirstRegionLevelBuilder.Build(weakLevel);
            Assert.That(weakBuild.State.Units.Values.Count(value => !value.IsHero), Is.EqualTo(2));
            Assert.That(weakLevel.ObjectiveType, Is.EqualTo(CombatObjectiveType.Elimination));
            Assert.That(weakLevel.ObjectiveSummary, Does.Contain("认输"));
            Assert.That(weakLevel.Terrain.Any(value => value.Kind == LevelTerrainKind.AetherObjective), Is.False);

            RogueliteEncounterDefinition strong = RogueliteEncounterCatalog.Package("strong_relay_raid_a");
            FirstRegionLevelDefinition strongLevel = CombatSceneSessionBuilder.BindEncounterToLevel(FirstRegionLevelCatalog.For(strong.LevelId), strong);
            FirstRegionLevelBuild strongBuild = FirstRegionLevelBuilder.Build(strongLevel);
            Assert.That(strongBuild.State.Units.Values.Count(value => !value.IsHero), Is.EqualTo(3));
            Assert.That(strongLevel.ObjectiveType, Is.EqualTo(CombatObjectiveType.Destruction));
            Assert.That(strongLevel.Terrain.Any(value => value.Kind == LevelTerrainKind.AetherObjective), Is.True);
        }

        [Test]
        public void EveryFormalEncounterPackage_BindsAndBuildsWithValidUniqueUnitSpawns()
        {
            foreach (RogueliteEncounterDefinition encounter in RogueliteEncounterCatalog.Packages)
            {
                FirstRegionLevelDefinition source = FirstRegionLevelCatalog.For(encounter.LevelId);
                FirstRegionLevelDefinition bound = CombatSceneSessionBuilder.BindEncounterToLevel(source, encounter);
                FirstRegionLevelBuild build = null;
                Assert.DoesNotThrow(() => build = FirstRegionLevelBuilder.Build(bound, "core_overseer"), encounter.VariantKey);
                Assert.That(build.State.Units.Values.All(unit => build.State.Map.IsInside(unit.Position)), Is.True, encounter.VariantKey);
                Assert.That(build.State.Units.Values.All(unit => !build.State.Map.IsBlocked(unit.Position)), Is.True, encounter.VariantKey);
                Assert.That(build.State.Units.Values.Select(unit => unit.Position).Distinct().Count(),
                    Is.EqualTo(build.State.Units.Count), encounter.VariantKey);
            }
        }

        [Test]
        public void WeakW2Layout_DoesNotInheritGatehouseDaisBlockersAcrossLayoutBoundary()
        {
            RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.Package("weak_arbalist_calibration");
            FirstRegionLevelDefinition source = FirstRegionLevelCatalog.For(encounter.LevelId);
            Assert.That(source.BlockedPositions, Does.Contain(new GridPosition(4, 7)));

            FirstRegionLevelDefinition bound = CombatSceneSessionBuilder.BindEncounterToLevel(source, encounter);
            FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(bound);

            Assert.That(bound.Width, Is.EqualTo(encounter.Layout.Width));
            Assert.That(bound.Height, Is.EqualTo(encounter.Layout.Height));
            Assert.That(bound.BlockedPositions, Is.EquivalentTo(encounter.Layout.BlockedPositions));
            Assert.That(bound.HeroSpawn, Is.EqualTo(new GridPosition(4, 7)));
            Assert.That(build.State.Map.IsBlocked(bound.HeroSpawn), Is.False);
        }

        [Test]
        public void EncounterWithoutLayout_PreservesBaseMapDimensionsAndPermanentBlockers()
        {
            RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.Package("strong_gatehouse_a");
            FirstRegionLevelDefinition source = FirstRegionLevelCatalog.For(encounter.LevelId);
            FirstRegionLevelDefinition bound = CombatSceneSessionBuilder.BindEncounterToLevel(source, encounter);

            Assert.That(encounter.Layout, Is.Null);
            Assert.That(bound.Width, Is.EqualTo(source.Width));
            Assert.That(bound.Height, Is.EqualTo(source.Height));
            Assert.That(bound.BlockedPositions, Is.EquivalentTo(source.BlockedPositions));
        }

        [Test]
        public void NonLevelIdMapNode_UsesAssignedLevelAndBuildsWithoutMarkerFallback()
        {
            RogueliteMapRun run = new RogueliteMapRun(913);
            Assert.That(run.IsNodeAvailable("tutorial_hall"), Is.True);
            run.SelectNode("tutorial_hall");
            RogueliteEncounterDefinition assigned = RogueliteEncounterCatalog.For(run, "tutorial_hall");
            CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(run, null, Array.Empty<CombatSceneMarker>());
            Assert.That(build, Is.Not.Null);
            Assert.That(build.Level.Id, Is.EqualTo(assigned.LevelId));
            Assert.That(build.State.Units.Values.Count(value => !value.IsHero), Is.EqualTo(assigned.EnemyArchetypeIds.Count));
        }

        [Test]
        public void UnknownFormalEncounter_ThrowsInsteadOfSilentlyFillingEnemies()
        {
            RogueliteMapRun run = new RogueliteMapRun(1001);
            Assert.Throws<InvalidOperationException>(() => RogueliteEncounterCatalog.For(run, "unregistered_encounter"));
        }

        [Test]
        public void Preview_ExposesTierEnemiesSpaceRiskAndHigherStrongRewardTier()
        {
            MemoryStore store = new MemoryStore(); RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(gateway.SaveMapRun(new RogueliteMapRun(1117)), Is.True, gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun run), Is.True, gateway.LastError);
            RogueliteMapNode weakNode = RogueliteMapCatalog.Nodes.First(node => run.TryGetEncounter(node.Id, out RogueliteEncounterDefinition value) && value.Tier == RogueliteEncounterTier.Weak);
            RogueliteMapNode strongNode = RogueliteMapCatalog.Nodes.First(node => run.TryGetEncounter(node.Id, out RogueliteEncounterDefinition value) && value.Tier == RogueliteEncounterTier.Strong);
            RogueNodePreviewPresentation weak = new RogueNodePreviewPresentation(run, weakNode);
            RogueNodePreviewPresentation strong = new RogueNodePreviewPresentation(run, strongNode);
            Assert.That(weak.EncounterLabel, Is.EqualTo("轻松"));
            Assert.That(strong.EncounterLabel, Is.EqualTo("棘手"));
            Assert.That(weak.EnemySummary, Is.Not.Empty); Assert.That(strong.SpatialRisk, Is.Not.Empty);
            Assert.That(strong.RewardLabel, Does.Contain("更好的奖励"));
            Assert.That(strong.RewardLabel, Is.Not.EqualTo(weak.RewardLabel));
        }
    }
}
