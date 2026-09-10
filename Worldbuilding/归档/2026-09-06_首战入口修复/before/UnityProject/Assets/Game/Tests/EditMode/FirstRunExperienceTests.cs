using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using RogueEquipmentSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat.Tests
{
    public sealed class FirstRunExperienceTests
    {
        [Test]
        public void PhaseAContract_HasTenContentNodesFourBattlesNoBossAndThreeMechanicPairs()
        {
            FirstRunExperienceState state = FirstRunExperienceCatalog.CreatePhaseA();

            Assert.That(state.Nodes.Count, Is.EqualTo(11));
            Assert.That(state.Nodes.Count(value => value.Type != FirstRunNodeType.Origin), Is.EqualTo(10));
            Assert.That(state.Nodes.Count(value => value.Type == FirstRunNodeType.Combat), Is.EqualTo(3));
            Assert.That(state.Nodes.Count(value => value.Type == FirstRunNodeType.Elite), Is.EqualTo(1));
            Assert.That(state.Nodes.Select(value => value.Type.ToString()), Has.None.EqualTo("Boss"));
            Assert.That(state.EncounterForNode("B1").LevelSlotId, Is.EqualTo(RainLanternCourtRuntime.LevelId));
            Assert.That(state.EncounterForNode("B1").MechanicSlotIds,
                Is.EqualTo(new[] { "FIRST-B1-WATER", "FIRST-B1-LAMP_VINE", "FIRST-B1-ORIGIN" }));
            Assert.That(state.EncounterForNode("B2").MechanicSlotIds, Is.EqualTo(new[] { "MECHANIC-SLOT-B", "MECHANIC-SLOT-C" }));
            Assert.That(state.EncounterForNode("B3").MechanicSlotIds, Is.EqualTo(new[] { "MECHANIC-SLOT-A", "MECHANIC-SLOT-C" }));
            Assert.That(state.Encounters.Where(value => value.NodeId.StartsWith("B", StringComparison.Ordinal)),
                Has.All.Matches<FirstRunEncounterSnapshot>(value => value.IdealRouteIds.Count >= 3));
            Assert.That(state.EncounterForNode("B2").BuildFeedbackTags, Is.Not.Empty);
            Assert.That(state.EncounterForNode("B3").BuildFeedbackTags, Is.Not.Empty);
        }

        [Test]
        public void FirstRunFlow_UnlocksServicesAndShopOnlyAfterEliteVictoryReward()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(9001);
            Assert.That(run.IsNodeAvailable("B1"), Is.False);
            run.AcknowledgeFirstRunOrigin();
            ResolveBattle(run, "B1");
            VisitEvent(run, "EV1", 0); VisitEvent(run, "EV2", 0);
            ResolveBattle(run, "B2");
            VisitEvent(run, "EV3", 1);
            run.SelectNode("W"); run.CompleteFirstRunForge("equipment-instance"); run.CompleteFirstRunSpecialization("spell-instance");
            ResolveBattle(run, "B3");
            run.SelectNode("M"); run.CompleteFirstRunHealthCheck();
            Assert.That(run.IsNodeAvailable("X"), Is.True);
            Assert.That(run.IsNodeAvailable("S"), Is.False);

            run.SelectNode("X"); run.CompleteCurrentCombat();
            Assert.That(run.IsNodeAvailable("S"), Is.False);
            run.ClaimReward("PHASE_A_PLACEHOLDER_X_REWARD");
            Assert.That(run.IsNodeAvailable("S"), Is.True);
            run.SelectNode("S");
            Assert.That(run.IsComplete, Is.True);
        }

        [Test]
        public void EliteDefeat_SealsRunAndNeverOpensShop()
        {
            RogueliteMapRun run = ReadyForElite(9002);
            run.SelectNode("X"); run.SealFirstRunEliteDefeat();

            Assert.That(run.FirstRunExperience.RunSealed, Is.True);
            Assert.That(run.FirstRunExperience.Outcome, Is.EqualTo(FirstRunOutcome.EliteDefeat));
            Assert.That(run.IsNodeAvailable("S"), Is.False);
            Assert.That(run.CurrentHealth, Is.Zero);
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(run).Summary);
        }

        [Test]
        public void FirstRunSave_RoundTripsStableChoicesAndNewRunCoordinatorUsesFixedProgram()
        {
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator coordinator = new RogueliteMapSaveCoordinator(gateway);
            RogueliteMapStartResult start = coordinator.TryStart(false, FireRogueliteStarterCatalog.Ranged, 42);

            Assert.That(start.Success, Is.True, start.FailureMessage + " | " + gateway.LastError);
            Assert.That(start.Run.IsFirstRunExperience, Is.True);
            string[] choices = start.Run.FirstRunExperience.RewardGroups.SelectMany(value => value.CandidateIds).ToArray();
            string saved = store.Values[RogueliteSaveGateway.MapRunKey];
            RogueRunDto dto = Rogue11Serializer.Deserialize(saved);
            Assert.That(dto.RunProgramId, Is.EqualTo(RogueliteRunProgram.FirstRunV1.ToString()));
            Assert.That(dto.FirstRunExperience.RewardGroups.SelectMany(value => value.CandidateIds), Is.EqualTo(choices));
            Assert.That(Rogue11Serializer.Serialize(dto), Is.EqualTo(saved));
        }

        [Test]
        public void EquipmentContract_HasExactlyNineActiveSlotsAndLegacyNamesNormalize()
        {
            Assert.That(EquipmentSlotRules.ActiveSlots.Count, Is.EqualTo(9));
            Assert.That(EquipmentSlotRules.ActiveSlots.Distinct().Count(), Is.EqualTo(9));
            Assert.That(EquipmentSlotRules.NormalizeLegacy(RogueEquipmentSlot.MainHand), Is.EqualTo(RogueEquipmentSlot.Weapon));
            Assert.That(EquipmentSlotRules.NormalizeLegacy(RogueEquipmentSlot.OffHand), Is.EqualTo(RogueEquipmentSlot.None));
            Assert.That(EquipmentSlotRules.NormalizeLegacy(RogueEquipmentSlot.Accessory2), Is.EqualTo(RogueEquipmentSlot.Ring2));
            Assert.That(new RogueRunDto().EquipmentSlotInstanceIds.Keys, Is.EquivalentTo(EquipmentSlotRules.ActiveSlots));
        }

        [Test]
        public void MechanicContract_ExposesAllWindowsAndMovementUsesRealPath()
        {
            Assert.That(Enum.GetValues(typeof(CombatMechanicTriggerWindow)).Cast<CombatMechanicTriggerWindow>(), Is.EquivalentTo(new[]
            {
                CombatMechanicTriggerWindow.Attack, CombatMechanicTriggerWindow.Proximity, CombatMechanicTriggerWindow.Enter,
                CombatMechanicTriggerWindow.PassThrough, CombatMechanicTriggerWindow.Interact,
                CombatMechanicTriggerWindow.Damaged, CombatMechanicTriggerWindow.Destroyed
            }));
            GridMap map = new GridMap(4, 3, new[] { new GridPosition(1, 0) });
            IReadOnlyList<GridPosition> path = map.FindShortestPath(new GridPosition(0, 0), new GridPosition(2, 0), 4);
            Assert.That(path, Is.EqualTo(new[] { new GridPosition(0, 0), new GridPosition(0, 1), new GridPosition(1, 1), new GridPosition(2, 1), new GridPosition(2, 0) }));

            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0), Facing.East);
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 2), Facing.West);
            IReadOnlyList<CombatMechanicTriggerContext> heroContexts = CombatMechanicTriggerContextFactory.ForCommand(
                CombatCommand.Move(hero.Id, new GridPosition(2, 0), Facing.East), hero, hero.Position, path, CombatEffectExecution.Empty);
            IReadOnlyList<CombatMechanicTriggerContext> enemyContexts = CombatMechanicTriggerContextFactory.ForCommand(
                CombatCommand.Move(enemy.Id, new GridPosition(2, 2), Facing.West), enemy, enemy.Position,
                new[] { enemy.Position, new GridPosition(2, 2) }, CombatEffectExecution.Empty);
            Assert.That(heroContexts.Count(value => value.Window == CombatMechanicTriggerWindow.PassThrough), Is.EqualTo(3));
            Assert.That(heroContexts.Last().Window, Is.EqualTo(CombatMechanicTriggerWindow.Enter));
            Assert.That(heroContexts.All(value => value.ActorIsHero), Is.True);
            Assert.That(enemyContexts.All(value => !value.ActorIsHero), Is.True);
        }

        private static void ResolveBattle(RogueliteMapRun run, string nodeId)
        {
            run.SelectNode(nodeId); run.CompleteCurrentCombat(); run.ClaimReward(run.CurrentFirstRunRewardIds[0]);
        }

        private static void VisitEvent(RogueliteMapRun run, string nodeId, int optionIndex)
        {
            run.SelectNode(nodeId); run.ChooseCurrentNodeContent(run.FirstRunExperience.EventForNode(nodeId).OptionIds[optionIndex]);
        }

        private static RogueliteMapRun ReadyForElite(int seed)
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(seed); run.AcknowledgeFirstRunOrigin();
            ResolveBattle(run, "B1"); VisitEvent(run, "EV1", 0); VisitEvent(run, "EV2", 0);
            ResolveBattle(run, "B2"); VisitEvent(run, "EV3", 1);
            run.SelectNode("W"); run.CompleteFirstRunForge("equipment"); run.CompleteFirstRunSpecialization("spell");
            ResolveBattle(run, "B3"); run.SelectNode("M"); run.CompleteFirstRunHealthCheck();
            return run;
        }

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }
    }
}
