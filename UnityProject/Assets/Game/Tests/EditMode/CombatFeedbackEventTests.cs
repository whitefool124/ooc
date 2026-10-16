using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

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

        [TestCase(StatusType.Burning, CombatFeedbackKind.Burning, "燃烧 2　持续燃烧")]
        [TestCase(StatusType.Bound, CombatFeedbackKind.Bound, "束缚 2　无法移动")]
        [TestCase(StatusType.BreakStance, CombatFeedbackKind.BreakStance, "破势 2　清盾禁盾")]
        public void Statuses_MapToOneSemantic(StatusType status, CombatFeedbackKind expectedKind, string expectedHudText)
        {
            Assert.That(CombatFeedbackCatalog.ForStatus(status), Is.EqualTo(expectedKind));
            Assert.That(CombatFeedbackCatalog.StatusHudText(status, 2), Is.EqualTo(expectedHudText));
        }

        [Test]
        public void NumericStatusFeedback_UsesItsNameAndSignedStrength()
        {
            Assert.That(CombatFeedbackCatalog.ForStatus(StatusType.Agility), Is.EqualTo(CombatFeedbackKind.Attribute));
            Assert.That(CombatFeedbackCatalog.StatusHudText(StatusType.Agility, 2, -1),
                Is.EqualTo("敏捷-1　剩余 2 回合"));
        }

        [Test]
        public void FeedbackEvent_FormatsNumericMeaningDeterministically()
        {
            GridPosition target = new GridPosition(3, 4);
            Assert.That(new CombatFeedbackEvent(CombatFeedbackKind.Damage, target, 5).FloatingText, Is.EqualTo("-5"));
            Assert.That(new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, target, 3).FloatingText, Is.EqualTo("盾 -3"));
            Assert.That(new CombatFeedbackEvent(CombatFeedbackKind.Healing, target, 4).FloatingText, Is.EqualTo("+4"));
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
        public void DamagePopup_KeepsShieldAndHealthReadableAsSeparateActualNumbers()
        {
            GridPosition target = new GridPosition(3, 4);
            CombatDamagePopupPresentation shield = CombatDamagePopupPresentation.From(
                new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, target, 3));
            CombatDamagePopupPresentation health = CombatDamagePopupPresentation.From(
                new CombatFeedbackEvent(CombatFeedbackKind.Damage, target, 5));

            Assert.That(shield.Text, Is.EqualTo("盾 -3"));
            Assert.That(health.Text, Is.EqualTo("-5"));
            Assert.That(() => shield.Merge(health), Throws.ArgumentException);
        }

        [Test]
        public void DamagePopup_RejectsNonDamageFeedback()
        {
            Assert.That(() => CombatDamagePopupPresentation.From(
                new CombatFeedbackEvent(CombatFeedbackKind.Healing, new GridPosition(0, 0), 3)),
                Throws.ArgumentException);
        }

        [Test]
        public void MixedDamage_PresentsTwoCompleteNonInteractiveLabelsAndReservesTheirWidths()
        {
            var root = new GameObject("damage-layout-test", typeof(RectTransform), typeof(Canvas), typeof(CombatVisualFeedback));
            try
            {
                var feedback = root.GetComponent<CombatVisualFeedback>();
                typeof(CombatVisualFeedback).GetField("canvas", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(feedback, root.GetComponent<Canvas>());
                var show = typeof(CombatVisualFeedback).GetMethod("ShowDamagePopup", BindingFlags.Instance | BindingFlags.NonPublic);
                var cell = new GridPosition(2, 4);
                show.Invoke(feedback, new object[] { new CombatFeedbackEvent(CombatFeedbackKind.ShieldAbsorb, cell, 4444) });
                show.Invoke(feedback, new object[] { new CombatFeedbackEvent(CombatFeedbackKind.Damage, cell, 4444) });
                var labels = root.GetComponentsInChildren<Text>().Where(t => t.name == "实际伤害跳字").ToArray();
                Assert.That(labels.Select(t => t.text), Is.EquivalentTo(new[] { "盾 -4444", "-4444" }));
                foreach (Text label in labels)
                {
                    Assert.That(label.rectTransform.sizeDelta.x, Is.GreaterThanOrEqualTo(label.preferredWidth));
                    Assert.That(label.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow));
                    Assert.That(label.resizeTextForBestFit, Is.False);
                    Assert.That(label.raycastTarget, Is.False);
                    Assert.That(label.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                    Assert.That(label.transform.localScale, Is.EqualTo(Vector3.one));
                }
                feedback.ResetBattleFeedback();
                Assert.That(root.GetComponentsInChildren<Text>().Count(t => t.name == "实际伤害跳字"), Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
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
            var previous = new Dictionary<StatusType, int> { [StatusType.Burning] = 2, [StatusType.Agility] = 1 };
            var decayed = new Dictionary<StatusType, int> { [StatusType.Burning] = 1, [StatusType.Agility] = 1 };
            var removed = new Dictionary<StatusType, int> { [StatusType.Burning] = 1 };

            Assert.That(CombatStatusFeedback.HasRemoval(previous, decayed), Is.False);
            Assert.That(CombatStatusFeedback.HasRemoval(previous, removed), Is.True);
        }

        [TestCase(240f, 48f, 28f)]
        [TestCase(168f, 104f, 52f)]
        [TestCase(240f, 48f, 0f)]
        [TestCase(168f, 104f, 0f)]
        public void FeedbackText_StaysInsideViewportThroughoutItsRise(float width, float height, float rise)
        {
            BattlefieldRect viewport = new BattlefieldPresentationAdapter().ViewportRect;
            foreach (float x in new[] { -2000f, 0f, 2000f })
            foreach (float y in new[] { -2000f, 0f, 2000f })
            {
                Vector2 center = CombatVisualFeedback.ClampFeedbackCenter(new Vector2(x, y),
                    new Vector2(width, height), viewport, rise);
                float left = 960f + center.x - width * .5f;
                float right = left + width;
                float topAtEnd = 540f - center.y - height * .5f - rise;
                float bottomAtStart = 540f - center.y + height * .5f;
                Assert.That(left, Is.GreaterThanOrEqualTo(viewport.X + 8f));
                Assert.That(right, Is.LessThanOrEqualTo(viewport.XMax - 8f));
                Assert.That(topAtEnd, Is.GreaterThanOrEqualTo(viewport.Y + 8f));
                Assert.That(bottomAtStart, Is.LessThanOrEqualTo(viewport.YMax - 8f));
                foreach (float scale in new[] { 1f, .5f })
                foreach (float edge in new[] { left, right, topAtEnd, bottomAtStart })
                    Assert.That(edge * scale % 1f, Is.Zero, "feedback must meet physical pixels at both resolutions");
            }
        }

        [TestCase(64f)]
        [TestCase(128f)]
        public void FeedbackPulse_UsesFourPixelAlignedEdgesInsideTheCurrentCell(float cellSize)
        {
            GameObject root = new GameObject("pulse-test", typeof(RectTransform));
            try
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                typeof(CombatVisualFeedback).GetMethod("BuildPulseFrame", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { rect, cellSize, Color.white });
                Assert.That(rect.localScale, Is.EqualTo(Vector3.one));
                Assert.That(root.transform.childCount, Is.EqualTo(4));
                foreach (RectTransform edge in root.transform)
                {
                    Vector2 min = edge.anchoredPosition - edge.sizeDelta * .5f;
                    Vector2 max = edge.anchoredPosition + edge.sizeDelta * .5f;
                    Assert.That(min.x, Is.GreaterThanOrEqualTo(-cellSize * .5f));
                    Assert.That(min.y, Is.GreaterThanOrEqualTo(-cellSize * .5f));
                    Assert.That(max.x, Is.LessThanOrEqualTo(cellSize * .5f));
                    Assert.That(max.y, Is.LessThanOrEqualTo(cellSize * .5f));
                    foreach (float position in new[] { min.x, min.y, max.x, max.y })
                        Assert.That(position * .5f % 1f, Is.Zero);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OutcomeFeedback_DoesNotDuplicateTheSettlementNotification(bool victory)
        {
            GameObject root = new GameObject("outcome-feedback-test", typeof(Canvas), typeof(CombatVisualFeedback));
            try
            {
                CombatVisualFeedback feedback = root.GetComponent<CombatVisualFeedback>();
                feedback.PlayOutcome(victory);
                feedback.PlayOutcome(victory);
                // Settlement owns victory/defeat. A.11 permits only the turn banner at the top.
                Assert.That(root.transform.childCount, Is.Zero);
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                    .Any(label => label.name == "战斗结果反馈"), Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
