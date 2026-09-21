using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using OCC.Combat.Presentation;
using RogueEquipmentSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat.Tests
{
    public sealed class FirstRunExperienceTests
    {
        [Test]
        public void FirstBattleBeforeOriginConfirmationExplainsPrerequisiteAndCanStartAfterConfirmation()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(906);
            RogueliteMapNode firstBattle = run.MapNode("B1");
            Assert.That(RogueliteUiPreferences.CanOpenCombatBriefing(run, firstBattle), Is.False);
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, firstBattle),
                Is.EqualTo("先确认学生基础配置，即可进入第一战"));

            RogueliteMapInteractionService interactions = new RogueliteMapInteractionService();
            interactions.AcknowledgeFirstRunOrigin(run);
            Assert.That(RogueliteUiPreferences.CanOpenCombatBriefing(run, firstBattle), Is.True);
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, firstBattle), Is.EqualTo("可以直接前往"));
            Assert.That(interactions.SelectNode(run, "B1").StartsCombat, Is.True);
            Assert.That(run.CurrentNodeId, Is.EqualTo("B1"));
            Assert.That(run.FirstRunExperience.Origin.Acknowledged, Is.True);
            Assert.That(run.IsNodeAvailable("B2"), Is.False);
        }

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
            Assert.That(state.EncounterForNode("B2").MechanicSlotIds,
                Is.EqualTo(new[] { "FIRST-B2-LAMP_VINE", "FIRST-B2-AETHER_CRYSTAL", "FIRST-B2-CRYSTAL_SHARD" }));
            Assert.That(state.EncounterForNode("B3").MechanicSlotIds,
                Is.EqualTo(new[] { "FIRST-B3-WATER", "FIRST-B3-SEALED-CRYSTAL", "FIRST-B3-BURNING" }));
            Assert.That(state.EncounterForNode("X").LevelSlotId, Is.EqualTo(FirstRegionLevelCatalog.ThreeMaterialPressure.Id));
            Assert.That(state.Encounters.Where(value => value.NodeId.StartsWith("B", StringComparison.Ordinal)),
                Has.All.Matches<FirstRunEncounterSnapshot>(value => value.IdealRouteIds.Count >= 3));
            Assert.That(state.EncounterForNode("B2").BuildFeedbackTags, Is.Not.Empty);
            Assert.That(state.EncounterForNode("B3").BuildFeedbackTags, Is.Not.Empty);
        }

        [Test]
        public void FrozenFirstRewardAndEvents_GrantRealContentAndUnlockBattleTwo()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(1906);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1"); run.CompleteCurrentCombat();

            Assert.That(run.CurrentFirstRunRewardIds, Is.EqualTo(new[] { "F-P-M03", "F-P-R19", "F-P-U07" }));
            Assert.That(run.CurrentRewards.Select(value => value.Id), Is.EqualTo(run.CurrentFirstRunRewardIds));
            run.ClaimReward("F-P-R19");
            Assert.That(run.RogueRunState.MasteredSpellIds, Contains.Item("F-P-R19"));
            Assert.That(run.RogueRunState.EquippedSpellIds, Contains.Item("F-P-R19"));

            run.SelectNode("EV1"); run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            Assert.That(run.FirstRunExperience.ForgeMaterialCount, Is.EqualTo(1));
            Assert.That(run.RogueRunState.EquipmentInstances.Select(value => value.DefinitionId), Contains.Item("ACA-EQ-CH04"));

            int goldBeforeEvent = run.Gold;
            run.SelectNode("EV2"); run.ChooseCurrentNodeContent("FIRST-EV2-GOLD");
            Assert.That(run.FirstRunExperience.AcademyFoodCount, Is.EqualTo(1));
            Assert.That(run.Gold, Is.EqualTo(goldBeforeEvent + 3));
            Assert.That(run.RogueRunState.TacticalItemInstances.Select(value => value.DefinitionId), Contains.Item("G-T09"));
            Assert.That(run.RogueRunState.ItemQuickbarInstanceIds.Count(value => !string.IsNullOrEmpty(value)), Is.EqualTo(1));
            Assert.That(run.IsNodeAvailable("B2"), Is.True);
        }

        [Test]
        public void BattleTwoLayout_CrystalBurstAndShardMovementMatchFrozenContract()
        {
            FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.GreenhouseCollectionRoom);
            CombatState state = build.State;
            GridPosition northCrystal = new GridPosition(6, 1);
            GridPosition southCrystal = new GridPosition(6, 5);

            Assert.That(build.Definition.Width, Is.EqualTo(8));
            Assert.That(build.Definition.Height, Is.EqualTo(7));
            Assert.That(build.Definition.HeroSpawn, Is.EqualTo(new GridPosition(1, 3)));
            Assert.That(state.Map.GetTile(northCrystal).BlocksMovement, Is.True);
            Assert.That(state.Map.GetTile(northCrystal).BlocksLineOfSight, Is.False);
            Assert.That(state.LootSource.Position, Is.EqualTo(new GridPosition(4, 3)));
            Assert.That(state.LootSource.HiddenCount, Is.EqualTo(1));
            Assert.That(state.Units.Values.Single(value => value.EnemyArchetypeId == "raider").Speed, Is.EqualTo(11));
            Assert.That(state.Units.Values.Single(value => value.EnemyArchetypeId == "sigil_mauler").Speed, Is.EqualTo(8));

            UnitState raider = state.Units.Values.Single(value => value.EnemyArchetypeId == "raider");
            CombatEffectExecutor.Execute(state, "hero", CombatEffect.DamageObject(northCrystal, 24));
            Assert.That(raider.Health, Is.EqualTo(8));
            Assert.That(state.Map.GetTile(northCrystal).IsCrystalShard, Is.True);
            Assert.That(state.Map.GetTile(northCrystal).BlocksMovement, Is.False);
            Assert.That(new[] { northCrystal, new GridPosition(6, 0), new GridPosition(7, 1), new GridPosition(6, 2), new GridPosition(5, 1) }
                .All(cell => state.Map.GetTile(cell).IsCrystalShard), Is.True);
            Assert.That(CombatMovementQuery.EntryCost(state, state.GetUnit("hero"), northCrystal), Is.EqualTo(2));
            Assert.That(state.Map.GetTile(southCrystal).Durability, Is.EqualTo(16));

            state.AttachRogueEquipmentRuntime(RogueEquipmentRuntime.CreateStarter(1907));
            UnitState hero = state.GetUnit("hero");
            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.Move(new GridPosition(3, 3)));
            state.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(state, hero.Id);
            CombatResolver.Resolve(state, CombatCommand.SearchLoot(hero.Id));
            CombatResolver.Resolve(state, CombatCommand.TakeLoot(hero.Id, "FIRST-B2-ACA-EQ-CR04"));
            Assert.That(state.RogueEquipment.AllInstances.Any(value => value.DefinitionId == "ACA-EQ-CR04"), Is.True);
            Assert.That(state.LootSource.State, Is.EqualTo(LootSearchState.Emptied));

            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(1907); run.CaptureCombatInventory(state);
            RogueRunDto restoredDto = Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState));
            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(restoredDto);
            LootSourceState restoredLoot = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.GreenhouseCollectionRoom).State.LootSource;
            restored.RestoreLootProgress(restoredLoot);
            Assert.That(restoredLoot.State, Is.EqualTo(LootSearchState.Emptied));
        }

        [Test]
        public void BattleTwoAi_MaulerCalibratesThenChasesAfterItsCrystalBreaks()
        {
            CombatState state = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.GreenhouseCollectionRoom).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState hero = state.GetUnit("hero");
            UnitState mauler = state.Units.Values.Single(value => value.EnemyArchetypeId == "sigil_mauler");
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();

            CombatCommand calibrate = plans.GetExecutionCommand(state, mauler, hero);
            EnemyIntentPresentation intent = plans.GetPublicIntent(state, mauler, hero);
            Assert.That(calibrate.Type, Is.EqualTo(CombatCommandType.EndTurn));
            Assert.That(intent.ActionName, Is.EqualTo("贴晶校准"));

            CombatEffectExecutor.Execute(state, hero.Id, CombatEffect.DamageObject(new GridPosition(6, 5), 24));
            plans.Invalidate();
            CombatCommand chase = plans.GetExecutionCommand(state, mauler, hero);
            Assert.That(chase.Type, Is.EqualTo(CombatCommandType.Move));
            Assert.That(chase.Destination, Is.Not.EqualTo(new GridPosition(6, 5)));
        }

        [Test]
        public void BattleTwoAi_RaiderUsesVineEntranceAndHidesAfterEntering()
        {
            CombatState state = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.GreenhouseCollectionRoom).State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState hero = state.GetUnit("hero");
            UnitState raider = state.Units.Values.Single(value => value.EnemyArchetypeId == "raider");
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();

            CombatResolver.BeginTurn(state, raider.Id);
            CombatCommand approach = plans.GetExecutionCommand(state, raider, hero);
            Assert.That(approach.Type, Is.EqualTo(CombatCommandType.Move));
            CombatResolver.Resolve(state, approach);
            Assert.That(state.Map.GetTile(raider.Position).IsLampVine, Is.False);

            plans.Invalidate(); CombatResolver.BeginTurn(state, raider.Id);
            CombatCommand enter = plans.GetExecutionCommand(state, raider, hero);
            Assert.That(enter.Type, Is.EqualTo(CombatCommandType.Move));
            CombatResolver.Resolve(state, enter);
            Assert.That(state.Map.GetTile(raider.Position).IsLampVine, Is.True);
            Assert.That(state.GreenhouseCollectionRoom.IsRaiderHidden(state, raider), Is.True);
            Assert.That(state.GreenhouseCollectionRoom.HasHiddenRaider(state), Is.True);
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
            int goldBeforeEliteReward = run.Gold;
            run.ClaimReward("PASSIVE-ELITE-01");
            Assert.That(run.FirstRunExperience.EliteSelectedPassiveId, Is.EqualTo("PASSIVE-ELITE-01"));
            Assert.That(run.RogueRunState.MasteredSpellIds, Contains.Item("PASSIVE-ELITE-01"));
            Assert.That(run.RogueRunState.EquipmentInstances.Select(value => value.DefinitionId), Contains.Item("ACA-EQ-HD02"));
            TacticalItemInstanceDto brace = run.RogueRunState.TacticalItemInstances.Single(value => value.DefinitionId == "G-T13");
            Assert.That(brace.ChargesCurrent, Is.EqualTo(4));
            Assert.That(run.Gold, Is.EqualTo(goldBeforeEliteReward + 6));
            Assert.That(run.IsNodeAvailable("S"), Is.True);

            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(
                Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState)));
            Assert.That(restored.FirstRunExperience.EliteSelectedPassiveId, Is.EqualTo("PASSIVE-ELITE-01"));
            Assert.That(restored.RogueRunState.EquipmentInstances.Count(value => value.DefinitionId == "ACA-EQ-HD02"), Is.EqualTo(1));
            Assert.That(restored.RogueRunState.TacticalItemInstances.Count(value => value.DefinitionId == "G-T13"), Is.EqualTo(1));
            Assert.That(restored.IsNodeAvailable("S"), Is.True);
            restored.SelectNode("S");
            Assert.That(restored.IsComplete, Is.False);
            restored.CompleteFirstRunExperience();
            // 离开商店不再结束本局：同一局进入随机层，只有首领战结算才结束整轮。
            Assert.That(restored.IsInAcademyLayer, Is.True);
            Assert.That(restored.IsComplete, Is.False);
            Assert.That(restored.FirstRunExperience.Lifecycle, Is.EqualTo(FirstRunLifecycle.RandomLayer));
        }

        [Test]
        public void EliteUnlocksAfterBattleThreeWithoutOptionalServiceCompletion()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(9005);
            run.AcknowledgeFirstRunOrigin();
            ResolveBattle(run, "B1");
            VisitEvent(run, "EV1", 0);
            VisitEvent(run, "EV2", 0);
            ResolveBattle(run, "B2");
            VisitEvent(run, "EV3", 1);
            ResolveBattle(run, "B3");

            Assert.That(run.FirstRunExperience.Workshop.ForgeCompleted, Is.False);
            Assert.That(run.FirstRunExperience.Medical.HealthCheckCompleted, Is.False);
            Assert.That(run.IsNodeAvailable("X"), Is.True);

            run.SelectNode("X");
            Assert.That(run.CurrentNodeId, Is.EqualTo("X"));
        }

        [Test]
        public void LoadingAnOldLockedEliteFlagRecomputesTheCurrentGate()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(9006);
            run.AcknowledgeFirstRunOrigin();
            ResolveBattle(run, "B1");
            VisitEvent(run, "EV1", 0);
            VisitEvent(run, "EV2", 0);
            ResolveBattle(run, "B2");
            VisitEvent(run, "EV3", 1);
            ResolveBattle(run, "B3");
            typeof(FirstRunNodeSnapshot).GetProperty(nameof(FirstRunNodeSnapshot.Flags))
                ?.SetValue(run.FirstRunExperience.Node("X"), FirstRunNodeFlags.Locked);

            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(
                Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState)));

            Assert.That(restored.IsNodeAvailable("X"), Is.True);
        }

        [Test]
        public void OrdinaryRewardCanBeAbandonedAndRoundTripsBeforeEventsUnlock()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(9011);
            run.AcknowledgeFirstRunOrigin();
            run.SelectNode("B1"); run.CompleteCurrentCombat();

            run.AbandonCurrentReward();

            Assert.That(run.AwaitingReward, Is.False);
            Assert.That(run.FirstRunExperience.RewardGroups.Single(value => value.NodeId == "B1").Abandoned, Is.True);
            Assert.That(run.IsNodeAvailable("EV1"), Is.True);
            Assert.That(run.IsNodeAvailable("EV2"), Is.True);
            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(
                Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(run.RogueRunState)));
            Assert.That(restored.FirstRunExperience.RewardGroups.Single(value => value.NodeId == "B1").Abandoned, Is.True);
            Assert.That(restored.IsNodeAvailable("EV1"), Is.True);
        }

        [Test]
        public void EliteRewardCanBeAbandonedAndShopExitHandsOffToTheRandomLayer()
        {
            RogueliteMapRun run = ReadyForElite(9012);
            run.SelectNode("X"); run.CompleteCurrentCombat();

            run.AbandonCurrentReward();

            Assert.That(run.FirstRunExperience.EliteRewardAbandoned, Is.True);
            Assert.That(run.IsNodeAvailable("S"), Is.True);
            run.SelectNode("S");
            Assert.That(run.IsComplete, Is.False);
            run.CompleteFirstRunExperience();
            Assert.That(run.IsInAcademyLayer, Is.True);
            Assert.That(run.FirstRunExperience.Lifecycle, Is.EqualTo(FirstRunLifecycle.RandomLayer));
            Assert.That(run.IsComplete, Is.False, "整轮结束只由首领战结算决定");
        }

        [Test]
        public void FrozenSecondThirdRewardsAndEventThree_GrantRealContent()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(9010); run.AcknowledgeFirstRunOrigin();
            ResolveBattle(run, "B1"); VisitEvent(run, "EV1", 0); VisitEvent(run, "EV2", 0);
            run.SelectNode("B2"); run.CompleteCurrentCombat();
            Assert.That(run.CurrentFirstRunRewardIds, Is.EqualTo(new[] { "F-P-U04", "F-P-U01", "F-P-U18" }));
            run.ClaimReward("F-P-U04");
            run.SelectNode("EV3");
            Assert.That(run.CurrentContentChoices.Select(value => value.Id), Is.EqualTo(new[] { "FIRST-EV3-CONTRIBUTION", "FIRST-EV3-REACTION-BELL" }));
            run.ChooseCurrentNodeContent("FIRST-EV3-REACTION-BELL");
            Assert.That(run.FirstRunExperience.SpecializationMaterialCount, Is.EqualTo(1));
            Assert.That(run.RogueRunState.TacticalItemInstances.Single(value => value.DefinitionId == "G-T10").ChargesCurrent, Is.EqualTo(2));
            run.SelectNode("B3"); run.CompleteCurrentCombat();
            Assert.That(run.CurrentFirstRunRewardIds, Is.EqualTo(new[] { "ACA-EQ-MH03", "ACA-EQ-DG02", "F-P-M12" }));
            Assert.That(run.CurrentRewards.Select(value => value.Id), Is.EqualTo(run.CurrentFirstRunRewardIds));
        }

        [Test]
        public void BattleThreeAndEliteArena_MatchFrozenLayoutAndChargeCollision()
        {
            FirstRegionLevelBuild b3 = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.RainPrismCourt);
            Assert.That(b3.Definition.Width, Is.EqualTo(8));
            Assert.That(b3.Definition.Height, Is.EqualTo(7));
            Assert.That(b3.Definition.HeroSpawn, Is.EqualTo(new GridPosition(1, 3)));
            Assert.That(b3.State.Map.GetTile(new GridPosition(2, 3)).IsWater, Is.True);
            Assert.That(b3.State.Map.GetTile(new GridPosition(5, 3)).Durability, Is.EqualTo(16));
            Assert.That(b3.State.LootSource.Position, Is.EqualTo(new GridPosition(6, 3)));
            Assert.That(b3.State.LootSource.RevealNext().DefinitionId, Is.EqualTo("ACA-EQ-CR01"));
            b3.State.ConfigureRuleset(CombatRuleset.Roguelite);
            UnitState b3Hero = b3.State.GetUnit("hero");
            b3Hero.ApplyStatus(StatusType.Burning, 2, 4);
            CombatResolver.BeginTurn(b3.State, b3Hero.Id);
            CombatResolver.Resolve(b3.State, CombatCommand.Move(b3Hero.Id, new GridPosition(2, 3)));
            Assert.That(b3Hero.HasStatus(StatusType.Burning), Is.False);

            CombatState elite = FirstRegionLevelBuilder.Build(FirstRegionLevelCatalog.ThreeMaterialPressure).State;
            elite.ConfigureRuleset(CombatRuleset.Roguelite);
            new RogueAcademyContentService().ApplyEnemyBaseline(elite, elite.GetUnit("enemy_0"));
            UnitState ram = elite.GetUnit("enemy_0");
            UnitState hero = elite.GetUnit("hero");
            Assert.That(ram.Health, Is.EqualTo(36));
            Assert.That(ram.Shield, Is.EqualTo(8));
            Assert.That(elite.Map.Width, Is.EqualTo(8));
            Assert.That(elite.Map.Height, Is.EqualTo(7));
            Assert.That(elite.Map.GetTile(new GridPosition(5, 3)).Durability, Is.EqualTo(24));
            EnemyTurnPlanBook plans = new EnemyTurnPlanBook();
            CombatCommand charge = plans.GetExecutionCommand(elite, ram, hero);
            Assert.That(charge.Type, Is.EqualTo(CombatCommandType.BreachCharge));
            Assert.That(plans.GetPublicIntent(elite, ram, hero).DetailedText, Does.Contain("F4").And.Contain("耐久 -8"));
            CombatResolver.BeginTurn(elite, ram.Id);
            CombatResolver.Resolve(elite, charge);
            Assert.That(elite.Map.GetTile(new GridPosition(5, 3)).Durability, Is.EqualTo(16));
            Assert.That(ram.Position, Is.EqualTo(new GridPosition(6, 3)));
            Assert.That(ram.Shield, Is.Zero);
            Assert.That(elite.ThreeMaterialPressure.IsVented, Is.True);
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
        public void RogueSpellLoadout_AssignsReplacesAndRemovesOnlyMasteredUniqueSpells()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(9013);
            RogueRunDto dto = run.RogueRunState;
            dto.MasteredSpellIds.Add("F-P-R01");

            Assert.That(run.AssignRogueSpell("F-P-R01", 5), Is.True);
            Assert.That(dto.EquippedSpellIds[5], Is.EqualTo("F-P-R01"));
            Assert.That(run.AssignRogueSpell("F-P-R01", 6), Is.False);
            Assert.That(run.AssignRogueSpell("UNKNOWN", 5), Is.False);
            Assert.That(run.AssignRogueSpell(string.Empty, 5), Is.True);
            Assert.That(dto.EquippedSpellIds[5], Is.Empty);
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

            UnitState hero = new UnitState("hero", true, new GridPosition(0, 0));
            UnitState enemy = new UnitState("enemy", false, new GridPosition(3, 2));
            IReadOnlyList<CombatMechanicTriggerContext> heroContexts = CombatMechanicTriggerContextFactory.ForCommand(
                CombatCommand.Move(hero.Id, new GridPosition(2, 0)), hero, hero.Position, path, CombatEffectExecution.Empty);
            IReadOnlyList<CombatMechanicTriggerContext> enemyContexts = CombatMechanicTriggerContextFactory.ForCommand(
                CombatCommand.Move(enemy.Id, new GridPosition(2, 2)), enemy, enemy.Position,
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

        [Test]
        public void FirstExperienceSlotsRouteOnlyMapRunKeys()
        {
            Assert.That(FirstExperienceSaveRouting.ResolveMapKey(RogueliteSaveGateway.MapRunKey, 1),
                Is.EqualTo("occ.first_experience.map_run.slot.1"));
            Assert.That(FirstExperienceSaveRouting.ResolveMapKey(
                RogueliteSaveGateway.MapRunKey + RogueliteSaveGateway.WriteLockSuffix, 2),
                Is.EqualTo("occ.first_experience.map_run.slot.2" + RogueliteSaveGateway.WriteLockSuffix));
            Assert.That(FirstExperienceSaveRouting.ResolveMapKey(RogueliteSaveGateway.UiPreferencesKey, 1),
                Is.EqualTo(RogueliteSaveGateway.UiPreferencesKey));
            Assert.That(FirstExperienceSaveRouting.ResolveMapKey(RogueliteSaveGateway.MapRunKey, -1),
                Is.EqualTo(RogueliteSaveGateway.MapRunKey));
        }

        [Test]
        public void FrontEndHandsEveryLegacyMapOrBattleStageToTheRealRuntime()
        {
            Assert.That(FirstExperiencePrototypeController.IsRuntimeStage(
                FirstExperiencePrototypeController.FlowStage.AcademyIntro), Is.False);
            Assert.That(FirstExperiencePrototypeController.IsRuntimeStage(
                FirstExperiencePrototypeController.FlowStage.Map), Is.True);
            Assert.That(FirstExperiencePrototypeController.IsRuntimeStage(
                FirstExperiencePrototypeController.FlowStage.Battle2Result), Is.True);
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
