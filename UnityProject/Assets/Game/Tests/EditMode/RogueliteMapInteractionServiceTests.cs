using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteMapInteractionServiceTests
    {
        private readonly RogueliteMapInteractionService service = new RogueliteMapInteractionService();

        [Test]
        public void SelectNode_ReportsLocationAndCombatConsequence()
        {
            RogueliteMapRun run = new RogueliteMapRun(401, FireRogueliteStarterCatalog.Universal);

            RogueliteMapInteractionResult result = service.SelectNode(run, "rail_patrol");

            Assert.That(run.CurrentNodeId, Is.EqualTo("rail_patrol"));
            Assert.That(result.PreviousNodeId, Is.EqualTo("start"));
            Assert.That(result.SubjectId, Is.EqualTo("rail_patrol"));
            Assert.That(result.StartsCombat, Is.True);
            Assert.That(result.SafeRevisit, Is.False);
        }

        [Test]
        public void ChooseContent_ReportsExactResourceChangeAndCombatRequirement()
        {
            RogueliteMapRun run = new RogueliteMapRun(402, FireRogueliteStarterCatalog.Universal);
            run.SelectNode("rail_patrol");
            run.CompleteCurrentCombat();
            FireSpellDefinition spell = run.CurrentFireSpellChoices.FirstOrDefault();
            if (spell != null) run.ClaimFireSpell(spell.Id);
            else run.ClaimReward(run.CurrentRewards[0].Id);
            run.SelectNode("switchyard");

            RogueliteMapInteractionResult result = service.ChooseContent(run, "overload");

            Assert.That(result.StartsCombat, Is.True);
            Assert.That(result.ResourcesAfter.Aether, Is.EqualTo(result.ResourcesBefore.Aether));
            Assert.That(run.PendingContentCombatMissionId, Is.Not.Empty);
        }

        [Test]
        public void CalibrateAether_ReportsTwoAetherSpent()
        {
            RogueliteMapRun run = new RogueliteMapRun(403, FireRogueliteStarterCatalog.Universal);

            RogueliteMapInteractionResult result = service.CalibrateAether(run);

            Assert.That(result.ResourcesBefore.Aether - result.ResourcesAfter.Aether, Is.EqualTo(2));
            Assert.That(run.IsAetherCalibrated, Is.True);
        }

        [Test]
        public void TryEquipNextFireSpell_PreservesExistingFallbackCycleSemantics()
        {
            RogueliteMapRun run = new RogueliteMapRun(404, FireRogueliteStarterCatalog.Universal);
            string[] before = run.EquippedFireSpellIds.ToArray();

            bool changed = service.TryEquipNextFireSpell(run, 0);

            Assert.That(changed, Is.True);
            Assert.That(run.EquippedFireSpellIds, Is.EqualTo(before));
        }
    }
}
