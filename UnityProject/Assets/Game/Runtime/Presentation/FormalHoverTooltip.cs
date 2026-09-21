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
        public const int MaximumSummaryCharacters = 20;
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
        /// <summary>Boxed 词条 shown between the title and the resource cells, e.g. 单点／4格／伤害.</summary>
        public IReadOnlyList<string> Tags { get; }
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
            string metricA, string metricB, string metricC, string effect, string summary, Color accent, string iconPath = "",
            IReadOnlyList<string> tags = null)
            : this(category, status, title, identity, metricA, metricB, metricC, effect, summary, effect, accent, iconPath, tags) { }

        private FormalTooltipContent(string category, string status, string title, string identity,
            string metricA, string metricB, string metricC, string effect, string summary, string body, Color accent, string iconPath,
            IReadOnlyList<string> tags = null)
        {
            Category = category ?? string.Empty;
            Status = status ?? string.Empty;
            Title = title ?? string.Empty;
            Identity = identity ?? string.Empty;
            MetricA = metricA ?? string.Empty;
            MetricB = metricB ?? string.Empty;
            MetricC = metricC ?? string.Empty;
            Effect = effect ?? string.Empty;
            Summary = NormalizeSummary(category, summary);
            Body = body ?? string.Empty;
            Accent = accent;
            IconPath = iconPath ?? string.Empty;
            Tags = tags ?? System.Array.Empty<string>();
        }

        /// <summary>Keeps the parsed status/metrics/effect/summary, but replaces the prose 目标 line with 词条.</summary>
        public FormalTooltipContent(string category, string title, string body, Color accent, string iconPath, IReadOnlyList<string> tags)
            : this(category, ParseStatus(body), title, ParseIdentity(category, body), ParseMetric(body, 0), ParseMetric(body, 1),
                ParseMetric(body, 2), ParseEffect(body), ParseSummary(category, title, body), body, accent, iconPath, tags) { }

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
                else if (StartsWithLabel(line, "次数")) metrics.Add(ValueAfterLabel(line));
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
                if (result.Count > 0) return string.Join("；", result);
            }
            string fallback = lines.FirstOrDefault(value => !IsMetricOrMeta(value));
            return string.IsNullOrEmpty(fallback) ? "—" : fallback.TrimStart('·', ' ', '　');
        }

        private static string ParseSummary(string category, string title, string body)
        {
            string[] lines = Lines(body);
            string introduction = lines.FirstOrDefault(value => StartsWithLabel(value, "简介"));
            if (!string.IsNullOrEmpty(introduction)) return NormalizeSummary(category, ValueAfterLabel(introduction));
            return NormalizeSummary(category, string.Empty);
        }

        private static string NormalizeSummary(string category, string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? DefaultSettingSummary(category) : FirstSentence(value);
            if (string.IsNullOrWhiteSpace(text) || text.Length <= MaximumSummaryCharacters) return text ?? string.Empty;
            return text.Substring(0, MaximumSummaryCharacters - 1).TrimEnd('，', '；', '：', '。', '！', '？', ' ') + "。";
        }

        private static string DefaultSettingSummary(string category)
        {
            string type = category ?? string.Empty;
            if (type.Contains("装备")) return "学院登记的标准化装备。";
            if (type.Contains("法宝") || type.Contains("道具") || type.Contains("物品")) return "学院登记的便携式器材。";
            if (type.Contains("术式")) return "学院备案的可维护术式。";
            return string.IsNullOrWhiteSpace(type) ? string.Empty : "学院档案中的常用内容。";
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

        private static bool IsSection(string line) => new[] { "效果", "附加效果", "条件", "注意", "简介", "比较", "领取", "来源", "目标", "当前" }
            .Any(label => StartsWithLabel(line, label));

        private static bool IsMetricOrMeta(string line) => new[] { "消耗", "循环", "次数", "数据", "目标", "条件", "当前", "领取", "来源", "注意", "比较" }
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
        private const float MaximumHeight = 720f;
        private const float ContentCardWidth = 432f;
        private const float ContentCardMinimumHeight = 388f;
        private const float ContentCardInset = 16f;
        private const float ContentEffectTop = 220f;
        private const float MetricRowTop = 148f;
        private const float MetricCellWidth = 124f;
        private const float MetricCellHeight = 48f;
        private const float MetricLabelWidth = 108f;
        private const float ContentEffectBodyWidth = ContentCardWidth - (ContentCardInset * 2f);
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
        private readonly RectTransform[] metricCells = new RectTransform[3];
        private readonly Text[] tagLabels = new Text[MaximumTags];
        private readonly RectTransform[] tagChips = new RectTransform[MaximumTags];
        private RectTransform contentDivider;
        private float effectTop = ContentEffectTop;
        private float metricRowHeight = MetricCellHeight;
        private float metricRowTop = MetricRowTop;
        private const int MaximumTags = 6;
        private const float TagRowTop = 96f;
        // 词条行与「内容标题／内容身份」同一起始 x，避开 16..88 的图标画框。
        private const float TagRowInset = 104f;
        private const float TagHeight = 34f;
        private const float TagGap = 8f;
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
            bodyLabel.verticalOverflow = VerticalWrapMode.Overflow;

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
                metricCells[i] = metric.GetComponent<RectTransform>();
                metricLabels[i] = FixedLabel("内容指标" + (i + 1), string.Empty, metric.transform, new Vector2(8f, -4f),
                    new Vector2(108f, 40f), 15, FormalUiTheme.Text, TextAnchor.MiddleLeft);
            }
            contentDivider = FormalUiKit.FlatPanel("内容分隔线", cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -208f), new Vector2(400f, 2f), FormalUiTheme.Ink).GetComponent<RectTransform>();
            for (int i = 0; i < tagLabels.Length; i++)
            {
                GameObject tag = FormalUiKit.FlatPanel("内容词条" + (i + 1), cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(ContentCardInset, -TagRowTop), new Vector2(60f, TagHeight), FormalUiTheme.SurfaceRaised);
                tagChips[i] = tag.GetComponent<RectTransform>();
                FormalUiKit.ThinFrame(tag.transform, new Vector2(60f, TagHeight), FormalUiTheme.Ink, "词条描边");
                tagLabels[i] = FixedLabel("词条" + (i + 1), string.Empty, tag.transform, new Vector2(8f, -4f),
                    new Vector2(44f, TagHeight - 8f), 14, FormalUiTheme.Text, TextAnchor.MiddleCenter);
                tagLabels[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                // 像素字体按 12 网格取整后单行可能高于内框，Truncate 会把整行裁掉，因此这里允许溢出。
                tagLabels[i].verticalOverflow = VerticalWrapMode.Overflow;
                tagLabels[i].transform.SetAsLastSibling();
                tag.SetActive(false);
            }
            effectBody = FixedLabel("效果内容", string.Empty, cardRoot, new Vector2(ContentCardInset, -effectTop), new Vector2(ContentEffectBodyWidth, 40f), 16, FormalUiTheme.Text, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(effectBody);
            effectBody.lineSpacing = 1f;
            effectBody.verticalOverflow = VerticalWrapMode.Overflow;
            FormalUiKit.FlatPanel("页脚分隔线", cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -304f), new Vector2(400f, 2f), FormalUiTheme.Ink);
            summaryLabel = FixedLabel("内容简介", string.Empty, cardRoot, new Vector2(16f, -316f), new Vector2(400f, 72f), 14, FormalUiTheme.Muted, TextAnchor.UpperLeft);
            FormalUiKit.ConfigureParagraph(summaryLabel);
            summaryLabel.verticalOverflow = VerticalWrapMode.Overflow;
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
                effectBody.text = WrapEffectAtClauses(content.Effect);
                summaryLabel.text = content.Summary;
                ApplyTags(content.Tags);
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
                float height = Mathf.Max(BodyTop + bodyLabel.preferredHeight + BottomPadding, MinimumHeight);
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
            // 词条行占用原「目标」行；没有词条时退回该行文本。指标格与以下各段都跟着游标走。
            LayoutTagRow();
            MeasureMetricRow();
            float dividerY = metricRowTop + metricRowHeight + 12f;
            if (contentDivider != null) contentDivider.anchoredPosition = new Vector2(ContentCardInset, -dividerY);
            effectTop = dividerY + 12f;
            effectBody.rectTransform.anchoredPosition = new Vector2(ContentCardInset, -effectTop);
            effectBody.rectTransform.sizeDelta = new Vector2(ContentEffectBodyWidth, MaximumHeight);
            Canvas.ForceUpdateCanvases();
            float effectHeight = Mathf.Max(Mathf.Ceil(effectBody.preferredHeight), 40f);
            effectBody.rectTransform.sizeDelta = new Vector2(ContentEffectBodyWidth, effectHeight);

            float footerTop = effectTop + effectHeight + 12f;
            RectTransform footerRule = cardRoot.Find("页脚分隔线") as RectTransform;
            if (footerRule != null) footerRule.anchoredPosition = new Vector2(ContentCardInset, -footerTop);

            float summaryTop = footerTop + 12f;
            summaryLabel.rectTransform.anchoredPosition = new Vector2(ContentCardInset, -summaryTop);
            summaryLabel.rectTransform.sizeDelta = new Vector2(ContentCardWidth - ContentCardInset * 2f, MaximumHeight);
            Canvas.ForceUpdateCanvases();
            float summaryHeight = Mathf.Max(Mathf.Ceil(summaryLabel.preferredHeight), 40f);
            summaryLabel.rectTransform.sizeDelta = new Vector2(ContentCardWidth - ContentCardInset * 2f, summaryHeight);

            float height = Mathf.Max(summaryTop + summaryHeight + ContentCardInset, ContentCardMinimumHeight);
            cardRoot.sizeDelta = panel.sizeDelta = new Vector2(ContentCardWidth, height);
        }

        private void ApplyTags(IReadOnlyList<string> tags)
        {
            int count = tags == null ? 0 : tags.Count;
            for (int i = 0; i < tagLabels.Length; i++)
            {
                bool used = i < count;
                tagLabels[i].gameObject.SetActive(used);
                if (tagChips[i] != null) tagChips[i].gameObject.SetActive(used);
                if (used) tagLabels[i].text = tags[i];
            }
            // 有词条时不再重复显示散文式的「目标」行。
            identityLabel.gameObject.SetActive(count == 0);
        }

        /// <summary>
        /// Lays the 词条 out as boxed chips, each sized to its own text and wrapping to a second line when the
        /// card width runs out. The row feeds <see cref="metricRowTop"/> so everything below follows it.
        /// </summary>
        private void LayoutTagRow()
        {
            float cursorX = TagRowInset;
            float cursorY = TagRowTop;
            float rightLimit = ContentCardWidth - ContentCardInset;
            int used = 0;
            for (int i = 0; i < tagLabels.Length; i++)
            {
                if (tagChips[i] == null || !tagChips[i].gameObject.activeSelf) continue;
                Text label = tagLabels[i];
                label.rectTransform.sizeDelta = new Vector2(200f, TagHeight - 8f);
                Canvas.ForceUpdateCanvases();
                float width = Mathf.Ceil(label.preferredWidth) + 16f;
                if (used > 0 && cursorX + width > rightLimit)
                {
                    cursorX = TagRowInset;
                    cursorY += TagHeight + 6f;
                }
                tagChips[i].anchoredPosition = new Vector2(cursorX, -cursorY);
                tagChips[i].sizeDelta = new Vector2(width, TagHeight);
                FormalUiKit.ThinFrame(tagChips[i], new Vector2(width, TagHeight), FormalUiTheme.Ink, "词条描边");
                label.rectTransform.sizeDelta = new Vector2(width - 16f, TagHeight - 8f);
                label.rectTransform.anchoredPosition = new Vector2(8f, -4f);
                // 描边是后加的兄弟物体，必须把文字重新提到最上层，否则会被框盖住。
                label.transform.SetAsLastSibling();
                cursorX += width + TagGap;
                used++;
            }
            metricRowTop = used == 0 ? MetricRowTop : cursorY + TagHeight + 12f;
        }

        /// <summary>
        /// Grows the metric row to whatever its tallest cell needs. Cells keep their 124 px width and the
        /// text wraps inside them, so a long term raises the row instead of overflowing it.
        /// </summary>
        private void MeasureMetricRow()
        {
            float height = MetricCellHeight;
            for (int i = 0; i < metricLabels.Length; i++)
            {
                Text label = metricLabels[i];
                if (label == null) continue;
                label.rectTransform.sizeDelta = new Vector2(MetricLabelWidth, MetricCellHeight);
                Canvas.ForceUpdateCanvases();
                float needed = Mathf.Ceil(label.preferredHeight) + 8f;
                if (needed > height) height = needed;
            }
            metricRowHeight = height;
            for (int i = 0; i < metricLabels.Length; i++)
            {
                Text label = metricLabels[i];
                if (label == null || metricCells[i] == null) continue;
                metricCells[i].anchoredPosition = new Vector2(16f + i * 134f, -metricRowTop);
                metricCells[i].sizeDelta = new Vector2(MetricCellWidth, height);
                label.rectTransform.sizeDelta = new Vector2(MetricLabelWidth, height - 8f);
                FormalUiKit.ThinFrame(metricCells[i], new Vector2(MetricCellWidth, height), FormalUiTheme.Ink, "内容指标描边");
            }
        }

        private static string WrapEffectAtClauses(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            List<string> clauses = new List<string>();
            System.Text.StringBuilder clause = new System.Text.StringBuilder();
            foreach (char character in value)
            {
                clause.Append(character);
                if (character == '；' || character == '。' || character == '，')
                {
                    clauses.Add(clause.ToString());
                    clause.Length = 0;
                }
            }
            if (clause.Length > 0) clauses.Add(clause.ToString());

            TextGenerationSettings settings = new TextGenerationSettings
            {
                font = FormalUiKit.Font,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                color = Color.white,
                textAnchor = TextAnchor.UpperLeft,
                generationExtents = new Vector2(ContentEffectBodyWidth, MaximumHeight),
                pivot = Vector2.zero,
                horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow,
                scaleFactor = 1f,
                lineSpacing = 1f,
                richText = true
            };
            TextGenerator generator = new TextGenerator();
            List<string> lines = new List<string>();
            string currentLine = string.Empty;
            foreach (string nextClause in clauses)
            {
                string candidate = currentLine + nextClause;
                if (currentLine.Length > 0 && generator.GetPreferredWidth(candidate, settings) > ContentEffectBodyWidth)
                {
                    lines.Add(currentLine);
                    currentLine = nextClause;
                }
                else currentLine = candidate;
            }
            if (currentLine.Length > 0) lines.Add(currentLine);
            return string.Join("\n", lines);
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
