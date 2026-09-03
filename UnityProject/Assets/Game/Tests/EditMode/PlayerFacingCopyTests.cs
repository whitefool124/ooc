using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class PlayerFacingCopyTests
    {
        [Test]
        public void AcademyTimeCopy_StatesCostAndResultWithoutInternalTerms()
        {
            Assert.That(PlayerFacingCopy.AcademyTimeCost(0, 8), Is.EqualTo("不花时间"));
            Assert.That(PlayerFacingCopy.AcademyTimeCost(2, 10), Is.EqualTo("用时 2 · 归来后 10"));
            Assert.That(PlayerFacingCopy.AcademyTimeOutcome(true, false, false), Is.EqualTo("回来后就是终考"));
            Assert.That(PlayerFacingCopy.AcademyTimeOutcome(false, true, false), Is.EqualTo("终考已经很近"));

            string combined = PlayerFacingCopy.AcademyTimeCost(2, 10) + PlayerFacingCopy.AcademyTimeOutcome(true, false, false);
            Assert.That(combined, Does.Not.Contain("时序"));
            Assert.That(combined, Does.Not.Contain("阈值"));
            Assert.That(combined, Does.Not.Contain("推进"));
        }

        [Test]
        public void FailureAndCostCopy_TellsPlayerWhyAndWhatWasNeeded()
        {
            Assert.That(PlayerFacingCopy.ResourceShortage("行动点", 2, 1), Is.EqualTo("行动点不足：需要 2，当前 1"));
            Assert.That(PlayerFacingCopy.ActionPointCost(1), Is.EqualTo("消耗 1 行动点"));
            Assert.That(PlayerFacingCopy.ReturnToMapFree, Does.Contain("不会花掉"));
        }

        [Test]
        public void PublicArtifactCosts_UsePlayerTermInsteadOfAbbreviation()
        {
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
            {
                Assert.That(artifact.PublicCost, Does.Not.Contain("AP"), artifact.Id);
                Assert.That(artifact.PublicCost, Does.Contain("行动点"), artifact.Id);
            }
        }

        [Test]
        public void AcademyChoices_ExplainTimeWithoutPipelineLanguage()
        {
            string[] forbidden = { "时序", "阈值", "快照", "持久化", "执行适配", "R2-03", "已预览", "结算", "失败档", "变体", "不消耗资源" };
            var choices = AcademyNodeContentCatalog.Events.SelectMany(value => value.Choices)
                .Concat(RogueliteMapCatalog.Nodes.SelectMany(AcademyNodeContentCatalog.FunctionChoices));

            foreach (RogueliteNodeContentChoice choice in choices)
            {
                foreach (string term in forbidden)
                    Assert.That(choice.Preview, Does.Not.Contain(term), choice.Id + " / " + term);
            }
        }

        [Test]
        public void FormalJourneyCopy_DoesNotExposeDesignOrDeveloperVocabulary()
        {
            string[] forbidden = { "节点", "弱遭遇", "强遭遇", "精英遭遇", "固定首领", "奖励档", "失败档", "变体", "确定性", "种子", "实例", "结算", "预览" };
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(RogueRunDto.CreateNew("world-voice", 917));

            foreach (RogueliteMapNode node in RogueliteMapCatalog.Nodes)
            {
                foreach (string term in forbidden)
                    Assert.That(node.Summary, Does.Not.Contain(term), node.Id + " / " + term);
                if (!node.IsCombat) continue;
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, node);
                string visible = string.Join(" | ", preview.RiskLabel, preview.RewardLabel, preview.FailureConsequence,
                    preview.EncounterLabel, preview.EnemySummary, preview.SpatialRisk);
                foreach (string term in forbidden)
                    Assert.That(visible, Does.Not.Contain(term), node.Id + " / " + term);
            }

            foreach (RogueliteEncounterDefinition encounter in RogueliteEncounterCatalog.Packages)
            {
                string visible = string.Join(" | ", encounter.PublicRisk, encounter.RewardTier,
                    encounter.SpawnRelationship, encounter.ObjectiveSummary);
                foreach (string term in forbidden)
                    Assert.That(visible, Does.Not.Contain(term), encounter.VariantKey + " / " + term);
            }
        }

        [Test]
        public void ShieldLog_DoesNotExposeItsInternalSourceId()
        {
            ShieldSourceRecord record = new ShieldSourceRecord("AFF-SECRET:turn_start", 3, ShieldEventKind.Granted, 2);
            string visible = RogueShieldLogPresentation.Format(record);

            Assert.That(visible, Does.Not.Contain("AFF-SECRET"));
            Assert.That(visible, Is.EqualTo("获得 3 护盾 · 第2回合"));
        }
    }
}
