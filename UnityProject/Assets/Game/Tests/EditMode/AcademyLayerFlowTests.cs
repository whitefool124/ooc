using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    /// <summary>
    /// 固定教学段 → 随机层 → 首领战 → 单轮结算 的同一局全流程契约。
    /// </summary>
    public sealed class AcademyLayerFlowTests
    {
        [Test]
        public void Lifecycle_AfterShopBecomesRandomLayerAndOnlyRoundSettlementIsTerminal()
        {
            RogueliteMapRun run = ReadyForShop(4201);
            Assert.That(run.FirstRunExperience.Lifecycle, Is.EqualTo(FirstRunLifecycle.ShopOpened));
            Assert.That(run.IsComplete, Is.False);

            run.CompleteFirstRunExperience();

            Assert.That(run.FirstRunExperience.Lifecycle, Is.EqualTo(FirstRunLifecycle.RandomLayer));
            Assert.That(run.FirstRunExperience.IsRandomLayer, Is.True);
            Assert.That(run.IsInAcademyLayer, Is.True);
            Assert.That(run.IsTutorialPhase, Is.False);
            Assert.That(run.IsComplete, Is.False, "离开商店不再结束本局");
            Assert.That(run.FirstRunExperience.IsTerminal, Is.False);
            Assert.That(run.RegionBossId, Is.EqualTo("core_overseer"));
        }

        [Test]
        public void Handoff_KeepsResourcesEquipmentAndCombatSnapshotInTheSameRun()
        {
            RogueliteMapRun run = ReadyForShop(4202);
            string[] masteredBefore = run.RogueRunState.MasteredSpellIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            int goldBefore = run.Gold;
            int equipmentBefore = run.RogueRunState.EquipmentInstances.Count;
            int tacticalBefore = run.RogueRunState.TacticalItemInstances.Count;

            run.CompleteFirstRunExperience();

            Assert.That(run.Gold, Is.EqualTo(goldBefore));
            Assert.That(run.RogueRunState.MasteredSpellIds, Is.SupersetOf(masteredBefore));
            Assert.That(run.RogueRunState.MasteredSpellIds, Contains.Item(FirstRunExperienceCatalog.OriginSpellId));
            Assert.That(run.RogueRunState.MasteredSpellIds, Contains.Item("PASSIVE-ELITE-01"));
            Assert.That(run.RogueRunState.EquipmentInstances.Count, Is.EqualTo(equipmentBefore));
            Assert.That(run.RogueRunState.TacticalItemInstances.Count, Is.EqualTo(tacticalBefore));
            Assert.That(run.CurrentHealth, Is.EqualTo(run.RogueRunState.CurrentHealth));
            Assert.That(run.CurrentMana, Is.EqualTo(run.RogueRunState.CurrentMana));
            Assert.That(run.HasCombatSnapshot, Is.True);
            Assert.That(run.CurrentHealth, Is.GreaterThan(0));
        }

        [Test]
        public void Handoff_SwitchesToTheAcademySubgraphWithFullContentCoverage()
        {
            RogueliteMapRun run = ReadyForShop(4203);
            run.CompleteFirstRunExperience();

            Assert.That(run.MapNodes.Count, Is.EqualTo(20));
            Assert.That(run.MapNodes.Select(node => node.Id), Contains.Item("academy_gate"));
            Assert.That(run.CurrentNodeId, Is.EqualTo("academy_gate"));
            Assert.That(run.EncounterAssignments.Count, Is.EqualTo(13));
            Assert.That(run.NodeContentAssignments.Count, Is.EqualTo(4));
            Assert.That(run.EncounterAssignments.ContainsKey("core_finale"), Is.True);
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(run).Summary);
            Assert.That(run.AvailableNodes.Select(node => node.Id), Does.Contain("academy_gate"));
            Assert.That(run.AvailableNodes.Select(node => node.Id), Does.Contain("dorm_drill"));
        }

        [Test]
        public void GeneratedAcademyMaps_AreSeededReachableAndSaveTheirExactLinks()
        {
            string first = null;
            bool sawVariation = false;
            for (int seed = 1; seed <= 64; seed++)
            {
                IReadOnlyList<RogueliteMapNode> nodes = RogueliteAcademyMapGenerator.Generate(seed);
                string[] rows = RogueliteAcademyMapGenerator.Encode(nodes).ToArray();
                Assert.That(nodes.Count, Is.EqualTo(20));
                Assert.That(nodes.Count(node => node.Type == RogueliteMapNodeType.Combat), Is.EqualTo(9));
                Assert.That(nodes.Count(node => node.Type == RogueliteMapNodeType.Elite), Is.EqualTo(3));
                Assert.That(nodes.Count(node => node.Type == RogueliteMapNodeType.Event), Is.EqualTo(4));
                Assert.That(nodes.Count(node => RogueliteAcademyLayerCatalog.IsServiceNode(node.Id)), Is.EqualTo(3));
                Assert.That(RogueliteAcademyMapGenerator.Encode(RogueliteAcademyMapGenerator.Decode(rows)), Is.EqualTo(rows));
                Assert.That(RogueliteAcademyMapGenerator.Encode(RogueliteAcademyMapGenerator.Generate(seed)), Is.EqualTo(rows));
                if (first == null) first = string.Join("|", rows);
                else if (string.Join("|", rows) != first) sawVariation = true;
            }
            Assert.That(sawVariation, Is.True);
        }

        [Test]
        public void AcademyRewardPackages_HaveEveryStageForGeneratedBattles()
        {
            for (int seed = 0; seed < 64; seed++)
                foreach (RogueliteMapNode node in RogueliteAcademyMapGenerator.Generate(seed).Where(value => value.IsCombat))
                {
                    AcademyBattleRewardPackage package = AcademyBattleRewardCatalog.Roll(seed, node.Id, Array.Empty<string>());
                    Assert.That(package.MainIds.Count, Is.EqualTo(3), node.Id + " @ " + seed);
                    Assert.That(package.MainIds.Distinct().Count(), Is.EqualTo(3), node.Id + " @ " + seed);
                    Assert.That(package.FollowupIds.Count, Is.EqualTo(node.Type == RogueliteMapNodeType.Combat &&
                        (RogueliteAcademyLayerCatalog.Mapping(node.Id).ContentTableId == "N01" ||
                         RogueliteAcademyLayerCatalog.Mapping(node.Id).ContentTableId == "N02") ? 0 :
                        node.Type == RogueliteMapNodeType.Finale ? 3 : 2), node.Id + " @ " + seed);
                    if (node.Type == RogueliteMapNodeType.Finale)
                        Assert.That(package.FixedMaterialIds.Count, Is.EqualTo(2));
                }
        }

        [Test]
        public void BossRewardChoices_PersistIndependentlyUntilRoundSettlement()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4306);
            run.ConfirmAcademyDeparture();
            Assert.That(RogueliteDeveloperRunPolicy.AdvanceToFinale(run).ReachedFinale, Is.True);
            int forgeBeforeBoss = run.ForgeMaterialCount;
            int specBeforeBoss = run.SpecializationMaterialCount;
            int goldBeforeBoss = run.Gold;
            int contributionBeforeBoss = run.StageContribution;
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run, false);
            Assert.That(run.AwaitingReward, Is.True);
            Assert.That(run.CurrentRewards.Count, Is.EqualTo(3));
            Assert.That(run.RogueRunState.PendingFixedMaterialIds,
                Is.EquivalentTo(new[] { AcademyBattleRewardCatalog.ForgeLoad, AcademyBattleRewardCatalog.SpecAmplify }));
            Assert.That(run.ForgeMaterialCount, Is.EqualTo(forgeBeforeBoss));
            Assert.That(run.SpecializationMaterialCount, Is.EqualTo(specBeforeBoss));
            Assert.That(run.Gold, Is.EqualTo(goldBeforeBoss));
            Assert.That(run.StageContribution, Is.EqualTo(contributionBeforeBoss));
            Assert.That(run.RogueRunState.PendingRewardGold, Is.EqualTo(10));
            Assert.That(run.RogueRunState.PendingRewardContribution, Is.EqualTo(3));
            string[] firstChoices = run.CurrentRewards.Select(value => value.Id).ToArray();
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(saves.Save(run), Is.True, gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun loaded), Is.True, gateway.LastError);
            Assert.That(loaded.CurrentRewards.Select(value => value.Id), Is.EqualTo(firstChoices));
            loaded.ClaimReward(firstChoices[0]);
            Assert.That(loaded.PendingRewardStepId, Is.EqualTo("followup"));
            Assert.That(loaded.CurrentRewards.Count, Is.EqualTo(3));
            string[] secondChoices = loaded.CurrentRewards.Select(value => value.Id).ToArray();
            Assert.That(saves.Save(loaded), Is.True, gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun restored), Is.True, gateway.LastError);
            Assert.That(restored.CurrentRewards.Select(value => value.Id), Is.EqualTo(secondChoices));
            restored.ClaimReward(secondChoices[0]);
            Assert.That(restored.IsComplete, Is.True);
            Assert.That(restored.AwaitingReward, Is.False);
            Assert.That(restored.ForgeMaterialCount, Is.GreaterThanOrEqualTo(forgeBeforeBoss + 1));
            Assert.That(restored.SpecializationMaterialCount, Is.GreaterThanOrEqualTo(specBeforeBoss + 1));
            Assert.That(restored.Gold, Is.EqualTo(goldBeforeBoss + 10));
            Assert.That(restored.StageContribution, Is.EqualTo(contributionBeforeBoss + 3));
        }

        [Test]
        public void BossEquipmentReward_CanBeDismantledIntoSavedForgeMaterial()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4308);
            run.ConfirmAcademyDeparture();
            Assert.That(RogueliteDeveloperRunPolicy.AdvanceToFinale(run).ReachedFinale, Is.True);
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run, false);
            RogueliteReward equipment = run.CurrentRewards.Single(value => value.Kind == RogueliteRewardKind.Equipment);
            int before = run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad);
            Assert.That(run.CanDismantleReward(equipment), Is.True);
            run.ClaimReward("dismantle:" + equipment.Id);
            Assert.That(run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad), Is.EqualTo(before));
            Assert.That(run.PendingRewardStepId, Is.EqualTo("followup"));
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(new RogueliteMapSaveCoordinator(gateway).Save(run), Is.True, gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun restored), Is.True, gateway.LastError);
            Assert.That(restored.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad), Is.EqualTo(before));
            restored.ClaimReward(restored.CurrentRewards.First(value => value.RogueSpell != null).Id);
            Assert.That(restored.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad), Is.GreaterThanOrEqualTo(before + 2));
        }

        [Test]
        public void BossFixedMaterials_WaitForBackpackSpaceBeforeFinalClaim()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4314);
            run.ConfirmAcademyDeparture();
            Assert.That(RogueliteDeveloperRunPolicy.AdvanceToFinale(run).ReachedFinale, Is.True);
            RogueEquipmentRuntime inventory = RogueEquipmentRuntime.FromDto(run.RogueRunState);
            int fill = 0;
            while (inventory.CanFitMaterials(1))
            {
                RogueEquipmentInstance item = inventory.CreateInstance("filler-" + fill++, "ACA-EQ-AC01",
                    EquipmentRarity.Common, 1000 + fill, "test");
                Assert.That(inventory.AddToBackpack(item), Is.True);
            }
            inventory.WriteToDto(run.RogueRunState);
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run, false);
            RogueliteReward firstSpell = run.CurrentRewards.First(value => value.RogueSpell != null);
            run.ClaimReward(firstSpell.Id);
            RogueliteReward finalSpell = run.CurrentRewards.First(value => value.RogueSpell != null);
            Assert.That(run.CanAcceptAcademyReward(finalSpell), Is.False);
            Assert.Throws<InvalidOperationException>(() => run.ClaimReward(finalSpell.Id));
            Assert.That(run.AwaitingReward, Is.True);
            Assert.That(run.RogueRunState.PendingFixedMaterialIds.Count, Is.EqualTo(2));

            foreach (EquipmentInstanceDto item in run.RogueRunState.EquipmentInstances
                .Where(value => value.InstanceId.StartsWith("filler-", StringComparison.Ordinal)).Take(2).ToArray())
                run.RogueRunState.EquipmentInstances.Remove(item);
            Assert.That(run.CanAcceptAcademyReward(finalSpell), Is.True);
            run.ClaimReward(finalSpell.Id);
            Assert.That(run.RogueRunState.PendingFixedMaterialIds, Is.Empty);
            Assert.That(run.IsComplete, Is.True);
        }

        [Test]
        public void SettledFirstRun_CreatesOneSubsequentRoundThenRequiresSavedDeparture()
        {
            RogueliteMapRun first = ReadyForShop(4301);
            first.CompleteFirstRunExperience();
            Assert.That(RogueliteDeveloperRunPolicy.AdvanceToFinale(first).ReachedFinale, Is.True);
            Assert.That(RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(first).RoundSettled, Is.True);
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(saves.Save(first), Is.True, gateway.LastError);

            RogueliteMapStartResult created = saves.TryStartSubsequentAcademyRound(4302);
            Assert.That(created.Success, Is.True, created.FailureMessage);
            Assert.That(created.Run.IsFirstRunExperience, Is.False);
            Assert.That(created.Run.IsInAcademyLayer, Is.True);
            Assert.That(created.Run.DeparturePending, Is.True);
            Assert.That(created.Run.MapNodes.Count, Is.EqualTo(20));
            Assert.That(created.Run.StageTime, Is.Zero);
            Assert.That(created.Run.IsNodeAvailable("dorm_drill"), Is.False);
            Assert.That(RogueliteMapRunValidator.Validate(created.Run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(created.Run).Summary);
            Assert.That(saves.TryStartSubsequentAcademyRound(4303).Success, Is.False,
                "An active departure cannot be replaced by another round.");

            RogueliteMapRun restored = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(restored.DeparturePending, Is.True);
            restored.ConfirmAcademyDeparture();
            Assert.That(saves.Save(restored), Is.True, gateway.LastError);
            RogueliteMapRun departed = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(departed.DeparturePending, Is.False);
            Assert.That(departed.IsNodeAvailable("dorm_drill"), Is.True);
            Assert.That(RogueliteMapRunValidator.Validate(departed).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(departed).Summary);
        }

        [Test]
        public void SubsequentRoundWriteFailure_PreservesSettledFirstRun()
        {
            RogueliteMapRun first = ReadyForShop(4304);
            first.CompleteFirstRunExperience();
            RogueliteDeveloperRunPolicy.AdvanceToFinale(first);
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(first);
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(saves.Save(first), Is.True, gateway.LastError);
            string before = store.Values[RogueliteSaveGateway.MapRunKey];
            store.FailWrites = true;

            Assert.That(saves.TryStartSubsequentAcademyRound(4305).Success, Is.False);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Is.EqualTo(before));
            store.FailWrites = false;
            Assert.That(saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run.IsComplete, Is.True);
        }

        [Test]
        public void SubsequentRound_CanFinishAndCreateAnotherRoundWithoutTutorial()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4306);
            run.ConfirmAcademyDeparture();
            Assert.That(RogueliteDeveloperRunPolicy.AdvanceToFinale(run).ReachedFinale, Is.True);
            Assert.That(RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run).RoundSettled, Is.True);
            Assert.That(run.RunEndReason, Is.EqualTo("victory"));
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(run).Summary);
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(saves.Save(run), Is.True, gateway.LastError);
            RogueliteMapRun restored = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(restored.IsComplete, Is.True);
            Assert.That(restored.FirstRunExperience, Is.Null);
            Assert.That(saves.TryStartSubsequentAcademyRound(4307).Success, Is.True);
        }

        [Test]
        public void SubsequentShop_PreservesSoldOffersAcrossSaveAndReturn()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4310);
            run.ConfirmAcademyDeparture();
            TravelToService(run, "layer_shop");
            run.PurchaseFirstRunOffer("LAYER-SHOP-WEDGE");
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(saves.Save(run), Is.True, gateway.LastError);

            RogueliteMapRun restored = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(restored.CurrentShopService.Offers.Single(value => value.OfferId == "LAYER-SHOP-WEDGE").Sold, Is.True);
            Assert.Throws<InvalidOperationException>(() => restored.PurchaseFirstRunOffer("LAYER-SHOP-WEDGE"));
            restored.SettleCurrentServiceNode();
            Assert.That(saves.Save(restored), Is.True, gateway.LastError);
            RogueliteMapRun returned = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(returned.CurrentShopService.Offers.Single(value => value.OfferId == "LAYER-SHOP-WEDGE").Sold, Is.True);
        }

        [Test]
        public void SubsequentMedical_PreservesItsSeparateServiceLimits()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            run.ConfirmAcademyDeparture();
            TravelToService(run, "layer_medical");
            run.CompleteFirstRunHealthCheck();
            run.ChooseFirstRunMeal("MEAL-POWER");
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(saves.Save(run), Is.True, gateway.LastError);

            RogueliteMapRun restored = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(restored.CurrentMedicalService.HealthCheckCompleted, Is.True);
            Assert.That(restored.CurrentMedicalService.MealUsed, Is.True);
            Assert.That(restored.CurrentMedicalService.SelectedMealId, Is.EqualTo("MEAL-POWER"));
            Assert.Throws<InvalidOperationException>(() => restored.ChooseFirstRunMeal("MEAL-POWER"));
        }

        [Test]
        public void SubsequentWorkshop_PreservesBothCompletedOperations()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4312);
            run.ConfirmAcademyDeparture();
            run.RogueRunState.ForgeMaterialCount = 1;
            run.RogueRunState.SpecializationMaterialCount = 1;
            Assert.That(run.RogueRunState.EquipmentInstances, Is.Not.Empty);
            TravelToService(run, "layer_workshop");
            string equipmentId = run.RogueRunState.EquipmentInstances.First().InstanceId;
            const string spellId = "BASE-FIRE-MELEE";
            run.CompleteFirstRunForge(equipmentId);
            run.CompleteFirstRunSpecialization(spellId);
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(saves.Save(run), Is.True, gateway.LastError);

            RogueliteMapRun restored = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(restored.CurrentWorkshopService.ForgeCompleted, Is.True);
            Assert.That(restored.CurrentWorkshopService.SpecializationCompleted, Is.True);
            Assert.That(restored.CurrentWorkshopService.ForgedTargetId, Is.EqualTo(equipmentId));
            Assert.That(restored.CurrentWorkshopService.SpecializedTargetId, Is.EqualTo(spellId));
            Assert.Throws<InvalidOperationException>(() => restored.CompleteFirstRunForge(equipmentId));
        }

        [Test]
        public void SubsequentWorkshop_PreservesCircuitAndEfficientMaterialIdentity()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4313);
            run.ConfirmAcademyDeparture();
            Assert.That(run.RogueRunState.EquipmentInstances, Is.Not.Empty);
            run.RogueRunState.ForgeMaterialCount = 1;
            run.RogueRunState.SpecializationMaterialCount = 1;
            run.RogueRunState.MaterialStockRows.Add(AcademyBattleRewardCatalog.ForgeCircuit + "=1");
            run.RogueRunState.MaterialStockRows.Add(AcademyBattleRewardCatalog.SpecEfficient + "=1");
            TravelToService(run, "layer_workshop");
            run.CompleteFirstRunForge("starter-chest", AcademyBattleRewardCatalog.ForgeCircuit);
            run.CompleteFirstRunSpecialization("BASE-FIRE-MELEE", AcademyBattleRewardCatalog.SpecEfficient);
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store));
            Assert.That(saves.Save(run), Is.True);

            RogueliteMapRun restored = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(restored.CurrentWorkshopService.ForgedMaterialId, Is.EqualTo(AcademyBattleRewardCatalog.ForgeCircuit));
            Assert.That(restored.CurrentWorkshopService.SpecializedMaterialId, Is.EqualTo(AcademyBattleRewardCatalog.SpecEfficient));
            Assert.That(restored.RogueRunState.EquipmentInstances.Single(value => value.InstanceId == "starter-chest").ForgeMaterialId,
                Is.EqualTo(AcademyBattleRewardCatalog.ForgeCircuit));
            Assert.That(restored.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeCircuit), Is.Zero);
            Assert.That(restored.AcademyMaterialCount(AcademyBattleRewardCatalog.SpecEfficient), Is.Zero);
        }

        [Test]
        public void AcademyMaterialDiscard_FreesBackpackCellAndPersists()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4315);
            run.ConfirmAcademyDeparture();
            run.RogueRunState.ForgeMaterialCount = 1;
            run.RogueRunState.MaterialStockRows.Add(AcademyBattleRewardCatalog.ForgeCircuit + "=1");
            RogueEquipmentRuntime before = RogueEquipmentRuntime.FromDto(run.RogueRunState);
            string materialId = before.Backpack.Keys.Single(id => before.MaterialIdFor(id) == AcademyBattleRewardCatalog.ForgeCircuit);

            Assert.That(run.DiscardRogueBackpackItem(materialId), Is.True);
            Assert.That(run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeCircuit), Is.Zero);
            RogueEquipmentRuntime after = RogueEquipmentRuntime.FromDto(run.RogueRunState);
            Assert.That(after.Backpack.Keys.Any(id => after.MaterialIdFor(id) == AcademyBattleRewardCatalog.ForgeCircuit), Is.False);
            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store));
            Assert.That(saves.Save(run), Is.True);
            Assert.That(saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeCircuit), Is.Zero);
        }

        [Test]
        public void FirstStageEvents_UseTheirDeclaredChoicesAndEventCombatDoesNotGrantRegularBattlePay()
        {
            RogueliteMapRun run = ReadyForShop(4291);
            run.CompleteFirstRunExperience();
            Assert.That(run.CurrentEventId, Is.EqualTo("EV01"));
            Assert.That(AcademyNodeContentCatalog.Event("EV08").Choices.Select(choice => choice.Id),
                Is.EquivalentTo(new[] { "EV08_read", "EV08_recover" }));
            Assert.That(AcademyNodeContentCatalog.Event("EV08").Choices.Any(choice => choice.RewardId == "G-T19"), Is.False);
            Assert.That(AcademyNodeContentCatalog.Event("EV09").Choices.Single(choice => choice.RequiresCombat).RewardId,
                Is.EqualTo("G-T06"));
            Assert.That(run.NodeContentAssignments["tower_lift"], Is.EqualTo("T02"));
            Assert.That(AcademyNodeContentCatalog.GenerateAssignments(4291).Any(value => value.EventId == "T02"), Is.False);
            Assert.That(AcademyMapTuning.TimeCost(run.MapNode("tower_lift")), Is.Zero);

            int gold = run.Gold;
            int contribution = run.StageContribution;
            int stageTime = run.StageTime;
            run.ChooseCurrentNodeContent("EV01_drill");
            Assert.That(run.HasPendingContentCombat, Is.True);
            RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.For(run, run.PendingContentCombatMissionId);
            Assert.That(encounter.VariantKey, Is.EqualTo("weak_flank_drill"));
            Assert.That(encounter.EnemyArchetypeIds, Is.EquivalentTo(new[] { "raider", "shieldguard" }));
            CombatSceneSessionBuild battle = new CombatSceneSessionBuilder().Build(run, null,
                Array.Empty<CombatSceneMarker>());
            Assert.That(battle.State.Units.Values.Count(unit => !unit.IsHero), Is.EqualTo(2));
            run.CompletePendingContentCombat();
            Assert.That(run.Gold, Is.EqualTo(gold + 1));
            Assert.That(run.StageContribution, Is.EqualTo(contribution));
            Assert.That(run.StageTime, Is.EqualTo(stageTime + 1));
            Assert.That(run.PendingResourceReceipt, Is.Not.Null);
            Assert.That(run.AwaitingReward, Is.True);
            Assert.That(run.CurrentRewards, Is.Empty);
        }

        [Test]
        public void ResourceOnlyEventReceipt_SurvivesReloadAndAcknowledgmentDoesNotPayTwice()
        {
            RogueliteMapRun run = ReadyForShop(4294);
            run.CompleteFirstRunExperience();
            int goldBefore = run.Gold;
            run.ChooseCurrentNodeContent("EV01_drill");
            run.CompletePendingContentCombat();
            Assert.That(run.Gold, Is.EqualTo(goldBefore + 1));
            Assert.That(run.PendingResourceReceipt.GoldChange, Is.EqualTo(1));
            Assert.That(run.PendingResourceReceipt.ContributionChange, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => run.AbandonCurrentReward());

            MemoryStore store = new MemoryStore();
            RogueliteMapSaveCoordinator saves = new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store));
            Assert.That(saves.Save(run), Is.True);
            RogueliteMapRun restored = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(restored.PendingResourceReceipt.ChoiceId, Is.EqualTo("EV01_drill"));
            Assert.That(restored.CurrentRewards, Is.Empty);
            Assert.That(restored.Gold, Is.EqualTo(goldBefore + 1));
            restored.ConfirmResourceReceipt();
            store.FailWrites = true;
            Assert.That(saves.Save(restored), Is.False);
            store.FailWrites = false;
            RogueliteMapRun afterFailedWrite = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(afterFailedWrite.PendingResourceReceipt.ChoiceId, Is.EqualTo("EV01_drill"));
            Assert.That(afterFailedWrite.Gold, Is.EqualTo(goldBefore + 1));
            afterFailedWrite.ConfirmResourceReceipt();
            restored = afterFailedWrite;
            Assert.That(saves.Save(restored), Is.True);

            RogueliteMapRun confirmed = saves.TryStart(true, FireRogueliteStarterCatalog.Universal, 0).Run;
            Assert.That(confirmed.PendingResourceReceipt, Is.Null);
            Assert.That(confirmed.AwaitingReward, Is.False);
            Assert.That(confirmed.Gold, Is.EqualTo(goldBefore + 1));
            Assert.Throws<InvalidOperationException>(() => confirmed.ConfirmResourceReceipt());
        }

        [Test]
        public void ResourceOnlyNonCombatEvent_RecordsTheActualResourceChanges()
        {
            RogueliteMapRun run = ReadyForShop(4295);
            run.CompleteFirstRunExperience();
            TravelToService(run, "tower_records");
            Assert.That(run.CurrentEventId, Is.EqualTo("EV08"));
            int stageTime = run.StageTime;
            int gold = run.Gold;
            run.ChooseCurrentNodeContent("EV08_read");

            AcademyResourceReceipt receipt = run.PendingResourceReceipt;
            Assert.That(receipt, Is.Not.Null);
            Assert.That(receipt.GoldChange, Is.EqualTo(run.Gold - gold));
            Assert.That(receipt.TimeChange, Is.EqualTo(run.StageTime - stageTime));
            Assert.That(run.AwaitingReward, Is.True);
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(run).Summary);
        }

        [Test]
        public void OldTowerLiftAssignment_MigratesToTheDedicatedT02Event()
        {
            RogueliteMapRun run = ReadyForShop(4292);
            run.CompleteFirstRunExperience();
            MemoryStore store = new MemoryStore();
            Assert.That(new RogueliteMapSaveCoordinator(new RogueliteSaveGateway(store)).Save(run), Is.True);
            RogueRunDto dto = Rogue11Serializer.Deserialize(store.Values[RogueliteSaveGateway.MapRunKey]);
            int index = dto.NodeContentAssignments.FindIndex(value => value.StartsWith("tower_lift=", StringComparison.Ordinal));
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            dto.NodeContentAssignments[index] = "tower_lift=EV16";

            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(dto);

            Assert.That(restored.NodeContentAssignments["tower_lift"], Is.EqualTo("T02"));
            Assert.That(RogueliteMapRunValidator.Validate(restored).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(restored).Summary);
        }

        [Test]
        public void AcademyOrdinaryVictory_UsesTheDeclaredFixedGoldAndContribution()
        {
            RogueliteMapRun run = ReadyForShop(4293);
            run.CompleteFirstRunExperience();
            int gold = run.Gold;
            int contribution = run.StageContribution;
            run.SelectNode("dorm_drill");

            run.CompleteCurrentCombat();

            Assert.That(run.Gold, Is.EqualTo(gold));
            Assert.That(run.StageContribution, Is.EqualTo(contribution));
            Assert.That(run.RogueRunState.PendingRewardGold, Is.EqualTo(3));
            Assert.That(run.RogueRunState.PendingRewardContribution, Is.EqualTo(1));
            Assert.That(run.AwaitingReward, Is.True);
            run.ClaimReward(run.CurrentRewards.First(reward => reward.RogueSpell != null).Id);
            if (run.PendingRewardStepId == "followup")
            {
                Assert.That(run.Gold, Is.EqualTo(gold));
                run.ClaimReward(run.CurrentRewards.First().Id);
            }
            Assert.That(run.Gold, Is.EqualTo(gold + 3));
            Assert.That(run.StageContribution, Is.EqualTo(contribution + 1));

            RogueliteMapRun abandoned = ReadyForShop(4295);
            abandoned.CompleteFirstRunExperience();
            int abandonedGold = abandoned.Gold;
            int abandonedContribution = abandoned.StageContribution;
            abandoned.SelectNode("dorm_drill");
            abandoned.CompleteCurrentCombat();
            abandoned.AbandonCurrentReward();
            Assert.That(abandoned.Gold, Is.EqualTo(abandonedGold));
            Assert.That(abandoned.StageContribution, Is.EqualTo(abandonedContribution));
            Assert.That(abandoned.RogueRunState.PendingRewardGold, Is.Zero);
            Assert.That(abandoned.RogueRunState.PendingRewardContribution, Is.Zero);
        }

        [Test]
        public void Ev09Escort_StartsItsDeclaredBarrierExerciseAndKeepsTheSpecifiedReward()
        {
            RogueliteMapRun run = ReadyForShop(4294);
            run.CompleteFirstRunExperience();
            TravelToService(run, "field_infirmary");
            Assert.That(run.CurrentEventId, Is.EqualTo("EV09"));
            run.ChooseCurrentNodeContent("EV09_escort");

            RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.For(run, run.PendingContentCombatMissionId);
            Assert.That(encounter.VariantKey, Is.EqualTo("weak_barrier_demo"));
            Assert.That(encounter.EnemyArchetypeIds, Is.EquivalentTo(new[] { "barrier_mender", "shieldguard" }));
            Assert.That(run.RogueRunState.PendingRewardIds, Contains.Item("G-T06"));
        }

        [Test]
        public void AcademyCombatRewardChoices_StayFixedAfterLoadAndInventoryChanges()
        {
            RogueliteMapRun run = ReadyForShop(4261);
            run.CompleteFirstRunExperience();
            run.SelectNode("dorm_drill");
            run.CompleteCurrentCombat();
            string[] choices = run.CurrentRewards.Select(reward => reward.Id).ToArray();
            Assert.That(choices, Is.Not.Empty);
            Assert.That(run.RogueRunState.RolledRewardChoiceIds, Is.EqualTo(choices));

            run.RogueRunState.MasteredSpellIds.AddRange(choices.Where(id =>
                RogueContentCatalog.CreateAcademyV01().Spells.Any(spell => spell.DefinitionId == id)));
            Assert.That(run.CurrentRewards.Select(reward => reward.Id), Is.EqualTo(choices));
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(new MemoryStore());
            RogueliteMapSaveCoordinator coordinator = new RogueliteMapSaveCoordinator(gateway);
            Assert.That(coordinator.Save(run), Is.True);
            RogueliteMapRun restored;
            Assert.That(gateway.TryLoadMapRun(out restored), Is.True, gateway.LastError);
            Assert.That(restored.CurrentRewards.Select(reward => reward.Id), Is.EqualTo(choices));
            Assert.That(RogueliteMapRunValidator.Validate(restored).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(restored).Summary);

            restored.AbandonCurrentReward();
            Assert.That(restored.RogueRunState.RolledRewardChoiceIds, Is.Empty);
        }

        [Test]
        public void LegacyPendingCombatReward_GainsAStableChoiceSnapshotOnLoad()
        {
            RogueliteMapRun run = ReadyForShop(4262);
            run.CompleteFirstRunExperience();
            run.SelectNode("dorm_drill");
            run.CompleteCurrentCombat();
            string[] original = run.CurrentRewards.Select(reward => reward.Id).ToArray();
            run.RogueRunState.RolledRewardChoiceIds.Clear();

            RogueliteSaveGateway gateway = new RogueliteSaveGateway(new MemoryStore());
            Assert.That(new RogueliteMapSaveCoordinator(gateway).Save(run), Is.True,
                gateway.LastError + " / " + RogueliteMapRunValidator.Validate(run).Summary);
            RogueliteMapRun restored;
            Assert.That(gateway.TryLoadMapRun(out restored), Is.True, gateway.LastError);
            Assert.That(restored.RogueRunState.RolledRewardChoiceIds, Is.EqualTo(original));
            Assert.That(restored.CurrentRewards.Select(reward => reward.Id), Is.EqualTo(original));
        }

        [Test]
        public void RandomLayerDefeat_ClosesWithoutGrantingNodeOrReward()
        {
            RogueliteMapRun run = ReadyForShop(4250);
            run.CompleteFirstRunExperience();
            run.SelectNode("dorm_drill");

            run.CloseAsFailure(false);
            Assert.That(run.IsComplete, Is.True);
            Assert.That(run.CompletedNodes, Does.Not.Contain("dorm_drill"));
            Assert.That(run.AwaitingReward, Is.False);
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(run).Summary);

            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(
                Rogue11Serializer.Serialize(run.RogueRunState)));
            Assert.That(restored.RunEndReason, Is.EqualTo("death"));
            Assert.That(restored.IsComplete, Is.True);
            Assert.That(restored.CurrentNodeId, Is.EqualTo("dorm_drill"));
        }

        [Test]
        public void EventCombatDefeat_ClosesWithoutApplyingTheChosenEventReward()
        {
            RogueliteMapRun run = ReadyForShop(4251);
            run.CompleteFirstRunExperience();
            int goldBefore = run.Gold;
            run.ChooseCurrentNodeContent("EV01_drill");
            Assert.That(run.HasPendingContentCombat, Is.True);

            run.CloseAsFailure(false);

            Assert.That(run.IsComplete, Is.True);
            Assert.That(run.CompletedNodes, Does.Not.Contain(run.CurrentNodeId));
            Assert.That(run.PendingResourceReceipt, Is.Null);
            Assert.That(run.Gold, Is.EqualTo(goldBefore));
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(run).Summary);
            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(
                Rogue11Serializer.Serialize(run.RogueRunState)));
            Assert.That(restored.RunEndReason, Is.EqualTo("death"));
            Assert.That(restored.Gold, Is.EqualTo(goldBefore));
        }

        [TestCase("academy_gate", "新生实战委托")]
        [TestCase("tutorial_hall", "中庭侧锋对练")]
        [TestCase("field_infirmary", "医务室临时征集")]
        [TestCase("supply_depot", "刻阵工坊高阶考核")]
        [TestCase("tower_foyer", "刻阵工坊高阶考核")]
        [TestCase("core_finale", "Boss战斗 01 · 古塔核心")]
        [TestCase("layer_medical", "医务室")]
        public void AcademyLayerNode_UsesActiveContentTitleWithoutChangingLegacyNode(string nodeId, string title)
        {
            Assert.That(RogueliteAcademyLayerCatalog.TryResolveLayerNode(nodeId, out RogueliteMapNode node), Is.True);
            Assert.That(node.DisplayName, Is.EqualTo(title));
            if (!RogueliteAcademyLayerCatalog.IsServiceNode(nodeId))
                Assert.That(node.NextIds, Is.EqualTo(RogueliteMapCatalog.Node(nodeId).NextIds));
        }

        [Test]
        public void EveryAcademyLayerNode_HasAContentTableMapping()
        {
            Assert.That(RogueliteAcademyLayerCatalog.ContentMappings.Count, Is.EqualTo(RogueliteAcademyLayerCatalog.NodeIds.Count));
            foreach (string nodeId in RogueliteAcademyLayerCatalog.NodeIds)
            {
                RogueliteAcademyLayerCatalog.AcademyNodeContentMapping mapping = RogueliteAcademyLayerCatalog.Mapping(nodeId);
                Assert.That(mapping.NodeId, Is.EqualTo(nodeId));
                Assert.That(mapping.ContentTableId, Is.Not.Empty, nodeId);
                RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
                if (node.IsCombat) Assert.That(mapping.EncounterVariantId, Is.Not.Empty, nodeId);
            }
        }

        [Test]
        public void AcademyLayerEncounters_AreTierMatchedAndNeverRepeatAcrossAdjacentNodes()
        {
            IReadOnlyList<RogueliteMapNode> combatNodes = RogueliteAcademyLayerCatalog.EncounterNodeIds
                .Select(RogueliteMapCatalog.Node).ToArray();
            Assert.That(combatNodes.Count(node => node.Type == RogueliteMapNodeType.Combat), Is.EqualTo(RogueliteAcademyLayerCatalog.ExpectedWeakCount + RogueliteAcademyLayerCatalog.ExpectedStrongCount));
            Assert.That(combatNodes.Count(node => node.Type == RogueliteMapNodeType.Elite), Is.EqualTo(4));
            Assert.That(combatNodes.Count(node => node.Type == RogueliteMapNodeType.Finale), Is.EqualTo(1));

            foreach (RogueliteMapNode node in combatNodes)
            {
                RogueliteEncounterDefinition definition = RogueliteEncounterCatalog.Package(RogueliteAcademyLayerCatalog.Mapping(node.Id).EncounterVariantId);
                bool tierValid = node.Type == RogueliteMapNodeType.Combat &&
                        (definition.Tier == RogueliteEncounterTier.Weak || definition.Tier == RogueliteEncounterTier.Strong) ||
                    node.Type == RogueliteMapNodeType.Elite && definition.Tier == RogueliteEncounterTier.Elite ||
                    node.Type == RogueliteMapNodeType.Finale && definition.Tier == RogueliteEncounterTier.Boss;
                Assert.That(tierValid, Is.True, node.Id);
                Assert.That(definition.EnemyArchetypeIds.All(id => EnemyArchetypes.All.Any(enemy => enemy.Id == id)), Is.True, node.Id);
            }
            Assert.That(RogueliteAcademyLayerCatalog.Mapping("core_finale").EncounterVariantId,
                Is.EqualTo(RogueliteEncounterCatalog.FixedBoss.VariantKey));

            foreach (RogueliteMapNode left in combatNodes)
            foreach (RogueliteMapNode right in combatNodes.Where(node => string.CompareOrdinal(node.Id, left.Id) > 0))
            {
                if (!left.NextIds.Contains(right.Id) && !right.NextIds.Contains(left.Id)) continue;
                RogueliteEncounterDefinition a = RogueliteEncounterCatalog.Package(RogueliteAcademyLayerCatalog.Mapping(left.Id).EncounterVariantId);
                RogueliteEncounterDefinition b = RogueliteEncounterCatalog.Package(RogueliteAcademyLayerCatalog.Mapping(right.Id).EncounterVariantId);
                Assert.That(a.LevelId == b.LevelId || a.SpatialGrammar == b.SpatialGrammar, Is.False, left.Id + " ↔ " + right.Id);
            }
        }

        [TestCase(4204)]
        [TestCase(7)]
        [TestCase(913)]
        [TestCase(20260917)]
        public void DeveloperAdvance_RunsTheWholeLayerAndSettlesTheRound(int seed)
        {
            RogueliteMapRun run = ReadyForShop(seed);
            run.CompleteFirstRunExperience();

            RogueliteDeveloperAdvanceReport report = RogueliteDeveloperRunPolicy.AdvanceToFinale(run);

            Assert.That(report.ReachedFinale, Is.True, report.Summary);
            Assert.That(run.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(run.CompletedAcademyNodeCount, Is.GreaterThanOrEqualTo(AcademyMapTuning.BossMinimumProgress));
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True, RogueliteMapRunValidator.Validate(run).Summary);

            RogueliteDeveloperAdvanceReport win = RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run);

            Assert.That(win.RoundSettled, Is.True, win.Summary);
            Assert.That(run.CompletedNodes, Contains.Item(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(run.AwaitingReward, Is.False);
            Assert.That(run.IsComplete, Is.True);
            Assert.That(run.FirstRunExperience.RoundSettled, Is.True);
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True, RogueliteMapRunValidator.Validate(run).Summary);
        }

        [Test]
        public void AcademyLayerRun_SurvivesSaveAndLoadWithAssignmentsAndSettlement()
        {
            const int seed = 4205;
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapSaveCoordinator coordinator = new RogueliteMapSaveCoordinator(gateway);
            RogueliteMapRun run = ReadyForShop(seed);
            run.CompleteFirstRunExperience();
            Assert.That(coordinator.Save(run), Is.True);

            RogueliteMapRun handoff;
            Assert.That(gateway.TryLoadMapRun(out handoff), Is.True, gateway.LastError);
            Assert.That(handoff.IsInAcademyLayer, Is.True);
            Assert.That(RogueliteAcademyMapGenerator.Encode(handoff.MapNodes),
                Is.EqualTo(RogueliteAcademyMapGenerator.Encode(run.MapNodes)));
            Assert.That(handoff.EncounterAssignments.Count, Is.EqualTo(13));
            Assert.That(handoff.NodeContentAssignments.Count, Is.EqualTo(4));
            Assert.That(RogueliteMapRunValidator.Validate(handoff).IsValid, Is.True, RogueliteMapRunValidator.Validate(handoff).Summary);

            RogueliteDeveloperRunPolicy.AdvanceToFinale(handoff);
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(handoff);
            Assert.That(handoff.IsComplete, Is.True);
            Assert.That(coordinator.Save(handoff), Is.True);

            RogueliteMapRun reloaded;
            Assert.That(gateway.TryLoadMapRun(out reloaded), Is.True, gateway.LastError);
            Assert.That(reloaded.IsComplete, Is.True);
            Assert.That(RogueliteAcademyMapGenerator.Encode(reloaded.MapNodes),
                Is.EqualTo(RogueliteAcademyMapGenerator.Encode(run.MapNodes)));
            Assert.That(reloaded.FirstRunExperience.RoundSettled, Is.True);
            Assert.That(reloaded.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(reloaded.EncounterAssignments.Count, Is.EqualTo(13));
            Assert.That(RogueliteMapRunValidator.Validate(reloaded).IsValid, Is.True, RogueliteMapRunValidator.Validate(reloaded).Summary);
        }

        [Test]
        public void RouteHistory_PreservesTutorialOrderAndAcademyHandoffAcrossSave()
        {
            RogueliteMapRun run = ReadyForShop(4213);
            run.CompleteFirstRunExperience();
            string[] expected = run.RouteHistoryNodeIds.ToArray();

            Assert.That(expected.Take(3), Is.EqualTo(new[] { FirstRunExperienceCatalog.OriginNodeId, "B1", "EV1" }));
            Assert.That(expected.Last(), Is.EqualTo(RogueliteAcademyLayerCatalog.EntryNodeIds[0]));

            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(new RogueliteMapSaveCoordinator(gateway).Save(run), Is.True, gateway.LastError);
            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(
                Rogue11Serializer.Deserialize(store.Values[RogueliteSaveGateway.MapRunKey]));
            Assert.That(restored.RouteHistoryNodeIds, Is.EqualTo(expected));
        }

        [Test]
        public void ServiceNodeSettlement_RoundTripsAndOldSaveMigratesToEmptySet()
        {
            RogueliteMapRun run = ReadyForShop(4210);
            run.CompleteFirstRunExperience();
            run.SettleServiceNode("layer_workshop");

            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(new RogueliteMapSaveCoordinator(gateway).Save(run), Is.True, gateway.LastError);
            string current = store.Values[RogueliteSaveGateway.MapRunKey];
            RogueRunDto restoredDto = Rogue11Serializer.Deserialize(current);
            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(restoredDto);
            Assert.That(restored.SettledServiceNodeIds, Is.EquivalentTo(new[] { "layer_workshop" }));
            Assert.That(restored.IsServiceNodeSettled("layer_workshop"), Is.True);
            Assert.That(RogueliteMapRunValidator.Validate(restored).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(restored).Summary);

            string[] fields = current.Split('|');
            string legacy = string.Join("|", fields.Take(32)); // before settled-service, route and run-end fields
            RogueRunDto migrated = Rogue11Serializer.Deserialize(legacy);
            Assert.That(migrated.SettledServiceNodeIds, Is.Empty);
        }

        [Test]
        public void ServiceNodeSettlement_ValidatorRejectsDuplicateAndNonLayerIds()
        {
            RogueliteMapRun run = ReadyForShop(4211);
            run.CompleteFirstRunExperience();
            run.RogueRunState.SettledServiceNodeIds.Add("layer_workshop");
            run.RogueRunState.SettledServiceNodeIds.Add("layer_workshop");
            run.RogueRunState.SettledServiceNodeIds.Add("W");

            RogueliteMapRunValidationResult result = RogueliteMapRunValidator.Validate(run);
            Assert.That(result.Errors, Contains.Item("academy_layer.service_settlement_duplicate"));
            Assert.That(result.Errors, Contains.Item("academy_layer.service_settlement_unknown"));
        }

        [Test]
        public void AcademyLayerWorkshop_UsesFreshNodeStateAndSettlesWithoutTouchingFixedWorkshop()
        {
            RogueliteMapRun run = ReadyForShop(4212);
            FirstRunWorkshopSnapshot fixedWorkshop = run.FirstRunExperience.Workshop;
            Assert.That(fixedWorkshop.ForgeCompleted, Is.True);
            Assert.That(fixedWorkshop.SpecializationCompleted, Is.True);
            run.CompleteFirstRunExperience();

            TravelToService(run, "layer_workshop");
            FirstRunWorkshopSnapshot layerWorkshop = run.CurrentWorkshopService;
            Assert.That(layerWorkshop, Is.Not.SameAs(fixedWorkshop));
            Assert.That(layerWorkshop.ForgeCompleted, Is.False);
            Assert.That(layerWorkshop.SpecializationCompleted, Is.False);

            run.SettleCurrentServiceNode();
            Assert.That(run.IsServiceNodeSettled("layer_workshop"), Is.True);
            Assert.That(run.CompletedNodes, Contains.Item("layer_workshop"));
            Assert.That(fixedWorkshop.ForgeCompleted, Is.True);
            Assert.That(fixedWorkshop.SpecializationCompleted, Is.True);
        }

        [Test]
        public void BossGate_RequiresTenCompletedAcademyNodes()
        {
            RogueliteMapRun run = ReadyForShop(4206);
            run.CompleteFirstRunExperience();
            Assert.That(run.CanChallengeAcademyFinale, Is.False);
            Assert.That(run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), Is.False);
            // 交付前首领仍属于未探明区域，地图只会提示“还看不清这里”。
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, RogueliteMapCatalog.Node("core_finale")),
                Is.EqualTo("还看不清这里"));

            int guard = 0;
            while (!run.CanChallengeAcademyFinale && guard++ < 40)
            {
                RogueliteMapNode next = RogueliteDeveloperRunPolicy.NextNodeTowardsFinale(run);
                if (next == null) break;
                RogueliteDeveloperRunPolicy.TravelTo(run, next.Id);
                RogueliteDeveloperRunPolicy.TryResolveCurrentNode(run, null);
            }

            Assert.That(run.CanChallengeAcademyFinale, Is.True);
            Assert.That(run.CompletedAcademyNodeCount, Is.GreaterThanOrEqualTo(AcademyMapTuning.BossMinimumProgress));
            // 终考是阶段转换，不要求先沿普通路线走到首领图标旁。
            Assert.That(RogueliteMapVisualPresentation.AcademyStatus(run), Does.Contain("现在可以参加终考"));
            Assert.That(run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), Is.True);

            // 开发推进策略使用同一终考转换规则，停在首领入口。
            RogueliteDeveloperAdvanceReport report = RogueliteDeveloperRunPolicy.AdvanceToFinale(run);
            Assert.That(report.ReachedFinale, Is.True, report.Summary);
            Assert.That(run.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), Is.True);
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, RogueliteMapCatalog.Node("core_finale")),
                Is.EqualTo("你就在这里"));
        }

        [Test]
        public void AcademyTimeTwentyFour_ClosesOrdinaryRoutesAndPersistsFinaleTransition()
        {
            RogueliteMapRun run = ReadyForShop(4207);
            run.CompleteFirstRunExperience();
            RogueliteDeveloperRunPolicy.TryResolveCurrentNode(run, null);
            if (run.HasPendingContentCombat) RogueliteDeveloperRunPolicy.TryResolveCurrentNode(run, null);
            run.RogueRunState.StageTime = AcademyMapTuning.TransitionProgress;

            Assert.That(run.IsTransitionPending, Is.True);
            Assert.That(run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), Is.True);
            Assert.That(run.AvailableNodes.Where(node => node.Id != run.CurrentNodeId)
                .Select(node => node.Id), Is.EquivalentTo(new[] { RogueliteAcademyLayerCatalog.FinaleNodeId }));

            run.SelectNode(RogueliteAcademyLayerCatalog.FinaleNodeId);
            Assert.That(run.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(run.MapNodes.Where(node => node.Type != RogueliteMapNodeType.Finale)
                .Any(node => run.IsNodeAvailable(node.Id)), Is.False);

            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(new RogueliteMapSaveCoordinator(gateway).Save(run), Is.True, gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun restored), Is.True, gateway.LastError);
            Assert.That(restored.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(restored.IsTransitionPending, Is.True);
            Assert.That(restored.MapNodes.Where(node => node.Type != RogueliteMapNodeType.Finale)
                .Any(node => restored.IsNodeAvailable(node.Id)), Is.False);
        }

        [Test]
        public void AcademyTimeTwentyFour_WaitsForCurrentNodeSettlementBeforeFinale()
        {
            RogueliteMapRun run = ReadyForShop(4208);
            run.CompleteFirstRunExperience();
            RogueliteMapNode next = run.AvailableNodes.First(node => node.Id != run.CurrentNodeId && node.Type != RogueliteMapNodeType.Finale);
            run.SelectNode(next.Id);
            run.RogueRunState.StageTime = AcademyMapTuning.TransitionProgress;

            Assert.That(run.IsTransitionPending, Is.True);
            Assert.That(run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), Is.False);
            Assert.That(run.CurrentNodeId, Is.EqualTo(next.Id));
        }

        [Test]
        public void ThirdBattleReward_AllowsDirectEliteWithoutWorkshopOrMedical()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(7021);
            run.AcknowledgeFirstRunOrigin();
            ResolveBattle(run, "B1");
            VisitEvent(run, "EV1", 0);
            VisitEvent(run, "EV2", 0);
            ResolveBattle(run, "B2");
            VisitEvent(run, "EV3", 1);
            ResolveBattle(run, "B3");

            Assert.That(run.IsNodeAvailable("X"), Is.True);
            run.SelectNode("X");
            Assert.That(run.CurrentNodeId, Is.EqualTo("X"));
            Assert.That(run.FirstRunExperience.Node("W").Flags & FirstRunNodeFlags.Completed, Is.EqualTo(FirstRunNodeFlags.None));
            Assert.That(run.FirstRunExperience.Node("M").Flags & FirstRunNodeFlags.Completed, Is.EqualTo(FirstRunNodeFlags.None));
        }

        private static RogueliteMapRun ReadyForShop(int seed)
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(seed);
            run.AcknowledgeFirstRunOrigin();
            ResolveBattle(run, "B1");
            VisitEvent(run, "EV1", 0);
            VisitEvent(run, "EV2", 0);
            ResolveBattle(run, "B2");
            VisitEvent(run, "EV3", 1);
            run.SelectNode("W");
            run.CompleteFirstRunForge("equipment");
            run.CompleteFirstRunSpecialization("spell");
            ResolveBattle(run, "B3");
            run.SelectNode("M");
            run.CompleteFirstRunHealthCheck();
            run.SelectNode("X");
            run.CompleteCurrentCombat();
            run.ClaimReward("PASSIVE-ELITE-01");
            run.SelectNode("S");
            return run;
        }

        private static void ResolveBattle(RogueliteMapRun run, string nodeId)
        {
            run.SelectNode(nodeId);
            run.CompleteCurrentCombat();
            run.ClaimReward(run.CurrentFirstRunRewardIds[0]);
        }

        private static void VisitEvent(RogueliteMapRun run, string nodeId, int optionIndex)
        {
            run.SelectNode(nodeId);
            run.ChooseCurrentNodeContent(run.FirstRunExperience.EventForNode(nodeId).OptionIds[optionIndex]);
        }

        private static void TravelToService(RogueliteMapRun run, string targetId)
        {
            HashSet<string> allowed = new HashSet<string>(run.MapNodes.Select(node => node.Id), StringComparer.Ordinal);
            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>(StringComparer.Ordinal);
            queue.Enqueue(run.CurrentNodeId);
            previous[run.CurrentNodeId] = null;
            while (queue.Count > 0 && !previous.ContainsKey(targetId))
            {
                string current = queue.Dequeue();
                RogueliteMapNode currentNode = run.MapNode(current);
                foreach (string next in allowed.Where(id => !previous.ContainsKey(id)))
                {
                    RogueliteMapNode nextNode = run.MapNode(next);
                    if (!currentNode.NextIds.Contains(next) && !nextNode.NextIds.Contains(current)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            Assert.That(previous.ContainsKey(targetId), Is.True, "No academy-layer route to " + targetId);
            List<string> path = new List<string>();
            for (string cursor = targetId; cursor != run.CurrentNodeId; cursor = previous[cursor]) path.Add(cursor);
            path.Reverse();
            foreach (string nodeId in path)
            {
                if (run.CompletedNodes.Contains(nodeId)) RogueliteDeveloperRunPolicy.TravelTo(run, nodeId);
                else run.SelectNode(nodeId);
                if (nodeId != targetId)
                {
                    RogueliteDeveloperRunPolicy.TryResolveCurrentNode(run, null);
                    if (run.HasPendingContentCombat) RogueliteDeveloperRunPolicy.TryResolveCurrentNode(run, null);
                }
            }
        }

        [Test]
        public void AcademyLayer_ExcludesLegacyCrossLayerNodesAndUsesTheDeclaredFirstStageContent()
        {
            Assert.That(RogueliteAcademyLayerCatalog.LayerNodes.Select(node => node.Id),
                Is.EquivalentTo(RogueliteAcademyLayerCatalog.NodeIds));
            Assert.That(RogueliteAcademyLayerCatalog.IsLayerNode("elite_foundry"), Is.False);
            Assert.That(RogueliteAcademyLayerCatalog.IsLayerNode("core_approach"), Is.False);
            Assert.That(RogueliteAcademyLayerCatalog.LayerNodes.Count(node => node.Type == RogueliteMapNodeType.Elite),
                Is.EqualTo(4));

            string[] expectedContentIds =
            {
                "N01", "N02", "N07", "N08", "N09", "N10", "N12", "N13", "N14", "N15", "N17", "N18",
                "E01", "E02", "E03", "EV01", "EV08", "EV09", "T02", "W01", "S01", "SHOP01", "B01"
            };
            Assert.That(RogueliteAcademyLayerCatalog.ContentMappings.Select(mapping => mapping.ContentTableId).Distinct(),
                Is.EquivalentTo(expectedContentIds));
        }

        [Test]
        public void AcademyLayerSave_MigratesLegacyCrossLayerProgressBackToAFormalEntry()
        {
            RogueliteMapRun source = ReadyForShop(4212);
            source.CompleteFirstRunExperience();
            MemoryStore store = new MemoryStore();
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(new RogueliteMapSaveCoordinator(gateway).Save(source), Is.True, gateway.LastError);
            RogueRunDto dto = Rogue11Serializer.Deserialize(store.Values[RogueliteSaveGateway.MapRunKey]);
            dto.CurrentNodeId = "elite_foundry";
            dto.VisitedNodeIds.Add("elite_foundry");
            dto.CompletedNodeIds.Add("core_approach");

            RogueliteMapRun restored = RogueliteMapRun.FromRogue11(dto);

            Assert.That(restored.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.EntryNodeIds[0]));
            Assert.That(restored.VisitedNodes, Does.Not.Contain("elite_foundry"));
            Assert.That(restored.CompletedNodes, Does.Not.Contain("core_approach"));
            Assert.That(RogueliteMapRunValidator.Validate(restored).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(restored).Summary);
        }

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool FailWrites { get; set; }
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) { if (FailWrites) throw new InvalidOperationException("Simulated save failure."); Values[key] = value; }
            public void DeleteKey(string key) { Values.Remove(key); }
            public void Flush() { }
        }
    }
}
