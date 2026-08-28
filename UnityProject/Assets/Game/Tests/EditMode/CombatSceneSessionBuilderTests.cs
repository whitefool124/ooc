using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatSceneSessionBuilderTests
    {
        [Test]
        public void FormalMapRun_BuildsLevelPreparationInventoryAndLoot()
        {
            RogueliteMapRun run = new RogueliteMapRun(501, FireRogueliteStarterCatalog.Ranged);
            run.SelectNode("rail_patrol");
            RogueliteEncounterDefinition assigned = RogueliteEncounterCatalog.For(run, "rail_patrol");

            CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(
                run, null, Array.Empty<CombatSceneMarker>());

            Assert.That(build, Is.Not.Null);
            Assert.That(build.Level.Id, Is.EqualTo(assigned.LevelId));
            Assert.That(build.Preparation.MissionId, Is.EqualTo(assigned.LevelId));
            Assert.That(build.Preparation.EnemySummary, Does.Contain(EnemyArchetypes.Get(assigned.EnemyArchetypeIds[0]).DisplayName));
            Assert.That(build.State.GetUnit("hero"), Is.Not.Null);
            Assert.That(build.State.ItemQuickbar.Take(2).All(id => !string.IsNullOrEmpty(id)), Is.True);
            Assert.That(build.State.LootSource.Id, Is.EqualTo("rail_patrol-relay-crate"));
        }

        [Test]
        public void ShortRunSecondCombat_AppliesAllPriorChoicesDuringBuild()
        {
            ShortRogueliteRun shortRun = new ShortRogueliteRun(502);
            shortRun.CompleteCombat();
            shortRun.ChooseEvent("field_repair");
            shortRun.ChooseSalvage("shield_cell");
            shortRun.ChooseUpgrade("calibrated_rifle");

            List<GameObject> objects = new List<GameObject>();
            try
            {
                CombatSceneMarker heroMarker = Marker(objects, "主角_测试", CombatSceneMarkerType.Unit, 1, 1);
                CombatSceneMarker objectiveMarker = Marker(objects, "目标_测试", CombatSceneMarkerType.Objective, 4, 1);
                CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(
                    null, new RogueliteDeveloperRun(shortRun), new[] { heroMarker, objectiveMarker });

                UnitState hero = build.State.GetUnit("hero");
                Assert.That(build.Preparation.MissionId, Is.EqualTo("factory_breach"));
                Assert.That(hero.MainHand.Id, Is.EqualTo(StageTwoBuilds.CalibratedRifle.Id));
                Assert.That(hero.Armor, Is.GreaterThan(0));
                Assert.That(build.State.ItemQuickbar.Take(3).All(id => !string.IsNullOrEmpty(id)), Is.True);
                Assert.That(build.State.LootSource.Id, Is.EqualTo("relay-crate"));
            }
            finally
            {
                foreach (GameObject value in objects) UnityEngine.Object.DestroyImmediate(value);
            }
        }

        [Test]
        public void PrototypeMarkerFallback_PreservesSuppliedPreparation()
        {
            List<GameObject> objects = new List<GameObject>();
            try
            {
                CombatSceneMarker heroMarker = Marker(objects, "主角_回退", CombatSceneMarkerType.Unit, 1, 1);
                MissionPreparation fallback = new MissionPreparation().Configure(
                    "relay_test", "破坏任务目标并清理威胁", "测试编成");

                CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(
                    null, null, new[] { heroMarker }, fallback);

                Assert.That(build.Preparation.MissionId, Is.EqualTo("relay_test"));
                Assert.That(build.Preparation, Is.Not.SameAs(fallback));
                Assert.That(build.State.GetUnit("hero"), Is.Not.Null);
            }
            finally
            {
                foreach (GameObject value in objects) UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private static CombatSceneMarker Marker(ICollection<GameObject> objects, string name,
            CombatSceneMarkerType type, int x, int y)
        {
            GameObject value = new GameObject(name);
            objects.Add(value);
            value.transform.position = new Vector3(x, y, 0);
            CombatSceneMarker marker = value.AddComponent<CombatSceneMarker>();
            marker.Configure(type, name);
            return marker;
        }
    }
}
