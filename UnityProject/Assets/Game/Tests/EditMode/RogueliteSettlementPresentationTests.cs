using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class RogueliteSettlementPresentationTests
    {
        private sealed class Host : ISettlementPresentationHost
        {
            public RogueliteMapRun CurrentMapRun { get; set; }
            public RogueliteUiPreferences UiPreferences { get; } = new RogueliteUiPreferences().Configure(1f, 0f, false, true, false, false, true);
            public UiPresentationVersions UiPresentationVersions { get; } = new UiPresentationVersions();
            public UiActionFeedback LastFeedback { get; private set; }
            public int LegacyClaims { get; private set; }
            public int RewardClaims { get; private set; }

            public void ClaimMapFireSpell(string spellId)
            {
                LegacyClaims++; CurrentMapRun.ClaimFireSpell(spellId); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }

            public void ClaimMapReward(string rewardId)
            {
                RewardClaims++; CurrentMapRun.ClaimReward(rewardId); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }

            public void PublishUiVisual(UiVisualEvent visualEvent) { }
            public void ShowUiFeedback(UiActionFeedback feedback) { LastFeedback = feedback; }
        }

        [Test]
        public void Rogue11FireSpellCard_UsesUnifiedRewardClaimInsteadOfLegacyFireClaim()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("settlement-route", 620);
            dto.CurrentNodeId = "rail_patrol"; dto.CompletedNodeIds.Add("rail_patrol"); dto.AwaitingReward = true;
            Host host = new Host { CurrentMapRun = RogueliteMapRun.FromRogue11(dto) };
            GameObject root = new GameObject("settlement-route-test");
            try
            {
                RogueliteSettlementPresentation presentation = root.AddComponent<RogueliteSettlementPresentation>();
                presentation.Initialize(host);
                string spellId = host.CurrentMapRun.CurrentRewards.First(reward => reward.RogueSpell != null).Id;

                InvokeClaim(presentation, spellId);

                Assert.That(host.RewardClaims, Is.EqualTo(1)); Assert.That(host.LegacyClaims, Is.Zero);
                Assert.That(dto.MasteredSpellIds, Does.Contain(spellId)); Assert.That(host.LastFeedback, Is.Null);
            }
            finally { Object.DestroyImmediate(root); DestroyCanvases(); }
        }

        [Test]
        public void ConsecutiveLegacyReselections_RebuildCardsAndRestoreInput()
        {
            string[] current = new RogueliteMapRun(7124).ToJson().Split('|');
            string[] legacy = current.Take(26).ToArray(); legacy[0] = "map7";
            legacy[20] = "F-P04,F-P12"; legacy[21] = "F-P04,F-P12";
            Host host = new Host { CurrentMapRun = RogueliteMapRun.FromJson(string.Join("|", legacy)) };
            GameObject root = new GameObject("settlement-refresh-test");
            try
            {
                RogueliteSettlementPresentation presentation = root.AddComponent<RogueliteSettlementPresentation>();
                presentation.Initialize(host);
                string firstChoice = host.CurrentMapRun.CurrentFireSpellChoices[0].Id;
                int beforeRefresh = presentation.RefreshCount;

                InvokeClaim(presentation, firstChoice);

                Assert.That(host.CurrentMapRun.PendingFireSpellReselections.Count, Is.EqualTo(1));
                Assert.That(presentation.RefreshCount, Is.GreaterThan(beforeRefresh));
                FieldInfo pending = typeof(RogueliteSettlementPresentation).GetField("claimPending", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((bool)pending.GetValue(presentation), Is.False);
                string[] cardIds = CardIds(presentation);
                Assert.That(cardIds, Is.EquivalentTo(host.CurrentMapRun.CurrentFireSpellChoices.Select(value => value.Id)));
                Assert.That(cardIds, Does.Not.Contain(firstChoice));
            }
            finally { Object.DestroyImmediate(root); DestroyCanvases(); }
        }

        [Test]
        public void SettlementLabels_WrapAndTruncateInsideTheirAssignedCards()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("settlement-wrap", 620);
            dto.CurrentNodeId = "rail_patrol"; dto.CompletedNodeIds.Add("rail_patrol"); dto.AwaitingReward = true;
            Host host = new Host { CurrentMapRun = RogueliteMapRun.FromRogue11(dto) };
            GameObject root = new GameObject("settlement-wrap-test");
            try
            {
                RogueliteSettlementPresentation presentation = root.AddComponent<RogueliteSettlementPresentation>(); presentation.Initialize(host);
                string[] settlementLabelNames = { "效果", "注意内容" };
                Text[] labels = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(text => text.transform.root.name == "肉鸽结算UI" && settlementLabelNames.Contains(text.name)).ToArray();
                Assert.That(labels, Is.Not.Empty);
                Assert.That(labels.All(text => text.horizontalOverflow == HorizontalWrapMode.Wrap), Is.True);
                Assert.That(labels.All(text => text.verticalOverflow == VerticalWrapMode.Truncate), Is.True);
            }
            finally { Object.DestroyImmediate(root); DestroyCanvases(); }
        }

        private static void InvokeClaim(RogueliteSettlementPresentation presentation, string rewardId)
        {
            MethodInfo method = typeof(RogueliteSettlementPresentation).GetMethod("TryClaim", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(presentation, new object[] { rewardId });
        }

        private static string[] CardIds(RogueliteSettlementPresentation presentation)
        {
            FieldInfo field = typeof(RogueliteSettlementPresentation).GetField("rewardCards", BindingFlags.Instance | BindingFlags.NonPublic);
            System.Collections.IEnumerable cards = (System.Collections.IEnumerable)field.GetValue(presentation);
            return cards.Cast<object>().Select(card => (string)card.GetType().GetField("RewardId").GetValue(card)).ToArray();
        }

        private static void DestroyCanvases()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.name == "肉鸽结算UI") Object.DestroyImmediate(canvas.gameObject);
        }
    }
}
