using System;
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
        private readonly Dictionary<GridPosition, CellView> cells = new Dictionary<GridPosition, CellView>();
        private readonly BattlefieldViewportInputController input = new BattlefieldViewportInputController();
        private IBattlefieldViewHost host;
        private Canvas canvas;
        private GameObject root;
        private RectTransform viewportRect;
        private RectTransform boardRect;
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
        private Texture2D moveIntentTexture;
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
                input.Reset();
                return;
            }

            EnsureCells(state.Map.Width, state.Map.Height);
            UpdateInput();
            RefreshGeometry(host.BattlefieldViewport);
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
            moveIntentTexture = Resources.Load<Texture2D>(FormalArtRegistry.IntentPath("move"));
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
            home.transform.SetAsLastSibling();

            tooltipRoot = FormalUiKit.Panel("敌情速览卡", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -76f), new Vector2(404f, 142f), FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .98f));
            tooltipRoot.GetComponent<Image>().raycastTarget = false;
            tooltipPortrait = TooltipRawIcon("敌人头像", tooltipRoot.transform, new Vector2(12f, -14f), 64f);
            tooltipName = FormalUiKit.Label("敌人名称", string.Empty, tooltipRoot.transform, new Vector2(88f, -10f),
                new Vector2(198f, 26f), 18, FormalUiTheme.Danger, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(tooltipName);
            Text hint = FormalUiKit.Label("锁定提示", "右键锁定", tooltipRoot.transform, new Vector2(300f, -12f),
                new Vector2(90f, 22f), 12, FormalUiTheme.Cyan, TextAnchor.MiddleRight);
            FormalUiKit.PreventAutomaticWrapping(hint);
            tooltipHealth = TooltipMetric("生命", "Art/FormalResourceIcons32/health", tooltipRoot.transform,
                new Vector2(88f, -42f), FormalUiTheme.Danger);
            tooltipShield = TooltipMetric("护盾", "Art/FormalResourceIcons32/shield", tooltipRoot.transform,
                new Vector2(176f, -42f), FormalUiTheme.Shield);
            tooltipArmor = TooltipMetric("护甲", FormalArtRegistry.ItemPath("category_armor"), tooltipRoot.transform,
                new Vector2(264f, -42f), FormalUiTheme.Muted);
            tooltipWeaponIcon = TooltipSpriteIcon("武器", tooltipRoot.transform, new Vector2(88f, -76f), 24f);
            tooltipWeapon = FormalUiKit.Label("武器读数", string.Empty, tooltipRoot.transform, new Vector2(118f, -76f),
                new Vector2(144f, 24f), 13, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(tooltipWeapon);
            tooltipIntentIcon = TooltipRawIcon("意图", tooltipRoot.transform, new Vector2(270f, -76f), 24f);
            tooltipIntent = FormalUiKit.Label("意图读数", string.Empty, tooltipRoot.transform, new Vector2(300f, -76f),
                new Vector2(90f, 24f), 13, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(tooltipIntent);
            for (int i = 0; i < tooltipStatusRoots.Length; i++)
            {
                GameObject status = FormalUiKit.Create("状态_" + i, tooltipRoot.transform);
                RectTransform statusRect = status.AddComponent<RectTransform>();
                SetTopLeft(statusRect, 88f + i * 34f, 110f, 28f, 24f);
                tooltipStatusRoots[i] = status;
                tooltipStatusIcons[i] = status.AddComponent<RawImage>();
                tooltipStatusIcons[i].raycastTarget = false;
                tooltipStatusValues[i] = Label("回合", statusRect);
                tooltipStatusValues[i].fontSize = 10;
                tooltipStatusValues[i].fontStyle = FontStyle.Bold;
                tooltipStatusValues[i].alignment = TextAnchor.LowerRight;
            }
            tooltipRoot.SetActive(false);
        }

        private void EnsureCells(int width, int height)
        {
            if (mapWidth == width && mapHeight == height && cells.Count == width * height) return;
            foreach (CellView cell in cells.Values) Destroy(cell.Root);
            cells.Clear();
            mapWidth = width;
            mapHeight = height;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                GridPosition position = new GridPosition(x, y);
                CellView cell = CreateCell(position);
                cells.Add(position, cell);
            }
        }

        private CellView CreateCell(GridPosition position)
        {
            GameObject rootObject = FormalUiKit.Create("格子_" + position.X + "_" + position.Y, boardRect);
            RectTransform rect = rootObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            RawImage floor = rootObject.AddComponent<RawImage>();
            floor.raycastTarget = true;
            Outline boundary = rootObject.AddComponent<Outline>();
            boundary.effectColor = new Color(.27f, .34f, .37f, .78f);
            boundary.effectDistance = new Vector2(1f, -1f);
            BattlefieldCellPointer pointer = rootObject.AddComponent<BattlefieldCellPointer>();
            pointer.Initialize(position, SubmitCell, ShowTooltip, HideTooltip);

            var cell = new CellView
            {
                Root = rootObject,
                Rect = rect,
                Floor = floor,
                Environment = Layer("环境效果", rect),
                Move = Layer("移动范围", rect),
                Attack = Layer("攻击范围", rect),
                Skill = Layer("技能范围", rect),
                IntentDestination = CreateIntentDestination("移动意图目标", rect),
                Object = Layer("地形物件", rect),
                Loot = Layer("战利品", rect),
                Unit = Layer("单位", rect),
                Selection = Layer("选中覆盖", rect),
                ObjectLabel = Label("地形标签", rect),
                Health = Bar("生命", rect, FormalUiTheme.Health),
                Shield = Bar("护盾", rect, FormalUiTheme.Shield),
                IntentRoot = FormalUiKit.Create("敌人意图", rect)
            };
            cell.IntentRect = cell.IntentRoot.AddComponent<RectTransform>();
            cell.IntentRect.anchorMin = cell.IntentRect.anchorMax = cell.IntentRect.pivot = new Vector2(0f, 1f);
            Image intentBackground = cell.IntentRoot.AddComponent<Image>();
            intentBackground.color = new Color(.025f, .045f, .052f, .96f);
            intentBackground.raycastTarget = false;
            cell.IntentIcon = Layer("意图图标", cell.IntentRect);
            cell.IntentDamage = Label("意图伤害", cell.IntentRect);
            cell.IntentDamage.alignment = TextAnchor.MiddleCenter;
            cell.IntentDamage.fontSize = 14;
            cell.IntentDamage.fontStyle = FontStyle.Bold;
            cell.IntentDamage.color = new Color(1f, .87f, .72f);
            cell.StatusRoots = new GameObject[6];
            cell.StatusIcons = new RawImage[6];
            cell.StatusValues = new Text[6];
            for (int i = 0; i < 6; i++)
            {
                GameObject status = FormalUiKit.Create("状态_" + i, rect);
                RectTransform statusRect = status.AddComponent<RectTransform>();
                statusRect.anchorMin = statusRect.anchorMax = statusRect.pivot = new Vector2(0f, 1f);
                cell.StatusRoots[i] = status;
                cell.StatusIcons[i] = status.AddComponent<RawImage>();
                cell.StatusIcons[i].raycastTarget = false;
                cell.StatusValues[i] = Label("数值", statusRect);
                cell.StatusValues[i].fontSize = 10;
                cell.StatusValues[i].fontStyle = FontStyle.Bold;
                cell.StatusValues[i].alignment = TextAnchor.LowerRight;
            }
            return cell;
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
                SetTopLeft(pair.Value.Rect, pair.Key.X * cellSize, (mapHeight - 1 - pair.Key.Y) * cellSize, cellSize, cellSize);
        }

        private void RefreshCell(CellView cell, BattlefieldCellPresentation model, BattlefieldViewport viewport,
            bool isIntentDestination)
        {
            if (model == null || viewport == null) { cell.Root.SetActive(false); return; }
            cell.Root.SetActive(true);
            Set(cell.Floor, model.FloorTexture, Color.white);
            Set(cell.Environment, model.EnvironmentTexture, Color.white);
            Set(cell.Move, model.MoveOverlayTexture, new Color(1f, 1f, 1f, model.MoveOverlayAlpha));
            Set(cell.Attack, model.AttackOverlayTexture, new Color(1f, 1f, 1f, model.AttackOverlayAlpha));
            Set(cell.Skill, model.SkillOverlayTexture, Color.white);
            cell.IntentDestination.Root.SetActive(isIntentDestination);
            if (isIntentDestination)
            {
                float markerSize = Mathf.Max(14f, viewport.CellSize * .1875f);
                SetTopLeft(cell.IntentDestination.Icon.rectTransform,
                    (viewport.CellSize - markerSize) * .5f, (viewport.CellSize - markerSize) * .5f,
                    markerSize, markerSize);
                cell.IntentDestination.Icon.texture = moveIntentTexture;
            }
            Set(cell.Selection, model.SelectionOverlayTexture, FormalUiTheme.Cyan);
            Set(cell.Object, model.ObjectTexture, Color.white);
            Set(cell.Loot, model.LootTexture, Color.white);
            float cellSize = viewport.CellSize;
            SetInset(cell.Object.rectTransform, cellSize * .0625f);
            SetInset(cell.Loot.rectTransform, cellSize * .09375f);

            cell.ObjectLabel.gameObject.SetActive(!string.IsNullOrEmpty(model.ObjectLabel));
            cell.ObjectLabel.text = model.ObjectLabel;
            cell.ObjectLabel.color = model.ObjectLabelColor;
            SetTopLeft(cell.ObjectLabel.rectTransform, 2f, 18f * cellSize / 128f, cellSize - 4f, 22f * cellSize / 128f);

            cell.Unit.gameObject.SetActive(model.UnitTexture != null);
            if (model.UnitTexture != null)
            {
                BattlefieldRect contract = viewport.CellRect(model.Position);
                Rect unit = CombatUnitHudLayout.UnitVisibleContentRect(contract, model.UnitTexture.name);
                float localX = unit.x - contract.X + model.UnitOffset.x;
                float localY = unit.y - contract.Y + model.UnitOffset.y;
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
            bar.Fill.color = fillColor;
            bar.Forecast.color = FormalUiTheme.WithAlpha(forecastColor, .82f);
            Rect absolute = health ? CombatUnitHudLayout.UnitHealthBarRect(cell) : CombatUnitHudLayout.UnitShieldBarRect(cell);
            SetTopLeft(bar.Rect, absolute.x - cell.X, absolute.y - cell.Y, absolute.width, absolute.height);
            bar.Fill.rectTransform.anchorMax = new Vector2(vital.RemainingRatio, 1f);
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
            SetTopLeft(cell.IntentIcon.rectTransform, 2f, 2f, 16f, 16f);
            cell.IntentIcon.texture = texture;
            cell.IntentIcon.color = Color.white;
            SetTopLeft(cell.IntentDamage.rectTransform, 19f, 0f, absolute.width - 20f, absolute.height);
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

        private void SubmitCell(GridPosition position, bool inspection)
        {
            if (!host.IsInteractionModalOpen) host.SubmitBattlefieldCell(position, inspection);
        }

        private void ShowTooltip(GridPosition position)
        {
            if (!cells.TryGetValue(position, out CellView cell)) return;
            BattlefieldCellPresentation model = host.PresentBattlefieldCell(position);
            if (model?.Unit == null || model.Unit.IsHero) { HideTooltip(); return; }
            UnitState enemy = model.Unit;
            tooltipPortrait.texture = model.UnitTexture;
            tooltipPortrait.uvRect = model.UnitUv;
            tooltipName.text = enemy.DisplayName;
            tooltipHealth.text = enemy.Health + "/" + enemy.MaxHealth;
            tooltipShield.text = enemy.Shield + "/" + enemy.MaxShield;
            tooltipArmor.text = enemy.EffectiveArmor.ToString();
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
            Text label = FormalUiKit.Label(name, string.Empty, parent, Vector2.zero, Vector2.zero, 12,
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
            Image icon = TooltipSpriteIcon(name + "图标", parent, position, 22f);
            icon.sprite = Resources.Load<Sprite>(iconPath);
            icon.color = color;
            Text value = FormalUiKit.Label(name + "数值", string.Empty, parent,
                new Vector2(position.x + 26f, position.y), new Vector2(58f, 22f), 13, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            FormalUiKit.PreventAutomaticWrapping(value);
            return value;
        }

        private static IntentDestinationView CreateIntentDestination(string name, Transform parent)
        {
            GameObject root = FormalUiKit.Create(name, parent);
            RectTransform rect = root.AddComponent<RectTransform>();
            Stretch(rect);
            Image fill = root.AddComponent<Image>();
            fill.color = FormalUiTheme.WithAlpha(FormalUiTheme.Amber, .18f);
            fill.raycastTarget = false;
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = FormalUiTheme.WithAlpha(FormalUiTheme.Amber, .92f);
            outline.effectDistance = new Vector2(2f, -2f);
            RawImage icon = Layer("移动落点", rect);
            icon.color = FormalUiTheme.Amber;
            root.SetActive(false);
            return new IntentDestinationView { Root = root, Icon = icon };
        }

        private static BarView Bar(string name, Transform parent, Color color)
        {
            GameObject root = FormalUiKit.Create(name, parent);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            Image background = root.AddComponent<Image>();
            background.color = FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .94f);
            background.raycastTarget = false;
            Image fill = ChildImage("当前", rect, color);
            Image forecast = ChildImage("预估损失", rect, FormalUiTheme.WithAlpha(FormalUiTheme.Danger, .82f));
            Text value = Label("数值", rect);
            value.color = FormalUiTheme.Text;
            value.fontStyle = FontStyle.Bold;
            value.alignment = TextAnchor.MiddleCenter;
            value.verticalOverflow = VerticalWrapMode.Overflow;
            Outline outline = value.gameObject.AddComponent<Outline>();
            outline.effectColor = FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .98f);
            outline.effectDistance = new Vector2(1f, -1f);
            Stretch(value.rectTransform);
            return new BarView { Root = root, Rect = rect, Fill = fill, Forecast = forecast, Value = value };
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

        private static void SetInset(RectTransform rect, float inset) =>
            SetTopLeft(rect, inset, inset, rect.parent is RectTransform parent ? parent.rect.width - inset * 2f : 0f,
                rect.parent is RectTransform sameParent ? sameParent.rect.height - inset * 2f : 0f);

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
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
            public RawImage Floor;
            public RawImage Environment;
            public RawImage Move;
            public RawImage Attack;
            public RawImage Skill;
            public IntentDestinationView IntentDestination;
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

        private sealed class IntentDestinationView
        {
            public GameObject Root;
            public RawImage Icon;
        }

        private sealed class BarView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Fill;
            public Image Forecast;
            public Text Value;
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
        private Action<GridPosition, bool> click;
        private Action<GridPosition> enter;
        private Action exit;

        public void Initialize(GridPosition value, Action<GridPosition, bool> onClick, Action<GridPosition> onEnter, Action onExit)
        {
            position = value;
            click = onClick;
            enter = onEnter;
            exit = onExit;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) click?.Invoke(position, false);
            else if (eventData.button == PointerEventData.InputButton.Right) click?.Invoke(position, true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!FormalBattlefieldView.ShouldInspectOnPointerDown(eventData.button)) return;
            click?.Invoke(position, true);
            eventData.Use();
        }

        public void OnPointerEnter(PointerEventData eventData) => enter?.Invoke(position);
        public void OnPointerExit(PointerEventData eventData) => exit?.Invoke();
    }
}
