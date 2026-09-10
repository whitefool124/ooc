using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

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

        [TestCase(StatusType.Burning, CombatFeedbackKind.Burning, "燃烧 2 · 持续燃烧")]
        [TestCase(StatusType.Bound, CombatFeedbackKind.Bound, "束缚 2 · 无法移动")]
        [TestCase(StatusType.Slow, CombatFeedbackKind.Slow, "迟缓 2 · 速度降低")]
        [TestCase(StatusType.ArmorBreak, CombatFeedbackKind.ArmorBreak, "破甲 2 · 护甲削弱")]
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

        [Test]
        public void GridFeedbackPosition_TracksTheEnlargedBoardCellCenter()
        {
            BattlefieldPresentationAdapter adapter = new BattlefieldPresentationAdapter();
            BattlefieldRect board = adapter.BoardRect();
            GridPosition position = new GridPosition(4, 6);
            BattlefieldRect cell = adapter.CellRect(board, BattlefieldPresentationAdapter.DefaultHeight, position);

            Vector2 feedback = CombatVisualFeedback.GridFeedbackPosition(position);

            Assert.That(feedback.x, Is.EqualTo(cell.X + cell.Width * .5f - 960f));
            Assert.That(feedback.y, Is.EqualTo(540f - cell.Y - cell.Height * .5f));
        }

        [Test]
        public void FeedbackClip_ConvertsCanvasCoordinatesWithoutDoubleOffset()
        {
            Vector2 canvasPosition = new Vector2(-176f, 82f);
            Vector2 clipCenter = new Vector2(-240f, 78f);

            Vector2 local = CombatVisualFeedback.CanvasToFeedbackLocal(canvasPosition, clipCenter);

            Assert.That(local, Is.EqualTo(new Vector2(64f, 4f)));
            Assert.That(local + clipCenter, Is.EqualTo(canvasPosition));
        }

        [Test]
        public void DamagePopup_MergesShieldAndHealthIntoOneActualNumber()
        {
            GridPosition target = new GridPosition(3, 4);
            CombatDamagePopupPresentation shield = CombatDamagePopupPresentation.From(
                new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, target, 3));
            CombatDamagePopupPresentation health = CombatDamagePopupPresentation.From(
                new CombatFeedbackEvent(CombatFeedbackKind.Damage, target, 5));

            CombatDamagePopupPresentation merged = shield.Merge(health);

            Assert.That(merged.Amount, Is.EqualTo(8));
            Assert.That(merged.Text, Is.EqualTo("-8"));
            Assert.That(merged.IncludesHealthDamage, Is.True);
        }

        [Test]
        public void DamagePopup_RejectsNonDamageFeedback()
        {
            Assert.That(() => CombatDamagePopupPresentation.From(
                new CombatFeedbackEvent(CombatFeedbackKind.Healing, new GridPosition(0, 0), 3)),
                Throws.ArgumentException);
        }

        [Test]
        public void ZeroAnimationIntensity_DisablesMotionButKeepsFeedbackPolicyAvailable()
        {
            Assert.That(CombatFeedbackPresentationPolicy.AnimationsEnabled(0f), Is.False);
            Assert.That(CombatFeedbackPresentationPolicy.AnimationsEnabled(.01f), Is.False);
            Assert.That(CombatFeedbackPresentationPolicy.AnimationsEnabled(.5f), Is.True);
        }

        [Test]
        public void StatusDiff_ReportsRemovalOnceWithoutTreatingDurationDecayAsRemoval()
        {
            var previous = new Dictionary<StatusType, int> { [StatusType.Burning] = 2, [StatusType.Slow] = 1 };
            var decayed = new Dictionary<StatusType, int> { [StatusType.Burning] = 1, [StatusType.Slow] = 1 };
            var removed = new Dictionary<StatusType, int> { [StatusType.Burning] = 1 };

            Assert.That(CombatStatusFeedback.HasRemoval(previous, decayed), Is.False);
            Assert.That(CombatStatusFeedback.HasRemoval(previous, removed), Is.True);
        }
    }
}
