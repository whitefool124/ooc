using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public static class FormalUiTheme
    {
        public static Color Ink => OccPixelUiConfig.Palette("ink");
        public static Color Panel => OccPixelUiConfig.Palette("panel");
        public static Color Cyan => OccPixelUiConfig.Palette("cyan");
        public static Color Amber => OccPixelUiConfig.Palette("amber");
        public static Color Safe => OccPixelUiConfig.Palette("safe");
        public static Color Danger => OccPixelUiConfig.Palette("danger");
        public static Color Text => OccPixelUiConfig.Palette("text");
        public static Color Muted => OccPixelUiConfig.Palette("muted");
        public static readonly Color Disabled = new Color(.04f, .046f, .05f, 1f);
        public static readonly Color Surface = new Color(.028f, .040f, .050f, 1f);
        public static readonly Color Focus = new Color(.82f, .95f, 1f, 1f);

        public const int CaptionFontSize = 15;
        public const int BodyFontSize = 18;
        public const int HeadingFontSize = 22;
        public const int TitleFontSize = 31;
        public const int SpaceSmall = 8;
        public const int SpaceMedium = 16;
        public const int SpaceLarge = 24;
        public const int IconSlotSize = 28;
        public const int IconTextInset = 28;
        public static readonly Vector2 FocusDistance = new Vector2(2f, -2f);

        public static int PixelAlignedFontSize(int fontSize, bool compact)
        {
            if (!compact) return fontSize;
            int target = fontSize <= 18 ? Mathf.CeilToInt(fontSize * 1.25f) : fontSize;
            return target % 2 == 0 ? target : target + 1;
        }

        public static int ResponsiveFontSize(int fontSize) => PixelAlignedFontSize(fontSize, Screen.height <= UiLayoutContract.CompactHeightThreshold);
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
            return new FormalUiButtonPalette(normal, Color.Lerp(normal, accent, .24f), Color.Lerp(normal, Color.black, .24f), Color.Lerp(normal, accent, .36f), FormalUiTheme.Disabled);
        }
    }

    public static class FormalUiMotionTokens
    {
        public const float RewardStaggerMultiplier = .55f;
        public static Ease FeedbackEase => Ease.OutQuad;
        public static Ease StandardEase => Ease.OutCubic;
    }

    public static class FormalUiKit
    {
        private static Font font;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> skin = new System.Collections.Generic.Dictionary<string, Sprite>(StringComparer.Ordinal);
        public const string FontResourcePath = "Fonts/FusionPixel12ProportionalZhHans";
        public static Font Font => font != null ? font : font = Resources.Load<Font>(FontResourcePath);

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
            image.sprite = SkinSprite(id); image.type = Image.Type.Sliced;
            image.color = new Color(Mathf.Lerp(1f, tint.r, .22f), Mathf.Lerp(1f, tint.g, .22f), Mathf.Lerp(1f, tint.b, .22f), tint.a);
            image.pixelsPerUnitMultiplier = 1f;
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
            rect.offsetMin = new Vector2(-3, -3); rect.offsetMax = new Vector2(3, 3);
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
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            Text label = result.AddComponent<Text>(); label.font = Font; label.text = value; label.fontSize = FormalUiTheme.ResponsiveFontSize(fontSize); label.color = color; label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; label.raycastTarget = false;
            return label;
        }

        public static Text PreventAutomaticWrapping(Text label)
        {
            if (label == null) return null;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        public static Button Button(string name, string title, Transform parent, Vector2 position, Vector2 size, Color color, int fontSize = 16)
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

        public static void Line(Transform parent, Vector2 position, Vector2 size, Color color, string name = "线")
        {
            GameObject result = Panel(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, size, color);
            Image image = result.GetComponent<Image>(); ApplySkin(image, "bar_fill", color); image.raycastTarget = false;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
    }
}
