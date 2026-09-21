using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

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

            Assert.That(run.MapNodes.Count, Is.EqualTo(RogueliteAcademyLayerCatalog.LayerNodes.Count));
            Assert.That(run.MapNodes.Select(node => node.Id), Contains.Item("academy_gate"));
            Assert.That(run.CurrentNodeId, Is.EqualTo("academy_gate"));
            Assert.That(run.EncounterAssignments.Count, Is.EqualTo(RogueliteAcademyLayerCatalog.EncounterNodeIds.Count));
            Assert.That(run.NodeContentAssignments.Count, Is.EqualTo(RogueliteAcademyLayerCatalog.EventNodeIds.Count));
            Assert.That(run.EncounterAssignments.ContainsKey("core_finale"), Is.True);
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True,
                RogueliteMapRunValidator.Validate(run).Summary);
            Assert.That(run.AvailableNodes.Select(node => node.Id), Is.EquivalentTo(new[] { "academy_gate", "tutorial_hall", "dorm_drill" }));
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
            Assert.That(handoff.EncounterAssignments.Count, Is.EqualTo(RogueliteAcademyLayerCatalog.EncounterNodeIds.Count));
            Assert.That(handoff.NodeContentAssignments.Count, Is.EqualTo(RogueliteAcademyLayerCatalog.EventNodeIds.Count));
            Assert.That(RogueliteMapRunValidator.Validate(handoff).IsValid, Is.True, RogueliteMapRunValidator.Validate(handoff).Summary);

            RogueliteDeveloperRunPolicy.AdvanceToFinale(handoff);
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(handoff);
            Assert.That(handoff.IsComplete, Is.True);
            Assert.That(coordinator.Save(handoff), Is.True);

            RogueliteMapRun reloaded;
            Assert.That(gateway.TryLoadMapRun(out reloaded), Is.True, gateway.LastError);
            Assert.That(reloaded.IsComplete, Is.True);
            Assert.That(reloaded.FirstRunExperience.RoundSettled, Is.True);
            Assert.That(reloaded.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(reloaded.EncounterAssignments.Count, Is.EqualTo(RogueliteAcademyLayerCatalog.EncounterNodeIds.Count));
            Assert.That(RogueliteMapRunValidator.Validate(reloaded).IsValid, Is.True, RogueliteMapRunValidator.Validate(reloaded).Summary);
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
                run.SelectNode(next.Id);
                RogueliteDeveloperRunPolicy.TryResolveCurrentNode(run, null);
            }

            Assert.That(run.CanChallengeAcademyFinale, Is.True);
            Assert.That(run.CompletedAcademyNodeCount, Is.GreaterThanOrEqualTo(AcademyMapTuning.BossMinimumProgress));
            // 终考门槛只解锁“可以参加终考”；此时首领尚未相邻，所以还看不到出发入口。
            Assert.That(RogueliteMapVisualPresentation.AcademyStatus(run), Does.Contain("现在可以参加终考"));
            Assert.That(run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), Is.False);

            // 门槛开放后，推进策略会带着玩家回走最后一段，直到站在首领入口。
            RogueliteDeveloperAdvanceReport report = RogueliteDeveloperRunPolicy.AdvanceToFinale(run);
            Assert.That(report.ReachedFinale, Is.True, report.Summary);
            Assert.That(run.CurrentNodeId, Is.EqualTo(RogueliteAcademyLayerCatalog.FinaleNodeId));
            Assert.That(run.IsNodeAvailable(RogueliteAcademyLayerCatalog.FinaleNodeId), Is.True);
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, RogueliteMapCatalog.Node("core_finale")),
                Is.EqualTo("你就在这里"));
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

        private sealed class MemoryStore : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string defaultValue = "") => Values.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetString(string key, string value) { Values[key] = value; }
            public void DeleteKey(string key) { Values.Remove(key); }
            public void Flush() { }
        }
    }
}
