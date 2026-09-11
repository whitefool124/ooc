using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using OCC.Combat.Roguelite;

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
        private readonly UiNavigationState navigation = new UiNavigationState(UiScreen.Landing, "按钮_开始新游戏");
        private readonly Dictionary<string, GameObject> focusTargets = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> resourceDeltas = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Text> resourceValues = new Dictionary<string, Text>(StringComparer.Ordinal);
        private UiOverlay overlay;
        private int archiveArtifactIndex;
        private UiScreen currentScreen = UiScreen.Landing;
        private string pendingFocusKey;
        private string selectedNodeId;
        private string selectedRogueInventoryId;
        private string selectedWorkshopEquipmentId;
        private string selectedWorkshopSpellId;
        private LoadoutSection loadoutSection = LoadoutSection.Equipment;
        private int selectedLoadoutSpellIndex;
        private string selectedLoadoutSpellId;
        private const float LoadoutCellSize = 56f;
        private RectTransform loadoutGridRect;
        private RogueEquipmentRuntime loadoutDragRuntime;
        private readonly Dictionary<OCC.Combat.Roguelite.EquipmentSlot, RectTransform> loadoutEquipmentSlotRects = new Dictionary<OCC.Combat.Roguelite.EquipmentSlot, RectTransform>();
        private readonly Dictionary<OCC.Combat.Roguelite.EquipmentSlot, Image> loadoutEquipmentDropOverlays = new Dictionary<OCC.Combat.Roguelite.EquipmentSlot, Image>();
        private string loadoutDragId;
        private OCC.Combat.Roguelite.EquipmentSlot? loadoutDragEquippedSlot;
        private bool loadoutDragRotated;
        private Vector2Int loadoutGrabOffset;
        private Vector2 loadoutLastPointer;
        private GameObject loadoutDragGhost;
        private CanvasGroup loadoutDragSource;
        private RogueMapViewportController mapViewportController;
        private bool frontEndSuppressed;
        private string loadoutInteractionMessage = "左键拖拽物品　拖拽中按 R 键或右键旋转";
        private bool pageDirty = true;
        private bool animateNextRebuild = true;
        public int FullRebuildCount { get; private set; }
        public int PartialRefreshCount { get; private set; }

        private enum LoadoutSection
        {
            Equipment,
            Spells
        }

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
            if (frontEndSuppressed)
            {
                if (root.activeSelf) root.SetActive(false);
                return;
            }
            bool visible = bootstrap.CurrentFlowPhase == CombatFlowPhase.DeveloperMenu || bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;
            if (overlay == UiOverlay.Loadout && !string.IsNullOrEmpty(loadoutDragId) && Keyboard.current?.rKey.wasPressedThisFrame == true)
                RotateLoadoutDragPreview();
            UiScreen nextScreen = bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing ? UiScreen.Briefing : bootstrap.IsMapMenuOpen && bootstrap.CurrentMapRun != null ? UiScreen.Map : UiScreen.Landing;
            if (nextScreen != currentScreen)
            {
                if (overlay == UiOverlay.Loadout) ClearLoadoutDrag();
                overlay = UiOverlay.None;
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
            mapViewportController = null;
            if (content != null) { content.transform.DOKill(); Destroy(content); }
            focusTargets.Clear();
            resourceValues.Clear();
            FullRebuildCount++;
            content = Create("内容", root.transform);
            RectTransform rect = content.AddComponent<RectTransform>();
            Stretch(rect);
            Image background = content.AddComponent<Image>();
            string backdropId = overlay == UiOverlay.Settings ? "settings" : overlay == UiOverlay.Archive ? "archive" : overlay == UiOverlay.Loadout ? "inventory" :
                overlay == UiOverlay.NodeRoom ? "briefing" :
                bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing ? "briefing" : bootstrap.IsMapMenuOpen && bootstrap.CurrentMapRun != null ? "map" : "landing";
            FormalUiEffects.ApplyBackdrop(background, backdropId);
            FormalUiEffects.AddPageDecorations(content.transform, backdropId, bootstrap.UiPreferences.AnimationIntensity);
            if (overlay == UiOverlay.Settings) DrawSettings();
            else if (overlay == UiOverlay.Archive) DrawArchive();
            else if (overlay == UiOverlay.Loadout) DrawLoadout();
            else if (overlay == UiOverlay.NodeRoom) DrawNodeRoom();
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
            Header("以太主界面", "人生档案\n学院　战争　转折");
            GameObject card = FormalUiKit.LayoutPanel("入口卡", content.transform, "landing.card", panel);
            bool hasSave = bootstrap.HasFirstExperienceSave;
            Label("标题", "以太人生档案", card.transform, new Vector2(56, -48), new Vector2(760, 72), 48, text, TextAnchor.MiddleLeft);
            Text description = Label("说明", "从学院出发，记录战争与人生转折。当前版本开放学院阶段。", card.transform, new Vector2(58, -120), new Vector2(750, 40), 24, muted, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(description);
            ActionButton("开始新游戏", "选择存档位并建立新档案", card.transform, new Vector2(56, -190), new Vector2(752, 112), cyan, true, BeginFirstRunJourney,
                iconPath: FormalArtRegistry.NavigationPath("continue"), emphasized: true);
            string continueDetail = hasSave ? "选择已有档案，从上次进度继续" : "尚无可继续的学院档案";
            ActionButton("继续游戏", continueDetail, card.transform, new Vector2(56, -326), new Vector2(752, 112), safe,
                hasSave, ContinueJourney, iconPath: FormalArtRegistry.NavigationPath("continue"));

            GameObject summary = Panel("档案摘要", card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(840, -190), new Vector2(424, 248), FormalUiTheme.SurfaceRaised);
            Label("状态标题", "当前档案状态", summary.transform, new Vector2(24, -20), new Vector2(260, 40), 24, muted, TextAnchor.MiddleLeft);
            Label("状态", hasSave ? "已有可继续档案" : "尚未建立档案", summary.transform, new Vector2(24, -68), new Vector2(300, 48), 32, hasSave ? safe : amber, TextAnchor.MiddleLeft);
            Text statusDetail = Label("状态说明", hasSave ? "学院：可继续\n战争与转折：尚未开放" : "学院：可开始\n战争与转折：尚未开放", summary.transform, new Vector2(24, -126), new Vector2(270, 80), 24, text, TextAnchor.UpperLeft);
            statusDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
            FormalUiEffects.AddEmptyIllustration(summary.transform, "empty_route_case", new Vector2(354, -150), 96f);

            Line(card.transform, new Vector2(56, -500), new Vector2(1208, 2), FormalUiTheme.Rule);
            ActionButton("行程与行囊", string.Empty, card.transform, new Vector2(56, -536), new Vector2(368, 92), amber, true, () => SetOverlay(UiOverlay.Archive), iconPath: FormalArtRegistry.NavigationPath("archive"));
            ActionButton("重看开场 CG", string.Empty, card.transform, new Vector2(448, -536), new Vector2(368, 92), cyan, true, bootstrap.ReplayOpeningCg,
                iconPath: FormalArtRegistry.NavigationPath("continue"));
            ActionButton("辅助设置", string.Empty, card.transform, new Vector2(840, -536), new Vector2(424, 92), amber, true, () => SetOverlay(UiOverlay.Settings), iconPath: FormalArtRegistry.NavigationPath("settings"));
        }

        private void BeginFirstRunJourney()
        {
            bootstrap.OpenFirstExperienceFrontEnd(false);
        }

        private void ContinueJourney()
        {
            bootstrap.OpenFirstExperienceFrontEnd(true);
        }

        public void SetFrontEndSuppressed(bool value)
        {
            frontEndSuppressed = value;
            if (root != null) root.SetActive(!value);
            if (!value) Invalidate(false);
        }

        private void DrawMap()
        {
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            if (string.IsNullOrEmpty(selectedNodeId) || !run.MapNodes.Any(node => node.Id == selectedNodeId)) selectedNodeId = run.CurrentNodeId;
            if (run.IsFirstRunExperience && run.FirstRunExperience.Origin.Acknowledged && run.CurrentNodeId == FirstRunExperienceCatalog.OriginNodeId &&
                (selectedNodeId == FirstRunExperienceCatalog.OriginNodeId || string.IsNullOrEmpty(selectedNodeId)) && run.MapNodes.Any(node => node.Id == "B1"))
                selectedNodeId = "B1";
            Header("学院地图", run.UsesRogue11
                ? new RogueMapStatusPresentation(run).PhaseLabel + "　" + run.MapNode(run.CurrentNodeId).DisplayName
                : FireRogueliteStarterCatalog.DisplayName(run.StarterId) + "　" + RogueliteMapVisualPresentation.AcademyStatus(run));
            GameObject status = FormalUiKit.LayoutPanel("行动状态栏", content.transform, "map.status", panel);
            if (run.UsesRogue11)
            {
                RogueMapStatusPresentation model = new RogueMapStatusPresentation(run);
                MetricChip(status.transform, 12, -16, "生命", model.Health + "/" + model.MaximumHealth, FormalUiTheme.Health, FormalArtRegistry.ResourceMetricPath("health"), 232);
                MetricChip(status.transform, 256, -16, "金币", model.Gold.ToString(), amber, FormalArtRegistry.ResourceMetricPath("gold"), 232);
                MetricChip(status.transform, 500, -16, "学院贡献", model.StageContribution.ToString(), safe, FormalArtRegistry.ResourceMetricPath("contribution"), 232);
                MetricChip(status.transform, 744, -16, "学院时序", model.StageTime + "/" + model.TransitionTime,
                    model.StageTime >= model.WarningTime ? danger : model.StageTime >= model.ConsolidationTime ? amber : cyan,
                    FormalArtRegistry.ResourceMetricPath("stage_time"), 232);
                MetricChip(status.transform, 988, -16, "角色", "维克多", cyan, FormalArtRegistry.NodeTypePath("start"), 244);
                MetricChip(status.transform, 1244, -16, "阶段", "学院", amber, FormalArtRegistry.MapRegionPath("teaching_archive"), 210);
            }
            else
            {
                MetricChip(status.transform, 12, -16, "生命", "—", FormalUiTheme.Health, FormalArtRegistry.ResourceMetricPath("health"), 272);
                MetricChip(status.transform, 296, -16, "零件", run.Parts.ToString(), amber, null, 272);
                MetricChip(status.transform, 580, -16, "以太", run.Aether.ToString(), cyan, null, 272);
                MetricChip(status.transform, 864, -16, "补给", run.Supplies.ToString(), safe, null, 272);
            }
            GameObject loadout = ActionButton("背包", string.Empty, status.transform, new Vector2(1466, -16), new Vector2(184, 60), cyan,
                run.UsesRogue11, () => SetOverlay(UiOverlay.Loadout), iconPath: FormalArtRegistry.EquipmentSlotPath("Backpack"));
            BindHover(loadout, "背包与整备", "更换术式、装备和随身道具。", cyan);
            ActionButton("设置", string.Empty, status.transform, new Vector2(1662, -16), new Vector2(190, 60), amber, true, () => SetOverlay(UiOverlay.Settings), iconPath: FormalArtRegistry.NavigationPath("settings"));

            GameObject mapPanel = FormalUiKit.LayoutPanel("节点地图视口", content.transform, "map.board", FormalUiTheme.Surface);
            mapPanel.AddComponent<RectMask2D>();
            GameObject mapCanvas = Create("学院分区地图画布", mapPanel.transform);
            RectTransform mapCanvasRect = mapCanvas.AddComponent<RectTransform>();
            mapCanvasRect.anchorMin = mapCanvasRect.anchorMax = mapCanvasRect.pivot = new Vector2(.5f, .5f);
            mapCanvasRect.anchoredPosition = Vector2.zero;
            mapCanvasRect.sizeDelta = new Vector2(1872, 874);
            ApplyFormalMapBoard(mapCanvas);
            DrawDistrictLabels(mapCanvas.transform);
            DrawConnections(mapCanvas.transform, run);
            foreach (RogueliteMapNode node in run.MapNodes) DrawNode(mapCanvas.transform, run, node);
        }

        private void DrawFlatMapDistricts(Transform parent)
        {
            string[] ids = { "teaching_archive", "training_workshop", "sealed_tower", "market_infirmary", "courtyard_dormitory", "campus_wilds" };
            Color[] colors =
            {
                Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Cyan, .08f),
                Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Amber, .08f),
                Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Danger, .07f),
                Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Safe, .08f),
                Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Amber, .05f),
                Color.Lerp(FormalUiTheme.SurfaceRaised, FormalUiTheme.Cyan, .05f)
            };
            const float columnWidth = 624f;
            const float upperHeight = 407f;
            const float lowerHeight = 463f;
            for (int index = 0; index < ids.Length; index++)
            {
                bool lower = index >= 3;
                GameObject district = Panel("学院分区_" + ids[index], parent, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(index % 3 * columnWidth, lower ? -411f : 0f),
                    new Vector2(columnWidth - 4f, lower ? lowerHeight : upperHeight), colors[index]);
                district.GetComponent<Image>().raycastTarget = false;
                Line(district.transform, new Vector2(0, -2), new Vector2(columnWidth - 4f, 2), FormalUiTheme.WithAlpha(muted, .32f));
                Text label = Label("分区名称", MapRegionLabel(ids[index]), district.transform, new Vector2(22, -18),
                    new Vector2(360, 36), 21, FormalUiTheme.WithAlpha(text, .72f), TextAnchor.MiddleLeft);
                label.raycastTarget = false;
            }
        }

        private void DrawDistrictLabels(Transform parent)
        {
            string[] ids = { "teaching_archive", "training_workshop", "sealed_tower", "market_infirmary", "courtyard_dormitory", "campus_wilds" };
            foreach (string regionId in ids)
            {
                Vector2 center = AcademyMapVisualLayout.SourceCenterForRegion(regionId);
                Vector2 position = ProjectMapPosition(center) + RegionLabelOffset(regionId);
                GameObject chip = Create("分区标签_" + regionId, parent);
                RectTransform chipRect = chip.AddComponent<RectTransform>();
                chipRect.anchorMin = chipRect.anchorMax = chipRect.pivot = new Vector2(.5f, .5f);
                chipRect.anchoredPosition = position; chipRect.sizeDelta = new Vector2(190, 34);
                Image plate = chip.AddComponent<Image>(); plate.color = FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .82f); plate.raycastTarget = false;
                Text label = Label("名称", MapRegionLabel(regionId), chip.transform, new Vector2(8, -3), new Vector2(174, 28),
                    16, FormalUiTheme.OnInk, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
            }
        }

        private void DrawCompactMapNodeCard(Transform parent, RogueliteMapRun run, RogueliteMapNode node, float viewportWidth)
        {
            RogueliteMapNodeVisualState state = run.VisualStateFor(node.Id);
            bool identified = state != RogueliteMapNodeVisualState.Unknown;
            GameObject card = Panel("节点摘要卡", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(viewportWidth - 496, -722), new Vector2(472, 128), FormalUiTheme.Panel);
            string title = identified ? node.DisplayName : "还看不清";
            string type = identified ? TypeLabel(node.Type) : "未知地点";
            string time = identified && run.UsesRogue11 ? (AcademyMapTuning.TimeCost(node.Type) == 0 ? "不耗时" : "耗时 " + AcademyMapTuning.TimeCost(node.Type)) : string.Empty;
            Label("名称", title, card.transform, new Vector2(18, -14), new Vector2(300, 38), 26, text, TextAnchor.MiddleLeft);
            Label("摘要", type + (string.IsNullOrEmpty(time) ? string.Empty : "　" + time) + "　" + RogueliteMapVisualPresentation.StateLabel(state),
                card.transform, new Vector2(18, -54), new Vector2(320, 28), 16, identified ? cyan : muted, TextAnchor.MiddleLeft);
            bool firstBattleNext = run.IsFirstRunExperience && node.Id == "B1" && run.FirstRunExperience.Origin.Acknowledged && run.CurrentNodeId == FirstRunExperienceCatalog.OriginNodeId;
            Label("提示", identified ? firstBattleNext ? "下一步：查看第一战，再决定是否出发" : "先看看这里，再决定要不要去" : "走近后才能看清",
                card.transform, new Vector2(18, -84), new Vector2(320, 24), 14, muted, TextAnchor.MiddleLeft);
            if (identified) AddNodeIcon(card.transform, node.Type);
            GameObject details = ActionButton(string.Empty, string.Empty, card.transform, new Vector2(346, -30), new Vector2(108, 72), amber, identified, OpenSelectedNodeRoom);
            if (identified) AddForwardArrow(details.transform, amber);
            else FormalUiEffects.AddEmptyIllustration(details.transform, "locked_document_satchel", new Vector2(54, -36), 64f);
            BindHover(details, identified ? "看看这里" : "现在还看不清", identified ? "看看会遇到什么、要花多久、能带回什么。" : "先走到附近，再回来查看。", identified ? amber : muted);

            bool hasOpenRoute = run.MapNodes.Any(candidate => run.VisualStateFor(candidate.Id) == RogueliteMapNodeVisualState.Available);
            if (!hasOpenRoute)
                FormalUiEffects.AddEmptyIllustration(parent, "empty_route_case", new Vector2(86, -748), 64f);
        }

        private void OpenSelectedNodeRoom()
        {
            if (bootstrap?.CurrentMapRun == null || string.IsNullOrEmpty(selectedNodeId)) return;
            if (bootstrap.CurrentMapRun.VisualStateFor(selectedNodeId) == RogueliteMapNodeVisualState.Unknown) return;
            SetOverlay(UiOverlay.NodeRoom);
        }

        private void DrawNodeRoom()
        {
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            if (run == null || string.IsNullOrEmpty(selectedNodeId) || !run.MapNodes.Any(value => value.Id == selectedNodeId))
            {
                SetOverlay(UiOverlay.None);
                return;
            }

            RogueliteMapNode node = run.MapNode(selectedNodeId);
            RogueliteMapNodeVisualState visual = run.VisualStateFor(node.Id);
            if (visual == RogueliteMapNodeVisualState.Unknown)
            {
                SetOverlay(UiOverlay.None);
                return;
            }

            bool current = node.Id == run.CurrentNodeId;
            bool cleared = run.CompletedNodes.Contains(node.Id);
            AcademyEventDefinition nodeEvent = null;
            if (node.Type == RogueliteMapNodeType.Event && run.NodeContentAssignments.TryGetValue(node.Id, out string eventId))
                nodeEvent = AcademyNodeContentCatalog.Event(eventId);
            string displayName = nodeEvent?.DisplayName ?? node.DisplayName;
            Color accent = NodeRoomAccent(node.Type);
            Header(node.IsCombat && !cleared ? "出发准备" : TypeLabel(node.Type), displayName);

            GameObject card = Panel("全屏节点房间", content.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(1760, 820), FormalUiTheme.Panel);
            GameObject identity = Panel("节点身份页", card.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(36, -36), new Vector2(500, 748), FormalUiTheme.SurfaceRaised);
            GameObject decisions = Panel("节点交互页", card.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(560, -36), new Vector2(1164, 748), FormalUiTheme.Surface);

            GameObject iconPlate = Panel("节点类型大图", identity.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(36, -28), new Vector2(160, 160), FormalUiTheme.Interactive);
            Image typeIcon = Icon("节点类型", FormalArtRegistry.NodeTypePath(node.Type.ToString().ToLowerInvariant()), iconPlate.transform,
                new Vector2(16, -16), new Vector2(128, 128));
            typeIcon.color = Color.white;
            Label("类型", TypeLabel(node.Type), identity.transform, new Vector2(220, -48), new Vector2(236, 34), 20, accent, TextAnchor.MiddleLeft);
            Label("状态", RogueliteMapVisualPresentation.StateLabel(visual), identity.transform, new Vector2(220, -88), new Vector2(236, 36), 24,
                cleared ? safe : current ? cyan : accent, TextAnchor.MiddleLeft);
            Label("节点名称", displayName, identity.transform, new Vector2(36, -202), new Vector2(420, 82), 34, text, TextAnchor.UpperLeft);
            Label("地点印象", NodeRoomMaterialCue(node.Type), identity.transform, new Vector2(36, -296), new Vector2(420, 64), 16, muted, TextAnchor.UpperLeft);
            Label("节点说明", nodeEvent == null ? node.Summary : node.Summary,
                identity.transform, new Vector2(36, -374), new Vector2(420, 104), 18, text, TextAnchor.UpperLeft);

            if (run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, node);
                RoomMetric(identity.transform, "难度", string.IsNullOrEmpty(preview.EncounterLabel) ? preview.RiskLabel : preview.EncounterLabel,
                    FormalArtRegistry.ResourceMetricPath("risk"), new Vector2(36, -506), accent);
                RoomMetric(identity.transform, "耗时", preview.IsZeroTime ? "不耗时" : preview.TimeCost.ToString(),
                    FormalArtRegistry.ResourceMetricPath("stage_time"), new Vector2(252, -506), cyan);
                RoomMetric(identity.transform, "回来时", "生命不恢复\n魔力 +" + preview.ExpectedManaRecovery,
                    FormalArtRegistry.ResourceMetricPath("health"), new Vector2(36, -570), safe, 432);
                Label("能带回来", preview.RewardLabel, identity.transform, new Vector2(36, -646), new Vector2(420, 56), 17, amber, TextAnchor.UpperLeft);
            }

            DrawNodeRoomActions(decisions.transform, run, node, current, cleared, accent);
        }

        private void DrawNodeRoomActions(Transform parent, RogueliteMapRun run, RogueliteMapNode node, bool current, bool cleared, Color accent)
        {
            if (run.IsFirstRunExperience && current && node.Id == FirstRunExperienceCatalog.OriginNodeId && !run.FirstRunExperience.Origin.Acknowledged)
            {
                DrawDepartureProgress(parent, new Vector2(44, -22), 336f, 0);
                Label("页面标题", "这是维克多的固定基础配置", parent, new Vector2(44, -106), new Vector2(900, 42), 30, text, TextAnchor.MiddleLeft);
                Label("页面说明", "首局不需要挑流派；这三项会一起带进第一战。", parent, new Vector2(44, -150), new Vector2(900, 32), 17, muted, TextAnchor.MiddleLeft);
                FormalUiEffects.AddChapterMarker(parent, "teaching_chalk_clip", new Vector2(1080, -122), 2f);
                FormalUiEffects.AddChapterDivider(parent, "teaching_record", new Vector2(824, -160), 2f);
                OriginFeatureCard(parent, "学生背景", "公开考核与工读入学", "普通学生，从学院公开课程开始。", FormalArtRegistry.NodeTypePath("start"), new Vector2(44, -202), 336f, cyan);
                OriginFeatureCard(parent, "固定天赋", "就地接线", "首次移动停在掩体旁：护盾 +2、魔力 +1。", FormalArtRegistry.ResourceMetricPath("shield"), new Vector2(400, -202), 336f, safe);
                OriginFeatureCard(parent, "专属术式", "借障导流", "1 行动点 + 1 魔力；掩体旁护盾 +4，下次移动 +2。", FormalArtRegistry.ResourceMetricPath("mana"), new Vector2(756, -202), 336f, amber);
                ActionButton("确认并解锁第一战", "确认后回到地图，第一战会替你选中。", parent, new Vector2(224, -414), new Vector2(720, 118), cyan, true,
                    ConfirmFirstRunOrigin, iconPath: FormalArtRegistry.NavigationPath("confirm"));
                ActionButton("回到地图", PlayerFacingCopy.ReturnToMapFree, parent, new Vector2(224, -616), new Vector2(720, 88), amber, true,
                    () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
                return;
            }
            if (run.IsFirstRunExperience && current && node.Id == "W")
            {
                DrawFirstRunWorkshop(parent, run);
                return;
            }
            if (run.IsFirstRunExperience && current && node.Id == "M")
            {
                DrawFirstRunMedical(parent, run);
                return;
            }
            if (run.IsFirstRunExperience && current && node.Id == FirstRunExperienceCatalog.ShopNodeId)
            {
                DrawFirstRunShop(parent, run);
                return;
            }
            if (node.IsCombat && !cleared)
            {
                DrawCombatActionDossier(parent, run, node, current, accent);
                return;
            }

            Label("页面标题", NodeRoomActionTitle(node, current, cleared), parent, new Vector2(44, -38), new Vector2(1000, 48), 32, text, TextAnchor.MiddleLeft);
            Label("页面说明", NodeRoomInstruction(node, current, cleared), parent, new Vector2(44, -96), new Vector2(720, 64), 18, muted, TextAnchor.UpperLeft);
            FormalUiEffects.AddChapterMarker(parent, FormalUiAssetPlacement.ChapterMarker(node), new Vector2(1080, -56), 2f);
            FormalUiEffects.AddChapterDivider(parent, FormalUiAssetPlacement.ChapterDivider(node), new Vector2(800, -128), 2f);

            if (current && !cleared && !node.IsCombat && node.Type != RogueliteMapNodeType.Start)
            {
                IReadOnlyList<RogueliteNodeContentChoice> choices = run.CurrentContentChoices;
                for (int i = 0; i < choices.Count; i++)
                {
                    RogueliteNodeContentChoice choice = choices[i];
                    UiOperationAvailability availability = RogueliteEconomyPresentation.ForNodeChoice(run, choice);
                    GameObject option = ActionButton(choice.DisplayName, RogueliteEconomyPresentation.NodeChoiceSummary(run, choice, availability), parent,
                        new Vector2(44, -184 - i * 126), new Vector2(1068, 104), accent, availability.CanExecute,
                        () => bootstrap.ChooseMapNodeContent(choice.Id), iconPath: FormalArtRegistry.NavigationPath("confirm"));
                    BindHover(option, choice.DisplayName, choice.Preview + "\n" + AvailabilityText(availability), accent);
                }
            }
            else if (!current)
            {
                bool canEnter = RogueliteUiPreferences.CanTravelTo(run, node);
                string label = cleared ? "再去看看" : "前往这里";
                string reason = canEnter ? "进入前仍可返回；不会消耗学院时间或资源。" : RogueliteMapVisualPresentation.RestrictionText(run, node);
                ActionButton(label, reason, parent, new Vector2(224, -204), new Vector2(720, 126), canEnter ? accent : muted, canEnter,
                    () => bootstrap.SelectMapNode(node.Id), iconPath: FormalArtRegistry.NavigationPath("continue"));
            }
            else if (cleared)
            {
                Label("回访说明", "这里已经处理妥当。再来看看不会花时间，也不会再次得到奖励。", parent,
                    new Vector2(44, -214), new Vector2(1068, 80), 22, safe, TextAnchor.UpperLeft);
                if (node.Type == RogueliteMapNodeType.Workshop && run.UsesRogue11)
                    ActionButton("打开学院整备", "装备、术式、背包与战术栏", parent, new Vector2(44, -324), new Vector2(1068, 104), cyan, true,
                        () => SetOverlay(UiOverlay.Loadout));
            }
            else
            {
                Label("入口说明", "从地图上挑一个相邻地点，先看看情况再出发。", parent,
                    new Vector2(44, -214), new Vector2(1068, 80), 22, muted, TextAnchor.UpperLeft);
            }

            string exitLabel = current && !cleared && !node.IsCombat && node.Type != RogueliteMapNodeType.Start ? "先不决定" : "回到地图";
            ActionButton(exitLabel, PlayerFacingCopy.ReturnToMapFree, parent, new Vector2(224, -616), new Vector2(720, 88), amber, true,
                () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawFirstRunWorkshop(Transform parent, RogueliteMapRun run)
        {
            FirstRunWorkshopSnapshot workshop = run.FirstRunExperience.Workshop;
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.FromDto(run.RogueRunState);
            RogueEquipmentInstance[] equipment = runtime.AllInstances.Where(item => IsFirstRunForgeSlot(runtime.DefinitionFor(item.InstanceId).Slot)).ToArray();
            string[] spells = run.RogueRunState.MasteredSpellIds.Where(id =>
                RogueContentCatalog.CreateAcademyV01().Spells.Any(spell => spell.DefinitionId == id && spell.Role != "passive")).Distinct(StringComparer.Ordinal).ToArray();
            if (equipment.All(item => item.InstanceId != selectedWorkshopEquipmentId)) selectedWorkshopEquipmentId = equipment.FirstOrDefault()?.InstanceId;
            if (!spells.Contains(selectedWorkshopSpellId)) selectedWorkshopSpellId = spells.FirstOrDefault();

            Label("页面标题", "工坊加工台", parent, new Vector2(44, -34), new Vector2(850, 44), 32, text, TextAnchor.MiddleLeft);
            Label("页面说明", "两项加工各结算一次、分别保存；完成后会刷新精英入口条件。", parent, new Vector2(44, -82), new Vector2(900, 32), 17, muted, TextAnchor.MiddleLeft);
            DrawWorkshopOperation(parent, run, runtime, true, new Vector2(44, -142), equipment, spells);
            DrawWorkshopOperation(parent, run, runtime, false, new Vector2(580, -142), equipment, spells);
            string gate = workshop.ForgeCompleted && workshop.SpecializationCompleted ? "精英门槛：工坊项目已完成" :
                "精英门槛尚缺：" + (!workshop.ForgeCompleted ? "装备锻造" : string.Empty) +
                (!workshop.ForgeCompleted && !workshop.SpecializationCompleted ? "、" : string.Empty) + (!workshop.SpecializationCompleted ? "术式专精" : string.Empty);
            Label("门槛", gate, parent, new Vector2(44, -604), new Vector2(1068, 34), 18,
                workshop.ForgeCompleted && workshop.SpecializationCompleted ? safe : amber, TextAnchor.MiddleCenter);
            ActionButton("返回地图", PlayerFacingCopy.ReturnToMapFree, parent, new Vector2(224, -650), new Vector2(720, 64), amber, true,
                () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawWorkshopOperation(Transform parent, RogueliteMapRun run, RogueEquipmentRuntime runtime, bool forge,
            Vector2 position, RogueEquipmentInstance[] equipment, string[] spells)
        {
            FirstRunWorkshopSnapshot workshop = run.FirstRunExperience.Workshop;
            bool completed = forge ? workshop.ForgeCompleted : workshop.SpecializationCompleted;
            string targetId = forge ? selectedWorkshopEquipmentId : selectedWorkshopSpellId;
            string completedTarget = forge ? workshop.ForgedTargetId : workshop.SpecializedTargetId;
            if (completed) targetId = completedTarget;
            string targetName = forge ? EquipmentName(runtime, targetId) : SpellName(targetId);
            int material = forge ? run.FirstRunExperience.ForgeMaterialCount : run.FirstRunExperience.SpecializationMaterialCount;
            GameObject card = Panel(forge ? "锻造卡" : "专精卡", parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(512, 432), FormalUiTheme.SurfaceRaised);
            Label("类别", forge ? "装备锻造" : "术式专精", card.transform, new Vector2(26, -22), new Vector2(300, 38), 25, forge ? amber : cyan, TextAnchor.MiddleLeft);
            Label("状态", completed ? "已完成" : "待处理", card.transform, new Vector2(350, -22), new Vector2(132, 38), 18, completed ? safe : muted, TextAnchor.MiddleRight);
            Label("目标", string.IsNullOrEmpty(targetName) ? "没有合法目标" : targetName, card.transform, new Vector2(26, -86), new Vector2(460, 42), 24, text, TextAnchor.MiddleLeft);
            string result = forge ? ForgePreview(runtime, targetId) : SpecializationPreview(targetId);
            Label("唯一结果", "安装前 → 安装后\n" + result, card.transform, new Vector2(26, -138), new Vector2(460, 92), 17, text, TextAnchor.UpperLeft);
            Label("材料", (forge ? "承力合金" : "增幅刻墨") + "　当前 " + material + "　需要 1", card.transform, new Vector2(26, -240), new Vector2(460, 30), 17, material > 0 ? safe : muted, TextAnchor.MiddleLeft);
            int count = forge ? equipment.Length : spells.Length;
            ActionButton("切换目标", count > 1 ? "查看下一个合法目标" : "当前仅有一个合法目标", card.transform,
                new Vector2(26, -286), new Vector2(218, 58), cyan, !completed && count > 1,
                () => CycleWorkshopTarget(forge, equipment, spells));
            bool canConfirm = !completed && material > 0 && !string.IsNullOrEmpty(targetId);
            ActionButton(completed ? "本轮已安装" : "确认安装", completed ? "结果已保存，不能更换" : canConfirm ? "消耗 1 份材料并保存" : "缺少材料或合法目标",
                card.transform, new Vector2(264, -286), new Vector2(222, 58), completed ? safe : amber, canConfirm,
                () => { if (forge) bootstrap.CompleteFirstRunForge(selectedWorkshopEquipmentId); else bootstrap.CompleteFirstRunSpecialization(selectedWorkshopSpellId); });
            Label("不可更换提示", "确认后本轮不可撤回或改装。", card.transform, new Vector2(26, -366), new Vector2(460, 30), 15, muted, TextAnchor.MiddleCenter);
        }

        private void CycleWorkshopTarget(bool forge, RogueEquipmentInstance[] equipment, string[] spells)
        {
            if (forge)
            {
                int index = Array.FindIndex(equipment, value => value.InstanceId == selectedWorkshopEquipmentId);
                if (equipment.Length > 0) selectedWorkshopEquipmentId = equipment[(index + 1 + equipment.Length) % equipment.Length].InstanceId;
            }
            else
            {
                int index = Array.IndexOf(spells, selectedWorkshopSpellId);
                if (spells.Length > 0) selectedWorkshopSpellId = spells[(index + 1 + spells.Length) % spells.Length];
            }
            Invalidate(false);
        }

        private void DrawFirstRunMedical(Transform parent, RogueliteMapRun run)
        {
            FirstRunMedicalSnapshot medical = run.FirstRunExperience.Medical;
            Label("页面标题", "医务室", parent, new Vector2(44, -34), new Vector2(760, 44), 32, text, TextAnchor.MiddleLeft);
            Label("页面说明", "健康确认不治疗；治疗与餐食都是可选服务，并分别保存。", parent, new Vector2(44, -82), new Vector2(900, 32), 17, muted, TextAnchor.MiddleLeft);
            RoomMetric(parent, "当前生命", PlayerFacingCopy.CurrentAndMaximum(run.CurrentHealth, 18), FormalArtRegistry.ResourceMetricPath("health"), new Vector2(44, -136), safe, 250);
            RoomMetric(parent, "学院贡献", run.StageContribution.ToString(), FormalArtRegistry.ResourceMetricPath("contribution"), new Vector2(306, -136), cyan, 250);
            RoomMetric(parent, "金币", run.Gold.ToString(), FormalArtRegistry.ResourceMetricPath("gold"), new Vector2(568, -136), amber, 250);
            RoomMetric(parent, "学院食材", run.FirstRunExperience.AcademyFoodCount.ToString(), FormalArtRegistry.SemanticPath("notice"), new Vector2(830, -136), text, 250);

            ActionButton(medical.HealthCheckCompleted ? "健康确认已完成" : "免费健康确认", medical.HealthCheckCompleted ? "精英入口条件已经记录" : "只记录状态，不恢复生命",
                parent, new Vector2(44, -214), new Vector2(330, 92), medical.HealthCheckCompleted ? safe : cyan, !medical.HealthCheckCompleted, bootstrap.CompleteFirstRunHealthCheck);
            bool canHeal = medical.HealthCheckCompleted && !medical.HealUsed && run.StageContribution >= 1 && run.CurrentHealth < 18;
            string healReason = medical.HealUsed ? "本轮已经治疗" : !medical.HealthCheckCompleted ? "请先完成健康确认" : run.CurrentHealth >= 18 ? "生命已满" : run.StageContribution < 1 ? "缺少 1 学院贡献" : "学院贡献 -1\n恢复 9 生命（不超过 18）";
            ActionButton(medical.HealUsed ? "治疗已完成" : "接受治疗", healReason, parent, new Vector2(398, -214), new Vector2(330, 92), medical.HealUsed ? safe : amber, canHeal, bootstrap.UseFirstRunHeal);
            Label("餐食标题", medical.MealUsed ? "餐食已选择：" + MealName(medical.SelectedMealId) : "选择一份餐食（可跳过）", parent,
                new Vector2(44, -332), new Vector2(1036, 38), 23, medical.MealUsed ? safe : text, TextAnchor.MiddleLeft);
            string[] meals = { "MEAL-POWER", "MEAL-AETHER", "MEAL-GUARD" };
            for (int i = 0; i < meals.Length; i++)
            {
                string mealId = meals[i]; bool affordable = MealAffordable(run, mealId); bool canBuy = medical.HealthCheckCompleted && !medical.MealUsed && affordable;
                string reason = medical.MealUsed ? (medical.SelectedMealId == mealId ? "本轮已选择" : "本轮只能选择一种餐食") :
                    !medical.HealthCheckCompleted ? "请先完成健康确认" : !affordable ? MealCostShortage(run, mealId) : MealEffect(mealId);
                ActionButton(MealName(mealId), MealCost(mealId) + "\n" + reason, parent, new Vector2(44 + i * 354, -386), new Vector2(330, 112),
                    canBuy ? safe : muted, canBuy, () => bootstrap.ChooseFirstRunMeal(mealId));
            }
            ActionButton("返回地图", PlayerFacingCopy.ReturnToMapFree, parent, new Vector2(224, -650), new Vector2(720, 64), amber, true,
                () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawFirstRunShop(Transform parent, RogueliteMapRun run)
        {
            Label("页面标题", "精英后商店", parent, new Vector2(44, -34), new Vector2(760, 44), 32, text, TextAnchor.MiddleLeft);
            Label("页面说明", "购买可选；每件商品独立结算并保存。离开商店后完成固定首次体验。", parent, new Vector2(44, -82), new Vector2(1000, 32), 17, muted, TextAnchor.MiddleLeft);
            RoomMetric(parent, "金币", run.Gold.ToString(), FormalArtRegistry.ResourceMetricPath("gold"), new Vector2(44, -132), amber, 260);
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            for (int i = 0; i < run.FirstRunExperience.Shop.Offers.Count; i++)
            {
                FirstRunShopOfferSnapshot offer = run.FirstRunExperience.Shop.Offers[i];
                string name = ContentName(catalog, offer.DefinitionId);
                bool space = run.CanAcceptFirstRunOffer(offer.OfferId);
                bool affordable = run.Gold >= offer.Price;
                bool canBuy = !offer.Sold && affordable && space;
                string reason = offer.Sold ? "已售罄" : !affordable ? "还差 " + (offer.Price - run.Gold) + " 金币" : !space ? "背包空间不足，请先整理" : "点击后直接购买并放入背包";
                string cardDetail = offer.Price + " 金币\n" + reason + "\n" + ShopContentDetail(catalog, offer.DefinitionId);
                GameObject button = ActionButton(name, cardDetail, parent, new Vector2(44 + i * 354, -220), new Vector2(330, 190),
                    offer.Sold ? muted : canBuy ? amber : muted, canBuy, () => bootstrap.PurchaseFirstRunOffer(offer.OfferId));
                BindHover(button, name, ShopContentDetail(catalog, offer.DefinitionId), offer.Sold ? muted : amber);
            }
            ActionButton("整理行囊", "购买前可腾出背包空间", parent, new Vector2(44, -446), new Vector2(330, 84), cyan, true, () => SetOverlay(UiOverlay.Loadout));
            ActionButton("离开商店并完成首次体验", "购买并非必需；离开后写入完成标记", parent, new Vector2(398, -446), new Vector2(684, 84), safe, true,
                bootstrap.CompleteFirstRunExperience, iconPath: FormalArtRegistry.NavigationPath("confirm"));
            Label("离店说明", "库存、购买与完成状态都会保留；返回入口后可继续查看档案。", parent,
                new Vector2(44, -558), new Vector2(1038, 52), 17, muted, TextAnchor.MiddleCenter);
        }

        private static bool IsFirstRunForgeSlot(OCC.Combat.Roguelite.EquipmentSlot slot)
            => slot == OCC.Combat.Roguelite.EquipmentSlot.Weapon || slot == OCC.Combat.Roguelite.EquipmentSlot.Head ||
               slot == OCC.Combat.Roguelite.EquipmentSlot.Chest || slot == OCC.Combat.Roguelite.EquipmentSlot.Feet ||
               slot == OCC.Combat.Roguelite.EquipmentSlot.Backpack || slot == OCC.Combat.Roguelite.EquipmentSlot.CastingUnit;

        private static string EquipmentName(RogueEquipmentRuntime runtime, string instanceId)
            => string.IsNullOrEmpty(instanceId) ? string.Empty : runtime.DefinitionFor(instanceId)?.DisplayName ?? instanceId;

        private static string SpellName(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            SpellDefinition spell = RogueContentCatalog.CreateAcademyV01().Spells.FirstOrDefault(value => value.DefinitionId == id);
            return spell?.DisplayName ?? id;
        }

        private static string ForgePreview(RogueEquipmentRuntime runtime, string instanceId)
        {
            EquipmentDefinition definition = string.IsNullOrEmpty(instanceId) ? null : runtime.DefinitionFor(instanceId);
            if (definition == null) return "请选择一件可锻造装备。";
            switch (definition.Slot)
            {
                case OCC.Combat.Roguelite.EquipmentSlot.Weapon: return "基础武器伤害 +2";
                case OCC.Combat.Roguelite.EquipmentSlot.Backpack: return "背包容量增加 1 行";
                case OCC.Combat.Roguelite.EquipmentSlot.CastingUnit: return "个人魔力上限 +1";
                default: return "自己回合开始获得 2 护盾";
            }
        }

        private static string SpecializationPreview(string spellId)
        {
            switch (spellId)
            {
                case "BASE-FIRE-MELEE": return "伤害 8 → 10";
                case "BASE-FIRE-RANGED": return "伤害 6 → 8";
                case "BASE-AETHER-SHIELD": return "护盾 6 → 8";
                case "BASE-MANA-RECOVER": return "恢复魔力 2 → 3";
                case "F-P-M01": return "追加火伤 8 → 10";
                case "F-P-M03": return "跃进命中武器伤害 8 → 10；起点火场不变";
                case "F-P-M06": return "下次近战命中额外造成 2 点火焰伤害";
                case "F-P-U01": return "最大突进距离 3 → 4 格；行动力奖励不变";
                case "F-P-U04": return "伤害 8 → 10；标记奖励不变";
                case "F-P-U18": return "落地冲击伤害 8 → 10；推位距离不变";
                case "F-P-R01": return "伤害 12 → 14";
                case "F-P-R19": return "中心伤害 16 → 20；邻格 8 点伤害不变";
                default: return "按增幅刻墨的固定结果强化此术式";
            }
        }

        private static string MealName(string id)
            => id == "MEAL-POWER" ? "强攻炖肉" : id == "MEAL-AETHER" ? "导能热饮" : id == "MEAL-GUARD" ? "岩盐浓汤" : "未知餐食";

        private static string MealCost(string id)
            => id == "MEAL-POWER" ? "3 金币" : id == "MEAL-AETHER" ? "1 学院贡献" : "1 学院食材";

        private static string MealEffect(string id)
            => id == "MEAL-POWER" ? "接下来 3 场战斗武器直接伤害 +2" : id == "MEAL-AETHER" ? "接下来 3 场个人魔力上限 +2，入场回满" : "接下来 3 场开战获得 6 护盾";

        private static bool MealAffordable(RogueliteMapRun run, string id)
            => id == "MEAL-POWER" ? run.Gold >= 3 : id == "MEAL-AETHER" ? run.StageContribution >= 1 : run.FirstRunExperience.AcademyFoodCount >= 1;

        private static string MealCostShortage(RogueliteMapRun run, string id)
            => id == "MEAL-POWER" ? "还差 " + Math.Max(0, 3 - run.Gold) + " 金币" :
               id == "MEAL-AETHER" ? "还差 " + Math.Max(0, 1 - run.StageContribution) + " 学院贡献" : "缺少 1 学院食材";

        private static string ContentName(RogueContentCatalog catalog, string definitionId)
        {
            EquipmentDefinition equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == definitionId);
            if (equipment != null) return equipment.DisplayName;
            TacticalItemDefinition tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == definitionId);
            if (tactical != null) return tactical.DisplayName;
            SpellDefinition spell = catalog.Spells.FirstOrDefault(value => value.DefinitionId == definitionId);
            return spell?.DisplayName ?? definitionId;
        }

        private static string ShopContentDetail(RogueContentCatalog catalog, string definitionId)
        {
            EquipmentDefinition equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == definitionId);
            if (equipment != null)
                return EquipmentSlotLabel(equipment.Slot) + "　占格 " + equipment.Width + "×" + equipment.Height + "\n" +
                    (equipment.FixedEffectIds.Count == 0 ? "无额外固定效果" : string.Join("；", equipment.FixedEffectIds));
            TacticalItemDefinition tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == definitionId);
            if (tactical != null)
                return "战术道具　占格 " + tactical.Width + "×" + tactical.Height + "\n完整次数 " + tactical.MaximumCharges + "　使用消耗 " + tactical.ActionPointCost + " 行动点";
            SpellDefinition spell = catalog.Spells.FirstOrDefault(value => value.DefinitionId == definitionId);
            return spell == null ? "商品详情不可用" : RogueliteSettlementPresentation.RogueSpellPlayerSummary(spell);
        }

        private void DrawCombatActionDossier(Transform parent, RogueliteMapRun run, RogueliteMapNode node, bool current, Color accent)
        {
            bool canEnter = (current && RogueliteUiPreferences.CanOpenCombatBriefing(run, node)) || RogueliteUiPreferences.CanTravelTo(run, node);
            RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.For(run, node.Id);
            RogueNodePreviewPresentation preview = run.UsesRogue11 ? new RogueNodePreviewPresentation(run, node) : null;
            string dividerId = FormalUiAssetPlacement.ChapterDivider(node);
            string markerId = FormalUiAssetPlacement.ChapterMarker(node);
            string objectiveText = encounter != null && !string.IsNullOrEmpty(encounter.ObjectiveSummary) ? encounter.ObjectiveSummary : node.Summary;
            string enemyText = encounter != null && (preview == null || string.IsNullOrEmpty(preview.EnemySummary))
                ? string.Join("、", encounter.EnemyArchetypeIds.Select(id => EnemyArchetypes.Get(id).DisplayName))
                : preview?.EnemySummary ?? "阶段 A 敌人脚本槽";
            string spatialText = preview == null ? encounter.SpatialGrammar + "；" + encounter.SpawnRelationship : preview.SpatialRisk;

            bool firstBattleDossier = run.IsFirstRunExperience && node.Id == "B1";
            if (firstBattleDossier) DrawDepartureProgress(parent, new Vector2(44, -12), 336f, 2);
            float headingY = firstBattleDossier ? -88f : -28f;
            float descriptionY = firstBattleDossier ? -126f : -76f;
            float dossierY = firstBattleDossier ? -170f : -128f;
            Label("页面标题", firstBattleDossier ? "最后确认，再进入第一战" : "出发前看一眼", parent, new Vector2(44, headingY), new Vector2(1000, 38), 30, text, TextAnchor.MiddleLeft);
            Label("页面说明", "看看要做什么、会遇到谁。准备好就出发，不想去也可以回地图。", parent, new Vector2(44, descriptionY), new Vector2(1000, 30), 17, muted, TextAnchor.MiddleLeft);
            FormalUiEffects.AddChapterMarker(parent, markerId, new Vector2(1080, firstBattleDossier ? -122 : -56), 2f);

            GameObject objective = Panel("行动目标区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, dossierY), new Vector2(500, 122), FormalUiTheme.SurfaceRaised);
            FormalUiEffects.AddChapterDivider(objective.transform, dividerId, new Vector2(18, -30), 2f);
            Label("目标标题", "这趟要做什么", objective.transform, new Vector2(22, -16), new Vector2(210, 26), 17, accent, TextAnchor.MiddleLeft);
            Label("目标", objectiveText, objective.transform, new Vector2(22, -58), new Vector2(456, 68), 18, text, TextAnchor.UpperLeft);

            GameObject enemy = Panel("敌情与空间区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(568, dossierY), new Vector2(552, 122), FormalUiTheme.SurfaceRaised);
            Label("敌情标题", "会遇到谁", enemy.transform, new Vector2(22, -14), new Vector2(180, 26), 17, amber, TextAnchor.MiddleLeft);
            Label("敌情", enemyText, enemy.transform, new Vector2(22, -44), new Vector2(508, 38), 17, text, TextAnchor.UpperLeft);
            Label("空间标题", "场地", enemy.transform, new Vector2(22, -86), new Vector2(110, 24), 14, muted, TextAnchor.MiddleLeft);
            Label("空间", spatialText, enemy.transform, new Vector2(132, -86), new Vector2(398, 42), 14, text, TextAnchor.UpperLeft);

            if (preview != null)
            {
                string threshold = PlayerFacingCopy.AcademyTimeOutcome(preview.CrossesTransition, preview.CrossesWarning, preview.CrossesConsolidation);
                RoomMetric(parent, "难度", string.IsNullOrEmpty(preview.EncounterLabel) ? preview.RiskLabel : preview.EncounterLabel,
                    FormalArtRegistry.ResourceMetricPath("risk"), new Vector2(44, -294), accent, 200);
                RoomMetric(parent, "用时", preview.IsZeroTime ? "不花时间" : preview.TimeCost.ToString(),
                    FormalArtRegistry.ResourceMetricPath("stage_time"), new Vector2(256, -294), cyan, 200);
                RoomMetric(parent, "生命结算", "不恢复",
                    FormalArtRegistry.ResourceMetricPath("health"), new Vector2(468, -294), FormalUiTheme.Health, 200);
                RoomMetric(parent, "回来时魔力", "+" + preview.ExpectedManaRecovery,
                    FormalArtRegistry.ResourceMetricPath("mana"), new Vector2(680, -294), FormalUiTheme.Magic, 200);
                RoomMetric(parent, "之后", threshold, FormalArtRegistry.SemanticPath("notice"), new Vector2(892, -294),
                    preview.CrossesTransition ? danger : preview.CrossesWarning ? amber : safe, 228);

                GameObject consequence = Panel("失败后果区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -372), new Vector2(648, 112), FormalUiTheme.SurfaceRaised);
                Label("失败标题", "如果输了", consequence.transform, new Vector2(20, -12), new Vector2(120, 24), 16, danger, TextAnchor.MiddleLeft);
                Label("失败后果", preview.FailureConsequence, consequence.transform, new Vector2(20, -42), new Vector2(608, 58), 15, text, TextAnchor.UpperLeft);
                GameObject reward = Panel("奖励预告区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(712, -372), new Vector2(408, 112), FormalUiTheme.SurfaceRaised);
                FormalUiEffects.AddChapterMarker(reward.transform, "reward_brass_tag", new Vector2(360, -30), 2f);
                Label("奖励标题", "赢了可得", reward.transform, new Vector2(20, -12), new Vector2(120, 24), 16, amber, TextAnchor.MiddleLeft);
                Label("奖励", preview.RewardLabel, reward.transform, new Vector2(20, -42), new Vector2(342, 58), 16, text, TextAnchor.UpperLeft);
            }

            bool contentReady = true;
            bool confirmOrigin = run.IsFirstRunExperience && node.Id == "B1" && !run.FirstRunExperience.Origin.Acknowledged;
            bool canStart = contentReady && (canEnter || confirmOrigin);
            string enterLabel = confirmOrigin ? "确认配置并出发" : "出发";
            string enterReason = confirmOrigin ? "学生背景\n就地接线　借障导流" :
                canEnter ? "准备好就出发" : RogueliteMapVisualPresentation.RestrictionText(run, node);
            GameObject start = ActionButton(enterLabel, enterReason, parent, new Vector2(44, -516), new Vector2(672, 126), canStart ? accent : muted, canStart,
                () =>
                {
                    if (confirmOrigin) bootstrap.AcknowledgeFirstRunOrigin();
                    bootstrap.StartMapNodeCombat(node.Id);
                }, focusKey: "按钮_进入战斗", iconPath: FormalArtRegistry.NavigationPath("confirm"));
            BindHover(start, enterLabel, confirmOrigin || !canEnter ? enterReason : "立刻前往场地。", canStart ? accent : muted);
            ActionButton("先不去", "回地图看看别处", parent, new Vector2(740, -516), new Vector2(380, 126), amber, true,
                () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void ConfirmFirstRunOrigin()
        {
            RogueliteMapRun run = bootstrap?.CurrentMapRun;
            if (run == null || !run.IsFirstRunExperience) return;
            bootstrap.AcknowledgeFirstRunOrigin();
            if (run.MapNodes.Any(node => node.Id == "B1")) selectedNodeId = "B1";
            SetOverlay(UiOverlay.None);
            pendingFocusKey = RogueliteMapVisualPresentation.FocusKey(selectedNodeId);
        }

        private void DrawDepartureProgress(Transform parent, Vector2 position, float width, int activeStep)
        {
            string[] titles = { "1  基础配置", "2  查看第一战", "3  战前确认" };
            string[] details = { "认识固定能力", "从地图查看目的地", "确认敌情与后果" };
            string[] icons = { FormalArtRegistry.NodeTypePath("start"), FormalArtRegistry.NodeTypePath("combat"), FormalArtRegistry.NavigationPath("confirm") };
            const float gap = 20f;
            for (int i = 0; i < titles.Length; i++)
            {
                Color stepAccent = i < activeStep ? safe : i == activeStep ? cyan : muted;
                GameObject step = Panel("出发进度_" + i, parent, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(position.x + i * (width + gap), position.y), new Vector2(width, 64),
                    i == activeStep ? FormalUiTheme.Interactive : FormalUiTheme.SurfaceRaised);
                Line(step.transform, Vector2.zero, new Vector2(4, 64), stepAccent);
                Icon("图标", icons[i], step.transform, new Vector2(14, -16), new Vector2(32, 32));
                Label("步骤", titles[i], step.transform, new Vector2(58, -8), new Vector2(width - 70, 24), 17, stepAccent, TextAnchor.MiddleLeft);
                Label("说明", details[i], step.transform, new Vector2(58, -32), new Vector2(width - 70, 22), 14, i <= activeStep ? text : muted, TextAnchor.MiddleLeft);
            }
        }

        private void OriginFeatureCard(Transform parent, string category, string title, string body, string iconPath, Vector2 position, float width, Color accent)
        {
            GameObject card = Panel("出身能力_" + title, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(width, 166), FormalUiTheme.SurfaceRaised);
            Line(card.transform, Vector2.zero, new Vector2(4, 166), accent);
            Icon("图标", iconPath, card.transform, new Vector2(18, -18), new Vector2(48, 48));
            Label("类别", category, card.transform, new Vector2(82, -14), new Vector2(width - 100, 24), 15, accent, TextAnchor.MiddleLeft);
            Label("名称", title, card.transform, new Vector2(82, -40), new Vector2(width - 100, 34), 23, text, TextAnchor.MiddleLeft);
            Label("效果", body, card.transform, new Vector2(18, -88), new Vector2(width - 36, 64), 16, muted, TextAnchor.UpperLeft);
        }

        private void RoomMetric(Transform parent, string title, string value, string iconPath, Vector2 position, Color accent, float width = 204)
        {
            GameObject metric = Panel("房间指标_" + title, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(width, 54), FormalUiTheme.Surface);
            Icon("图标", iconPath, metric.transform, new Vector2(12, -11), new Vector2(32, 32));
            Label("标题", title, metric.transform, new Vector2(54, -4), new Vector2(width - 66, 18), 15, muted, TextAnchor.MiddleLeft);
            Label("读数", value, metric.transform, new Vector2(54, -21), new Vector2(width - 66, 28), 20, accent, TextAnchor.MiddleLeft);
        }

        private static string NodeRoomActionTitle(RogueliteMapNode node, bool current, bool cleared)
            => cleared ? "再来看看" : !current ? "去之前看一眼" : node.IsCombat ? "准备出发" : node.Type == RogueliteMapNodeType.Start ? "学院门厅" : "你想怎么做？";

        private static string NodeRoomInstruction(RogueliteMapNode node, bool current, bool cleared)
        {
            if (cleared) return "这里已经处理妥当，可以放心回来看看。";
            if (!current) return "先看看这里有什么，再决定要不要过去。";
            if (node.IsCombat) return "对手和场地都写在下面，准备好就出发。";
            if (node.Type == RogueliteMapNodeType.Start) return "旅程从这里开始。先在地图上选个相邻地点。";
            return "选定后就会立刻行动；拿不准的话，可以先回地图。";
        }

        private static Color NodeRoomAccent(RogueliteMapNodeType type)
        {
            if (type == RogueliteMapNodeType.Elite || type == RogueliteMapNodeType.Finale) return FormalUiTheme.Danger;
            if (type == RogueliteMapNodeType.Shop || type == RogueliteMapNodeType.Workshop) return FormalUiTheme.Amber;
            if (type == RogueliteMapNodeType.Medical) return FormalUiTheme.Safe;
            return FormalUiTheme.Cyan;
        }

        private static string NodeRoomMaterialCue(RogueliteMapNodeType type)
        {
            switch (type)
            {
                case RogueliteMapNodeType.Start: return "学院门厅里人来人往，今天的安排已经贴出。";
                case RogueliteMapNodeType.Combat: return "教员划好了场地，对手正在等你。";
                case RogueliteMapNodeType.Elite: return "高年级生和教员都在场，这一回不会轻松。";
                case RogueliteMapNodeType.Event: return "这里有人等着答复，先听听他们怎么说。";
                case RogueliteMapNodeType.Workshop: return "工坊里工具齐全，适合整理和校准装备。";
                case RogueliteMapNodeType.Shop: return "摊主已经摆好货物，价钱都写在牌上。";
                case RogueliteMapNodeType.Medical: return "医务室提供公开费用的治疗与餐食服务。";
                case RogueliteMapNodeType.Finale: return "塔心就在前面。学院的终考只剩这一关。";
                default: return "学院里还有许多地方值得看看。";
            }
        }

        private void MapRegionShortcut(Transform parent, string regionId, Vector2 position)
        {
            GameObject buttonObject = Panel("区域定位_" + regionId, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(52, 52), FormalUiTheme.SurfaceRaised);
            Image background = buttonObject.GetComponent<Image>();
            Button button = buttonObject.AddComponent<Button>(); button.targetGraphic = background;
            button.onClick.AddListener(() => mapViewportController.CenterOnSourcePosition(AcademyMapVisualLayout.SourceCenterForRegion(regionId)));
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(background.color, cyan),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            GameObject iconObject = Create("区域图标", buttonObject.transform); RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f); iconRect.anchoredPosition = Vector2.zero;
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = Resources.Load<Sprite>(FormalArtRegistry.MapRegionPath(regionId)); icon.preserveAspect = true; icon.raycastTarget = false;
            int iconSize = FormalUiKit.IntegerSpriteSize(icon.sprite, 36f); iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            BindHover(buttonObject, MapRegionLabel(regionId), "定位地图，不移动角色。", cyan);
        }

        private void ResourceChip(Transform parent, float x, float y, string label, int value, Color accent)
        { MetricChip(parent, x, y, label, value.ToString(), accent); }

        private void MetricChip(Transform parent, float x, float y, string label, string value, Color accent, string iconPath = null, float width = 270f)
        {
            GameObject chip = Panel("资源_" + label, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y), new Vector2(width, 60), FormalUiTheme.Surface);
            Line(chip.transform, Vector2.zero, new Vector2(4, 60), accent);
            float labelX = 14f;
            if (!string.IsNullOrEmpty(iconPath))
            {
                Image icon = FormalUiKit.TopLeftIconSlot("语义图标", chip.transform, Resources.Load<Sprite>(iconPath), new Vector2(10, -10));
                int size = FormalUiKit.IntegerSpriteSize(icon.sprite, 32f); icon.rectTransform.sizeDelta = new Vector2(size, size); labelX = size + 14f;
            }
            bool changed = resourceDeltas.TryGetValue(label, out int delta);
            string valueText = changed ? value + " " + (delta > 0 ? "+" : string.Empty) + delta : value;
            Text valueLabel = Label("读数", label + "  " + valueText, chip.transform, new Vector2(labelX, -12), new Vector2(width - labelX - 8, 36), 16, changed ? accent : text, TextAnchor.MiddleLeft);
            valueLabel.fontStyle = FontStyle.Normal;
            valueLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            resourceValues[label] = valueLabel;
            BindHover(chip, label, label + "：" + valueText, accent);
            if (changed) FormalUiKit.ApplySkin(chip.GetComponent<Image>(), "reward", Color.white);
        }

        private void RefreshMapResources()
        {
            RogueliteMapRun run = bootstrap == null ? null : bootstrap.CurrentMapRun;
            if (run == null || currentScreen != UiScreen.Map || resourceValues.Count == 0) return;
            RogueliteMapPresentationModel model = RogueliteMapPresentationModel.From(run);
            SetResourceValue("等级", model.Level);
            SetResourceValue("经验", model.Experience);
            if (run.UsesRogue11)
            {
                RogueMapStatusPresentation status = new RogueMapStatusPresentation(run);
                SetMetricValue("生命", status.Health + "/" + status.MaximumHealth);
                SetResourceValue("金币", status.Gold); SetResourceValue("学院贡献", status.StageContribution);
                SetMetricValue("学院时序", status.StageTime + "/" + status.TransitionTime);
                PartialRefreshCount++; return;
            }
            SetResourceValue("零件", model.Parts);
            SetResourceValue("以太", model.Aether);
            SetResourceValue("补给", model.Supplies);
            SetResourceValue("侦测", model.Scouting);
            PartialRefreshCount++;
            if (resourceDeltas.Count > 0)
                DOVirtual.DelayedCall(1.1f, () => { resourceDeltas.Clear(); RefreshMapResources(); }, true).SetTarget(this);
        }

        private void SetResourceValue(string key, int value)
        {
            if (!resourceValues.TryGetValue(key, out Text label) || label == null) return;
            bool changed = resourceDeltas.TryGetValue(key, out int delta);
            string valueText = changed ? value + " " + (delta > 0 ? "+" : string.Empty) + delta : value.ToString();
            label.text = key + "  " + valueText;
        }

        private void SetMetricValue(string key, string value)
        {
            if (!resourceValues.TryGetValue(key, out Text label) || label == null) return;
            label.text = key + "  " + value; label.color = text;
        }

        private void DrawConnections(Transform parent, RogueliteMapRun run)
        {
            var drawn = new HashSet<string>(StringComparer.Ordinal);
            foreach (RogueliteMapNode from in run.MapNodes)
            foreach (string nextId in from.NextIds)
            {
                string key = string.CompareOrdinal(from.Id, nextId) < 0 ? from.Id + "|" + nextId : nextId + "|" + from.Id;
                if (!drawn.Add(key)) continue;
                RogueliteMapNode to = run.MapNode(nextId);
                Vector2 a = NodePosition(from); Vector2 b = NodePosition(to);
                RogueliteMapNodeVisualState fromState = run.VisualStateFor(from.Id);
                RogueliteMapNodeVisualState toState = run.VisualStateFor(to.Id);
                RogueliteMapRouteVisualState route = RogueliteMapVisualPresentation.RouteState(fromState, toState);
                bool available = route == RogueliteMapRouteVisualState.Available;
                Color color = available ? FormalUiTheme.WithAlpha(cyan, .94f) :
                    route == RogueliteMapRouteVisualState.Locked ? FormalUiTheme.WithAlpha(danger, .26f) :
                    route == RogueliteMapRouteVisualState.Safe ? FormalUiTheme.WithAlpha(safe, .30f) :
                    FormalUiTheme.WithAlpha(muted, .20f);
                if (available)
                {
                    MapRouteLine(parent, a, b, 10f, FormalUiTheme.WithAlpha(ink, .76f));
                    MapRouteLine(parent, a, b, 5f, color);
                }
                else
                {
                    MapDashedRouteLine(parent, a, b, 7f, FormalUiTheme.WithAlpha(ink, .58f));
                    MapDashedRouteLine(parent, a, b, 3f, color);
                }
            }
        }

        private void DrawNode(Transform parent, RogueliteMapRun run, RogueliteMapNode node)
        {
            RogueliteMapNodeVisualState state = run.VisualStateFor(node.Id);
            bool identified = state != RogueliteMapNodeVisualState.Unknown;
            bool selected = node.Id == selectedNodeId;
            Color accent = state == RogueliteMapNodeVisualState.Current || state == RogueliteMapNodeVisualState.Available ? cyan :
                state == RogueliteMapNodeVisualState.Cleared ? safe : state == RogueliteMapNodeVisualState.Locked ? danger : state == RogueliteMapNodeVisualState.Known || state == RogueliteMapNodeVisualState.Visited ? amber : muted;
            string focusKey = RogueliteMapVisualPresentation.FocusKey(node.Id);
            string time = identified && run.UsesRogue11 ? (AcademyMapTuning.TimeCost(node.Type) == 0 ? "　零时" : "　+" + AcademyMapTuning.TimeCost(node.Type) + "时") : string.Empty;
            GameObject buttonObject = FormalMapNodeButton(parent, NodePosition(node), node.Type, state, accent, () =>
            {
                selectedNodeId = node.Id;
                pendingFocusKey = focusKey;
                bootstrap.NotifyMapNodeSelected(node.Id);
                if (identified) SetOverlay(UiOverlay.NodeRoom);
                else Invalidate(false);
            }, focusKey);
            if (identified)
            {
                AddCompactNodeIcon(buttonObject.transform, node.Type);
                BindHover(buttonObject, node.DisplayName, TypeLabel(node.Type) + time + "\n" + node.Summary, accent);
            }
            else BindHover(buttonObject, "还看不清", "先走到附近，才能看清这里。", muted);
            if (selected && buttonObject.transform.Find("节点选中动效") == null) AddMapNodeSelectionEffect(buttonObject.transform, node.Type, accent);
        }

        private void AddMapNodeSelectionEffect(Transform parent, RogueliteMapNodeType type, Color accent)
        {
            float size = MapNodeDisplaySize(type);
            GameObject effect = Create("节点选中动效", parent);
            RectTransform rect = effect.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size + 20f, size + 20f);
            CanvasGroup group = effect.AddComponent<CanvasGroup>();
            group.alpha = .58f;

            Sprite haloSprite = Resources.Load<Sprite>(FormalArtRegistry.LargeMapNodeMarkerPath("Current"));
            GameObject haloObject = Create("呼吸光环", effect.transform);
            RectTransform haloRect = haloObject.AddComponent<RectTransform>();
            haloRect.anchorMin = haloRect.anchorMax = haloRect.pivot = new Vector2(.5f, .5f);
            haloRect.anchoredPosition = Vector2.zero;
            haloRect.sizeDelta = new Vector2(size + 12f, size + 12f);
            Image halo = haloObject.AddComponent<Image>();
            halo.sprite = haloSprite;
            halo.preserveAspect = true;
            halo.color = FormalUiTheme.WithAlpha(accent, .72f);
            halo.raycastTarget = false;

            SelectionSpark(effect.transform, new Vector2(-size * .58f, 0), accent);
            SelectionSpark(effect.transform, new Vector2(size * .52f, 0), accent);
            effect.transform.SetAsFirstSibling();
            float intensity = bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity;
            if (intensity <= 0f) return;
            float duration = Mathf.Lerp(.9f, .55f, intensity);
            haloRect.DOScale(1.12f, duration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(effect);
            DOTween.To(() => group.alpha, value => group.alpha = value, .28f, duration)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(effect);
        }

        private static void SelectionSpark(Transform parent, Vector2 position, Color accent)
        {
            GameObject spark = Create("选中火花", parent);
            RectTransform rect = spark.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(6, 3);
            Image image = spark.AddComponent<Image>();
            image.color = accent;
            image.raycastTarget = false;
        }

        private static void AddCompactNodeIcon(Transform parent, RogueliteMapNodeType type)
        {
            string runtimeId = type.ToString().ToLowerInvariant();
            Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.MapNodeTypeIconPath(runtimeId));
            if (sprite == null) throw new KeyNotFoundException("Missing formal node icon: " + runtimeId);
            GameObject iconObject = Create("节点类型图标_" + runtimeId, parent);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
            float size = type == RogueliteMapNodeType.Finale ? 96f : type == RogueliteMapNodeType.Elite ? 80f : 64f;
            iconRect.anchoredPosition = Vector2.zero; iconRect.sizeDelta = new Vector2(size, size);
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
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

        private static void AddMapStateIcon(Transform parent, RogueliteMapNodeVisualState state, Vector2 position, Vector2 size)
        {
            string path = FormalArtRegistry.MapStatePath(state.ToString());
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null) throw new KeyNotFoundException("Missing formal map-state icon: " + path);
            GameObject iconObject = Create("节点状态图标_" + state, parent);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 1); iconRect.pivot = new Vector2(0, 1);
            iconRect.anchoredPosition = position; iconRect.sizeDelta = size;
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
        }

        private void DrawMapStateLegend(Transform parent, RogueliteMapNodeVisualState state, Vector2 position)
        {
            GameObject chip = Panel("图例_" + state, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(44, 44), FormalUiTheme.Surface);
            GameObject iconObject = Create("状态牌", chip.transform);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.anchoredPosition = Vector2.zero; iconRect.sizeDelta = new Vector2(32, 32);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(FormalArtRegistry.MapNodeMarkerPath(state.ToString()));
            icon.preserveAspect = true; icon.raycastTarget = false;
            BindHover(chip, RogueliteMapVisualPresentation.StateLabel(state), MapStateTooltip(state), state == RogueliteMapNodeVisualState.Locked ? danger : state == RogueliteMapNodeVisualState.Cleared ? safe : cyan);
        }

        private void DrawNodeDetail(Transform parent, RogueliteMapRun run, RogueliteMapNode node)
        {
            RogueliteMapNodeVisualState visual = run.VisualStateFor(node.Id);
            bool identified = visual != RogueliteMapNodeVisualState.Unknown;
            bool current = node.Id == run.CurrentNodeId;
            AcademyEventDefinition currentEvent = current && node.Type == RogueliteMapNodeType.Event ? run.CurrentEvent : null;
            string regionId = MapRegionId(node);
            Label("类型", identified ? TypeLabel(node.Type).ToUpperInvariant() : "未知地点", parent, new Vector2(28, -28), new Vector2(210, 26), 17, amber, TextAnchor.MiddleLeft);
            AddRegionIdentity(parent, regionId);
            Label("名称", identified ? currentEvent?.DisplayName ?? node.DisplayName : "还看不清", parent, new Vector2(28, -62), new Vector2(390, 48), 30, text, TextAnchor.MiddleLeft);
            Label("摘要", identified ? node.Summary : "走近后才能看清", parent, new Vector2(28, -116), new Vector2(390, 36), 16, muted, TextAnchor.UpperLeft);
            bool cleared = run.CompletedNodes.Contains(node.Id);
            string stateText = RogueliteMapVisualPresentation.RestrictionText(run, node);
            Color stateColor = visual == RogueliteMapNodeVisualState.Locked ? danger : visual == RogueliteMapNodeVisualState.Unknown ? muted : cleared ? safe : cyan;
            DetailIconMetric(parent, "状态", stateText, FormalArtRegistry.MapStatePath(visual.ToString()), new Vector2(28, -174), stateText, stateColor);
            if (!identified) return;

            if (run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, node);
                string threshold = PlayerFacingCopy.AcademyTimeOutcome(preview.CrossesTransition, preview.CrossesWarning, preview.CrossesConsolidation);
                string encounterRisk = string.IsNullOrEmpty(preview.EncounterLabel) ? preview.RiskLabel : preview.EncounterLabel + "\n" + preview.RiskLabel;
                string encounterDetail = string.IsNullOrEmpty(preview.EnemySummary) ? preview.FailureConsequence : "敌方：" + preview.EnemySummary + "\n空间：" + preview.SpatialRisk + "\n" + preview.FailureConsequence;
                DetailIconMetric(parent, "难度", encounterRisk, FormalArtRegistry.ResourceMetricPath("risk"), new Vector2(28, -218), encounterDetail, preview.CrossesTransition ? danger : amber);
                DetailIconMetric(parent, "用时", preview.IsZeroTime ? "不花时间" : preview.TimeCost.ToString(), FormalArtRegistry.ResourceMetricPath("stage_time"), new Vector2(218, -218), "回来后，学期进度是 " + preview.ProjectedStageTime, cyan);
                DetailIconMetric(parent, "生命结算", "战损继承", FormalArtRegistry.ResourceMetricPath("health"), new Vector2(28, -262), "节点结算不会自动恢复生命", FormalUiTheme.Health);
                DetailIconMetric(parent, "回来时魔力", "+" + preview.ExpectedManaRecovery, FormalArtRegistry.ResourceMetricPath("mana"), new Vector2(218, -262), "回来时恢复 " + preview.ExpectedManaRecovery + " 个人魔力", FormalUiTheme.Magic);
                Label("之后", threshold + "　" + preview.RewardLabel, parent, new Vector2(28, -306), new Vector2(390, 30), 15,
                    preview.CrossesTransition ? danger : preview.CrossesWarning ? amber : text, TextAnchor.MiddleLeft);
            }

            if (current && !cleared && !node.IsCombat && node.Type != RogueliteMapNodeType.Start)
            {
                IReadOnlyList<RogueliteNodeContentChoice> choices = run.CurrentContentChoices;
                Label("选择标题", "你想怎么做？", parent, new Vector2(28, -346), new Vector2(390, 30), 18, text, TextAnchor.MiddleLeft);
                for (int i = 0; i < choices.Count; i++)
                {
                    RogueliteNodeContentChoice choice = choices[i];
                    UiOperationAvailability availability = RogueliteEconomyPresentation.ForNodeChoice(run, choice);
                    GameObject choiceButton = ActionButton(choice.DisplayName, RogueliteEconomyPresentation.NodeChoiceSummary(run, choice, availability), parent,
                        new Vector2(28, -386 - i * 78), new Vector2(390, 66), amber, availability.CanExecute, () => bootstrap.ChooseMapNodeContent(choice.Id));
                    BindHover(choiceButton, choice.DisplayName, choice.Preview + "\n" + AvailabilityText(availability), amber);
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
            string action = cleared ? "再去看看" : resumeCurrentCombat ? "回到战斗" : node.IsCombat ? "出发前看一眼" : "前往这里";
            string detail = canTravel ? string.Empty : "先选择一个相邻地点";
            if (!canTravel) detail = RogueliteMapVisualPresentation.RestrictionText(run, node);
            string travelTooltip = cleared ? "回访不再触发战斗或奖励。" : "抵达后仍可沿路线返回。";
            if (run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, node);
                travelTooltip += "\n" + preview.FailureConsequence;
            }
            GameObject travelButton = ActionButton(action, detail, parent, new Vector2(28, -350), new Vector2(390, 76), canTravel ? cyan : muted, canTravel,
                node.IsCombat ? OpenSelectedNodeRoom : () => bootstrap.SelectMapNode(node.Id));
            BindHover(travelButton, action, travelTooltip, canTravel ? cyan : muted);
            if (current && node.Type == RogueliteMapNodeType.Start)
                Label("入口提示", "先从地图上选个相邻地点。看清情况后，再决定要不要去。", parent, new Vector2(28, -450), new Vector2(390, 60), 18, muted, TextAnchor.UpperLeft);
        }

        private void DrawWorkshop(Transform parent, RogueliteMapRun run)
        {
            if (run.UsesRogue11) { DrawRogueWorkshop(parent, run); return; }
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
            Label("锻造口径", "装备锻造按确定性升级分支执行；不提供随机重掷。", parent, new Vector2(28, y), new Vector2(390, 72), 16, muted, TextAnchor.MiddleLeft);
        }

        private void DrawRogueWorkshop(Transform parent, RogueliteMapRun run)
        {
            Label("整理行囊", "更换术式、装备和随身道具", parent, new Vector2(28, -270), new Vector2(390, 30), 20, text, TextAnchor.MiddleLeft);
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            string[] slots = run.RogueRunState.EquippedSpellIds;
            for (int index = 0; index < slots.Length; index++)
            {
                string id = slots[index]; string name = string.IsNullOrEmpty(id) ? "空" : catalog.Spells.First(value => value.DefinitionId == id).DisplayName;
                Label("术式槽" + (index + 1), (index + 1) + "　" + name, parent, new Vector2(28 + (index % 2) * 196, -312 - (index / 2) * 42), new Vector2(186, 34), 14, string.IsNullOrEmpty(id) ? muted : amber, TextAnchor.MiddleLeft);
            }
            Label("装备", "行囊里 " + run.RogueRunState.EquipmentInstances.Count + " 件　身上 " + run.RogueRunState.EquipmentSlotInstanceIds.Count(value => !string.IsNullOrEmpty(value.Value)) + " 件",
                parent, new Vector2(28, -490), new Vector2(390, 34), 15, cyan, TextAnchor.MiddleLeft);
            Label("整备规则", "锻造由装备、材料与部件位唯一决定；每件装备最多一次，不能重掷或替换。", parent, new Vector2(28, -532), new Vector2(390, 48), 13, muted, TextAnchor.UpperLeft);
        }

        private void DrawBriefing()
        {
            MissionPreparation preparation = bootstrap.CurrentPreparation;
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            string missionName = run == null ? preparation?.MissionId ?? "未知任务" : run.MapNode(run.CurrentNodeId).DisplayName;
            Header("出发准备", missionName);
            GameObject card = FormalUiKit.LayoutPanel("简报卡", content.transform, "briefing.card", panel);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 680);
            Label("任务", missionName, card.transform, new Vector2(40, -24), new Vector2(1200, 48), 34, text, TextAnchor.MiddleLeft);
            GameObject objective = Panel("行动目标区", card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -88), new Vector2(1200, 118), FormalUiTheme.SurfaceRaised);
            Label("目标标题", "这趟要做什么", objective.transform, new Vector2(18, -10), new Vector2(210, 26), 18, cyan, TextAnchor.MiddleLeft);
            Label("目标", preparation?.RulesSummary ?? "无", objective.transform, new Vector2(18, -42), new Vector2(1164, 62), 23, text, TextAnchor.UpperLeft);
            GameObject enemy = Panel("敌方编成区", card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -220), new Vector2(1200, 132), FormalUiTheme.SurfaceRaised);
            Label("敌情标题", "会遇到谁", enemy.transform, new Vector2(18, -10), new Vector2(210, 26), 18, amber, TextAnchor.MiddleLeft);
            Label("敌情", preparation?.EnemySummary ?? "无", enemy.transform, new Vector2(18, -42), new Vector2(1164, 76), 22, text, TextAnchor.UpperLeft);
            string briefingTooltip = "准备好就出发；想再看看，也可以先回地图。";
            if (run != null && run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, run.MapNode(run.CurrentNodeId));
                string threshold = PlayerFacingCopy.AcademyTimeOutcome(preview.CrossesTransition, preview.CrossesWarning, preview.CrossesConsolidation);
                string encounterRisk = string.IsNullOrEmpty(preview.EncounterLabel) ? preview.RiskLabel : preview.EncounterLabel + "\n" + preview.RiskLabel;
                DetailIconMetric(card.transform, "难度", encounterRisk, FormalArtRegistry.ResourceMetricPath("risk"), new Vector2(40, -368), "对手：" + preview.EnemySummary + "\n场地：" + preview.SpatialRisk + "\n" + preview.FailureConsequence, amber, 224, 58);
                DetailIconMetric(card.transform, "用时", preview.IsZeroTime ? "不花时间" : preview.TimeCost.ToString(), FormalArtRegistry.ResourceMetricPath("stage_time"), new Vector2(284, -368), "回来后，学期进度是 " + preview.ProjectedStageTime, cyan, 224, 58);
                DetailIconMetric(card.transform, "生命结算", "战损继承", FormalArtRegistry.ResourceMetricPath("health"), new Vector2(528, -368), "节点结算不会自动恢复生命", FormalUiTheme.Health, 224, 58);
                DetailIconMetric(card.transform, "回来时魔力", "+" + preview.ExpectedManaRecovery, FormalArtRegistry.ResourceMetricPath("mana"), new Vector2(772, -368), "回来时恢复个人魔力", FormalUiTheme.Magic, 224, 58);
                DetailIconMetric(card.transform, "之后", threshold, FormalArtRegistry.SemanticPath("notice"), new Vector2(1016, -368), threshold, preview.CrossesTransition ? danger : preview.CrossesWarning ? amber : safe, 224, 58);
                briefingTooltip = preview.FailureConsequence + "\n赢了可得：" + preview.RewardLabel;
            }
            else Label("注意", briefingTooltip, card.transform, new Vector2(40, -368), new Vector2(1200, 58), 18, muted, TextAnchor.UpperLeft);
            GameObject start = ActionButton("出发", "立刻前往场地", card.transform, new Vector2(40, -454), new Vector2(580, 142), cyan, true, bootstrap.StartDeveloperCombat, iconPath: FormalArtRegistry.NavigationPath("confirm"));
            BindHover(start, "出发", briefingTooltip, cyan);
            ActionButton("先不去", "回地图看看别处", card.transform, new Vector2(660, -454), new Vector2(580, 142), amber, bootstrap.CurrentMapRun != null, bootstrap.ReturnToMapRun, iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawSettings()
        {
            Header("辅助设置", "即时生效");
            RogueliteUiPreferences p = bootstrap.UiPreferences;
            GameObject card = FormalUiKit.LayoutPanel("设置卡", content.transform, "settings.card", panel);
            Label("标题", "显示、动效与输入提示", card.transform, new Vector2(48, -38), new Vector2(920, 46), 32, text, TextAnchor.MiddleLeft);
            SettingRow(card.transform, 0, "主音量", Mathf.RoundToInt(p.MasterVolume * 100) + "%", "档位　0　25　50　75　100", cyan, () => ChangeSettings(volume: Step(p.MasterVolume)));
            SettingRow(card.transform, 1, "动画强度", Mathf.RoundToInt(p.AnimationIntensity * 100) + "%", "档位　0　25　50　75　100", cyan, () => ChangeSettings(animation: Step(p.AnimationIntensity)));
            SettingRow(card.transform, 2, "屏幕震动", OnOff(p.ScreenShake), string.Empty, p.ScreenShake ? safe : muted, () => ChangeSettings(screenShake: !p.ScreenShake));
            SettingRow(card.transform, 3, "战斗浮字", OnOff(p.FloatingText), string.Empty, p.FloatingText ? safe : muted, () => ChangeSettings(floatingText: !p.FloatingText));
            SettingRow(card.transform, 4, "高对比色彩", OnOff(p.HighContrast), string.Empty, p.HighContrast ? Color.white : muted, () => ChangeSettings(highContrast: !p.HighContrast));
            SettingRow(card.transform, 5, "大号文字", OnOff(p.LargeText), string.Empty, p.LargeText ? safe : muted, () => ChangeSettings(largeText: !p.LargeText));
            SettingRow(card.transform, 6, "键位提示", OnOff(p.KeyHints), string.Empty, p.KeyHints ? safe : muted, () => ChangeSettings(keyHints: !p.KeyHints));
            ActionButton("返回", bootstrap.SettingsSaveDetail, card.transform, new Vector2(48, -660), new Vector2(944, 62), cyan, true, () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawLoadout()
        {
            RogueliteMapRun run = bootstrap.CurrentMapRun ?? bootstrap.ArchivedMapRun;
            if (run == null || !run.UsesRogue11) { SetOverlay(UiOverlay.None); return; }
            Header("角色与整备", "换好装备和术式，再去下一站");
            GameObject card = Panel("整备总览", content.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -80), new Vector2(1872, 976), panel);
            RogueRunDto dto = run.RogueRunState;
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.FromDto(dto);
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            Dictionary<string, SpellDefinition> spells = catalog.Spells.ToDictionary(value => value.DefinitionId, StringComparer.Ordinal);
            if (run.IsFirstRunExperience)
                spells[RogueSpellCombatRuntime.FirstBattleOriginSpell.DefinitionId] = RogueSpellCombatRuntime.FirstBattleOriginSpell;
            IReadOnlyList<RogueInventoryItemPresentation> items = RogueInventoryPresentation.Build(runtime);
            loadoutDragRuntime = runtime;
            loadoutGridRect = null;
            loadoutEquipmentSlotRects.Clear();
            loadoutEquipmentDropOverlays.Clear();
            if (string.IsNullOrEmpty(selectedRogueInventoryId) || runtime.EquipmentItem(selectedRogueInventoryId) == null && runtime.TacticalItem(selectedRogueInventoryId) == null)
                selectedRogueInventoryId = items.FirstOrDefault()?.InstanceId ?? runtime.Equipped.Values.FirstOrDefault(value => !string.IsNullOrEmpty(value));

            DrawLoadoutNavigation(card.transform, runtime, dto);
            if (loadoutSection == LoadoutSection.Equipment)
                DrawEquipmentLoadout(card.transform, runtime, items);
            else
                DrawSpellLoadout(card.transform, dto, spells);

            ActionButton("返回地图", string.Empty, card.transform, new Vector2(1518, -906), new Vector2(330, 52), cyan, true,
                () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawLoadoutNavigation(Transform parent, RogueEquipmentRuntime runtime, RogueRunDto dto)
        {
            LoadoutTab(parent, LoadoutSection.Equipment, "装备、背包与战术栏", "装备槽 9　背包 10×6　战术栏 4", 16, cyan, FormalArtRegistry.ItemPath("category_armor"));
            LoadoutTab(parent, LoadoutSection.Spells, "术式编组", "8 个术式槽", 410, amber, RogueSpellIconPath(dto.EquippedSpellIds.FirstOrDefault()));
            int occupied = RogueInventoryPresentation.Build(runtime).Count;
            Label("整备摘要", "背包物品 " + occupied + "\n页面详情常驻，悬浮窗辅助快速查看", parent,
                new Vector2(1000, -22), new Vector2(848, 52), 18, muted, TextAnchor.MiddleRight);
        }

        private void LoadoutTab(Transform parent, LoadoutSection section, string title, string detail, float x, Color accent, string iconPath)
        {
            bool active = loadoutSection == section;
            ActionButton(title, active ? detail : string.Empty, parent, new Vector2(x, -22), new Vector2(374, 62), active ? accent : muted, true,
                () => { ClearLoadoutDrag(); loadoutSection = section; Invalidate(false); }, iconPath: iconPath);
        }

        private void DrawEquipmentLoadout(Transform parent, RogueEquipmentRuntime runtime, IReadOnlyList<RogueInventoryItemPresentation> items)
        {
            GameObject characterPanel = Panel("角色信息栏", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(16, -96), new Vector2(450, 794), FormalUiTheme.Surface);
            DrawLoadoutCharacter(characterPanel.transform, runtime);

            GameObject equipmentPanel = Panel("装备工作区", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(486, -96), new Vector2(700, 794), FormalUiTheme.Surface);
            Label("装备区标题", "身体装备", equipmentPanel.transform, new Vector2(28, -18), new Vector2(360, 42), 26, cyan, TextAnchor.MiddleLeft);
            Label("装备区说明", "拖入装备或替换\n将已装备物品拖回背包卸下", equipmentPanel.transform,
                new Vector2(28, -56), new Vector2(640, 30), 16, muted, TextAnchor.MiddleLeft);
            IReadOnlyList<OCC.Combat.Roguelite.EquipmentSlot> slots = EquipmentSlotsForPresentation();
            for (int index = 0; index < slots.Count; index++)
            {
                OCC.Combat.Roguelite.EquipmentSlot slotType = slots[index];
                runtime.Equipped.TryGetValue(slotType, out string instanceId);
                EquipmentDefinition definition = runtime.DefinitionFor(instanceId);
                bool selected = instanceId == selectedRogueInventoryId;
                Vector2 slotPosition = new Vector2(28 + index % 3 * 214, -100 - index / 3 * 82);
                GameObject slot;
                if (definition == null)
                {
                    slot = Panel("空装备槽_" + slotType, equipmentPanel.transform, new Vector2(0, 1), new Vector2(0, 1),
                        slotPosition, new Vector2(196, 68), Color.clear);
                    Image transparentTarget = slot.GetComponent<Image>();
                    Button emptyButton = slot.AddComponent<Button>(); emptyButton.targetGraphic = transparentTarget;
                    emptyButton.onClick.AddListener(() => TryEquipSelected(runtime, slotType));
                    FormalUiKit.ConfigureButtonFeedback(emptyButton, FormalUiButtonPalette.ForAccent(Color.clear, cyan),
                        () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
                    Label("槽位名称", EquipmentSlotLabel(slotType), slot.transform, new Vector2(8, -18), new Vector2(180, 28),
                        16, muted, TextAnchor.MiddleLeft);
                    Line(slot.transform, new Vector2(8, -54), new Vector2(180, 2), FormalUiTheme.WithAlpha(muted, .28f));
                }
                else
                {
                    slot = ActionButton(EquipmentSlotLabel(slotType) + "  " + definition.DisplayName, string.Empty,
                        equipmentPanel.transform, slotPosition, new Vector2(196, 68), selected ? amber : cyan, true,
                        () => { selectedRogueInventoryId = instanceId; Invalidate(false); },
                        iconPath: FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
                }
                loadoutEquipmentSlotRects[slotType] = slot.GetComponent<RectTransform>();
                loadoutEquipmentDropOverlays[slotType] = CreateEquipmentDropOverlay(slot.transform);
                if (!string.IsNullOrEmpty(instanceId))
                {
                    if (slot.GetComponent<CanvasGroup>() == null) slot.AddComponent<CanvasGroup>();
                    RogueLoadoutDragHandler drag = slot.AddComponent<RogueLoadoutDragHandler>();
                    drag.Configure(eventData => BeginEquippedLoadoutDrag(slotType, instanceId, slot, eventData), UpdateLoadoutDrag,
                        EndLoadoutDrag, RotateLoadoutDragPreview);
                }
                BindContentHover(slot, "装备", definition == null ? EquipmentSlotLabel(slotType) : definition.DisplayName,
                    definition == null ? "选择背包中的匹配装备。" : RogueEquipmentDetailBody(runtime, instanceId, false), selected ? amber : cyan,
                    definition == null ? EquipmentIconPath(slotType) : FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
            }

            DrawLoadoutQuickbar(equipmentPanel.transform, runtime);
            DrawLoadoutBackpack(parent, runtime, items, new Vector2(1206, -96), new Vector2(650, 794));
        }

        private void DrawLoadoutCharacter(Transform parent, RogueEquipmentRuntime runtime)
        {
            RogueliteMapRun run = bootstrap.CurrentMapRun ?? bootstrap.ArchivedMapRun;
            RogueRunDto dto = run.RogueRunState;
            Label("角色栏标题", "角色状态", parent, new Vector2(24, -18), new Vector2(300, 42), 26, amber, TextAnchor.MiddleLeft);
            GameObject portrait = Panel("角色立绘框", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, -72), new Vector2(370, 326), FormalUiTheme.SurfaceRaised);
            GameObject heroObject = Create("角色像素像", portrait.transform);
            RectTransform heroRect = heroObject.AddComponent<RectTransform>();
            heroRect.anchorMin = heroRect.anchorMax = heroRect.pivot = new Vector2(.5f, .5f);
            heroRect.anchoredPosition = new Vector2(0, -8); heroRect.sizeDelta = new Vector2(256, 256);
            Image hero = heroObject.AddComponent<Image>(); hero.sprite = Resources.Load<Sprite>(FormalArtRegistry.UnitPath("hero"));
            hero.preserveAspect = true; hero.raycastTarget = false;
            Label("角色称谓", "学院学员", portrait.transform, new Vector2(20, -278), new Vector2(330, 32), 20, text, TextAnchor.MiddleCenter);

            MetricChip(parent, 24, -424, "生命", PlayerFacingCopy.CurrentAndMaximum(dto.CurrentHealth, 18), FormalUiTheme.Health,
                FormalArtRegistry.ResourceMetricPath("health"), 190);
            MetricChip(parent, 226, -424, "魔力", PlayerFacingCopy.CurrentAndMaximum(dto.CurrentMana, RogueRuntimeConstants.MaximumPersonalMana), cyan,
                FormalArtRegistry.ResourceMetricPath("mana"), 190);
            MetricChip(parent, 24, -496, "金币", dto.Gold.ToString(), amber, FormalArtRegistry.ResourceMetricPath("gold"), 190);
            MetricChip(parent, 226, -496, "贡献", dto.StageContribution.ToString(), safe,
                FormalArtRegistry.ResourceMetricPath("contribution"), 190);

            RogueEquipmentInstance equipment = runtime.EquipmentItem(selectedRogueInventoryId);
            RogueTacticalItemInstance tactical = runtime.TacticalItem(selectedRogueInventoryId);
            string selectedName = equipment != null ? runtime.DefinitionFor(equipment.InstanceId).DisplayName :
                tactical != null ? runtime.TacticalDefinitionFor(tactical.InstanceId).DisplayName : "尚未选择物品";
            Label("当前选择标题", "当前选择", parent, new Vector2(24, -584), new Vector2(180, 28), 17, amber, TextAnchor.MiddleLeft);
            Label("当前选择名称", selectedName, parent, new Vector2(24, -616), new Vector2(392, 42), 23, text, TextAnchor.MiddleLeft);
            bool inBackpack = runtime.Backpack.ContainsKey(selectedRogueInventoryId);
            if (inBackpack)
                ActionButton("旋转", "R", parent, new Vector2(24, -676), new Vector2(188, 58), cyan, true,
                    () => bootstrap.RotateRogueBackpackItem(selectedRogueInventoryId), iconPath: FormalArtRegistry.ItemPath("inventory_rotate"));
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(equipment.InstanceId);
                bool equippedNow = runtime.Equipped.Values.Contains(equipment.InstanceId);
                OCC.Combat.Roguelite.EquipmentSlot equippedSlot = runtime.Equipped.FirstOrDefault(pair => pair.Value == equipment.InstanceId).Key;
                ActionButton(equippedNow ? "卸下" : "装备", string.Empty, parent, new Vector2(226, -676), new Vector2(190, 58), amber, true,
                    () => { if (equippedNow) bootstrap.UnequipRogueEquipment(equippedSlot); else bootstrap.EquipRogueEquipment(equipment.InstanceId, PreferredEquipSlot(runtime, definition)); },
                    iconPath: FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
            }
        }

        private void DrawLoadoutQuickbar(Transform parent, RogueEquipmentRuntime runtime)
        {
            Label("快捷栏标题", "战斗快捷栏", parent, new Vector2(28, -372), new Vector2(320, 38), 24, safe, TextAnchor.MiddleLeft);
            Label("快捷栏说明", "战斗中按 1–4 使用；点击空位可关联当前选中的战术道具", parent,
                new Vector2(28, -408), new Vector2(640, 28), 15, muted, TextAnchor.MiddleLeft);
            for (int index = 0; index < RogueRuntimeConstants.ItemQuickbarSize; index++)
            {
                int slotIndex = index;
                string id = runtime.ItemQuickbarInstanceIds[index];
                RogueTacticalItemInstance item = runtime.TacticalItem(id);
                TacticalItemDefinition definition = runtime.TacticalDefinitionFor(id);
                ActionButton((index + 1) + "  " + (definition == null ? "空" : definition.DisplayName),
                    item == null ? string.Empty : PlayerFacingCopy.RemainingAndTotal(item.ChargesCurrent, item.ChargesMaximum, " 次"), parent,
                    new Vector2(28 + index * 160, -452), new Vector2(148, 96), safe, true,
                    () =>
                    {
                        if (runtime.TacticalItem(selectedRogueInventoryId) != null) bootstrap.AssignRogueQuickbar(selectedRogueInventoryId, slotIndex);
                        else if (item != null) { selectedRogueInventoryId = id; Invalidate(false); }
                    }, iconPath: item == null ? FormalArtRegistry.ItemPath("category_container") : FormalArtRegistry.ItemPath(item.DefinitionId));
            }
            Label("整备提示", loadoutInteractionMessage, parent, new Vector2(28, -572), new Vector2(640, 30), 15, muted, TextAnchor.MiddleLeft);
        }

        private static IReadOnlyList<OCC.Combat.Roguelite.EquipmentSlot> EquipmentSlotsForPresentation()
            => EquipmentSlotRules.ActiveSlots;

        private void DrawLoadoutBackpack(Transform parent, RogueEquipmentRuntime runtime, IReadOnlyList<RogueInventoryItemPresentation> items, Vector2 position, Vector2 size)
        {
            GameObject backpackPanel = Panel("背包工作区", parent, new Vector2(0, 1), new Vector2(0, 1), position, size, FormalUiTheme.Surface);
            Label("背包标题", "背包 10×6", backpackPanel.transform, new Vector2(24, -18), new Vector2(440, 40), 25, safe, TextAnchor.MiddleLeft);
            Label("背包提示", "拖拽整理　R 键或右键旋转", backpackPanel.transform, new Vector2(24, -54),
                new Vector2(Mathf.Max(360f, size.x - 48f), 28f), 15, muted, TextAnchor.MiddleLeft);
            Vector2 gridOrigin = new Vector2(44, -88);
            GameObject gridObject = Panel("战外背包网格", backpackPanel.transform, new Vector2(0, 1), new Vector2(0, 1), gridOrigin,
                new Vector2(RogueLoadoutScreenGridPresentation.Columns * LoadoutCellSize,
                    RogueLoadoutScreenGridPresentation.Rows * LoadoutCellSize), FormalUiTheme.WithAlpha(FormalUiTheme.Surface, .5f));
            loadoutGridRect = gridObject.GetComponent<RectTransform>();
            for (int y = 0; y < RogueLoadoutScreenGridPresentation.Rows; y++)
            for (int x = 0; x < RogueLoadoutScreenGridPresentation.Columns; x++)
                BackpackInsetCell(gridObject.transform, x, y);
            foreach (RogueInventoryItemPresentation item in items)
            {
                bool selected = item.InstanceId == selectedRogueInventoryId;
                RogueLoadoutGridPoint screenPosition = RogueLoadoutScreenGridPresentation.FromRuntime(item.X, item.Y);
                RogueLoadoutGridPoint screenFootprint = RogueLoadoutScreenGridPresentation.FootprintFromRuntime(
                    new RogueLoadoutGridPoint(item.Width, item.Height));
                GameObject itemButton = InventoryGridButton(item, gridObject.transform,
                    new Vector2(screenPosition.X * LoadoutCellSize, -screenPosition.Y * LoadoutCellSize),
                    new Vector2(screenFootprint.X * LoadoutCellSize - 3, screenFootprint.Y * LoadoutCellSize - 3),
                    selected ? amber : item.IsEquipment ? cyan : safe, () => { selectedRogueInventoryId = item.InstanceId; Invalidate(false); });
                RogueLoadoutDragHandler drag = itemButton.AddComponent<RogueLoadoutDragHandler>();
                drag.Configure(eventData => BeginLoadoutDrag(item, itemButton, eventData), UpdateLoadoutDrag,
                    EndLoadoutDrag, RotateLoadoutDragPreview);
                BindContentHover(itemButton, item.IsEquipment ? "装备" : "战术道具", item.DisplayName,
                    RogueInventoryDetailBody(runtime, item.InstanceId, false), selected ? amber : item.IsEquipment ? cyan : safe, RogueInventoryIconPath(item));
            }
            if (items.Count == 0)
                FormalUiEffects.AddEmptyIllustration(backpackPanel.transform, "empty_inventory_pouch", new Vector2(324, -252), 128f);
            Label("背包交互状态", loadoutInteractionMessage, backpackPanel.transform, new Vector2(24, -446),
                new Vector2(size.x - 48f, 30), 15, muted, TextAnchor.MiddleLeft);
            RogueEquipmentInstance selectedEquipment = runtime.EquipmentItem(selectedRogueInventoryId);
            RogueTacticalItemInstance selectedTactical = runtime.TacticalItem(selectedRogueInventoryId);
            string detail = selectedEquipment != null ? RogueEquipmentDetailBody(runtime, selectedEquipment.InstanceId, false) :
                selectedTactical != null ? RogueInventoryDetailBody(runtime, selectedTactical.InstanceId, false) : "选择一件背包物品查看详情。";
            Text detailLabel = Label("背包选中详情", detail, backpackPanel.transform, new Vector2(24, -492),
                new Vector2(size.x - 48f, 250), 15, text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(detailLabel);
        }

        private void DrawEmbeddedQuickbar(Transform parent, RogueEquipmentRuntime runtime, Vector2 position)
        {
            Label("背包内战术栏标题", "背包快捷使用区", parent, position, new Vector2(590, 38), 24, safe, TextAnchor.MiddleLeft);
            Label("背包内战术栏说明", "战斗中按 1–4 使用；槽位只引用背包中的战术道具", parent,
                position + new Vector2(0, -38), new Vector2(590, 30), 15, muted, TextAnchor.MiddleLeft);
            string[] quickbar = runtime.ItemQuickbarInstanceIds;
            for (int index = 0; index < RogueRuntimeConstants.ItemQuickbarSize; index++)
            {
                int slotIndex = index;
                string id = quickbar[index];
                RogueTacticalItemInstance item = runtime.TacticalItem(id);
                TacticalItemDefinition definition = runtime.TacticalDefinitionFor(id);
                GameObject slot = ActionButton((index + 1) + "  " + (definition == null ? "空" : definition.DisplayName),
                    item == null ? "从背包选择战术道具" : PlayerFacingCopy.RemainingAndTotal(item.ChargesCurrent, item.ChargesMaximum, " 次"),
                    parent, position + new Vector2((index % 2) * 300, -82 - (index / 2) * 108), new Vector2(280, 92), safe, true,
                    () =>
                    {
                        if (runtime.TacticalItem(selectedRogueInventoryId) != null) bootstrap.AssignRogueQuickbar(selectedRogueInventoryId, slotIndex);
                        else if (item != null) { selectedRogueInventoryId = id; Invalidate(false); }
                    }, iconPath: item == null ? FormalArtRegistry.ItemPath("category_container") : FormalArtRegistry.ItemPath(item.DefinitionId));
                if (item != null)
                    BindContentHover(slot, "战术道具", definition.DisplayName, RogueInventoryDetailBody(runtime, id, false), safe,
                        FormalArtRegistry.ItemPath(item.DefinitionId));
            }
        }

        private void DrawSelectedInventoryActions(Transform parent, RogueEquipmentRuntime runtime, Vector2 position)
        {
            RogueEquipmentInstance equipment = runtime.EquipmentItem(selectedRogueInventoryId);
            RogueTacticalItemInstance tactical = runtime.TacticalItem(selectedRogueInventoryId);
            string name = equipment != null ? runtime.DefinitionFor(equipment.InstanceId).DisplayName :
                tactical != null ? runtime.TacticalDefinitionFor(tactical.InstanceId).DisplayName : "尚未选择物品";
            Label("当前选择标题", "当前选择", parent, position, new Vector2(180, 34), 18, amber, TextAnchor.MiddleLeft);
            Label("当前选择名称", name, parent, position + new Vector2(0, -38), new Vector2(590, 40), 24, text, TextAnchor.MiddleLeft);
            string fullDetail = equipment != null ? RogueEquipmentDetailBody(runtime, equipment.InstanceId, false) :
                tactical != null ? RogueInventoryDetailBody(runtime, tactical.InstanceId, false) : "在背包或装备槽中选择一件物品。";
            Label("当前选择完整详情", fullDetail, parent, position + new Vector2(0, -78), new Vector2(590, 96), 14, muted, TextAnchor.UpperLeft);
            if (equipment == null && tactical == null) return;
            bool inBackpack = runtime.Backpack.ContainsKey(selectedRogueInventoryId);
            if (inBackpack)
                ActionButton("旋转", "R", parent, position + new Vector2(0, -180), new Vector2(280, 52), cyan, true,
                    () => bootstrap.RotateRogueBackpackItem(selectedRogueInventoryId), iconPath: FormalArtRegistry.ItemPath("inventory_rotate"));
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(equipment.InstanceId);
                bool equippedNow = runtime.Equipped.Values.Contains(equipment.InstanceId);
                OCC.Combat.Roguelite.EquipmentSlot equippedSlot = runtime.Equipped.FirstOrDefault(pair => pair.Value == equipment.InstanceId).Key;
                ActionButton(equippedNow ? "卸下" : "装备", string.Empty, parent, position + new Vector2(300, -180), new Vector2(280, 52), amber, true,
                    () => { if (equippedNow) bootstrap.UnequipRogueEquipment(equippedSlot); else bootstrap.EquipRogueEquipment(equipment.InstanceId, PreferredEquipSlot(runtime, definition)); },
                    iconPath: FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
            }
        }

        private void DrawTacticalLoadout(Transform parent, RogueEquipmentRuntime runtime, IReadOnlyList<RogueInventoryItemPresentation> items)
        {
            GameObject quickbarPanel = Panel("战术工作区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -104), new Vector2(600, 620), FormalUiTheme.Surface);
            Label("战术标题", "战术快捷栏", quickbarPanel.transform, new Vector2(28, -20), new Vector2(360, 42), 26, safe, TextAnchor.MiddleLeft);
            Label("战术说明", "先选中背包道具，再点击槽位关联", quickbarPanel.transform, new Vector2(28, -62), new Vector2(520, 32), 16, muted, TextAnchor.MiddleLeft);
            for (int index = 0; index < RogueRuntimeConstants.ItemQuickbarSize; index++)
            {
                int slotIndex = index; string id = runtime.ItemQuickbarInstanceIds[index]; RogueTacticalItemInstance item = runtime.TacticalItem(id);
                GameObject slot = ActionButton((index + 1) + "  " + (item == null ? "空" : runtime.TacticalDefinitionFor(id).DisplayName), item == null ? string.Empty : PlayerFacingCopy.RemainingAndTotal(item.ChargesCurrent, item.ChargesMaximum),
                    quickbarPanel.transform, new Vector2(28 + (index % 2) * 276, -122 - (index / 2) * 116), new Vector2(260, 96), safe, true,
                    () => { if (runtime.TacticalItem(selectedRogueInventoryId) != null) bootstrap.AssignRogueQuickbar(selectedRogueInventoryId, slotIndex); else if (!string.IsNullOrEmpty(id)) { selectedRogueInventoryId = id; Invalidate(false); } },
                    iconPath: item == null ? FormalArtRegistry.ItemPath("category_container") : FormalArtRegistry.ItemPath(item.DefinitionId));
                BindHover(slot, "战术栏 " + (index + 1), item == null ? "选择背包中的战术道具。" : RogueInventoryDetail(runtime, id), safe);
            }
            Label("战术规则", "战斗中按 1–4 快速使用；调整位置不会消耗道具。", quickbarPanel.transform,
                new Vector2(28, -390), new Vector2(540, 72), 17, muted, TextAnchor.UpperLeft);

            DrawLoadoutBackpack(parent, runtime, items, new Vector2(656, -104), new Vector2(420, 620));
            DrawRogueInventoryDetails(parent, runtime, new Vector2(1096, -104), new Vector2(628, 620));
        }

        private void DrawSpellLoadout(Transform parent, RogueRunDto dto, IReadOnlyDictionary<string, SpellDefinition> spells)
        {
            GameObject spellPanel = Panel("术式工作区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -104), new Vector2(1060, 620), FormalUiTheme.Surface);
            Label("术式标题", "8 个术式槽", spellPanel.transform, new Vector2(28, -20), new Vector2(500, 42), 26, amber, TextAnchor.MiddleLeft);
            Label("术式说明", "选择槽位后在右侧查看完整详情、替换或移除；数字就是战斗快捷键", spellPanel.transform, new Vector2(28, -62), new Vector2(980, 32), 16, muted, TextAnchor.MiddleLeft);
            selectedLoadoutSpellIndex = Mathf.Clamp(selectedLoadoutSpellIndex, 0, RogueRuntimeConstants.SpellSlotCount - 1);
            for (int index = 0; index < RogueRuntimeConstants.SpellSlotCount; index++)
            {
                int slotIndex = index;
                string id = dto.EquippedSpellIds[index];
                SpellDefinition spell = !string.IsNullOrEmpty(id) && spells.TryGetValue(id, out SpellDefinition foundSpell) ? foundSpell : null;
                string name = spell == null ? "空槽" : spell.DisplayName;
                string detail = spell == null ? "尚未获得术式" : "行动 " + spell.ActionPointCost + "　魔力 " + spell.ManaCost + "　冷却 " + spell.CooldownOwnTurns;
                bool selected = index == selectedLoadoutSpellIndex;
                GameObject slot = ActionButton((index + 1) + "  " + name, detail, spellPanel.transform,
                    new Vector2(28 + (index % 2) * 506, -106 - (index / 2) * 124), new Vector2(482, 108), selected ? amber : cyan, true,
                    () => { selectedLoadoutSpellIndex = slotIndex; selectedLoadoutSpellId = id; Invalidate(false); }, iconPath: RogueSpellIconPath(id));
                if (spell != null) BindContentHover(slot, "个人术式", name, SpellTooltipBody(spell), selected ? amber : cyan, RogueSpellIconPath(id));
            }
            DrawSpellDetails(parent, dto, spells, new Vector2(1116, -104));
        }

        private void DrawSpellDetails(Transform parent, RogueRunDto dto, IReadOnlyDictionary<string, SpellDefinition> spells, Vector2 position)
        {
            GameObject detailPanel = Panel("术式详情", parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(608, 620), FormalUiTheme.Surface);
            string equippedId = dto.EquippedSpellIds[selectedLoadoutSpellIndex];
            string[] learned = dto.MasteredSpellIds.Where(spells.ContainsKey).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (string.IsNullOrEmpty(selectedLoadoutSpellId) || !spells.ContainsKey(selectedLoadoutSpellId))
                selectedLoadoutSpellId = !string.IsNullOrEmpty(equippedId) ? equippedId : learned.FirstOrDefault();
            string id = selectedLoadoutSpellId;
            SpellDefinition spell = !string.IsNullOrEmpty(id) && spells.TryGetValue(id, out SpellDefinition found) ? found : null;
            if (spell == null)
            {
                Label("空术式", "空术式槽", detailPanel.transform, new Vector2(32, -30), new Vector2(540, 52), 28, muted, TextAnchor.MiddleLeft);
                Label("空术式说明", "尚无可选术式；获得术式后可在这里装入槽位。", detailPanel.transform, new Vector2(32, -100), new Vector2(540, 80), 18, muted, TextAnchor.UpperLeft);
                return;
            }
            Image icon = FormalUiKit.TopLeftIconSlot("术式图标", detailPanel.transform, Resources.Load<Sprite>(RogueSpellIconPath(id)), new Vector2(32, -30));
            int detailIconSize = FormalUiKit.IntegerSpriteSize(icon.sprite, 88f); icon.rectTransform.sizeDelta = new Vector2(detailIconSize, detailIconSize);
            Label("术式名称", spell.DisplayName, detailPanel.transform, new Vector2(140, -28), new Vector2(430, 52), 30, text, TextAnchor.MiddleLeft);
            Label("术式槽位", "候选术式　目标槽位 " + (selectedLoadoutSpellIndex + 1), detailPanel.transform, new Vector2(140, -78), new Vector2(430, 32), 17, amber, TextAnchor.MiddleLeft);
            Label("资源标题", "施放消耗与循环", detailPanel.transform, new Vector2(32, -132), new Vector2(540, 40), 20, amber, TextAnchor.MiddleLeft);
            DetailIconMetric(detailPanel.transform, "行动", spell.ActionPointCost.ToString(), FormalArtRegistry.SemanticPath("action"), new Vector2(32, -176), "每次施放消耗的行动点", amber, 164f, 64f);
            DetailIconMetric(detailPanel.transform, "魔力", spell.ManaCost.ToString(), FormalArtRegistry.SemanticPath("aether"), new Vector2(212, -176), "每次施放消耗的个人魔力", cyan, 164f, 64f);
            DetailIconMetric(detailPanel.transform, "冷却", spell.CooldownOwnTurns.ToString(), FormalArtRegistry.SemanticPath("notice"), new Vector2(392, -176), "以自身回合计算；0 表示无冷却", safe, 164f, 64f);
            Label("目标标题", "施放目标", detailPanel.transform, new Vector2(32, -252), new Vector2(160, 40), 20, amber, TextAnchor.MiddleLeft);
            Text target = Label("目标规则", SpellTargetSummary(spell), detailPanel.transform, new Vector2(180, -252), new Vector2(392, 72), 18, text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(target);
            Label("规则标题", "术式效果", detailPanel.transform, new Vector2(32, -332), new Vector2(300, 40), 20, amber, TextAnchor.MiddleLeft);
            Text rules = Label("术式规则", string.Join("\n", spell.Rules.Take(5).Select(SpellRuleText)), detailPanel.transform, new Vector2(32, -376), new Vector2(540, 156), 18, text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(rules);
            ActionButton("上一术式", string.Empty, detailPanel.transform, new Vector2(32, -536), new Vector2(124, 52), cyan, learned.Length > 1,
                () => CycleLoadoutSpell(learned, -1));
            ActionButton(equippedId == id ? "已装入" : "装入或替换", "槽位 " + (selectedLoadoutSpellIndex + 1), detailPanel.transform, new Vector2(168, -536), new Vector2(168, 52), amber,
                equippedId != id, () => bootstrap.AssignRogueSpell(id, selectedLoadoutSpellIndex));
            ActionButton("移除槽位", string.Empty, detailPanel.transform, new Vector2(348, -536), new Vector2(112, 52), danger,
                !string.IsNullOrEmpty(equippedId), () => bootstrap.AssignRogueSpell(string.Empty, selectedLoadoutSpellIndex));
            ActionButton("下一术式", string.Empty, detailPanel.transform, new Vector2(472, -536), new Vector2(104, 52), cyan, learned.Length > 1,
                () => CycleLoadoutSpell(learned, 1));
        }

        private void CycleLoadoutSpell(IReadOnlyList<string> learned, int delta)
        {
            if (learned == null || learned.Count == 0) return;
            int current = learned.ToList().IndexOf(selectedLoadoutSpellId);
            selectedLoadoutSpellId = learned[(current + delta + learned.Count) % learned.Count];
            Invalidate(false);
        }

        public static string SpellTooltipBody(SpellDefinition spell)
        {
            if (spell == null) return "尚未获得术式。";
            string cooldown = spell.CooldownOwnTurns <= 0 ? "无冷却" : spell.CooldownOwnTurns + " 个自身回合";
            string effects = string.Join("\n", spell.Rules.Take(4).Select(SpellRuleText));
            return "消耗　" + spell.ActionPointCost + " 行动点　" + spell.ManaCost + " 个人魔力" +
                "\n循环　" + cooldown +
                "\n目标　" + SpellTargetSummary(spell) +
                "\n效果\n" + effects;
        }

        private static string SpellTargetSummary(SpellDefinition spell)
        {
            if (spell == null) return "—";
            string target;
            switch (spell.Targeting)
            {
                case "self": case "Self": target = "自身"; break;
                case "adjacent_visible_enemy": case "AdjacentEnemy": target = "相邻可见敌人"; break;
                case "visible_enemy": case "Enemy": target = "可见敌人"; break;
                case "adjacent_hittable": target = "相邻可见敌人或可破坏物"; break;
                case "visible_hittable": target = "可见敌人或可破坏物"; break;
                case "AllyOrSelf": target = "自身或友军"; break;
                case "Unit": target = "任意单位"; break;
                case "EmptyCell": target = "空地格"; break;
                case "BurningUnit": target = "燃烧单位"; break;
                case "BurningCell": target = "燃烧地格"; break;
                case "Destructible": target = "可破坏物件"; break;
                case "Hittable": target = "敌方单位或可破坏物件"; break;
                case "AdjacentBurningEnemy": target = "相邻燃烧敌人"; break;
                case "BurningOrArmorBrokenEnemy": target = "燃烧或破甲敌人"; break;
                default: target = "指定目标"; break;
            }
            if (spell.Range > 0) target += "　射程 " + spell.Range + " 格";
            if (spell.Targeting != "self" && spell.Targeting != "Self")
                target += spell.LineOfSightRule == "not_required" ? "　无需视线" : "　需要视线";
            return target;
        }

        private static string SpellRuleText(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule)) return "—";
            string[] parts = rule.Split(':');
            if (parts.Length >= 3 && parts[0] == "damage")
                return "造成 " + parts[2] + " 点" + (parts[1] == "fire" ? "火焰" : parts[1] == "physical" ? "物理" : string.Empty) + "伤害";
            if (parts.Length >= 2 && parts[0] == "grant_shield") return "获得 " + parts[1] + " 点普通盾";
            if (parts.Length >= 2 && parts[0] == "restore_mana") return "恢复 " + parts[1] + " 点个人魔力";
            if (rule == "apply_break_stance") return "对目标施加破势";
            if (rule == "grant_shield_before_ranged") return "承受远程伤害前获得护盾";
            if (rule == "clear_one_self_status") return "清除自身 1 个负面状态";
            if (rule.StartsWith("legacy_rule:", StringComparison.Ordinal))
            {
                string kind = rule.Substring("legacy_rule:".Length);
                switch (kind)
                {
                    case "Damage": return "造成术式伤害";
                    case "WeaponDamage": return "强化下一次武器伤害";
                    case "ApplyBurning": return "施加燃烧";
                    case "CreateFireground": return "生成燃烧地面";
                    case "RestoreShield": return "恢复普通盾";
                    case "RestoreMana": return "恢复个人魔力";
                    case "Push": return "推动目标";
                    case "MoveSource": return "移动施术者";
                    default: return "触发专项术式效果";
                }
            }
            return "触发术式效果";
        }

        private void BeginLoadoutDrag(RogueInventoryItemPresentation item, GameObject source, PointerEventData eventData)
        {
            if (item == null || source == null || loadoutGridRect == null || loadoutDragRuntime == null) return;
            selectedRogueInventoryId = item.InstanceId;
            loadoutDragId = item.InstanceId;
            loadoutDragEquippedSlot = null;
            loadoutDragRotated = item.Rotated;
            loadoutLastPointer = eventData.position;
            if (!TryLoadoutLocalPointer(eventData.position, out Vector2 local)) { CancelLoadoutDrag(); return; }
            RogueLoadoutGridPoint pointerCell = RogueLoadoutDragPresentation.AnchorForLocalPointer(local.x, local.y, LoadoutCellSize, 0, 0);
            RogueLoadoutGridPoint screenPosition = RogueLoadoutScreenGridPresentation.FromRuntime(item.X, item.Y);
            loadoutGrabOffset = new Vector2Int(pointerCell.X - screenPosition.X, pointerCell.Y - screenPosition.Y);
            loadoutDragSource = source.GetComponent<CanvasGroup>();
            if (loadoutDragSource == null) { CancelLoadoutDrag(); return; }
            loadoutDragSource.alpha = 0f;
            loadoutDragSource.blocksRaycasts = true;
            loadoutDragGhost = Panel("战外背包拖拽预览", loadoutGridRect, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.one,
                FormalUiTheme.WithAlpha(cyan, .42f));
            FormalUiKit.FocusFrame(loadoutDragGhost.transform);
            loadoutDragGhost.GetComponent<Image>().raycastTarget = false;
            loadoutInteractionMessage = "拖拽中　R 键或右键旋转　松开左键放置";
            UpdateLoadoutDrag(eventData);
        }

        private void BeginEquippedLoadoutDrag(OCC.Combat.Roguelite.EquipmentSlot slot, string instanceId, GameObject source, PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(instanceId) || source == null || loadoutGridRect == null || loadoutDragRuntime == null) return;
            selectedRogueInventoryId = instanceId;
            loadoutDragId = instanceId;
            loadoutDragEquippedSlot = slot;
            loadoutDragRotated = false;
            loadoutGrabOffset = Vector2Int.zero;
            loadoutLastPointer = eventData.position;
            loadoutDragSource = source.GetComponent<CanvasGroup>();
            if (loadoutDragSource == null) { CancelLoadoutDrag(); return; }
            loadoutDragSource.alpha = 0f;
            loadoutDragSource.blocksRaycasts = true;
            loadoutDragGhost = Panel("战外背包拖拽预览", loadoutGridRect, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.one,
                FormalUiTheme.WithAlpha(cyan, .42f));
            FormalUiKit.FocusFrame(loadoutDragGhost.transform);
            loadoutDragGhost.GetComponent<Image>().raycastTarget = false;
            loadoutDragGhost.SetActive(false);
            loadoutInteractionMessage = "拖到背包中的目标格卸下　R 键或右键旋转";
            UpdateLoadoutDrag(eventData);
        }

        private void UpdateLoadoutDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(loadoutDragId) || eventData == null) return;
            loadoutLastPointer = eventData.position;
            UpdateLoadoutDragPreview();
        }

        private void RotateLoadoutDragPreview()
        {
            if (string.IsNullOrEmpty(loadoutDragId)) return;
            loadoutDragRotated = !loadoutDragRotated;
            loadoutInteractionMessage = loadoutDragRotated ? "已经横过来了，松开左键放下" : "已经竖回来了，松开左键放下";
            UpdateLoadoutDragPreview();
        }

        private void UpdateLoadoutDragPreview()
        {
            if (loadoutDragGhost == null || loadoutDragRuntime == null) return;
            ClearEquipmentDropHighlights();
            if (!loadoutDragEquippedSlot.HasValue && TryEquipmentSlotAtPointer(loadoutLastPointer, out OCC.Combat.Roguelite.EquipmentSlot targetSlot))
            {
                bool compatible = loadoutDragRuntime.CanEquipOrReplace(loadoutDragId, targetSlot);
                Image overlay = loadoutEquipmentDropOverlays[targetSlot];
                overlay.color = FormalUiTheme.WithAlpha(compatible ? safe : danger, .32f);
                overlay.gameObject.SetActive(true);
                loadoutDragGhost.SetActive(false);
                loadoutInteractionMessage = compatible ? "松开以装备到" + EquipmentSlotLabel(targetSlot) + "；原装备会安全回包" : "该装备不能放入此槽";
                return;
            }
            if (!IsPointerOverBackpack(loadoutLastPointer) || !TryLoadoutLocalPointer(loadoutLastPointer, out Vector2 local))
            {
                loadoutDragGhost.SetActive(false);
                return;
            }
            loadoutDragGhost.SetActive(true);
            int grabX = loadoutDragEquippedSlot.HasValue ? 0 : loadoutGrabOffset.x;
            int grabY = loadoutDragEquippedSlot.HasValue ? 0 : loadoutGrabOffset.y;
            RogueLoadoutGridPoint anchor = RogueLoadoutDragPresentation.AnchorForLocalPointer(local.x, local.y, LoadoutCellSize, grabX, grabY);
            RogueLoadoutGridPoint footprint = LoadoutFootprint(loadoutDragRuntime, loadoutDragId, loadoutDragRotated);
            RogueLoadoutGridPoint runtimeAnchor = RogueLoadoutScreenGridPresentation.ToRuntime(anchor.X, anchor.Y);
            RectTransform rect = loadoutDragGhost.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(anchor.X * LoadoutCellSize, -anchor.Y * LoadoutCellSize);
            rect.sizeDelta = new Vector2(footprint.X * LoadoutCellSize - 3, footprint.Y * LoadoutCellSize - 3);
            bool legal = loadoutDragEquippedSlot.HasValue
                ? loadoutDragRuntime.CanUnequipToBackpack(loadoutDragEquippedSlot.Value, runtimeAnchor.X, runtimeAnchor.Y, loadoutDragRotated)
                : loadoutDragRuntime.CanMoveBackpack(loadoutDragId, runtimeAnchor.X, runtimeAnchor.Y, loadoutDragRotated);
            Image image = loadoutDragGhost.GetComponent<Image>();
            image.color = FormalUiTheme.WithAlpha(legal ? cyan : danger, .42f);
            loadoutDragGhost.transform.SetAsLastSibling();
        }

        private void EndLoadoutDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(loadoutDragId)) return;
            loadoutLastPointer = eventData.position;
            bool submitted = false;
            bool succeeded = false;
            if (!loadoutDragEquippedSlot.HasValue && TryEquipmentSlotAtPointer(eventData.position, out OCC.Combat.Roguelite.EquipmentSlot targetSlot))
            {
                submitted = true;
                succeeded = bootstrap.EquipOrReplaceRogueEquipment(loadoutDragId, targetSlot);
                loadoutInteractionMessage = succeeded ? "已装备到" + EquipmentSlotLabel(targetSlot) : "无法装备，物品保持原位";
            }
            else if (IsPointerOverBackpack(eventData.position) && TryLoadoutLocalPointer(eventData.position, out Vector2 local))
            {
                submitted = true;
                int grabX = loadoutDragEquippedSlot.HasValue ? 0 : loadoutGrabOffset.x;
                int grabY = loadoutDragEquippedSlot.HasValue ? 0 : loadoutGrabOffset.y;
                RogueLoadoutGridPoint anchor = RogueLoadoutDragPresentation.AnchorForLocalPointer(local.x, local.y, LoadoutCellSize, grabX, grabY);
                RogueLoadoutGridPoint runtimeAnchor = RogueLoadoutScreenGridPresentation.ToRuntime(anchor.X, anchor.Y);
                succeeded = loadoutDragEquippedSlot.HasValue
                    ? bootstrap.UnequipRogueEquipmentTo(loadoutDragEquippedSlot.Value, runtimeAnchor.X, runtimeAnchor.Y, loadoutDragRotated)
                    : bootstrap.MoveRogueBackpackItem(loadoutDragId, runtimeAnchor.X, runtimeAnchor.Y, loadoutDragRotated);
                loadoutInteractionMessage = succeeded
                    ? (loadoutDragEquippedSlot.HasValue ? "已卸下到 " : "已移动到 ") + (anchor.X + 1) + "," + (anchor.Y + 1)
                    : "不可放置，物品保持原位";
            }
            if (!submitted) loadoutInteractionMessage = "已取消拖拽，物品保持原位";
            ClearLoadoutDrag();
            Invalidate(false);
        }

        private bool IsPointerOverBackpack(Vector2 screenPoint)
            => loadoutGridRect != null && RectTransformUtility.RectangleContainsScreenPoint(loadoutGridRect, screenPoint,
                canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null);

        private bool TryEquipmentSlotAtPointer(Vector2 screenPoint, out OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            foreach (KeyValuePair<OCC.Combat.Roguelite.EquipmentSlot, RectTransform> pair in loadoutEquipmentSlotRects)
                if (pair.Value != null && RectTransformUtility.RectangleContainsScreenPoint(pair.Value, screenPoint, eventCamera))
                { slot = pair.Key; return true; }
            slot = default;
            return false;
        }

        private static Image CreateEquipmentDropOverlay(Transform parent)
        {
            GameObject overlayObject = Create("装备槽拖入反馈", parent);
            RectTransform rect = overlayObject.AddComponent<RectTransform>(); Stretch(rect);
            Image overlay = overlayObject.AddComponent<Image>(); overlay.raycastTarget = false;
            overlay.color = Color.clear; overlayObject.SetActive(false);
            return overlay;
        }

        private void ClearEquipmentDropHighlights()
        {
            foreach (Image overlay in loadoutEquipmentDropOverlays.Values)
                if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private bool TryLoadoutLocalPointer(Vector2 screenPoint, out Vector2 local)
        {
            local = default;
            return loadoutGridRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(loadoutGridRect, screenPoint,
                canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out local);
        }

        private static RogueLoadoutGridPoint LoadoutFootprint(RogueEquipmentRuntime runtime, string instanceId, bool rotated)
        {
            EquipmentDefinition equipment = runtime.DefinitionFor(instanceId);
            TacticalItemDefinition tactical = runtime.TacticalDefinitionFor(instanceId);
            RogueLoadoutGridPoint runtimeFootprint = RogueLoadoutDragPresentation.Footprint(
                equipment?.Width ?? tactical.Width, equipment?.Height ?? tactical.Height, rotated);
            return RogueLoadoutScreenGridPresentation.FootprintFromRuntime(runtimeFootprint);
        }

        private void CancelLoadoutDrag()
        {
            ClearLoadoutDrag();
            Invalidate(false);
        }

        private void ClearLoadoutDrag()
        {
            if (loadoutDragSource != null) { loadoutDragSource.alpha = 1f; loadoutDragSource.blocksRaycasts = true; }
            if (loadoutDragGhost != null) Destroy(loadoutDragGhost);
            ClearEquipmentDropHighlights();
            loadoutDragId = null; loadoutDragEquippedSlot = null; loadoutDragGhost = null; loadoutDragSource = null;
        }

        private static string EquipmentSlotLabel(OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            switch (slot)
            {
                case OCC.Combat.Roguelite.EquipmentSlot.Weapon: return "武器";
                case OCC.Combat.Roguelite.EquipmentSlot.Head: return "头部";
                case OCC.Combat.Roguelite.EquipmentSlot.Chest: return "胸部";
                case OCC.Combat.Roguelite.EquipmentSlot.Feet: return "足部";
                case OCC.Combat.Roguelite.EquipmentSlot.Backpack: return "背部";
                case OCC.Combat.Roguelite.EquipmentSlot.Ring1: return "戒指一";
                case OCC.Combat.Roguelite.EquipmentSlot.Ring2: return "戒指二";
                case OCC.Combat.Roguelite.EquipmentSlot.Necklace: return "项链";
                case OCC.Combat.Roguelite.EquipmentSlot.CastingUnit: return "施法单元";
                default: return "非活动旧槽位";
            }
        }

        private void DrawRogueInventoryDetails(Transform parent, RogueEquipmentRuntime runtime, Vector2 position, Vector2 size)
        {
            GameObject panel = Panel("选中详情", parent, new Vector2(0, 1), new Vector2(0, 1), position, size, FormalUiTheme.Surface);
            RogueEquipmentInstance equipment = runtime.EquipmentItem(selectedRogueInventoryId);
            RogueTacticalItemInstance tactical = runtime.TacticalItem(selectedRogueInventoryId);
            if (equipment == null && tactical == null)
            {
                Label("空详情", "选择一件物品", panel.transform, new Vector2(32, -30), new Vector2(540, 52), 28, muted, TextAnchor.MiddleLeft);
                Label("空详情说明", "点击背包物品或已装备物品后，在这里完成旋转、装备与卸下。", panel.transform,
                    new Vector2(32, -100), new Vector2(540, 90), 18, muted, TextAnchor.UpperLeft);
                FormalUiEffects.AddEmptyIllustration(panel.transform, "empty_loadout_rack", new Vector2(304, -350), 128f);
                return;
            }
            string name; string iconPath; string type; string effects;
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(equipment.InstanceId);
                name = definition.DisplayName; iconPath = FormalArtRegistry.EquipmentIconPath(definition.DefinitionId); type = EquipmentSlotLabel(definition.Slot) + "　" + RarityLabel(equipment.Rarity);
                effects = RogueEquipmentEffects(definition, equipment);
            }
            else
            {
                TacticalItemDefinition definition = runtime.TacticalDefinitionFor(tactical.InstanceId);
                name = definition.DisplayName; iconPath = FormalArtRegistry.ItemPath(tactical.DefinitionId); type = "战术道具";
                effects = "可关联至 4 格战术栏";
            }
            Image icon = FormalUiKit.TopLeftIconSlot("物品图标", panel.transform, Resources.Load<Sprite>(iconPath), new Vector2(32, -30));
            int itemIconSize = FormalUiKit.IntegerSpriteSize(icon.sprite, 88f); icon.rectTransform.sizeDelta = new Vector2(itemIconSize, itemIconSize);
            Label("名称", name, panel.transform, new Vector2(140, -28), new Vector2(440, 48), 29, text, TextAnchor.MiddleLeft);
            Label("类型", type, panel.transform, new Vector2(140, -78), new Vector2(440, 32), 17, equipment != null ? cyan : safe, TextAnchor.MiddleLeft);
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(equipment.InstanceId);
                DetailIconMetric(panel.transform, "占格", definition.Width + "×" + definition.Height, FormalArtRegistry.ItemPath("category_container"), new Vector2(32, -142), "背包占格", text);
                DetailIconMetric(panel.transform, "重量", definition.BaseWeight.ToString(), FormalArtRegistry.ResourceMetricPath("weight"), new Vector2(216, -142), "装备重量", text);
                DetailIconMetric(panel.transform, "以太负荷", definition.BaseAetherLoad.ToString(), FormalArtRegistry.ResourceMetricPath("aether_load"), new Vector2(400, -142), "装备以太负荷", cyan);
            }
            else
            {
                TacticalItemDefinition definition = runtime.TacticalDefinitionFor(tactical.InstanceId);
                DetailIconMetric(panel.transform, "占格", definition.Width + "×" + definition.Height, FormalArtRegistry.ItemPath("category_container"), new Vector2(32, -142), "背包占格", text);
                DetailIconMetric(panel.transform, "行动点", definition.ActionPointCost.ToString(), FormalArtRegistry.SemanticPath("action"), new Vector2(216, -142), "使用消耗的行动点", amber);
                DetailIconMetric(panel.transform, "次数", PlayerFacingCopy.RemainingAndTotal(tactical.ChargesCurrent, tactical.ChargesMaximum), FormalArtRegistry.ResourceMetricPath("charges"), new Vector2(400, -142), "这次旅程还可使用", safe);
            }
            Label("效果标题", "效果", panel.transform, new Vector2(32, -204), new Vector2(160, 34), 20, amber, TextAnchor.MiddleLeft);
            Label("效果", string.IsNullOrEmpty(effects) ? "—" : effects, panel.transform, new Vector2(32, -250), new Vector2(548, 142), 17, muted, TextAnchor.UpperLeft);
            bool inBackpack = runtime.Backpack.ContainsKey(selectedRogueInventoryId);
            if (inBackpack)
                ActionButton("旋转", "R", panel.transform, new Vector2(32, -424), new Vector2(248, 64), cyan, true,
                    () => bootstrap.RotateRogueBackpackItem(selectedRogueInventoryId), iconPath: FormalArtRegistry.ItemPath("inventory_rotate"));
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(equipment.InstanceId);
                OCC.Combat.Roguelite.EquipmentSlot equippedSlot = runtime.Equipped.FirstOrDefault(pair => pair.Value == equipment.InstanceId).Key;
                bool equippedNow = runtime.Equipped.Values.Contains(equipment.InstanceId);
                ActionButton(equippedNow ? "卸下" : "装备", string.Empty, panel.transform, new Vector2(300, -424), new Vector2(248, 64), amber, true,
                    () => { if (equippedNow) bootstrap.UnequipRogueEquipment(equippedSlot); else bootstrap.EquipRogueEquipment(equipment.InstanceId, PreferredEquipSlot(runtime, definition)); },
                    iconPath: FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
            }
            Label("注意", equipment != null ? "不会损坏　整理界面内可以换装" : "可以把战术道具放进数字快捷位",
                panel.transform, new Vector2(32, -522), new Vector2(548, 54), 16, muted, TextAnchor.MiddleLeft);
        }

        private void TryEquipSelected(RogueEquipmentRuntime runtime, OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            RogueEquipmentInstance selected = runtime.EquipmentItem(selectedRogueInventoryId);
            if (selected != null) bootstrap.EquipRogueEquipment(selected.InstanceId, slot);
        }

        private static OCC.Combat.Roguelite.EquipmentSlot PreferredEquipSlot(RogueEquipmentRuntime runtime, EquipmentDefinition definition)
        {
            if (definition.Slot != OCC.Combat.Roguelite.EquipmentSlot.Ring1) return definition.Slot;
            return string.IsNullOrEmpty(runtime.Equipped[OCC.Combat.Roguelite.EquipmentSlot.Ring1])
                ? OCC.Combat.Roguelite.EquipmentSlot.Ring1 : OCC.Combat.Roguelite.EquipmentSlot.Ring2;
        }

        private void BindHover(GameObject target, string title, string body, Color accent)
        {
            if (target == null || string.IsNullOrWhiteSpace(body)) return;
            FormalHoverTooltipTrigger trigger = target.GetComponent<FormalHoverTooltipTrigger>() ?? target.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () => new FormalTooltipContent(title, body, accent));
        }

        private void BindContentHover(GameObject target, string category, string title, string body, Color accent, string iconPath = "")
        {
            if (target == null || string.IsNullOrWhiteSpace(body)) return;
            FormalHoverTooltipTrigger trigger = target.GetComponent<FormalHoverTooltipTrigger>() ?? target.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () => new FormalTooltipContent(category, title, body, accent, iconPath));
        }

        private void DetailIconMetric(Transform parent, string label, string value, string iconPath, Vector2 position, string tooltipBody, Color accent, float width = 180f, float height = 38f)
        {
            GameObject chip = FormalUiKit.FlatPanel("指标_" + label, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(width, height),
                Color.Lerp(FormalUiTheme.SurfaceRaised, accent, .06f));
            FormalUiKit.ThinFrame(chip.transform, new Vector2(width, height), FormalUiTheme.WithAlpha(accent, .82f), "指标细框");
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            float iconSize = FormalUiKit.IntegerSpriteSize(sprite, 32f);
            Image icon = FormalUiKit.TopLeftIconSlot("图标", chip.transform, sprite, new Vector2(12, -(height - iconSize) * .5f));
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            Text metric = Label("值", label + " " + value, chip.transform, new Vector2(iconSize + 20, -12), new Vector2(width - iconSize - 32, height - 24), 18, accent, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(metric);
            BindHover(chip, label, tooltipBody, accent);
        }

        private static string RogueInventoryDetail(RogueEquipmentRuntime runtime, string instanceId)
            => RogueInventoryDetailBody(runtime, instanceId, true);

        public static string RogueInventoryDetailBody(RogueEquipmentRuntime runtime, string instanceId, bool includeName)
        {
            RogueEquipmentInstance equipment = runtime.EquipmentItem(instanceId);
            if (equipment != null) return RogueEquipmentDetailBody(runtime, instanceId, includeName);
            RogueTacticalItemInstance tactical = runtime.TacticalItem(instanceId); TacticalItemDefinition definition = runtime.TacticalDefinitionFor(instanceId);
            return (includeName ? definition.DisplayName + "\n" : string.Empty) +
                "数据　占格 " + definition.Width + "×" + definition.Height + "\n次数　" + PlayerFacingCopy.RemainingAndTotal(tactical.ChargesCurrent, tactical.ChargesMaximum) +
                "\n消耗　" + definition.ActionPointCost + " 行动点\n效果\n可关联至 4 格战术栏，战斗中快速使用";
        }

        private static string RogueEquipmentDetail(RogueEquipmentRuntime runtime, string instanceId)
            => RogueEquipmentDetailBody(runtime, instanceId, true);

        public static string RogueEquipmentDetailBody(RogueEquipmentRuntime runtime, string instanceId, bool includeName)
        {
            RogueEquipmentInstance item = runtime.EquipmentItem(instanceId); EquipmentDefinition definition = runtime.DefinitionFor(instanceId);
            string effects = RogueEquipmentEffects(definition, item);
            return (includeName ? definition.DisplayName + "\n" : string.Empty) +
                "数据　" + RarityLabel(item.Rarity) + "　占格 " + definition.Width + "×" + definition.Height +
                "\n重量　" + definition.BaseWeight + "　以太负荷 " + definition.BaseAetherLoad +
                "\n效果\n" + (string.IsNullOrWhiteSpace(effects) ? "无额外效果" : string.Join("\n", effects.Split('\n')));
        }

        private static string MapStateTooltip(RogueliteMapNodeVisualState state)
        {
            switch (state)
            {
                case RogueliteMapNodeVisualState.Current: return "你现在所在的地点。";
                case RogueliteMapNodeVisualState.Available: return "可从当前位置前往。";
                case RogueliteMapNodeVisualState.Cleared: return "已完成，可安全回访。";
                case RogueliteMapNodeVisualState.Locked: return "当前路线尚未开放。";
                default: return "已发现，但当前不可前往。";
            }
        }

        private static string RogueEquipmentEffects(EquipmentDefinition definition, RogueEquipmentInstance item)
        {
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            IEnumerable<string> fixedEffects = definition.FixedEffectIds.Select(PlayerEquipmentEffect);
            IEnumerable<string> affixes = item.MutableAffixIds.Select(id => "附加　" +
                (catalog.Affixes.FirstOrDefault(value => value.AffixId == id)?.DisplayName ?? "未辨认的效果"));
            IEnumerable<string> upgrades = item.UpgradeBranchIds.Select(value =>
            {
                int separator = value.IndexOf(':');
                return "校准　" + PlayerEquipmentEffect(separator >= 0 ? value.Substring(separator + 1) : value);
            });
            return string.Join("\n", fixedEffects.Concat(affixes).Concat(upgrades).Where(value => !string.IsNullOrWhiteSpace(value)).Take(6));
        }

        private static string PlayerEquipmentEffect(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId)) return string.Empty;
            string[] parts = effectId.Split(':');
            int amount = parts.Length > 1 && int.TryParse(parts[1].TrimStart('+'), out int parsed) ? parsed : 0;
            switch (parts[0])
            {
                case "turn_start_shield": return "回合开始获得 " + amount + " 普通盾";
                case "first_move": return "每场首次移动距离 +" + amount;
                case "first_task_interact_free": case "first_task_free": return "每场首次任务互动免费";
                case "weapon_range": return "武器射程 +" + amount;
                case "low_mana_shield": return "低魔力时获得 " + amount + " 普通盾";
                case "move_attack_damage": return "移动后攻击伤害 +" + amount;
                case "defensive_spell_shield": return "防御术式额外获得 " + amount + " 普通盾";
                case "forced_move": return "被强制移动距离 " + amount;
                case "first_search_free": return "每场首次搜刮免费";
                case "first_quickbar_swap_free": return "每场首次调整战术栏免费";
                case "max_mana": return "魔力上限 +" + amount;
                case "burn_apply_mana": return "施加燃烧时恢复 " + amount + " 魔力";
                case "r_spell_range": case "r_range": return "远程术式射程 +" + amount;
                case "r_spell_mana": return "远程术式魔力消耗 +" + amount;
                case "m_spell_first_mana": case "u_spell_first_mana": return "每场首个对应术式少消耗 " + Math.Abs(amount) + " 魔力";
                case "burning_direct_damage": return "对燃烧目标伤害 +" + amount;
                case "zero_mana_restore": return "魔力耗尽时恢复 " + amount + " 魔力";
                case "adjacent_enemy_turn_shield": return "相邻敌人行动时获得 " + amount + " 普通盾";
                case "unit_damage": case "shot_damage": return "攻击伤害 +" + amount;
                case "object_damage": return "对物件伤害 +" + amount;
                case "remove_delay": return "移除攻击延迟";
                case "push": return "攻击推动 " + amount + " 格";
                case "shot_range": return "射程 +" + amount;
                case "first_reload_free": return "每场首次装填免费";
                case "remove_shot_delay": return "移除射击延迟";
                case "raise_shield": case "turn_shield": return "获得护盾 +" + amount;
                case "raise_then_move": return "架盾后可移动 " + amount + " 格";
                case "weight": return "重量 " + amount;
                case "burn_mana": return "燃烧回魔 +" + amount;
                case "burn_or_ground_mana": return "燃烧或火地形可恢复魔力";
                case "aether_load": return "以太负荷 " + amount;
                case "first_r_no_surcharge": return "每场首个远程术式免除额外消耗";
                default: return "特殊效果";
            }
        }

        private static string RogueInventoryIconPath(RogueInventoryItemPresentation item)
            => item.IsEquipment ? FormalArtRegistry.EquipmentFootprintPath(item.DefinitionId) : FormalArtRegistry.ItemPath(item.DefinitionId);

        private static string EquipmentIconPath(OCC.Combat.Roguelite.EquipmentSlot slot)
            => FormalArtRegistry.EquipmentSlotPath(slot.ToString());

        public static string RogueSpellIconPath(string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId) && definitionId.StartsWith("F-P-", StringComparison.Ordinal)) return FormalArtRegistry.FireSpellPath(definitionId);
            if (definitionId == "BASE-AETHER-SHIELD") return FormalArtRegistry.FeedbackPath("shield_restore");
            if (definitionId == "BASE-MANA-RECOVER") return FormalArtRegistry.FeedbackPath("mana_restore");
            return FormalArtRegistry.CommandPath(definitionId == "BASE-FIRE-RANGED" ? "skill_two" : "skill");
        }

        private static string RarityLabel(EquipmentRarity rarity)
            => rarity == EquipmentRarity.Common ? "普通" : rarity == EquipmentRarity.Uncommon ? "少见" : rarity == EquipmentRarity.Rare ? "稀有" : "传说";

        private void SettingRow(Transform parent, int index, string name, string value, string detail, Color accent, Action action)
        {
            float y = -104 - index * 76;
            Label("设置_" + name, name, parent, new Vector2(48, y), new Vector2(520, 58), 20, text, TextAnchor.MiddleLeft);
            ActionButton(value, detail, parent, new Vector2(620, y), new Vector2(372, 58), accent, true, action, "按钮_设置_" + index);
        }

        private void DrawArchive()
        {
            Header("行程与行囊", string.Empty);
            RogueliteMapRun run = bootstrap.ArchivedMapRun;
            GameObject card = FormalUiKit.LayoutPanel("档案卡", content.transform, "archive.card", panel);
            Label("标题", run == null ? "还没有开始旅程" : "这次学院旅程", card.transform, new Vector2(48, -42), new Vector2(940, 48), 32, text, TextAnchor.MiddleLeft);
            if (run == null)
                FormalUiEffects.AddEmptyIllustration(card.transform, "empty_archive_tray", new Vector2(512, -314), 256f);
            if (run != null)
            {
                if (run.UsesRogue11)
                {
                    RogueliteMapNode rogueCurrent = run.MapNode(run.CurrentNodeId);
                    ArchiveMetric(card.transform, new Vector2(48, -132), new Vector2(440, 64), "当前位置", rogueCurrent.DisplayName, cyan);
                    ArchiveMetric(card.transform, new Vector2(508, -132), new Vector2(220, 64), "已访问", run.VisitedNodes.Count.ToString(), safe);
                    ArchiveMetric(card.transform, new Vector2(748, -132), new Vector2(240, 64), "已完成", run.CompletedNodes.Count.ToString(), safe);
                    ArchiveMetric(card.transform, new Vector2(48, -212), new Vector2(216, 56), "金币", run.Gold.ToString(), amber);
                    ArchiveMetric(card.transform, new Vector2(284, -212), new Vector2(216, 56), "学院贡献", run.StageContribution.ToString(), safe);
                    ArchiveMetric(card.transform, new Vector2(520, -212), new Vector2(216, 56), "学期进度", run.StageTime.ToString(), cyan);
                    Label("构筑标题", FireRogueliteStarterCatalog.DisplayName(run.StarterId) + "　生命 " + run.CurrentHealth + "　个人魔力 " + run.CurrentMana,
                        card.transform, new Vector2(48, -292), new Vector2(940, 26), 18, cyan, TextAnchor.MiddleLeft);
                    string equipped = string.Join("\n", run.RogueRunState.EquippedSpellIds.Select((id, index) => (index + 1) + "：" + FireSpellDisplayName(id)));
                    Label("八槽", equipped, card.transform, new Vector2(48, -338), new Vector2(924, 92), 14, amber, TextAnchor.UpperLeft);
                    Label("装备", "背包装备 " + run.RogueRunState.EquipmentInstances.Count + "　战术道具 " + run.RogueRunState.TacticalItemInstances.Count + "\n护盾不会保留到下一场战斗",
                        card.transform, new Vector2(48, -448), new Vector2(924, 40), 16, muted, TextAnchor.UpperLeft);
                    ActionButton("返回", string.Empty, card.transform, new Vector2(520, -638), new Vector2(472, 48), cyan, true, () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
                    return;
                }
                RogueliteMapNode current = run.MapNode(run.CurrentNodeId);
                Label("进度标题", "旅程概况", card.transform, new Vector2(48, -98), new Vector2(940, 26), 18, cyan, TextAnchor.MiddleLeft);
                ArchiveMetric(card.transform, new Vector2(48, -132), new Vector2(440, 64), "当前位置", current.DisplayName, cyan);
                ArchiveMetric(card.transform, new Vector2(508, -132), new Vector2(220, 64), "已访问", "当前 " + run.VisitedNodes.Count + "　总计 " + run.MapNodes.Count, safe);
                ArchiveMetric(card.transform, new Vector2(748, -132), new Vector2(240, 64), "已完成", run.CompletedNodes.Count.ToString(), safe);
                ArchiveMetric(card.transform, new Vector2(48, -212), new Vector2(216, 56), "等级", run.Level.ToString(), cyan);
                ArchiveMetric(card.transform, new Vector2(284, -212), new Vector2(216, 56), "经验", run.Experience.ToString(), cyan);
                ArchiveMetric(card.transform, new Vector2(520, -212), new Vector2(216, 56), "零件", run.Parts.ToString(), amber);
                ArchiveMetric(card.transform, new Vector2(756, -212), new Vector2(216, 56), "以太", run.Aether.ToString(), cyan);
                ArchiveMetric(card.transform, new Vector2(48, -284), new Vector2(216, 56), "补给", run.Supplies.ToString(), safe);
                ArchiveMetric(card.transform, new Vector2(284, -284), new Vector2(216, 56), "侦测", run.ScoutingBeacons.ToString(), muted);
                ArchiveMetric(card.transform, new Vector2(756, -284), new Vector2(216, 56), "锻造规则", "确定性分支", safe);
                Label("构筑标题", FireRogueliteStarterCatalog.DisplayName(run.StarterId) + "　生命 " + run.CurrentHealth + "　护盾 " + run.CurrentShield + "　以太 " + run.CurrentMana, card.transform, new Vector2(48, -360), new Vector2(940, 26), 18, cyan, TextAnchor.MiddleLeft);
                ArchiveMetric(card.transform, new Vector2(48, -398), new Vector2(292, 72), "主手武器", RewardDisplayName(run.EquippedWeaponId, "学院训练武器"), cyan);
                ArchiveMetric(card.transform, new Vector2(356, -398), new Vector2(292, 72), "个人术式 1", FireSpellDisplayName(run.EquippedFireSpellIds[0]), amber);
                ArchiveMetric(card.transform, new Vector2(664, -398), new Vector2(308, 72), "个人术式 2", FireSpellDisplayName(run.EquippedFireSpellIds[1]), amber);
                string ownedFire = run.OwnedFireSpellIds.Count == 0 ? "无" : string.Join("、", run.OwnedFireSpellIds.Select(FireSpellDisplayName));
                Label("火术档案", "已学会：" + ownedFire, card.transform, new Vector2(48, -478), new Vector2(924, 40), 16, amber, TextAnchor.UpperLeft);
                string migration = run.PendingFireSpellReselections.Count == 0 && run.FireSpellRetirementCompensations.Count == 0 && run.FireSpellMigrationWarnings.Count == 0
                    ? "所有术式都已收好"
                    : "旧术式待重选 " + run.PendingFireSpellReselections.Count + "　替代术式待领取 " + run.FireSpellRetirementCompensations.Count + "　无法辨认 " + run.FireSpellMigrationWarnings.Count;
                Label("术式整理", migration, card.transform, new Vector2(48, -516), new Vector2(924, 34), 15,
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
                    Label("法宝档案", "第 " + (archiveArtifactIndex + 1) + " 件　共 " + artifacts.Length + " 件\n" + artifact.DisplayName + "　" + PlayerFacingCopy.RemainingAndTotal(instance.RemainingUses, artifact.MaximumUses, " 次") + "\n来源 " + artifact.Provenance,
                        card.transform, new Vector2(48, -548), new Vector2(924, 30), 16, amber, TextAnchor.UpperLeft);
                    FormalUiKit.SemanticChip("action", artifact.ActionPointCost.ToString(), card.transform, new Vector2(48, -580), tooltip, 32, 16, cyan);
                    string perUseCost = artifact.PublicCost
                        .Replace(artifact.ActionPointCost + " 行动点，", string.Empty)
                        .Replace("消耗 ", string.Empty);
                    Label("法宝详情", "每次 " + perUseCost + "\n" + artifact.EffectSummary + "\n目标：" + artifact.TargetSummary,
                        card.transform, new Vector2(108, -580), new Vector2(864, 28), 13, text, TextAnchor.UpperLeft);
                    FormalUiKit.SemanticChip("notice", string.Empty, card.transform, new Vector2(48, -620), tooltip, 32, 16, amber);
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
            if (overlay == UiOverlay.Loadout && value != UiOverlay.Loadout) ClearLoadoutDrag();
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
                bool combatDossier = value == UiOverlay.NodeRoom && bootstrap?.CurrentMapRun != null &&
                    !string.IsNullOrEmpty(selectedNodeId) && bootstrap.CurrentMapRun.MapNodes.Any(node => node.Id == selectedNodeId && node.IsCombat) &&
                    !bootstrap.CurrentMapRun.CompletedNodes.Contains(selectedNodeId);
                pendingFocusKey = value == UiOverlay.Settings ? "按钮_设置_0" : combatDossier ? "按钮_进入战斗" : "按钮_返回";
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
            return screen == UiScreen.Map ? RogueliteMapVisualPresentation.FocusKey(bootstrap?.CurrentMapRun?.CurrentNodeId) : screen == UiScreen.Briefing ? "按钮_进入战斗" : "按钮_开始新游戏";
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
            ClearLoadoutDrag();
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
            Line(header.transform, new Vector2(18, -53), new Vector2(1836, 2), FormalUiTheme.Rule);
        }

        private GameObject ActionButton(string title, string detail, Transform parent, Vector2 position, Vector2 size, Color accent, bool interactable, Action action, string focusKey = null, string iconPath = null, bool emphasized = false)
        {
            Color buttonSurface = !interactable ? FormalUiTheme.Disabled : emphasized ? Color.Lerp(FormalUiTheme.SurfaceRaised, accent, .22f) : FormalUiTheme.Interactive;
            GameObject result = Panel(string.IsNullOrEmpty(focusKey) ? "按钮_" + title : focusKey, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, buttonSurface);
            Image image = result.GetComponent<Image>();
            FormalUiKit.ApplySkin(image, OccPixelUiConfig.StateSkin("button", interactable ? "normal" : "disabled"), buttonSurface);
            Button button = result.AddComponent<Button>(); button.targetGraphic = image; button.interactable = interactable;
            if (action != null) button.onClick.AddListener(() => action());
            Line(result.transform, new Vector2(0, 0), new Vector2(4, size.y), interactable ? accent : muted);
            int titleSize = FormalUiTheme.ButtonFontSize;
            bool hasDetail = !string.IsNullOrWhiteSpace(detail);
            const float lineHeight = FormalUiTheme.BodyTextSlotHeight;
            float safeInset = FormalUiTheme.FramedContentInset;
            float blockHeight = hasDetail && size.y >= 104f ? lineHeight * 2f : lineHeight;
            float top = Mathf.Max(safeInset, Mathf.Round((size.y - blockHeight) * .5f));
            Text name = Label("名称", title, result.transform, new Vector2(safeInset + 4f, -top), new Vector2(size.x - safeInset * 2f - 4f, lineHeight), titleSize, interactable ? text : muted, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(name);
            if (hasDetail && size.y >= 104f)
            {
                Text detailLabel = Label("详情", detail, result.transform, new Vector2(safeInset + 4f, -top - lineHeight), new Vector2(size.x - safeInset * 2f - 4f, lineHeight), FormalUiTheme.ButtonDetailFontSize, interactable ? accent : muted, TextAnchor.MiddleLeft);
                FormalUiKit.PreventAutomaticWrapping(detailLabel);
            }
            if (!string.IsNullOrEmpty(iconPath)) AddActionIcon(result.transform, iconPath, size.y);
            Color normal = image.color;
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(normal, accent),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback, interactable ? string.Empty : detail);
            if (!focusTargets.ContainsKey(result.name)) focusTargets.Add(result.name, result);
            return result;
        }

        private void AddForwardArrow(Transform parent, Color accent)
        {
            GameObject arrow = Create("前进箭头", parent);
            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = arrowRect.anchorMax = arrowRect.pivot = new Vector2(.5f, .5f);
            arrowRect.anchoredPosition = new Vector2(-2, 0);
            arrowRect.sizeDelta = new Vector2(58, 34);

            ArrowPart(arrow.transform, "箭杆", new Vector2(-5, 0), new Vector2(34, 5), 0, accent);
            ArrowPart(arrow.transform, "上箭翼", new Vector2(16, 6), new Vector2(20, 5), 42, accent);
            ArrowPart(arrow.transform, "下箭翼", new Vector2(16, -6), new Vector2(20, 5), -42, accent);
            ArrowPart(arrow.transform, "尾迹一", new Vector2(-27, 0), new Vector2(5, 5), 0, FormalUiTheme.WithAlpha(accent, .82f));
            ArrowPart(arrow.transform, "尾迹二", new Vector2(-37, 0), new Vector2(4, 4), 0, FormalUiTheme.WithAlpha(accent, .46f));

            float intensity = bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity;
            if (intensity > 0f)
                DOTween.To(() => arrowRect.anchoredPosition.x,
                        value => arrowRect.anchoredPosition = new Vector2(value, arrowRect.anchoredPosition.y), 5f, Mathf.Lerp(.8f, .45f, intensity))
                    .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(arrow);
        }

        private static void ArrowPart(Transform parent, string name, Vector2 position, Vector2 size, float rotation, Color color)
        {
            GameObject part = Create(name, parent);
            RectTransform rect = part.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0, 0, rotation);
            Image image = part.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void BackpackInsetCell(Transform parent, int x, int y)
        {
            float size = LoadoutCellSize - 3;
            GameObject cell = Panel("背包格_" + x + "_" + y, parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(x * LoadoutCellSize, -y * LoadoutCellSize), new Vector2(size, size), FormalUiTheme.Surface);
            Image background = cell.GetComponent<Image>(); background.raycastTarget = false;
            Line(cell.transform, Vector2.zero, new Vector2(size, 3), FormalUiTheme.Ink);
            Line(cell.transform, Vector2.zero, new Vector2(3, size), FormalUiTheme.Ink);
            Line(cell.transform, new Vector2(0, -size + 2), new Vector2(size, 2), FormalUiTheme.WithAlpha(muted, .34f));
            Line(cell.transform, new Vector2(size - 2, 0), new Vector2(2, size), FormalUiTheme.WithAlpha(muted, .34f));
        }

        private GameObject InventoryGridButton(RogueInventoryItemPresentation item, Transform parent, Vector2 position, Vector2 size, Color accent, Action action)
        {
            GameObject result = Panel("背包物品_" + item.InstanceId, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, FormalUiTheme.SurfaceRaised);
            result.AddComponent<CanvasGroup>();
            Image background = result.GetComponent<Image>(); Button button = result.AddComponent<Button>(); button.targetGraphic = background;
            if (action != null) button.onClick.AddListener(() => action());
            Shadow shadow = result.AddComponent<Shadow>(); shadow.effectColor = FormalUiTheme.WithAlpha(Color.black, .72f); shadow.effectDistance = new Vector2(3, -3); shadow.useGraphicAlpha = true;
            Line(result.transform, Vector2.zero, new Vector2(size.x, 3), FormalUiTheme.WithAlpha(accent, .82f));
            Line(result.transform, Vector2.zero, new Vector2(3, size.y), accent);
            Line(result.transform, new Vector2(0, -size.y + 2), new Vector2(size.x, 2), FormalUiTheme.Ink);
            Line(result.transform, new Vector2(size.x - 2, 0), new Vector2(2, size.y), FormalUiTheme.Ink);
            Sprite sprite = Resources.Load<Sprite>(RogueInventoryIconPath(item));
            GameObject iconObject = Create("图标", result.transform); RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f); iconRect.anchoredPosition = Vector2.zero;
            int artSize = FormalUiKit.IntegerSpriteSize(sprite, Mathf.Min(size.x, size.y) - 10f);
            iconRect.sizeDelta = Vector2.one * artSize;
            if (item.IsEquipment && item.Rotated) iconRect.localEulerAngles = new Vector3(0, 0, -90);
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
            if (!item.IsEquipment)
                Label("次数", "×" + item.ChargesCurrent, result.transform, new Vector2(4, -size.y + 22), new Vector2(size.x - 8, 18), 12, safe, TextAnchor.MiddleRight);
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(background.color, accent),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback, string.Empty);
            return result;
        }

        private static void AddActionIcon(Transform parent, string iconPath, float buttonHeight)
        {
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            if (sprite == null) throw new KeyNotFoundException("Missing formal action icon: " + iconPath);
            RectTransform buttonRect = parent as RectTransform;
            float buttonWidth = buttonRect == null ? buttonHeight : buttonRect.rect.width;
            bool iconOnly = string.IsNullOrWhiteSpace(parent.Find("名称")?.GetComponent<Text>()?.text) &&
                            string.IsNullOrWhiteSpace(parent.Find("详情")?.GetComponent<Text>()?.text);
            float requestedSize = iconOnly ? Mathf.Clamp(Mathf.Min(buttonWidth, buttonHeight) - 16f, 24f, 48f)
                : buttonHeight >= 96f ? 56f : buttonHeight >= 64f ? 40f : 28f;
            float iconSize = FormalUiKit.IntegerSpriteSize(sprite, requestedSize);
            GameObject iconObject = Create("操作图标", parent);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconOnly ? new Vector2(.5f, .5f) : new Vector2(0, 1);
            iconRect.pivot = iconOnly ? new Vector2(.5f, .5f) : new Vector2(0, 1);
            iconRect.anchoredPosition = iconOnly ? Vector2.zero : new Vector2(14, -Mathf.Max(8, (buttonHeight - iconSize) * .5f));
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
            if (iconOnly) return;
            foreach (string labelName in new[] { "名称", "详情" })
            {
                RectTransform label = parent.Find(labelName)?.GetComponent<RectTransform>();
                if (label == null) continue;
                float labelX = 24f + iconSize;
                label.anchoredPosition = new Vector2(labelX, label.anchoredPosition.y);
                label.sizeDelta = new Vector2(Mathf.Max(40, label.sizeDelta.x - labelX + 16f), label.sizeDelta.y);
            }
        }

        private static Image Icon(string name, string resourcePath, Transform parent, Vector2 position, Vector2 size)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null) throw new KeyNotFoundException("Missing formal icon: " + resourcePath);
            GameObject iconObject = Create(name, parent);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            int integerSize = FormalUiKit.IntegerSpriteSize(sprite, Mathf.Min(size.x, size.y));
            rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * integerSize;
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
            return icon;
        }

        private static string AvailabilityText(UiOperationAvailability availability)
        {
            if (string.IsNullOrWhiteSpace(availability.Reason) || availability.Reason == availability.Status) return availability.Status;
            if (string.IsNullOrWhiteSpace(availability.Status)) return availability.Reason;
            return availability.Status + "，" + availability.Reason;
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

        private static Vector2 NodePosition(RogueliteMapNode node)
        {
            return ProjectMapPosition(AcademyMapVisualLayout.AnchorFor(node).SourcePosition);
        }

        private static Vector2 ProjectMapPosition(Vector2 source)
        {
            return new Vector2(source.x / AcademyMapVisualLayout.SourceSize.x * 1760f - 880f,
                397f - source.y / AcademyMapVisualLayout.SourceSize.y * 794f);
        }

        private static Vector2 RegionLabelOffset(string regionId)
        {
            if (regionId == "market_infirmary") return new Vector2(-420f, 226f);
            if (regionId == "courtyard_dormitory") return new Vector2(0f, 104f);
            return new Vector2(0f, 62f);
        }

        public static string MapRegionId(RogueliteMapNode node)
        {
            return AcademyMapVisualLayout.AnchorFor(node).RegionId;
        }

        private static string MapRegionLabel(string id)
        {
            switch (id)
            {
                case "teaching_archive": return "教学区";
                case "training_workshop": return "工坊区";
                case "market_infirmary": return "市集医务区";
                case "campus_wilds": return "校园荒野区";
                case "sealed_tower": return "封存高塔区";
                default: return "中庭宿舍区";
            }
        }

        private static void ApplyFormalMapBoard(GameObject mapCanvas)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.MapDecorPath("academy_network"));
            if (sprite == null) throw new KeyNotFoundException("Missing formal academy map board");
            GameObject backdrop = Create("学院连续鸟瞰底图", mapCanvas.transform);
            RectTransform rect = backdrop.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1872f, 1053f);
            Image image = backdrop.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.color = Color.white; image.raycastTarget = false;
            backdrop.transform.SetAsFirstSibling();
        }

        private static void AddRouteJoint(Transform parent, Vector2 position, Color tint)
        {
            GameObject joint = Create("路线转接件", parent); RectTransform rect = joint.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(16, 16);
            Image image = joint.AddComponent<Image>(); image.sprite = Resources.Load<Sprite>(FormalArtRegistry.MapDecorPath("route_joint")); image.color = tint; image.raycastTarget = false;
        }

        private void AddRegionIdentity(Transform parent, string regionId)
        {
            GameObject iconObject = Create("区域徽记_" + regionId, parent); RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(246, -23); rect.sizeDelta = new Vector2(32, 32);
            Image image = iconObject.AddComponent<Image>(); image.sprite = Resources.Load<Sprite>(FormalArtRegistry.MapRegionPath(regionId)); image.preserveAspect = true; image.raycastTarget = false;
            Label("区域名称", MapRegionLabel(regionId), parent, new Vector2(284, -28), new Vector2(132, 26), 15, muted, TextAnchor.MiddleLeft);
        }

        private static void MapRouteLine(Transform parent, Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            GameObject lineObject = Create("地图路线", parent);
            RectTransform rect = lineObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = (from + to) * .5f;
            rect.sizeDelta = new Vector2(delta.magnitude, thickness);
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = lineObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        }

        private static void MapDashedRouteLine(Transform parent, Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length <= .01f) return;
            Vector2 direction = delta / length;
            const float dash = 12f;
            const float gap = 10f;
            for (float start = 0; start < length; start += dash + gap)
            {
                float segment = Mathf.Min(dash, length - start);
                Vector2 a = from + direction * start;
                Vector2 b = a + direction * segment;
                MapRouteLine(parent, a, b, thickness, color);
            }
        }

        private GameObject FormalMapNodeButton(Transform parent, Vector2 position, RogueliteMapNodeType type, RogueliteMapNodeVisualState state, Color accent, Action action, string focusKey)
        {
            float size = MapNodeDisplaySize(type);
            GameObject result = Panel(focusKey, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(size, size), Color.white);
            Image image = result.GetComponent<Image>(); image.sprite = Resources.Load<Sprite>(FormalArtRegistry.LargeMapNodeMarkerPath(state.ToString())); image.type = Image.Type.Simple; image.color = Color.white;
            Button button = result.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 1f, 1f, .88f); colors.pressedColor = FormalUiTheme.WithAlpha(accent, .82f); colors.selectedColor = Color.white; colors.fadeDuration = .06f; button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(Color.white, accent),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            if (!focusTargets.ContainsKey(result.name)) focusTargets.Add(result.name, result);
            return result;
        }

        private static float MapNodeDisplaySize(RogueliteMapNodeType type)
        {
            return type == RogueliteMapNodeType.Finale ? 176f : type == RogueliteMapNodeType.Elite ? 160f : 112f;
        }
        private static string AcademyPhaseLabel(AcademyMapPhase phase) => phase == AcademyMapPhase.Consolidation ? "学期将尽" : phase == AcademyMapPhase.TransitionReady ? "终考将至" : "日程宽裕";
        private static string TypeLabel(RogueliteMapNodeType type) => type == RogueliteMapNodeType.Combat ? "巡哨" : type == RogueliteMapNodeType.Elite ? "高阶考核" : type == RogueliteMapNodeType.Event ? "见闻" : type == RogueliteMapNodeType.Workshop ? "工坊" : type == RogueliteMapNodeType.Medical ? "医务室" : type == RogueliteMapNodeType.Shop ? "市集" : type == RogueliteMapNodeType.Finale ? "终考" : "学院门厅";
        private static GameObject Create(string name, Transform parent) => FormalUiKit.Create(name, parent);
        private static void Stretch(RectTransform rect) => FormalUiKit.Stretch(rect);
    }

    public sealed class RogueLoadoutDragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private Action<PointerEventData> begin;
        private Action<PointerEventData> drag;
        private Action<PointerEventData> end;
        private Action rotate;
        private bool active;

        public void Configure(Action<PointerEventData> onBegin, Action<PointerEventData> onDrag, Action<PointerEventData> onEnd, Action onRotate)
        { begin = onBegin; drag = onDrag; end = onEnd; rotate = onRotate; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || active) return;
            active = true; begin?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !active) return;
            active = false; end?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (active) return;
            active = true; begin?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData) { if (active) drag?.Invoke(eventData); }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!active) return;
            active = false; end?.Invoke(eventData);
        }
        public void OnPointerClick(PointerEventData eventData)
        { if (eventData.button == PointerEventData.InputButton.Right) rotate?.Invoke(); }
    }
}
