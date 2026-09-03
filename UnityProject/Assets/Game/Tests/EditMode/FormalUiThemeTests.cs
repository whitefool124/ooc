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
            Assert.That(FormalUiTheme.CaptionFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.BodyFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.HeadingFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.TitleFontSize, Is.EqualTo(48));
            Assert.That(FormalUiTheme.FeedbackFontSize, Is.EqualTo(72));
            Assert.That(FormalUiTheme.IconSlotSize, Is.EqualTo(32));
            Assert.That(FormalUiTheme.MinimumInteractiveHeight, Is.GreaterThanOrEqualTo(48));
            Assert.That(FormalUiTheme.ResponsiveFontSize(14), Is.EqualTo(24));
            Assert.That(FormalUiTheme.SpaceMedium, Is.EqualTo(FormalUiTheme.SpaceSmall * 2));
            Assert.That(FormalUiTheme.SpaceLarge, Is.EqualTo(FormalUiTheme.SpaceSmall * 3));
        }

        [Test]
        public void LightReadingSurfaces_KeepBodyAndSemanticTextAboveContrastFloor()
        {
            Assert.That(FormalUiTheme.ContrastRatio(FormalUiTheme.Text, FormalUiTheme.Surface), Is.GreaterThanOrEqualTo(7f));
            Assert.That(FormalUiTheme.ContrastRatio(FormalUiTheme.Muted, FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(FormalUiTheme.ContrastRatio(FormalUiTheme.ReadableLabelColor(FormalUiTheme.Cyan), FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(FormalUiTheme.ContrastRatio(FormalUiTheme.ReadableLabelColor(FormalUiTheme.Amber), FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(FormalUiTheme.ContrastRatio(FormalUiTheme.ReadableLabelColor(FormalUiTheme.Safe), FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(FormalUiTheme.ContrastRatio(FormalUiTheme.ReadableLabelColor(FormalUiTheme.Danger), FormalUiTheme.Panel), Is.GreaterThanOrEqualTo(4.5f));
        }

        [Test]
        public void PixelFrameTokens_RemainChunkyAtBothReferenceScales()
        {
            Assert.That(FormalUiTheme.FrameThickness, Is.EqualTo(6));
            Assert.That(FormalUiTheme.FrameCornerSize, Is.EqualTo(12));
            Assert.That(FormalUiTheme.InnerHighlightThickness, Is.EqualTo(2));
            Assert.That(FormalUiTheme.PressedOffset, Is.EqualTo(4));
            Assert.That(FormalUiTheme.FrameCornerSize, Is.GreaterThanOrEqualTo(FormalUiTheme.FrameThickness * 2));
            Assert.That(FormalUiTheme.FrameTextSafetyMargin, Is.EqualTo(6));
            Assert.That(FormalUiTheme.FramedContentInset, Is.EqualTo(12));
            Assert.That(FormalUiTheme.FullyFramedSingleLineHeight, Is.GreaterThanOrEqualTo(48));
        }

        [Test]
        public void ApplySkin_BindsAuthoredNineSliceWithoutProceduralFrame()
        {
            GameObject target = new GameObject("pixel-frame", typeof(RectTransform), typeof(Image));
            try
            {
                Image image = target.GetComponent<Image>();
                FormalUiKit.ApplySkin(image, "panel", FormalUiTheme.Panel);
                FormalUiKit.ApplySkin(image, "panel", FormalUiTheme.Panel);

                Assert.That(target.GetComponent<Outline>(), Is.Null);
                Assert.That(image.sprite, Is.Null);
                Image overlay = target.transform.Find("正式皮肤").GetComponent<Image>();
                Assert.That(overlay.sprite, Is.SameAs(FormalUiKit.SkinSprite("panel")));
                Assert.That(overlay.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(overlay.fillCenter, Is.False);
                Assert.That(overlay.sprite.border, Is.EqualTo(new Vector4(4f, 4f, 4f, 4f)));
                Assert.That(target.transform.Find("像素框架"), Is.Null);
            }
            finally { Object.DestroyImmediate(target); }
        }

        [Test]
        public void TimelineRows_UseFlatSurfacesAndKeepGlyphsAboveTheirDivider()
        {
            GameObject canvasRoot = FormalUiKit.CanvasRoot("timeline-test-canvas", 0).gameObject;
            GameObject hudObject = new GameObject("timeline-test-hud", typeof(FormalCombatHud));
            try
            {
                GameObject timeline = FormalUiKit.Panel("行动序列模块", canvasRoot.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(416f, 270f), FormalUiTheme.Panel);
                FormalCombatHud hud = hudObject.GetComponent<FormalCombatHud>();
                typeof(FormalCombatHud).GetField("timelineModule", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(hud, timeline);
                MethodInfo createRow = typeof(FormalCombatHud).GetMethod("CreateTimelineSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                for (int index = 0; index < 5; index++) createRow?.Invoke(hud, new object[] { index });
                Canvas.ForceUpdateCanvases();

                RectTransform[] rows = timeline.GetComponentsInChildren<RectTransform>(true)
                    .Where(rect => rect.name.StartsWith("行动位")).OrderBy(rect => rect.name).ToArray();
                Assert.That(rows, Has.Length.EqualTo(5));
                for (int index = 0; index < rows.Length; index++)
                {
                    RectTransform row = rows[index];
                    Assert.That(row.sizeDelta, Is.EqualTo(new Vector2(388f, 40f)));
                    Assert.That(row.anchoredPosition.y, Is.EqualTo(-48f - index * 42f));
                    Assert.That(row.Find("正式皮肤"), Is.Null,
                        "a 40px timeline row cannot spend 12px on a repeated heavy frame");
                    RectTransform divider = row.Find("细分隔").GetComponent<RectTransform>();
                    Assert.That(divider.anchoredPosition.y, Is.EqualTo(-38f));
                    Assert.That(divider.sizeDelta.y, Is.EqualTo(2f));
                    foreach (Text label in row.GetComponentsInChildren<Text>())
                    {
                        label.cachedTextGenerator.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
                        float glyphBottom = label.cachedTextGenerator.verts.Min(vertex => vertex.position.y);
                        Assert.That(glyphBottom - divider.anchoredPosition.y,
                            Is.GreaterThanOrEqualTo(FormalUiTheme.FrameTextSafetyMargin));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(canvasRoot);
            }
        }

        [Test]
        public void ActionPointPips_StayInsideHeroFrameSafeArea()
        {
            GameObject hudObject = new GameObject("action-point-safe-area-hud", typeof(FormalCombatHud));
            try
            {
                FormalCombatHud hud = hudObject.GetComponent<FormalCombatHud>();
                MethodInfo positionMethod = typeof(FormalCombatHud).GetMethod("ActionPointPipPosition", BindingFlags.Static | BindingFlags.NonPublic);
                Vector2[] positions = Enumerable.Range(0, 3)
                    .Select(index => (Vector2)positionMethod.Invoke(hud, new object[] { index })).ToArray();
                float safeRightEdge = 416f - FormalUiTheme.FramedContentInset;
                float rightmostEdge = positions.Max(position => position.x + 20f);
                Assert.That(rightmostEdge, Is.LessThanOrEqualTo(safeRightEdge));
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
            }
        }

        [Test]
        public void SharedLabel_UsesReadableChineseFontAndEvenPixelSize()
        {
            GameObject root = new GameObject("font-root", typeof(RectTransform));
            try
            {
                Text label = FormalUiKit.Label("正文", "行动点不足", root.transform, Vector2.zero, new Vector2(200, 40), 13,
                    FormalUiTheme.Text, TextAnchor.MiddleLeft);
                Assert.That(FormalUiKit.Font.name, Does.Contain("FusionPixel12ProportionalZhHans"));
                Assert.That(label.font, Is.SameAs(FormalUiKit.Font));
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(FormalUiTheme.MinimumReadableFontSize));
                Assert.That(label.fontSize % FormalUiTheme.NativeFontGrid, Is.Zero);
                Assert.That(label.fontStyle, Is.EqualTo(FontStyle.Normal));
                Assert.That(label.resizeTextForBestFit, Is.False);
                Assert.That(label.rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(FormalUiTheme.BodyTextSlotHeight));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void BattlefieldContextMenu_UsesReadableInsetTypographyWithoutFullscreenRaycastBlocker()
        {
            GameObject root = new GameObject("context-menu-root", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            GameObject viewObject = new GameObject("context-menu-view", typeof(FormalBattlefieldView));
            try
            {
                FormalBattlefieldView view = viewObject.GetComponent<FormalBattlefieldView>();
                typeof(FormalBattlefieldView).GetField("root", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(view, root);
                typeof(FormalBattlefieldView).GetMethod("EnsureContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(view, null);

                GameObject menuRoot = root.transform.Find("战场右键菜单遮罩").gameObject;
                Transform panel = menuRoot.transform.Find("战场右键行动菜单");
                Text title = panel.Find("菜单标题").GetComponent<Text>();
                Text hint = panel.Find("菜单提示").GetComponent<Text>();

                Assert.That(menuRoot.GetComponent<Image>().raycastTarget, Is.False,
                    "the invisible fullscreen root must not steal right-clicks from another battlefield cell");
                Canvas menuCanvas = menuRoot.GetComponent<Canvas>();
                Assert.That(menuCanvas.overrideSorting, Is.True);
                Assert.That(menuCanvas.sortingOrder, Is.GreaterThan(UiLayoutContract.CombatSortingOrder),
                    "the context menu must render above the combat HUD instead of being clipped by it");
                Assert.That(menuCanvas.sortingOrder, Is.LessThan(UiLayoutContract.InteractionSortingOrder),
                    "confirmation and feedback overlays must remain above the context menu");
                Assert.That(menuRoot.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                Assert.That(title.font, Is.SameAs(FormalUiKit.Font));
                Assert.That(title.fontStyle, Is.EqualTo(FontStyle.Normal));
                Assert.That(title.fontSize, Is.EqualTo(FormalUiTheme.BodyFontSize));
                Assert.That(title.rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(FormalUiTheme.BodyTextSlotHeight));
                Assert.That(title.rectTransform.anchoredPosition.x, Is.GreaterThanOrEqualTo(16f));
                Assert.That(hint.fontSize, Is.EqualTo(FormalUiTheme.BodyFontSize));
                Assert.That(hint.rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(FormalUiTheme.BodyTextSlotHeight));
                Assert.That(hint.rectTransform.anchoredPosition.x, Is.GreaterThanOrEqualTo(16f));
                Assert.That(hint.rectTransform.anchoredPosition.y, Is.EqualTo(-40f));

                typeof(FormalBattlefieldView).GetMethod("CreateContextMenuButton", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(view, null);
                RectTransform row = panel.Find("位置行动_0").GetComponent<RectTransform>();
                Text action = row.GetComponentInChildren<Text>();
                Text detail = row.Find("行动资源").GetComponent<Text>();
                action.text = "攻击 高年级陪练生";
                detail.text = "1 行动点 · 11 个人魔力";
                action.cachedTextGenerator.Populate(action.text, action.GetGenerationSettings(action.rectTransform.rect.size));
                detail.cachedTextGenerator.Populate(detail.text, detail.GetGenerationSettings(detail.rectTransform.rect.size));
                float actionTop = -(action.rectTransform.anchoredPosition.y + action.cachedTextGenerator.verts.Max(vertex => vertex.position.y));
                float actionBottom = -(action.rectTransform.anchoredPosition.y + action.cachedTextGenerator.verts.Min(vertex => vertex.position.y));
                float detailTop = -(detail.rectTransform.anchoredPosition.y + detail.cachedTextGenerator.verts.Max(vertex => vertex.position.y));
                float detailBottomClearance = row.rect.height + detail.rectTransform.anchoredPosition.y +
                    detail.cachedTextGenerator.verts.Min(vertex => vertex.position.y);
                Assert.That(actionTop, Is.GreaterThanOrEqualTo(FormalUiTheme.FramedContentInset));
                Assert.That(detailTop - actionBottom, Is.GreaterThanOrEqualTo(4f));
                Assert.That(detailBottomClearance, Is.GreaterThanOrEqualTo(FormalUiTheme.FramedContentInset));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SpellSlots_PresentKeyIconNameAndTwoResourceCostsAsOneReadableCard()
        {
            GameObject root = new GameObject("spell-slot-root", typeof(RectTransform));
            GameObject hudObject = new GameObject("spell-slot-hud", typeof(FormalCombatHud));
            try
            {
                Button button = FormalUiKit.Button("技能1", "火花", root.transform, Vector2.zero, new Vector2(268f, 76f),
                    FormalUiTheme.Interactive, FormalUiTheme.ButtonFontSize);
                Image icon = FormalUiKit.IconSlot("正式图标", button.transform, null, Vector2.zero);
                typeof(FormalCombatHud).GetMethod("ConfigureSpellSlotLayout", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] { button, icon, 0 });
                FormalCombatHud hud = hudObject.GetComponent<FormalCombatHud>();
                typeof(FormalCombatHud).GetMethod("SetCostChips", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(hud, new object[] { button, 2, 3 });

                Text name = button.transform.Find("文字").GetComponent<Text>();
                Text key = button.transform.Find("键位底/键位").GetComponent<Text>();
                RectTransform resourceBlock = button.transform.Find("术式资源块").GetComponent<RectTransform>();
                RectTransform actionCost = button.transform.Find("语义_action").GetComponent<RectTransform>();
                RectTransform aetherCost = button.transform.Find("语义_aether").GetComponent<RectTransform>();
                Image actionIcon = actionCost.Find("图标_行动").GetComponent<Image>();
                Image aetherIcon = aetherCost.Find("图标_以太").GetComponent<Image>();
                Image standardSkin = FormalUiKit.SkinOverlay(button.GetComponent<Image>());
                RectTransform frameTop = button.transform.Find("术式细框_上").GetComponent<RectTransform>();
                RectTransform frameBottom = button.transform.Find("术式细框_下").GetComponent<RectTransform>();
                RectTransform frameLeft = button.transform.Find("术式细框_左").GetComponent<RectTransform>();
                RectTransform frameRight = button.transform.Find("术式细框_右").GetComponent<RectTransform>();
                float safeRight = button.GetComponent<RectTransform>().rect.width - FormalUiTheme.FrameThickness;

                Assert.That(icon.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(6f, 0f)));
                Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(64f, 64f)));
                Assert.That(key.transform.parent.name, Is.EqualTo("键位底"));
                Assert.That(key.text, Is.EqualTo("1"));
                Assert.That(key.transform.parent.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(2f, -2f)));
                Assert.That(name.text, Is.EqualTo("火花"));
                Assert.That(name.alignment, Is.EqualTo(TextAnchor.MiddleCenter));
                Assert.That(name.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(74f, -18f)));
                Assert.That(name.rectTransform.sizeDelta, Is.EqualTo(new Vector2(122f, 40f)));
                Assert.That(resourceBlock.anchoredPosition, Is.EqualTo(new Vector2(202f, -6f)));
                Assert.That(resourceBlock.sizeDelta, Is.EqualTo(new Vector2(60f, 64f)));
                Assert.That(standardSkin, Is.Not.Null);
                Assert.That(standardSkin.gameObject.activeSelf, Is.False);
                Assert.That(button.GetComponent<Image>().sprite, Is.Null);
                Assert.That(button.GetComponent<Image>().type, Is.EqualTo(Image.Type.Simple));
                Assert.That(frameTop.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(frameTop.sizeDelta, Is.EqualTo(new Vector2(268f, 2f)));
                Assert.That(frameBottom.anchoredPosition, Is.EqualTo(new Vector2(0f, -74f)));
                Assert.That(frameBottom.sizeDelta, Is.EqualTo(new Vector2(268f, 2f)));
                Assert.That(frameLeft.sizeDelta, Is.EqualTo(new Vector2(2f, 76f)));
                Assert.That(frameRight.anchoredPosition, Is.EqualTo(new Vector2(266f, 0f)));
                Assert.That(frameRight.sizeDelta, Is.EqualTo(new Vector2(2f, 76f)));
                Assert.That(frameTop.GetComponent<Image>().color, Is.EqualTo(FormalUiTheme.Rule));
                Assert.That(resourceBlock.GetComponent<Image>().color.a, Is.EqualTo(1f));
                Assert.That(resourceBlock.GetComponent<Image>().color.b, Is.GreaterThan(resourceBlock.GetComponent<Image>().color.r));
                Assert.That(actionCost.anchoredPosition, Is.EqualTo(new Vector2(204f, -6f)));
                Assert.That(aetherCost.anchoredPosition, Is.EqualTo(new Vector2(204f, -38f)));
                Assert.That(actionCost.sizeDelta, Is.EqualTo(new Vector2(56f, 32f)));
                Assert.That(aetherCost.sizeDelta, Is.EqualTo(new Vector2(56f, 32f)));
                Assert.That(actionIcon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(32f, 32f)));
                Assert.That(aetherIcon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(32f, 32f)));
                Assert.That(aetherCost.anchoredPosition.x + aetherCost.rect.width, Is.LessThanOrEqualTo(safeRight));
                foreach (Text value in button.GetComponentsInChildren<Text>(true).Where(label => label.gameObject.name == "数值"))
                {
                    Assert.That(value.fontSize, Is.EqualTo(FormalUiTheme.BodyFontSize));
                    Assert.That(value.transform.parent.Find("费用数值底"), Is.Null);
                    Assert.That(value.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(32f, 4f)));
                    Assert.That(value.rectTransform.sizeDelta.x, Is.EqualTo(24f));
                    Assert.That(value.rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(FormalUiTheme.BodyTextSlotHeight));
                    Assert.That(value.color, Is.EqualTo(FormalUiTheme.OnInk));
                    Assert.That(FormalUiTheme.ContrastRatio(value.color, FormalUiTheme.Ink), Is.GreaterThanOrEqualTo(7f));
                    value.cachedTextGenerator.Populate(value.text, value.GetGenerationSettings(value.rectTransform.rect.size));
                    Assert.That(value.cachedTextGenerator.verts, Is.Not.Empty);
                }

                typeof(FormalCombatHud).GetMethod("SetNoticeChip", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(hud, new object[] { button, true, 2 });
                RectTransform notice = button.transform.Find("语义_notice").GetComponent<RectTransform>();
                Assert.That(name.rectTransform.sizeDelta, Is.EqualTo(new Vector2(84f, 40f)));
                Assert.That(notice.anchoredPosition, Is.EqualTo(new Vector2(164f, -24f)));
                Assert.That(notice.sizeDelta, Is.EqualTo(new Vector2(32f, 28f)));
                Assert.That(name.rectTransform.anchoredPosition.x + name.rectTransform.rect.width, Is.LessThan(notice.anchoredPosition.x));
                Assert.That(notice.anchoredPosition.x + notice.rect.width, Is.LessThan(resourceBlock.anchoredPosition.x));
                typeof(FormalCombatHud).GetMethod("SetNoticeChip", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(hud, new object[] { button, false, -1 });
                Assert.That(name.rectTransform.sizeDelta, Is.EqualTo(new Vector2(122f, 40f)));
                Assert.That(notice.gameObject.activeSelf, Is.False);
                typeof(FormalCombatHud).GetMethod("ApplySpellAvailabilityVisual", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] { button, false, "术式槽为空", false });
                Color emptyResourceColor = resourceBlock.GetComponent<Image>().color;
                Assert.That(emptyResourceColor, Is.Not.EqualTo(FormalUiTheme.Muted));
                Assert.That(emptyResourceColor.a, Is.EqualTo(1f));
                Assert.That(emptyResourceColor.b, Is.GreaterThan(emptyResourceColor.r));
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResourceBars_ExposeThickQuarterScaleAndAnimatedChangeMarker()
        {
            GameObject root = new GameObject("resource-bar-root", typeof(RectTransform));
            GameObject hudObject = new GameObject("resource-bar-hud", typeof(FormalCombatHud));
            try
            {
                FormalCombatHud hud = hudObject.GetComponent<FormalCombatHud>();
                MethodInfo create = typeof(FormalCombatHud).GetMethod("ResourceBar", BindingFlags.Instance | BindingFlags.NonPublic);
                object[] arguments = { root.transform, "生命", new Vector2(16f, -104f), FormalUiTheme.Health, null };
                Image fill = (Image)create.Invoke(hud, arguments);
                RectTransform track = root.transform.Find("生命轨道").GetComponent<RectTransform>();
                Transform marker = track.Find("生命变化落点");

                Assert.That(track.sizeDelta.y, Is.EqualTo(24f));
                Assert.That(track.GetComponent<Image>().color, Is.EqualTo(FormalUiTheme.ResourceTrack));
                Assert.That(fill.color, Is.EqualTo(FormalUiTheme.Health));
                Assert.That(FormalUiKit.SkinOverlay(fill), Is.Null);
                Assert.That(fill.rectTransform.offsetMin, Is.EqualTo(new Vector2(4f, 4f)));
                Assert.That(fill.rectTransform.offsetMax, Is.EqualTo(new Vector2(-4f, -4f)));
                Assert.That(fill.transform.GetSiblingIndex(), Is.GreaterThan(FormalUiKit.SkinOverlay(track.GetComponent<Image>()).transform.GetSiblingIndex()));
                Assert.That(FormalUiTheme.ContrastRatio(fill.color, track.GetComponent<Image>().color), Is.GreaterThan(1.5f));
                Assert.That(FormalUiTheme.Health, Is.Not.EqualTo(FormalUiTheme.Shield));
                Assert.That(FormalUiTheme.Shield, Is.Not.EqualTo(FormalUiTheme.Magic));
                Assert.That(FormalUiTheme.Magic, Is.Not.EqualTo(FormalUiTheme.Health));
                Assert.That(track.GetComponentsInChildren<RectTransform>(true).Count(rect => rect.name.StartsWith("生命比例刻度_")), Is.EqualTo(3));
                Assert.That(marker, Is.Not.Null);
                Assert.That(marker.GetComponent<RectTransform>().sizeDelta.x, Is.EqualTo(8f));
                Assert.That((string)typeof(FormalCombatHud).GetMethod("RatioText", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] { 9, 18 }), Is.EqualTo("9 / 18 · 50%"));

                MethodInfo setBar = typeof(FormalCombatHud).GetMethod("SetBar", BindingFlags.Instance | BindingFlags.NonPublic);
                object[] first = { fill, .75f, -1f };
                setBar.Invoke(hud, first);
                object[] second = { fill, .25f, first[2] };
                setBar.Invoke(hud, second);
                Assert.That(marker.GetComponent<RectTransform>().anchorMin.x, Is.EqualTo(.25f).Within(.001f));
                Assert.That(marker.GetComponent<Image>().color.a, Is.GreaterThan(.5f));
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyIntentBadge_DisplaysNative16PixelIconAtExactTwoTimesScale()
        {
            BattlefieldRect cell = new BattlefieldRect(100f, 120f, 128f, 128f);
            Rect badge = CombatUnitHudLayout.EnemyIntentBadgeRect(cell, 7);
            Rect icon = CombatUnitHudLayout.EnemyIntentIconLocalRect();
            Rect damage = CombatUnitHudLayout.EnemyIntentDamageLocalRect(badge.width);

            Assert.That(badge.size, Is.EqualTo(new Vector2(68f, 40f)));
            Assert.That(icon, Is.EqualTo(new Rect(4f, 4f, 32f, 32f)));
            Assert.That(icon.width / 16f, Is.EqualTo(2f));
            Assert.That(icon.height / 16f, Is.EqualTo(2f));
            Assert.That(damage.x, Is.GreaterThanOrEqualTo(icon.xMax));
            Assert.That(damage.xMax, Is.LessThanOrEqualTo(badge.width - 4f));
        }

        [Test]
        public void ButtonFeedback_RefreshesItsAnimationOriginAfterDynamicLayout()
        {
            GameObject root = new GameObject("dynamic-button-root", typeof(RectTransform));
            try
            {
                Button button = FormalUiKit.Button("dynamic-button", "施放术式", root.transform,
                    Vector2.zero, new Vector2(396f, 64f), FormalUiTheme.Interactive,
                    FormalUiTheme.ButtonFontSize);
                UiButtonFeedback feedback = FormalUiKit.ConfigureButtonFeedback(button,
                    FormalUiTheme.ButtonPalette(FormalUiButtonTone.Neutral),
                    () => UiMotionProfile.FromIntensity(0f), null);
                RectTransform rect = button.GetComponent<RectTransform>();
                Vector2 laidOut = new Vector2(12f, -216f);
                rect.anchoredPosition = laidOut;

                feedback.RefreshLayoutPosition();

                Vector2 basePosition = (Vector2)typeof(UiButtonFeedback)
                    .GetField("basePosition", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(feedback);
                Assert.That(basePosition, Is.EqualTo(laidOut),
                    "hover animation must start from the button's final menu row, not its creation row");
                feedback.OnPointerEnter(null);
                Assert.That(rect.anchoredPosition, Is.EqualTo(laidOut));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SemanticIcons_KeepTheirNativeThirtyTwoPixelGrid()
        {
            GameObject root = new GameObject("semantic-size-root", typeof(RectTransform));
            try
            {
                FormalUiKit.SemanticChip("action", "2", root.transform, Vector2.zero, null, 22, 14);
                Image icon = root.GetComponentsInChildren<Image>().Single();
                Assert.That(icon.sprite.rect.size, Is.EqualTo(new Vector2(32f, 32f)));
                Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(32f, 32f)));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PeripheralArtSizes_AreRoundedToWholeNativeMultiples()
        {
            GameObject root = new GameObject("peripheral-size-root", typeof(RectTransform));
            try
            {
                FormalUiEffects.AddChapterDivider(root.transform, "teaching_record", Vector2.zero, 2.4f);
                FormalUiEffects.AddChapterMarker(root.transform, "reward_brass_tag", Vector2.zero, 1.7f);
                FormalUiEffects.AddEmptyIllustration(root.transform, "empty_inventory_pouch", Vector2.zero, 90f);
                Assert.That(root.transform.Find("章节分隔横幅_teaching_record").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(256f, 64f)));
                Assert.That(root.transform.Find("章节角标_reward_brass_tag").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(64f, 64f)));
                Assert.That(root.transform.Find("空状态插图_empty_inventory_pouch").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(64f, 64f)));
            }
            finally { Object.DestroyImmediate(root); }
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
        public void FocusFrame_UsesAuthoredSemanticNineSlice()
        {
            GameObject root = new GameObject("focus-root", typeof(RectTransform));
            try
            {
                Image focus = FormalUiKit.FocusFrame(root.transform);
                Assert.That(focus.sprite, Is.Null);
                Assert.That(focus.color, Is.EqualTo(Color.clear));
                Image overlay = focus.transform.Find("正式皮肤").GetComponent<Image>();
                Assert.That(overlay.sprite, Is.SameAs(FormalUiKit.SkinSprite("focus")));
                Assert.That(overlay.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(focus.transform.Find("像素框架"), Is.Null);
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
        public void FormalButtonFeedback_SwapsApprovedStateSkins()
        {
            GameObject parent = new GameObject("formal-button-parent", typeof(RectTransform));
            GameObject eventSystemObject = new GameObject("formal-button-events", typeof(EventSystem));
            try
            {
                Button button = FormalUiKit.Button("状态按钮", "出发", parent.transform, Vector2.zero, new Vector2(180, 64), FormalUiTheme.Interactive);
                UiButtonFeedback feedback = FormalUiKit.ConfigureButtonFeedback(button, FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                    () => UiMotionProfile.FromIntensity(0f), null);
                Image image = FormalUiKit.SkinOverlay(button.targetGraphic as Image);
                EventSystem events = eventSystemObject.GetComponent<EventSystem>();
                Assert.That(image.sprite, Is.SameAs(FormalUiKit.SkinSprite("button_idle")));
                feedback.OnPointerEnter(new PointerEventData(events));
                Assert.That(image.sprite, Is.SameAs(FormalUiKit.SkinSprite("button_hover")));
                feedback.OnPointerDown(new PointerEventData(events) { button = PointerEventData.InputButton.Left });
                Assert.That(image.sprite, Is.SameAs(FormalUiKit.SkinSprite("button_pressed")));
                feedback.SetAvailability(false, "暂时不能出发");
                Assert.That(image.sprite, Is.SameAs(FormalUiKit.SkinSprite("button_disabled")));
            }
            finally
            {
                Object.DestroyImmediate(eventSystemObject);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ButtonFeedback_OnlyPrimaryPointerClicksAndDisabledRejectsLocally()
        {
            GameObject eventSystemObject = new GameObject("event-system", typeof(EventSystem));
            GameObject root = new GameObject("primary-button", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                Image image = root.GetComponent<Image>();
                Button button = root.GetComponent<Button>(); button.targetGraphic = image;
                UiActionFeedback reported = null;
                UiButtonFeedback feedback = FormalUiKit.ConfigureButtonFeedback(button,
                    FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                    () => UiMotionProfile.FromIntensity(1f), value => reported = value, "缺少许可");
                EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();

                feedback.OnPointerClick(new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Right });
                Assert.That(root.transform.Cast<Transform>().Any(child => child.name.StartsWith("像素反馈_")), Is.False);
                Assert.That(reported, Is.Null);

                feedback.SetAvailability(false, "缺少许可");
                feedback.OnPointerClick(new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left });
                Assert.That(reported, Is.Not.Null);
                Assert.That(reported.Kind, Is.EqualTo(UiFeedbackKind.Rejected));
                Assert.That(reported.Message, Is.EqualTo("缺少许可"));
                Assert.That(root.transform.Cast<Transform>().Any(child => child.name == "像素反馈_rejected"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(eventSystemObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ButtonFeedback_KeyboardSubmitStartsAVisiblePressPulse()
        {
            GameObject eventSystemObject = new GameObject("event-system", typeof(EventSystem));
            GameObject root = new GameObject("submit-button", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                Image image = root.GetComponent<Image>();
                Button button = root.GetComponent<Button>(); button.targetGraphic = image;
                UiButtonFeedback feedback = FormalUiKit.ConfigureButtonFeedback(button,
                    FormalUiTheme.ButtonPalette(FormalUiButtonTone.Primary),
                    () => UiMotionProfile.FromIntensity(1f), null);

                feedback.OnSubmit(new BaseEventData(eventSystemObject.GetComponent<EventSystem>()));

                FieldInfo pressing = typeof(UiButtonFeedback).GetField("pressing", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((bool)pressing.GetValue(feedback), Is.True);
                Assert.That(root.transform.Cast<Transform>().Any(child => child.name == "像素反馈_click"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(eventSystemObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InteractionToast_NeverBlocksInputAndLongMessagesRemainLonger()
        {
            GameObject host = new GameObject("interaction-test-host");
            GameObject canvasRoot = null;
            try
            {
                FormalUiInteractionLayer layer = host.AddComponent<FormalUiInteractionLayer>();
                layer.ShowFeedback(new UiActionFeedback(UiFeedbackKind.Rejected,
                    "该操作当前不能执行，请先完成前置选择并确认目标范围后重试"));
                canvasRoot = GameObject.Find("正式交互层");
                Transform toast = canvasRoot.transform.Find("短时提示条");
                Assert.That(toast, Is.Not.Null);
                CanvasGroup group = toast.GetComponent<CanvasGroup>();
                Assert.That(group.blocksRaycasts, Is.False);
                Assert.That(group.interactable, Is.False);
                Assert.That(toast.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget), Is.True);

                MethodInfo duration = typeof(FormalUiInteractionLayer).GetMethod("FeedbackHoldDuration", BindingFlags.Static | BindingFlags.NonPublic);
                float shortHold = (float)duration.Invoke(null, new object[] { "已保存" });
                float longHold = (float)duration.Invoke(null, new object[] { "该操作当前不能执行，请先完成前置选择并确认目标范围后重试" });
                Assert.That(shortHold, Is.EqualTo(1.35f).Within(.001f));
                Assert.That(longHold, Is.GreaterThan(shortHold));
                Assert.That(longHold, Is.LessThanOrEqualTo(3.25f));
            }
            finally
            {
                Object.DestroyImmediate(host);
                if (canvasRoot != null) Object.DestroyImmediate(canvasRoot);
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
            Assert.That(FormalUiPageChecklist.Entries.Single(entry => entry.Id == "landing").DefaultFocusKey, Is.EqualTo("按钮_近战训练"));
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
            Assert.That(detail, Does.Contain("附加 · 紫色回合盾"));
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
