using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatHoverDescriptionTableTests
    {
        [Test]
        public void Table_CoversEverySpellAndArtifactWithPlayerFacingEffects()
        {
            SpellDefinition[] spells = RogueContentCatalog.CreateAcademyV01().Spells.ToArray();
            Assert.That(CombatHoverDescriptionTable.Spells.Count, Is.EqualTo(spells.Length));
            Assert.That(CombatHoverDescriptionTable.Artifacts.Count, Is.EqualTo(ArtifactCatalog.All.Count));

            foreach (SpellDefinition spell in spells)
            {
                CombatHoverDescriptionRow row = CombatHoverDescriptionTable.Spell(spell.DefinitionId);
                Assert.That(row, Is.Not.Null, spell.DefinitionId);
                Assert.That(row.Effect, Is.Not.Empty, spell.DefinitionId);
                Assert.That(row.FormatBody(), Does.Contain("效果"), spell.DefinitionId);
                Assert.That(row.FormatBody(), Does.Not.Contain("legacy_rule:"), spell.DefinitionId);
            }

            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
            {
                CombatHoverDescriptionRow row = CombatHoverDescriptionTable.Artifact(artifact.Id);
                Assert.That(row, Is.Not.Null, artifact.Id);
                Assert.That(row.Effect, Is.EqualTo(artifact.EffectSummary), artifact.Id);
                Assert.That(row.FormatBody(), Does.Contain(artifact.TargetSummary), artifact.Id);
            }
        }

        [Test]
        public void Bodies_ShowDetailedSpellAndArtifactEffects()
        {
            SpellDefinition spell = RogueContentCatalog.CreateAcademyV01().Spells
                .Single(value => value.DefinitionId == "F-P-M19");
            string spellBody = CombatHoverDescriptionTable.SpellBody(spell);
            string artifactBody = CombatHoverDescriptionTable.ArtifactBody(ArtifactCatalog.Get("F-T01"), 2, 4);

            Assert.That(spellBody, Does.Contain("造成 24 点武器伤害"));
            Assert.That(spellBody, Does.Contain("无视护盾失去 8 点生命"));
            Assert.That(artifactBody, Does.Contain(ArtifactCatalog.Get("F-T01").EffectSummary));
            Assert.That(artifactBody, Does.Contain("剩余2　总计4 次"));

            FormalTooltipContent parsed = new FormalTooltipContent("法宝", "测试法宝", artifactBody, Color.white);
            Assert.That(parsed.MetricA, Does.Contain("剩余2"));
            Assert.That(parsed.Effect, Is.EqualTo(ArtifactCatalog.Get("F-T01").EffectSummary));
            Assert.That(parsed.Summary, Is.EqualTo("学院登记的便携式器材。"));
            Assert.That(parsed.Summary.Length, Is.LessThanOrEqualTo(FormalTooltipContent.MaximumSummaryCharacters));
            Assert.That(parsed.Summary, Does.Not.Contain(ArtifactCatalog.Get("F-T01").EffectSummary));
        }
    }
}
