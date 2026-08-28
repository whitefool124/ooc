using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class FormalUiThemeTests
    {
        [Test]
        public void ComponentTokens_KeepReadablePixelScaleAndMinimumTargets()
        {
            Assert.That(FormalUiTheme.CaptionFontSize, Is.GreaterThanOrEqualTo(17));
            Assert.That(FormalUiTheme.BodyFontSize, Is.GreaterThan(FormalUiTheme.CaptionFontSize));
            Assert.That(FormalUiTheme.HeadingFontSize, Is.GreaterThan(FormalUiTheme.BodyFontSize));
            Assert.That(FormalUiTheme.TitleFontSize, Is.GreaterThan(FormalUiTheme.HeadingFontSize));
            Assert.That(FormalUiTheme.IconSlotSize, Is.EqualTo(32));
            Assert.That(FormalUiTheme.MinimumInteractiveHeight, Is.GreaterThanOrEqualTo(48));
            Assert.That(FormalUiTheme.ResponsiveFontSize(14), Is.GreaterThanOrEqualTo(16));
            Assert.That(FormalUiTheme.SpaceMedium, Is.EqualTo(FormalUiTheme.SpaceSmall * 2));
            Assert.That(FormalUiTheme.SpaceLarge, Is.EqualTo(FormalUiTheme.SpaceSmall * 3));
        }

        [Test]
        public void PixelFrameTokens_RemainChunkyAtBothReferenceScales()
        {
            Assert.That(FormalUiTheme.FrameThickness, Is.EqualTo(6));
            Assert.That(FormalUiTheme.FrameCornerSize, Is.EqualTo(12));
            Assert.That(FormalUiTheme.InnerHighlightThickness, Is.EqualTo(2));
            Assert.That(FormalUiTheme.PressedOffset, Is.EqualTo(4));
            Assert.That(FormalUiTheme.FrameCornerSize, Is.GreaterThanOrEqualTo(FormalUiTheme.FrameThickness * 2));
        }

        [Test]
        public void ApplySkin_CreatesChunkySquarePixelFrameWithoutUnityOutline()
        {
            GameObject target = new GameObject("pixel-frame", typeof(RectTransform), typeof(Image));
            try
            {
                Image image = target.GetComponent<Image>();
                FormalUiKit.ApplySkin(image, "panel", FormalUiTheme.Panel);
                FormalUiKit.ApplySkin(image, "panel", FormalUiTheme.Panel);

                Assert.That(target.GetComponent<Outline>(), Is.Null);
                Transform frame = target.transform.Find("像素框架");
                Assert.That(frame, Is.Not.Null);
                Assert.That(target.transform.Cast<Transform>().Count(child => child.name == "像素框架"), Is.EqualTo(1));
                Assert.That(frame.Find("上").GetComponent<RectTransform>().sizeDelta.y, Is.EqualTo(FormalUiTheme.FrameThickness));
                Assert.That(frame.Find("左").GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(FormalUiTheme.FrameThickness));
                Assert.That(frame.Find("左上").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(Vector2.one * FormalUiTheme.FrameCornerSize));
                Assert.That(frame.Find("内高光_上").GetComponent<RectTransform>().sizeDelta.y, Is.EqualTo(FormalUiTheme.InnerHighlightThickness));
            }
            finally { Object.DestroyImmediate(target); }
        }

        [Test]
        public void BattlefieldCell_UsesTileTextureEdgeWithoutUnityOutline()
        {
            GameObject board = new GameObject("battlefield-board", typeof(RectTransform));
            GameObject viewObject = new GameObject("battlefield-view", typeof(FormalBattlefieldView));
            try
            {
                FormalBattlefieldView view = viewObject.GetComponent<FormalBattlefieldView>();
                typeof(FormalBattlefieldView).GetField("boardRect", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(view, board.GetComponent<RectTransform>());
                typeof(FormalBattlefieldView).GetMethod("CreateCell", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(view, new object[] { new GridPosition(0, 0) });

                GameObject cell = board.transform.Find("格子_0_0").gameObject;
                Assert.That(cell.GetComponent<RawImage>(), Is.Not.Null);
                Assert.That(cell.GetComponent<Outline>(), Is.Null,
                    "Battlefield grid boundaries must come from the 32x32 tile texture itself.");
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(board);
            }
        }

        [Test]
        public void TopLeftIconSlot_InterpretsPositionFromParentTopLeft()
        {
            GameObject parent = new GameObject("icon-parent", typeof(RectTransform));
            try
            {
                Image icon = FormalUiKit.TopLeftIconSlot("icon", parent.transform, null, new Vector2(32f, -30f));

                Assert.That(icon.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(icon.rectTransform.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(icon.rectTransform.pivot, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(icon.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(32f, -30f)));
            }
            finally { Object.DestroyImmediate(parent); }
        }

        [Test]
        public void BattlefieldUnits_UseOneBoardLevelLayerAboveEveryCellFloor()
        {
            GameObject board = new GameObject("battlefield-board", typeof(RectTransform));
            GameObject viewObject = new GameObject("battlefield-view", typeof(FormalBattlefieldView));
            try
            {
                FormalBattlefieldView view = viewObject.GetComponent<FormalBattlefieldView>();
                typeof(FormalBattlefieldView).GetField("boardRect", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(view, board.GetComponent<RectTransform>());
                typeof(FormalBattlefieldView).GetMethod("EnsureCells", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(view, new object[] { 2, 1 });

                Transform layer = board.transform.Find("单位独立层");
                Transform overlay = board.transform.Find("单位信息顶层");
                Assert.That(layer, Is.Not.Null);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(layer.GetSiblingIndex(), Is.EqualTo(board.transform.childCount - 2));
                Assert.That(overlay.GetSiblingIndex(), Is.EqualTo(board.transform.childCount - 1));
                Assert.That(layer.childCount, Is.EqualTo(2));
                Assert.That(layer.Cast<Transform>().All(unit => unit.name == "单位"), Is.True);
                Assert.That(overlay.childCount, Is.EqualTo(2));
                Assert.That(board.transform.Find("格子_0_0/地面"), Is.Not.Null);
                Assert.That(board.transform.Find("格子_1_0/地面"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(board);
            }
        }

        [Test]
        public void FocusFrame_UsesSemanticChunkyPixelCorners()
        {
            GameObject root = new GameObject("focus-root", typeof(RectTransform));
            try
            {
                Image focus = FormalUiKit.FocusFrame(root.transform);
                Transform frame = focus.transform.Find("像素框架");
                Assert.That(focus.sprite, Is.Null);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.Find("右下").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(Vector2.one * FormalUiTheme.FrameCornerSize));
                Assert.That(frame.Find("上").GetComponent<Image>().color, Is.EqualTo(FormalUiTheme.Focus));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void LoadoutDragHandler_CapturesPointerDownDragAndReleaseWithoutThresholdDependency()
        {
            GameObject target = new GameObject("loadout-drag");
            GameObject eventSystemObject = new GameObject("event-system", typeof(EventSystem));
            try
            {
                int begins = 0, drags = 0, ends = 0;
                RogueLoadoutDragHandler handler = target.AddComponent<RogueLoadoutDragHandler>();
                handler.Configure(_ => begins++, _ => drags++, _ => ends++, () => { });
                PointerEventData pointer = new PointerEventData(eventSystemObject.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left };
                handler.OnPointerDown(pointer); handler.OnDrag(pointer); handler.OnPointerUp(pointer);
                Assert.That((begins, drags, ends), Is.EqualTo((1, 1, 1)));
            }
            finally { Object.DestroyImmediate(target); Object.DestroyImmediate(eventSystemObject); }
        }

        [TestCase(FormalUiButtonTone.Neutral)]
        [TestCase(FormalUiButtonTone.Primary)]
        [TestCase(FormalUiButtonTone.Positive)]
        [TestCase(FormalUiButtonTone.Warning)]
        [TestCase(FormalUiButtonTone.Dangerous)]
        public void ButtonPalette_ExposesEveryRequiredInteractionState(FormalUiButtonTone tone)
        {
            FormalUiButtonPalette palette = FormalUiTheme.ButtonPalette(tone);

            Assert.That(palette.Hover, Is.Not.EqualTo(palette.Normal));
            Assert.That(palette.Pressed, Is.Not.EqualTo(palette.Normal));
            Assert.That(palette.Selected, Is.Not.EqualTo(palette.Normal));
            Assert.That(palette.Disabled, Is.EqualTo(FormalUiTheme.Disabled));
            Assert.That(Vector4.Distance(palette.Normal, palette.Selected), Is.GreaterThan(.08f));
            Assert.That(Vector4.Distance(palette.Normal, palette.Disabled), Is.GreaterThan(.08f));
        }

        [Test]
        public void SemanticColors_DoNotCollapseToOneSignal()
        {
            Assert.That(FormalUiTheme.Cyan, Is.Not.EqualTo(FormalUiTheme.Danger));
            Assert.That(FormalUiTheme.Cyan, Is.Not.EqualTo(FormalUiTheme.Amber));
            Assert.That(FormalUiTheme.Safe, Is.Not.EqualTo(FormalUiTheme.Danger));
            Assert.That(FormalUiTheme.Focus, Is.Not.EqualTo(FormalUiTheme.Disabled));
        }

        [Test]
        public void ArchiveLedgerTheme_UsesWarmLightPaperInsteadOfBlueBlackSurfaces()
        {
            Assert.That(FormalUiTheme.ThemeId, Is.EqualTo("academy-archive-ledger"));
            Assert.That(FormalUiTheme.UsesAmbientScanlines, Is.False);
            Assert.That(Luminance(FormalUiTheme.Surface), Is.GreaterThan(.75f));
            Assert.That(Luminance(FormalUiTheme.SurfaceRaised), Is.GreaterThan(Luminance(FormalUiTheme.Surface)));
            Assert.That(FormalUiTheme.Surface.r, Is.GreaterThan(FormalUiTheme.Surface.b));
            Assert.That(FormalUiTheme.Panel.r, Is.GreaterThan(FormalUiTheme.Panel.b));
            Assert.That(Luminance(FormalUiTheme.Ink), Is.LessThan(.05f));
        }

        [Test]
        public void ArchiveLedgerTheme_KeepsInkReadableAndAetherAsAccent()
        {
            Assert.That(ContrastRatio(FormalUiTheme.Text, FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(7f));
            Assert.That(ContrastRatio(FormalUiTheme.Muted, FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(FormalUiTheme.Cyan.r, Is.LessThan(FormalUiTheme.Surface.r));
            Assert.That(FormalUiTheme.Cyan.g, Is.GreaterThan(FormalUiTheme.Cyan.r));
            Assert.That(FormalUiTheme.Cyan.b, Is.GreaterThan(FormalUiTheme.Cyan.r));
        }

        [Test]
        public void SurfaceTextPairing_PreventsDarkInkOnDarkReadingSurfaces()
        {
            Assert.That(FormalUiTheme.TextForSurface(FormalUiTheme.Ink), Is.EqualTo(FormalUiTheme.OnInk));
            Assert.That(FormalUiTheme.TextForSurface(FormalUiTheme.Surface), Is.EqualTo(FormalUiTheme.Text));
            Assert.That(ContrastRatio(FormalUiTheme.TextForSurface(FormalUiTheme.Ink), FormalUiTheme.Ink), Is.GreaterThanOrEqualTo(7f));
            Assert.That(ContrastRatio(FormalUiTheme.TextForSurface(FormalUiTheme.SurfaceRaised), FormalUiTheme.SurfaceRaised), Is.GreaterThanOrEqualTo(7f));
        }

        [Test]
        public void InventoryReadingSlots_UseLightPaperWithInkOrSemanticSelection()
        {
            Assert.That(Luminance(FormalUiTheme.InventorySlotSurface), Is.GreaterThan(.65f));
            Assert.That(Luminance(FormalUiTheme.InventorySlotSelected), Is.GreaterThan(.55f));
            Assert.That(ContrastRatio(FormalUiTheme.Text, FormalUiTheme.InventorySlotSurface), Is.GreaterThanOrEqualTo(7f));
            Assert.That(ContrastRatio(FormalUiTheme.Text, FormalUiTheme.InventorySlotSelected), Is.GreaterThanOrEqualTo(7f));
            Assert.That(FormalUiTheme.InventorySlotSelected, Is.Not.EqualTo(FormalUiTheme.InventorySlotSurface));
        }

        [Test]
        public void ArchiveLedgerButtons_DoNotRestoreLegacyBlueBlackStateSprites()
        {
            GameObject root = new GameObject("archive-button", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                Image image = root.GetComponent<Image>();
                Button button = root.GetComponent<Button>(); button.targetGraphic = image;
                FormalUiKit.ConfigureButtonFeedback(button, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                    () => UiMotionProfile.FromIntensity(0f), null);
                Assert.That(image.sprite, Is.Null);
                Assert.That(image.color, Is.EqualTo(FormalUiTheme.Interactive));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ButtonFeedback_PreservesAuthoredSpriteAndReportsDisabledReason()
        {
            GameObject root = new GameObject("skinned-button", typeof(RectTransform), typeof(Image), typeof(Button));
            GameObject eventSystemObject = new GameObject("events", typeof(EventSystem));
            Texture2D texture = new Texture2D(8, 8);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 8f,
                0, SpriteMeshType.FullRect, Vector4.one * 2f);
            try
            {
                Image image = root.GetComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Sliced;
                Button button = root.GetComponent<Button>(); button.targetGraphic = image;
                UiActionFeedback reported = null;
                UiButtonFeedback feedback = FormalUiKit.ConfigureButtonFeedback(button,
                    FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                    () => UiMotionProfile.FromIntensity(0f), value => reported = value, "缺少以太");

                Assert.That(image.sprite, Is.SameAs(sprite));
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
                feedback.OnSelect(new BaseEventData(eventSystemObject.GetComponent<EventSystem>()));
                Assert.That(root.transform.Find("像素焦点框").gameObject.activeSelf, Is.True);
                feedback.SetAvailability(false, "缺少以太");
                feedback.OnPointerClick(new PointerEventData(eventSystemObject.GetComponent<EventSystem>()));
                Assert.That(reported, Is.Not.Null);
                Assert.That(reported.Kind, Is.EqualTo(UiFeedbackKind.Rejected));
                Assert.That(reported.Message, Is.EqualTo("缺少以太"));
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(eventSystemObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InventoryClickFeedback_UsesEveryConfiguredFormalFrame()
        {
            GameObject root = new GameObject("inventory-feedback", typeof(TarkovInventoryPanel));
            GameObject developerRoot = new GameObject("developer-feedback", typeof(DeveloperConsolePanel));
            try
            {
                var frames = (Texture2D[])typeof(TarkovInventoryPanel)
                    .GetField("clickFeedbackFrames", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(root.GetComponent<TarkovInventoryPanel>());
                var developerFrames = (Texture2D[])typeof(DeveloperConsolePanel)
                    .GetField("clickFeedbackFrames", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(developerRoot.GetComponent<DeveloperConsolePanel>());
                OccPeripheralFeedbackEntry click = FormalUiEffectsConfig.Feedback("click");

                Assert.That(frames, Is.Not.Null);
                Assert.That(frames, Has.Length.EqualTo(click.frameCount));
                Assert.That(developerFrames, Is.Not.Null);
                Assert.That(developerFrames, Has.Length.EqualTo(click.frameCount));
                Assert.That(click.frameCount, Is.EqualTo(6));
                for (int i = 0; i < click.frameCount; i++)
                    Assert.That(Resources.Load<Texture2D>(click.resourcePath + "/frame_" + i.ToString("00")), Is.Not.Null, "frame " + i);
            }
            finally { Object.DestroyImmediate(developerRoot); Object.DestroyImmediate(root); }
        }

        [Test]
        public void PageChecklist_CoversEveryFormalPlayerSurfaceWithStableFocusKeys()
        {
            string[] expected =
            {
                "landing", "map", "briefing", "combat", "shop-workshop",
                "inventory-loot", "settlement", "settings", "archive"
            };

            Assert.That(FormalUiPageChecklist.Entries.Select(entry => entry.Id), Is.EquivalentTo(expected));
            Assert.That(FormalUiPageChecklist.Entries.All(entry => !string.IsNullOrWhiteSpace(entry.DefaultFocusKey)), Is.True);
            Assert.That(FormalUiPageChecklist.Entries.Single(entry => entry.Id == "landing").DefaultFocusKey, Is.EqualTo("按钮_近战热压"));
            Assert.That(FormalUiPageChecklist.Entries.Single(entry => entry.Id == "settings").DefaultFocusKey, Is.EqualTo("按钮_设置_0"));
            Assert.That(FormalUiPageChecklist.Entries.Where(entry => entry.Id != "landing").All(entry => entry.HasBackPath), Is.True);
            Assert.That(FormalUiPageChecklist.Entries.Any(entry => entry.CoversDisabledState), Is.True);
            Assert.That(FormalUiPageChecklist.Entries.Any(entry => entry.CoversEmptyState), Is.True);
        }

        [Test]
        public void AccessibilityPreferencesApplyToSharedContrastAndTextTokens()
        {
            FormalUiTheme.ConfigureAccessibility(false, false);
            float baseContrast = ContrastRatio(FormalUiTheme.Text, FormalUiTheme.Panel);
            int baseSize = FormalUiTheme.ResponsiveFontSize(FormalUiTheme.BodyFontSize);
            try
            {
                FormalUiTheme.ConfigureAccessibility(true, true);
                Assert.That(FormalUiTheme.HighContrastEnabled, Is.True);
                Assert.That(FormalUiTheme.LargeTextEnabled, Is.True);
                Assert.That(ContrastRatio(FormalUiTheme.Text, FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(baseContrast));
                Assert.That(FormalUiTheme.ResponsiveFontSize(FormalUiTheme.BodyFontSize), Is.GreaterThan(baseSize));
                Assert.That(ContrastRatio(FormalUiTheme.Muted, FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
            }
            finally
            {
                FormalUiTheme.ConfigureAccessibility(false, false);
            }
        }

        [TestCase("action", "7")]
        [TestCase("aether", "4")]
        [TestCase("notice", "")]
        public void SemanticChip_UsesIconAndKeepsWordOutOfPersistentText(string semanticId, string value)
        {
            GameObject root = new GameObject("root", typeof(RectTransform));
            try
            {
                Text label = FormalUiKit.SemanticChip(semanticId, value, root.transform, Vector2.zero, null);
                Assert.That(label.text, Is.EqualTo(value));
                Image icon = root.GetComponentsInChildren<Image>().Single();
                Assert.That(icon.sprite, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Text>().Select(item => item.text), Does.Not.Contain("行动"));
                Assert.That(root.GetComponentsInChildren<Text>().Select(item => item.text), Does.Not.Contain("以太"));
                Assert.That(root.GetComponentsInChildren<Text>().Select(item => item.text), Does.Not.Contain("注意"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SemanticIcon_MouseHoverShowsPlayerWord()
        {
            GameObject canvasObject = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
            GameObject eventSystemObject = new GameObject("events", typeof(EventSystem));
            try
            {
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                FormalHoverTooltip tooltip = canvasObject.AddComponent<FormalHoverTooltip>();
                tooltip.Initialize(canvas);
                FormalUiKit.SemanticChip("action", "2", canvasObject.transform, Vector2.zero, tooltip);
                FormalHoverTooltipTrigger trigger = canvasObject.GetComponentInChildren<FormalHoverTooltipTrigger>();
                trigger.OnPointerEnter(new PointerEventData(eventSystemObject.GetComponent<EventSystem>()) { position = Vector2.zero });

                Assert.That(tooltip.IsVisible, Is.True);
                Text title = canvasObject.GetComponentsInChildren<Text>(true).Single(item => item.gameObject.name == "悬浮标题");
                Text body = canvasObject.GetComponentsInChildren<Text>(true).Single(item => item.gameObject.name == "悬浮正文");
                RectTransform panel = canvasObject.GetComponentsInChildren<RectTransform>(true).Single(item => item.gameObject.name == "悬浮详情");
                Assert.That(title.text, Is.EqualTo("行动"));
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(20));
                Assert.That(body.fontSize, Is.GreaterThanOrEqualTo(16));
                Assert.That(panel.sizeDelta.x, Is.InRange(220f, 480f));
                Canvas.ForceUpdateCanvases();
                Assert.That(title.cachedTextGenerator.vertexCount, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(eventSystemObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void SharedTooltip_UsesOpaqueRaisedPaperAndReadableInkBody()
        {
            GameObject canvasObject = new GameObject("tooltip-canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                FormalHoverTooltip tooltip = canvasObject.AddComponent<FormalHoverTooltip>();
                tooltip.Initialize(canvasObject.GetComponent<Canvas>());
                Image background = canvasObject.GetComponentsInChildren<Image>(true).Single(item => item.gameObject.name == "悬浮详情");
                Text body = canvasObject.GetComponentsInChildren<Text>(true).Single(item => item.gameObject.name == "悬浮正文");

                Assert.That(background.color, Is.EqualTo(FormalUiTheme.SurfaceRaised));
                Assert.That(background.color.a, Is.GreaterThanOrEqualTo(.98f));
                Assert.That(body.color, Is.EqualTo(FormalUiTheme.Text));
                Assert.That(ContrastRatio(body.color, background.color), Is.GreaterThanOrEqualTo(7f));
            }
            finally { Object.DestroyImmediate(canvasObject); }
        }

        [Test]
        public void TextPolish_DefaultLabelsStayInsideBoundsAndRolesAreExplicit()
        {
            GameObject root = new GameObject("typography-root", typeof(RectTransform));
            try
            {
                Text standard = FormalUiKit.Label("standard", "两行说明", root.transform, Vector2.zero, new Vector2(160, 32), 16,
                    FormalUiTheme.Text, TextAnchor.UpperLeft);
                Text numeric = FormalUiKit.Label("numeric", "18 / 18", root.transform, Vector2.zero, new Vector2(100, 24), 16,
                    FormalUiTheme.Text, TextAnchor.MiddleRight);
                Text paragraph = FormalUiKit.Label("paragraph", "第一行说明，第二行说明。", root.transform, Vector2.zero, new Vector2(160, 64), 16,
                    FormalUiTheme.Text, TextAnchor.UpperLeft);

                FormalUiKit.ConfigureNumericLabel(numeric);
                FormalUiKit.ConfigureParagraph(paragraph);

                Assert.That(standard.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
                Assert.That(numeric.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow));
                Assert.That(numeric.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
                Assert.That(numeric.alignment, Is.EqualTo(TextAnchor.MiddleRight));
                Assert.That(numeric.alignByGeometry, Is.True);
                Assert.That(numeric.resizeTextForBestFit, Is.False);
                Assert.That(paragraph.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
                Assert.That(paragraph.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
                Assert.That(paragraph.lineSpacing, Is.EqualTo(1.08f).Within(.001f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SharedTooltip_AdaptsToContentWithoutBecomingAScreenBlockingCard()
        {
            GameObject canvasObject = new GameObject("tooltip-canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(1920f, 1080f);
                FormalHoverTooltip tooltip = canvasObject.AddComponent<FormalHoverTooltip>();
                tooltip.Initialize(canvasObject.GetComponent<Canvas>());
                RectTransform panel = canvasObject.GetComponentsInChildren<RectTransform>(true).Single(item => item.gameObject.name == "悬浮详情");

                object shortOwner = new object();
                tooltip.Show(shortOwner, new FormalTooltipContent("缩小地图", "查看全貌。", FormalUiTheme.Text), new Vector2(400f, 400f));
                Vector2 shortSize = panel.sizeDelta;

                object longOwner = new object();
                tooltip.Show(longOwner, new FormalTooltipContent("当前行动", "消耗：1 行动点\n目标：相邻敌人\n效果：造成 8 点伤害，并使目标在下一回合前无法重新获得护盾\n风险：可能触发反击", FormalUiTheme.Cyan), new Vector2(400f, 400f));
                Vector2 longSize = panel.sizeDelta;

                Assert.That(shortSize.x, Is.LessThan(longSize.x));
                Assert.That(shortSize.y, Is.LessThan(longSize.y));
                Assert.That(shortSize.x, Is.LessThanOrEqualTo(340f));
                Assert.That(shortSize.y, Is.LessThanOrEqualTo(120f));
                Assert.That(longSize.x, Is.LessThanOrEqualTo(480f));
                Assert.That(longSize.y, Is.LessThanOrEqualTo(360f));
            }
            finally { Object.DestroyImmediate(canvasObject); }
        }

        [Test]
        public void EquipmentTooltip_UsesPlayerFacingAffixNamesInsteadOfInternalIds()
        {
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.CreateStarter(1717);
            OCC.Combat.Roguelite.RogueEquipmentInstance equipment = runtime.EquipmentItem("starter-chest");
            equipment.MutableAffixIds.Add("AFF-ROUND-SHIELD-P");
            MethodInfo method = typeof(FormalRogueliteUi).GetMethod("RogueEquipmentDetail", BindingFlags.NonPublic | BindingFlags.Static);

            string detail = (string)method.Invoke(null, new object[] { runtime, equipment.InstanceId });

            Assert.That(detail, Does.Contain("紫色回合盾"));
            Assert.That(detail, Does.Not.Contain("AFF-"));
        }

        [Test]
        public void EquipmentDetailPanel_TranslatesFixedAffixAndUpgradeIdsForPlayers()
        {
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.CreateStarter(1718);
            OCC.Combat.Roguelite.RogueEquipmentInstance equipment = runtime.EquipmentItem("starter-chest");
            equipment.MutableAffixIds.Add("AFF-ROUND-SHIELD-P");
            equipment.UpgradeBranchIds.Add("node1:turn_shield:+1");
            MethodInfo method = typeof(FormalRogueliteUi).GetMethod("RogueEquipmentEffects", BindingFlags.NonPublic | BindingFlags.Static);

            string detail = (string)method.Invoke(null, new object[] { runtime.DefinitionFor(equipment.InstanceId), equipment });

            Assert.That(detail, Does.Contain("回合开始获得 2 普通盾"));
            Assert.That(detail, Does.Contain("词条 · 紫色回合盾"));
            Assert.That(detail, Does.Contain("校准 · 获得护盾 +1"));
            Assert.That(detail, Does.Not.Contain("AFF-"));
            Assert.That(detail, Does.Not.Contain("node1"));
            Assert.That(detail, Does.Not.Contain("turn_start_shield"));
        }

        [Test]
        public void FireSpellRewardCopy_DescribesPlayerEffectInsteadOfImplementationEnums()
        {
            FireSpellDefinition fireball = FireSpellCatalog.All.Single(spell => spell.Id == "F-P-R01");
            FireSpellDefinition weaponLoad = FireSpellCatalog.All.Single(spell => spell.Id == "F-P-U01");
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(fireball), Does.Contain("12 点火焰伤害"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(weaponLoad), Does.Contain("下一次武器攻击"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(weaponLoad), Does.Not.Contain("WeaponAttachment"));
            Assert.That(RogueliteSettlementPresentation.FireSpellPlayerSummary(weaponLoad), Does.Not.Contain("OnTrigger"));
        }

        [Test]
        public void EveryFireSpell_UsesPlayerEffectAndTargetCopyWithoutImplementationEnums()
        {
            string[] forbidden = System.Enum.GetNames(typeof(FireCombatAffinity))
                .Concat(System.Enum.GetNames(typeof(FireDeliveryMode)))
                .Concat(System.Enum.GetNames(typeof(FireWeaponRequirement)))
                .Concat(System.Enum.GetNames(typeof(FireTriggerWindow)))
                .Concat(System.Enum.GetNames(typeof(FireConsumptionRule)))
                .Concat(System.Enum.GetNames(typeof(FireTargetKind)))
                .Concat(System.Enum.GetNames(typeof(FireSelectionShape)))
                .Concat(System.Enum.GetNames(typeof(FireRuleKind))).Distinct().ToArray();
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                string effect = RogueliteSettlementPresentation.FireSpellPlayerSummary(spell);
                string target = RogueliteSettlementPresentation.FireSpellTargetSummary(spell);
                Assert.That(effect, Is.Not.Empty, spell.Id);
                Assert.That(target, Is.Not.Empty, spell.Id);
                Assert.That(effect, Does.Not.Contain("产生术式效果"), spell.Id);
                foreach (string token in forbidden)
                {
                    Assert.That(effect, Does.Not.Contain(token), spell.Id + " effect exposed " + token);
                    Assert.That(target, Does.Not.Contain(token), spell.Id + " target exposed " + token);
                }
            }
        }

        [Test]
        public void ArchiveSpellLabels_HideInternalIdsAndUseFullWidthSlotPunctuation()
        {
            MethodInfo method = typeof(FormalRogueliteUi).GetMethod("FireSpellDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
            string name = (string)method.Invoke(null, new object[] { "F-P-R01" });

            Assert.That(name, Is.EqualTo(FireSpellCatalog.Get("F-P-R01").DisplayName));
            Assert.That(name, Does.Not.Contain("F-P-"));
            Assert.That("1：" + name, Does.Contain("："));
        }

        private static float ContrastRatio(UnityEngine.Color a, UnityEngine.Color b)
        {
            float bright = System.Math.Max(Luminance(a), Luminance(b));
            float dark = System.Math.Min(Luminance(a), Luminance(b));
            return (bright + .05f) / (dark + .05f);
        }

        private static float Luminance(UnityEngine.Color color)
        {
            return .2126f * Linear(color.r) + .7152f * Linear(color.g) + .0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= .03928f ? value / 12.92f : UnityEngine.Mathf.Pow((value + .055f) / 1.055f, 2.4f);
        }
    }
}
