using System.Linq;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    public sealed class CombatFeedbackEventTests
    {
        [Test]
        public void Catalog_CoversEveryFeedbackKind_WithUniqueStableKeysAndReadableLabels()
        {
            CombatFeedbackKind[] kinds = (CombatFeedbackKind[])System.Enum.GetValues(typeof(CombatFeedbackKind));
            CombatFeedbackSemantic[] semantics = kinds.Select(CombatFeedbackCatalog.For).ToArray();

            Assert.That(semantics.Select(item => item.Key).Distinct().Count(), Is.EqualTo(kinds.Length));
            Assert.That(semantics.All(item => !string.IsNullOrWhiteSpace(item.ShortLabel)), Is.True);
            Assert.That(semantics.All(item => !string.IsNullOrWhiteSpace(item.HudLabel)), Is.True);
            Assert.That(semantics.All(item => item.ColorHex.StartsWith("#") && item.ColorHex.Length == 7), Is.True);
            string[] approvedIcons = { "move", "attack", "skill", "skill_two", "loot", "interact" };
            Assert.That(semantics.All(item => approvedIcons.Contains(item.IconKey)), Is.True);
            Assert.That(semantics.Select(item => item.IconKey + "|" + item.ColorHex + "|" + item.ShortLabel).Distinct().Count(), Is.EqualTo(kinds.Length));
        }

        [Test]
        public void ValidationSkills_ResolveToApprovedReusablePresentationSemantics()
        {
            string[] approvedIcons = { "move", "attack", "skill", "skill_two", "loot", "interact" };
            Assert.That(RogueliteSkillCatalog.All.Count, Is.EqualTo(27));
            Assert.That(RogueliteSkillCatalog.All.All(skill => approvedIcons.Contains(CombatFeedbackCatalog.For(skill.PresentationKind).IconKey)), Is.True);
        }

        [TestCase(StatusType.Burning, CombatFeedbackKind.Burning, "燃烧 2 // 持续燃烧")]
        [TestCase(StatusType.Bound, CombatFeedbackKind.Bound, "束缚 2 // 无法移动")]
        [TestCase(StatusType.Slow, CombatFeedbackKind.Slow, "迟缓 2 // 速度降低")]
        [TestCase(StatusType.ArmorBreak, CombatFeedbackKind.ArmorBreak, "破甲 2 // 护甲削弱")]
        public void Statuses_MapToOneSemantic(StatusType status, CombatFeedbackKind expectedKind, string expectedHudText)
        {
            Assert.That(CombatFeedbackCatalog.ForStatus(status), Is.EqualTo(expectedKind));
            Assert.That(CombatFeedbackCatalog.StatusHudText(status, 2), Is.EqualTo(expectedHudText));
        }

        [Test]
        public void FeedbackEvent_FormatsNumericMeaningDeterministically()
        {
            GridPosition target = new GridPosition(3, 4);
            Assert.That(new CombatFeedbackEvent(CombatFeedbackKind.Damage, target, 5).FloatingText, Is.EqualTo("-5 伤害"));
            Assert.That(new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, target, 3).FloatingText, Is.EqualTo("护盾吸收 -3"));
            Assert.That(new CombatFeedbackEvent(CombatFeedbackKind.Healing, target, 4).FloatingText, Is.EqualTo("修复 +4"));
            Assert.That(new CombatFeedbackEvent(CombatFeedbackKind.Burning, target, duration: 2).FloatingText, Is.EqualTo("燃烧 2"));
        }
    }
}
