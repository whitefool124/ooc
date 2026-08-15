using System;
using System.Collections.Generic;
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
        private Text tooltipText;
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
            foreach (KeyValuePair<GridPosition, CellView> pair in cells)
                RefreshCell(pair.Value, host.PresentBattlefieldCell(pair.Key), host.BattlefieldViewport);
        }

        private void EnsureUi()
        {
            if (root != null) return;
            canvas = FormalUiKit.CanvasRoot("正式UGUI战场", UiLayoutContract.BattlefieldSortingOrder);
            root = canvas.gameObject;
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

            tooltipRoot = FormalUiKit.Panel("战场悬停详情", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -76f), new Vector2(456f, 196f), FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .98f));
            tooltipText = FormalUiKit.Label("详情文字", string.Empty, tooltipRoot.transform, new Vector2(14f, -12f),
                new Vector2(428f, 170f), 14, FormalUiTheme.Text, TextAnchor.UpperLeft);
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipText.verticalOverflow = VerticalWrapMode.Truncate;
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

        private void RefreshCell(CellView cell, BattlefieldCellPresentation model, BattlefieldViewport viewport)
        {
            if (model == null || viewport == null) { cell.Root.SetActive(false); return; }
            cell.Root.SetActive(true);
            Set(cell.Floor, model.FloorTexture, Color.white);
            Set(cell.Environment, model.EnvironmentTexture, Color.white);
            Set(cell.Move, model.MoveOverlayTexture, new Color(1f, 1f, 1f, model.MoveOverlayAlpha));
            Set(cell.Attack, model.AttackOverlayTexture, new Color(1f, 1f, 1f, model.AttackOverlayAlpha));
            Set(cell.Skill, model.SkillOverlayTexture, Color.white);
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
            if (model == null || string.IsNullOrEmpty(model.HoverText)) { HideTooltip(); return; }
            tooltipText.text = model.HoverText;
            tooltipRoot.SetActive(true);
            tooltipRoot.transform.SetAsLastSibling();
            float x = Mathf.Min(960f, cell.Rect.anchoredPosition.x + host.BattlefieldViewport.BoardRect.X + host.BattlefieldViewport.CellSize + 18f);
            float y = Mathf.Min(650f, -cell.Rect.anchoredPosition.y + host.BattlefieldViewport.BoardRect.Y + 18f);
            ((RectTransform)tooltipRoot.transform).anchoredPosition = new Vector2(x, -Mathf.Max(64f, y));
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
