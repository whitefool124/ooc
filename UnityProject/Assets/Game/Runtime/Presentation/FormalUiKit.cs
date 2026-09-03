using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public enum FormalUiButtonTone
    {
        Neutral,
        Primary,
        Positive,
        Warning,
        Dangerous
    }

    public static class FormalUiTheme
    {
        public const string ThemeId = "academy-archive-ledger";
        public const bool UsesAmbientScanlines = false;
        private static bool highContrast;
        private static bool largeText;
        public static bool HighContrastEnabled => highContrast;
        public static bool LargeTextEnabled => largeText;
        public static Color Ink => highContrast ? new Color(.035f, .031f, .025f, 1f) : OccPixelUiConfig.Palette("ink");
        public static Color Panel => highContrast ? new Color(1f, .992f, .96f, 1f) : OccPixelUiConfig.Palette("panel");
        public static Color Cyan => Accent(OccPixelUiConfig.Palette("cyan"));
        public static Color Amber => Accent(OccPixelUiConfig.Palette("amber"));
        public static Color Safe => Accent(OccPixelUiConfig.Palette("safe"));
        public static Color Danger => Accent(OccPixelUiConfig.Palette("danger"));
        public static Color Text => highContrast ? new Color(.015f, .013f, .01f, 1f) : OccPixelUiConfig.Palette("text");
        public static Color Muted => highContrast ? new Color(.24f, .22f, .19f, 1f) : OccPixelUiConfig.Palette("muted");
        public static Color Disabled => highContrast ? new Color(.72f, .71f, .68f, 1f) : new Color(.70f, .68f, .64f, 1f);
        public static Color Surface => highContrast ? new Color(1f, .995f, .975f, 1f) : OccPixelUiConfig.Palette("surface");
        public static Color SurfaceRaised => highContrast ? Color.white : OccPixelUiConfig.Palette("raised");
        public static Color Interactive => highContrast ? new Color(.90f, .87f, .80f, 1f) : new Color(.835f, .788f, .702f, 1f);
        public static Color InteractivePressed => highContrast ? new Color(.76f, .71f, .62f, 1f) : new Color(.71f, .65f, .55f, 1f);
        public static Color Overlay => new Color(.12f, .11f, .09f, .58f);
        public static Color Focus => Cyan;
        public static Color Rule => highContrast ? new Color(.12f, .11f, .09f, 1f) : new Color(.37f, .34f, .29f, 1f);
        public static Color OnInk => highContrast ? Color.white : new Color(.96f, .93f, .85f, 1f);
        public static Color InventorySlotSurface => highContrast ? SurfaceRaised : Color.Lerp(SurfaceRaised, Panel, .22f);
        public static Color InventorySlotSelected => Color.Lerp(InventorySlotSurface, Cyan, .12f);
        public static Color InventorySlotLocked => Color.Lerp(Panel, Disabled, .36f);
        public static readonly Color Health = new Color(.68f, .25f, .19f, 1f);
        public static readonly Color Shield = new Color(.27f, .56f, .38f, 1f);
        public static readonly Color Magic = new Color(.18f, .48f, .51f, 1f);
        public static Color ResourceTrack => highContrast
            ? new Color(.72f, .70f, .65f, 1f)
            : Color.Lerp(SurfaceRaised, Ink, .24f);

        public const int NativeFontGrid = 12;
        public const int MinimumReadableFontSize = 24;
        public const int MinimumCompactFontSize = 24;
        public const int CaptionFontSize = 24;
        public const int BodyFontSize = 24;
        public const int HeadingFontSize = 24;
        public const int TitleFontSize = 48;
        public const int FeedbackFontSize = 72;
        public const int ButtonFontSize = 24;
        public const int ButtonDetailFontSize = 24;
        public const int BodyTextSlotHeight = 40;
        public const int TwoLineTextSlotHeight = 72;
        public const int TitleTextSlotHeight = 72;
        public const int MinimumInteractiveHeight = 48;
        public const int SpaceSmall = 8;
        public const int SpaceMedium = 16;
        public const int SpaceLarge = 24;
        public const int IconSlotSize = 32;
        public const int IconTextInset = 36;
        public const int FrameThickness = 6;
        public const int FrameTextSafetyMargin = 6;
        public const int FramedContentInset = FrameThickness + FrameTextSafetyMargin;
        public const int FullyFramedSingleLineHeight = BodyFontSize + FramedContentInset * 2;
        public const int FrameCornerSize = 12;
        public const int InnerHighlightThickness = 2;
        public const int PressedOffset = 4;
        public static readonly Vector2 FocusDistance = new Vector2(FrameThickness, -FrameThickness);

        public static void ConfigureAccessibility(bool useHighContrast, bool useLargeText)
        {
            highContrast = useHighContrast;
            largeText = useLargeText;
        }

        private static Color Accent(Color color) => highContrast ? Color.Lerp(color, Color.white, .14f) : color;

        public static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));

        public static Color TextForSurface(Color surface) => RelativeLuminance(surface) < .42f ? OnInk : Text;

        private static float RelativeLuminance(Color color)
        {
            float r = color.r <= .04045f ? color.r / 12.92f : Mathf.Pow((color.r + .055f) / 1.055f, 2.4f);
            float g = color.g <= .04045f ? color.g / 12.92f : Mathf.Pow((color.g + .055f) / 1.055f, 2.4f);
            float b = color.b <= .04045f ? color.b / 12.92f : Mathf.Pow((color.b + .055f) / 1.055f, 2.4f);
            return .2126f * r + .7152f * g + .0722f * b;
        }

        public static float ContrastRatio(Color foreground, Color background)
        {
            float bright = Mathf.Max(RelativeLuminance(foreground), RelativeLuminance(background));
            float dark = Mathf.Min(RelativeLuminance(foreground), RelativeLuminance(background));
            return (bright + .05f) / (dark + .05f);
        }

        public static Color ReadableLabelColor(Color requested)
        {
            if (Approximately(requested, Cyan)) return highContrast ? Text : new Color(.105f, .34f, .37f, requested.a);
            if (Approximately(requested, Amber)) return highContrast ? Text : new Color(.40f, .235f, .055f, requested.a);
            if (Approximately(requested, Safe)) return highContrast ? Text : new Color(.19f, .335f, .225f, requested.a);
            if (Approximately(requested, Danger)) return highContrast ? Text : new Color(.52f, .20f, .17f, requested.a);
            return requested;
        }

        private static bool Approximately(Color left, Color right) =>
            Mathf.Abs(left.r - right.r) < .002f && Mathf.Abs(left.g - right.g) < .002f &&
            Mathf.Abs(left.b - right.b) < .002f;

        public static FormalUiButtonPalette ButtonPalette(FormalUiButtonTone tone)
        {
            Color accent = tone == FormalUiButtonTone.Primary ? Cyan :
                tone == FormalUiButtonTone.Positive ? Safe :
                tone == FormalUiButtonTone.Warning ? Amber :
                tone == FormalUiButtonTone.Dangerous ? Danger : Muted;
            Color normal = tone == FormalUiButtonTone.Dangerous ? Color.Lerp(Interactive, Danger, .16f) : Interactive;
            return FormalUiButtonPalette.ForAccent(normal, accent);
        }

        public static int PixelAlignedFontSize(int fontSize, bool compact)
        {
            int target = Mathf.Max(compact ? MinimumCompactFontSize : MinimumReadableFontSize, fontSize);
            if (target <= 35) return BodyFontSize;
            if (target <= 60) return TitleFontSize;
            return FeedbackFontSize;
        }

        public static int ResponsiveFontSize(int fontSize)
        {
            int responsive = PixelAlignedFontSize(fontSize, Screen.height <= UiLayoutContract.CompactHeightThreshold);
            if (!largeText) return responsive;
            return responsive == BodyFontSize ? TitleFontSize : FeedbackFontSize;
        }

        public static int MinimumTextSlotHeight(int resolvedFontSize, int lineCount = 1)
        {
            if (lineCount > 1) return resolvedFontSize <= BodyFontSize ? TwoLineTextSlotHeight : resolvedFontSize + 32;
            if (resolvedFontSize <= BodyFontSize) return BodyTextSlotHeight;
            if (resolvedFontSize <= TitleFontSize) return TitleTextSlotHeight;
            return resolvedFontSize + 32;
        }
    }

    public readonly struct FormalUiButtonPalette
    {
        public Color Normal { get; }
        public Color Hover { get; }
        public Color Pressed { get; }
        public Color Selected { get; }
        public Color Disabled { get; }

        public FormalUiButtonPalette(Color normal, Color hover, Color pressed, Color selected, Color disabled)
        {
            Normal = normal;
            Hover = hover;
            Pressed = pressed;
            Selected = selected;
            Disabled = disabled;
        }

        public static FormalUiButtonPalette ForAccent(Color normal, Color accent)
        {
            return new FormalUiButtonPalette(normal, Color.Lerp(normal, accent, .24f), Color.Lerp(normal, Color.black, .28f), Color.Lerp(normal, accent, .48f), FormalUiTheme.Disabled);
        }
    }

    public static class FormalUiMotionTokens
    {
        public const float RewardStaggerMultiplier = .55f;
        public static Ease FeedbackEase => Ease.OutQuad;
        public static Ease StandardEase => Ease.OutCubic;
    }

    public readonly struct FormalUiPageChecklistEntry
    {
        public string Id { get; }
        public string DefaultFocusKey { get; }
        public bool HasBackPath { get; }
        public bool CoversDisabledState { get; }
        public bool CoversEmptyState { get; }

        public FormalUiPageChecklistEntry(string id, string defaultFocusKey, bool hasBackPath, bool coversDisabledState, bool coversEmptyState)
        {
            Id = id;
            DefaultFocusKey = defaultFocusKey;
            HasBackPath = hasBackPath;
            CoversDisabledState = coversDisabledState;
            CoversEmptyState = coversEmptyState;
        }
    }

    public static class FormalUiPageChecklist
    {
        private static readonly FormalUiPageChecklistEntry[] entries =
        {
            new FormalUiPageChecklistEntry("landing", "按钮_近战训练", false, true, true),
            new FormalUiPageChecklistEntry("map", "map.node.{current}", true, true, true),
            new FormalUiPageChecklistEntry("briefing", "按钮_进入战斗", true, true, false),
            new FormalUiPageChecklistEntry("combat", "移动", true, true, false),
            new FormalUiPageChecklistEntry("shop-workshop", "按钮_返回", true, true, true),
            new FormalUiPageChecklistEntry("inventory-loot", "inventory.back", true, true, true),
            new FormalUiPageChecklistEntry("settlement", "reward.first", true, true, false),
            new FormalUiPageChecklistEntry("settings", "按钮_设置_0", true, true, false),
            new FormalUiPageChecklistEntry("archive", "按钮_返回", true, false, true)
        };

        public static IReadOnlyList<FormalUiPageChecklistEntry> Entries => entries;
    }

    public static class FormalUiKit
    {
        private static Font font;
        private static Font displayFont;
        private static Font readingFont;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> skin = new System.Collections.Generic.Dictionary<string, Sprite>(StringComparer.Ordinal);
        public const string FontResourcePath = "Fonts/FusionPixel12ProportionalZhHans";
        public const string DisplayFontResourcePath = "Fonts/FusionPixel12ProportionalZhHans";
        public const string ReadingFontResourcePath = "Fonts/SimHei";
        public static Font Font => font != null ? font : font = Resources.Load<Font>(FontResourcePath);
        public static Font DisplayFont => displayFont != null ? displayFont : displayFont = Resources.Load<Font>(DisplayFontResourcePath);
        public static Font ReadingFont => readingFont != null ? readingFont : readingFont = Resources.Load<Font>(ReadingFontResourcePath);

        public static Sprite SkinSprite(string id)
        {
            if (skin.TryGetValue(id, out Sprite cached)) return cached;
            Sprite loaded = Resources.Load<Sprite>(OccPixelUiConfig.SkinPath(id));
            if (loaded == null) throw new InvalidOperationException("Missing formal pixel UI skin: " + id);
            skin[id] = loaded;
            return loaded;
        }

        public static void ApplySkin(Image image, string id, Color tint)
        {
            if (image == null) return;
            Outline legacyOutline = image.GetComponent<Outline>();
            if (legacyOutline != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(legacyOutline);
                else UnityEngine.Object.DestroyImmediate(legacyOutline);
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = id == "focus" ? Color.clear : tint;
            Transform existingFrame = image.transform.Find("像素框架");
            if (existingFrame != null) existingFrame.gameObject.SetActive(false);

            Image overlay = SkinOverlay(image, true);
            Sprite sprite = SkinSprite(id);
            overlay.sprite = sprite;
            overlay.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            overlay.fillCenter = false;
            overlay.preserveAspect = false;
            overlay.color = Color.white;
            overlay.raycastTarget = false;
        }

        public static Image SkinOverlay(Image image, bool createIfMissing = false)
        {
            if (image == null) return null;
            Transform child = image.transform.Find("正式皮肤");
            if (child != null) return child.GetComponent<Image>();
            if (!createIfMissing) return null;
            GameObject overlayObject = Create("正式皮肤", image.transform);
            RectTransform rect = overlayObject.AddComponent<RectTransform>();
            Stretch(rect);
            return overlayObject.AddComponent<Image>();
        }

        public static bool IsStandardButtonSkin(Sprite sprite)
        {
            if (sprite == null) return false;
            return sprite == SkinSprite("button_idle") || sprite == SkinSprite("button_hover") ||
                sprite == SkinSprite("button_pressed") || sprite == SkinSprite("button_disabled") ||
                sprite == SkinSprite("tab_active");
        }

        public static Sprite ButtonStateSprite(bool available, bool pressed, bool selected, bool hovered)
        {
            string id = !available ? "button_disabled" : pressed ? "button_pressed" : selected ? "tab_active" : hovered ? "button_hover" : "button_idle";
            return SkinSprite(id);
        }

        private static string PanelSkin(string name)
        {
            if (name.Contains("战术读数") || name.Contains("控制台") || name.Contains("战斗信息")) return "panel_console";
            if (name.Contains("行动状态") || name.Contains("行动序列") || name.Contains("本轮行动") || name.Contains("英雄概况")) return "panel_module";
            if (name.Contains("行动预览") || name.Contains("目标")) return "panel_target";
            if (name.Contains("现场记录") || name.Contains("记录")) return "panel_log";
            if (name.Contains("武器组")) return "group_weapon";
            if (name.Contains("术式组")) return "group_spell";
            if (name.Contains("交互组")) return "group_interaction";
            if (name.Contains("物品组")) return "group_item";
            if (name.Contains("结束行动")) return "button_end_turn";
            if (name.Contains("页眉") || name.Contains("抬头")) return "header";
            if (name.Contains("轨道")) return "bar_track";
            if (name.Contains("填充")) return "bar_fill";
            if (name.Contains("槽")) return "slot";
            if (name.Contains("确认卡") || name.Contains("结算卡") || name.Contains("详情")) return "panel_elevated";
            return "panel";
        }

        public static Image FocusFrame(Transform parent)
        {
            GameObject result = Create("像素焦点框", parent); RectTransform rect = result.AddComponent<RectTransform>(); Stretch(rect);
            rect.offsetMin = new Vector2(-FormalUiTheme.FrameThickness, -FormalUiTheme.FrameThickness);
            rect.offsetMax = new Vector2(FormalUiTheme.FrameThickness, FormalUiTheme.FrameThickness);
            Image image = result.AddComponent<Image>(); ApplySkin(image, "focus", Color.white); image.raycastTarget = false; return image;
        }

        public static Canvas CanvasRoot(string name, int sortingOrder)
        {
            GameObject root = new GameObject(name);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = true;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight);
            scaler.matchWidthOrHeight = UiLayoutContract.MatchWidthOrHeight;
            scaler.referencePixelsPerUnit = 32f * OccPixelUiConfig.Data.logicalPixelScale;
            root.AddComponent<GraphicRaycaster>();
            RuntimeUiEventSystem.Ensure();
            return canvas;
        }

        public static GameObject Create(string name, Transform parent)
        {
            GameObject result = new GameObject(name);
            result.transform.SetParent(parent, false);
            return result;
        }

        public static GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = anchorMax; rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = result.AddComponent<Image>(); ApplySkin(image, PanelSkin(name), color);
            return result;
        }

        public static GameObject FlatPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = anchorMax; rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = result.AddComponent<Image>();
            image.sprite = null; image.type = Image.Type.Simple; image.color = color; image.raycastTarget = false;
            return result;
        }

        public static GameObject AnchoredPanel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = result.AddComponent<Image>(); ApplySkin(image, PanelSkin(name), color);
            return result;
        }

        public static GameObject LayoutPanel(string name, Transform parent, string layoutId, Color color)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            ApplyLayout(rect, layoutId);
            Image image = result.AddComponent<Image>(); ApplySkin(image, PanelSkin(name), color);
            return result;
        }

        public static void ApplyLayout(RectTransform rect, string layoutId)
        {
            OccPixelUiLayoutEntry layout = OccPixelUiConfig.Layout(layoutId);
            Vector2 anchor = ResolveAnchor(layout.anchor);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = layout.Position;
            rect.sizeDelta = layout.Size;
        }

        public static Vector2 ResolveAnchor(string anchor)
        {
            switch (anchor)
            {
                case "top-left": return new Vector2(0f, 1f);
                case "top-center": return new Vector2(.5f, 1f);
                case "top-right": return Vector2.one;
                case "bottom-left": return Vector2.zero;
                case "bottom-center": return new Vector2(.5f, 0f);
                case "bottom-right": return new Vector2(1f, 0f);
                case "center": return new Vector2(.5f, .5f);
                default: throw new ArgumentException("Unsupported layout anchor: " + anchor, nameof(anchor));
            }
        }

        public static Text Label(string name, string value, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
            int resolvedFontSize = FormalUiTheme.ResponsiveFontSize(fontSize);
            float resolvedHeight = size.y > 0f ? Mathf.Max(size.y, FormalUiTheme.MinimumTextSlotHeight(resolvedFontSize)) : size.y;
            rect.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(resolvedHeight));
            Text label = result.AddComponent<Text>(); label.font = Font; label.text = value; label.fontSize = resolvedFontSize; label.color = FormalUiTheme.ReadableLabelColor(color); label.alignment = alignment;
            label.fontStyle = FontStyle.Normal; label.alignByGeometry = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; label.raycastTarget = false;
            label.resizeTextForBestFit = false; label.lineSpacing = 1f;
            return label;
        }

        public static Text PreventAutomaticWrapping(Text label)
        {
            if (label == null) return null;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.alignByGeometry = true;
            EnsureTextSlotHeight(label, 1);
            return label;
        }

        public static Text ConfigureNumericLabel(Text label)
        {
            if (label == null) return null;
            PreventAutomaticWrapping(label);
            label.resizeTextForBestFit = false;
            return label;
        }

        public static Text ConfigureParagraph(Text label, float lineSpacing = 1.08f)
        {
            if (label == null) return null;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = false;
            label.lineSpacing = lineSpacing;
            label.alignByGeometry = true;
            EnsureTextSlotHeight(label, 2);
            return label;
        }

        public static Text ConfigureReadingParagraph(Text label, float lineSpacing = 1.08f)
        {
            ConfigureParagraph(label, lineSpacing);
            if (label != null && ReadingFont != null) label.font = ReadingFont;
            return label;
        }

        private static void EnsureTextSlotHeight(Text label, int lineCount)
        {
            RectTransform rect = label.rectTransform;
            if (rect.sizeDelta.y <= 0f) return;
            float minimum = FormalUiTheme.MinimumTextSlotHeight(label.fontSize, lineCount);
            if (rect.sizeDelta.y < minimum) rect.sizeDelta = new Vector2(rect.sizeDelta.x, minimum);
        }

        public static Button Button(string name, string title, Transform parent, Vector2 position, Vector2 size, Color color, int fontSize = FormalUiTheme.ButtonFontSize)
        {
            GameObject result = Panel(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, color);
            string buttonSkin = name.Contains("结束行动") ? "button_end_turn" : OccPixelUiConfig.StateSkin("button", "normal");
            ApplySkin(result.GetComponent<Image>(), buttonSkin, color);
            Button button = result.AddComponent<Button>(); button.targetGraphic = result.GetComponent<Image>(); button.transition = Selectable.Transition.None;
            Text label = Label("文字", title, result.transform, Vector2.zero, size, fontSize, FormalUiTheme.Text, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.pivot = new Vector2(.5f, .5f); label.rectTransform.anchoredPosition = Vector2.zero; label.rectTransform.sizeDelta = Vector2.zero;
            PreventAutomaticWrapping(label);
            return button;
        }

        public static UiButtonFeedback ConfigureButtonFeedback(Button button, FormalUiButtonPalette palette, Func<UiMotionProfile> motion,
            Action<UiActionFeedback> feedback, string disabledReason = null)
        {
            UiButtonFeedback component = button.gameObject.GetComponent<UiButtonFeedback>() ?? button.gameObject.AddComponent<UiButtonFeedback>();
            component.Configure(button, palette.Normal, palette.Hover, palette.Pressed, palette.Selected, palette.Disabled, motion, feedback, disabledReason);
            return component;
        }

        public static Image IconSlot(string name, Transform parent, Sprite sprite, Vector2 position)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = new Vector2(0, .5f); rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * FormalUiTheme.IconSlotSize;
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        public static Image TopLeftIconSlot(string name, Transform parent, Sprite sprite, Vector2 position)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.one * FormalUiTheme.IconSlotSize;
            Image image = result.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Text SemanticChip(string semanticId, string value, Transform parent, Vector2 position, FormalHoverTooltip tooltip,
            int iconSize = 32, int valueFontSize = FormalUiTheme.BodyFontSize, Color? valueColor = null)
        {
            string word = semanticId == "action" ? "行动" : semanticId == "aether" ? "以太" : "注意";
            string explanation = semanticId == "action" ? "使用这项能力需要的行动点。" : semanticId == "aether"
                ? "使用这项能力需要的以太。" : "这项效果有需要留意的限制或风险。";
            Sprite sprite = Resources.Load<Sprite>(FormalArtRegistry.SemanticPath(semanticId));
            if (sprite == null) throw new KeyNotFoundException("Missing formal semantic icon: " + semanticId);
            iconSize = IntegerSpriteSize(sprite, iconSize);

            GameObject chip = Create("语义_" + semanticId, parent);
            RectTransform chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = chipRect.anchorMax = new Vector2(0, 1); chipRect.pivot = new Vector2(0, 1);
            chipRect.anchoredPosition = position; chipRect.sizeDelta = new Vector2(string.IsNullOrEmpty(value) ? iconSize : iconSize + 24, iconSize);

            GameObject iconObject = Create("图标_" + word, chip.transform);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f); iconRect.pivot = new Vector2(0, .5f);
            iconRect.anchoredPosition = Vector2.zero; iconRect.sizeDelta = Vector2.one * iconSize;
            Image icon = iconObject.AddComponent<Image>(); icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = tooltip != null;
            if (tooltip != null)
            {
                FormalHoverTooltipTrigger trigger = iconObject.AddComponent<FormalHoverTooltipTrigger>();
                trigger.Configure(tooltip, () => new FormalTooltipContent(word, explanation, semanticId == "notice" ? FormalUiTheme.Amber : FormalUiTheme.Cyan));
            }

            Text valueLabel = Label("数值", value ?? string.Empty, chip.transform, new Vector2(iconSize + 2, 0), new Vector2(22, iconSize),
                valueFontSize, valueColor ?? FormalUiTheme.Text, TextAnchor.MiddleLeft);
            valueLabel.raycastTarget = false;
            PreventAutomaticWrapping(valueLabel);
            return valueLabel;
        }

        public static int IntegerSpriteSize(Sprite sprite, float requestedSize)
        {
            if (sprite == null) return Mathf.Max(1, Mathf.RoundToInt(requestedSize));
            int native = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(sprite.rect.width, sprite.rect.height)));
            int multiplier = Mathf.Max(1, Mathf.RoundToInt(requestedSize / native));
            return native * multiplier;
        }

        public static void Line(Transform parent, Vector2 position, Vector2 size, Color color, string name = "线")
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = result.AddComponent<Image>(); image.sprite = null; image.type = Image.Type.Simple; image.color = color; image.raycastTarget = false;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }
}
