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
        public void FirstRunBattleTwo_BuildsItsFormalLevelAndKeepsRogueInventoryRuntime()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(1908);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1");
            run.CompleteCurrentCombat();
            run.ClaimReward(run.CurrentFirstRunRewardIds[0]);
            run.SelectNode("EV1");
            run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.SelectNode("EV2");
            run.ChooseCurrentNodeContent("FIRST-EV2-GOLD");
            run.SelectNode("B2");

            CombatSceneSessionBuild build = new CombatSceneSessionBuilder().Build(
                run, null, Array.Empty<CombatSceneMarker>());

            Assert.That(build, Is.Not.Null);
            Assert.That(build.Level.Id, Is.EqualTo("first_battle_greenhouse_collection_room"));
            Assert.That(build.Preparation.MissionId, Is.EqualTo(build.Level.Id));
            Assert.That(build.State.RogueEquipment, Is.Not.Null);
            Assert.That(build.State.RogueEquipment.Backpack, Is.Not.Null);
            Assert.That(build.State.RogueEquipment.ItemQuickbarInstanceIds.Length, Is.EqualTo(4));
        }

        [Test]
        public void FirstRunBattleThreeAndElite_BuildFormalSessionsThroughTheNormalFlow()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(1910);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1"); run.CompleteCurrentCombat(); run.ClaimReward("F-P-M03");
            run.SelectNode("EV1"); run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.SelectNode("EV2"); run.ChooseCurrentNodeContent("FIRST-EV2-GOLD");
            run.SelectNode("B2"); run.CompleteCurrentCombat(); run.ClaimReward("F-P-U04");
            run.SelectNode("EV3"); run.ChooseCurrentNodeContent("FIRST-EV3-CONTRIBUTION");
            run.SelectNode("W");
            run.CompleteFirstRunForge(run.RogueRunState.EquipmentInstances[0].InstanceId);
            run.CompleteFirstRunSpecialization("F-P-M03");
            run.SelectNode("B3");

            CombatSceneSessionBuild battleThree = new CombatSceneSessionBuilder().Build(
                run, null, Array.Empty<CombatSceneMarker>());
            Assert.That(battleThree.Level.Id, Is.EqualTo(FirstRegionLevelCatalog.RainPrismCourt.Id));
            Assert.That(battleThree.State.RogueSpells, Is.Not.Null);
            Assert.That(battleThree.State.Map.GetTile(new GridPosition(6, 4)).Durability, Is.EqualTo(16));

            run.CompleteCurrentCombat(); run.ClaimReward("ACA-EQ-MH03");
            run.SelectNode("M"); run.CompleteFirstRunHealthCheck();
            run.SelectNode("X");
            CombatSceneSessionBuild elite = new CombatSceneSessionBuilder().Build(
                run, null, Array.Empty<CombatSceneMarker>());
            Assert.That(elite.Level.Id, Is.EqualTo(FirstRegionLevelCatalog.ThreeMaterialPressure.Id));
            Assert.That(elite.State.ThreeMaterialPressure, Is.Not.Null);
            Assert.That(elite.State.GetUnit("enemy_0").EnemyArchetypeId, Is.EqualTo("breach_ram"));
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
