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
            public bool IsMapRunSaved => true;
            public RogueliteUiPreferences UiPreferences { get; } = new RogueliteUiPreferences().Configure(1f, 0f, false, true, false, false, true);
            public UiPresentationVersions UiPresentationVersions { get; } = new UiPresentationVersions();
            public UiActionFeedback LastFeedback { get; private set; }
            public int LegacyClaims { get; private set; }
            public int RewardClaims { get; private set; }
            public int FixedClaims { get; private set; }
            public int LeaveRequests { get; private set; }

            public void ClaimMapFireSpell(string spellId)
            {
                LegacyClaims++; CurrentMapRun.ClaimFireSpell(spellId); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }

            public void ClaimMapReward(string rewardId)
            {
                RewardClaims++; CurrentMapRun.ClaimReward(rewardId); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }
            public void ClaimMapLootChoice(string rewardId)
            {
                RewardClaims++; CurrentMapRun.ClaimLootChoice(rewardId); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }
            public void ClaimMapFixedLoot(string lootId)
            {
                FixedClaims++; CurrentMapRun.ClaimFixedLoot(lootId); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }
            public void RequestLeaveMapLoot()
            {
                LeaveRequests++; CurrentMapRun.LeaveLoot(); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }

            public void RequestAbandonMapReward()
            {
                CurrentMapRun.AbandonCurrentReward(); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }

            public void ConfirmMapResourceReceipt()
            {
                CurrentMapRun.ConfirmResourceReceipt(); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }

            public void OpenRewardInventory() { }
            public void SelectMapReward(string rewardId)
            {
                CurrentMapRun.SelectPendingReward(rewardId); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
            }
            public void OpenMapRewardChoices()
            {
                CurrentMapRun.OpenPendingRewardChoices(); UiPresentationVersions.Mark(UiPresentationArea.Settlement);
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
                string[] settlementLabelNames = { "关键效果", "名称", "预计去向" };
                Text[] labels = Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                    .Where(text => text.transform.root.name == "肉鸽结算UI" && settlementLabelNames.Contains(text.name)).ToArray();
                Assert.That(labels, Is.Not.Empty);
                Assert.That(labels.All(text => text.horizontalOverflow == HorizontalWrapMode.Wrap), Is.True);
                Assert.That(labels.All(text => text.verticalOverflow == VerticalWrapMode.Truncate), Is.True);
            }
            finally { Object.DestroyImmediate(root); DestroyCanvases(); }
        }

        [Test]
        public void RewardRows_ShowIconAndShortEffectBesideBackpack()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("settlement-layout", 621);
            dto.CurrentNodeId = "rail_patrol"; dto.CompletedNodeIds.Add("rail_patrol"); dto.AwaitingReward = true;
            Host host = new Host { CurrentMapRun = RogueliteMapRun.FromRogue11(dto) };
            GameObject root = new GameObject("settlement-layout-test");
            try
            {
                RogueliteSettlementPresentation presentation = root.AddComponent<RogueliteSettlementPresentation>();
                presentation.Initialize(host);
                RectTransform[] cards = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Where(rect => rect.name == "reward.first" || rect.name.StartsWith("reward."))
                    .Where(rect => rect.Find("关键效果") != null).ToArray();
                Assert.That(cards, Is.Not.Empty);
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "随身背包"), Is.True);
                foreach (RectTransform card in cards)
                {
                    Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(986f, 148f)));
                    Assert.That(card.GetComponentsInChildren<Image>(true)
                        .Any(image => image.name.StartsWith("战利品图标_") && image.sprite != null), Is.True);
                    Assert.That(card.Find("关键效果").GetComponent<Text>().text.Length, Is.LessThanOrEqualTo(26));
                    Assert.That(card.GetComponent<FormalHoverTooltipTrigger>(), Is.Not.Null);
                }
            }
            finally { Object.DestroyImmediate(root); DestroyCanvases(); }
        }

        [Test]
        public void RandomRewardEnvelope_OpensSavedThreeChoiceScreenWithoutClaiming()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4306);
            run.ConfirmAcademyDeparture();
            Assert.That(RogueliteDeveloperRunPolicy.AdvanceToFinale(run).ReachedFinale, Is.True);
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run, false);
            Host host = new Host { CurrentMapRun = run };
            GameObject root = new GameObject("settlement-envelope-test");
            try
            {
                RogueliteSettlementPresentation presentation = root.AddComponent<RogueliteSettlementPresentation>();
                presentation.Initialize(host);
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "战后奖励封套"), Is.True);
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                    .Any(label => label.name == "标题" && label.text == "战利品搜刮"), Is.True);
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "背包收纳槽"), Is.True);
                RectTransform lootPane = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Single(rect => rect.name == "待领取战利品");
                RectTransform backpackPane = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Single(rect => rect.name == "随身背包");
                RectTransform backpackGrid = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Single(rect => rect.name == "背包收纳槽");
                Assert.That(backpackPane.sizeDelta.x, Is.GreaterThan(lootPane.sizeDelta.x * .8f));
                Assert.That(backpackGrid.sizeDelta.x, Is.GreaterThan(650f));
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "入袋预览区"), Is.False);
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Count(rect => rect.name.StartsWith("固定战利品_")), Is.GreaterThanOrEqualTo(2));
                Assert.That(CardIds(presentation), Is.Empty);
                host.OpenMapRewardChoices();
                Assert.That(CardIds(presentation), Has.Length.EqualTo(3));
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "三选一窗口"), Is.True);
                FormalHoverTooltip detail = Object.FindObjectsByType<FormalHoverTooltip>(FindObjectsInactive.Include)
                    .Single(view => view.transform.parent.name == "肉鸽结算UI");
                Assert.That(detail.transform.GetSiblingIndex(), Is.EqualTo(detail.transform.parent.childCount - 1),
                    "The hover card must render above the reveal window.");
                RectTransform[] revealCards = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Where(rect => rect.name.StartsWith("揭晓卡牌_")).ToArray();
                Assert.That(revealCards, Has.Length.EqualTo(3));
                foreach (RectTransform reveal in revealCards)
                {
                    Assert.That(reveal.GetComponent<Image>().raycastTarget, Is.True);
                    Assert.That(reveal.GetComponent<FormalHoverTooltipTrigger>(), Is.Not.Null);
                    Assert.That(reveal.GetComponentsInChildren<RectTransform>(true)
                        .Single(rect => rect.name.StartsWith("奖励大图标_")).sizeDelta.x, Is.EqualTo(320f));
                    Assert.That(reveal.Find("关键效果"), Is.Null);
                }
                string choiceId = host.CurrentMapRun.CurrentRewards[1].Id;
                host.SelectMapReward(choiceId);
                Assert.That(host.CurrentMapRun.SelectedRewardId, Is.EqualTo(choiceId));
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "三选一窗口"), Is.False);
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                    .Any(label => label.name == "战利品名称" && label.text == host.CurrentMapRun.CurrentRewards[1].DisplayName), Is.True);
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "待入袋物品画像"), Is.False);
                Assert.That(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .Any(rect => rect.name == "预计入袋占格"), Is.True);
                Assert.That(host.RewardClaims, Is.Zero);
                InvokeClaim(presentation, choiceId);
                Assert.That(host.RewardClaims, Is.EqualTo(1));
                Assert.That(host.CurrentMapRun.PendingRewardStepId, Is.EqualTo("collected"));
            }
            finally { Object.DestroyImmediate(root); DestroyCanvases(); }
        }

        [Test]
        public void FixedLootRow_ClaimsOnClickAndLeaveIsTheOnlyBottomRightAction()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateSubsequentAcademyRun(4306);
            run.ConfirmAcademyDeparture();
            Assert.That(RogueliteDeveloperRunPolicy.AdvanceToFinale(run).ReachedFinale, Is.True);
            RogueliteDeveloperRunPolicy.ForceWinCurrentCombat(run, false);
            int goldBefore = run.Gold;
            Host host = new Host { CurrentMapRun = run };
            GameObject root = new GameObject("settlement-individual-loot-test");
            try
            {
                RogueliteSettlementPresentation presentation = root.AddComponent<RogueliteSettlementPresentation>();
                presentation.Initialize(host);
                Button goldRow = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                    .Single(button => button.name == "固定战利品_0");
                Assert.That(goldRow.targetGraphic.raycastTarget, Is.True,
                    "The visible loot row must receive pointer hits before its button can claim the reward.");
                goldRow.onClick.Invoke();
                Assert.That(host.FixedClaims, Is.EqualTo(1));
                Assert.That(run.Gold, Is.EqualTo(goldBefore + 10));
                Assert.That(Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                    .Any(button => button.name == "确认领取" || button.name == "放弃奖励"), Is.False);
                Button leave = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                    .Single(button => button.name == "离开战利品");
                leave.onClick.Invoke();
                Assert.That(host.LeaveRequests, Is.EqualTo(1));
                Assert.That(run.AwaitingReward, Is.False);
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
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                if (canvas.name == "肉鸽结算UI") Object.DestroyImmediate(canvas.gameObject);
        }
    }
}
