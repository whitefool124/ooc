using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteDeveloperRunTests
    {
        [Test]
        public void Catalog_OpensOnlyEliminationAndDestructionSandboxTemplates()
        {
            Assert.That(RogueliteDeveloperCatalog.OpenSandboxTemplates.Select(template => template.Type), Is.EquivalentTo(new[] { CombatObjectiveType.Elimination, CombatObjectiveType.Destruction }));
            Assert.That(RogueliteDeveloperCatalog.FindMission("factory_breach").ObjectiveType, Is.EqualTo(CombatObjectiveType.Destruction));
        }

        [Test]
        public void StoryRun_AdvancesOnlyWhenItsMissionIsCompleted()
        {
            var run = new RogueliteDeveloperRun(RogueliteStoryCatalog.CreateDefault(100));
            Assert.That(run.CurrentMission.Id, Is.EqualTo("dead_signal"));
            run.Complete("signal secured");
            Assert.That(run.Package.CurrentMissionId, Is.EqualTo("factory_breach"));
            Assert.That(run.CurrentMission.ObjectiveType, Is.EqualTo(CombatObjectiveType.Destruction));
        }

        [Test]
        public void SandboxRun_DoesNotAdvanceStoryPackage()
        {
            var run = new RogueliteDeveloperRun("elimination_rail", 200);
            run.Complete("sandbox result");
            Assert.That(run.Package.CurrentMissionIndex, Is.EqualTo(0));
            Assert.That(run.CurrentMission.Id, Is.EqualTo("sandbox_elimination"));
        }

        [Test]
        public void RogueliteSaveManager_IsolatedAndRoundTripsPackageOnly()
        {
            var save = new RogueliteSaveManager();
            var package = RogueliteStoryCatalog.CreateDefault(300);
            package.CompleteCurrentMission("first"); save.Save(package);
            Assert.That(save.HasSave("iron_echoes"), Is.True);
            Assert.That(save.Load("iron_echoes").CurrentMissionId, Is.EqualTo("factory_breach"));
            save.Delete("iron_echoes"); Assert.That(save.HasSave("iron_echoes"), Is.False);
        }

        [Test]
        public void DebugOutcome_ProducesBothObjectiveOutcomesWithoutChangingSnapshotData()
        {
            var map = new GridMap(2, 2);
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            var enemy = new UnitState("enemy", false, new GridPosition(1, 0), Facing.West);
            var victory = new CombatState(map, new[] { hero, enemy }, new CombatObjective[] { new EliminationObjective() });
            victory.ResolveDebugOutcome(true); Assert.That(victory.IsVictory, Is.True);
            var defeat = new CombatState(map, new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East), new UnitState("enemy", false, new GridPosition(1, 0), Facing.West) });
            defeat.ResolveDebugOutcome(false); Assert.That(defeat.IsDefeat, Is.True);

            var objectiveMap = new GridMap(2, 2); objectiveMap.SetTile(new GridPosition(1, 1), new TileState { IsObjective = true, Durability = 6 });
            var destruction = new CombatState(objectiveMap, new[] { new UnitState("hero", true, new GridPosition(0, 0), Facing.East) }, new CombatObjective[] { new DestructionObjective(new[] { new GridPosition(1, 1) }) });
            destruction.ResolveDebugOutcome(true); Assert.That(destruction.IsVictory, Is.True);
        }

        [Test]
        public void ShortRun_RequiresEventSalvageAndUpgradeBeforeSecondCombat()
        {
            var run = new ShortRogueliteRun(777);
            Assert.That(run.CurrentMissionId, Is.EqualTo("dead_signal"));
            run.CompleteCombat(); Assert.That(run.Phase, Is.EqualTo(ShortRoguelitePhase.Event));
            run.ChooseEvent("field_repair"); run.ChooseSalvage("shield_cell"); run.ChooseUpgrade("calibrated_rifle");
            Assert.That(run.CurrentMissionId, Is.EqualTo("factory_breach"));
            Assert.That(run.Choices, Is.EquivalentTo(new[] { "field_repair", "shield_cell", "calibrated_rifle" }));
        }

        [Test]
        public void ShortRun_RoundTripsAtEveryInterlude()
        {
            var run = new ShortRogueliteRun(88); run.CompleteCombat(); run.ChooseEvent("field_repair");
            var restored = ShortRogueliteRun.FromJson(run.ToJson());
            Assert.That(restored.Phase, Is.EqualTo(ShortRoguelitePhase.Salvage));
            restored.ChooseSalvage("shield_cell"); restored.ChooseUpgrade("calibrated_rifle"); restored.CompleteCombat();
            Assert.That(restored.Phase, Is.EqualTo(ShortRoguelitePhase.Complete));
        }

        [Test]
        public void MapRun_VisitsAdjacentRoomsSettlesAndRoundTrips()
        {
            var run = new RogueliteMapRun(901);
            Assert.That(RogueliteMapCatalog.Nodes.Count, Is.EqualTo(20));
            Assert.That(run.VisitedNodes, Does.Contain("start"));
            Assert.Throws<InvalidOperationException>(() => run.SelectNode("core_finale"));
            run.SelectNode("rail_patrol"); run.CompleteCurrentCombat();
            Assert.That(run.Level, Is.EqualTo(2)); Assert.That(run.AwaitingReward, Is.True); Assert.That(run.CompletedNodes, Does.Contain("rail_patrol"));
            string reward = run.CurrentRewards[0].Id; run.ClaimReward(reward);
            var restored = RogueliteMapRun.FromJson(run.ToJson());
            Assert.That(restored.ClaimedRewards, Does.Contain(reward)); Assert.That(restored.AwaitingReward, Is.False);
        }

        [Test]
        public void MapRun_VisitedRoomsCanBeRevisitedAndPermissionGateNeedsCard()
        {
            var run = new RogueliteMapRun(902);
            run.SelectNode("rail_patrol"); run.CompleteCurrentCombat(); run.ClaimReward(run.CurrentRewards[0].Id);
            run.SelectNode("start"); run.SelectNode("rail_patrol");
            Assert.That(run.VisitedNodes, Does.Contain("rail_patrol"));
            Assert.Throws<InvalidOperationException>(() => run.SelectNode("relay_event"));

            run.SelectNode("switchyard"); run.CompleteCurrentNode(); run.SelectNode("relay_event"); run.CompleteCurrentNode();
            run.SelectNode("med_bay"); run.CompleteCurrentNode(); run.SelectNode("permit_archive"); run.CompleteCurrentNode();
            Assert.That(run.AccessCards, Is.EqualTo(1));
            run.SelectNode("safety_room"); run.SelectNode("aether_refinery"); run.SelectNode("transmission_tower");
            Assert.That(run.CurrentNodeId, Is.EqualTo("transmission_tower"));
            Assert.That(RogueliteMapRun.FromJson(run.ToJson()).AccessCards, Is.EqualTo(1));
        }

        [Test]
        public void MapRun_ImportsLegacyMap1Save()
        {
            var restored = RogueliteMapRun.FromJson("map1|17|rail_patrol|2|1|start,rail_patrol|rail_patrol|war_hammer|0");
            Assert.That(restored.CurrentNodeId, Is.EqualTo("rail_patrol"));
            Assert.That(restored.VisitedNodes, Does.Contain("rail_patrol"));
            Assert.That(restored.ClaimedRewards, Does.Contain("war_hammer"));
        }

        [Test]
        public void MapRun_ContentChoicesPreviewAndPersistTheirResults()
        {
            var run = new RogueliteMapRun(515);
            run.SelectNode("supply_checkpoint");
            Assert.That(run.CurrentContentChoices.Select(choice => choice.Preview), Has.All.Not.Empty);
            run.ChooseCurrentNodeContent("medical_cache");
            Assert.That(run.Supplies, Is.EqualTo(1));
            Assert.That(run.CompletedNodes, Does.Contain("supply_checkpoint"));
            Assert.That(RogueliteMapRun.FromJson(run.ToJson()).Supplies, Is.EqualTo(1));
        }

        [Test]
        public void MapRun_RiskyEventOnlyStartsDisclosedCombatThenGrantsCard()
        {
            var run = new RogueliteMapRun(516);
            run.SelectNode("rail_patrol"); run.CompleteCurrentCombat(); run.ClaimReward(run.CurrentRewards[0].Id);
            run.SelectNode("switchyard");
            RogueliteNodeContentChoice riskyChoice = run.CurrentContentChoices.Single(choice => choice.Id == "overload");
            Assert.That(riskyChoice.RequiresCombat, Is.True);
            Assert.That(riskyChoice.Preview, Does.Contain("额外战斗"));
            run.ChooseCurrentNodeContent("overload");
            Assert.That(run.HasPendingContentCombat, Is.True);
            Assert.That(run.AccessCards, Is.EqualTo(0));
            run.CompletePendingContentCombat();
            Assert.That(run.AccessCards, Is.EqualTo(1));
            Assert.That(run.CompletedNodes, Does.Contain("switchyard"));
        }

        [Test]
        public void MapRun_RewardChoicesAreDeterministicAndRequireWorkshopEquip()
        {
            var first = new RogueliteMapRun(77); first.SelectNode("rail_patrol"); first.CompleteCurrentCombat();
            var second = new RogueliteMapRun(77); second.SelectNode("rail_patrol"); second.CompleteCurrentCombat();
            Assert.That(first.CurrentRewards.Select(reward => reward.Id), Is.EqualTo(second.CurrentRewards.Select(reward => reward.Id)));
            string weaponId = first.CurrentRewards.First(reward => reward.Kind == RogueliteRewardKind.Weapon).Id; first.ClaimReward(weaponId);
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East); first.ApplyBuild(hero);
            Assert.That(hero.MainHand.Id, Is.EqualTo(CombatCatalog.Rifle.Id));
            first.EquipReward(weaponId); first.ApplyBuild(hero);
            Assert.That(hero.MainHand.Id, Is.EqualTo(RogueliteMapCatalog.Rewards.First(reward => reward.Id == weaponId).Weapon.Id));
        }

        [Test]
        public void MapRun_ShopCostsAndWorkshopCalibrationPersist()
        {
            var run = new RogueliteMapRun(617);
            Assert.That(run.Parts, Is.EqualTo(4)); Assert.That(run.Aether, Is.EqualTo(2));
            run.SelectNode("supply_checkpoint"); run.ChooseCurrentNodeContent("medical_cache");
            Assert.That(run.Parts, Is.EqualTo(2)); Assert.That(run.Supplies, Is.EqualTo(1));
            run.SelectNode("field_workshop"); run.ChooseCurrentNodeContent("wand_calibration");
            Assert.That(run.ClaimedRewards, Does.Contain("arcane_wand"));
            run.EquipReward("arcane_wand"); run.CalibrateAether();
            var restored = RogueliteMapRun.FromJson(run.ToJson());
            Assert.That(restored.EquippedWeaponId, Is.EqualTo("arcane_wand")); Assert.That(restored.IsAetherCalibrated, Is.True); Assert.That(restored.Aether, Is.EqualTo(0));
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East) { Armor = 1 };
            restored.ApplyBuild(hero);
            Assert.That(hero.MainHand.Id, Is.EqualTo("arcane_wand")); Assert.That(hero.Armor, Is.EqualTo(2));
        }

        [Test]
        public void MapRun_ShopRejectsPurchaseWithoutEnoughCurrency()
        {
            var run = new RogueliteMapRun(618);
            run.SelectNode("supply_checkpoint"); run.ChooseCurrentNodeContent("medical_cache");
            Assert.Throws<InvalidOperationException>(() => run.ChooseCurrentNodeContent("signal_contract"));
            Assert.That(run.ScoutingBeacons, Is.EqualTo(0));
        }

        [Test]
        public void RegionEncounterCatalog_CoversNineArchetypesAndDifferentiatesEliteAndBoss()
        {
            Assert.That(EnemyArchetypes.All.Count, Is.GreaterThanOrEqualTo(10));
            RogueliteEncounterDefinition normal = RogueliteEncounterCatalog.For("rail_patrol");
            RogueliteEncounterDefinition elite = RogueliteEncounterCatalog.For("elite_foundry");
            RogueliteEncounterDefinition boss = RogueliteEncounterCatalog.For("core_finale");
            Assert.That(normal.IsElite, Is.False); Assert.That(normal.IsBoss, Is.False);
            Assert.That(elite.IsElite, Is.True); Assert.That(elite.EnemyArchetypeIds, Does.Contain("elite_vanguard"));
            Assert.That(boss.IsBoss, Is.True); Assert.That(boss.EnemyArchetypeIds, Does.Contain("core_overseer"));
        }

        [Test]
        public void RegionBoss_HasDocumentedVitalityAndDefenses()
        {
            EnemyArchetype boss = EnemyArchetypes.Get("core_overseer");
            var unit = new UnitState("boss", false, new GridPosition(1, 1), Facing.West);
            boss.Apply(unit);
            Assert.That(unit.DisplayName, Is.EqualTo("核心守备监工")); Assert.That(unit.MaxHealth, Is.EqualTo(30));
            Assert.That(unit.Health, Is.EqualTo(30)); Assert.That(unit.Shield, Is.EqualTo(4)); Assert.That(unit.Armor, Is.EqualTo(3));
        }

        [Test]
        public void MapRun_OffersMixedWeaponAndSpellRewards()
        {
            for (int seed = 1; seed < 20; seed++)
            {
                var run = new RogueliteMapRun(seed); run.SelectNode("rail_patrol"); run.CompleteCurrentCombat();
                Assert.That(run.CurrentRewards.Count, Is.EqualTo(3));
                Assert.That(run.CurrentRewards.Select(reward => reward.Kind).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            }
        }

        [Test]
        public void MapRun_RewardCardsHaveDisplayableCombatStatistics()
        {
            var run = new RogueliteMapRun(321);
            run.SelectNode("rail_patrol");
            run.CompleteCurrentCombat();

            foreach (RogueliteReward reward in run.CurrentRewards)
            {
                Assert.That(reward.DisplayName, Is.Not.Empty);
                if (reward.Kind == RogueliteRewardKind.Weapon)
                {
                    Assert.That(reward.Weapon.Damage, Is.GreaterThan(0));
                    Assert.That(reward.Weapon.Range, Is.GreaterThan(0));
                }
                else
                {
                    Assert.That(reward.Spell.Damage, Is.GreaterThan(0));
                    Assert.That(reward.Spell.Range, Is.GreaterThan(0));
                }
            }
        }
    }
}
