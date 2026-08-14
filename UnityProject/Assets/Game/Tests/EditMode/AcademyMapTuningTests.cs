using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class AcademyMapTuningTests
    {
        private static readonly string[] VerifiedRoute =
        {
            "supply_checkpoint", "field_workshop", "permit_archive", "sparring_ring", "supply_depot", "wilds_camp",
            "observatory_path", "core_vault", "safety_room", "sealed_market", "gatehouse", "aether_refinery"
        };

        [Test]
        public void AcademyFinale_RequiresBothPublishedThresholdsAndExplainsTheGap()
        {
            RogueliteMapRun run = new RogueliteMapRun(1400);
            ProcessRoute(run, VerifiedRoute.Take(8));

            Assert.That(run.AcademyProgress, Is.EqualTo(8));
            Assert.That(run.CorePermits, Is.GreaterThanOrEqualTo(2));
            Assert.That(run.IsNodeAvailable("core_finale"), Is.False);
            Assert.That(run.VisualStateFor("core_finale"), Is.EqualTo(RogueliteMapNodeVisualState.Locked));
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, RogueliteMapCatalog.Node("core_finale")),
                Is.EqualTo("首领门槛：时序 8/12，核心许可 2/2"));

            ProcessRoute(run, VerifiedRoute.Skip(8));
            run.SelectNode("core_vault");

            Assert.That(run.AcademyProgress, Is.EqualTo(AcademyMapTuning.BossMinimumProgress));
            Assert.That(run.CanChallengeAcademyFinale, Is.True);
            Assert.That(run.IsNodeAvailable("core_finale"), Is.True);
            Assert.That(RogueliteMapVisualPresentation.AcademyStatus(run), Does.Contain("首领可挑战"));
        }

        [Test]
        public void AcademyFinale_RemainsLockedAtEnoughProgressWithOnlyOneCorePermit()
        {
            RogueliteMapRun run = new RogueliteMapRun(1401);
            ProcessRoute(run, new[]
            {
                "supply_checkpoint", "field_workshop", "permit_archive", "med_bay", "relay_event", "switchyard",
                "signal_hub", "elite_foundry", "transmission_tower", "aether_refinery", "gatehouse", "sealed_market"
            });
            run.SelectNode("aether_refinery");
            run.SelectNode("core_vault");

            Assert.That(run.AcademyProgress, Is.GreaterThanOrEqualTo(AcademyMapTuning.BossMinimumProgress));
            Assert.That(run.CorePermits, Is.EqualTo(1));
            Assert.That(run.IsNodeAvailable("core_finale"), Is.False);
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, RogueliteMapCatalog.Node("core_finale")), Does.EndWith("核心许可 1/2"));
        }

        [Test]
        public void AcademyMap_HasThreeDistinctPublishedBossRoutesWithinExpectedProgress()
        {
            string[][] routes =
            {
                new[] { "start", "supply_checkpoint", "field_workshop", "permit_archive", "sparring_ring", "supply_depot", "wilds_camp", "observatory_path", "core_vault", "safety_room", "sealed_market", "gatehouse", "aether_refinery", "core_vault", "core_finale" },
                new[] { "start", "supply_checkpoint", "field_workshop", "permit_archive", "med_bay", "relay_event", "gatehouse", "sealed_market", "safety_room", "aether_refinery", "core_vault", "observatory_path", "tower_foyer", "core_finale" },
                new[] { "start", "tutorial_hall", "dorm_watch", "market_lane", "field_infirmary", "study_vault", "sparring_ring", "supply_depot", "wilds_camp", "observatory_path", "wilds_camp", "tower_records", "tower_lift", "tower_foyer", "core_finale" }
            };

            foreach (string[] route in routes)
            {
                AssertConnected(route);
                Assert.That(route.Distinct(StringComparer.Ordinal).Count() - 1, Is.LessThanOrEqualTo(AcademyMapTuning.ExpectedBossProgress));
                Assert.That(route.Take(route.Length - 1).Select(RogueliteMapCatalog.Node).Count(node => node.GrantedAccessCards > 0), Is.GreaterThanOrEqualTo(2));
                Assert.That(route.Take(route.Length - 1).Distinct(StringComparer.Ordinal).Count() - 1, Is.GreaterThanOrEqualTo(AcademyMapTuning.BossMinimumProgress));
            }
        }

        [Test]
        public void OneHundredFixedSeeds_ReachTheFinaleWithTheSamePublicGateContract()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                RogueliteMapRun run = new RogueliteMapRun(seed);
                ProcessRoute(run, VerifiedRoute);
                run.SelectNode("core_vault");

                Assert.That(run.AcademyProgress, Is.EqualTo(AcademyMapTuning.BossMinimumProgress), "seed " + seed);
                Assert.That(run.CorePermits, Is.GreaterThanOrEqualTo(AcademyMapTuning.CorePermitRequirement), "seed " + seed);
                Assert.That(run.IsNodeAvailable("core_finale"), Is.True, "seed " + seed);
                Assert.That(RogueliteMapRun.FromJson(run.ToJson()).IsNodeAvailable("core_finale"), Is.True, "round trip seed " + seed);
            }
        }

        private static void ProcessRoute(RogueliteMapRun run, IEnumerable<string> nodeIds)
        {
            foreach (string nodeId in nodeIds)
            {
                run.SelectNode(nodeId);
                RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
                if (run.CompletedNodes.Contains(nodeId)) continue;
                if (node.IsCombat) run.CompleteCurrentCombat();
                else
                {
                    RogueliteNodeContentChoice choice = run.CurrentContentChoices.First(candidate => candidate.PartsCost <= run.Parts && candidate.AetherCost <= run.Aether);
                    run.ChooseCurrentNodeContent(choice.Id);
                    if (run.HasPendingContentCombat) run.CompletePendingContentCombat();
                }
                SettleReward(run);
            }
        }

        private static void SettleReward(RogueliteMapRun run)
        {
            if (!run.AwaitingReward) return;
            FireSpellDefinition spell = run.CurrentFireSpellChoices.FirstOrDefault();
            if (spell != null) run.ClaimFireSpell(spell.Id);
            else run.ClaimReward(run.CurrentRewards.First().Id);
        }

        private static void AssertConnected(IReadOnlyList<string> route)
        {
            for (int i = 1; i < route.Count; i++)
            {
                RogueliteMapNode from = RogueliteMapCatalog.Node(route[i - 1]);
                RogueliteMapNode to = RogueliteMapCatalog.Node(route[i]);
                Assert.That(from.NextIds.Contains(to.Id) || to.NextIds.Contains(from.Id), Is.True,
                    route[i - 1] + " must connect to " + route[i]);
            }
        }
    }
}
