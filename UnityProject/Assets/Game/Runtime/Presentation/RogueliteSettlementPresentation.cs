using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    // Runtime settlement layer keeps the authored combat HUD untouched while reward UI is iterated.
    [DefaultExecutionOrder(-100)]
    public sealed class RogueliteSettlementPresentation : MonoBehaviour
    {
        private ISettlementPresentationHost bootstrap;
        private Canvas canvas;
        private FormalHoverTooltip tooltip;
        private GameObject panel;
        private int presentedSeed = int.MinValue;
        private readonly List<RewardCardInput> rewardCards = new List<RewardCardInput>();
        private bool claimPending;
        private bool choiceWindowSuppressed;
        private bool hasPresentedModel;
        private SettlementPresentationModel presentedModel;
        public int RefreshCount { get; private set; }

        private sealed class RewardCardInput
        {
            public string RewardId;
            public RogueliteReward Reward;
            public bool CanClaim;
            public RectTransform Rect;
            public Image Image;
            public Color Normal;
            public Color Hover;
            public bool IsHovering;
            public Button Button;
        }

        public void Initialize(ISettlementPresentationHost source)
        {
            bootstrap = source;
            bootstrap.UiPresentationVersions.Changed += OnPresentationChanged;
            RefreshNow();
        }

        private void OnPresentationChanged(UiPresentationChange change)
        {
            if (change.Area == UiPresentationArea.Settlement || change.Area == UiPresentationArea.Flow || change.Area == UiPresentationArea.MapStructure)
                RefreshNow();
        }

        public void RefreshNow()
        {
            RogueliteMapRun run = bootstrap == null ? null : bootstrap.CurrentMapRun;
            if (bootstrap != null && !bootstrap.IsMapRunSaved)
            {
                hasPresentedModel = false;
                Hide();
                return;
            }
            SettlementPresentationModel nextModel = SettlementPresentationModel.From(run);
            if (hasPresentedModel && presentedModel.Equals(nextModel)) return;
            presentedModel = nextModel;
            hasPresentedModel = true;
            RefreshCount++;
            if (run == null || !nextModel.Visible)
            {
                Hide();
                return;
            }

            Show(run);
        }

        private void OnGUI()
        {
            if (panel == null || Event.current == null) return;
            Vector2 screenPoint = new Vector2(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y);
            foreach (RewardCardInput card in rewardCards)
            {
                if (card.Rect == null || !card.Rect.gameObject.activeInHierarchy) continue;
                bool hovering = RectTransformUtility.RectangleContainsScreenPoint(card.Rect, screenPoint);
                SetHover(card, hovering);
                if (hovering && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (card.Rect.name.StartsWith("揭晓卡牌_", System.StringComparison.Ordinal)) TrySelect(card.RewardId);
                    else TryClaim(card.RewardId);
                    Event.current.Use();
                    return;
                }
            }
        }

        private void Show(RogueliteMapRun run)
        {
            Hide();
            EnsureCanvas();
            rewardCards.Clear();
            claimPending = false;
            presentedSeed = run.Seed;
            bootstrap.PublishUiVisual(new UiVisualEvent(UiVisualEventKind.SettlementOpened, run.Seed.ToString()));
            FormalRogueliteSettlementShellView shell = FormalRogueliteSettlementShellView.Create(FormalUiKit.ContentParent(canvas.transform));
            panel = shell.gameObject;
            Image veil = panel.GetComponent<Image>();
            FormalUiEffects.ApplyBackdrop(veil, "settlement");
            FormalUiEffects.AddPageDecorations(panel.transform, "settlement", bootstrap.UiPreferences.AnimationIntensity);

            GameObject card = shell.Card.gameObject;
            FormalUiKit.ApplySkin(card.GetComponent<Image>(), "panel_elevated", FormalUiTheme.SurfaceRaised);
            card.transform.SetAsLastSibling();
            RectTransform cardRect = shell.Card;
            AcademyResourceReceipt receipt = run.PendingResourceReceipt;
            cardRect.sizeDelta = OccPixelUiConfig.Layout("settlement.card").Size;
            RogueliteMapNode settlementNode = run.MapNodes.FirstOrDefault(value => value.Id == run.CurrentNodeId);

            bool firstEliteReward = run.IsTutorialPhase && run.FirstRunExperience.Outcome == FirstRunOutcome.EliteVictory &&
                (run.AwaitingReward || run.LootPendingExit);
            AddLabel(shell.Header, "标题", "战利品搜刮", new Vector2(54, -42), new Vector2(1550, 54), 38, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            FormalUiKit.Line(shell.Header, new Vector2(56, -164), new Vector2(1588, 2), FormalUiTheme.WithAlpha(FormalUiTheme.Muted, .72f), "分隔");

            List<RogueliteReward> choices = run.CurrentFireSpellChoices.Select(AsReward).ToList();
            choices.AddRange(run.CurrentRewards.Take(3 - choices.Count));
            bool unopenedRandomReward = (run.IsInAcademyLayer || run.IsTutorialPhase) && receipt == null && choices.Count == 3 && !run.RewardChoicesOpened;
            bool ritualReward = (run.IsInAcademyLayer || run.IsTutorialPhase) && receipt == null &&
                (choices.Count == 3 || run.LootPendingExit || run.PendingRewardStepId == "collected");
            string envelopeName = run.PendingRewardStepId == "followup" ? "额外掉落" :
                run.IsTutorialPhase ? "固定奖励" : "随机奖励";
            float lootWidth = ritualReward ? 850f : 1034f;
            float backpackX = ritualReward ? 908f : 1092f;
            float backpackWidth = ritualReward ? 752f : 568f;
            GameObject lootPane = FormalUiKit.FlatPanel("待领取战利品", shell.Rewards, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, -184), new Vector2(lootWidth, 646), ritualReward ? FormalUiTheme.Surface : FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .92f));
            GameObject backpackPane = FormalUiKit.FlatPanel("随身背包", shell.Rewards, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(backpackX, -184), new Vector2(backpackWidth, 646), FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .92f));
            AddLootIcon(lootPane.transform, "战利品标记", FormalArtRegistry.CommandPath("loot"), new Vector2(24, -17), 40);
            AddLabel(lootPane.transform, "待领取标题", receipt != null ? "结算收据" : "待领取",
                new Vector2(76, -17), new Vector2(770, 42), 27, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            int visibleLootCount = ritualReward ? firstEliteReward
                    ? (run.ClaimedRewards.Contains("ACA-EQ-HD02") ? 0 : 1) +
                        (run.ClaimedRewards.Contains("G-T13") ? 0 : 1) +
                        (run.ClaimedRewards.Contains("first-elite:gold") ? 0 : 1) +
                        (run.RogueRunState?.PendingRewardContribution > 0 ? 1 : 0) +
                        (run.RogueRunState?.PendingFixedMaterialIds.Count > 0 ? 1 : 0) +
                        (choices.Count > 0 ? 1 : 0)
                    : (run.RogueRunState?.PendingRewardGold > 0 ? 1 : 0) +
                        (run.RogueRunState?.PendingRewardContribution > 0 ? 1 : 0) +
                        (run.RogueRunState?.PendingFixedMaterialIds.Count > 0 ? 1 : 0) + (choices.Count > 0 ? 1 : 0)
                : choices.Count;
            AddLabel(lootPane.transform, "待领取数量", visibleLootCount.ToString("00"),
                new Vector2(lootWidth - 118, -17), new Vector2(90, 42), 27, FormalUiTheme.Amber, TextAnchor.MiddleRight);
            FormalUiKit.Line(lootPane.transform, new Vector2(24, -67), new Vector2(lootWidth - 48, 2), FormalUiTheme.WithAlpha(FormalUiTheme.Amber, .66f), "清点分隔");
            if (ritualReward) DrawScavengeTray(lootPane.transform, run, choices, settlementNode, unopenedRandomReward, envelopeName);
            else for (int i = 0; i < choices.Count; i++) AddLootChoiceRow(lootPane.transform, choices[i], i, run);
            if (receipt != null)
            {
                RogueliteNodeContentChoice chosen = run.CurrentContentChoices.FirstOrDefault(value => value.Id == receipt.ChoiceId);
                AddLabel(lootPane.transform, "收据选项", "已选择　" + (chosen?.DisplayName ?? receipt.ChoiceId),
                    new Vector2(26, -112), new Vector2(940, 48), 28, FormalUiTheme.Text, TextAnchor.MiddleLeft);
                AddLabel(lootPane.transform, "收据描述", chosen?.Preview ?? "事件结果已记录。",
                    new Vector2(26, -182), new Vector2(940, 120), FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.UpperLeft);
            }
            bool eventSettlement = settlementNode != null && settlementNode.Type == RogueliteMapNodeType.Event;
            string fixedText = receipt != null ? "本次变化　" + ResourceReceiptText(receipt) : firstEliteReward
                ? "固定所得　金币 +6　学院贡献 +2；固定掉落　低压回路护额、定锚支架"
                : run.IsTutorialPhase ? "固定所得　金币 +3　学院贡献 +1"
                : run.UsesRogue11 && eventSettlement ? "固定所得　事件指定奖励待领取；资源依所选事件结算"
                : run.UsesRogue11 ? "固定所得　金币 +" + (settlementNode?.Type == RogueliteMapNodeType.Finale ? 10 : settlementNode?.Type == RogueliteMapNodeType.Elite ? 6 : 3) +
                    "　学院贡献 +" + (settlementNode?.Type == RogueliteMapNodeType.Finale ? 3 : settlementNode?.Type == RogueliteMapNodeType.Elite ? 2 : 1)
                : "固定所得　结算资源已写入行程";
            if (run.RogueRunState?.PendingFixedMaterialIds.Count > 0)
                fixedText += "　待收集 " + string.Join("、", run.RogueRunState.PendingFixedMaterialIds.Select(id =>
                    id == AcademyBattleRewardCatalog.ForgeLoad ? "承力合金" :
                    id == AcademyBattleRewardCatalog.ForgeCircuit ? "导能晶片" :
                    id == AcademyBattleRewardCatalog.SpecAmplify ? "增幅刻墨" : "节流刻墨"));
            DrawBackpackPreview(backpackPane.transform, run, choices, ritualReward ? string.Empty : fixedText, unopenedRandomReward, ritualReward);
            if (choices.Count == 0 && receipt == null && !ritualReward && !run.LootPendingExit)
            {
                FormalUiEffects.AddEmptyIllustration(lootPane.transform, "empty_reward_crate", new Vector2(515, -180), 128f);
                AddLabel(lootPane.transform, "空奖励说明", run.UsesRogue11 ? "本次为资源结算，固定所得已写入行程。" : "这次没有可领取的物品。返回地图继续前进。", new Vector2(235, -336), new Vector2(560, 40),
                    FormalUiTheme.BodyFontSize, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            }

            bool needsInventory = !unopenedRandomReward && choices.Any(value => !RogueliteEconomyPresentation.ForReward(run, value).CanExecute &&
                RogueliteEconomyPresentation.ForReward(run, value).Status.Contains("行囊"));
            if (run.UsesRogue11 && receipt == null)
            {
                Button inventory = FormalUiKit.Button("整理行囊", needsInventory ? "空间不足 · 整理背包" : "整理背包", shell.Footer, new Vector2(56, -862), new Vector2(320, 64), FormalUiTheme.Interactive);
                inventory.onClick.AddListener(bootstrap.OpenRewardInventory);
                FormalUiKit.ConfigureButtonFeedback(inventory, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                    () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            }

            string progressText = receipt != null ? "已结算" : visibleLootCount > 0 ? "待领取 " + visibleLootCount + " 项" : "已搜刮完毕";
            AddLabel(shell.Footer, "收集进度", progressText,
                new Vector2(run.UsesRogue11 && receipt == null ? 400 : 56, -862), new Vector2(run.UsesRogue11 && receipt == null ? 510 : 820, 48),
                18, needsInventory ? FormalUiTheme.Amber : FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            Button leave = FormalUiKit.Button("离开战利品", "离开", shell.Footer,
                new Vector2(1310, -862), new Vector2(304, 64), FormalUiTheme.Interactive);
            leave.onClick.AddListener(bootstrap.RequestLeaveMapLoot);
            FormalUiKit.ConfigureButtonFeedback(leave, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            CanvasGroup group = card.AddComponent<CanvasGroup>();
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate)
            {
                group.alpha = 1f;
                cardRect.localScale = Vector3.one;
            }
            else
            {
                group.alpha = 0f;
                cardRect.localScale = Vector3.one * (1f - motion.ModalScaleOffset);
                DOTween.Sequence().SetUpdate(true)
                    .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.StandardDuration))
                    .Join(cardRect.DOScale(1f, motion.ToastDuration).SetEase(FormalUiMotionTokens.StandardEase));
            }

            if (ritualReward && run.RewardChoicesOpened && string.IsNullOrEmpty(run.SelectedRewardId) && !choiceWindowSuppressed)
                DrawChoiceWindow(shell.Card, choices, run);

            tooltip.transform.SetAsLastSibling();

            RewardCardInput firstAvailable = rewardCards.FirstOrDefault(item => item.Button != null && item.Button.interactable);
            if (firstAvailable != null) RuntimeUiEventSystem.Select(firstAvailable.Button.gameObject);
            else if (ritualReward && !string.IsNullOrEmpty(run.SelectedRewardId))
            {
                Transform envelope = lootPane.transform.Find("战后奖励封套");
                if (envelope != null) RuntimeUiEventSystem.Select(envelope.gameObject);
            }
            else if (ritualReward && choiceWindowSuppressed)
            {
                Transform envelope = lootPane.transform.Find("战后奖励封套");
                if (envelope != null) RuntimeUiEventSystem.Select(envelope.gameObject);
            }
            else if (receipt != null || visibleLootCount == 0) RuntimeUiEventSystem.Select(leave.gameObject);
        }

        private static string ResourceReceiptText(AcademyResourceReceipt receipt)
        {
            List<string> changes = new List<string>();
            void Add(string label, int value)
            {
                if (value != 0) changes.Add(label + " " + (value > 0 ? "+" : string.Empty) + value);
            }
            Add("金币", receipt.GoldChange);
            Add("学院贡献", receipt.ContributionChange);
            Add("生命", receipt.HealthChange);
            Add("个人魔力", receipt.ManaChange);
            Add("学院时序", receipt.TimeChange);
            return changes.Count == 0 ? "无资源变化" : string.Join("　", changes);
        }

        private static Image AddLootIcon(Transform parent, string name, string resourcePath, Vector2 position, float size)
        {
            GameObject icon = CreateObject(name, parent);
            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            Image image = icon.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(resourcePath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void DrawScavengeTray(Transform parent, RogueliteMapRun run, IReadOnlyList<RogueliteReward> choices,
            RogueliteMapNode node, bool unopened, string envelopeName)
        {
            int row = 0;
            bool firstElite = run.IsTutorialPhase && run.FirstRunExperience.Outcome == FirstRunOutcome.EliteVictory &&
                (run.AwaitingReward || run.LootPendingExit);
            if (firstElite)
            {
                if (!run.ClaimedRewards.Contains("ACA-EQ-HD02"))
                    AddScavengeEntry(parent, row++, "护额", "×1", FormalArtRegistry.EquipmentSlotPath("Head"), false,
                        () => TryClaimFixed("elite-head"), true);
                if (!run.ClaimedRewards.Contains("G-T13"))
                    AddScavengeEntry(parent, row++, "定锚支架", "×1", FormalArtRegistry.ItemPath("loot_unknown"), false,
                        () => TryClaimFixed("elite-brace"), true);
                if (!run.ClaimedRewards.Contains("first-elite:gold"))
                    AddScavengeEntry(parent, row++, "金币", "+6", FormalArtRegistry.ResourceMetricPath("gold"), false,
                        () => TryClaimFixed("elite-gold"), true);
                if (run.RogueRunState?.PendingRewardContribution > 0)
                    AddScavengeEntry(parent, row++, "学院贡献", "+2", FormalArtRegistry.ResourceMetricPath("contribution"), false,
                        () => TryClaimFixed("elite-contribution"), true);
                if (run.RogueRunState?.PendingFixedMaterialIds.Count > 0)
                    AddScavengeEntry(parent, row++, "随机强化材料", "×1", FormalArtRegistry.ResourceMetricPath("parts"), false,
                        () => TryClaimFixed("elite-material"), true);
            }
            else if (run.UsesRogue11 && node != null && node.Type != RogueliteMapNodeType.Event)
            {
                int gold = run.RogueRunState?.PendingRewardGold ?? 0;
                int contribution = run.RogueRunState?.PendingRewardContribution ?? 0;
                if (gold > 0)
                    AddScavengeEntry(parent, row++, "金币", "+" + gold, FormalArtRegistry.ResourceMetricPath("gold"), false,
                        () => TryClaimFixed("gold"));
                if (contribution > 0)
                    AddScavengeEntry(parent, row++, "学院贡献", "+" + contribution, FormalArtRegistry.ResourceMetricPath("contribution"), false,
                        () => TryClaimFixed("contribution"));
                int materials = run.RogueRunState?.PendingFixedMaterialIds.Count ?? 0;
                if (materials > 0)
                    AddScavengeEntry(parent, row++, "材料", "×" + materials, FormalArtRegistry.ResourceMetricPath("parts"), false,
                        () => TryClaimFixed("materials"));
            }
            if (choices.Count == 0) return;
            RogueliteReward chosen = choices.FirstOrDefault(value => value.Id == run.SelectedRewardId ||
                "dismantle:" + value.Id == run.SelectedRewardId);
            string rewardIcon = chosen == null ? "Art/FormalAcademyCombat32/academy_loot_chest_closed" : RewardIconPath(chosen);
            string title = chosen == null ? "一份" + envelopeName : chosen.DisplayName;
            string amount = chosen == null ? "?" : "✓";
            AddScavengeEntry(parent, row, title, amount, rewardIcon, true, () =>
            {
                if (unopened) bootstrap.OpenMapRewardChoices();
                else if (chosen != null) TryClaim(run.SelectedRewardId);
                else { choiceWindowSuppressed = false; Show(run); }
            }, firstElite);
            if (chosen != null)
            {
                GameObject rewardRow = parent.Find("战后奖励封套").gameObject;
                FormalHoverTooltipTrigger trigger = rewardRow.AddComponent<FormalHoverTooltipTrigger>();
                trigger.Configure(tooltip, () => new FormalTooltipContent("待领取", chosen.DisplayName,
                    RewardEffect(chosen), FormalUiTheme.Amber, rewardIcon));
            }
            float paneWidth = parent.GetComponent<RectTransform>().sizeDelta.x;
            AddLabel(parent, "收集提示", chosen == null ? "" : "↓", new Vector2(paneWidth - 120, -570), new Vector2(52, 44),
                28, FormalUiTheme.Amber, TextAnchor.MiddleCenter);
        }

        private void AddScavengeEntry(Transform parent, int index, string title, string amount,
            string iconPath, bool actionable, System.Action action, bool compact = false)
        {
            float height = compact ? 80f : actionable ? 145f : 112f;
            float rowWidth = parent.GetComponent<RectTransform>().sizeDelta.x - 80f;
            GameObject row = FormalUiKit.FlatPanel(actionable ? "战后奖励封套" : "固定战利品_" + index, parent,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(40f, -87f - index * (compact ? 88f : 122f)),
                new Vector2(rowWidth, height), Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Amber,
                    actionable ? .13f : .025f));
            row.GetComponent<Image>().raycastTarget = action != null;
            FormalUiKit.Line(row.transform, new Vector2(0, 0), new Vector2(rowWidth, 3),
                actionable ? FormalUiTheme.Amber : FormalUiTheme.WithAlpha(FormalUiTheme.Rule, .48f), "战利品上沿");
            FormalUiKit.Line(row.transform, new Vector2(0, -height + 3), new Vector2(rowWidth, 3),
                actionable ? FormalUiTheme.Amber : FormalUiTheme.WithAlpha(FormalUiTheme.Rule, .32f), "战利品下沿");
            AddLootIcon(row.transform, "战利品图标_" + index, iconPath,
                compact ? new Vector2(20, -8) : actionable ? new Vector2(12, -13) : new Vector2(22, -16), compact ? 64 : actionable ? 118 : 80);
            AddLabel(row.transform, "战利品名称", title, new Vector2(compact ? 110 : actionable ? 145 : 122, compact ? -7 : actionable ? -38 : -23),
                new Vector2(rowWidth - 270, 66), compact ? 28 : actionable ? 36 : 30,
                actionable ? FormalUiTheme.Amber : FormalUiTheme.Text, TextAnchor.MiddleLeft);
            AddLabel(row.transform, "战利品数量", amount, new Vector2(rowWidth - 110, compact ? -7 : actionable ? -37 : -22), new Vector2(80, 66),
                compact ? 28 : 35, actionable ? FormalUiTheme.Amber : FormalUiTheme.Text, TextAnchor.MiddleCenter);
            if (action != null)
            {
                Button button = row.AddComponent<Button>();
                button.targetGraphic = row.GetComponent<Image>();
                button.onClick.AddListener(() => action?.Invoke());
                FormalUiKit.ConfigureButtonFeedback(button,
                    FormalUiButtonPalette.ForAccent(row.GetComponent<Image>().color, FormalUiTheme.Amber),
                    () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            }
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (!motion.IsImmediate)
            {
                CanvasGroup group = row.AddComponent<CanvasGroup>();
                group.alpha = 0;
                DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.StandardDuration)
                    .SetDelay(index * motion.QuickDuration).SetUpdate(true);
                row.transform.localScale = new Vector3(.96f, .96f, 1f);
                row.transform.DOScale(1f, motion.StandardDuration).SetDelay(index * motion.QuickDuration).SetUpdate(true);
            }
        }

        private void DrawChoiceWindow(RectTransform card, IReadOnlyList<RogueliteReward> choices, RogueliteMapRun run)
        {
            GameObject window = FormalUiKit.FlatPanel("三选一窗口", card, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(18, -18), new Vector2(1664, 914), FormalUiTheme.SurfaceRaised);
            window.transform.SetAsLastSibling();
            FormalUiKit.Line(window.transform, new Vector2(25, -98), new Vector2(1614, 3), FormalUiTheme.Amber, "揭晓分隔");
            AddLootIcon(window.transform, "揭晓图标", FormalArtRegistry.CommandPath("loot"), new Vector2(46, -30), 50);
            AddLabel(window.transform, "揭晓标题", choices.Count == 1 ? "额外掉落" : "战利品搜刮", new Vector2(112, -26), new Vector2(900, 62),
                42, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            AddLabel(window.transform, "选择提示", choices.Count == 1 ? "◇" : "◇  ◇  ◇", new Vector2(1340, -38), new Vector2(270, 44),
                27, FormalUiTheme.Amber, TextAnchor.MiddleRight);
            for (int i = 0; i < choices.Count; i++) AddRevealCard(window.transform, choices[i], i, run);
            Button back = FormalUiKit.Button("返回清点", "返回", window.transform, new Vector2(674, -820),
                new Vector2(316, 64), FormalUiTheme.Interactive);
            back.onClick.AddListener(() => { choiceWindowSuppressed = true; Show(run); });
        }

        private void AddRevealCard(Transform parent, RogueliteReward reward, int index, RogueliteMapRun run)
        {
            string iconPath = RewardIconPath(reward);
            GameObject card = FormalUiKit.FlatPanel("揭晓卡牌_" + index, parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(run.CurrentRewards.Count == 1 ? 586 : 86 + index * 500, -146), new Vector2(488, 634),
                FormalUiTheme.Surface);
            RectTransform rect = card.GetComponent<RectTransform>();
            Image background = card.GetComponent<Image>();
            background.raycastTarget = true;
            AddRewardCardFrame(card.transform, rect.sizeDelta);
            FormalUiKit.Line(card.transform, new Vector2(16, -16), new Vector2(456, 4), FormalUiTheme.Amber, "卡牌顶光");
            GameObject portrait = FormalUiKit.FlatPanel("奖励画像底", card.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -60), new Vector2(440, 430), FormalUiTheme.Panel);
            AddLootIcon(portrait.transform, "奖励大图标_" + reward.Id, iconPath, new Vector2(60, -54), 320);
            AddLabel(card.transform, "奖励类别", RewardCategory(reward), new Vector2(28, -18), new Vector2(432, 40),
                22, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            FormalUiKit.Line(card.transform, new Vector2(24, -500), new Vector2(440, 2), FormalUiTheme.Amber, "卡牌分隔");
            AddLabel(card.transform, "名称", reward.DisplayName, new Vector2(24, -506), new Vector2(440, 60),
                34, FormalUiTheme.Text, TextAnchor.MiddleCenter);
            AddLabel(card.transform, "查看详情", "悬浮查看详情", new Vector2(24, -576), new Vector2(440, 36),
                20, FormalUiTheme.Muted, TextAnchor.MiddleCenter);
            Button button = card.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => TrySelect(reward.Id));
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(background.color, FormalUiTheme.Amber),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            FormalHoverTooltipTrigger trigger = card.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () => RewardTooltipContent(reward, iconPath));
            rewardCards.Add(new RewardCardInput { RewardId = reward.Id, Reward = reward,
                CanClaim = RogueliteEconomyPresentation.ForReward(run, reward).CanExecute,
                Rect = rect, Image = background, Button = button, Normal = background.color,
                Hover = Color.Lerp(background.color, FormalUiTheme.Amber, .2f) });
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (!motion.IsImmediate)
            {
                CanvasGroup group = card.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.StandardDuration)
                    .SetDelay(index * motion.QuickDuration).SetUpdate(true);
            }
        }

        private static string RewardIconPath(RogueliteReward reward)
        {
            FireSpellDefinition spell = FireSpellCatalog.All.FirstOrDefault(value => value.Id == reward.Id);
            return reward.Kind == RogueliteRewardKind.Item ? reward.Item.IconPath :
                reward.Kind == RogueliteRewardKind.Equipment ? FormalArtRegistry.EquipmentIconPath(reward.Equipment.DefinitionId) :
                reward.Kind == RogueliteRewardKind.Spell && spell != null ? spell.IconPath :
                reward.Kind == RogueliteRewardKind.Spell ? FormalRogueliteUi.RogueSpellIconPath(reward.Id) :
                reward.Kind == RogueliteRewardKind.Resource ? FormalArtRegistry.SemanticPath("notice") :
                reward.Kind == RogueliteRewardKind.TacticalItem ? FormalArtRegistry.ItemPath(reward.TacticalItem.DefinitionId) :
                FormalArtRegistry.ItemPath(reward.Id);
        }

        private static string RewardEffect(RogueliteReward reward)
        {
            FireSpellDefinition spell = FireSpellCatalog.All.FirstOrDefault(value => value.Id == reward.Id);
            string effect = spell != null ? FireSpellPlayerSummary(spell) :
                reward.RogueSpell != null ? RogueSpellPlayerSummary(reward.RogueSpell) :
                reward.Item != null ? reward.Item.Description :
                reward.Kind == RogueliteRewardKind.Equipment ? EquipmentSlotLabel(reward.Equipment.Slot) + " · 重量 " + reward.Equipment.BaseWeight :
                reward.Kind == RogueliteRewardKind.TacticalItem ? "次数 " + reward.TacticalItem.MaximumCharges : reward.BuildPath;
            return effect.Length > 50 ? effect.Substring(0, 49) + "…" : effect;
        }

        private static string RewardCategory(RogueliteReward reward) => reward.Kind == RogueliteRewardKind.Spell ? "术式" :
            reward.Kind == RogueliteRewardKind.Equipment ? "装备" :
            reward.Kind == RogueliteRewardKind.TacticalItem || reward.Item?.Category == ItemCategory.Artifact ? "法宝" :
            reward.Kind == RogueliteRewardKind.Resource ? "物资" :
            reward.Kind == RogueliteRewardKind.Weapon ? "武器" : "物品";

        private static FormalTooltipContent RewardTooltipContent(RogueliteReward reward, string iconPath)
        {
            string category = RewardCategory(reward);
            if (FormalRogueliteUi.TryGetSharedContentCard(category, reward.DisplayName, out FormalTooltipContent shared))
                return shared;
            FireSpellDefinition fireSpell = FireSpellCatalog.All.FirstOrDefault(value => value.Id == reward.Id);
            ArtifactDefinition artifact = reward.TacticalItem == null ? null :
                ArtifactCatalog.All.FirstOrDefault(value => value.Id == reward.Id);
            string effect = fireSpell != null ? FireSpellPlayerSummary(fireSpell) :
                reward.RogueSpell != null ? RogueSpellPlayerSummary(reward.RogueSpell) :
                artifact != null ? artifact.EffectSummary + "。" + artifact.RiskSummary :
                reward.Item != null ? reward.Item.Description : reward.BuildPath;
            string metricA = reward.RogueSpell != null ? "行动 " + reward.RogueSpell.ActionPointCost :
                reward.TacticalItem != null ? "行动 " + reward.TacticalItem.ActionPointCost :
                reward.Equipment != null ? "占格 " + reward.Equipment.Width + "×" + reward.Equipment.Height :
                reward.Item != null ? "占格 " + reward.Item.Width + "×" + reward.Item.Height :
                reward.Kind == RogueliteRewardKind.Resource ? "数量 " + reward.ResourceAmount : string.Empty;
            string metricB = reward.RogueSpell != null ? "魔力 " + reward.RogueSpell.ManaCost :
                reward.TacticalItem != null ? "次数 " + reward.TacticalItem.MaximumCharges :
                reward.Equipment != null ? "重量 " + reward.Equipment.BaseWeight :
                reward.Item != null && reward.Item.MaximumUses > 0 ? "次数 " + reward.Item.MaximumUses : string.Empty;
            string metricC = reward.RogueSpell != null ? "冷却 " + reward.RogueSpell.CooldownOwnTurns :
                reward.Equipment != null ? "负荷 " + reward.Equipment.BaseAetherLoad : string.Empty;
            return new FormalTooltipContent(category, "候选", reward.DisplayName, reward.BuildPath,
                metricA, metricB, metricC, effect, string.Empty, FormalUiTheme.Amber, iconPath);
        }

        private void AddLootChoiceRow(Transform parent, RogueliteReward reward, int index, RogueliteMapRun run)
        {
            GameObject row = CreateObject(index == 0 ? "reward.first" : "reward." + index, parent);
            RectTransform rect = row.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -88f - index * 163f);
            rect.sizeDelta = new Vector2(986f, 148f);
            Image background = row.AddComponent<Image>();
            bool selected = run.SelectedRewardId == reward.Id || run.SelectedRewardId == "dismantle:" + reward.Id;
            Color accent = reward.Kind == RogueliteRewardKind.Weapon || reward.Kind == RogueliteRewardKind.Equipment
                ? FormalUiTheme.Cyan : FormalUiTheme.Amber;
            background.color = Color.Lerp(FormalUiTheme.SurfaceRaised, accent, selected ? .19f : .055f);
            AddRewardCardFrame(row.transform, rect.sizeDelta);
            if (selected) FormalUiKit.Line(row.transform, new Vector2(2f, -2f), new Vector2(6f, 144f), accent, "选中线");

            string category = reward.Kind == RogueliteRewardKind.Spell ? "个人术式" :
                reward.Kind == RogueliteRewardKind.Equipment ? "装备" :
                reward.Kind == RogueliteRewardKind.TacticalItem ? "战术道具" :
                reward.Kind == RogueliteRewardKind.Resource ? "物资" :
                reward.Kind == RogueliteRewardKind.Weapon ? "武器" :
                reward.Item != null && reward.Item.Category == ItemCategory.Artifact ? "法宝" : "物品";
            FireSpellDefinition spell = FireSpellCatalog.All.FirstOrDefault(value => value.Id == reward.Id);
            string iconPath = reward.Kind == RogueliteRewardKind.Item ? reward.Item.IconPath :
                reward.Kind == RogueliteRewardKind.Equipment ? FormalArtRegistry.EquipmentIconPath(reward.Equipment.DefinitionId) :
                reward.Kind == RogueliteRewardKind.Spell && spell != null ? spell.IconPath :
                reward.Kind == RogueliteRewardKind.Spell ? FormalRogueliteUi.RogueSpellIconPath(reward.Id) :
                reward.Kind == RogueliteRewardKind.Resource ? FormalArtRegistry.SemanticPath("notice") :
                reward.Kind == RogueliteRewardKind.TacticalItem ? FormalArtRegistry.ItemPath(reward.TacticalItem.DefinitionId) :
                FormalArtRegistry.ItemPath(reward.Id);
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            GameObject icon = CreateObject("战利品图标_" + reward.Id, row.transform);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(30f, -40f);
            iconRect.sizeDelta = new Vector2(78f, 78f);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            string effect = spell != null ? FireSpellPlayerSummary(spell) :
                reward.RogueSpell != null ? RogueSpellPlayerSummary(reward.RogueSpell) :
                reward.Item != null ? reward.Item.Description :
                reward.Kind == RogueliteRewardKind.Equipment ? EquipmentSlotLabel(reward.Equipment.Slot) + " · 重量 " + reward.Equipment.BaseWeight :
                reward.Kind == RogueliteRewardKind.TacticalItem ? "完整次数 " + reward.TacticalItem.MaximumCharges :
                reward.Kind == RogueliteRewardKind.Resource ? reward.BuildPath : reward.BuildPath;
            AddLabel(row.transform, "类型", "0" + (index + 1) + "　" + category, new Vector2(132, -10), new Vector2(620, 30),
                24, accent, TextAnchor.MiddleLeft);
            AddLabel(row.transform, "名称", reward.DisplayName, new Vector2(132, -44), new Vector2(730, 42),
                31, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            string shortEffect = effect.Replace("\r", " ").Replace("\n", " · ");
            if (shortEffect.Length > 26) shortEffect = shortEffect.Substring(0, 25) + "…";
            AddLabel(row.transform, "关键效果", shortEffect, new Vector2(132, -96), new Vector2(690, 34),
                24, FormalUiTheme.Muted, TextAnchor.UpperLeft);
            AddLootIcon(row.transform, "查看详情图标", FormalArtRegistry.ItemPath("inventory_search"),
                new Vector2(920, -48), 44);
            if (selected) AddLabel(row.transform, "已选奖励",
                run.SelectedRewardId.StartsWith("dismantle:", System.StringComparison.Ordinal) ? "✓ 拆解" : "✓ 已选",
                new Vector2(802, -10), new Vector2(152, 32),
                24, accent, TextAnchor.MiddleRight);
            Button button = row.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => TryClaim(reward.Id));
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(background.color, accent),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            FormalHoverTooltipTrigger trigger = row.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () =>
            {
                if (FormalRogueliteUi.TryGetSharedContentCard(category, reward.DisplayName, out FormalTooltipContent content))
                    return content;
                string details = effect + "\n" + reward.BuildPath;
                if (reward.Equipment != null)
                    details += "\n占格 " + reward.Equipment.Width + "×" + reward.Equipment.Height +
                        "　重量 " + reward.Equipment.BaseWeight + "　以太负荷 " + reward.Equipment.BaseAetherLoad;
                if (reward.TacticalItem != null)
                    details += "\n占格 " + reward.TacticalItem.Width + "×" + reward.TacticalItem.Height +
                        "　次数 " + reward.TacticalItem.MaximumCharges;
                if (reward.Item != null)
                    details += "\n占格 " + reward.Item.Width + "×" + reward.Item.Height +
                        "　次数 " + reward.Item.MaximumUses;
                if (reward.RogueSpell != null)
                    details += "\n行动 " + reward.RogueSpell.ActionPointCost + "　魔力 " + reward.RogueSpell.ManaCost +
                        "　射程 " + reward.RogueSpell.Range;
                return new FormalTooltipContent(category, reward.DisplayName, details, accent, iconPath);
            });
            if (run.CanDismantleReward(reward))
            {
                GameObject salvage = CreateObject("拆解_" + reward.Id, row.transform);
                RectTransform salvageRect = salvage.AddComponent<RectTransform>();
                salvageRect.anchorMin = salvageRect.anchorMax = salvageRect.pivot = new Vector2(0f, 1f);
                salvageRect.anchoredPosition = new Vector2(850f, -86f);
                salvageRect.sizeDelta = new Vector2(56f, 52f);
                Image salvageImage = salvage.AddComponent<Image>();
                salvageImage.sprite = Resources.Load<Sprite>(FormalArtRegistry.ItemPath("inventory_salvage"));
                salvageImage.preserveAspect = true;
                Button salvageButton = salvage.AddComponent<Button>();
                salvageButton.targetGraphic = salvageImage;
                salvageButton.onClick.AddListener(() => TryClaim("dismantle:" + reward.Id));
                FormalHoverTooltipTrigger salvageTip = salvage.AddComponent<FormalHoverTooltipTrigger>();
                salvageTip.Configure(tooltip, () => new FormalTooltipContent("拆解", reward.DisplayName,
                    reward.Kind == RogueliteRewardKind.Equipment ? "换取一份锻造材料" : "换取一份专精材料",
                    FormalUiTheme.Amber, FormalArtRegistry.ItemPath("inventory_salvage")));
            }
            rewardCards.Add(new RewardCardInput
            {
                RewardId = reward.Id, Reward = reward, CanClaim = RogueliteEconomyPresentation.ForReward(run, reward).CanExecute,
                Rect = rect, Image = background, Button = button, Normal = background.color,
                Hover = Color.Lerp(background.color, accent, .22f)
            });
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            if (!motion.IsImmediate)
            {
                rect.localScale = Vector3.one * (1f - motion.ModalScaleOffset);
                rect.DOScale(1f, motion.StandardDuration).SetDelay(index * motion.QuickDuration).SetEase(FormalUiMotionTokens.StandardEase).SetUpdate(true);
            }
        }

        private void DrawBackpackPreview(Transform parent, RogueliteMapRun run, IReadOnlyList<RogueliteReward> choices,
            string fixedText, bool unopened, bool ritualReward)
        {
            RogueEquipmentRuntime runtime = run.RogueRunState == null ? null : RogueEquipmentRuntime.FromDto(run.RogueRunState);
            IReadOnlyList<RogueInventoryItemPresentation> items = RogueInventoryPresentation.Build(runtime);
            RogueliteReward selected = choices.FirstOrDefault(value => value.Id == run.SelectedRewardId ||
                "dismantle:" + value.Id == run.SelectedRewardId);
            bool wideBackpack = runtime != null && runtime.BackpackColumns > 7;
            bool bookBackpack = ritualReward && runtime != null && !wideBackpack;
            float paneWidth = parent.GetComponent<RectTransform>().sizeDelta.x;
            if (bookBackpack)
                parent.GetComponent<Image>().color = FormalUiTheme.Surface;
            AddLootIcon(parent, "背包标记", FormalArtRegistry.EquipmentSlotPath("Backpack"), new Vector2(24, -17), 40);
            AddLabel(parent, "背包标题", "背包", new Vector2(76, -17), new Vector2(300, 42),
                27, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            int usedCells = items.Sum(item => item.Width * item.Height);
            AddLabel(parent, "背包容量", runtime == null ? "未启用" : bookBackpack
                    ? usedCells + " / " + runtime.BackpackColumns * runtime.BackpackRows
                    : runtime.BackpackColumns + " × " + runtime.BackpackRows,
                new Vector2(paneWidth - 198, -21), new Vector2(174, 36), 24,
                FormalUiTheme.Amber, TextAnchor.MiddleRight);
            FormalUiKit.Line(parent, new Vector2(24, -67), new Vector2(paneWidth - 48, 2), FormalUiTheme.WithAlpha(FormalUiTheme.Amber, .66f), "背包分隔");
            if (runtime != null)
            {
                if (bookBackpack)
                {
                    FormalUiKit.FlatPanel("背包收纳槽", parent, new Vector2(0, 1), new Vector2(0, 1),
                        new Vector2(20, -83), new Vector2(paneWidth - 40f, 526), FormalUiTheme.Panel);
                }
                int gridColumns = bookBackpack ? runtime.BackpackRows : runtime.BackpackColumns;
                int gridRows = bookBackpack ? runtime.BackpackColumns : runtime.BackpackRows;
                float cell = wideBackpack
                    ? Mathf.Min(45f, (paneWidth - 58f) / runtime.BackpackColumns, 350f / runtime.BackpackRows)
                    : bookBackpack ? Mathf.Min(64f, (paneWidth - 64f) / gridColumns, 486f / gridRows)
                    : Mathf.Min(46f, 280f / runtime.BackpackColumns, 480f / runtime.BackpackRows);
                float originX = wideBackpack ? Mathf.Round((paneWidth - runtime.BackpackColumns * cell) * .5f) :
                    bookBackpack ? Mathf.Round((paneWidth - gridColumns * cell) * .5f) : 24f;
                float gridY = bookBackpack ? -83f - Mathf.Round((526f - gridRows * cell) * .5f) : -89f;
                for (int y = 0; y < runtime.BackpackRows; y++)
                for (int x = 0; x < runtime.BackpackColumns; x++)
                    FormalUiKit.FlatPanel("背包格_" + x + "_" + y, parent, new Vector2(0, 1), new Vector2(0, 1),
                        new Vector2(originX + (bookBackpack ? y : x) * cell + 1, gridY - (bookBackpack ? x : y) * cell - 1),
                        new Vector2(cell - 2, cell - 2),
                        bookBackpack ? Color.Lerp(FormalUiTheme.Panel, FormalUiTheme.Ink, (x + y) % 2 == 0 ? .09f : .13f)
                            : FormalUiTheme.WithAlpha(FormalUiTheme.Surface, .8f));
                foreach (RogueInventoryItemPresentation item in items)
                {
                    GameObject placed = FormalUiKit.FlatPanel("背包物品_" + item.InstanceId, parent,
                        new Vector2(0, 1), new Vector2(0, 1),
                        new Vector2(originX + (bookBackpack ? item.Y : item.X) * cell + 1,
                            gridY - (bookBackpack ? item.X : item.Y) * cell - 1),
                        new Vector2((bookBackpack ? item.Height : item.Width) * cell - 2,
                            (bookBackpack ? item.Width : item.Height) * cell - 2),
                        bookBackpack ? Color.Lerp(FormalUiTheme.SurfaceRaised, item.IsEquipment ? FormalUiTheme.Cyan : FormalUiTheme.Amber, .20f)
                            : Color.Lerp(FormalUiTheme.SurfaceRaised, item.IsEquipment ? FormalUiTheme.Cyan : FormalUiTheme.Amber, .2f));
                    placed.GetComponent<Image>().raycastTarget = true;
                    string path = item.IsMaterial ? FormalArtRegistry.ResourceMetricPath(item.DefinitionId.StartsWith("FORGE-") ? "parts" : "operational_aether") :
                        item.IsEquipment ? FormalArtRegistry.EquipmentFootprintPath(item.DefinitionId) : FormalArtRegistry.ItemPath(item.DefinitionId);
                    Sprite sprite = Resources.Load<Sprite>(path);
                    GameObject icon = CreateObject("物品图标", placed.transform);
                    RectTransform iconRect = icon.AddComponent<RectTransform>();
                    iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, .5f);
                    iconRect.pivot = new Vector2(.5f, .5f);
                    iconRect.sizeDelta = new Vector2(Mathf.Min(50f, (bookBackpack ? item.Height : item.Width) * cell - 8f),
                        Mathf.Min(50f, (bookBackpack ? item.Width : item.Height) * cell - 8f));
                    Image image = icon.AddComponent<Image>();
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    FormalHoverTooltipTrigger trigger = placed.AddComponent<FormalHoverTooltipTrigger>();
                    trigger.Configure(tooltip, () => new FormalTooltipContent("背包物品", item.DisplayName,
                        item.CompactBadge, FormalUiTheme.Cyan, path));
                }
                if (selected != null && !run.SelectedRewardId.StartsWith("dismantle:", System.StringComparison.Ordinal) &&
                    (run.IsInAcademyLayer ? run.CanAcceptIndividualLootChoice(selected) :
                        RogueliteEconomyPresentation.ForReward(run, selected).CanExecute))
                {
                    int width = selected.Kind == RogueliteRewardKind.Equipment ? selected.Equipment.Width :
                        selected.Kind == RogueliteRewardKind.TacticalItem ? selected.TacticalItem.Width :
                        selected.Kind == RogueliteRewardKind.Item ? selected.Item.Width : 0;
                    int height = selected.Kind == RogueliteRewardKind.Equipment ? selected.Equipment.Height :
                        selected.Kind == RogueliteRewardKind.TacticalItem ? selected.TacticalItem.Height :
                        selected.Kind == RogueliteRewardKind.Item ? selected.Item.Height : 0;
                    if (width > 0 && height > 0)
                    {
                        bool[,] occupied = new bool[runtime.BackpackColumns, runtime.BackpackRows];
                        foreach (RogueInventoryItemPresentation item in items)
                            for (int y = item.Y; y < item.Y + item.Height; y++)
                            for (int x = item.X; x < item.X + item.Width; x++)
                                if (x >= 0 && y >= 0 && x < runtime.BackpackColumns && y < runtime.BackpackRows) occupied[x, y] = true;
                        if (RogueInventoryGridSystem.TryFindFirstFit(occupied, width, height, out RogueLoadoutGridPoint fit))
                        {
                            GameObject ghost = FormalUiKit.FlatPanel("预计入袋占格", parent, new Vector2(0, 1), new Vector2(0, 1),
                                new Vector2(originX + (bookBackpack ? fit.Y : fit.X) * cell + 1,
                                    gridY - (bookBackpack ? fit.X : fit.Y) * cell - 1),
                                new Vector2((bookBackpack ? height : width) * cell - 2,
                                    (bookBackpack ? width : height) * cell - 2),
                                FormalUiTheme.WithAlpha(FormalUiTheme.Amber, bookBackpack ? .55f : .46f));
                            string path = selected.Kind == RogueliteRewardKind.Equipment ?
                                FormalArtRegistry.EquipmentFootprintPath(selected.Equipment.DefinitionId) :
                                selected.Kind == RogueliteRewardKind.TacticalItem ?
                                    FormalArtRegistry.ItemPath(selected.TacticalItem.DefinitionId) : selected.Item.IconPath;
                            AddLootIcon(ghost.transform, "待放入图标", path, new Vector2(6, -6),
                                Mathf.Min(48f, Mathf.Min(width * cell, height * cell) - 12f));
                            AddLabel(ghost.transform, "待入袋标记", "+", new Vector2((bookBackpack ? height : width) * cell - 34f, -2f),
                                new Vector2(30, 34), 28, FormalUiTheme.Amber, TextAnchor.MiddleCenter);
                            ghost.GetComponent<Image>().raycastTarget = true;
                            FormalHoverTooltipTrigger trigger = ghost.AddComponent<FormalHoverTooltipTrigger>();
                            trigger.Configure(tooltip, () => new FormalTooltipContent("待领取", selected.DisplayName,
                                RewardEffect(selected), FormalUiTheme.Amber, path));
                        }
                    }
                }
                if (bookBackpack)
                    return;
                AddLootIcon(parent, "背包件数图标", FormalArtRegistry.ItemPath("category_container"),
                    wideBackpack ? new Vector2(26, -455) : new Vector2(318, -112), 36);
                AddLabel(parent, "背包现况", items.Count.ToString(),
                    wideBackpack ? new Vector2(72, -455) : new Vector2(366, -112),
                    new Vector2(100, 42), 28, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            }
            string destination = unopened || selected == null ? "待选" :
                selected.Kind == RogueliteRewardKind.Spell ? "术式库" :
                selected.Kind == RogueliteRewardKind.Resource ? "资源账" :
                run.SelectedRewardId.StartsWith("dismantle:") ? "材料" : "背包";
            string destinationIcon = selected == null ? FormalArtRegistry.ItemPath("loot_unknown") :
                selected.Kind == RogueliteRewardKind.Spell ? FormalArtRegistry.CommandPath("skill") :
                selected.Kind == RogueliteRewardKind.Resource ? FormalArtRegistry.ResourceMetricPath("gold") :
                run.SelectedRewardId.StartsWith("dismantle:") ? FormalArtRegistry.ItemPath("inventory_salvage") :
                FormalArtRegistry.EquipmentSlotPath("Backpack");
            Vector2 destinationPosition = wideBackpack ? new Vector2(26, -508) : new Vector2(318, -218);
            AddLootIcon(parent, "去向图标", destinationIcon, destinationPosition, 42);
            AddLabel(parent, "预计去向", destination,
                destinationPosition + new Vector2(52, 0),
                wideBackpack ? new Vector2(260, 48) : new Vector2(160, 48),
                27, selected == null ? FormalUiTheme.Muted : FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            bool ordinaryResources = fixedText.StartsWith("固定所得　金币 +", System.StringComparison.Ordinal);
            if (ordinaryResources)
            {
                RogueliteMapNode node = run.MapNodes.FirstOrDefault(value => value.Id == run.CurrentNodeId);
                int gold = node?.Type == RogueliteMapNodeType.Finale ? 10 : node?.Type == RogueliteMapNodeType.Elite ? 6 : 3;
                int contribution = node?.Type == RogueliteMapNodeType.Finale ? 3 : node?.Type == RogueliteMapNodeType.Elite ? 2 : 1;
                AddLabel(parent, "固定所得标题", "固定所得",
                    wideBackpack ? new Vector2(294, -508) : new Vector2(318, -312),
                    new Vector2(210, 36), 24, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
                Vector2 chipOrigin = wideBackpack ? new Vector2(294, -552) : new Vector2(318, -358);
                AddLootIcon(parent, "金币图标", FormalArtRegistry.ResourceMetricPath("gold"), chipOrigin, 40);
                AddLabel(parent, "金币所得", "+" + gold, chipOrigin + new Vector2(50, 0), new Vector2(100, 42), 26,
                    FormalUiTheme.Amber, TextAnchor.MiddleLeft);
                AddLootIcon(parent, "学院贡献图标", FormalArtRegistry.ResourceMetricPath("contribution"),
                    chipOrigin + new Vector2(0, wideBackpack ? -46 : -58), 40);
                AddLabel(parent, "学院贡献所得", "+" + contribution, chipOrigin + new Vector2(50, wideBackpack ? -46 : -58),
                    new Vector2(100, 42), 26, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
                int materialCount = run.RogueRunState?.PendingFixedMaterialIds.Count ?? 0;
                if (materialCount > 0)
                {
                    Image materialIcon = AddLootIcon(parent, "固定材料图标", FormalArtRegistry.ResourceMetricPath("parts"),
                        chipOrigin + (wideBackpack ? new Vector2(132, -46) : new Vector2(0, -116)), 40);
                    materialIcon.raycastTarget = true;
                    AddLabel(parent, "固定材料数量", "×" + materialCount,
                        chipOrigin + (wideBackpack ? new Vector2(174, -46) : new Vector2(50, -116)),
                        new Vector2(68, 42), 26, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
                    FormalHoverTooltipTrigger materialTip = materialIcon.gameObject.AddComponent<FormalHoverTooltipTrigger>();
                    materialTip.Configure(tooltip, () => new FormalTooltipContent("固定所得", "工坊材料",
                        fixedText, FormalUiTheme.Amber, FormalArtRegistry.ResourceMetricPath("parts")));
                }
            }
            else if (!string.IsNullOrEmpty(fixedText))
                AddLabel(parent, "固定所得", fixedText,
                    wideBackpack ? new Vector2(26, -558) : new Vector2(318, -370),
                    wideBackpack ? new Vector2(510, 50) : new Vector2(220, 205),
                    24, FormalUiTheme.Amber, TextAnchor.UpperLeft);
        }

        private static string AffinityLabel(FireCombatAffinity value) => value == FireCombatAffinity.MeleeOnly ? "近战亲和" : value == FireCombatAffinity.RangedSpell ? "远程亲和" : "近远程通用";
        private static string FireSpellRarityLabel(FireSpellRarity value) => value == FireSpellRarity.Common ? "普通" : value == FireSpellRarity.Uncommon ? "罕见" : "稀有";
        private static string EquipmentSlotLabel(OCC.Combat.Roguelite.EquipmentSlot value)
            => value == OCC.Combat.Roguelite.EquipmentSlot.Weapon ? "武器" : value == OCC.Combat.Roguelite.EquipmentSlot.Head ? "头部" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Chest ? "胸部" : value == OCC.Combat.Roguelite.EquipmentSlot.Feet ? "足部" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Backpack ? "背部" : value == OCC.Combat.Roguelite.EquipmentSlot.Ring1 ? "戒指一" :
                value == OCC.Combat.Roguelite.EquipmentSlot.Ring2 ? "戒指二" : value == OCC.Combat.Roguelite.EquipmentSlot.Necklace ? "项链" : "施法单元";

        private static string HandednessLabel(OCC.Combat.Roguelite.EquipmentHandedness value)
            => value == OCC.Combat.Roguelite.EquipmentHandedness.OneHanded ? "单手" : value == OCC.Combat.Roguelite.EquipmentHandedness.TwoHanded ? "双手" :
                value == OCC.Combat.Roguelite.EquipmentHandedness.OffHand ? "副手" : "无手持要求";

        private static string DeliveryLabel(FireDeliveryMode value) => value == FireDeliveryMode.WeaponAttachment ? "武器附着" : value == FireDeliveryMode.DetachedProjection ? "远程投射" : value == FireDeliveryMode.BodyEnhancement ? "身体强化" : value == FireDeliveryMode.ContactConduction ? "接触导能" : value == FireDeliveryMode.SelfStance ? "自身架势" : value == FireDeliveryMode.TargetMarking ? "目标标记" : value == FireDeliveryMode.Movement ? "位移" : "操纵火场";
        private static string WeaponLabel(FireWeaponRequirement value) => value == FireWeaponRequirement.MeleeWeapon ? "需近战武器" : value == FireWeaponRequirement.RangedWeapon ? "需远程武器" : value == FireWeaponRequirement.AnyWeapon ? "需任意武器" : "无武器要求";
        private static string ShapeLabel(FireSelectionShape value) => CombatRangeText.ShapeText(value, 1);


        public static string FireSpellPlayerSummary(FireSpellDefinition spell)
        {
            if (spell != null && FireSpellCardCopyCatalog.TryGet(spell.Id, out string cardCopy)) return cardCopy;
            string timing = FireTimingPlayerText(spell.TriggerWindow);
            string effects = string.Join("；", spell.Rules.Select(FireRulePlayerText));
            return timing + effects;
        }

        public static string FireSpellTargetSummary(FireSpellDefinition spell)
        {
            string target = spell.TargetKind == FireTargetKind.Self ? "自身" :
                spell.TargetKind == FireTargetKind.Enemy ? "一名敌人" :
                spell.TargetKind == FireTargetKind.AllyOrSelf ? "自身或一名友军" :
                spell.TargetKind == FireTargetKind.Unit ? "一个单位" :
                spell.TargetKind == FireTargetKind.Cell ? "一处战场格" :
                spell.TargetKind == FireTargetKind.EmptyCell ? "一个空地格" :
                spell.TargetKind == FireTargetKind.BurningUnit ? "一名燃烧单位" :
                spell.TargetKind == FireTargetKind.BurningEnemy ? "一名燃烧敌人" :
                spell.TargetKind == FireTargetKind.BurningCell ? "一处燃烧地格" :
                spell.TargetKind == FireTargetKind.Destructible ? "一处可破坏物件" :
                spell.TargetKind == FireTargetKind.Hittable ? "一名敌人或一处可破坏物件" :
                spell.TargetKind == FireTargetKind.AdjacentEnemy ? "一名相邻敌人" :
                spell.TargetKind == FireTargetKind.AdjacentBurningEnemy ? "一名相邻的燃烧敌人" :
                "一名燃烧或已破甲的敌人";
            return target + "　" + CombatRangeText.RangeLine(spell);
        }

        public static string FireSpellRangeText(FireSpellDefinition spell)
            => CombatRangeText.SelectionLine(spell);

        public static string RogueSpellTargetSummary(OCC.Combat.Roguelite.SpellDefinition spell)
        {
            if (spell == null) return "未装备";
            FireSpellDefinition fire = FireSpellCatalog.All.FirstOrDefault(value => value.Id == spell.DefinitionId);
            if (fire != null) return FireSpellTargetSummary(fire);
            switch (spell.DefinitionId)
            {
                case "BASE-FIRE-MELEE": return "相邻可见敌人或可破坏物件";
                case "BASE-FIRE-RANGED": return "4 格内可见敌人或可破坏物件";
                case "BASE-AETHER-SHIELD":
                case "BASE-MANA-RECOVER": return "自身";
                default: return spell.Range > 0 ? spell.Range + " 格内亮起的目标" : "自身";
            }
        }

        public static string RogueSpellPlayerSummary(OCC.Combat.Roguelite.SpellDefinition spell)
        {
            if (spell == null) return "术式槽为空";
            FireSpellDefinition fire = FireSpellCatalog.All.FirstOrDefault(value => value.Id == spell.DefinitionId);
            if (fire != null) return FireSpellPlayerSummary(fire);
            switch (spell.DefinitionId)
            {
                case "BASE-FIRE-MELEE": return "对单位或物件造成 8 点火焰伤害";
                case "BASE-FIRE-RANGED": return "对单位或物件造成 6 点火焰伤害";
                case "BASE-AETHER-SHIELD": return "自身获得 8 点普通盾";
                case "BASE-MANA-RECOVER": return "恢复 2 点个人魔力，最多恢复至 12";
                default: return "依照术式说明生效";
            }
        }

        private static string FireTimingPlayerText(FireTriggerWindow timing)
        {
            switch (timing)
            {
                case FireTriggerWindow.NextLegalWeaponAttack: return "下一次武器攻击：";
                case FireTriggerWindow.CurrentAction: return "本次行动：";
                case FireTriggerWindow.UntilNextAction: return "持续到下次行动：";
                case FireTriggerWindow.FirstAdjacentAttack: return "首次受到相邻攻击时：";
                case FireTriggerWindow.FirstMarkedTargetMove: return "标记目标首次移动时：";
                case FireTriggerWindow.FirstEnemyEntry: return "首名敌人进入时：";
                case FireTriggerWindow.AfterNextWeaponAttack: return "下一次武器攻击后：";
                default: return string.Empty;
            }
        }

        private static string FireRulePlayerText(FireSpellRule rule)
        {
            // 破障是词条标记（总案 3.5.6.1）：卡面只写可读结果，不暴露内部枚举名。
            if (rule.Kind == FireRuleKind.BreakBarrier) return "破障：对物件造成双倍耐久伤害";
            string condition = rule.Condition == FireCondition.TargetBurning ? "若目标正在燃烧，" :
                rule.Condition == FireCondition.TargetOnFireground ? "若目标位于火场，" :
                rule.Condition == FireCondition.TargetBurningAndOnFireground ? "若目标燃烧且位于火场，" :
                rule.Condition == FireCondition.TargetArmorBroken ? "若目标已破甲，" :
                rule.Condition == FireCondition.TargetBurningOrArmorBroken ? "若目标燃烧或已破甲，" :
                rule.Condition == FireCondition.SourceBurning ? "若自身正在燃烧，" :
                rule.Condition == FireCondition.SourceNotBurning ? "若自身没有燃烧，" :
                rule.Condition == FireCondition.SourceBound ? "若自身被束缚，" :
                rule.Condition == FireCondition.SourceSlowed ? "若自身处于迟缓，" :
                rule.Condition == FireCondition.SourceNotArmorBroken ? "若自身没有破甲，" :
                rule.Condition == FireCondition.TargetAtWeaponMaxRange ? "若目标位于当前武器最大射程，" :
                rule.Condition == FireCondition.LightCoverDestroyed ? "若轻掩体被摧毁，" :
                rule.Condition == FireCondition.DurabilityDepleted ? "若目标耐久归零，" : string.Empty;
            string effect;
            switch (rule.Kind)
            {
                case FireRuleKind.Damage: effect = "造成 " + rule.Amount + " 点火焰伤害"; break;
                case FireRuleKind.WeaponDamage: effect = "武器伤害 +" + rule.Amount; break;
                case FireRuleKind.ApplyBurning: effect = "施加燃烧 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ExtendBurning: effect = "燃烧至少延至 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyArmorBreak: effect = "施加破甲 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyBreakStance: effect = "清空护盾，施加破势"; break;
                case FireRuleKind.CreateFireground: effect = "生成火场 " + rule.Duration + " 来源自身回合"; break;
                case FireRuleKind.ExtendFireground: effect = "火场延长 " + rule.Duration + " 来源自身回合"; break;
                case FireRuleKind.ApplyFiregroundBoost: effect = "获得火势 +" + rule.Amount + "，持续 " + rule.Duration + " 回合"; break;
                case FireRuleKind.ApplyFiregroundVulnerability: effect = "施加助燃 +" + rule.Amount + "，持续 " + rule.Duration + " 回合"; break;
                case FireRuleKind.RestoreShield: effect = "恢复 " + rule.Amount + " 点护盾"; break;
                case FireRuleKind.RestoreMana: effect = "恢复 " + rule.Amount + " 点以太"; break;
                case FireRuleKind.RestoreMovement: effect = "恢复 " + rule.Amount + " 步"; break;
                case FireRuleKind.AddMovement: effect = "本轮额外移动 " + rule.Amount + " 格"; break;
                case FireRuleKind.MoveSource: effect = "移至目标格"; break;
                case FireRuleKind.MoveAfterAttack: effect = "攻击后向远离目标的方向后退 " + rule.Amount + " 格"; break;
                case FireRuleKind.SwapUnits: effect = "与目标交换位置"; break;
                case FireRuleKind.Push: effect = "将目标推开 " + rule.Amount + " 格"; break;
                case FireRuleKind.PushAllUnits: effect = "将范围内单位推开 " + rule.Amount + " 格"; break;
                case FireRuleKind.PushFromDestroyedObjects: effect = "物件被摧毁时，将其相邻单位向外推 " + rule.Amount + " 格，同一单位最多一次"; break;
                case FireRuleKind.BreakBarrier: effect = "对物块造成的耐久伤害翻倍"; break;
                case FireRuleKind.CreateLightCover: effect = "在目标格生成耐久 " + rule.Amount + " 的轻掩体"; break;
                case FireRuleKind.AdvanceIntoBreach: effect = "打开缺口后继续前进 " + rule.Amount + " 格"; break;
                case FireRuleKind.ClearBoundOrSlow: effect = "清除自身的束缚或迟缓"; break;
                case FireRuleKind.OfferRetreat: effect = "下次突进后沿原路退回 " + rule.Amount + " 格"; break;
                case FireRuleKind.OfferBreachMove: effect = "下次亲手摧毁物块后移入空出的格子"; break;
                case FireRuleKind.ArmAllyNextAttack: effect = "攻击燃烧敌人后，友方下一次攻击额外造成 " + rule.Amount + " 点火焰伤害"; break;
                case FireRuleKind.ApplyMeltBarrierMark: effect = "施加持续 " + rule.Duration + " 回合的熔障标记"; break;
                case FireRuleKind.ApplyFracture: effect = "施加裂痕"; break;
                case FireRuleKind.ArmFractureShield: effect = "下次己方摧毁裂痕物块时额外获得 4 护盾"; break;
                case FireRuleKind.ReserveNextTurnAction: effect = "脱离全部敌方攻击范围时，下回合行动力 +" + rule.Amount; break;
                case FireRuleKind.ReduceIncomingDamage: effect = "下一次受到的伤害减少 " + rule.Amount; break;
                case FireRuleKind.GrantShieldBeforeRanged: effect = "首次符合条件的远距伤害前获得 " + rule.Amount + " 点护盾"; break;
                case FireRuleKind.DamageDurability: effect = "对物件造成 " + rule.Amount + " 点耐久伤害"; break;
                case FireRuleKind.DestroyLightCover: effect = "摧毁轻掩体"; break;
                case FireRuleKind.ClearStatus: effect = "清除一个负面状态"; break;
                case FireRuleKind.ClearOneSelfStatus: effect = "清除自身一种负面状态"; break;
                case FireRuleKind.ConsumeBurning: effect = "消耗目标的燃烧"; break;
                case FireRuleKind.ConsumeFireground: effect = "消耗目标地格的火场"; break;
                case FireRuleKind.SetBurningDuration: effect = "将燃烧调整为 " + rule.Duration + " 回合"; break;
                case FireRuleKind.LoseHealth: effect = "自身失去 " + rule.Amount + " 点生命"; break;
                case FireRuleKind.RepairWeapon: effect = "恢复武器 " + rule.Amount + " 点耐久"; break;
                case FireRuleKind.SpendActionPoints: effect = "额外消耗 " + rule.Amount + " 点行动"; break;
                case FireRuleKind.SpendMana: effect = "额外消耗 " + rule.Amount + " 点以太"; break;
                case FireRuleKind.ReduceForcedMove: effect = "自身下回合前，下一次受到的强制位移距离减少 " + rule.Amount + " 格，最低为 0"; break;
                case FireRuleKind.ArmTrigger: effect = "布置一次待触发效果"; break;
                case FireRuleKind.ConsumeTrigger: effect = "触发后移除该效果"; break;
                case FireRuleKind.OverloadDevice: effect = "使装置过载" + (rule.Amount > 0 ? "，造成 " + rule.Amount + " 点效果" : string.Empty); break;
                default: effect = "产生术式效果"; break;
            }
            if (rule.AlternateAmount > 0) effect += "，满足条件时提高至 " + rule.AlternateAmount;
            return condition + effect;
        }

        private void Hide()
        {
            if (panel != null)
            {
                foreach (RewardCardInput card in rewardCards)
                {
                    card.Image?.DOKill();
                    card.Rect?.DOKill();
                }
                panel.transform.DOKill();
                if (Application.isPlaying) Destroy(panel);
                else DestroyImmediate(panel);
            }
            panel = null;
            presentedSeed = int.MinValue;
            rewardCards.Clear();
            claimPending = false;
        }

        public void HideForInventory()
        {
            Hide();
            hasPresentedModel = false;
        }

        private void TrySelect(string rewardId)
        {
            if (claimPending || bootstrap == null || string.IsNullOrWhiteSpace(rewardId)) return;
            choiceWindowSuppressed = false;
            try { bootstrap.SelectMapReward(rewardId); }
            catch (System.InvalidOperationException exception)
            {
                bootstrap.ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, exception.Message));
            }
        }

        private void TryClaimFixed(string lootId)
        {
            if (claimPending || bootstrap == null) return;
            claimPending = true;
            try { bootstrap.ClaimMapFixedLoot(lootId); }
            catch (System.InvalidOperationException exception)
            {
                claimPending = false;
                bootstrap.ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "没能拿走这件东西：" + exception.Message));
            }
        }

        private void TryClaim(string rewardId)
        {
            if (claimPending || bootstrap == null || string.IsNullOrWhiteSpace(rewardId)) return;
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            bool dismantle = rewardId.StartsWith("dismantle:", System.StringComparison.Ordinal);
            string choiceId = dismantle ? rewardId.Substring("dismantle:".Length) : rewardId;
            RewardCardInput selected = rewardCards.FirstOrDefault(card => card.RewardId == choiceId);
            RogueliteReward selectedReward = selected?.Reward ?? run?.CurrentRewards.FirstOrDefault(value => value.Id == choiceId);
            if (selectedReward == null && run != null)
            {
                FireSpellDefinition spell = run.CurrentFireSpellChoices.FirstOrDefault(value => value.Id == choiceId);
                if (spell != null) selectedReward = AsReward(spell);
            }
            bool individualAcademy = run != null && run.IsInAcademyLayer && run.UsesRogue11;
            UiOperationAvailability availability = individualAcademy
                ? new UiOperationAvailability(selectedReward != null && (!dismantle || run.CanDismantleReward(selectedReward)) &&
                    run.CanAcceptIndividualLootChoice(selectedReward, dismantle), "可领取", "先整理出这件奖励需要的行囊格")
                : dismantle
                    ? new UiOperationAvailability(run != null && run.CanDismantleReward(selectedReward) &&
                        run.CanAcceptAcademyReward(selectedReward, true), "拆解", "先整理出材料需要的行囊格")
                    : RogueliteEconomyPresentation.ForReward(run, selectedReward);
            if (!availability.CanExecute)
            {
                bootstrap.ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, availability.Reason));
                return;
            }
            claimPending = true;
            foreach (RewardCardInput card in rewardCards)
                card.Button?.GetComponent<UiButtonFeedback>()?.SetAvailability(false, "正在收好奖励");
            try
            {
                if (ShouldUseLegacyFireClaim(run, rewardId)) bootstrap.ClaimMapFireSpell(rewardId);
                else bootstrap.ClaimMapLootChoice(rewardId);
            }
            catch (System.InvalidOperationException exception)
            {
                claimPending = false;
                foreach (RewardCardInput card in rewardCards)
                    card.Button?.GetComponent<UiButtonFeedback>()?.SetAvailability(card.CanClaim, card.CanClaim ? string.Empty : "现在不能拿走这件东西");
                bootstrap.ShowUiFeedback(new UiActionFeedback(UiFeedbackKind.Rejected, "没能拿走这件东西：" + exception.Message));
            }
        }

        private static RogueliteReward AsReward(FireSpellDefinition spell)
        {
            int damage = spell.Rules.Where(rule => rule.Kind == FireRuleKind.Damage).Select(rule => rule.Amount).FirstOrDefault();
            SkillDefinition adapter = new SkillDefinition(spell.Id, spell.DisplayName, DamageType.Fire, System.Math.Max(1, damage), spell.Range, spell.ManaCost, spell.Cooldown, shape: spell.Shape);
            return new RogueliteReward(spell.Id, spell.DisplayName, adapter, spell.Group.ToString());
        }

        private static bool ShouldUseLegacyFireClaim(RogueliteMapRun run, string rewardId)
        {
            return run != null && !run.UsesRogue11 &&
                run.CurrentFireSpellChoices.Any(spell => spell.Id == rewardId);
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("肉鸽结算UI", UiLayoutContract.SettlementSortingOrder);
            tooltip = FormalHoverTooltip.Create(canvas);
        }

        private static void SetHover(RewardCardInput card, bool hovering)
        {
            if (card.IsHovering == hovering) return;
            card.IsHovering = hovering;
        }

        private void OnDestroy()
        {
            if (bootstrap != null) bootstrap.UiPresentationVersions.Changed -= OnPresentationChanged;
            Hide();
            if (canvas != null)
            {
                if (Application.isPlaying) Destroy(canvas.gameObject);
                else DestroyImmediate(canvas.gameObject);
            }
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            return FormalUiKit.Create(name, parent);
        }

        private static void Stretch(RectTransform rect)
        {
            FormalUiKit.Stretch(rect);
        }

        private static void AddLabel(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            Text text = FormalUiKit.Label(name, value, parent, position, size, fontSize, color, alignment);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void AddRewardCardFrame(Transform parent, Vector2 size)
        {
            AddRewardFrameEdge(parent, "奖励细框_上", Vector2.zero, new Vector2(size.x, 2f));
            AddRewardFrameEdge(parent, "奖励细框_下", new Vector2(0f, -size.y + 2f), new Vector2(size.x, 2f));
            AddRewardFrameEdge(parent, "奖励细框_左", Vector2.zero, new Vector2(2f, size.y));
            AddRewardFrameEdge(parent, "奖励细框_右", new Vector2(size.x - 2f, 0f), new Vector2(2f, size.y));
        }

        /// <summary>
        /// Lets one text row of the settlement card stretch past its nominal height. Everything anchored below
        /// that row moves down by the same delta, the card grows by it with the top edge pinned, and the frame
        /// is recomputed. Nominal content therefore keeps the designed layout untouched; only a row that really
        /// needs more room (a long 选取／作用 line, a three-line artifact effect) raises the card.
        /// </summary>
        private static void AdaptRewardRow(RectTransform card, Transform cardRoot, string rowName, float nominalHeight)
        {
            Transform row = cardRoot.Find(rowName);
            Text text = row == null ? null : row.GetComponent<Text>();
            RectTransform rowRect = row as RectTransform;
            if (text == null || rowRect == null) return;
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, nominalHeight);
            Canvas.ForceUpdateCanvases();
            float needed = Mathf.Ceil(text.preferredHeight);
            float delta = needed - nominalHeight;
            if (delta <= 0f) return;
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, needed);
            // Only what sits strictly below this row's bottom edge moves; the action/aether chips that share
            // the 数值 row keep their place.
            float rowBottom = rowRect.anchoredPosition.y - nominalHeight;
            for (int i = 0; i < cardRoot.childCount; i++)
            {
                RectTransform child = cardRoot.GetChild(i) as RectTransform;
                if (child == null || child == rowRect) continue;
                if (child.anchoredPosition.y >= rowBottom) continue;
                child.anchoredPosition -= new Vector2(0f, delta);
            }
            Vector2 size = card.sizeDelta + new Vector2(0f, delta);
            card.sizeDelta = size;
            card.anchoredPosition -= new Vector2(0f, delta * .5f);
            ResizeRewardCardFrame(cardRoot, size);
        }

        private static void ResizeRewardCardFrame(Transform parent, Vector2 size)
        {
            ResizeRewardFrameEdge(parent, "奖励细框_上", Vector2.zero, new Vector2(size.x, 2f));
            ResizeRewardFrameEdge(parent, "奖励细框_下", new Vector2(0f, -size.y + 2f), new Vector2(size.x, 2f));
            ResizeRewardFrameEdge(parent, "奖励细框_左", Vector2.zero, new Vector2(2f, size.y));
            ResizeRewardFrameEdge(parent, "奖励细框_右", new Vector2(size.x - 2f, 0f), new Vector2(2f, size.y));
        }

        private static void ResizeRewardFrameEdge(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform edge = parent.Find(name) as RectTransform;
            if (edge == null) return;
            edge.anchoredPosition = position;
            edge.sizeDelta = size;
        }

        private static void AddRewardFrameEdge(Transform parent, string name, Vector2 position, Vector2 size)
        {
            Image edge = FormalUiKit.FlatPanel(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                position, size, FormalUiTheme.Rule).GetComponent<Image>();
            edge.raycastTarget = false;
            edge.transform.SetAsLastSibling();
        }
    }
}
