using System;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CampaignStateTests
    {
        [Test]
        public void CampaignState_JsonRoundTripPreservesProgress()
        {
            var state = new CampaignState("hub");
            state.AddLocation(new LocationState("factory")); state.AddLocation(new LocationState("archive"));
            state.Discover("factory"); state.AddRoute("hub", "factory"); state.Visit("factory"); state.SetQuest("relay", "active"); state.SetResources(12, 4, 1, 2); state.Story.Set("met_engineer", "yes");
            var restored = CampaignState.FromJson(state.ToJson());
            Assert.That(restored.CurrentLocationId, Is.EqualTo("factory")); Assert.That(restored.Locations["factory"].Visited, Is.True); Assert.That(restored.Credits, Is.EqualTo(12));
        }

        [Test]
        public void CampaignState_CloneIsIndependent()
        {
            var state = new CampaignState("hub"); state.AddLocation(new LocationState("factory"));
            var clone = state.Clone(); clone.Discover("factory"); clone.SetResources(3, 2, 1, 0);
            Assert.That(state.Locations["factory"].Discovered, Is.False); Assert.That(state.Credits, Is.EqualTo(0));
        }

        [Test]
        public void SaveVersion_RejectsUnknownVersion()
        {
            Assert.Throws<InvalidOperationException>(() => CampaignState.FromJson("v99|hub|0,0,0,0|hub,1,0,default"));
        }

        [Test]
        public void IndustrialCity_ProvidesServicesHiddenDiscoveryAndSideStoryReturn()
        {
            var city = new IndustrialCityPrototype();
            Assert.That(city.State.Locations["sealed_archive"].Discovered, Is.False);
            city.RevealArchive(); city.CompleteEngineerSideStory();
            Assert.That(city.State.Locations["sealed_archive"].Discovered, Is.True);
            Assert.That(city.State.Locations["central_depot"].Status, Is.EqualTo("workshop_discount"));
        }

        [Test]
        public void TaskTemplates_ContainSixTypesWithoutDuplicateCombinations()
        {
            TaskTemplateCatalog.ValidateNoDuplicateCombinations();
            Assert.That(TaskTemplateCatalog.All.Select(t => t.Type).Distinct().Count(), Is.EqualTo(6));
        }

        [Test]
        public void SaveManager_SupportsSlotsAndAutomaticBackup()
        {
            var state = new CampaignState("hub"); state.SetResources(9, 2, 0, 0);
            var saves = new CampaignSaveManager(); saves.Save(1, state, "before_choice"); saves.Backup(1, state);
            Assert.That(saves.Load(1).Credits, Is.EqualTo(9)); Assert.That(saves.LoadBackup(1).Aether, Is.EqualTo(2));
        }

        [Test]
        public void MissionPreparation_IsCloneableAndContainsNoTimePressureFields()
        {
            var prep = new MissionPreparation().Configure("relay", "destroy targets", "armored guard");
            var clone = prep.Clone();
            Assert.That(clone.MissionId, Is.EqualTo("relay")); Assert.That(clone.RulesSummary, Is.EqualTo("destroy targets"));
        }

        [Test]
        public void Progression_RepairsWithoutDisablingEquipmentAndValidatesSixTemplates()
        {
            var equipment = new EquipmentState("rifle", 10); equipment.Wear(4); equipment.Repair();
            var services = new ServiceLedger(); services.Train(); services.ResetWorkshop(); services.AddUpgrade("ether_coil");
            Assert.That(equipment.Durability, Is.EqualTo(10)); Assert.That(equipment.IsDisabled, Is.False); Assert.That(services.Upgrades, Does.Contain("ether_coil"));
            TaskTemplateValidator.Validate(TaskTemplateCatalog.All);
        }

        [Test]
        public void RogueliteStoryPackage_UsesIndependentDeterministicSave()
        {
            var first = RogueliteStoryCatalog.CreateDefault(4242);
            first.CompleteCurrentMission("signal recovered", new[] { "coil_mod" });
            var restored = RogueliteStoryPackage.FromJson(first.ToJson());
            var clone = restored.Clone(); clone.CompleteCurrentMission("breach secured", new[] { "armor_patch" });
            Assert.That(restored.Seed, Is.EqualTo(4242)); Assert.That(restored.CurrentMissionId, Is.EqualTo("factory_breach"));
            Assert.That(clone.CurrentMissionId, Is.EqualTo("last_conduit")); Assert.That(restored.UnlockedContent, Does.Contain("coil_mod"));
            Assert.That(restored.CompletedMissions, Does.Not.Contain("factory_breach"));
        }

        [Test]
        public void RogueliteStoryPackage_CompletesChainAndStoresSettlementOnlyInRogueliteSave()
        {
            var package = RogueliteStoryCatalog.CreateDefault(7);
            package.CompleteCurrentMission("one", null); package.CompleteCurrentMission("two", null); package.CompleteCurrentMission("three", new[] { "epilogue" });
            Assert.That(package.IsComplete, Is.True); Assert.That(package.SettlementSummary, Is.EqualTo("three"));
            Assert.That(package.ToJson(), Does.Contain("rogue1"));
        }

        [Test]
        public void CombatFlow_TransitionsMenuBriefingCombatRestartAndReturn()
        {
            var map = new GridMap(3, 3); var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            var flow = new CombatFlowController(); flow.Configure(new MissionPreparation().Configure("relay", "destroy", "guard"), new CombatState(map, new[] { hero }));
            Assert.That(flow.Phase, Is.EqualTo(CombatFlowPhase.DeveloperMenu));
            flow.OpenBriefing(); flow.BeginCombat(); flow.TacticalRestart(); flow.ResumeAfterRestart(); flow.ReturnToDeveloperMenu();
            Assert.That(flow.Phase, Is.EqualTo(CombatFlowPhase.DeveloperMenu));
            Assert.That(flow.State.GetUnit("hero").Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void CombatFlow_AllowsRestartAfterVictory()
        {
            var map = new GridMap(2, 2);
            var hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            var state = new CombatState(map, new[] { hero }); state.ConfigureObjectives(new CaptureObjective(new GridPosition(0, 0), "hero"));
            var flow = new CombatFlowController(); flow.Configure(new MissionPreparation().Configure("relay", "capture", "none"), state);
            flow.OpenBriefing(); flow.BeginCombat(); flow.RefreshOutcome();
            Assert.That(flow.Phase, Is.EqualTo(CombatFlowPhase.Victory));
            flow.TacticalRestart();
            Assert.That(flow.Phase, Is.EqualTo(CombatFlowPhase.TacticalRestart));
        }

        [Test]
        public void CombatSceneConfiguration_RejectsIncompleteMarkerSet()
        {
            Assert.Throws<InvalidOperationException>(() => CombatSceneConfigurationValidator.Validate(1, 1, 1, 0, 1));
            CombatSceneConfigurationValidator.Validate(1, 1, 1, 1, 1);
        }
    }
}
