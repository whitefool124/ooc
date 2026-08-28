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
        private readonly UiNavigationState navigation = new UiNavigationState(UiScreen.Landing, "按钮_近战热压");
        private readonly Dictionary<string, GameObject> focusTargets = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> resourceDeltas = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Text> resourceValues = new Dictionary<string, Text>(StringComparer.Ordinal);
        private UiOverlay overlay;
        private int archiveArtifactIndex;
        private UiScreen currentScreen = UiScreen.Landing;
        private string pendingFocusKey;
        private string selectedNodeId;
        private string selectedRogueInventoryId;
        private LoadoutSection loadoutSection = LoadoutSection.Equipment;
        private int selectedLoadoutSpellIndex;
        private const float LoadoutCellSize = 52f;
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
        private Vector2 savedMapPan;
        private int savedMapZoomIndex;
        private bool hasSavedMapView;
        private bool mapRegionsExpanded;
        private string loadoutInteractionMessage = "左键拖拽物品 · 拖拽中按 R 或右键旋转";
        private bool pageDirty = true;
        private bool animateNextRebuild = true;
        public int FullRebuildCount { get; private set; }
        public int PartialRefreshCount { get; private set; }

        private enum LoadoutSection
        {
            Equipment,
            Spells,
            Tactical
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
            if (mapViewportController != null && mapViewportController.MapContent != null)
            {
                savedMapPan = mapViewportController.MapContent.anchoredPosition;
                savedMapZoomIndex = mapViewportController.ZoomIndex;
                hasSavedMapView = true;
            }
            mapViewportController = null;
            if (content != null) { content.transform.DOKill(); Destroy(content); }
            focusTargets.Clear();
            resourceValues.Clear();
            FullRebuildCount++;
            content = Create("内容", root.transform);
            RectTransform rect = content.AddComponent<RectTransform>();
            Stretch(rect);
            Image background = content.AddComponent<Image>();
            string backdropId = overlay == UiOverlay.Settings ? "settings" : overlay == UiOverlay.Archive || overlay == UiOverlay.Loadout ? "archive" :
                overlay == UiOverlay.NodeRoom ? "briefing" :
                bootstrap.CurrentFlowPhase == CombatFlowPhase.Briefing ? "briefing" : bootstrap.IsMapMenuOpen && bootstrap.CurrentMapRun != null ? "map" : "landing";
            FormalUiEffects.ApplyBackdrop(background, backdropId);
            FormalUiEffects.AddAmbientScanlines(content.transform, bootstrap.UiPreferences.AnimationIntensity);
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
            Header("学院行动档案", string.Empty);
            GameObject card = FormalUiKit.LayoutPanel("入口卡", content.transform, "landing.card", panel);
            Label("标题", "选择出发方案", card.transform, new Vector2(64, -44), new Vector2(1180, 96), 42, text, TextAnchor.MiddleLeft);
            Text description = Label("说明", "选择一个学院方向开始；风险与奖励会在行动前显示。", card.transform, new Vector2(66, -130), new Vector2(1180, 48), 22, muted, TextAnchor.UpperLeft);
            FormalUiKit.PreventAutomaticWrapping(description);
            ActionButton("近战热压", "学院近战方向 · 四基础术式", card.transform, new Vector2(64, -220), new Vector2(580, 112), cyan, true, () => bootstrap.RequestStartMapRoguelite(false, FireRogueliteStarterCatalog.Melee));
            ActionButton("武器热载", "学院混合方向 · 四基础术式", card.transform, new Vector2(676, -220), new Vector2(580, 112), cyan, true, () => bootstrap.RequestStartMapRoguelite(false, FireRogueliteStarterCatalog.Universal));
            ActionButton("远程导能", "学院远程方向 · 四基础术式", card.transform, new Vector2(64, -354), new Vector2(580, 112), cyan, true, () => bootstrap.RequestStartMapRoguelite(false, FireRogueliteStarterCatalog.Ranged));
            ActionButton("继续推进", bootstrap.MapSavePresentation.ContinueDetail, card.transform, new Vector2(676, -354), new Vector2(580, 112), safe, bootstrap.MapSavePresentation.CanContinue, () => bootstrap.RequestStartMapRoguelite(true), iconPath: FormalArtRegistry.NavigationPath("continue"));
            ActionButton("行动档案", string.Empty, card.transform, new Vector2(64, -516), new Vector2(580, 96), amber, true, () => SetOverlay(UiOverlay.Archive), iconPath: FormalArtRegistry.NavigationPath("archive"));
            ActionButton("辅助设置", string.Empty, card.transform, new Vector2(676, -516), new Vector2(580, 96), amber, true, () => SetOverlay(UiOverlay.Settings), iconPath: FormalArtRegistry.NavigationPath("settings"));
        }

        private void DrawMap()
        {
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            if (string.IsNullOrEmpty(selectedNodeId) || !RogueliteMapCatalog.Nodes.Any(node => node.Id == selectedNodeId)) selectedNodeId = run.CurrentNodeId;
            Header("学院实训档案", run.UsesRogue11
                ? new RogueMapStatusPresentation(run).PhaseLabel + " · " + RogueliteMapCatalog.Node(run.CurrentNodeId).DisplayName
                : FireRogueliteStarterCatalog.DisplayName(run.StarterId) + " · " + RogueliteMapVisualPresentation.AcademyStatus(run));
            GameObject status = FormalUiKit.LayoutPanel("行动状态栏", content.transform, "map.status", panel);
            if (run.UsesRogue11)
            {
                RogueMapStatusPresentation model = new RogueMapStatusPresentation(run);
                MetricChip(status.transform, 12, -16, "生命", model.Health + "/" + model.MaximumHealth, FormalUiTheme.Health, FormalArtRegistry.ResourceMetricPath("health"), 198);
                MetricChip(status.transform, 220, -16, "魔力", model.Mana + "/" + model.MaximumMana, cyan, FormalArtRegistry.ResourceMetricPath("mana"), 198);
                MetricChip(status.transform, 428, -16, "金币", model.Gold.ToString(), amber, FormalArtRegistry.ResourceMetricPath("gold"), 198);
                MetricChip(status.transform, 636, -16, "贡献", model.StageContribution.ToString(), safe, FormalArtRegistry.ResourceMetricPath("contribution"), 198);
                MetricChip(status.transform, 844, -16, "时序", model.StageTime + "/" + model.TransitionTime, model.StageTime >= model.WarningTime ? danger : model.StageTime >= model.ConsolidationTime ? amber : cyan, FormalArtRegistry.ResourceMetricPath("stage_time"), 198);
                MetricChip(status.transform, 1052, -16, "探索", model.ExploredNodes + "/" + model.RequiredExploredNodes, model.EarlyFinaleReady ? safe : cyan, FormalArtRegistry.ResourceMetricPath("explored"), 198);
                MetricChip(status.transform, 1260, -16, "许可", model.CorePermits + "/" + model.RequiredCorePermits, model.EarlyFinaleReady ? safe : amber, FormalArtRegistry.ResourceMetricPath("core_permit"), 198);
            }
            else
            {
                MetricChip(status.transform, 12, -16, "等级", run.Level.ToString(), cyan, null, 198);
                MetricChip(status.transform, 220, -16, "经验", run.Experience.ToString(), cyan, null, 198);
                MetricChip(status.transform, 428, -16, "零件", run.Parts.ToString(), amber, null, 198);
                MetricChip(status.transform, 636, -16, "以太", run.Aether.ToString(), cyan, null, 198);
                MetricChip(status.transform, 844, -16, "补给", run.Supplies.ToString(), safe, null, 198);
                MetricChip(status.transform, 1052, -16, "侦测", run.ScoutingBeacons.ToString(), muted, null, 198);
                MetricChip(status.transform, 1260, -16, "权限卡", run.AccessCards.ToString(), danger, null, 198);
            }
            GameObject entrance = ActionButton(string.Empty, string.Empty, status.transform, new Vector2(1474, -16), new Vector2(88, 60), safe, true, bootstrap.RequestReturnToLanding, iconPath: FormalArtRegistry.NavigationPath("home"));
            BindHover(entrance, "返回入口", bootstrap.MapSavePresentation.ReturnDetail, safe);
            GameObject loadout = ActionButton("整", string.Empty, status.transform, new Vector2(1570, -16), new Vector2(88, 60), cyan, run.UsesRogue11, () => SetOverlay(UiOverlay.Loadout));
            BindHover(loadout, "学院整备", "8 术式 · 11 装备 · 6×10 背包 · 4 战术栏", cyan);
            ActionButton(string.Empty, string.Empty, status.transform, new Vector2(1666, -16), new Vector2(88, 60), amber, true, () => SetOverlay(UiOverlay.Archive), iconPath: FormalArtRegistry.NavigationPath("archive"));
            ActionButton(string.Empty, string.Empty, status.transform, new Vector2(1762, -16), new Vector2(88, 60), amber, true, () => SetOverlay(UiOverlay.Settings), iconPath: FormalArtRegistry.NavigationPath("settings"));

            GameObject mapPanel = FormalUiKit.LayoutPanel("节点地图视口", content.transform, "map.board", FormalUiTheme.Surface);
            RectTransform viewportRect = mapPanel.GetComponent<RectTransform>();
            mapPanel.AddComponent<RectMask2D>();
            GameObject mapCanvas = Create("学院分区地图画布", mapPanel.transform);
            RectTransform mapCanvasRect = mapCanvas.AddComponent<RectTransform>();
            mapCanvasRect.anchorMin = mapCanvasRect.anchorMax = mapCanvasRect.pivot = new Vector2(.5f, .5f);
            mapCanvasRect.anchoredPosition = Vector2.zero;
            mapCanvasRect.sizeDelta = AcademyMapVisualLayout.LogicalCanvasSize;
            RawImage mapRender = mapCanvas.AddComponent<RawImage>();
            AcademyMap3DRenderer map3D = mapCanvas.AddComponent<AcademyMap3DRenderer>();
            map3D.Initialize(mapRender);
            DrawDistrictLabels(mapCanvas.transform);
            DrawConnections(mapCanvas.transform, run);
            foreach (RogueliteMapNode node in RogueliteMapCatalog.Nodes) DrawNode(mapCanvas.transform, run, node);

            mapViewportController = mapPanel.AddComponent<RogueMapViewportController>();
            mapViewportController.Initialize(viewportRect, mapCanvasRect, canvas, hasSavedMapView ? savedMapZoomIndex : 0);
            if (hasSavedMapView) mapViewportController.SetView(savedMapPan, savedMapZoomIndex);
            else mapViewportController.ResetToOverview();

            RogueliteMapNodeVisualState[] legendStates = { RogueliteMapNodeVisualState.Current, RogueliteMapNodeVisualState.Available,
                RogueliteMapNodeVisualState.Cleared, RogueliteMapNodeVisualState.Locked };
            for (int i = 0; i < legendStates.Length; i++) DrawMapStateLegend(mapPanel.transform, legendStates[i], new Vector2(14 + i * 52, -814));
            float viewportWidth = viewportRect.sizeDelta.x;
            GameObject locate = ActionButton("◎", string.Empty, mapPanel.transform, new Vector2(viewportWidth - 250, -16), new Vector2(52, 52), cyan, true,
                () => mapViewportController.CenterOnSourcePosition(AcademyMapVisualLayout.AnchorFor(RogueliteMapCatalog.Node(run.CurrentNodeId)).SourcePosition));
            BindHover(locate, "回到当前位置", "拖动地图；滚轮缩放。", cyan);
            GameObject zoomOut = ActionButton("−", string.Empty, mapPanel.transform, new Vector2(viewportWidth - 192, -16), new Vector2(52, 52), text, true, () => mapViewportController.ZoomOut());
            BindHover(zoomOut, "缩小地图", "查看分区全貌。", text);
            GameObject zoomIn = ActionButton("+", string.Empty, mapPanel.transform, new Vector2(viewportWidth - 134, -16), new Vector2(52, 52), text, true, () => mapViewportController.ZoomIn());
            BindHover(zoomIn, "放大地图", "放大后可拖动查看。", text);
            GameObject regions = ActionButton("区", string.Empty, mapPanel.transform, new Vector2(viewportWidth - 76, -16), new Vector2(52, 52), amber, true,
                () => { mapRegionsExpanded = !mapRegionsExpanded; Invalidate(false); });
            BindHover(regions, "区域定位", "选择要查看的学院分区。", amber);

            if (mapRegionsExpanded)
            {
                string[] regionShortcuts = { "teaching_archive", "training_workshop", "courtyard_dormitory", "market_infirmary", "campus_wilds", "sealed_tower" };
                for (int i = 0; i < regionShortcuts.Length; i++)
                    MapRegionShortcut(mapPanel.transform, regionShortcuts[i], new Vector2(viewportWidth - 192 + i % 3 * 58, -76 - i / 3 * 58));
            }

            DrawCompactMapNodeCard(mapPanel.transform, run, RogueliteMapCatalog.Node(selectedNodeId), viewportWidth);
        }

        private void DrawDistrictLabels(Transform parent)
        {
            foreach (AcademyMapDistrictSpec district in AcademyMap3DLayout.Districts)
            {
                Vector2 center = Vector2.zero;
                for (int i = 0; i < district.MapPolygon.Count; i++) center += district.MapPolygon[i];
                center /= district.MapPolygon.Count;
                Vector2 position = AcademyMap3DLayout.ProjectMapToCanvas(center, 0f);
                GameObject chip = Panel("分区标签_" + district.Id, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    position, new Vector2(176, 30), FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .78f));
                chip.GetComponent<Image>().raycastTarget = false;
                Text label = Label("名称", district.DisplayName, chip.transform, new Vector2(4, -2), new Vector2(168, 26),
                    15, FormalUiTheme.OnInk, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
            }
        }

        private void DrawCompactMapNodeCard(Transform parent, RogueliteMapRun run, RogueliteMapNode node, float viewportWidth)
        {
            RogueliteMapNodeVisualState state = run.VisualStateFor(node.Id);
            bool identified = state != RogueliteMapNodeVisualState.Unknown;
            GameObject card = Panel("节点摘要卡", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(viewportWidth - 496, -722), new Vector2(472, 128), FormalUiTheme.Panel);
            string title = identified ? node.DisplayName : "尚未侦测";
            string type = identified ? TypeLabel(node.Type) : "未知节点";
            string time = identified && run.UsesRogue11 ? (AcademyMapTuning.TimeCost(node.Type) == 0 ? "零时序" : "+" + AcademyMapTuning.TimeCost(node.Type) + " 时序") : string.Empty;
            Label("名称", title, card.transform, new Vector2(18, -14), new Vector2(300, 38), 26, text, TextAnchor.MiddleLeft);
            Label("摘要", type + (string.IsNullOrEmpty(time) ? string.Empty : " · " + time) + " · " + RogueliteMapVisualPresentation.StateLabel(state),
                card.transform, new Vector2(18, -54), new Vector2(320, 28), 16, identified ? cyan : muted, TextAnchor.MiddleLeft);
            Label("提示", identified ? "悬停看摘要；详情中确认行动" : "抵达相邻节点后公开内容",
                card.transform, new Vector2(18, -84), new Vector2(320, 24), 14, muted, TextAnchor.MiddleLeft);
            if (identified) AddNodeIcon(card.transform, node.Type);
            GameObject details = ActionButton(string.Empty, string.Empty, card.transform, new Vector2(346, -30), new Vector2(108, 72), amber, identified, OpenSelectedNodeRoom);
            AddForwardArrow(details.transform, identified ? amber : muted);
            BindHover(details, identified ? "打开节点档案" : "节点尚未侦测", identified ? "进入独立全屏界面，查看敌情、奖励和行动。" : "抵达相邻节点后公开。", identified ? amber : muted);
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
            if (run == null || string.IsNullOrEmpty(selectedNodeId) || !RogueliteMapCatalog.Nodes.Any(value => value.Id == selectedNodeId))
            {
                SetOverlay(UiOverlay.None);
                return;
            }

            RogueliteMapNode node = RogueliteMapCatalog.Node(selectedNodeId);
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
            Header(TypeLabel(node.Type) + "节点档案", displayName);

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
            Label("材质主题", NodeRoomMaterialCue(node.Type), identity.transform, new Vector2(36, -296), new Vector2(420, 64), 16, muted, TextAnchor.UpperLeft);
            Label("节点说明", nodeEvent == null ? node.Summary : nodeEvent.Region + "事件 · 本局固定内容，读档不重掷",
                identity.transform, new Vector2(36, -374), new Vector2(420, 104), 18, text, TextAnchor.UpperLeft);

            if (run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, node);
                RoomMetric(identity.transform, "风险", string.IsNullOrEmpty(preview.EncounterLabel) ? preview.RiskLabel : preview.EncounterLabel,
                    FormalArtRegistry.ResourceMetricPath("risk"), new Vector2(36, -506), accent);
                RoomMetric(identity.transform, "时序", preview.IsZeroTime ? "0" : "+" + preview.TimeCost,
                    FormalArtRegistry.ResourceMetricPath("stage_time"), new Vector2(252, -506), cyan);
                RoomMetric(identity.transform, "恢复", "+" + preview.ExpectedHealthRecovery + "生命 / +" + preview.ExpectedManaRecovery + "魔力",
                    FormalArtRegistry.ResourceMetricPath("health"), new Vector2(36, -570), safe, 432);
                Label("奖励预览", preview.RewardLabel, identity.transform, new Vector2(36, -646), new Vector2(420, 56), 17, amber, TextAnchor.UpperLeft);
            }

            DrawNodeRoomActions(decisions.transform, run, node, current, cleared, accent);
        }

        private void DrawNodeRoomActions(Transform parent, RogueliteMapRun run, RogueliteMapNode node, bool current, bool cleared, Color accent)
        {
            Label("页面标题", NodeRoomActionTitle(node, current, cleared), parent, new Vector2(44, -38), new Vector2(1068, 48), 32, text, TextAnchor.MiddleLeft);
            Label("页面说明", NodeRoomInstruction(node, current, cleared), parent, new Vector2(44, -96), new Vector2(1068, 64), 18, muted, TextAnchor.UpperLeft);

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
            else if (!current || (node.IsCombat && !cleared))
            {
                bool resumeCurrentCombat = current && RogueliteUiPreferences.CanOpenCombatBriefing(run, node);
                bool canEnter = resumeCurrentCombat || RogueliteUiPreferences.CanTravelTo(run, node);
                string label = cleared ? "进入回访" : resumeCurrentCombat ? "进入战前简报" : node.IsCombat ? "进入节点并查看简报" : "进入节点";
                string reason = canEnter ? "进入后仍可在结算前退出；退出不消耗资源或时序。" : RogueliteMapVisualPresentation.RestrictionText(run, node);
                ActionButton(label, reason, parent, new Vector2(224, -204), new Vector2(720, 126), canEnter ? accent : muted, canEnter,
                    () => bootstrap.SelectMapNode(node.Id), iconPath: FormalArtRegistry.NavigationPath("continue"));
            }
            else if (cleared)
            {
                Label("回访说明", "该节点已完成。回访不会重复触发奖励、战斗或时序结算。", parent,
                    new Vector2(44, -214), new Vector2(1068, 80), 22, safe, TextAnchor.UpperLeft);
                if (node.Type == RogueliteMapNodeType.Workshop && run.UsesRogue11)
                    ActionButton("打开学院整备", "装备、术式、背包与战术栏", parent, new Vector2(44, -324), new Vector2(1068, 104), cyan, true,
                        () => SetOverlay(UiOverlay.Loadout));
            }
            else
            {
                Label("入口说明", "从地图选择相邻节点，再进入独立节点界面。", parent,
                    new Vector2(44, -214), new Vector2(1068, 80), 22, muted, TextAnchor.UpperLeft);
            }

            string exitLabel = current && !cleared && !node.IsCombat && node.Type != RogueliteMapNodeType.Start ? "暂不结算，退出节点" : "返回学院地图";
            ActionButton(exitLabel, "不消耗时序、金币、贡献或节点内容", parent, new Vector2(224, -616), new Vector2(720, 88), amber, true,
                () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void RoomMetric(Transform parent, string title, string value, string iconPath, Vector2 position, Color accent, float width = 204)
        {
            GameObject metric = Panel("房间指标_" + title, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(width, 54), FormalUiTheme.Surface);
            Icon("图标", iconPath, metric.transform, new Vector2(12, -11), new Vector2(32, 32));
            Label("名称", title, metric.transform, new Vector2(54, -6), new Vector2(width - 66, 20), 13, muted, TextAnchor.MiddleLeft);
            Label("数值", value, metric.transform, new Vector2(54, -25), new Vector2(width - 66, 24), 15, accent, TextAnchor.MiddleLeft);
        }

        private static string NodeRoomActionTitle(RogueliteMapNode node, bool current, bool cleared)
            => cleared ? "安全回访" : !current ? "进入前确认" : node.IsCombat ? "战斗入口" : node.Type == RogueliteMapNodeType.Start ? "学院入口" : "公开选项";

        private static string NodeRoomInstruction(RogueliteMapNode node, bool current, bool cleared)
        {
            if (cleared) return "已完成节点只保留回访与整备功能，不会重复产出。";
            if (!current) return "核对风险、时序和奖励后再进入；此页退出不会改变局内状态。";
            if (node.IsCombat) return "进入战前简报后仍可返回地图，开始战斗才锁定战斗快照。";
            if (node.Type == RogueliteMapNodeType.Start) return "这里是本轮学院行动的安全起点。";
            return "选择会立即结算并关闭本节点；也可以暂不结算，退出后再回来。";
        }

        private static Color NodeRoomAccent(RogueliteMapNodeType type)
        {
            if (type == RogueliteMapNodeType.Elite || type == RogueliteMapNodeType.Finale) return FormalUiTheme.Danger;
            if (type == RogueliteMapNodeType.Shop || type == RogueliteMapNodeType.Treasure || type == RogueliteMapNodeType.Workshop) return FormalUiTheme.Amber;
            if (type == RogueliteMapNodeType.Rest) return FormalUiTheme.Safe;
            return FormalUiTheme.Cyan;
        }

        private static string NodeRoomMaterialCue(RogueliteMapNodeType type)
        {
            switch (type)
            {
                case RogueliteMapNodeType.Start: return "入学登记页 · 布面档案夹与路线签";
                case RogueliteMapNodeType.Combat: return "训练记录页 · 炭墨阵位与冷青行动签";
                case RogueliteMapNodeType.Elite: return "高阶考核页 · 封存红警示与教官印记";
                case RogueliteMapNodeType.Event: return "事件文书页 · 折角记录与公开批注";
                case RogueliteMapNodeType.Workshop: return "校准工单页 · 木制导具与氧化黄铜";
                case RogueliteMapNodeType.Shop: return "补给登记页 · 物资票据与黄铜计价签";
                case RogueliteMapNodeType.Rest: return "医务记录页 · 灰绿恢复签与药剂栏";
                case RogueliteMapNodeType.Treasure: return "封存领取页 · 锁匣编号与安全黄封签";
                case RogueliteMapNodeType.Finale: return "核心封存页 · 红色权限警示与塔心记录";
                default: return "学院行动档案页";
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
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f); iconRect.anchoredPosition = Vector2.zero; iconRect.sizeDelta = new Vector2(36, 36);
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = Resources.Load<Sprite>(FormalArtRegistry.MapRegionPath(regionId)); icon.preserveAspect = true; icon.raycastTarget = false;
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
                icon.rectTransform.sizeDelta = new Vector2(40, 40); labelX = 56f;
            }
            Label("名称", label, chip.transform, new Vector2(labelX, -4), new Vector2(70, 22), 14, muted, TextAnchor.MiddleLeft);
            bool changed = resourceDeltas.TryGetValue(label, out int delta);
            string valueText = changed ? value + " " + (delta > 0 ? "+" : string.Empty) + delta : value;
            Text valueLabel = Label("数值", valueText, chip.transform, new Vector2(labelX, -24), new Vector2(width - labelX - 12, 30), 22, changed ? accent : text, TextAnchor.MiddleLeft);
            valueLabel.fontStyle = FontStyle.Bold;
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
                SetMetricValue("魔力", status.Mana + "/" + status.MaximumMana);
                SetResourceValue("金币", status.Gold); SetResourceValue("贡献", status.StageContribution);
                SetMetricValue("时序", status.StageTime + "/" + status.TransitionTime);
                SetMetricValue("探索", status.ExploredNodes + "/" + status.RequiredExploredNodes);
                SetMetricValue("许可", status.CorePermits + "/" + status.RequiredCorePermits);
                PartialRefreshCount++; return;
            }
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

        private void SetMetricValue(string key, string value)
        {
            if (!resourceValues.TryGetValue(key, out Text label) || label == null) return;
            label.text = value; label.color = text;
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
                bool available = route == RogueliteMapRouteVisualState.Available;
                Color color = available ? FormalUiTheme.WithAlpha(cyan, .94f) :
                    route == RogueliteMapRouteVisualState.Locked ? FormalUiTheme.WithAlpha(danger, .26f) :
                    route == RogueliteMapRouteVisualState.Safe ? FormalUiTheme.WithAlpha(safe, .30f) :
                    FormalUiTheme.WithAlpha(muted, .20f);
                if (available) MapRouteLine(parent, a, b, 4f, color);
                else MapDashedRouteLine(parent, a, b, 2f, color);
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
            string time = identified && run.UsesRogue11 ? (AcademyMapTuning.TimeCost(node.Type) == 0 ? " · 零时" : " · +" + AcademyMapTuning.TimeCost(node.Type) + "时") : string.Empty;
            GameObject buttonObject = FormalMapNodeButton(parent, NodePosition(node), state, accent, () => { selectedNodeId = node.Id; pendingFocusKey = focusKey; bootstrap.NotifyMapNodeSelected(node.Id); Invalidate(false); }, focusKey);
            if (identified)
            {
                AddCompactNodeIcon(buttonObject.transform, node.Type);
                BindHover(buttonObject, node.DisplayName, TypeLabel(node.Type) + time + "\n" + node.Summary, accent);
            }
            else BindHover(buttonObject, "未侦测节点", "抵达相邻节点后揭示。", muted);
            if (selected && buttonObject.transform.Find("节点选中动效") == null) AddMapNodeSelectionEffect(buttonObject.transform, accent);
        }

        private void AddMapNodeSelectionEffect(Transform parent, Color accent)
        {
            GameObject effect = Create("节点选中动效", parent);
            RectTransform rect = effect.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(66, 66);
            CanvasGroup group = effect.AddComponent<CanvasGroup>();
            group.alpha = .58f;

            Sprite haloSprite = Resources.Load<Sprite>(FormalArtRegistry.MapNodeMarkerPath("Current"));
            GameObject haloObject = Create("呼吸光环", effect.transform);
            RectTransform haloRect = haloObject.AddComponent<RectTransform>();
            haloRect.anchorMin = haloRect.anchorMax = haloRect.pivot = new Vector2(.5f, .5f);
            haloRect.anchoredPosition = Vector2.zero;
            haloRect.sizeDelta = new Vector2(62, 62);
            Image halo = haloObject.AddComponent<Image>();
            halo.sprite = haloSprite;
            halo.preserveAspect = true;
            halo.color = FormalUiTheme.WithAlpha(accent, .72f);
            halo.raycastTarget = false;

            SelectionSpark(effect.transform, new Vector2(-32, 0), accent);
            SelectionSpark(effect.transform, new Vector2(28, 0), accent);
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
            Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.NodeTypePath(runtimeId));
            if (sprite == null) throw new KeyNotFoundException("Missing formal node icon: " + runtimeId);
            GameObject iconObject = Create("节点类型图标_" + runtimeId, parent);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.anchoredPosition = Vector2.zero; iconRect.sizeDelta = new Vector2(32, 32);
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
            Label("类型", identified ? TypeLabel(node.Type).ToUpperInvariant() : "未知节点", parent, new Vector2(28, -28), new Vector2(210, 26), 17, amber, TextAnchor.MiddleLeft);
            AddRegionIdentity(parent, regionId);
            Label("名称", identified ? currentEvent?.DisplayName ?? node.DisplayName : "尚未侦测", parent, new Vector2(28, -62), new Vector2(390, 48), 30, text, TextAnchor.MiddleLeft);
            Label("摘要", identified ? currentEvent == null ? node.Summary : currentEvent.Region + "事件 · 本局固定内容，读档不重掷" : "抵达相邻节点后公开", parent, new Vector2(28, -116), new Vector2(390, 36), 16, muted, TextAnchor.UpperLeft);
            bool cleared = run.CompletedNodes.Contains(node.Id);
            string stateText = RogueliteMapVisualPresentation.RestrictionText(run, node);
            Color stateColor = visual == RogueliteMapNodeVisualState.Locked ? danger : visual == RogueliteMapNodeVisualState.Unknown ? muted : cleared ? safe : cyan;
            DetailIconMetric(parent, "状态", stateText, FormalArtRegistry.MapStatePath(visual.ToString()), new Vector2(28, -174), stateText, stateColor);
            if (!identified) return;

            if (run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, node);
                string threshold = preview.CrossesTransition ? "强制首领" : preview.CrossesWarning ? "首领警告" : preview.CrossesConsolidation ? "学期收束" : "阈值安全";
                string encounterRisk = string.IsNullOrEmpty(preview.EncounterLabel) ? preview.RiskLabel : preview.EncounterLabel + " · " + preview.RiskLabel;
                string encounterDetail = string.IsNullOrEmpty(preview.EnemySummary) ? preview.FailureConsequence : "敌方：" + preview.EnemySummary + "\n空间：" + preview.SpatialRisk + "\n" + preview.FailureConsequence;
                DetailIconMetric(parent, "风险", encounterRisk, FormalArtRegistry.ResourceMetricPath("risk"), new Vector2(28, -218), encounterDetail, preview.CrossesTransition ? danger : amber);
                DetailIconMetric(parent, "时序", preview.IsZeroTime ? "0" : "+" + preview.TimeCost + " → " + preview.ProjectedStageTime, FormalArtRegistry.ResourceMetricPath("stage_time"), new Vector2(218, -218), "结算后公开时序 " + preview.ProjectedStageTime + "/" + AcademyMapTuning.TransitionProgress, cyan);
                DetailIconMetric(parent, "生命", "+" + preview.ExpectedHealthRecovery, FormalArtRegistry.ResourceMetricPath("health"), new Vector2(28, -262), "预计结算恢复生命 " + preview.ExpectedHealthRecovery, FormalUiTheme.Health);
                DetailIconMetric(parent, "魔力", "+" + preview.ExpectedManaRecovery, FormalArtRegistry.ResourceMetricPath("mana"), new Vector2(218, -262), "预计结算恢复个人魔力 " + preview.ExpectedManaRecovery, FormalUiTheme.Magic);
                Label("阈值", threshold + "  ·  " + preview.RewardLabel, parent, new Vector2(28, -306), new Vector2(390, 30), 15,
                    preview.CrossesTransition ? danger : preview.CrossesWarning ? amber : text, TextAnchor.MiddleLeft);
            }

            if (current && !cleared && !node.IsCombat && node.Type != RogueliteMapNodeType.Start)
            {
                IReadOnlyList<RogueliteNodeContentChoice> choices = run.CurrentContentChoices;
                Label("选择标题", "公开选项（确认后结算）", parent, new Vector2(28, -346), new Vector2(390, 30), 18, text, TextAnchor.MiddleLeft);
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
            string action = cleared ? "安全回访" : resumeCurrentCombat ? "恢复战前简报" : node.IsCombat ? "进入战前简报" : "前往节点";
            string detail = canTravel ? string.Empty : "选择相邻可达节点";
            if (!canTravel) detail = RogueliteMapVisualPresentation.RestrictionText(run, node);
            string travelTooltip = cleared ? "回访不再触发战斗或奖励。" : "抵达后仍可沿路线返回。";
            if (run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, node);
                travelTooltip += "\n" + preview.FailureConsequence;
            }
            GameObject travelButton = ActionButton(action, detail, parent, new Vector2(28, -350), new Vector2(390, 76), canTravel ? cyan : muted, canTravel, () => bootstrap.SelectMapNode(node.Id));
            BindHover(travelButton, action, travelTooltip, canTravel ? cyan : muted);
            if (current && node.Type == RogueliteMapNodeType.Start)
                Label("入口提示", "选择地图上的相邻节点查看完整预览，再确认前往。", parent, new Vector2(28, -450), new Vector2(390, 60), 18, muted, TextAnchor.UpperLeft);
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
            string calibrationDetail = run.IsAetherCalibrated ? "当前阶段不可执行：本局已完成校准" : run.Aether < 2 ? "以太不足：需要 2 以太" : "消耗 2 以太；后续战斗 +1 护甲";
            ActionButton(run.IsAetherCalibrated ? "以太校准：已完成" : "以太校准", calibrationDetail, parent, new Vector2(28, y), new Vector2(390, 72), amber, !run.IsAetherCalibrated && run.Aether >= 2, bootstrap.CalibrateMapAether);
        }

        private void DrawRogueWorkshop(Transform parent, RogueliteMapRun run)
        {
            Label("学院整备", "8 术式槽 · 11 装备槽 · 无耐久", parent, new Vector2(28, -270), new Vector2(390, 30), 20, text, TextAnchor.MiddleLeft);
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            string[] slots = run.RogueRunState.EquippedSpellIds;
            for (int index = 0; index < slots.Length; index++)
            {
                string id = slots[index]; string name = string.IsNullOrEmpty(id) ? "空" : catalog.Spells.First(value => value.DefinitionId == id).DisplayName;
                Label("术式槽" + (index + 1), (index + 1) + " · " + name, parent, new Vector2(28 + (index % 2) * 196, -312 - (index / 2) * 42), new Vector2(186, 34), 14, string.IsNullOrEmpty(id) ? muted : amber, TextAnchor.MiddleLeft);
            }
            Label("装备实例", "已持有 " + run.RogueRunState.EquipmentInstances.Count + " · 已装备 " + run.RogueRunState.EquipmentSlotInstanceIds.Count(value => !string.IsNullOrEmpty(value.Value)),
                parent, new Vector2(28, -490), new Vector2(390, 34), 15, cyan, TextAnchor.MiddleLeft);
            Label("整备规则", "战外换装；双手锁副手；装备无耐久；重铸消耗金币。", parent, new Vector2(28, -532), new Vector2(390, 48), 13, muted, TextAnchor.UpperLeft);
        }

        private void DrawBriefing()
        {
            MissionPreparation preparation = bootstrap.CurrentPreparation;
            RogueliteMapRun run = bootstrap.CurrentMapRun;
            string missionName = run == null ? preparation?.MissionId ?? "未知任务" : RogueliteMapCatalog.Node(run.CurrentNodeId).DisplayName;
            Header("战前简报", missionName);
            GameObject card = FormalUiKit.LayoutPanel("简报卡", content.transform, "briefing.card", panel);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 680);
            Label("任务", missionName, card.transform, new Vector2(40, -24), new Vector2(1200, 48), 34, text, TextAnchor.MiddleLeft);
            GameObject objective = Panel("行动目标区", card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -88), new Vector2(1200, 118), FormalUiTheme.SurfaceRaised);
            Label("目标标题", "行动目标", objective.transform, new Vector2(18, -10), new Vector2(210, 26), 18, cyan, TextAnchor.MiddleLeft);
            Label("目标", preparation?.RulesSummary ?? "无", objective.transform, new Vector2(18, -42), new Vector2(1164, 62), 23, text, TextAnchor.UpperLeft);
            GameObject enemy = Panel("敌方编成区", card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -220), new Vector2(1200, 132), FormalUiTheme.SurfaceRaised);
            Label("敌情标题", "敌方编成", enemy.transform, new Vector2(18, -10), new Vector2(210, 26), 18, amber, TextAnchor.MiddleLeft);
            Label("敌情", preparation?.EnemySummary ?? "无", enemy.transform, new Vector2(18, -42), new Vector2(1164, 76), 22, text, TextAnchor.UpperLeft);
            string briefingTooltip = "行动结果固定；没有隐藏倒计时。";
            if (run != null && run.UsesRogue11)
            {
                RogueNodePreviewPresentation preview = new RogueNodePreviewPresentation(run, RogueliteMapCatalog.Node(run.CurrentNodeId));
                string threshold = preview.CrossesTransition ? "强制首领" : preview.CrossesWarning ? "首领警告" : preview.CrossesConsolidation ? "学期收束" : "阈值安全";
                string encounterRisk = string.IsNullOrEmpty(preview.EncounterLabel) ? preview.RiskLabel : preview.EncounterLabel + " · " + preview.RiskLabel;
                DetailIconMetric(card.transform, "风险", encounterRisk, FormalArtRegistry.ResourceMetricPath("risk"), new Vector2(40, -368), "敌方：" + preview.EnemySummary + "\n空间：" + preview.SpatialRisk + "\n" + preview.FailureConsequence, amber, 224, 58);
                DetailIconMetric(card.transform, "时序", "+" + preview.TimeCost + " → " + preview.ProjectedStageTime, FormalArtRegistry.ResourceMetricPath("stage_time"), new Vector2(284, -368), "公开时序 " + run.StageTime + " → " + preview.ProjectedStageTime + "/" + AcademyMapTuning.TransitionProgress, cyan, 224, 58);
                DetailIconMetric(card.transform, "生命", "+" + preview.ExpectedHealthRecovery, FormalArtRegistry.ResourceMetricPath("health"), new Vector2(528, -368), "预计恢复生命", FormalUiTheme.Health, 224, 58);
                DetailIconMetric(card.transform, "魔力", "+" + preview.ExpectedManaRecovery, FormalArtRegistry.ResourceMetricPath("mana"), new Vector2(772, -368), "预计恢复个人魔力", FormalUiTheme.Magic, 224, 58);
                DetailIconMetric(card.transform, "阈值", threshold, FormalArtRegistry.SemanticPath("notice"), new Vector2(1016, -368), threshold, preview.CrossesTransition ? danger : preview.CrossesWarning ? amber : safe, 224, 58);
                briefingTooltip = preview.FailureConsequence + "\n奖励：" + preview.RewardLabel;
            }
            else Label("规则", briefingTooltip, card.transform, new Vector2(40, -368), new Vector2(1200, 58), 18, muted, TextAnchor.UpperLeft);
            GameObject start = ActionButton("开始战斗", "载入战斗地图并锁定本场战斗快照", card.transform, new Vector2(40, -454), new Vector2(580, 142), cyan, true, bootstrap.StartDeveloperCombat, iconPath: FormalArtRegistry.NavigationPath("confirm"));
            BindHover(start, "结算规则", briefingTooltip, cyan);
            ActionButton("返回地图", "保留节点状态；不消耗资源或时序", card.transform, new Vector2(660, -454), new Vector2(580, 142), amber, bootstrap.CurrentMapRun != null, bootstrap.ReturnToMapRun, iconPath: FormalArtRegistry.NavigationPath("back"));
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

        private void DrawLoadout()
        {
            RogueliteMapRun run = bootstrap.CurrentMapRun ?? bootstrap.ArchivedMapRun;
            if (run == null || !run.UsesRogue11) { SetOverlay(UiOverlay.None); return; }
            Header("学院整备", "装备、术式与战术栏分区整备");
            GameObject card = Panel("整备总览", content.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(1760, 820), panel);
            RogueRunDto dto = run.RogueRunState;
            RogueEquipmentRuntime runtime = RogueEquipmentRuntime.FromDto(dto);
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            Dictionary<string, SpellDefinition> spells = catalog.Spells.ToDictionary(value => value.DefinitionId, StringComparer.Ordinal);
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
            else if (loadoutSection == LoadoutSection.Spells)
                DrawSpellLoadout(card.transform, dto, spells);
            else
                DrawTacticalLoadout(card.transform, runtime, items);

            ActionButton("返回地图", string.Empty, card.transform, new Vector2(1390, -750), new Vector2(330, 52), cyan, true,
                () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
        }

        private void DrawLoadoutNavigation(Transform parent, RogueEquipmentRuntime runtime, RogueRunDto dto)
        {
            LoadoutTab(parent, LoadoutSection.Equipment, "装备与背包", "11 槽 · 拖拽整理", 36, cyan, FormalArtRegistry.ItemPath("category_armor"));
            LoadoutTab(parent, LoadoutSection.Spells, "术式编组", "8 个术式槽", 310, amber, RogueSpellIconPath(dto.EquippedSpellIds.FirstOrDefault()));
            LoadoutTab(parent, LoadoutSection.Tactical, "战术栏", "4 格快捷栏", 584, safe, FormalArtRegistry.ItemPath("category_container"));
            int occupied = RogueInventoryPresentation.Build(runtime).Count;
            Label("整备摘要", "背包物品  " + occupied + "   ·   拖拽移动   ·   R / 右键旋转", parent,
                new Vector2(900, -30), new Vector2(820, 52), 18, muted, TextAnchor.MiddleRight);
        }

        private void LoadoutTab(Transform parent, LoadoutSection section, string title, string detail, float x, Color accent, string iconPath)
        {
            bool active = loadoutSection == section;
            ActionButton(title, active ? detail : string.Empty, parent, new Vector2(x, -22), new Vector2(254, 62), active ? accent : muted, true,
                () => { ClearLoadoutDrag(); loadoutSection = section; Invalidate(false); }, iconPath: iconPath);
        }

        private void DrawEquipmentLoadout(Transform parent, RogueEquipmentRuntime runtime, IReadOnlyList<RogueInventoryItemPresentation> items)
        {
            GameObject equipmentPanel = Panel("装备工作区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -104), new Vector2(600, 620), FormalUiTheme.Surface);
            Label("装备区标题", "装备槽", equipmentPanel.transform, new Vector2(28, -20), new Vector2(360, 42), 26, cyan, TextAnchor.MiddleLeft);
            Label("装备区说明", "拖入装备或替换 · 将已装备物品拖回背包卸下", equipmentPanel.transform, new Vector2(28, -62), new Vector2(540, 32), 16, muted, TextAnchor.MiddleLeft);
            OCC.Combat.Roguelite.EquipmentSlot[] slots = Enum.GetValues(typeof(OCC.Combat.Roguelite.EquipmentSlot)).Cast<OCC.Combat.Roguelite.EquipmentSlot>().ToArray();
            for (int index = 0; index < slots.Length; index++)
            {
                OCC.Combat.Roguelite.EquipmentSlot slotType = slots[index]; string instanceId = runtime.Equipped[slotType];
                EquipmentDefinition definition = runtime.DefinitionFor(instanceId); string value = definition == null ? "空" : definition.DisplayName;
                bool selected = instanceId == selectedRogueInventoryId;
                GameObject slot = ActionButton(EquipmentSlotLabel(slotType) + "  " + value, string.Empty, equipmentPanel.transform,
                    new Vector2(28 + (index % 2) * 276, -108 - (index / 2) * 72), new Vector2(260, 60), selected ? amber : cyan, true,
                    () => { if (string.IsNullOrEmpty(instanceId)) TryEquipSelected(runtime, slotType); else { selectedRogueInventoryId = instanceId; Invalidate(false); } },
                    iconPath: definition == null ? EquipmentIconPath(slotType) : FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
                loadoutEquipmentSlotRects[slotType] = slot.GetComponent<RectTransform>();
                loadoutEquipmentDropOverlays[slotType] = CreateEquipmentDropOverlay(slot.transform);
                if (!string.IsNullOrEmpty(instanceId))
                {
                    if (slot.GetComponent<CanvasGroup>() == null) slot.AddComponent<CanvasGroup>();
                    RogueLoadoutDragHandler drag = slot.AddComponent<RogueLoadoutDragHandler>();
                    drag.Configure(eventData => BeginEquippedLoadoutDrag(slotType, instanceId, slot, eventData), UpdateLoadoutDrag,
                        EndLoadoutDrag, RotateLoadoutDragPreview);
                }
                BindHover(slot, EquipmentSlotLabel(slotType), definition == null ? "选择背包中的匹配装备。" : RogueEquipmentDetail(runtime, instanceId), selected ? amber : cyan);
            }

            DrawLoadoutBackpack(parent, runtime, items, new Vector2(656, -104), new Vector2(420, 620));
            DrawRogueInventoryDetails(parent, runtime, new Vector2(1096, -104), new Vector2(628, 620));
        }

        private void DrawLoadoutBackpack(Transform parent, RogueEquipmentRuntime runtime, IReadOnlyList<RogueInventoryItemPresentation> items, Vector2 position, Vector2 size)
        {
            GameObject backpackPanel = Panel("背包工作区", parent, new Vector2(0, 1), new Vector2(0, 1), position, size, FormalUiTheme.Surface);
            Label("背包标题", "背包 6×10", backpackPanel.transform, new Vector2(24, -18), new Vector2(250, 40), 25, safe, TextAnchor.MiddleLeft);
            Label("背包提示", "拖拽整理  ·  R / 右键旋转", backpackPanel.transform, new Vector2(24, -54), new Vector2(360, 28), 15, muted, TextAnchor.MiddleLeft);
            Vector2 gridOrigin = new Vector2(52, -76);
            GameObject gridObject = Panel("战外背包网格", backpackPanel.transform, new Vector2(0, 1), new Vector2(0, 1), gridOrigin,
                new Vector2(6 * LoadoutCellSize, 10 * LoadoutCellSize), FormalUiTheme.WithAlpha(FormalUiTheme.Surface, .5f));
            loadoutGridRect = gridObject.GetComponent<RectTransform>();
            for (int y = 0; y < 10; y++)
            for (int x = 0; x < 6; x++)
                BackpackInsetCell(gridObject.transform, x, y);
            foreach (RogueInventoryItemPresentation item in items)
            {
                bool selected = item.InstanceId == selectedRogueInventoryId;
                GameObject itemButton = InventoryGridButton(item, gridObject.transform,
                    new Vector2(item.X * LoadoutCellSize, -item.Y * LoadoutCellSize), new Vector2(item.Width * LoadoutCellSize - 3, item.Height * LoadoutCellSize - 3),
                    selected ? amber : item.IsEquipment ? cyan : safe, () => { selectedRogueInventoryId = item.InstanceId; Invalidate(false); });
                RogueLoadoutDragHandler drag = itemButton.AddComponent<RogueLoadoutDragHandler>();
                drag.Configure(eventData => BeginLoadoutDrag(item, itemButton, eventData), UpdateLoadoutDrag,
                    EndLoadoutDrag, RotateLoadoutDragPreview);
                BindHover(itemButton, item.DisplayName, RogueInventoryDetailBody(runtime, item.InstanceId, false), selected ? amber : item.IsEquipment ? cyan : safe);
            }
            Label("背包交互状态", loadoutInteractionMessage, backpackPanel.transform, new Vector2(24, -588), new Vector2(370, 28), 15, muted, TextAnchor.MiddleLeft);
        }

        private void DrawTacticalLoadout(Transform parent, RogueEquipmentRuntime runtime, IReadOnlyList<RogueInventoryItemPresentation> items)
        {
            GameObject quickbarPanel = Panel("战术工作区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -104), new Vector2(600, 620), FormalUiTheme.Surface);
            Label("战术标题", "战术快捷栏", quickbarPanel.transform, new Vector2(28, -20), new Vector2(360, 42), 26, safe, TextAnchor.MiddleLeft);
            Label("战术说明", "先选中背包道具，再点击槽位关联", quickbarPanel.transform, new Vector2(28, -62), new Vector2(520, 32), 16, muted, TextAnchor.MiddleLeft);
            for (int index = 0; index < RogueRuntimeConstants.ItemQuickbarSize; index++)
            {
                int slotIndex = index; string id = runtime.ItemQuickbarInstanceIds[index]; RogueTacticalItemInstance item = runtime.TacticalItem(id);
                GameObject slot = ActionButton((index + 1) + "  " + (item == null ? "空" : runtime.TacticalDefinitionFor(id).DisplayName), item == null ? string.Empty : item.ChargesCurrent + "/" + item.ChargesMaximum,
                    quickbarPanel.transform, new Vector2(28 + (index % 2) * 276, -122 - (index / 2) * 116), new Vector2(260, 96), safe, true,
                    () => { if (runtime.TacticalItem(selectedRogueInventoryId) != null) bootstrap.AssignRogueQuickbar(selectedRogueInventoryId, slotIndex); else if (!string.IsNullOrEmpty(id)) { selectedRogueInventoryId = id; Invalidate(false); } },
                    iconPath: item == null ? FormalArtRegistry.ItemPath("category_container") : FormalArtRegistry.ItemPath(item.DefinitionId));
                BindHover(slot, "战术栏 " + (index + 1), item == null ? "选择背包中的战术道具。" : RogueInventoryDetail(runtime, id), safe);
            }
            Label("战术规则", "战斗中通过 1–4 快速使用；更换关联不会消耗道具。", quickbarPanel.transform,
                new Vector2(28, -390), new Vector2(540, 72), 17, muted, TextAnchor.UpperLeft);

            DrawLoadoutBackpack(parent, runtime, items, new Vector2(656, -104), new Vector2(420, 620));
            DrawRogueInventoryDetails(parent, runtime, new Vector2(1096, -104), new Vector2(628, 620));
        }

        private void DrawSpellLoadout(Transform parent, RogueRunDto dto, IReadOnlyDictionary<string, SpellDefinition> spells)
        {
            GameObject spellPanel = Panel("术式工作区", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -104), new Vector2(1060, 620), FormalUiTheme.Surface);
            Label("术式标题", "8 个术式槽", spellPanel.transform, new Vector2(28, -20), new Vector2(500, 42), 26, amber, TextAnchor.MiddleLeft);
            Label("术式说明", "点击术式查看消耗与规则；槽位数字对应战斗快捷键", spellPanel.transform, new Vector2(28, -62), new Vector2(900, 32), 16, muted, TextAnchor.MiddleLeft);
            selectedLoadoutSpellIndex = Mathf.Clamp(selectedLoadoutSpellIndex, 0, RogueRuntimeConstants.SpellSlotCount - 1);
            for (int index = 0; index < RogueRuntimeConstants.SpellSlotCount; index++)
            {
                int slotIndex = index;
                string id = dto.EquippedSpellIds[index];
                SpellDefinition spell = !string.IsNullOrEmpty(id) && spells.TryGetValue(id, out SpellDefinition foundSpell) ? foundSpell : null;
                string name = spell == null ? "空槽" : spell.DisplayName;
                string detail = spell == null ? "等待获得术式" : "AP " + spell.ActionPointCost + "   魔力 " + spell.ManaCost + "   CD " + spell.CooldownOwnTurns;
                bool selected = index == selectedLoadoutSpellIndex;
                GameObject slot = ActionButton((index + 1) + "  " + name, detail, spellPanel.transform,
                    new Vector2(28 + (index % 2) * 506, -112 - (index / 2) * 108), new Vector2(478, 92), selected ? amber : cyan, true,
                    () => { selectedLoadoutSpellIndex = slotIndex; Invalidate(false); }, iconPath: RogueSpellIconPath(id));
                if (spell != null) BindHover(slot, name, string.Join(" · ", spell.Rules.Select(SpellRuleText)), selected ? amber : cyan);
            }

            DrawSpellDetails(parent, dto, spells, new Vector2(1116, -104));
        }

        private void DrawSpellDetails(Transform parent, RogueRunDto dto, IReadOnlyDictionary<string, SpellDefinition> spells, Vector2 position)
        {
            GameObject detailPanel = Panel("术式详情", parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(608, 620), FormalUiTheme.Surface);
            string id = dto.EquippedSpellIds[selectedLoadoutSpellIndex];
            SpellDefinition spell = !string.IsNullOrEmpty(id) && spells.TryGetValue(id, out SpellDefinition found) ? found : null;
            if (spell == null)
            {
                Label("空术式", "空术式槽", detailPanel.transform, new Vector2(32, -30), new Vector2(540, 52), 28, muted, TextAnchor.MiddleLeft);
                Label("空术式说明", "在行动奖励中获得术式后可填入此槽。", detailPanel.transform, new Vector2(32, -100), new Vector2(540, 80), 18, muted, TextAnchor.UpperLeft);
                return;
            }
            Image icon = FormalUiKit.TopLeftIconSlot("术式图标", detailPanel.transform, Resources.Load<Sprite>(RogueSpellIconPath(id)), new Vector2(32, -30));
            icon.rectTransform.sizeDelta = new Vector2(88, 88);
            Label("术式名称", spell.DisplayName, detailPanel.transform, new Vector2(140, -28), new Vector2(430, 52), 30, text, TextAnchor.MiddleLeft);
            Label("术式槽位", "槽位 " + (selectedLoadoutSpellIndex + 1), detailPanel.transform, new Vector2(140, -78), new Vector2(430, 32), 17, amber, TextAnchor.MiddleLeft);
            DetailIconMetric(detailPanel.transform, "行动点", spell.ActionPointCost.ToString(), FormalArtRegistry.SemanticPath("action"), new Vector2(32, -144), "施放消耗的行动点", amber);
            DetailIconMetric(detailPanel.transform, "魔力", spell.ManaCost.ToString(), FormalArtRegistry.SemanticPath("aether"), new Vector2(224, -144), "施放消耗的个人魔力", cyan);
            DetailIconMetric(detailPanel.transform, "冷却", spell.CooldownOwnTurns.ToString(), FormalArtRegistry.SemanticPath("notice"), new Vector2(416, -144), "以自身回合计算的冷却", safe);
            Label("规则标题", "术式效果", detailPanel.transform, new Vector2(32, -214), new Vector2(300, 36), 20, amber, TextAnchor.MiddleLeft);
            Label("术式规则", string.Join("\n", spell.Rules.Take(6).Select(SpellRuleText)), detailPanel.transform, new Vector2(32, -262), new Vector2(540, 210), 18, text, TextAnchor.UpperLeft);
            Label("术式提示", "战斗中按对应数字键选择；不可在战斗中改装。", detailPanel.transform, new Vector2(32, -510), new Vector2(540, 42), 16, muted, TextAnchor.MiddleLeft);
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
            loadoutGrabOffset = new Vector2Int(pointerCell.X - item.X, pointerCell.Y - item.Y);
            loadoutDragSource = source.GetComponent<CanvasGroup>();
            if (loadoutDragSource == null) { CancelLoadoutDrag(); return; }
            loadoutDragSource.alpha = 0f;
            loadoutDragSource.blocksRaycasts = true;
            loadoutDragGhost = Panel("战外背包拖拽预览", loadoutGridRect, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.one,
                FormalUiTheme.WithAlpha(cyan, .42f));
            FormalUiKit.FocusFrame(loadoutDragGhost.transform);
            loadoutDragGhost.GetComponent<Image>().raycastTarget = false;
            loadoutInteractionMessage = "拖拽中 · R / 右键旋转 · 松开左键放置";
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
            loadoutInteractionMessage = "拖到背包中的目标格卸下 · R / 右键旋转";
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
            loadoutInteractionMessage = loadoutDragRotated ? "拖拽预览已旋转" : "拖拽预览为标准朝向";
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
            RectTransform rect = loadoutDragGhost.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(anchor.X * LoadoutCellSize, -anchor.Y * LoadoutCellSize);
            rect.sizeDelta = new Vector2(footprint.X * LoadoutCellSize - 3, footprint.Y * LoadoutCellSize - 3);
            bool legal = loadoutDragEquippedSlot.HasValue
                ? loadoutDragRuntime.CanUnequipToBackpack(loadoutDragEquippedSlot.Value, anchor.X, anchor.Y, loadoutDragRotated)
                : loadoutDragRuntime.CanMoveBackpack(loadoutDragId, anchor.X, anchor.Y, loadoutDragRotated);
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
                loadoutInteractionMessage = succeeded ? "已装备到" + EquipmentSlotLabel(targetSlot) : "无法装备 · 物品保持原位";
            }
            else if (IsPointerOverBackpack(eventData.position) && TryLoadoutLocalPointer(eventData.position, out Vector2 local))
            {
                submitted = true;
                int grabX = loadoutDragEquippedSlot.HasValue ? 0 : loadoutGrabOffset.x;
                int grabY = loadoutDragEquippedSlot.HasValue ? 0 : loadoutGrabOffset.y;
                RogueLoadoutGridPoint anchor = RogueLoadoutDragPresentation.AnchorForLocalPointer(local.x, local.y, LoadoutCellSize, grabX, grabY);
                succeeded = loadoutDragEquippedSlot.HasValue
                    ? bootstrap.UnequipRogueEquipmentTo(loadoutDragEquippedSlot.Value, anchor.X, anchor.Y, loadoutDragRotated)
                    : bootstrap.MoveRogueBackpackItem(loadoutDragId, anchor.X, anchor.Y, loadoutDragRotated);
                loadoutInteractionMessage = succeeded
                    ? (loadoutDragEquippedSlot.HasValue ? "已卸下到 " : "已移动到 ") + (anchor.X + 1) + "," + (anchor.Y + 1)
                    : "不可放置 · 物品保持原位";
            }
            if (!submitted) loadoutInteractionMessage = "已取消拖拽 · 物品保持原位";
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
            return RogueLoadoutDragPresentation.Footprint(equipment?.Width ?? tactical.Width, equipment?.Height ?? tactical.Height, rotated);
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
                case OCC.Combat.Roguelite.EquipmentSlot.MainHand: return "主手"; case OCC.Combat.Roguelite.EquipmentSlot.OffHand: return "副手"; case OCC.Combat.Roguelite.EquipmentSlot.Head: return "头部";
                case OCC.Combat.Roguelite.EquipmentSlot.Chest: return "胸甲"; case OCC.Combat.Roguelite.EquipmentSlot.Hands: return "手部"; case OCC.Combat.Roguelite.EquipmentSlot.Legs: return "腿部";
                case OCC.Combat.Roguelite.EquipmentSlot.Backpack: return "背架"; case OCC.Combat.Roguelite.EquipmentSlot.AetherCore: return "以太核心"; case OCC.Combat.Roguelite.EquipmentSlot.Conduit: return "导器";
                case OCC.Combat.Roguelite.EquipmentSlot.Accessory1: return "饰品一"; default: return "饰品二";
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
                    new Vector2(32, -100), new Vector2(540, 90), 18, muted, TextAnchor.UpperLeft); return;
            }
            string name; string iconPath; string type; string effects;
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(equipment.InstanceId);
                name = definition.DisplayName; iconPath = FormalArtRegistry.EquipmentIconPath(definition.DefinitionId); type = EquipmentSlotLabel(definition.Slot) + " · " + RarityLabel(equipment.Rarity);
                effects = RogueEquipmentEffects(definition, equipment);
            }
            else
            {
                TacticalItemDefinition definition = runtime.TacticalDefinitionFor(tactical.InstanceId);
                name = definition.DisplayName; iconPath = FormalArtRegistry.ItemPath(tactical.DefinitionId); type = "战术道具";
                effects = "可关联至 4 格战术栏";
            }
            Image icon = FormalUiKit.TopLeftIconSlot("物品图标", panel.transform, Resources.Load<Sprite>(iconPath), new Vector2(32, -30));
            icon.rectTransform.sizeDelta = new Vector2(88, 88);
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
                DetailIconMetric(panel.transform, "次数", tactical.ChargesCurrent + "/" + tactical.ChargesMaximum, FormalArtRegistry.ResourceMetricPath("charges"), new Vector2(400, -142), "本局剩余使用次数", safe);
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
            Label("规则", equipment != null ? "无耐久  ·  战斗中锁定装备" : "战术道具可在战术栏页关联快捷槽",
                panel.transform, new Vector2(32, -522), new Vector2(548, 54), 16, muted, TextAnchor.MiddleLeft);
        }

        private void TryEquipSelected(RogueEquipmentRuntime runtime, OCC.Combat.Roguelite.EquipmentSlot slot)
        {
            RogueEquipmentInstance selected = runtime.EquipmentItem(selectedRogueInventoryId);
            if (selected != null) bootstrap.EquipRogueEquipment(selected.InstanceId, slot);
        }

        private static OCC.Combat.Roguelite.EquipmentSlot PreferredEquipSlot(RogueEquipmentRuntime runtime, EquipmentDefinition definition)
        {
            if (definition.Slot != OCC.Combat.Roguelite.EquipmentSlot.Accessory1) return definition.Slot;
            return string.IsNullOrEmpty(runtime.Equipped[OCC.Combat.Roguelite.EquipmentSlot.Accessory1])
                ? OCC.Combat.Roguelite.EquipmentSlot.Accessory1 : OCC.Combat.Roguelite.EquipmentSlot.Accessory2;
        }

        private void BindHover(GameObject target, string title, string body, Color accent)
        {
            if (target == null || string.IsNullOrWhiteSpace(body)) return;
            FormalHoverTooltipTrigger trigger = target.GetComponent<FormalHoverTooltipTrigger>() ?? target.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () => new FormalTooltipContent(title, body, accent));
        }

        private void DetailIconMetric(Transform parent, string label, string value, string iconPath, Vector2 position, string tooltipBody, Color accent, float width = 180f, float height = 38f)
        {
            GameObject chip = Panel("指标_" + label, parent, new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(width, height), FormalUiTheme.Surface);
            float iconSize = height >= 50f ? 40f : 30f;
            Image icon = FormalUiKit.TopLeftIconSlot("图标", chip.transform, Resources.Load<Sprite>(iconPath), new Vector2(8, -(height - iconSize) * .5f));
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            Label("值", value, chip.transform, new Vector2(iconSize + 16, -4), new Vector2(width - iconSize - 24, height - 8), height >= 50f ? 18 : 15, accent, TextAnchor.MiddleLeft);
            BindHover(chip, label, tooltipBody, accent);
        }

        private static string RogueInventoryDetail(RogueEquipmentRuntime runtime, string instanceId)
            => RogueInventoryDetailBody(runtime, instanceId, true);

        private static string RogueInventoryDetailBody(RogueEquipmentRuntime runtime, string instanceId, bool includeName)
        {
            RogueEquipmentInstance equipment = runtime.EquipmentItem(instanceId);
            if (equipment != null) return RogueEquipmentDetailBody(runtime, instanceId, includeName);
            RogueTacticalItemInstance tactical = runtime.TacticalItem(instanceId); TacticalItemDefinition definition = runtime.TacticalDefinitionFor(instanceId);
            return (includeName ? definition.DisplayName + "\n" : string.Empty) + definition.Width + "×" + definition.Height + " · " + definition.ActionPointCost + " 行动点 · 剩余 " + tactical.ChargesCurrent + "/" + tactical.ChargesMaximum;
        }

        private static string RogueEquipmentDetail(RogueEquipmentRuntime runtime, string instanceId)
            => RogueEquipmentDetailBody(runtime, instanceId, true);

        private static string RogueEquipmentDetailBody(RogueEquipmentRuntime runtime, string instanceId, bool includeName)
        {
            RogueEquipmentInstance item = runtime.EquipmentItem(instanceId); EquipmentDefinition definition = runtime.DefinitionFor(instanceId);
            RogueContentCatalog catalog = RogueContentCatalog.CreateAcademyV01();
            string affixes = string.Join("、", item.MutableAffixIds.Select(id => catalog.Affixes.FirstOrDefault(value => value.AffixId == id)?.DisplayName ?? "未知词条"));
            return (includeName ? definition.DisplayName + "\n" : string.Empty) + RarityLabel(item.Rarity) + " · " + definition.Width + "×" + definition.Height + " · 重量 " + definition.BaseWeight + " · 以太负荷 " + definition.BaseAetherLoad +
                (definition.TurnStartShield > 0 ? "\n回合盾 +" + definition.TurnStartShield : string.Empty) +
                (item.MutableAffixIds.Count > 0 ? "\n词条：" + affixes : string.Empty);
        }

        private static string MapStateTooltip(RogueliteMapNodeVisualState state)
        {
            switch (state)
            {
                case RogueliteMapNodeVisualState.Current: return "你现在所在的节点。";
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
            IEnumerable<string> affixes = item.MutableAffixIds.Select(id => "词条 · " +
                (catalog.Affixes.FirstOrDefault(value => value.AffixId == id)?.DisplayName ?? "未知词条"));
            IEnumerable<string> upgrades = item.UpgradeBranchIds.Select(value =>
            {
                int separator = value.IndexOf(':');
                return "校准 · " + PlayerEquipmentEffect(separator >= 0 ? value.Substring(separator + 1) : value);
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
                case "first_fire_spell_free_facing": return "每场首个火术可免费转向";
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

        private static string RogueSpellIconPath(string definitionId)
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
            Header("行动档案", string.Empty);
            RogueliteMapRun run = bootstrap.ArchivedMapRun;
            GameObject card = FormalUiKit.LayoutPanel("档案卡", content.transform, "archive.card", panel);
            Label("标题", run == null ? "暂无行动记录" : "首区行动 · 种子 " + run.Seed, card.transform, new Vector2(48, -42), new Vector2(940, 48), 32, text, TextAnchor.MiddleLeft);
            if (run == null)
                FormalUiEffects.AddEmptyIllustration(card.transform, "empty_archive_tray", new Vector2(512, -314), 256f);
            if (run != null)
            {
                if (run.UsesRogue11)
                {
                    RogueliteMapNode rogueCurrent = RogueliteMapCatalog.Node(run.CurrentNodeId);
                    ArchiveMetric(card.transform, new Vector2(48, -132), new Vector2(440, 64), "当前位置", rogueCurrent.DisplayName, cyan);
                    ArchiveMetric(card.transform, new Vector2(508, -132), new Vector2(220, 64), "已访问", run.VisitedNodes.Count.ToString(), safe);
                    ArchiveMetric(card.transform, new Vector2(748, -132), new Vector2(240, 64), "已完成", run.CompletedNodes.Count.ToString(), safe);
                    ArchiveMetric(card.transform, new Vector2(48, -212), new Vector2(216, 56), "金币", run.Gold.ToString(), amber);
                    ArchiveMetric(card.transform, new Vector2(284, -212), new Vector2(216, 56), "阶段贡献", run.StageContribution.ToString(), safe);
                    ArchiveMetric(card.transform, new Vector2(520, -212), new Vector2(216, 56), "公开时间", run.StageTime.ToString(), cyan);
                    ArchiveMetric(card.transform, new Vector2(756, -212), new Vector2(216, 56), "核心许可", run.ProgressPermits.ToString(), danger);
                    Label("构筑标题", FireRogueliteStarterCatalog.DisplayName(run.StarterId) + " · 生命 " + run.CurrentHealth + " · 个人魔力 " + run.CurrentMana,
                        card.transform, new Vector2(48, -292), new Vector2(940, 26), 18, cyan, TextAnchor.MiddleLeft);
                    string equipped = string.Join("  /  ", run.RogueRunState.EquippedSpellIds.Select((id, index) => (index + 1) + "：" + FireSpellDisplayName(id)));
                    Label("八槽", equipped, card.transform, new Vector2(48, -338), new Vector2(924, 92), 14, amber, TextAnchor.UpperLeft);
                    Label("装备", "装备实例 " + run.RogueRunState.EquipmentInstances.Count + " · 战术道具 " + run.RogueRunState.TacticalItemInstances.Count + " · 普通盾不跨战",
                        card.transform, new Vector2(48, -448), new Vector2(924, 40), 16, muted, TextAnchor.UpperLeft);
                    ActionButton("返回", string.Empty, card.transform, new Vector2(520, -638), new Vector2(472, 48), cyan, true, () => SetOverlay(UiOverlay.None), iconPath: FormalArtRegistry.NavigationPath("back"));
                    return;
                }
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
                ArchiveMetric(card.transform, new Vector2(48, -398), new Vector2(292, 72), "主手武器", RewardDisplayName(run.EquippedWeaponId, "学院训练武器"), cyan);
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

        private GameObject ActionButton(string title, string detail, Transform parent, Vector2 position, Vector2 size, Color accent, bool interactable, Action action, string focusKey = null, string iconPath = null)
        {
            GameObject result = Panel(string.IsNullOrEmpty(focusKey) ? "按钮_" + title : focusKey, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, interactable ? FormalUiTheme.Interactive : FormalUiTheme.Disabled);
            Image image = result.GetComponent<Image>();
            Button button = result.AddComponent<Button>(); button.targetGraphic = image; button.interactable = interactable;
            if (action != null) button.onClick.AddListener(() => action());
            Line(result.transform, new Vector2(0, 0), new Vector2(4, size.y), interactable ? accent : muted);
            int titleSize = FormalUiTheme.ButtonFontSize + (size.y >= 96f ? 6 : size.y >= 64f ? 2 : 0);
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
            Vector2 artSize = item.IsEquipment
                ? item.Rotated ? new Vector2(size.y - 8, size.x - 8) : new Vector2(size.x - 8, size.y - 8)
                : Vector2.one * Mathf.Clamp(Mathf.Min(size.x, size.y) - 10, 24, 48);
            iconRect.sizeDelta = artSize;
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
            float iconSize = iconOnly ? Mathf.Clamp(Mathf.Min(buttonWidth, buttonHeight) - 16f, 24f, 48f)
                : buttonHeight >= 96f ? 56f : buttonHeight >= 64f ? 40f : 28f;
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
            rect.anchoredPosition = position; rect.sizeDelta = size;
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
            return icon;
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

        private static Vector2 NodePosition(RogueliteMapNode node)
            => AcademyMap3DLayout.ProjectMapToCanvas(AcademyMapVisualLayout.AnchorFor(node).SourcePosition);

        public static string MapRegionId(RogueliteMapNode node)
        {
            return AcademyMapVisualLayout.AnchorFor(node).RegionId;
        }

        private static string MapRegionLabel(string id)
        {
            switch (id)
            {
                case "teaching_archive": return "教学档案区";
                case "training_workshop": return "训练工坊区";
                case "market_infirmary": return "市集医务区";
                case "campus_wilds": return "校园荒野区";
                case "sealed_tower": return "封存高塔区";
                default: return "中庭宿舍区";
            }
        }

        private static void ApplyFormalMapBoard(GameObject mapPanel)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.MapDecorPath("academy_network"));
            if (sprite == null) throw new KeyNotFoundException("Missing formal academy map board");
            Image image = mapPanel.GetComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.color = Color.white;
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

        private GameObject FormalMapNodeButton(Transform parent, Vector2 position, RogueliteMapNodeVisualState state, Color accent, Action action, string focusKey)
        {
            GameObject result = Panel(focusKey, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(48, 48), Color.white);
            Image image = result.GetComponent<Image>(); image.sprite = Resources.Load<Sprite>(FormalArtRegistry.MapNodeMarkerPath(state.ToString())); image.type = Image.Type.Simple; image.color = Color.white;
            Button button = result.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f, 1f, 1f, .88f); colors.pressedColor = FormalUiTheme.WithAlpha(accent, .82f); colors.selectedColor = Color.white; colors.fadeDuration = .06f; button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            FormalUiKit.ConfigureButtonFeedback(button, FormalUiButtonPalette.ForAccent(Color.white, accent),
                () => UiMotionProfile.FromIntensity(bootstrap.UiPreferences.AnimationIntensity), bootstrap.ShowUiFeedback);
            if (!focusTargets.ContainsKey(result.name)) focusTargets.Add(result.name, result);
            return result;
        }
        private static string AcademyPhaseLabel(AcademyMapPhase phase) => phase == AcademyMapPhase.Consolidation ? "学期收束" : phase == AcademyMapPhase.TransitionReady ? "阶段转换待定" : "正常学期";
        private static string TypeLabel(RogueliteMapNodeType type) => type == RogueliteMapNodeType.Combat ? "战斗" : type == RogueliteMapNodeType.Elite ? "精英" : type == RogueliteMapNodeType.Event ? "事件" : type == RogueliteMapNodeType.Workshop ? "工坊" : type == RogueliteMapNodeType.Shop ? "商店" : type == RogueliteMapNodeType.Rest ? "休整" : type == RogueliteMapNodeType.Treasure ? "库房" : type == RogueliteMapNodeType.Finale ? "核心" : "入口";
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
