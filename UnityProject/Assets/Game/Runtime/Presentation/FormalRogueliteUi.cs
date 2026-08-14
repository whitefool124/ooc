using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class FormalRogueliteUi : MonoBehaviour
    {
        private static Color ink => FormalUiTheme.Ink;
        private static Color panel => FormalUiTheme.Panel;
        private static Color cyan => FormalUiTheme.Cyan;
        private static Color amber => FormalUiTheme.Amber;
        private static Color safe => FormalUiTheme.Safe;
        private static Color danger => FormalUiTheme.Danger;
        private static Color text => FormalUiTheme.Text;
        private static Color muted => FormalUiTheme.Muted;
        private IRogueliteUiHost bootstrap;
        private Canvas canvas;
        private GameObject root;
        private FormalHoverTooltip tooltip;
        private GameObject content;
        private readonly UiNavigationState navigation = new UiNavigationState(UiScreen.Landing, "按钮_近战热压");
        private readonly Dictionary<string, GameObject> focusTargets = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> resourceDeltas = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Text> resourceValues = new Dictionary<string, Text>(StringComparer.Ordinal);
        private UiOverlay overlay;
        private int archiveArtifactIndex;
        private UiScreen currentScreen = UiScreen.Landing;
        private string pendingFocusKey;
        private string selectedNodeId;
        private bool pageDirty = true;
        private bool animateNextRebuild = true;
        public int FullRebuildCount { get; private set; }
        public int PartialRefreshCount { get; private set; }

        public void Initialize(IRogueliteUiHost source)
        {
            bootstrap = source;
            bootstrap.UiVisualEvents.Published += OnVisualEvent;
            bootstrap.UiPresentationVersions.Changed += OnPresentationChanged;
            EnsureUi();
        }

        private void OnVisualEvent(UiVisualEvent visualEvent)
        {
            if (visualEvent.Kind != UiVisualEventKind.ResourceChanged) return;
            resourceDeltas[visualEvent.Subject] = visualEvent.Delta;
            RefreshMapResources();
        }

        private void OnPresentationChanged(UiPresentationChange change)
        {
            if (change.Area == UiPresentationArea.MapResources)
            {
                RefreshMapResources();
                return;
            }
            if (change.Area == UiPresentationArea.Flow || change.Area == UiPresentationArea.MapStructure || change.Area == UiPresentationArea.Settings)
                Invalidate();
        }

        private void Update()
        {
            if (bootstrap == null || root == null) return;
            bool visible = bootstrap.CurrentFlowPhase == CombatFlowPhase.DeveloperMenu || bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;
            UiScreen nextScreen = bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing ? UiScreen.Briefing : bootstrap.IsMapMenuOpen && bootstrap.CurrentMapRun != null ? UiScreen.Map : UiScreen.Landing;
            if (nextScreen != currentScreen)
            {
                currentScreen = nextScreen;
                navigation.Navigate(nextScreen, DefaultFocusKey(nextScreen));
                pendingFocusKey = navigation.DefaultFocusKey;
                Invalidate();
            }
            if (!bootstrap.IsInteractionModalOpen && RuntimeUiEventSystem.CancelPressedThisFrame()) HandleBack();
            if (!pageDirty) return;
            pageDirty = false;
            Rebuild();
        }

        private void EnsureUi()
        {
            if (root != null) return;
            canvas = FormalUiKit.CanvasRoot("正式肉鸽UI", UiLayoutContract.RogueliteSortingOrder);
            root = canvas.gameObject;
            tooltip = root.AddComponent<FormalHoverTooltip>();
            tooltip.Initialize(canvas);
        }

        private void Rebuild()
        {
            if (content != null) { content.transform.DOKill(); Destroy(content); }
            focusTargets.Clear();
            resourceValues.Clear();
            FullRebuildCount++;
            content = Create("内容", root.transform);
            RectTransform rect = content.AddComponent<RectTransform>();
            Stretch(rect);
            Image background = content.AddComponent<Image>();
            string backdropId = overlay == UiOverlay.Settings ? "settings" : overlay == UiOverlay.Archive ? "archive" :
                bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing ? "briefing" : bootstrap.IsMapMenuOpen && bootstrap.CurrentMapRun != null ? "map" : "landing";
            FormalUiEffects.ApplyBackdrop(background, backdropId);
            FormalUiEffects.AddAmbientScanlines(content.transform, bootstrap.UiPreferences.AnimationIntensity);
            if (overlay == UiOverlay.Settings) DrawSettings();
            else if (overlay == UiOverlay.Archive) DrawArchive();
            else if (bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing) DrawBriefing();
            else if (bootstrap.IsMapMenuOpen && bootstrap.CurrentMapRun != null) DrawMap();
            else DrawLanding();
            bool animate = animateNextRebuild;
            animateNextRebuild = true;
            AnimatePage(rect, animate);
            RestoreFocus();
            if (resourceDeltas.Count > 0)
                DOVirtual.DelayedCall(1.1f, () => { resourceDeltas.Clear(); Invalidate(); }, true).SetTarget(this);
        }

        private void DrawLanding()
        {
            Header("肉鸽行动", string.Empty);
            GameObject card = FormalUiKit.LayoutPanel("入口卡", content.transform, "landing.card", panel);
            Label("标题", "选择出发方案", card.transform, new Vector2(54, -46), new Vector2(860, 52), 38, text, TextAnchor.MiddleLeft);
            Text description = Label("说明", "自由探索；风险与奖励会在行动前显示。", card.transform, new Vector2(56, -120), new Vector2(928, 42), 20, muted, TextAnchor.UpperLeft);
            FormalUiKit.PreventAutomaticWrapping(description);
            ActionButton("近战热压", "战锤 · M01/M02", card.transform, new Vector2(56, -236), new Vector2(210, 92), cyan, true, () => bootstrap.RequestStartMapRoguelite(false, FireRogueliteStarterCatalog.Melee), iconPath: FormalArtRegistry.ItemPath("war_hammer"));
            ActionButton("武器热载", "步枪 · U01/U02", card.transform, new Vector2(276, -236), new Vector2(210, 92), cyan, true, () => bootstrap.RequestStartMapRoguelite(false, FireRogueliteStarterCatalog.Universal), iconPath: FormalArtRegistry.ItemPath("rifle"));
            ActionButton("远程导能", "手杖 · R01/R03", card.transform, new Vector2(496, -236), new Vector2(210, 92), cyan, true, () => bootstrap.RequestStartMapRoguelite(false, FireRogueliteStarterCatalog.Ranged), iconPath: FormalArtRegistry.ItemPath("wand"));
            ActionButton("继续推进", bootstrap.MapSavePresentation.ContinueDetail, card.transform, new Vector2(716, -236), new Vector2(208, 92), safe, bootstrap.MapSavePresentation.CanContinue, () => bootstrap.RequestStartMapRoguelite(true), iconPath: FormalArtRegistry.NavigationPath("continue"));
            ActionButton("行动档案", string.Empty, card.transform, new Vector2(56, -376), new Vector2(410, 82), amber, true, () => SetOverlay(UiOverlay.Archive), iconPath: FormalArtRegistry.NavigationPath("archive"));
            ActionButton("辅助设置", string.Empty, card.transform, new Vector2(514, -376), new Vector2(410, 82), amber, true, () => SetOverlay(UiOverlay.Settings), iconPath: FormalArtRegistry.NavigationPath("settings"));
        }

        private void DrawMap()
        {
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            if (string.IsNullOrEmpty(selectedNodeId) || !RogueliteMapCatalog.Nodes.Any(node => node.Id == selectedNodeId)) selectedNodeId = run.CurrentNodeId;
            Header("学院实训网络", FireRogueliteStarterCatalog.DisplayName(run.StarterId) + " · 探索 " + run.AcademyProgress + " 节点 · " + AcademyPhaseLabel(run.AcademyPhase) + " · 生命 " + run.CurrentHealth + " · 护盾 " + run.CurrentShield + " · 以太 " + run.CurrentMana + " · " + RogueliteMapCatalog.Node(run.CurrentNodeId).DisplayName);
            GameObject status = FormalUiKit.LayoutPanel("行动状态栏", content.transform, "map.status", panel);
            ResourceChip(status.transform, 18, "等级", run.Level, cyan);
            ResourceChip(status.transform, 185, "经验", run.Experience, cyan);
            ResourceChip(status.transform, 352, "零件", run.Parts, amber);
            ResourceChip(status.transform, 519, "以太", run.Aether, cyan);
            ResourceChip(status.transform, 686, "补给", run.Supplies, safe);
            ResourceChip(status.transform, 853, "侦测", run.ScoutingBeacons, muted);
            ResourceChip(status.transform, 1020, "权限卡", run.AccessCards, danger);
            ActionButton("入口", bootstrap.MapSavePresentation.ReturnDetail, content.transform, new Vector2(1236, -82), new Vector2(184, 60), safe, true, bootstrap.RequestReturnToLanding, iconPath: FormalArtRegistry.NavigationPath("home"));
            ActionButton("档案", string.Empty, content.transform, new Vector2(1440, -82), new Vector2(184, 60), amber, true, () => SetOverlay(UiOverlay.Archive), iconPath: FormalArtRegistry.NavigationPath("archive"));
            ActionButton("设置", string.Empty, content.transform, new Vector2(1644, -82), new Vector2(248, 60), amber, true, () => SetOverlay(UiOverlay.Settings), iconPath: FormalArtRegistry.NavigationPath("settings"));

            GameObject mapPanel = FormalUiKit.LayoutPanel("节点地图", content.transform, "map.board", FormalUiTheme.Surface);
            DrawConnections(mapPanel.transform, run);
            foreach (RogueliteMapNode node in RogueliteMapCatalog.Nodes) DrawNode(mapPanel.transform, run, node);
            Label("图例", "现 当前    可 前往    清 安全    锁 权限不足    ? 未知", mapPanel.transform, new Vector2(28, -802), new Vector2(1320, 28), 16, muted, TextAnchor.MiddleLeft);

            GameObject detail = FormalUiKit.LayoutPanel("节点详情", content.transform, "map.detail", panel);
            DrawNodeDetail(detail.transform, run, RogueliteMapCatalog.Node(selectedNodeId));
        }

        private void ResourceChip(Transform parent, float x, string label, int value, Color accent)
        {
            GameObject chip = Panel("资源_" + label, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -10), new Vector2(150, 40), FormalUiTheme.Surface);
            Line(chip.transform, Vector2.zero, new Vector2(3, 40), accent);
            Label("名称", label, chip.transform, new Vector2(12, -4), new Vector2(82, 32), 15, muted, TextAnchor.MiddleLeft);
            bool changed = resourceDeltas.TryGetValue(label, out int delta);
            string valueText = changed ? value + " " + (delta > 0 ? "+" : string.Empty) + delta : value.ToString();
            Text valueLabel = Label("数值", valueText, chip.transform, new Vector2(changed ? 72 : 94, -4), new Vector2(changed ? 66 : 44, 32), 19, changed ? accent : text, TextAnchor.MiddleRight);
            resourceValues[label] = valueLabel;
            if (changed) FormalUiKit.ApplySkin(chip.GetComponent<Image>(), "reward", Color.white);
        }

        private void RefreshMapResources()
        {
            RogueliteMapRun run = bootstrap == null ? null : bootstrap.CurrentMapRun;
            if (run == null || currentScreen != UiScreen.Map || resourceValues.Count == 0) return;
            RogueliteMapPresentationModel model = RogueliteMapPresentationModel.From(run);
            SetResourceValue("等级", model.Level);
            SetResourceValue("经验", model.Experience);
            SetResourceValue("零件", model.Parts);
            SetResourceValue("以太", model.Aether);
            SetResourceValue("补给", model.Supplies);
            SetResourceValue("侦测", model.Scouting);
            SetResourceValue("权限卡", model.AccessCards);
            PartialRefreshCount++;
            if (resourceDeltas.Count > 0)
                DOVirtual.DelayedCall(1.1f, () => { resourceDeltas.Clear(); RefreshMapResources(); }, true).SetTarget(this);
        }

        private void SetResourceValue(string key, int value)
        {
            if (!resourceValues.TryGetValue(key, out Text label) || label == null) return;
            bool changed = resourceDeltas.TryGetValue(key, out int delta);
            label.text = changed ? value + " " + (delta > 0 ? "+" : string.Empty) + delta : value.ToString();
            label.color = changed ? (key == "零件" ? amber : key == "补给" ? safe : key == "权限卡" ? danger : cyan) : text;
        }

        private void DrawConnections(Transform parent, RogueliteMapRun run)
        {
            var drawn = new HashSet<string>(StringComparer.Ordinal);
            foreach (RogueliteMapNode from in RogueliteMapCatalog.Nodes)
            foreach (string nextId in from.NextIds)
            {
                string key = string.CompareOrdinal(from.Id, nextId) < 0 ? from.Id + "|" + nextId : nextId + "|" + from.Id;
                if (!drawn.Add(key)) continue;
                RogueliteMapNode to = RogueliteMapCatalog.Node(nextId);
                Vector2 a = NodePosition(from); Vector2 b = NodePosition(to);
                RogueliteMapNodeVisualState fromState = run.VisualStateFor(from.Id);
                RogueliteMapNodeVisualState toState = run.VisualStateFor(to.Id);
                RogueliteMapRouteVisualState route = RogueliteMapVisualPresentation.RouteState(fromState, toState);
                Color color = route == RogueliteMapRouteVisualState.Unknown ? FormalUiTheme.WithAlpha(muted, .34f) : route == RogueliteMapRouteVisualState.Available ? FormalUiTheme.WithAlpha(cyan, .92f) :
                    route == RogueliteMapRouteVisualState.Safe ? FormalUiTheme.WithAlpha(safe, .72f) : route == RogueliteMapRouteVisualState.Locked ? FormalUiTheme.WithAlpha(danger, .62f) : FormalUiTheme.WithAlpha(cyan, .54f);
                float thickness = route == RogueliteMapRouteVisualState.Available ? 5f : 3f;
                float midX = (a.x + b.x) * .5f;
                Line(parent, new Vector2(Mathf.Min(a.x, midX), a.y), new Vector2(Mathf.Abs(midX - a.x) + 2, thickness), color);
                Line(parent, new Vector2(Mathf.Min(midX, b.x), b.y), new Vector2(Mathf.Abs(b.x - midX) + 2, thickness), color);
                Line(parent, new Vector2(midX, Mathf.Min(a.y, b.y)), new Vector2(thickness, Mathf.Abs(a.y - b.y) + 2), color);
                Label("路径状态_" + key, RogueliteMapVisualPresentation.RouteGlyph(route), parent,
                    new Vector2(midX - 10f, (a.y + b.y) * .5f + 10f), new Vector2(20f, 20f), 15, color, TextAnchor.MiddleCenter);
            }
        }

        private void DrawNode(Transform parent, RogueliteMapRun run, RogueliteMapNode node)
        {
            RogueliteMapNodeVisualState state = run.VisualStateFor(node.Id);
            bool identified = state != RogueliteMapNodeVisualState.Unknown;
            bool selected = node.Id == selectedNodeId;
            Color accent = state == RogueliteMapNodeVisualState.Current || state == RogueliteMapNodeVisualState.Available ? cyan :
                state == RogueliteMapNodeVisualState.Cleared ? safe : state == RogueliteMapNodeVisualState.Locked ? danger : state == RogueliteMapNodeVisualState.Known || state == RogueliteMapNodeVisualState.Visited ? amber : muted;
            string stateText = RogueliteMapVisualPresentation.StateLabel(state);
            string focusKey = RogueliteMapVisualPresentation.FocusKey(node.Id);
            GameObject buttonObject = ActionButton((selected ? "[" + RogueliteMapVisualPresentation.StateGlyph(state) + "] " : RogueliteMapVisualPresentation.StateGlyph(state) + " ") + (identified ? node.DisplayName : "未知节点"), identified ? TypeLabel(node.Type) + " · " + stateText : "尚未侦测", parent, NodePosition(node), new Vector2(146, 66), accent, true, () => { selectedNodeId = node.Id; pendingFocusKey = focusKey; bootstrap.NotifyMapNodeSelected(node.Id); Invalidate(false); }, focusKey);
            if (identified) AddNodeIcon(buttonObject.transform, node.Type);
            buttonObject.GetComponent<UiButtonFeedback>()?.SetSelectedState(selected);
            if (selected && buttonObject.transform.Find("像素焦点框") == null) FormalUiKit.FocusFrame(buttonObject.transform);
        }

        private static void AddNodeIcon(Transform parent, RogueliteMapNodeType type)
        {
            string runtimeId = type.ToString().ToLowerInvariant();
            Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.NodeTypePath(runtimeId));
            if (sprite == null) throw new KeyNotFoundException("Missing formal node icon: " + runtimeId);
            GameObject iconObject = Create("节点类型图标_" + runtimeId, parent);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1); iconRect.pivot = new Vector2(0, 1);
            iconRect.anchoredPosition = new Vector2(10, -17); iconRect.sizeDelta = new Vector2(32, 32);
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
            foreach (string labelName in new[] { "名称", "详情" })
            {
                RectTransform label = parent.Find(labelName)?.GetComponent<RectTransform>();
                if (label == null) continue;
                label.anchoredPosition = new Vector2(48, label.anchoredPosition.y);
                label.sizeDelta = new Vector2(Mathf.Max(40, label.sizeDelta.x - 32), label.sizeDelta.y);
            }
        }

        private void DrawNodeDetail(Transform parent, RogueliteMapRun run, RogueliteMapNode node)
        {
            RogueliteMapNodeVisualState visual = run.VisualStateFor(node.Id);
            bool identified = visual != RogueliteMapNodeVisualState.Unknown;
            Label("类型", identified ? TypeLabel(node.Type).ToUpperInvariant() : "未知节点", parent, new Vector2(28, -28), new Vector2(390, 26), 17, amber, TextAnchor.MiddleLeft);
            Label("名称", identified ? node.DisplayName : "尚未侦测", parent, new Vector2(28, -62), new Vector2(390, 48), 30, text, TextAnchor.MiddleLeft);
            Label("摘要", (identified ? node.Summary : "抵达相邻节点后公开地点信息。") + "\n" + RogueliteMapVisualPresentation.ConnectionSummary(run, node), parent, new Vector2(28, -126), new Vector2(390, 74), 19, muted, TextAnchor.UpperLeft);
            bool current = node.Id == run.CurrentNodeId;
            bool cleared = run.CompletedNodes.Contains(node.Id);
            string stateText = RogueliteMapVisualPresentation.RestrictionText(run, node);
            Color stateColor = visual == RogueliteMapNodeVisualState.Locked ? danger : visual == RogueliteMapNodeVisualState.Unknown ? muted : cleared ? safe : cyan;
            Label("状态", RogueliteMapVisualPresentation.StateGlyph(visual) + " " + stateText, parent, new Vector2(28, -214), new Vector2(390, 34), 19, stateColor, TextAnchor.MiddleLeft);
            if (!identified) return;

            if (current && !cleared && !node.IsCombat && node.Type != RogueliteMapNodeType.Start)
            {
                IReadOnlyList<RogueliteNodeContentChoice> choices = run.CurrentContentChoices;
                Label("选择标题", "公开选项（确认后结算）", parent, new Vector2(28, -266), new Vector2(390, 30), 19, text, TextAnchor.MiddleLeft);
                for (int i = 0; i < choices.Count; i++)
                {
                    RogueliteNodeContentChoice choice = choices[i];
                    UiOperationAvailability availability = RogueliteEconomyPresentation.ForNodeChoice(run, choice);
                    string choiceDetail = AvailabilityText(availability) + "\n" + choice.Preview;
                    ActionButton(choice.DisplayName, choiceDetail, parent, new Vector2(28, -310 - i * 116), new Vector2(390, 98), amber, availability.CanExecute, () => bootstrap.ChooseMapNodeContent(choice.Id));
                }
                return;
            }

            if (current && node.Type == RogueliteMapNodeType.Workshop && cleared)
            {
                DrawWorkshop(parent, run);
                return;
            }

            bool resumeCurrentCombat = current && RogueliteUiPreferences.CanOpenCombatBriefing(run, node);
            bool canTravel = resumeCurrentCombat || RogueliteUiPreferences.CanTravelTo(run, node);
            string action = cleared ? "安全回访" : resumeCurrentCombat ? "恢复战前简报" : node.IsCombat ? "进入战前简报" : "前往节点";
            string detail = canTravel ? (cleared ? "不会重新触发战斗或节点收益" : "移动后仍可沿连接自由返回") : "选择一个相邻且权限满足的节点";
            if (!canTravel) detail = RogueliteMapVisualPresentation.RestrictionText(run, node);
            ActionButton(action, detail, parent, new Vector2(28, -330), new Vector2(390, 88), canTravel ? cyan : muted, canTravel, () => bootstrap.SelectMapNode(node.Id));
            if (current && node.Type == RogueliteMapNodeType.Start)
                Label("入口提示", "选择地图上的相邻节点查看完整预览，再确认前往。", parent, new Vector2(28, -450), new Vector2(390, 60), 18, muted, TextAnchor.UpperLeft);
        }

        private void DrawWorkshop(Transform parent, RogueliteMapRun run)
        {
            Label("工坊", "装备与校准", parent, new Vector2(28, -270), new Vector2(390, 30), 20, text, TextAnchor.MiddleLeft);
            RogueliteReward[] owned = run.ClaimedRewards.Select(id => RogueliteMapCatalog.Rewards.First(item => item.Id == id)).Where(reward => reward.Kind != RogueliteRewardKind.Item).Take(2).ToArray();
            for (int i = 0; i < owned.Length; i++)
            {
                RogueliteReward reward = owned[i];
                UiOperationAvailability availability = RogueliteEconomyPresentation.ForEquipment(run, reward);
                ActionButton((availability.Status == "已装备" ? "已装备 " : "装备 ") + reward.DisplayName, AvailabilityText(availability), parent, new Vector2(28, -312 - i * 80), new Vector2(390, 66), cyan, availability.CanExecute, () => bootstrap.EquipMapReward(reward.Id));
            }
            float y = -326 - owned.Length * 80;
            if (run.OwnedFireSpellIds.Count > 0)
            {
                for (int slot = 0; slot < run.EquippedFireSpellIds.Count; slot++)
                {
                    int capturedSlot = slot;
                    string equipped = run.EquippedFireSpellIds[slot];
                    string display = string.IsNullOrEmpty(equipped) ? "空" : FireSpellCatalog.Get(equipped).DisplayName;
                    ActionButton("术式槽 " + (slot + 1) + "：" + display, "切换已获得术式", parent,
                        new Vector2(28, y - slot * 72), new Vector2(390, 60), amber, true,
                        () => bootstrap.EquipNextMapFireSpell(capturedSlot));
                }
                y -= run.EquippedFireSpellIds.Count * 72;
            }
            string calibrationDetail = run.IsAetherCalibrated ? "当前阶段不可执行：本局已完成校准" : run.Aether < 2 ? "以太不足：需要 2 以太" : "消耗 2 以太；后续战斗 +1 护甲";
            ActionButton(run.IsAetherCalibrated ? "以太校准：已完成" : "以太校准", calibrationDetail, parent, new Vector2(28, y), new Vector2(390, 72), amber, !run.IsAetherCalibrated && run.Aether >= 2, bootstrap.CalibrateMapAether);
        }

        private void DrawBriefing()
        {
            MissionPreparation preparation = bootstrap.CurrentPreparation;
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            string missionName = run == null ? preparation?.MissionId ?? "未知任务" : RogueliteMapCatalog.Node(run.CurrentNodeId).DisplayName;
            Header("战前简报", missionName);
            GameObject card = FormalUiKit.LayoutPanel("简报卡", content.transform, "briefing.card", panel);
            Label("任务", missionName, card.transform, new Vector2(56, -52), new Vector2(1008, 48), 34, text, TextAnchor.MiddleLeft);
            Label("目标标题", "行动目标", card.transform, new Vector2(56, -138), new Vector2(240, 28), 18, cyan, TextAnchor.MiddleLeft);
            Label("目标", preparation?.RulesSummary ?? "无", card.transform, new Vector2(56, -174), new Vector2(1008, 66), 23, text, TextAnchor.UpperLeft);
            Label("敌情标题", "敌方编成", card.transform, new Vector2(56, -270), new Vector2(240, 28), 18, amber, TextAnchor.MiddleLeft);
            Label("敌情", preparation?.EnemySummary ?? "无", card.transform, new Vector2(56, -306), new Vector2(1008, 88), 21, text, TextAnchor.UpperLeft);
            Label("规则", "行动结果固定；没有倒计时。", card.transform, new Vector2(56, -424), new Vector2(1008, 40), 18, muted, TextAnchor.MiddleLeft);
            ActionButton("开始战斗", string.Empty, card.transform, new Vector2(56, -520), new Vector2(480, 88), cyan, true, bootstrap.StartDeveloperCombat, iconPath: FormalArtRegistry.NavigationPath("confirm"));
            ActionButton("返回地图", string.Empty, card.transform, new Vector2(584, -520), new Vector2(480, 88), amber, bootstrap.CurrentMapRun != null, bootstrap.ReturnToMapRun, iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawSettings()
        {
            Header("辅助设置", "即时生效");
            RogueliteUiPreferences p = bootstrap.UiPreferences;
            GameObject card = FormalUiKit.LayoutPanel("设置卡", content.transform, "settings.card", panel);
            Label("标题", "显示、动效与输入提示", card.transform, new Vector2(48, -38), new Vector2(920, 46), 32, text, TextAnchor.MiddleLeft);
            SettingRow(card.transform, 0, "主音量", Mathf.RoundToInt(p.MasterVolume * 100) + "%", "0 / 25 / 50 / 75 / 100", cyan, () => ChangeSettings(volume: Step(p.MasterVolume)));
            SettingRow(card.transform, 1, "动画强度", Mathf.RoundToInt(p.AnimationIntensity * 100) + "%", "0 / 25 / 50 / 75 / 100", cyan, () => ChangeSettings(animation: Step(p.AnimationIntensity)));
            SettingRow(card.transform, 2, "屏幕震动", OnOff(p.ScreenShake), string.Empty, p.ScreenShake ? safe : muted, () => ChangeSettings(screenShake: !p.ScreenShake));
            SettingRow(card.transform, 3, "战斗浮字", OnOff(p.FloatingText), string.Empty, p.FloatingText ? safe : muted, () => ChangeSettings(floatingText: !p.FloatingText));
            SettingRow(card.transform, 4, "高对比色彩", OnOff(p.HighContrast), string.Empty, p.HighContrast ? Color.white : muted, () => ChangeSettings(highContrast: !p.HighContrast));
            SettingRow(card.transform, 5, "大号文字", OnOff(p.LargeText), string.Empty, p.LargeText ? safe : muted, () => ChangeSettings(largeText: !p.LargeText));
            SettingRow(card.transform, 6, "键位提示", OnOff(p.KeyHints), string.Empty, p.KeyHints ? safe : muted, () => ChangeSettings(keyHints: !p.KeyHints));
            ActionButton("返回", bootstrap.SettingsSaveDetail, card.transform, new Vector2(48, -660), new Vector2(944, 62), cyan, true, () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void SettingRow(Transform parent, int index, string name, string value, string detail, Color accent, Action action)
        {
            float y = -104 - index * 76;
            Label("设置_" + name, name, parent, new Vector2(48, y), new Vector2(520, 58), 20, text, TextAnchor.MiddleLeft);
            ActionButton(value, detail, parent, new Vector2(620, y), new Vector2(372, 58), accent, true, action, "按钮_设置_" + index);
        }

        private void DrawArchive()
        {
            Header("行动档案", string.Empty);
            RogueliteMapRun run = bootstrap.ArchivedMapRun;
            GameObject card = FormalUiKit.LayoutPanel("档案卡", content.transform, "archive.card", panel);
            Label("标题", run == null ? "暂无行动记录" : "首区行动 · 种子 " + run.Seed, card.transform, new Vector2(48, -42), new Vector2(940, 48), 32, text, TextAnchor.MiddleLeft);
            if (run != null)
            {
                RogueliteMapNode current = RogueliteMapCatalog.Node(run.CurrentNodeId);
                Label("进度标题", "推进概况", card.transform, new Vector2(48, -98), new Vector2(940, 26), 18, cyan, TextAnchor.MiddleLeft);
                ArchiveMetric(card.transform, new Vector2(48, -132), new Vector2(440, 64), "当前位置", current.DisplayName, cyan);
                ArchiveMetric(card.transform, new Vector2(508, -132), new Vector2(220, 64), "已访问", run.VisitedNodes.Count + " / " + RogueliteMapCatalog.Nodes.Count, safe);
                ArchiveMetric(card.transform, new Vector2(748, -132), new Vector2(240, 64), "已完成", run.CompletedNodes.Count.ToString(), safe);
                ArchiveMetric(card.transform, new Vector2(48, -212), new Vector2(216, 56), "等级", run.Level.ToString(), cyan);
                ArchiveMetric(card.transform, new Vector2(284, -212), new Vector2(216, 56), "经验", run.Experience.ToString(), cyan);
                ArchiveMetric(card.transform, new Vector2(520, -212), new Vector2(216, 56), "零件", run.Parts.ToString(), amber);
                ArchiveMetric(card.transform, new Vector2(756, -212), new Vector2(216, 56), "以太", run.Aether.ToString(), cyan);
                ArchiveMetric(card.transform, new Vector2(48, -284), new Vector2(216, 56), "补给", run.Supplies.ToString(), safe);
                ArchiveMetric(card.transform, new Vector2(284, -284), new Vector2(216, 56), "侦测", run.ScoutingBeacons.ToString(), muted);
                ArchiveMetric(card.transform, new Vector2(520, -284), new Vector2(216, 56), "权限卡", run.AccessCards.ToString(), danger);
                ArchiveMetric(card.transform, new Vector2(756, -284), new Vector2(216, 56), "以太校准", run.IsAetherCalibrated ? "已完成" : "未完成", run.IsAetherCalibrated ? safe : muted);
                Label("构筑标题", FireRogueliteStarterCatalog.DisplayName(run.StarterId) + " · 生命 " + run.CurrentHealth + " · 护盾 " + run.CurrentShield + " · 以太 " + run.CurrentMana, card.transform, new Vector2(48, -360), new Vector2(940, 26), 18, cyan, TextAnchor.MiddleLeft);
                ArchiveMetric(card.transform, new Vector2(48, -398), new Vector2(292, 72), "主手武器", RewardDisplayName(run.EquippedWeaponId, "制式步枪"), cyan);
                ArchiveMetric(card.transform, new Vector2(356, -398), new Vector2(292, 72), "个人术式 1", FireSpellDisplayName(run.EquippedFireSpellIds[0]), amber);
                ArchiveMetric(card.transform, new Vector2(664, -398), new Vector2(308, 72), "个人术式 2", FireSpellDisplayName(run.EquippedFireSpellIds[1]), amber);
                string ownedFire = run.OwnedFireSpellIds.Count == 0 ? "无" : string.Join("、", run.OwnedFireSpellIds.Select(FireSpellDisplayName));
                Label("火术档案", "个人术式 " + run.OwnedFireSpellIds.Count + "/" + FireSpellCatalog.All.Count + " · " + ownedFire, card.transform, new Vector2(48, -478), new Vector2(924, 40), 16, amber, TextAnchor.UpperLeft);
                string migration = run.PendingFireSpellReselections.Count == 0 && run.FireSpellRetirementCompensations.Count == 0 && run.FireSpellMigrationWarnings.Count == 0
                    ? "v0.2 迁移：无待处理项"
                    : "v0.2 迁移：待重选 " + run.PendingFireSpellReselections.Count + "  /  退役补偿 " + run.FireSpellRetirementCompensations.Count + "  /  隔离异常 " + run.FireSpellMigrationWarnings.Count;
                Label("火术迁移", migration, card.transform, new Vector2(48, -516), new Vector2(924, 34), 15,
                    run.FireSpellMigrationWarnings.Count > 0 ? danger : muted, TextAnchor.UpperLeft);
                ItemInstance[] artifacts = run.Inventory.Items.Where(item => ItemCatalog.Get(item.DefinitionId).Category == ItemCategory.Artifact).ToArray();
                if (artifacts.Length == 0)
                {
                    Label("法宝档案", "本次行动尚未获得法宝", card.transform, new Vector2(48, -552), new Vector2(924, 72), 16, muted, TextAnchor.UpperLeft);
                }
                else
                {
                    archiveArtifactIndex = ((archiveArtifactIndex % artifacts.Length) + artifacts.Length) % artifacts.Length;
                    ItemInstance instance = artifacts[archiveArtifactIndex]; ArtifactDefinition artifact = ArtifactCatalog.Get(instance.DefinitionId);
                    Label("法宝档案", (archiveArtifactIndex + 1) + "/" + artifacts.Length + " · " + artifact.DisplayName + " · 次数 " + instance.RemainingUses + "/" + artifact.MaximumUses + " · 来源 " + artifact.Provenance,
                        card.transform, new Vector2(48, -548), new Vector2(924, 30), 16, amber, TextAnchor.UpperLeft);
                    FormalUiKit.SemanticChip("action", artifact.ActionPointCost.ToString(), card.transform, new Vector2(48, -580), tooltip, 22, 14, cyan);
                    string perUseCost = artifact.PublicCost.Replace(artifact.ActionPointCost + " AP，", string.Empty).Replace("消耗 ", string.Empty);
                    Label("法宝详情", "每次 " + perUseCost + " · " + artifact.EffectSummary + " · 目标：" + artifact.TargetSummary,
                        card.transform, new Vector2(108, -580), new Vector2(864, 28), 13, text, TextAnchor.UpperLeft);
                    FormalUiKit.SemanticChip("notice", string.Empty, card.transform, new Vector2(48, -610), tooltip, 22, 14, amber);
                    Label("法宝注意", artifact.RiskSummary, card.transform, new Vector2(80, -608), new Vector2(892, 26), 13, amber, TextAnchor.UpperLeft);
                }
                ActionButton("下一件法宝", artifacts.Length > 1 ? string.Empty : "仅有一件", card.transform, new Vector2(48, -638), new Vector2(452, 48), amber, artifacts.Length > 1, () => { archiveArtifactIndex++; Invalidate(false); }, iconPath: FormalArtRegistry.NavigationPath("continue"));
            }
            ActionButton("返回", string.Empty, card.transform, new Vector2(520, -638), new Vector2(472, 48), cyan, true, () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void ArchiveMetric(Transform parent, Vector2 position, Vector2 size, string label, string value, Color accent)
        {
            GameObject metric = Panel("档案_" + label, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, FormalUiTheme.Surface);
            Line(metric.transform, Vector2.zero, new Vector2(3, size.y), accent);
            Label("标签", label, metric.transform, new Vector2(14, -5), new Vector2(size.x - 28, 22), 14, muted, TextAnchor.MiddleLeft);
            Label("值", value, metric.transform, new Vector2(14, -27), new Vector2(size.x - 28, size.y - 30), 19, text, TextAnchor.UpperLeft);
        }

        private static string FireSpellDisplayName(string spellId)
        {
            if (string.IsNullOrEmpty(spellId)) return "未装备";
            FireSpellDefinition spell = FireSpellCatalog.All.FirstOrDefault(candidate => candidate.Id == spellId);
            return spell == null ? spellId : spell.DisplayName;
        }

        private static string RewardDisplayName(string rewardId, string fallback)
        {
            if (string.IsNullOrEmpty(rewardId)) return fallback;
            RogueliteReward reward = RogueliteMapCatalog.Rewards.FirstOrDefault(item => item.Id == rewardId);
            return reward == null ? fallback : reward.DisplayName;
        }

        private void ChangeSettings(float? volume = null, float? animation = null, bool? screenShake = null, bool? floatingText = null, bool? highContrast = null, bool? largeText = null, bool? keyHints = null)
        {
            RogueliteUiPreferences p = bootstrap.UiPreferences;
            bootstrap.UpdateUiPreferences(volume ?? p.MasterVolume, animation ?? p.AnimationIntensity, screenShake ?? p.ScreenShake,
                floatingText ?? p.FloatingText, highContrast ?? p.HighContrast, largeText ?? p.LargeText, keyHints ?? p.KeyHints);
            Invalidate();
        }

        private static float Step(float value) => value >= .99f ? 0f : Mathf.Min(1f, value + .25f);
        private static string OnOff(bool value) => value ? "开启" : "关闭";
        private void SetOverlay(UiOverlay value)
        {
            if (value == UiOverlay.None)
            {
                pendingFocusKey = navigation.CloseOverlay();
                overlay = UiOverlay.None;
            }
            else
            {
                GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
                navigation.OpenOverlay(value, selected == null ? string.Empty : selected.name);
                overlay = value;
                pendingFocusKey = value == UiOverlay.Settings ? "按钮_设置_0" : "按钮_返回";
            }
            Invalidate();
        }
        private void Invalidate(bool animate = true) { pageDirty = true; animateNextRebuild &= animate; }

        private void HandleBack()
        {
            UiBackAction action = navigation.ResolveBack();
            if (action == UiBackAction.CloseOverlay) SetOverlay(UiOverlay.None);
            else if (action == UiBackAction.NavigateMap) bootstrap.ReturnToMapRun();
            else if (action == UiBackAction.NavigateLanding) bootstrap.RequestReturnToLanding();
        }

        private string DefaultFocusKey(UiScreen screen)
        {
            return screen == UiScreen.Map ? RogueliteMapVisualPresentation.FocusKey(bootstrap?.CurrentMapRun?.CurrentNodeId) : screen == UiScreen.Briefing ? "按钮_开始战斗" : "按钮_近战热压";
        }

        private void RestoreFocus()
        {
            string key = string.IsNullOrEmpty(pendingFocusKey) ? navigation.DefaultFocusKey : pendingFocusKey;
            pendingFocusKey = null;
            if (!string.IsNullOrEmpty(key) && focusTargets.TryGetValue(key, out GameObject target) && target != null && target.GetComponent<Button>()?.interactable == true)
            {
                RuntimeUiEventSystem.Select(target);
                return;
            }
            GameObject first = focusTargets.Values.FirstOrDefault(item => item != null && item.GetComponent<Button>()?.interactable == true);
            if (first != null) RuntimeUiEventSystem.Select(first);
        }

        private void AnimatePage(RectTransform rect, bool animate)
        {
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity);
            CanvasGroup group = content.AddComponent<CanvasGroup>();
            if (!animate || motion.IsImmediate) { group.alpha = 1f; rect.anchoredPosition = Vector2.zero; return; }
            Vector2 end = rect.anchoredPosition;
            rect.anchoredPosition = end + new Vector2(motion.PageOffset, 0f);
            group.alpha = 0f;
            DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.StandardDuration))
                .Join(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, end, motion.StandardDuration).SetEase(FormalUiMotionTokens.StandardEase));
        }

        private void OnDestroy()
        {
            if (bootstrap != null) bootstrap.UiVisualEvents.Published -= OnVisualEvent;
            if (bootstrap != null) bootstrap.UiPresentationVersions.Changed -= OnPresentationChanged;
            DOTween.Kill(this);
            if (content != null) content.transform.DOKill();
        }

        private void Header(string title, string subtitle)
        {
            GameObject header = FormalUiKit.LayoutPanel("页眉", content.transform, "global.header", FormalUiTheme.SurfaceRaised);
            Label("标题", title, header.transform, new Vector2(20, -8), new Vector2(string.IsNullOrEmpty(subtitle) ? 1810 : 800, 38), 23, text, TextAnchor.MiddleLeft);
            if (!string.IsNullOrEmpty(subtitle)) Label("副标题", subtitle, header.transform, new Vector2(850, -8), new Vector2(980, 38), 17, muted, TextAnchor.MiddleRight);
            Line(header.transform, new Vector2(18, -53), new Vector2(1836, 2), cyan);
        }

        private GameObject ActionButton(string title, string detail, Transform parent, Vector2 position, Vector2 size, Color accent, bool interactable, Action action, string focusKey = null, string iconPath = null)
        {
            GameObject result = Panel(string.IsNullOrEmpty(focusKey) ? "按钮_" + title : focusKey, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, interactable ? FormalUiTheme.Interactive : FormalUiTheme.Disabled);
            Image image = result.GetComponent<Image>();
            Button button = result.AddComponent<Button>(); button.targetGraphic = image; button.interactable = interactable;
            if (action != null) button.onClick.AddListener(() => action());
            Line(result.transform, new Vector2(0, 0), new Vector2(4, size.y), interactable ? accent : muted);
            int titleSize = FormalUiTheme.ButtonFontSize;
            bool hasDetail = !string.IsNullOrWhiteSpace(detail);
            float lineHeight = Mathf.Max(32f, Mathf.Floor(size.y * .36f * .5f) * 2f);
            float detailY = -Mathf.Round(size.y * .25f) * 2f;
            float titleY = hasDetail ? -8 : -Mathf.Max(0, (size.y - lineHeight) * .5f);
            FormalUiKit.PreventAutomaticWrapping(Label("名称", title, result.transform, new Vector2(16, titleY), new Vector2(size.x - 28, lineHeight), titleSize, interactable ? text : muted, TextAnchor.MiddleLeft));
            if (hasDetail) FormalUiKit.PreventAutomaticWrapping(Label("详情", detail, result.transform, new Vector2(16, detailY), new Vector2(size.x - 28, lineHeight), Math.Max(FormalUiTheme.ButtonDetailFontSize, titleSize - 4), interactable ? accent : muted, TextAnchor.UpperLeft));
            if (!string.IsNullOrEmpty(iconPath)) AddActionIcon(result.transform, iconPath, size.y);
            Color normal = image.color;
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(normal, accent),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback, interactable ? string.Empty : detail);
            if (!focusTargets.ContainsKey(result.name)) focusTargets.Add(result.name, result);
            return result;
        }

        private static void AddActionIcon(Transform parent, string iconPath, float buttonHeight)
        {
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            if (sprite == null) throw new KeyNotFoundException("Missing formal action icon: " + iconPath);
            GameObject iconObject = Create("操作图标", parent);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1); iconRect.pivot = new Vector2(0, 1);
            iconRect.anchoredPosition = new Vector2(14, -Mathf.Max(8, (buttonHeight - 32) * .5f)); iconRect.sizeDelta = new Vector2(32, 32);
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
            foreach (string labelName in new[] { "名称", "详情" })
            {
                RectTransform label = parent.Find(labelName)?.GetComponent<RectTransform>();
                if (label == null) continue;
                label.anchoredPosition = new Vector2(54, label.anchoredPosition.y);
                label.sizeDelta = new Vector2(Mathf.Max(40, label.sizeDelta.x - 38), label.sizeDelta.y);
            }
        }

        private static string AvailabilityText(UiOperationAvailability availability)
        {
            if (string.IsNullOrWhiteSpace(availability.Reason) || availability.Reason == availability.Status) return availability.Status;
            if (string.IsNullOrWhiteSpace(availability.Status)) return availability.Reason;
            return availability.Status + " · " + availability.Reason;
        }

        private GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            return FormalUiKit.AnchoredPanel(name, parent, anchor, pivot, position, size, color);
        }

        private Text Label(string name, string value, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            return FormalUiKit.Label(name, value, parent, position, size, fontSize, color, alignment);
        }

        private static void Line(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            FormalUiKit.Line(parent, position, size, color, "线");
        }

        private static Vector2 NodePosition(RogueliteMapNode node) => new Vector2(34 + node.GridX * 166, 40 - node.GridY * 150);
        private static string AcademyPhaseLabel(AcademyMapPhase phase) => phase == AcademyMapPhase.Consolidation ? "学期收束" : phase == AcademyMapPhase.TransitionReady ? "阶段转换待定" : "正常学期";
        private static string TypeLabel(RogueliteMapNodeType type) => type == RogueliteMapNodeType.Combat ? "战斗" : type == RogueliteMapNodeType.Elite ? "精英" : type == RogueliteMapNodeType.Event ? "事件" : type == RogueliteMapNodeType.Workshop ? "工坊" : type == RogueliteMapNodeType.Shop ? "商店" : type == RogueliteMapNodeType.Rest ? "休整" : type == RogueliteMapNodeType.Treasure ? "库房" : type == RogueliteMapNodeType.Finale ? "核心" : "入口";
        private static GameObject Create(string name, Transform parent) => FormalUiKit.Create(name, parent);
        private static void Stretch(RectTransform rect) => FormalUiKit.Stretch(rect);
    }
}
