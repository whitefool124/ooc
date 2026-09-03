using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class FormalBattlefieldView : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float DoubleClickWindowSeconds = .32f;
        private const float ContextMenuWidth = 560f;
        private const float ContextMenuHeaderHeight = 84f;
        private const float ContextMenuRowHeight = 84f;
        private const float ContextMenuButtonHeight = 76f;
        private const float ContextMenuPadding = 12f;
        private const int ContextMenuTitleFontSize = FormalUiTheme.BodyFontSize;
        private const int ContextMenuActionFontSize = FormalUiTheme.BodyFontSize;
        private const int ContextMenuDetailFontSize = FormalUiTheme.BodyFontSize;
        private readonly Dictionary<GridPosition, CellView> cells = new Dictionary<GridPosition, CellView>();
        private readonly BattlefieldViewportInputController input = new BattlefieldViewportInputController();
        private IBattlefieldViewHost host;
        private Canvas canvas;
        private GameObject root;
        private RectTransform viewportRect;
        private RectTransform boardRect;
        private RectTransform structureLayerRect;
        private RectTransform unitLayerRect;
        private RectTransform overlayLayerRect;
        private readonly List<RawImage> structures = new List<RawImage>();
        private string structureLevelId;
        private GameObject tooltipRoot;
        private RawImage tooltipPortrait;
        private Text tooltipName;
        private Text tooltipHealth;
        private Text tooltipShield;
        private Text tooltipArmor;
        private Image tooltipWeaponIcon;
        private Text tooltipWeapon;
        private RawImage tooltipIntentIcon;
        private Text tooltipIntent;
        private readonly GameObject[] tooltipStatusRoots = new GameObject[6];
        private readonly RawImage[] tooltipStatusIcons = new RawImage[6];
        private readonly Text[] tooltipStatusValues = new Text[6];
        private Texture2D moveIntentFrameTexture;
        private FormalHoverTooltip cellTooltip;
        private object cellTooltipOwner;
        private GameObject contextMenuRoot;
        private RectTransform contextMenuPanel;
        private Text contextMenuTitle;
        private Text contextMenuHint;
        private readonly List<Button> contextMenuButtons = new List<Button>();
        private readonly List<Text> contextMenuButtonLabels = new List<Text>();
        private readonly List<Text> contextMenuButtonDetails = new List<Text>();
        private Coroutine pendingPrimaryClick;
        private GridPosition pendingPrimaryPosition;
        private bool hasPendingPrimaryPosition;
        private bool submitPendingPrimaryOnTimeout;
        private int contextMenuOpenedFrame = -1;
        private int mapWidth;
        private int mapHeight;

        public bool IsVisible => root != null && root.activeSelf;
        public int CellCount => cells.Count;
        public static bool ShouldInspectOnPointerDown(PointerEventData.InputButton button) =>
            button == PointerEventData.InputButton.Right;

        public void Initialize(IBattlefieldViewHost source)
        {
            host = source ?? throw new ArgumentNullException(nameof(source));
        }

        private void Update()
        {
            if (host == null || !Application.isPlaying) return;
            EnsureUi();
            CombatState state = host.CurrentState;
            bool visible = host.IsBattlefieldVisible && state != null;
            root.SetActive(visible);
            if (!visible)
            {
                HideTooltip();
                HideContextMenu();
                CancelPendingPrimaryClick();
                input.Reset();
                return;
            }

            if (contextMenuRoot != null && contextMenuRoot.activeSelf)
            {
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) HideContextMenu();
                else DismissContextMenuFromOutsideClick();
            }

            EnsureCells(state.Map.Width, state.Map.Height);
            UpdateInput();
            RefreshGeometry(host.BattlefieldViewport);
            RefreshStructures(host.CurrentLevelId, host.BattlefieldViewport);
            HashSet<GridPosition> intentDestinations = CollectIntentDestinations(state.Units.Values
                .Where(unit => unit.IsAlive && !unit.IsHero)
                .Select(unit => host.PresentBattlefieldCell(unit.Position)?.Intent));
            foreach (KeyValuePair<GridPosition, CellView> pair in cells)
                RefreshCell(pair.Value, host.PresentBattlefieldCell(pair.Key), host.BattlefieldViewport,
                    intentDestinations.Contains(pair.Key));
        }

        private void EnsureUi()
        {
            if (root != null) return;
            canvas = FormalUiKit.CanvasRoot("正式UGUI战场", UiLayoutContract.BattlefieldSortingOrder);
            root = canvas.gameObject;
            cellTooltip = root.AddComponent<FormalHoverTooltip>();
            cellTooltip.Initialize(canvas);
            moveIntentFrameTexture = Resources.Load<Texture2D>("Art/FormalTacticalOverlays32V2/move_range");
            GameObject viewport = FormalUiKit.Create("战场裁切视口", root.transform);
            viewportRect = viewport.AddComponent<RectTransform>();
            SetTopLeft(viewportRect, 0f, 0f, BattlefieldPresentationAdapter.BattlefieldWidth,
                BattlefieldPresentationAdapter.BattlefieldHeight);
            Image surface = viewport.AddComponent<Image>();
            surface.color = new Color(.012f, .022f, .027f, 1f);
            viewport.AddComponent<RectMask2D>();
            BattlefieldViewportInputSurface inputSurface = viewport.AddComponent<BattlefieldViewportInputSurface>();
            inputSurface.Initialize(this);

            GameObject board = FormalUiKit.Create("战场棋盘", viewport.transform);
            boardRect = board.AddComponent<RectTransform>();
            boardRect.anchorMin = boardRect.anchorMax = boardRect.pivot = new Vector2(0f, 1f);

            Button home = FormalUiKit.Button("战场归中", "⌂", viewport.transform,
                new Vector2(BattlefieldPresentationAdapter.BattlefieldWidth - 42f, -8f), new Vector2(32f, 32f),
                FormalUiTheme.SurfaceRaised, 18);
            home.onClick.AddListener(host.FocusBattlefieldOnHero);
            FormalUiKit.ConfigureButtonFeedback(home,
                FormalUiTheme.ButtonPalette(FormalUiButtonTone.Neutral),
                () => UiMotionProfile.FromIntensity(1f), null);
            home.transform.SetAsLastSibling();

            tooltipRoot = FormalUiKit.Panel("敌情速览卡", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -76f), new Vector2(456f, 208f), FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .99f));
            tooltipRoot.GetComponent<Image>().raycastTarget = false;
            tooltipPortrait = TooltipRawIcon("敌人头像", tooltipRoot.transform, new Vector2(12f, -14f), 64f);
            tooltipName = FormalUiKit.Label("敌人名称", string.Empty, tooltipRoot.transform, new Vector2(88f, -4f),
                new Vector2(236f, 40f), FormalUiTheme.BodyFontSize, FormalUiTheme.Danger, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(tooltipName);
            Text hint = FormalUiKit.Label("锁定提示", "右键行动", tooltipRoot.transform, new Vector2(328f, -4f),
                new Vector2(112f, 40f), FormalUiTheme.BodyFontSize, FormalUiTheme.Cyan, TextAnchor.MiddleRight);
            FormalUiKit.PreventAutomaticWrapping(hint);
            tooltipHealth = TooltipMetric("生命", "Art/FormalResourceIcons32/health", tooltipRoot.transform,
                new Vector2(88f, -46f), FormalUiTheme.Danger);
            tooltipShield = TooltipMetric("护盾", "Art/FormalResourceIcons32/shield", tooltipRoot.transform,
                new Vector2(208f, -46f), FormalUiTheme.Shield);
            tooltipArmor = TooltipMetric("护甲", FormalArtRegistry.ItemPath("category_armor"), tooltipRoot.transform,
                new Vector2(328f, -46f), FormalUiTheme.Muted);
            tooltipWeaponIcon = TooltipSpriteIcon("武器", tooltipRoot.transform, new Vector2(88f, -88f), 32f);
            tooltipWeapon = FormalUiKit.Label("武器读数", string.Empty, tooltipRoot.transform, new Vector2(126f, -84f),
                new Vector2(190f, 40f), FormalUiTheme.BodyFontSize, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(tooltipWeapon);
            tooltipIntentIcon = TooltipRawIcon("意图", tooltipRoot.transform, new Vector2(286f, -88f), 32f);
            tooltipIntent = FormalUiKit.Label("意图读数", string.Empty, tooltipRoot.transform, new Vector2(324f, -84f),
                new Vector2(116f, 40f), FormalUiTheme.BodyFontSize, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(tooltipIntent);
            for (int i = 0; i < tooltipStatusRoots.Length; i++)
            {
                GameObject status = FormalUiKit.Create("状态_" + i, tooltipRoot.transform);
                RectTransform statusRect = status.AddComponent<RectTransform>();
                SetTopLeft(statusRect, 88f + i * 44f, 140f, 40f, 40f);
                tooltipStatusRoots[i] = status;
                tooltipStatusIcons[i] = status.AddComponent<RawImage>();
                tooltipStatusIcons[i].raycastTarget = false;
                tooltipStatusValues[i] = Label("回合", statusRect);
                tooltipStatusValues[i].fontSize = FormalUiTheme.BodyFontSize;
                tooltipStatusValues[i].fontStyle = FontStyle.Normal;
                tooltipStatusValues[i].alignment = TextAnchor.LowerRight;
            }
            tooltipRoot.SetActive(false);
        }

        private void EnsureCells(int width, int height)
        {
            if (mapWidth == width && mapHeight == height && cells.Count == width * height) return;
            foreach (CellView cell in cells.Values) Destroy(cell.Root);
            if (unitLayerRect != null) Destroy(unitLayerRect.gameObject);
            if (overlayLayerRect != null) Destroy(overlayLayerRect.gameObject);
            if (structureLayerRect != null) Destroy(structureLayerRect.gameObject);
            cells.Clear();
            structures.Clear();
            structureLevelId = null;
            unitLayerRect = null;
            overlayLayerRect = null;
            structureLayerRect = null;
            mapWidth = width;
            mapHeight = height;
            EnsureUnitLayer();
            EnsureOverlayLayer();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                GridPosition position = new GridPosition(x, y);
                CellView cell = CreateCell(position);
                cells.Add(position, cell);
            }
            foreach (CellView cell in cells.OrderByDescending(pair => pair.Key.Y).ThenBy(pair => pair.Key.X).Select(pair => pair.Value))
                cell.Unit.transform.SetAsLastSibling();
            EnsureStructureLayer();
            structureLayerRect.SetAsLastSibling();
            unitLayerRect.SetAsLastSibling();
            overlayLayerRect.SetAsLastSibling();
        }

        private void EnsureStructureLayer()
        {
            if (structureLayerRect != null) return;
            GameObject layer = FormalUiKit.Create("学院多格结构层", boardRect);
            structureLayerRect = layer.AddComponent<RectTransform>();
            Stretch(structureLayerRect);
        }

        private void RefreshStructures(string levelId, BattlefieldViewport viewport)
        {
            EnsureStructureLayer();
            if (!string.Equals(levelId, structureLevelId, StringComparison.Ordinal))
            {
                foreach (RawImage structure in structures)
                    if (structure != null) Destroy(structure.gameObject);
                structures.Clear();
                structureLevelId = levelId;
                foreach (AcademyStructurePlacement placement in AcademyBattlefieldLayoutCatalog.VisualModules(levelId))
                {
                    GameObject root = FormalUiKit.Create("结构_" + placement.AssetId, structureLayerRect);
                    RawImage image = root.AddComponent<RawImage>();
                    image.texture = Resources.Load<Texture2D>("Art/FormalAcademyStructures32/" + placement.AssetId);
                    image.raycastTarget = false;
                    image.color = Color.white;
                    structures.Add(image);
                }
            }

            AcademyStructurePlacement[] placements = AcademyBattlefieldLayoutCatalog.VisualModules(levelId);
            float cellSize = viewport.CellSize;
            for (int index = 0; index < structures.Count && index < placements.Length; index++)
            {
                AcademyStructurePlacement placement = placements[index];
                float top = (mapHeight - 1 - placement.TopY) * cellSize;
                SetCenteredTopLeft(structures[index].rectTransform, placement.X * cellSize, top,
                    placement.WidthCells * cellSize, placement.HeightCells * cellSize);
                structures[index].rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * placement.QuarterTurns);
            }
        }

        private CellView CreateCell(GridPosition position)
        {
            EnsureUnitLayer();
            EnsureOverlayLayer();
            GameObject rootObject = FormalUiKit.Create("格子_" + position.X + "_" + position.Y, boardRect);
            RectTransform rect = rootObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            RawImage hitSurface = rootObject.AddComponent<RawImage>();
            hitSurface.color = Color.clear;
            hitSurface.raycastTarget = true;
            BattlefieldCellPointer pointer = rootObject.AddComponent<BattlefieldCellPointer>();
            pointer.Initialize(position, SubmitPrimaryCell, SubmitContextCell, ShowTooltip, HideTooltip);

            GameObject overlayObject = FormalUiKit.Create("格子信息_" + position.X + "_" + position.Y, overlayLayerRect);
            RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
            overlayRect.anchorMin = overlayRect.anchorMax = overlayRect.pivot = new Vector2(0f, 1f);

            var cell = new CellView
            {
                Root = rootObject,
                Rect = rect,
                OverlayRect = overlayRect,
                Floor = Layer("地面", rect),
                TerrainBoundary = Layer("地台压边", rect),
                Environment = Layer("环境效果", rect),
                Move = Layer("移动范围", rect),
                Attack = Layer("攻击范围", rect),
                Skill = Layer("技能范围", rect),
                IntentDestination = Layer("移动意图目标", rect),
                Object = Layer("地形物件", rect),
                Loot = Layer("战利品", rect),
                Unit = Layer("单位", unitLayerRect),
                Selection = Layer("选中覆盖", overlayRect),
                ObjectLabel = Label("地形标签", rect),
                Health = Bar("生命", overlayRect, FormalUiTheme.Health),
                Shield = Bar("护盾", overlayRect, FormalUiTheme.Shield),
                IntentRoot = FormalUiKit.Create("敌人意图", overlayRect)
            };
            float rangePhase = position.X * .47f + position.Y * .29f;
            cell.MoveMotion = cell.Move.gameObject.AddComponent<CombatRangeOverlayMotion>();
            cell.MoveMotion.Initialize(cell.Move, rangePhase);
            cell.AttackMotion = cell.Attack.gameObject.AddComponent<CombatRangeOverlayMotion>();
            cell.AttackMotion.Initialize(cell.Attack, rangePhase + .7f);
            cell.SkillMotion = cell.Skill.gameObject.AddComponent<CombatRangeOverlayMotion>();
            cell.SkillMotion.Initialize(cell.Skill, rangePhase + 1.4f);
            cell.IntentRect = cell.IntentRoot.AddComponent<RectTransform>();
            cell.IntentRect.anchorMin = cell.IntentRect.anchorMax = cell.IntentRect.pivot = new Vector2(0f, 1f);
            Image intentBackground = cell.IntentRoot.AddComponent<Image>();
            intentBackground.color = new Color(.025f, .045f, .052f, .96f);
            intentBackground.raycastTarget = false;
            cell.IntentIcon = Layer("意图图标", cell.IntentRect);
            cell.IntentDamage = Label("意图伤害", cell.IntentRect);
            cell.IntentDamage.alignment = TextAnchor.MiddleCenter;
            cell.IntentDamage.fontSize = FormalUiTheme.BodyFontSize;
            cell.IntentDamage.fontStyle = FontStyle.Normal;
            cell.IntentDamage.color = new Color(1f, .87f, .72f);
            cell.StatusRoots = new GameObject[6];
            cell.StatusIcons = new RawImage[6];
            cell.StatusValues = new Text[6];
            for (int i = 0; i < 6; i++)
            {
                GameObject status = FormalUiKit.Create("状态_" + i, overlayRect);
                RectTransform statusRect = status.AddComponent<RectTransform>();
                statusRect.anchorMin = statusRect.anchorMax = statusRect.pivot = new Vector2(0f, 1f);
                cell.StatusRoots[i] = status;
                cell.StatusIcons[i] = status.AddComponent<RawImage>();
                cell.StatusIcons[i].raycastTarget = false;
                cell.StatusValues[i] = Label("数值", statusRect);
                cell.StatusValues[i].fontSize = FormalUiTheme.BodyFontSize;
                cell.StatusValues[i].fontStyle = FontStyle.Normal;
                cell.StatusValues[i].alignment = TextAnchor.LowerRight;
            }
            return cell;
        }

        private void EnsureUnitLayer()
        {
            if (unitLayerRect != null) return;
            GameObject unitLayer = FormalUiKit.Create("单位独立层", boardRect);
            unitLayerRect = unitLayer.AddComponent<RectTransform>();
            Stretch(unitLayerRect);
        }

        private void EnsureOverlayLayer()
        {
            if (overlayLayerRect != null) return;
            GameObject overlayLayer = FormalUiKit.Create("单位信息顶层", boardRect);
            overlayLayerRect = overlayLayer.AddComponent<RectTransform>();
            Stretch(overlayLayerRect);
        }

        private void RefreshGeometry(BattlefieldViewport viewport)
        {
            if (viewport == null) return;
            BattlefieldRect view = viewport.ViewportRect;
            BattlefieldRect board = viewport.BoardRect;
            SetTopLeft(viewportRect, view.X, view.Y, view.Width, view.Height);
            SetTopLeft(boardRect, board.X - view.X, board.Y - view.Y, board.Width, board.Height);
            float cellSize = viewport.CellSize;
            foreach (KeyValuePair<GridPosition, CellView> pair in cells)
            {
                SetTopLeft(pair.Value.Rect, pair.Key.X * cellSize, (mapHeight - 1 - pair.Key.Y) * cellSize, cellSize, cellSize);
                SetTopLeft(pair.Value.OverlayRect, pair.Key.X * cellSize, (mapHeight - 1 - pair.Key.Y) * cellSize, cellSize, cellSize);
            }
        }

        private void RefreshCell(CellView cell, BattlefieldCellPresentation model, BattlefieldViewport viewport,
            bool isIntentDestination)
        {
            if (model == null || viewport == null) { cell.Root.SetActive(false); return; }
            cell.Root.SetActive(true);
            Set(cell.Floor, model.FloorTexture, Color.white);
            cell.Floor.uvRect = model.FloorUv;
            cell.Floor.rectTransform.localEulerAngles = new Vector3(0f, 0f, model.FloorRotationDegrees);
            Set(cell.TerrainBoundary, model.TerrainBoundaryTexture, Color.white);
            cell.TerrainBoundary.rectTransform.localEulerAngles = new Vector3(0f, 0f,
                model.TerrainBoundaryRotationDegrees);
            Set(cell.Environment, model.EnvironmentTexture, Color.white);
            cell.MoveMotion.Refresh(model.MoveOverlayTexture, model.MoveOverlayAlpha);
            cell.AttackMotion.Refresh(model.AttackOverlayTexture, model.AttackOverlayAlpha);
            cell.SkillMotion.Refresh(model.SkillOverlayTexture, 1f);
            Set(cell.IntentDestination, isIntentDestination ? moveIntentFrameTexture : null, Color.white);
            Set(cell.Selection, model.SelectionOverlayTexture, FormalUiTheme.Cyan);
            Set(cell.Object, model.ObjectTexture, Color.white);
            Set(cell.Loot, model.LootTexture, Color.white);
            float cellSize = viewport.CellSize;
            // 32x32 battlefield assets render across the complete logical cell. Their authored
            // transparent margins determine visual footprint, preserving 2x/3x/4x/5x pixel scales.
            SetInset(cell.Object.rectTransform, 0f);
            SetInset(cell.Loot.rectTransform, 0f);

            cell.ObjectLabel.gameObject.SetActive(!string.IsNullOrEmpty(model.ObjectLabel));
            cell.ObjectLabel.text = model.ObjectLabel;
            cell.ObjectLabel.color = model.ObjectLabelColor;
            SetTopLeft(cell.ObjectLabel.rectTransform, 2f, 12f * cellSize / 128f, cellSize - 4f, 40f);

            cell.Unit.gameObject.SetActive(model.UnitTexture != null);
            if (model.UnitTexture != null)
            {
                BattlefieldRect contract = viewport.CellRect(model.Position);
                Rect unit = CombatUnitHudLayout.UnitVisibleContentRect(contract, model.UnitTexture.name);
                float localX = cell.Rect.anchoredPosition.x + unit.x - contract.X + model.UnitOffset.x;
                float localY = -cell.Rect.anchoredPosition.y + unit.y - contract.Y + model.UnitOffset.y;
                SetTopLeft(cell.Unit.rectTransform, localX, localY, unit.width, unit.height);
                cell.Unit.texture = model.UnitTexture;
                cell.Unit.uvRect = model.UnitUv;
                cell.Unit.color = model.UnitTint;
            }

            bool isHero = model.Unit?.IsHero != false;
            RefreshVital(cell.Health, model.Vitals?.Health, viewport.CellRect(model.Position), true,
                CombatUnitHudLayout.HealthFillColor(isHero), CombatUnitHudLayout.HealthForecastColor(isHero));
            RefreshVital(cell.Shield, model.Vitals?.Shield, viewport.CellRect(model.Position), false,
                FormalUiTheme.Shield, FormalUiTheme.Danger);
            RefreshStatuses(cell, model.Statuses, viewport.CellRect(model.Position));
            RefreshIntent(cell, model.Intent, model.IntentTexture, viewport.CellRect(model.Position));
        }


        private static void RefreshVital(BarView bar, CombatUnitVitalPresentation vital, BattlefieldRect cell, bool health,
            Color fillColor, Color forecastColor)
        {
            bar.Root.SetActive(vital != null);
            if (vital == null) return;
            if (bar.LastCurrent >= 0 && bar.LastCurrent != vital.Current)
            {
                bar.FlashUntil = Time.unscaledTime + .28f;
                bar.MarkerUntil = Time.unscaledTime + .42f;
                bar.MarkerRatio = vital.RemainingRatio;
                bar.MarkerColor = vital.Current < bar.LastCurrent ? FormalUiTheme.Danger : FormalUiTheme.Safe;
            }
            bar.LastCurrent = vital.Current;
            float flash = bar.FlashUntil > Time.unscaledTime
                ? Mathf.PingPong((bar.FlashUntil - Time.unscaledTime) * 12f, 1f)
                : 0f;
            bar.Fill.color = Color.Lerp(fillColor, Color.white, flash * .42f);
            bar.Forecast.color = FormalUiTheme.WithAlpha(forecastColor, .82f);
            float markerFlash = bar.MarkerUntil > Time.unscaledTime
                ? Mathf.PingPong((bar.MarkerUntil - Time.unscaledTime) * 10f, 1f)
                : 0f;
            bar.Marker.rectTransform.anchorMin = new Vector2(Mathf.Clamp(bar.MarkerRatio, .02f, .98f), 0f);
            bar.Marker.rectTransform.anchorMax = new Vector2(Mathf.Clamp(bar.MarkerRatio, .02f, .98f), 1f);
            bar.Marker.color = FormalUiTheme.WithAlpha(bar.MarkerColor, markerFlash * .95f);
            Rect absolute = health ? CombatUnitHudLayout.UnitHealthBarRect(cell) : CombatUnitHudLayout.UnitShieldBarRect(cell);
            SetTopLeft(bar.Rect, absolute.x - cell.X, absolute.y - cell.Y, absolute.width, absolute.height);
            if (bar.DisplayedRatio < 0f) bar.DisplayedRatio = vital.RemainingRatio;
            bar.DisplayedRatio = Mathf.MoveTowards(bar.DisplayedRatio, vital.RemainingRatio,
                Time.unscaledDeltaTime / .18f);
            bar.Fill.rectTransform.anchorMax = new Vector2(bar.DisplayedRatio, 1f);
            bar.Forecast.gameObject.SetActive(vital.ForecastLoss > 0);
            bar.Forecast.rectTransform.anchorMin = new Vector2(vital.RemainingRatio, 0f);
            bar.Forecast.rectTransform.anchorMax = new Vector2(vital.CurrentRatio, 1f);
            string text = CombatUnitHudLayout.VitalText(vital, cell.Width);
            bar.Value.gameObject.SetActive(!string.IsNullOrEmpty(text));
            bar.Value.text = text;
            bar.Value.fontSize = CombatUnitHudLayout.VitalFontSize(cell.Width, health);
        }

        private static void RefreshStatuses(CellView cell, IReadOnlyList<BattlefieldStatusVisual> statuses, BattlefieldRect contract)
        {
            for (int i = 0; i < cell.StatusRoots.Length; i++)
            {
                bool active = statuses != null && i < statuses.Count;
                cell.StatusRoots[i].SetActive(active);
                if (!active) continue;
                Rect absolute = CombatUnitHudLayout.UnitStatusIconRect(contract, i);
                RectTransform rect = (RectTransform)cell.StatusRoots[i].transform;
                SetTopLeft(rect, absolute.x - contract.X, absolute.y - contract.Y, absolute.width, absolute.height);
                cell.StatusIcons[i].texture = statuses[i].Texture;
                cell.StatusValues[i].text = statuses[i].Presentation.ValueText;
                Stretch(cell.StatusValues[i].rectTransform);
            }
        }

        private static void RefreshIntent(CellView cell, EnemyIntentPresentation intent, Texture2D texture, BattlefieldRect contract)
        {
            bool active = intent != null && texture != null;
            cell.IntentRoot.SetActive(active);
            if (!active) return;
            Rect absolute = CombatUnitHudLayout.EnemyIntentBadgeRect(contract, intent.ExpectedDamage);
            SetTopLeft(cell.IntentRect, absolute.x - contract.X, absolute.y - contract.Y, absolute.width, absolute.height);
            Rect icon = CombatUnitHudLayout.EnemyIntentIconLocalRect();
            SetTopLeft(cell.IntentIcon.rectTransform, icon.x, icon.y, icon.width, icon.height);
            cell.IntentIcon.texture = texture;
            cell.IntentIcon.color = Color.white;
            Rect damage = CombatUnitHudLayout.EnemyIntentDamageLocalRect(absolute.width);
            SetTopLeft(cell.IntentDamage.rectTransform, damage.x, damage.y, damage.width, damage.height);
            cell.IntentDamage.text = intent.ExpectedDamage > 0 ? intent.ExpectedDamage.ToString() : string.Empty;
        }

        private void UpdateInput()
        {
            BattlefieldViewport viewport = host.BattlefieldViewport;
            if (viewport == null) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard?.homeKey.wasPressedThisFrame == true) host.FocusBattlefieldOnHero();
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            bool held = mouse.backButton.isPressed || mouse.forwardButton.isPressed;
            bool pressed = mouse.backButton.wasPressedThisFrame || mouse.forwardButton.wasPressedThisFrame;
            Vector2 pointer = BattlefieldViewportInputController.ScreenToReferenceUi(mouse.position.ReadValue(),
                Screen.width, Screen.height, ReferenceWidth, ReferenceHeight);
            input.UpdateSideButtonPan(viewport, !host.IsInteractionModalOpen, held, pressed, pointer,
                mouse.delta.ReadValue(), canvas.scaleFactor);
        }

        internal void BeginDrag(PointerEventData eventData)
        {
            bool allowed = eventData.button == PointerEventData.InputButton.Middle ||
                           (eventData.button == PointerEventData.InputButton.Left && Keyboard.current?.spaceKey.isPressed == true);
            if (allowed && !host.IsInteractionModalOpen) eventData.Use();
        }

        internal void Drag(PointerEventData eventData)
        {
            bool allowed = eventData.button == PointerEventData.InputButton.Middle ||
                           (eventData.button == PointerEventData.InputButton.Left && Keyboard.current?.spaceKey.isPressed == true);
            if (!allowed || host.IsInteractionModalOpen || host.BattlefieldViewport == null) return;
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            host.BattlefieldViewport.Pan(eventData.delta.x / scale, -eventData.delta.y / scale);
            eventData.Use();
        }

        internal void Scroll(PointerEventData eventData)
        {
            if (host.IsInteractionModalOpen || host.BattlefieldViewport == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, eventData.position,
                    eventData.pressEventCamera, out Vector2 local)) return;
            BattlefieldRect bounds = host.BattlefieldViewport.ViewportRect;
            int direction = eventData.scrollDelta.y > 0f ? 1 : -1;
            host.BattlefieldViewport.ZoomAt(bounds.X + local.x, bounds.Y - local.y, direction);
            eventData.Use();
        }

        private void SubmitPrimaryCell(GridPosition position, int _)
        {
            if (contextMenuRoot != null && contextMenuRoot.activeSelf)
            {
                HideContextMenu();
                return;
            }
            if (host.IsInteractionModalOpen) return;

            bool canQuickMove = host.CanQuickMoveTo(position);
            if (IsConfirmedQuickMove(position, pendingPrimaryPosition, hasPendingPrimaryPosition, canQuickMove))
            {
                CancelPendingPrimaryClick();
                host.SubmitBattlefieldQuickMove(position);
                return;
            }

            if (!canQuickMove)
            {
                CancelPendingPrimaryClick();
                host.SubmitBattlefieldCell(position, false);
                return;
            }

            CancelPendingPrimaryClick();
            pendingPrimaryPosition = position;
            hasPendingPrimaryPosition = true;
            submitPendingPrimaryOnTimeout = host.ShouldDeferPrimaryClickForQuickMove(position);
            pendingPrimaryClick = StartCoroutine(SubmitPendingPrimaryClick());
        }

        public static bool IsConfirmedQuickMove(GridPosition position, GridPosition pendingPosition,
            bool hasPendingPosition, bool canQuickMove) =>
            hasPendingPosition && canQuickMove && position == pendingPosition;

        private IEnumerator SubmitPendingPrimaryClick()
        {
            yield return new WaitForSecondsRealtime(DoubleClickWindowSeconds);
            GridPosition position = pendingPrimaryPosition;
            bool submit = submitPendingPrimaryOnTimeout;
            pendingPrimaryClick = null;
            hasPendingPrimaryPosition = false;
            submitPendingPrimaryOnTimeout = false;
            if (submit && host != null && !host.IsInteractionModalOpen && IsVisible)
                host.SubmitBattlefieldCell(position, false);
        }

        private void CancelPendingPrimaryClick()
        {
            if (pendingPrimaryClick != null) StopCoroutine(pendingPrimaryClick);
            pendingPrimaryClick = null;
            hasPendingPrimaryPosition = false;
            submitPendingPrimaryOnTimeout = false;
        }

        private void SubmitContextCell(GridPosition position)
        {
            if (contextMenuRoot != null && contextMenuRoot.activeSelf)
                HideContextMenu();
            if (host.IsInteractionModalOpen) return;
            CancelPendingPrimaryClick();
            host.SubmitBattlefieldCell(position, true);
            ShowContextMenu(position);
        }

        private void ShowContextMenu(GridPosition position)
        {
            IReadOnlyList<BattlefieldContextAction> actions = host.ContextActionsAt(position);
            if (actions == null || actions.Count == 0)
            {
                host.NotifyBattlefieldContextUnavailable(position);
                return;
            }

            EnsureContextMenu();
            float height = ContextMenuHeaderHeight + actions.Count * ContextMenuRowHeight + ContextMenuPadding;
            Vector2 screen = Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
            Vector2 pointer = BattlefieldViewportInputController.ScreenToReferenceUi(screen,
                Screen.width, Screen.height, ReferenceWidth, ReferenceHeight);
            float x = Mathf.Clamp(pointer.x + 12f, 8f, ReferenceWidth - ContextMenuWidth - 8f);
            float y = Mathf.Clamp(pointer.y + 12f, 64f, ReferenceHeight - height - 8f);
            SetTopLeft(contextMenuPanel, x, y, ContextMenuWidth, height);
            contextMenuTitle.text = "位置 " + position.X + "," + position.Y + " 的可执行行动";
            contextMenuHint.text = "右键切换目标  ·  Esc 或左键空白处关闭";

            while (contextMenuButtons.Count < actions.Count)
            {
                CreateContextMenuButton();
            }

            for (int i = 0; i < contextMenuButtons.Count; i++)
            {
                Button button = contextMenuButtons[i];
                bool active = i < actions.Count;
                button.gameObject.SetActive(active);
                if (!active) continue;
                BattlefieldContextAction action = actions[i];
                RectTransform rect = button.GetComponent<RectTransform>();
                SetTopLeft(rect, ContextMenuPadding,
                    ContextMenuHeaderHeight + i * ContextMenuRowHeight,
                    ContextMenuWidth - ContextMenuPadding * 2f, ContextMenuButtonHeight);
                button.GetComponent<UiButtonFeedback>()?.RefreshLayoutPosition();
                Text label = contextMenuButtonLabels[i];
                label.text = action.Label;
                contextMenuButtonDetails[i].text = action.Detail;
                button.onClick.RemoveAllListeners();
                string actionId = action.Id;
                button.onClick.AddListener(() =>
                {
                    HideContextMenu();
                    host.SubmitBattlefieldContextAction(position, actionId);
                });
            }

            contextMenuRoot.SetActive(true);
            contextMenuRoot.transform.SetAsLastSibling();
            contextMenuOpenedFrame = Time.frameCount;
            host.SetBattlefieldContextMenuOpen(true);
        }

        private void EnsureContextMenu()
        {
            if (contextMenuRoot != null) return;
            contextMenuRoot = FormalUiKit.Create("战场右键菜单遮罩", root.transform);
            RectTransform blockerRect = contextMenuRoot.AddComponent<RectTransform>();
            Stretch(blockerRect);
            Canvas contextCanvas = contextMenuRoot.AddComponent<Canvas>();
            contextCanvas.overrideSorting = true;
            contextCanvas.sortingOrder = UiLayoutContract.InteractionSortingOrder - 1;
            contextMenuRoot.AddComponent<GraphicRaycaster>();
            Image blocker = contextMenuRoot.AddComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = false;

            GameObject panel = FormalUiKit.Panel("战场右键行动菜单", contextMenuRoot.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero,
                FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .99f));
            contextMenuPanel = panel.GetComponent<RectTransform>();
            contextMenuTitle = FormalUiKit.Label("菜单标题", string.Empty, panel.transform,
                new Vector2(16f, -4f), new Vector2(ContextMenuWidth - 32f, 40f),
                ContextMenuTitleFontSize,
                FormalUiTheme.Cyan, TextAnchor.MiddleLeft);
            contextMenuTitle.fontStyle = FontStyle.Normal;
            FormalUiKit.PreventAutomaticWrapping(contextMenuTitle);
            contextMenuHint = FormalUiKit.Label("菜单提示", string.Empty, panel.transform,
                new Vector2(16f, -40f), new Vector2(ContextMenuWidth - 32f, 40f),
                ContextMenuDetailFontSize,
                FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(contextMenuHint);
            contextMenuRoot.SetActive(false);
        }

        private void CreateContextMenuButton()
        {
            Button button = FormalUiKit.Button("位置行动_" + contextMenuButtons.Count, string.Empty,
                contextMenuPanel, new Vector2(ContextMenuPadding, -ContextMenuHeaderHeight),
                new Vector2(ContextMenuWidth - ContextMenuPadding * 2f, ContextMenuButtonHeight),
                FormalUiTheme.Interactive, ContextMenuActionFontSize);
            Text label = button.GetComponentInChildren<Text>();
            SetTopLeft(label.rectTransform, 14f, 4f,
                ContextMenuWidth - ContextMenuPadding * 2f - 28f, 40f);
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = FormalUiTheme.ResponsiveFontSize(ContextMenuActionFontSize);
            label.fontStyle = FontStyle.Normal;
            label.color = FormalUiTheme.Text;
            Text detail = FormalUiKit.Label("行动资源", string.Empty, button.transform,
                new Vector2(14f, -32f),
                new Vector2(ContextMenuWidth - ContextMenuPadding * 2f - 28f, 40f),
                ContextMenuDetailFontSize, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(detail);
            FormalUiKit.ConfigureButtonFeedback(button,
                FormalUiTheme.ButtonPalette(FormalUiButtonTone.Neutral),
                () => UiMotionProfile.FromIntensity(1f), null);
            contextMenuButtons.Add(button);
            contextMenuButtonLabels.Add(label);
            contextMenuButtonDetails.Add(detail);
        }

        private void DismissContextMenuFromOutsideClick()
        {
            if (Time.frameCount == contextMenuOpenedFrame || contextMenuPanel == null) return;
            Mouse mouse = Mouse.current;
            if (mouse == null || (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame)) return;
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(contextMenuPanel,
                    mouse.position.ReadValue(), eventCamera))
                HideContextMenu();
        }

        internal void HideContextMenu()
        {
            if (contextMenuRoot == null || !contextMenuRoot.activeSelf) return;
            contextMenuRoot.SetActive(false);
            host?.SetBattlefieldContextMenuOpen(false);
        }

        private void ShowTooltip(GridPosition position)
        {
            if (!cells.TryGetValue(position, out CellView cell)) return;
            BattlefieldCellPresentation model = host.PresentBattlefieldCell(position);
            if (model == null) { HideTooltip(); return; }
            if (model.Unit == null || model.Unit.IsHero)
            {
                if (string.IsNullOrWhiteSpace(model.HoverText)) { HideTooltip(); return; }
                HideTooltip();
                string[] lines = model.HoverText.Split(new[] { '\n' }, 2);
                string title = lines[0];
                string body = lines.Length > 1 ? lines[1] : lines[0];
                cellTooltipOwner = cell.Root;
                Vector2 pointer = Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
                cellTooltip.Show(cellTooltipOwner, new FormalTooltipContent(title, body, FormalUiTheme.Amber), pointer);
                return;
            }
            if (cellTooltipOwner != null) cellTooltip.Hide(cellTooltipOwner);
            cellTooltipOwner = null;
            UnitState enemy = model.Unit;
            tooltipPortrait.texture = model.UnitTexture;
            tooltipPortrait.uvRect = model.UnitUv;
            tooltipName.text = enemy.DisplayName;
            tooltipHealth.text = enemy.Health + "/" + enemy.MaxHealth;
            bool rogue = host.CurrentState.Ruleset == CombatRuleset.Roguelite;
            tooltipShield.text = rogue ? enemy.Shield + "（无上限）" : enemy.Shield + "/" + enemy.MaxShield;
            tooltipArmor.transform.parent.gameObject.SetActive(!rogue);
            if (!rogue) tooltipArmor.text = enemy.EffectiveArmor.ToString();
            tooltipWeaponIcon.sprite = Resources.Load<Sprite>(WeaponIconPath(enemy));
            tooltipWeapon.text = enemy.MainHand == null ? "无武器" :
                enemy.MainHand.DisplayName + "  " + enemy.MainHand.Damage + "/R" + enemy.MainHand.Range;
            bool hasIntent = model.Intent != null && model.IntentTexture != null;
            tooltipIntentIcon.gameObject.SetActive(hasIntent);
            tooltipIntent.gameObject.SetActive(hasIntent);
            if (hasIntent)
            {
                tooltipIntentIcon.texture = model.IntentTexture;
                tooltipIntent.text = CompactIntent(model.Intent);
            }
            for (int i = 0; i < tooltipStatusRoots.Length; i++)
            {
                bool active = i < model.Statuses.Count;
                tooltipStatusRoots[i].SetActive(active);
                if (!active) continue;
                tooltipStatusIcons[i].texture = model.Statuses[i].Texture;
                tooltipStatusValues[i].text = model.Statuses[i].Presentation.ValueText;
                Stretch(tooltipStatusValues[i].rectTransform);
            }
            tooltipRoot.SetActive(true);
            tooltipRoot.transform.SetAsLastSibling();
            float x = Mathf.Min(1018f, cell.Rect.anchoredPosition.x + host.BattlefieldViewport.BoardRect.X + host.BattlefieldViewport.CellSize + 18f);
            float y = Mathf.Min(714f, -cell.Rect.anchoredPosition.y + host.BattlefieldViewport.BoardRect.Y + 18f);
            ((RectTransform)tooltipRoot.transform).anchoredPosition = new Vector2(x, -Mathf.Max(64f, y));
        }

        public static string CompactIntent(EnemyIntentPresentation intent)
        {
            if (intent == null) return string.Empty;
            return intent.ExpectedDamage > 0 ? intent.ActionName + " -" + intent.ExpectedDamage : intent.ActionName;
        }

        public static HashSet<GridPosition> CollectIntentDestinations(IEnumerable<EnemyIntentPresentation> intents)
        {
            var destinations = new HashSet<GridPosition>();
            if (intents == null) return destinations;
            foreach (EnemyIntentPresentation intent in intents)
                if (intent?.HasDestination == true) destinations.Add(intent.Destination);
            return destinations;
        }

        private static string WeaponIconPath(UnitState unit)
        {
            string weaponId = unit?.MainHand?.Id;
            FormalArtEntry entry = FormalArtRegistry.Items.FirstOrDefault(candidate =>
                string.Equals(candidate.RuntimeId, weaponId, StringComparison.OrdinalIgnoreCase));
            return entry?.ResourcePath ?? FormalArtRegistry.ItemPath("category_weapon");
        }

        private void HideTooltip()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
            if (cellTooltipOwner != null) cellTooltip?.Hide(cellTooltipOwner);
            cellTooltipOwner = null;
        }

        private static RawImage Layer(string name, Transform parent)
        {
            GameObject value = FormalUiKit.Create(name, parent);
            RectTransform rect = value.AddComponent<RectTransform>();
            Stretch(rect);
            RawImage image = value.AddComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(string name, Transform parent)
        {
            Text label = FormalUiKit.Label(name, string.Empty, parent, Vector2.zero, Vector2.zero, FormalUiTheme.BodyFontSize,
                FormalUiTheme.Text, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            return label;
        }

        private static RawImage TooltipRawIcon(string name, Transform parent, Vector2 position, float size)
        {
            GameObject root = FormalUiKit.Create(name, parent);
            RectTransform rect = root.AddComponent<RectTransform>();
            SetTopLeft(rect, position.x, -position.y, size, size);
            RawImage icon = root.AddComponent<RawImage>();
            icon.raycastTarget = false;
            return icon;
        }

        private static Image TooltipSpriteIcon(string name, Transform parent, Vector2 position, float size)
        {
            GameObject root = FormalUiKit.Create(name, parent);
            RectTransform rect = root.AddComponent<RectTransform>();
            SetTopLeft(rect, position.x, -position.y, size, size);
            Image icon = root.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private static Text TooltipMetric(string name, string iconPath, Transform parent, Vector2 position, Color color)
        {
            Image icon = TooltipSpriteIcon(name + "图标", parent, position, 32f);
            icon.sprite = Resources.Load<Sprite>(iconPath);
            icon.color = color;
            Text value = FormalUiKit.Label(name + "数值", string.Empty, parent,
                new Vector2(position.x + 36f, position.y - 4f), new Vector2(80f, 40f), FormalUiTheme.BodyFontSize, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(value);
            return value;
        }

        private static BarView Bar(string name, Transform parent, Color color)
        {
            GameObject root = FormalUiKit.Create(name, parent);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            Image background = root.AddComponent<Image>();
            FormalUiKit.ApplySkin(background, "bar_track", FormalUiTheme.ResourceTrack);
            background.raycastTarget = false;
            Image fill = ChildImage("当前", rect, color);
            Image forecast = ChildImage("预估损失", rect, FormalUiTheme.WithAlpha(FormalUiTheme.Danger, .82f));
            fill.rectTransform.offsetMin = new Vector2(2f, 2f);
            fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            forecast.rectTransform.offsetMin = new Vector2(2f, 2f);
            forecast.rectTransform.offsetMax = new Vector2(-2f, -2f);
            for (int index = 1; index <= 3; index++)
            {
                float fraction = index / 4f;
                GameObject tick = FormalUiKit.FlatPanel(name + "比例刻度_" + index, rect,
                    new Vector2(fraction, 0f), new Vector2(fraction, 1f), Vector2.zero, new Vector2(2f, -4f),
                    FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .58f));
                tick.GetComponent<RectTransform>().pivot = new Vector2(.5f, .5f);
            }
            Image marker = ChildImage("变化落点", rect, Color.clear);
            marker.rectTransform.anchorMin = new Vector2(1f, 0f);
            marker.rectTransform.anchorMax = new Vector2(1f, 1f);
            marker.rectTransform.pivot = new Vector2(.5f, .5f);
            marker.rectTransform.sizeDelta = new Vector2(6f, -4f);
            Text value = Label("数值", rect);
            value.color = CombatUnitHudLayout.VitalTextColor();
            value.fontStyle = FontStyle.Normal;
            value.alignment = TextAnchor.MiddleCenter;
            value.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(value.rectTransform);
            return new BarView
            {
                Root = root, Rect = rect, Fill = fill, Forecast = forecast, Marker = marker, Value = value,
                LastCurrent = -1, DisplayedRatio = -1f, MarkerRatio = 1f, MarkerColor = FormalUiTheme.Safe
            };
        }

        private static Image ChildImage(string name, Transform parent, Color color)
        {
            GameObject root = FormalUiKit.Create(name, parent);
            RectTransform rect = root.AddComponent<RectTransform>();
            Stretch(rect);
            Image image = root.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Set(RawImage image, Texture texture, Color color)
        {
            image.gameObject.SetActive(texture != null);
            if (texture == null) return;
            image.texture = texture;
            image.color = color;
        }

        private void OnDisable()
        {
            CancelPendingPrimaryClick();
            if (contextMenuRoot != null && contextMenuRoot.activeSelf)
            {
                contextMenuRoot.SetActive(false);
                host?.SetBattlefieldContextMenuOpen(false);
            }
        }

        private static void SetInset(RectTransform rect, float inset) =>
            SetTopLeft(rect, inset, inset, rect.parent is RectTransform parent ? parent.rect.width - inset * 2f : 0f,
                rect.parent is RectTransform sameParent ? sameParent.rect.height - inset * 2f : 0f);

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        private static void SetCenteredTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x + width * .5f, -y - height * .5f);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class CellView
        {
            public GameObject Root;
            public RectTransform Rect;
            public RectTransform OverlayRect;
            public RawImage Floor;
            public RawImage TerrainBoundary;
            public RawImage Environment;
            public RawImage Move;
            public RawImage Attack;
            public RawImage Skill;
            public CombatRangeOverlayMotion MoveMotion;
            public CombatRangeOverlayMotion AttackMotion;
            public CombatRangeOverlayMotion SkillMotion;
            public RawImage IntentDestination;
            public RawImage Selection;
            public RawImage Object;
            public RawImage Loot;
            public RawImage Unit;
            public Text ObjectLabel;
            public BarView Health;
            public BarView Shield;
            public GameObject[] StatusRoots;
            public RawImage[] StatusIcons;
            public Text[] StatusValues;
            public GameObject IntentRoot;
            public RectTransform IntentRect;
            public RawImage IntentIcon;
            public Text IntentDamage;
        }

        private sealed class BarView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Fill;
            public Image Forecast;
            public Image Marker;
            public Text Value;
            public int LastCurrent;
            public float DisplayedRatio;
            public float FlashUntil;
            public float MarkerUntil;
            public float MarkerRatio;
            public Color MarkerColor;
        }
    }

    internal sealed class CombatRangeOverlayMotion : MonoBehaviour
    {
        private RawImage image;
        private Texture displayedTexture;
        private float visibility;
        private float phase;

        public void Initialize(RawImage target, float pulsePhase)
        {
            image = target;
            phase = pulsePhase;
            visibility = 0f;
            if (image != null) image.gameObject.SetActive(false);
        }

        public void Refresh(Texture target, float baseAlpha)
        {
            if (image == null) return;
            if (target != null && displayedTexture != target)
            {
                displayedTexture = target;
                image.texture = target;
                visibility = 0f;
            }

            float targetVisibility = target != null ? 1f : 0f;
            visibility = Mathf.MoveTowards(visibility, targetVisibility, Time.unscaledDeltaTime / .16f);
            bool visible = displayedTexture != null && visibility > .001f;
            image.gameObject.SetActive(visible);
            if (!visible)
            {
                if (target == null) displayedTexture = null;
                return;
            }

            float pulse = .91f + .09f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 4.2f + phase));
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(baseAlpha) * visibility * pulse);
        }
    }

    internal sealed class BattlefieldViewportInputSurface : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IScrollHandler
    {
        private FormalBattlefieldView view;
        public void Initialize(FormalBattlefieldView value) => view = value;
        public void OnBeginDrag(PointerEventData eventData) => view?.BeginDrag(eventData);
        public void OnDrag(PointerEventData eventData) => view?.Drag(eventData);
        public void OnEndDrag(PointerEventData eventData) { }
        public void OnScroll(PointerEventData eventData) => view?.Scroll(eventData);
    }

    internal sealed class BattlefieldCellPointer : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private GridPosition position;
        private Action<GridPosition, int> primaryClick;
        private Action<GridPosition> contextClick;
        private Action<GridPosition> enter;
        private Action exit;

        public void Initialize(GridPosition value, Action<GridPosition, int> onPrimaryClick,
            Action<GridPosition> onContextClick, Action<GridPosition> onEnter, Action onExit)
        {
            position = value;
            primaryClick = onPrimaryClick;
            contextClick = onContextClick;
            enter = onEnter;
            exit = onExit;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                primaryClick?.Invoke(position, Math.Max(1, eventData.clickCount));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!FormalBattlefieldView.ShouldInspectOnPointerDown(eventData.button)) return;
            contextClick?.Invoke(position);
            eventData.Use();
        }

        public void OnPointerEnter(PointerEventData eventData) => enter?.Invoke(position);
        public void OnPointerExit(PointerEventData eventData) => exit?.Invoke();
    }

}
