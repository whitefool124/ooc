using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public readonly struct FormalTooltipContent
    {
        public string Category { get; }
        public string Status { get; }
        public string Title { get; }
        public string Identity { get; }
        public string MetricA { get; }
        public string MetricB { get; }
        public string MetricC { get; }
        public string Effect { get; }
        public string Summary { get; }
        public string Body { get; }
        public Color Accent { get; }
        public string IconPath { get; }

        public FormalTooltipContent(string title, string body, Color accent)
            : this(string.Empty, string.Empty, title, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, body, accent, string.Empty) { }

        public FormalTooltipContent(string category, string title, string body, Color accent)
            : this(category, title, body, accent, string.Empty) { }

        // Existing providers can keep emitting player-facing prose; categorized cards normalize it into
        // UniversalContentCard fields instead of drawing one free-form paragraph.
        public FormalTooltipContent(string category, string title, string body, Color accent, string iconPath)
            : this(category, ParseStatus(body), title, ParseIdentity(category, body), ParseMetric(body, 0), ParseMetric(body, 1),
                ParseMetric(body, 2), ParseEffect(body), ParseSummary(category, title, body), body, accent, iconPath) { }

        public FormalTooltipContent(string category, string status, string title, string identity,
            string metricA, string metricB, string metricC, string effect, string summary, Color accent, string iconPath = "")
            : this(category, status, title, identity, metricA, metricB, metricC, effect, summary, effect, accent, iconPath) { }

        private FormalTooltipContent(string category, string status, string title, string identity,
            string metricA, string metricB, string metricC, string effect, string summary, string body, Color accent, string iconPath)
        {
            Category = category ?? string.Empty;
            Status = status ?? string.Empty;
            Title = title ?? string.Empty;
            Identity = identity ?? string.Empty;
            MetricA = metricA ?? string.Empty;
            MetricB = metricB ?? string.Empty;
            MetricC = metricC ?? string.Empty;
            Effect = effect ?? string.Empty;
            Summary = summary ?? string.Empty;
            Body = body ?? string.Empty;
            Accent = accent;
            IconPath = iconPath ?? string.Empty;
        }

        private static string[] Lines(string body) => (body ?? string.Empty).Replace("\r", string.Empty)
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).ToArray();

        private static string ParseStatus(string body)
        {
            string line = Lines(body).FirstOrDefault(value => StartsWithLabel(value, "当前") || StartsWithLabel(value, "领取"));
            return string.IsNullOrEmpty(line) ? string.Empty : ValueAfterLabel(line);
        }

        private static string ParseIdentity(string category, string body)
        {
            string target = Lines(body).FirstOrDefault(value => StartsWithLabel(value, "目标") || StartsWithLabel(value, "来源"));
            return string.IsNullOrEmpty(target) ? (category ?? string.Empty) : ValueAfterLabel(target);
        }

        private static string ParseMetric(string body, int index)
        {
            List<string> metrics = new List<string>();
            foreach (string line in Lines(body))
            {
                if (StartsWithLabel(line, "消耗"))
                {
                    foreach (string part in ValueAfterLabel(line).Split('·'))
                    {
                        string value = part.Trim();
                        bool recognized = false;
                        if (value.Contains("行动点")) { metrics.Add("行动 " + FirstNumber(value)); recognized = true; }
                        if (value.Contains("魔力")) { metrics.Add("魔力 " + NumberBeforeLabel(value, "魔力")); recognized = true; }
                        if (!recognized && !string.IsNullOrEmpty(value)) metrics.Add(value);
                    }
                }
                else if (StartsWithLabel(line, "循环")) metrics.Add("冷却 " + ValueAfterLabel(line));
                else if (StartsWithLabel(line, "数据"))
                {
                    foreach (string part in ValueAfterLabel(line).Split('·'))
                        if (!string.IsNullOrWhiteSpace(part)) metrics.Add(part.Trim());
                }
                else if (StartsWithLabel(line, "占格") || StartsWithLabel(line, "重量") || StartsWithLabel(line, "以太负荷"))
                    metrics.Add(line.Replace('　', ' ').Replace("：", " "));
            }
            return index < metrics.Count ? metrics[index] : "—";
        }

        private static string ParseEffect(string body)
        {
            string[] lines = Lines(body);
            int effect = Array.FindIndex(lines, value => value == "效果" || value == "附加效果");
            if (effect >= 0)
            {
                List<string> result = new List<string>();
                for (int i = effect + 1; i < lines.Length && !IsSection(lines[i]); i++)
                    result.Add(lines[i].TrimStart('·', ' ', '　'));
                if (result.Count > 0) return string.Join("；", result.Take(2));
            }
            string fallback = lines.FirstOrDefault(value => !IsMetricOrMeta(value));
            return string.IsNullOrEmpty(fallback) ? "—" : fallback.TrimStart('·', ' ', '　');
        }

        private static string ParseSummary(string category, string title, string body)
        {
            string[] lines = Lines(body);
            string introduction = lines.FirstOrDefault(value => StartsWithLabel(value, "简介"));
            if (!string.IsNullOrEmpty(introduction)) return FirstSentence(ValueAfterLabel(introduction));

            int effectIndex = Array.FindIndex(lines, value => value == "效果" || value == "附加效果");
            IEnumerable<string> preEffect = effectIndex < 0 ? lines : lines.Take(effectIndex);
            string plainText = preEffect.FirstOrDefault(value => !IsMetricOrMeta(value) && !IsSection(value));
            if (!string.IsNullOrEmpty(plainText)) return FirstSentence(plainText.TrimStart('·', ' ', '　'));

            string type = (category ?? string.Empty).Split('·')[0].Trim();
            string effect = ParseEffect(body).Trim().TrimEnd('。', '！', '？');
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            if (string.IsNullOrWhiteSpace(effect) || effect == "—")
                return title + (string.IsNullOrWhiteSpace(type) ? "是一项可用内容。" : "是一项" + type + "。");
            return title + (string.IsNullOrWhiteSpace(type) ? "的主要效果是" : "是一项" + type + "，主要效果是") +
                effect.Replace("；", "，并") + "。";
        }

        private static string FirstSentence(string value)
        {
            string text = (value ?? string.Empty).Trim();
            int end = text.IndexOfAny(new[] { '。', '！', '？' });
            return end < 0 ? (string.IsNullOrEmpty(text) ? string.Empty : text + "。") : text.Substring(0, end + 1);
        }

        private static bool StartsWithLabel(string line, string label) => line == label ||
            line.StartsWith(label + "　", StringComparison.Ordinal) || line.StartsWith(label + " ", StringComparison.Ordinal) ||
            line.StartsWith(label + "：", StringComparison.Ordinal);

        private static string ValueAfterLabel(string line)
        {
            int split = line.IndexOfAny(new[] { '　', ' ', '：' });
            return split < 0 ? string.Empty : line.Substring(split + 1).Trim();
        }

        private static bool IsSection(string line) => new[] { "效果", "附加效果", "注意", "比较", "领取", "来源", "目标", "当前" }
            .Any(label => StartsWithLabel(line, label));

        private static bool IsMetricOrMeta(string line) => new[] { "消耗", "循环", "数据", "目标", "当前", "领取", "来源", "注意", "比较" }
            .Any(label => StartsWithLabel(line, label)) || line == "效果" || line == "附加效果";

        private static string FirstNumber(string value)
        {
            string result = new string((value ?? string.Empty).SkipWhile(character => !char.IsDigit(character) && character != '-')
                .TakeWhile(character => char.IsDigit(character) || character == '-' || character == '.').ToArray());
            return string.IsNullOrEmpty(result) ? "—" : result;
        }

        private static string NumberBeforeLabel(string value, string label)
        {
            int labelIndex = (value ?? string.Empty).IndexOf(label, StringComparison.Ordinal);
            if (labelIndex < 0) return "—";
            string prefix = value.Substring(0, labelIndex).TrimEnd();
            int start = prefix.Length - 1;
            while (start >= 0 && !char.IsDigit(prefix[start]) && prefix[start] != '-' && prefix[start] != '.') start--;
            int end = start;
            while (start >= 0 && (char.IsDigit(prefix[start]) || prefix[start] == '-' || prefix[start] == '.')) start--;
            string result = end < 0 ? string.Empty : prefix.Substring(start + 1, end - start);
            return string.IsNullOrEmpty(result) ? "—" : result;
        }
    }

    public sealed class FormalHoverTooltip : MonoBehaviour
    {
        private const float MinimumWidth = 220f;
        private const float MaximumWidth = 480f;
        private const float MinimumHeight = 120f;
        private const float MaximumHeight = 360f;
        private const float ContentCardWidth = 432f;
        private const float ContentCardMinimumHeight = 388f;
        private const float ContentCardMaximumHeight = 520f;
        private const float ContentCardInset = 16f;
        private const float ContentEffectTop = 220f;
        private const float HorizontalPadding = 16f;
        private const float TopPadding = 16f;
        private const float TitleHeight = 40f;
        private const float BodyTop = 64f;
        private const float BottomPadding = 16f;
        private const float EdgeMargin = 24f;
        private Canvas canvas;
        private RectTransform layer;
        private RectTransform panel;
        private Text titleLabel;
        private Text bodyLabel;
        private RectTransform titleRule;
        private RectTransform cardRoot;
        private Text categoryLabel;
        private Text statusLabel;
        private Image artworkFrame;
        private Image contentIcon;
        private Text cardTitle;
        private Text identityLabel;
        private readonly Text[] metricLabels = new Text[3];
        private Text effectLabel;
        private Text effectBody;
        private Text summaryLabel;
        private object owner;

        public bool IsVisible => panel != null && panel.gameObject.activeSelf;

        public void Initialize(Canvas hostCanvas)
        {
            if (panel != null) return;
            canvas = hostCanvas != null ? hostCanvas : throw new ArgumentNullException(nameof(hostCanvas));

            GameObject layerObject = FormalUiKit.Create("悬浮信息层", canvas.transform);
            layer = layerObject.AddComponent<RectTransform>();
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            CanvasGroup group = layerObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject panelObject = FormalUiKit.AnchoredPanel("悬浮详情", layer, new Vector2(.5f, .5f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(MinimumWidth, MinimumHeight), FormalUiTheme.SurfaceRaised);
            panel = panelObject.GetComponent<RectTransform>();
            Image background = panelObject.GetComponent<Image>();
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = FormalUiTheme.SurfaceRaised;
            background.raycastTarget = false;

            titleLabel = FixedLabel("悬浮标题", string.Empty, panel, new Vector2(HorizontalPadding, -TopPadding),
                new Vector2(MaximumWidth - HorizontalPadding * 2f, TitleHeight), 20, FormalUiTheme.Amber, TextAnchor.MiddleLeft);
            titleRule = FormalUiKit.FlatPanel("悬浮标题分隔", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(HorizontalPadding, -58f), new Vector2(MinimumWidth - HorizontalPadding * 2f, 2f), FormalUiTheme.Rule).GetComponent<RectTransform>();
            bodyLabel = FixedLabel("悬浮正文", string.Empty, panel, new Vector2(HorizontalPadding, -BodyTop),
                new Vector2(MaximumWidth - HorizontalPadding * 2f, MaximumHeight - BodyTop - BottomPadding), 16,
                FormalUiTheme.Text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(bodyLabel);

            GameObject cardObject = FormalUiKit.Create("通用内容卡", panel);
            cardRoot = cardObject.AddComponent<RectTransform>();
            cardRoot.anchorMin = cardRoot.anchorMax = cardRoot.pivot = new Vector2(0f, 1f);
            cardRoot.anchoredPosition = Vector2.zero;
            cardRoot.sizeDelta = new Vector2(ContentCardWidth, ContentCardMinimumHeight);
            categoryLabel = FixedLabel("内容类别", string.Empty, cardRoot, new Vector2(16f, -10f), new Vector2(250f, 40f), 14, FormalUiTheme.Cyan, TextAnchor.MiddleLeft);
            statusLabel = FixedLabel("内容状态", string.Empty, cardRoot, new Vector2(276f, -10f), new Vector2(140f, 40f), 14, FormalUiTheme.Muted, TextAnchor.MiddleRight);

            GameObject artworkObject = FormalUiKit.FlatPanel("图标画框", cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -60f), new Vector2(72f, 72f), FormalUiTheme.Surface);
            artworkFrame = artworkObject.GetComponent<Image>();
            FormalUiKit.ThinFrame(artworkObject.transform, new Vector2(72f, 72f), FormalUiTheme.Cyan, "图标画框描边");
            contentIcon = FormalUiKit.TopLeftIconSlot("内容图标", artworkObject.transform, null, new Vector2(4f, -4f));
            contentIcon.rectTransform.sizeDelta = new Vector2(64f, 64f);

            cardTitle = FixedLabel("内容标题", string.Empty, cardRoot, new Vector2(104f, -58f), new Vector2(312f, 40f), 26, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            identityLabel = FixedLabel("内容身份", string.Empty, cardRoot, new Vector2(104f, -98f), new Vector2(312f, 40f), 14, FormalUiTheme.Muted, TextAnchor.MiddleLeft);
            for (int i = 0; i < metricLabels.Length; i++)
            {
                GameObject metric = FormalUiKit.FlatPanel("内容指标格" + (i + 1), cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(16f + i * 134f, -148f), new Vector2(124f, 48f), FormalUiTheme.SurfaceRaised);
                FormalUiKit.ThinFrame(metric.transform, new Vector2(124f, 48f), FormalUiTheme.Ink, "内容指标描边");
                metricLabels[i] = FixedLabel("内容指标" + (i + 1), string.Empty, metric.transform, new Vector2(8f, -4f),
                    new Vector2(108f, 40f), 15, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            }
            FormalUiKit.FlatPanel("内容分隔线", cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -208f), new Vector2(400f, 2f), FormalUiTheme.Ink);
            effectLabel = FixedLabel("效果标签", "效果", cardRoot, new Vector2(16f, -ContentEffectTop), new Vector2(64f, 40f), 14, FormalUiTheme.Amber, TextAnchor.UpperLeft);
            effectBody = FixedLabel("效果内容", string.Empty, cardRoot, new Vector2(88f, -ContentEffectTop), new Vector2(328f, 72f), 16, FormalUiTheme.Text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(effectBody);
            effectBody.lineSpacing = 1f;
            FormalUiKit.FlatPanel("页脚分隔线", cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -304f), new Vector2(400f, 2f), FormalUiTheme.Ink);
            summaryLabel = FixedLabel("内容简介", string.Empty, cardRoot, new Vector2(16f, -316f), new Vector2(400f, 72f), 14, FormalUiTheme.Muted, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(summaryLabel);
            cardRoot.gameObject.SetActive(false);
            panelObject.SetActive(false);
        }

        public void Show(object source, FormalTooltipContent content, Vector2 screenPosition)
        {
            if (panel == null || source == null || (string.IsNullOrWhiteSpace(content.Title) && string.IsNullOrWhiteSpace(content.Body))) return;
            owner = source;
            bool categorized = !string.IsNullOrWhiteSpace(content.Category);
            cardRoot.gameObject.SetActive(categorized);
            titleLabel.gameObject.SetActive(!categorized);
            titleRule.gameObject.SetActive(!categorized);
            bodyLabel.gameObject.SetActive(!categorized);

            if (categorized)
            {
                categoryLabel.text = content.Category;
                categoryLabel.color = content.Accent;
                statusLabel.text = content.Status;
                artworkFrame.color = FormalUiTheme.Surface;
                FormalUiKit.ThinFrame(artworkFrame.transform, new Vector2(56f, 56f), content.Accent, "图标画框描边");
                contentIcon.sprite = Resources.Load<Sprite>(string.IsNullOrWhiteSpace(content.IconPath) ? FallbackIconPath(content.Category) : content.IconPath);
                contentIcon.color = contentIcon.sprite == null ? Color.clear : Color.white;
                cardTitle.text = content.Title;
                identityLabel.text = content.Identity;
                metricLabels[0].text = content.MetricA;
                metricLabels[1].text = content.MetricB;
                metricLabels[2].text = content.MetricC;
                effectLabel.color = FormalUiTheme.Amber;
                effectBody.text = content.Effect;
                summaryLabel.text = content.Summary;
                LayoutCategorizedCard();
            }
            else
            {
                titleLabel.text = content.Title;
                titleLabel.color = FormalUiTheme.ReadableLabelColor(content.Accent);
                bodyLabel.text = content.Body;
                bodyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                titleLabel.rectTransform.sizeDelta = new Vector2(MaximumWidth - HorizontalPadding * 2f, TitleHeight);
                bodyLabel.rectTransform.sizeDelta = new Vector2(MaximumWidth - HorizontalPadding * 2f, MaximumHeight - BodyTop - BottomPadding);
                Canvas.ForceUpdateCanvases();
                float width = Mathf.Clamp(Mathf.Ceil(Mathf.Max(titleLabel.preferredWidth, bodyLabel.preferredWidth)) + HorizontalPadding * 2f,
                    MinimumWidth, MaximumWidth);
                float textWidth = width - HorizontalPadding * 2f;
                titleLabel.rectTransform.sizeDelta = new Vector2(textWidth, TitleHeight);
                titleRule.sizeDelta = new Vector2(textWidth, 2f);
                bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyLabel.rectTransform.sizeDelta = new Vector2(textWidth, MaximumHeight - BodyTop - BottomPadding);
                Canvas.ForceUpdateCanvases();
                float height = Mathf.Clamp(BodyTop + bodyLabel.preferredHeight + BottomPadding, MinimumHeight, MaximumHeight);
                panel.sizeDelta = new Vector2(width, height);
                bodyLabel.rectTransform.sizeDelta = new Vector2(textWidth, height - BodyTop - BottomPadding);
                FormalUiKit.KeepInsideParentFrame(titleLabel);
                FormalUiKit.KeepInsideParentFrame(bodyLabel);
            }

            FormalUiKit.ThinFrame(panel, panel.sizeDelta, FormalUiTheme.Ink, "内容卡描边");
            layer.SetAsLastSibling();
            panel.SetAsLastSibling();
            panel.gameObject.SetActive(true);
            Move(source, screenPosition);
        }

        private static Text FixedLabel(string name, string value, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            Text label = FormalUiKit.Label(name, value, parent, position, size, fontSize, color, alignment);
            // FormalUiKit aligns FusionPixel to its native 12 px grid. Reapplying the requested
            // legacy size here produced thin, blurred 14/16/20/26 px rasterization in tooltips.
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private void LayoutCategorizedCard()
        {
            Canvas.ForceUpdateCanvases();
            float effectHeight = Mathf.Clamp(Mathf.Ceil(effectBody.preferredHeight), 72f, 144f);
            effectBody.rectTransform.sizeDelta = new Vector2(328f, effectHeight);
            effectLabel.rectTransform.sizeDelta = new Vector2(64f, Mathf.Max(40f, effectHeight));

            float footerTop = ContentEffectTop + effectHeight + 12f;
            RectTransform footerRule = cardRoot.Find("页脚分隔线") as RectTransform;
            if (footerRule != null) footerRule.anchoredPosition = new Vector2(ContentCardInset, -footerTop);

            float summaryTop = footerTop + 12f;
            summaryLabel.rectTransform.anchoredPosition = new Vector2(ContentCardInset, -summaryTop);
            summaryLabel.rectTransform.sizeDelta = new Vector2(ContentCardWidth - ContentCardInset * 2f, 112f);
            Canvas.ForceUpdateCanvases();
            float summaryHeight = Mathf.Clamp(Mathf.Ceil(summaryLabel.preferredHeight), 40f, 112f);
            summaryLabel.rectTransform.sizeDelta = new Vector2(ContentCardWidth - ContentCardInset * 2f, summaryHeight);

            float height = Mathf.Clamp(summaryTop + summaryHeight + ContentCardInset,
                ContentCardMinimumHeight, ContentCardMaximumHeight);
            cardRoot.sizeDelta = panel.sizeDelta = new Vector2(ContentCardWidth, height);
        }

        private static string FallbackIconPath(string category)
        {
            if (category.Contains("术式")) return FormalArtRegistry.CommandPath("skill");
            if (category.Contains("装备")) return FormalArtRegistry.ItemPath("category_armor");
            if (category.Contains("物品") || category.Contains("法宝") || category.Contains("道具")) return FormalArtRegistry.ItemPath("category_container");
            if (category.Contains("状态") || category.Contains("意图") || category.Contains("地形")) return FormalArtRegistry.SemanticPath("notice");
            return FormalArtRegistry.CommandPath("skill");
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
