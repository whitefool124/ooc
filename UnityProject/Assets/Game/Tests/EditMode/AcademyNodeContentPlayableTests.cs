using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class AcademyNodeContentPlayableTests
    {
        private sealed class Store : IRogueliteSaveStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string fallback = "") => Values.TryGetValue(key, out string value) ? value : fallback;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }

        [Test]
        public void AcademyEventPool_HasSixteenStablePlayableDefinitions()
        {
            Assert.That(AcademyNodeContentCatalog.Events.Count, Is.EqualTo(16));
            Assert.That(AcademyNodeContentCatalog.Events.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(16));
            foreach (AcademyEventDefinition definition in AcademyNodeContentCatalog.Events)
            {
                Assert.That(definition.Id, Does.Match("^EV(0[1-9]|1[0-6])$"));
                Assert.That(definition.DisplayName, Is.Not.Empty);
                Assert.That(definition.Choices.Count, Is.EqualTo(2), definition.Id);
                Assert.That(definition.Choices.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2), definition.Id);
                Assert.That(definition.Choices.All(value => value.Preview.Contains("用时 1") || value.RequiresCombat), Is.True, definition.Id);
                Assert.That(definition.Choices.Where(value => value.RequiresCombat).All(value => value.Preview.Contains("输")), Is.True, definition.Id);
            }
        }

        [Test]
        public void AcademyLayer_KeepsPublishedFortyNodeContentMix()
        {
            Assert.That(RogueliteMapCatalog.Nodes.Count, Is.EqualTo(40));
            Assert.That(RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Combat), Is.EqualTo(18));
            Assert.That(RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Elite), Is.EqualTo(6));
            Assert.That(RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Event), Is.EqualTo(10));
            Assert.That(RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Shop || value.Type == RogueliteMapNodeType.Workshop || value.Type == RogueliteMapNodeType.Medical), Is.EqualTo(4));
            Assert.That(RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Rest || value.Type == RogueliteMapNodeType.Treasure), Is.Zero);
            Assert.That(RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Start), Is.EqualTo(1));
            Assert.That(RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Finale), Is.EqualTo(1));
        }

        [Test]
        public void FixedSeed_AssignsEveryEventSlotWithoutDuplicatesAndRoundTrips()
        {
            int eventSlots = RogueliteMapCatalog.Nodes.Count(value => value.Type == RogueliteMapNodeType.Event && value.Id != "core_vault" && value.Id != "tower_lift");
            for (int seed = 0; seed < 64; seed++)
            {
                RogueliteMapRun run = new RogueliteMapRun(seed);
                Assert.That(run.NodeContentAssignments.Count, Is.EqualTo(eventSlots), "seed " + seed);
                Assert.That(run.NodeContentAssignments.Values.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(eventSlots), "seed " + seed);
                Assert.That(run.NodeContentAssignments.Keys.All(id => RogueliteMapCatalog.Node(id).Type == RogueliteMapNodeType.Event), Is.True);

                Store store = new Store(); RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
                Assert.That(gateway.SaveMapRun(run), Is.True, gateway.LastError);
                Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun restored), Is.True, gateway.LastError);
                Assert.That(restored.NodeContentAssignments.OrderBy(value => value.Key),
                    Is.EqualTo(run.NodeContentAssignments.OrderBy(value => value.Key)), "seed " + seed);
            }
        }

        [Test]
        public void NodeChoiceSummary_ShowsIrreversibleCostAndOutcomeWithoutHover()
        {
            RogueliteMapRun run = CreateRogue11AtSwitchyard(240824);
            RogueliteNodeContentChoice shop = AcademyNodeContentCatalog.FunctionChoices(RogueliteMapCatalog.Node("supply_checkpoint"))
                .Single(value => value.Id == "buy_hazard_condenser");
            string summary = RogueliteEconomyPresentation.NodeChoiceSummary(run, shop, RogueliteEconomyPresentation.ForNodeChoice(run, shop));
            Assert.That(summary, Does.Contain("5金"));
            Assert.That(summary, Does.Contain("险地冷凝器"));

            RogueliteNodeContentChoice combat = AcademyNodeContentCatalog.Event("EV16").Choices.Single(value => value.RequiresCombat);
            string combatSummary = RogueliteEconomyPresentation.NodeChoiceSummary(run, combat, RogueliteEconomyPresentation.ForNodeChoice(run, combat));
            Assert.That(combatSummary, Does.Contain("胜利"));
            Assert.That(combatSummary, Does.Contain("3金 + 2学院贡献"));

            RogueliteNodeContentChoice mixedVersion = AcademyNodeContentCatalog.FunctionChoices(RogueliteMapCatalog.Node("supply_checkpoint"))
                .Single(value => value.Id == "medical_cache");
            string currentEconomySummary = RogueliteEconomyPresentation.NodeChoiceSummary(run, mixedVersion,
                RogueliteEconomyPresentation.ForNodeChoice(run, mixedVersion));
            Assert.That(currentEconomySummary, Does.Contain("4金"));
            Assert.That(currentEconomySummary, Does.Not.Contain("零件"), "Rogue11 must not display legacy-only costs that it will not deduct.");
        }

        [Test]
        public void EveryEvent_HasAZeroCurrencyFallbackSoArrivalNeverBecomesAnEmptyNode()
        {
            foreach (AcademyEventDefinition definition in AcademyNodeContentCatalog.Events)
                Assert.That(definition.Choices.Any(value => value.GoldCost == 0 && value.ContributionCost == 0), Is.True, definition.Id);
        }

        [Test]
        public void EventChoice_ConsumesRogue11CurrencyAndCannotBeRepeated()
        {
            RogueliteMapRun run = CreateRogue11AtSwitchyard(FindSeedForSwitchyard("EV04"));
            int goldBefore = run.Gold;
            run.ChooseCurrentNodeContent("EV04_calibrate");

            Assert.That(run.Gold, Is.EqualTo(goldBefore + 1), "the fallback is free and still receives the event base settlement");
            Assert.That(run.CurrentMana, Is.EqualTo(12));
            Assert.That(run.CompletedNodes, Does.Contain("switchyard"));
            Assert.Throws<InvalidOperationException>(() => run.ChooseCurrentNodeContent("EV04_calibrate"));
        }

        [Test]
        public void EventReward_UsesPendingSettlementQueueUntilClaimed()
        {
            RogueliteMapRun run = CreateRogue11AtSwitchyard(FindSeedForSwitchyard("EV04"));
            int contributionBefore = run.StageContribution;

            run.ChooseCurrentNodeContent("EV04_medical");

            Assert.That(run.AwaitingReward, Is.True);
            Assert.That(run.CurrentRewards.Select(value => value.Id), Is.EqualTo(new[] { "G-T06" }));
            Assert.That(run.RogueRunState.PendingRewardIds, Does.Contain("G-T06"));
            Assert.That(run.RogueRunState.StageContribution, Is.EqualTo(contributionBefore - 1));
            Assert.That(run.ClaimedRewards, Does.Not.Contain("G-T06"));

            run.ClaimReward("G-T06");

            Assert.That(run.RogueRunState.PendingRewardIds, Is.Empty);
            Assert.That(run.ClaimedRewards, Does.Contain("G-T06"));
            Assert.That(run.RogueRunState.TacticalItemInstances.Any(value => value.DefinitionId == "G-T06"), Is.True);
        }

        [Test]
        public void SurvivedEventCombatFailure_ClosesNodeAndGrantsOnlyHalfBaseCurrency()
        {
            RogueliteMapRun run = CreateRogue11AtSwitchyard(FindSeedForSwitchyard("EV03"));
            int goldBefore = run.Gold, contributionBefore = run.StageContribution;
            run.ChooseCurrentNodeContent("EV03_fight");
            Assert.That(run.HasPendingContentCombat, Is.True);

            run.FailCurrentCombatSurvived();

            Assert.That(run.CompletedNodes, Does.Contain("switchyard"));
            Assert.That(run.HasPendingContentCombat, Is.False);
            Assert.That(run.Gold, Is.EqualTo(goldBefore + 1));
            Assert.That(run.StageContribution, Is.EqualTo(contributionBefore + 1));
            Assert.That(run.AwaitingReward, Is.False);
            Assert.Throws<InvalidOperationException>(() => run.ChooseCurrentNodeContent("EV03_fight"));
        }

        [Test]
        public void EventCombatVictory_SettlesBaseCurrencyOnlyAfterVictory()
        {
            RogueliteMapRun run = CreateRogue11AtSwitchyard(FindSeedForSwitchyard("EV03"));
            int goldBefore = run.Gold, contributionBefore = run.StageContribution;
            run.ChooseCurrentNodeContent("EV03_fight");
            Assert.That(run.Gold, Is.EqualTo(goldBefore));
            Assert.That(run.StageContribution, Is.EqualTo(contributionBefore));

            run.CompletePendingContentCombat();

            Assert.That(run.Gold, Is.EqualTo(goldBefore + 3));
            Assert.That(run.StageContribution, Is.EqualTo(contributionBefore + 2));
            Assert.That(run.HasPendingContentCombat, Is.False);
            Assert.Throws<InvalidOperationException>(() => run.CompletePendingContentCombat());
        }


        [Test]
        public void ServiceAndFixedEventNodes_AreZeroTimeOneShotChoicesWithoutFreeBaseIncome()
        {
            Store store = new Store(); RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapRun run = new RogueliteMapRun(2408, FireRogueliteStarterCatalog.Universal);
            Assert.That(gateway.SaveMapRun(run), Is.True, gateway.LastError);
            int startTime = run.StageTime;

            run.SelectNode("supply_checkpoint");
            int goldBeforeShop = run.Gold, contributionBeforeShop = run.StageContribution;
            run.ChooseCurrentNodeContent("medical_cache");
            Assert.That(run.Gold, Is.EqualTo(goldBeforeShop - 4));
            Assert.That(run.StageContribution, Is.EqualTo(contributionBeforeShop));
            Assert.That(run.StageTime, Is.EqualTo(startTime));
            Assert.Throws<InvalidOperationException>(() => run.ChooseCurrentNodeContent("medical_cache"));

            run.SelectNode("field_workshop");
            int goldBeforeWorkshop = run.Gold, contributionBeforeWorkshop = run.StageContribution;
            run.ChooseCurrentNodeContent("supply_strip");
            Assert.That(run.Gold, Is.EqualTo(goldBeforeWorkshop));
            Assert.That(run.StageContribution, Is.EqualTo(contributionBeforeWorkshop + 1));
            Assert.That(run.StageTime, Is.EqualTo(startTime));

            run.SelectNode("med_bay");
            RogueliteNodeContentChoice restChoice = run.CurrentContentChoices.First(value => value.GoldCost <= run.Gold && value.ContributionCost <= run.StageContribution);
            run.ChooseCurrentNodeContent(restChoice.Id);
            Assert.That(run.StageTime, Is.EqualTo(startTime), "Medical service must be zero-time.");

            run.SelectNode("records_archive");
            RogueliteNodeContentChoice eventChoice = run.CurrentContentChoices.First(value => value.GoldCost <= run.Gold && value.ContributionCost <= run.StageContribution && run.CurrentHealth + value.HealthGain > 0);
            run.ChooseCurrentNodeContent(eventChoice.Id);
            if (run.HasPendingContentCombat) run.CompletePendingContentCombat();
            run.SelectNode("safety_room");
            RogueliteNodeContentChoice secondEventChoice = run.CurrentContentChoices.First(value => value.GoldCost <= run.Gold && value.ContributionCost <= run.StageContribution && run.CurrentHealth + value.HealthGain > 0);
            run.ChooseCurrentNodeContent(secondEventChoice.Id);
            if (run.HasPendingContentCombat) run.CompletePendingContentCombat();
            int timeAfterEvents = run.StageTime;
            run.SelectNode("core_vault");
            run.ChooseCurrentNodeContent("vault_fire_cache");
            run.ClaimReward("G-T19");
            Assert.That(run.ClaimedRewards, Does.Contain("G-T19"));
            Assert.That(run.StageTime, Is.EqualTo(timeAfterEvents + 1), "Reclassified fixed content follows the event-node time rule.");

            RogueliteMapNode towerEvent = RogueliteMapCatalog.Node("tower_lift");
            Assert.That(AcademyNodeContentCatalog.FunctionChoices(towerEvent).Single().RewardId, Is.EqualTo("G-T19"));
        }

        private static RogueliteMapRun CreateRogue11AtSwitchyard(int seed)
        {
            Store store = new Store(); RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            RogueliteMapRun run = new RogueliteMapRun(seed, FireRogueliteStarterCatalog.Universal);
            Assert.That(gateway.SaveMapRun(run), Is.True, gateway.LastError);
            run.SelectNode("rail_patrol"); run.CompleteCurrentCombat(); run.ClaimReward(run.CurrentRewards.First().Id);
            run.SelectNode("switchyard");
            return run;
        }

        private static int FindSeedForSwitchyard(string eventId)
        {
            for (int seed = 0; seed < 10000; seed++)
                if (new RogueliteMapRun(seed).NodeContentAssignments.TryGetValue("switchyard", out string found) && found == eventId)
                    return seed;
            throw new AssertionException("No deterministic seed assigned " + eventId + " to switchyard.");
        }
    }
}
