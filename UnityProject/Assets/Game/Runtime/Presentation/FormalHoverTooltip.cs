using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public readonly struct FormalTooltipContent
    {
        public string Title { get; }
        public string Body { get; }
        public Color Accent { get; }

        public FormalTooltipContent(string title, string body, Color accent)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Accent = accent;
        }
    }

    // One shared, raycast-transparent tooltip layer keeps dense reference text out of the combat HUD.
    public sealed class FormalHoverTooltip : MonoBehaviour
    {
        private const float Width = 420f;
        private const float MinimumHeight = 122f;
        private const float MaximumHeight = 430f;
        private const float EdgeMargin = 24f;
        private Canvas canvas;
        private RectTransform layer;
        private RectTransform panel;
        private Text titleLabel;
        private Text bodyLabel;
        private object owner;

        public bool IsVisible => panel != null && panel.gameObject.activeSelf;

        public void Initialize(Canvas hostCanvas)
        {
            if (panel != null) return;
            canvas = hostCanvas != null ? hostCanvas : throw new ArgumentNullException(nameof(hostCanvas));

            GameObject layer = FormalUiKit.Create("悬浮信息层", canvas.transform);
            this.layer = layer.AddComponent<RectTransform>();
            this.layer.anchorMin = Vector2.zero;
            this.layer.anchorMax = Vector2.one;
            this.layer.offsetMin = Vector2.zero;
            this.layer.offsetMax = Vector2.zero;
            CanvasGroup group = layer.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject panelObject = FormalUiKit.AnchoredPanel("悬浮详情", layer.transform, new Vector2(.5f, .5f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(Width, MinimumHeight), new Color(.025f, .045f, .052f, .98f));
            panel = panelObject.GetComponent<RectTransform>();
            Image background = panelObject.GetComponent<Image>();
            FormalUiKit.ApplySkin(background, "panel_elevated", new Color(.025f, .045f, .052f, .98f));
            background.raycastTarget = false;

            titleLabel = FormalUiKit.Label("悬浮标题", string.Empty, panel, new Vector2(18f, -12f), new Vector2(Width - 36f, 28f),
                17, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            bodyLabel = FormalUiKit.Label("悬浮正文", string.Empty, panel, new Vector2(18f, -48f), new Vector2(Width - 36f, MaximumHeight - 64f),
                14, FormalUiTheme.Text, TextAnchor.UpperLeft);
            bodyLabel.verticalOverflow = VerticalWrapMode.Truncate;
            panelObject.SetActive(false);
        }

        public void Show(object source, FormalTooltipContent content, Vector2 screenPosition)
        {
            if (panel == null || source == null || string.IsNullOrWhiteSpace(content.Body)) return;
            owner = source;
            titleLabel.text = content.Title;
            titleLabel.color = content.Accent;
            bodyLabel.text = content.Body;
            bodyLabel.rectTransform.sizeDelta = new Vector2(Width - 36f, MaximumHeight - 64f);
            float height = Mathf.Clamp(68f + bodyLabel.preferredHeight, MinimumHeight, MaximumHeight);
            panel.sizeDelta = new Vector2(Width, height);
            bodyLabel.rectTransform.sizeDelta = new Vector2(Width - 36f, height - 62f);
            layer.SetAsLastSibling();
            panel.SetAsLastSibling();
            panel.gameObject.SetActive(true);
            Move(source, screenPosition);
        }

        public void Move(object source, Vector2 screenPosition)
        {
            if (!ReferenceEquals(owner, source) || !IsVisible) return;
            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvas.worldCamera, out Vector2 local)) return;
            Vector2 preferred = local + new Vector2(18f, -18f);
            panel.anchoredPosition = ClampLocalPosition(canvasRect.rect, preferred, panel.sizeDelta, EdgeMargin);
        }

        public void Hide(object source)
        {
            if (!ReferenceEquals(owner, source)) return;
            owner = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public static Vector2 ClampLocalPosition(Rect bounds, Vector2 preferredTopLeft, Vector2 size, float margin)
        {
            float minX = bounds.xMin + margin;
            float maxX = Mathf.Max(minX, bounds.xMax - size.x - margin);
            float minY = bounds.yMin + size.y + margin;
            float maxY = Mathf.Max(minY, bounds.yMax - margin);
            return new Vector2(Mathf.Clamp(preferredTopLeft.x, minX, maxX), Mathf.Clamp(preferredTopLeft.y, minY, maxY));
        }
    }

    public sealed class FormalHoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private FormalHoverTooltip tooltip;
        private Func<FormalTooltipContent> contentProvider;

        public void Configure(FormalHoverTooltip host, Func<FormalTooltipContent> provider)
        {
            tooltip = host;
            contentProvider = provider;
        }

        public void OnPointerEnter(PointerEventData eventData) => Show(eventData.position);
        public void OnPointerMove(PointerEventData eventData) => tooltip?.Move(this, eventData.position);
        public void OnPointerExit(PointerEventData eventData) => tooltip?.Hide(this);

        public void OnSelect(BaseEventData eventData)
        {
            if (!NavigationFocusRequestedThisFrame()) return;
            RectTransform rect = transform as RectTransform;
            Vector2 position = rect == null ? Vector2.zero : RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Show(position);
        }

        public void OnDeselect(BaseEventData eventData) => tooltip?.Hide(this);
        private void OnDisable() => tooltip?.Hide(this);

        private void Show(Vector2 position)
        {
            if (tooltip == null || contentProvider == null) return;
            tooltip.Show(this, contentProvider(), position);
        }

        private static bool NavigationFocusRequestedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.downArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)) return true;
            Gamepad gamepad = Gamepad.current;
            return gamepad != null && (gamepad.dpad.up.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame);
        }
    }
}
