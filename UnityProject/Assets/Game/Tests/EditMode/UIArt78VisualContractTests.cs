using NUnit.Framework;
using OCC.Combat.Presentation;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class UIArt78VisualContractTests
    {
        [TestCase("move_range")]
        [TestCase("attack_range")]
        public void TacticalRangeV2_IsNativePointFilteredHardPixelAsset(string id)
        {
            Texture2D texture = Resources.Load<Texture2D>("Art/FormalTacticalOverlays32V2/" + id);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(32));
            Assert.That(texture.height, Is.EqualTo(32));

            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(32f));
        }

        [Test]
        public void UnitVitals_UseReadableSeparatedPixelTracks()
        {
            BattlefieldRect cell = new BattlefieldRect(0f, 0f, 128f, 128f);
            Rect health = CombatUnitHudLayout.UnitHealthBarRect(cell);
            Rect shield = CombatUnitHudLayout.UnitShieldBarRect(cell);

            Assert.That(health.width, Is.EqualTo(112f));
            Assert.That(health.height, Is.EqualTo(16f));
            Assert.That(shield.width, Is.EqualTo(112f));
            Assert.That(shield.height, Is.EqualTo(8f));
            Assert.That(health.Overlaps(shield), Is.False);
            Assert.That(FormalUiKit.SkinSprite("bar_track"), Is.Not.Null);
            Assert.That(FormalUiKit.SkinSprite("bar_segment_health"), Is.Not.Null);
            Assert.That(FormalUiKit.SkinSprite("bar_segment_mana"), Is.Not.Null);
        }

        [Test]
        public void BattlefieldUnitVitals_KeepSemanticFillAboveTheTrackAndExposeChangeFeedback()
        {
            GameObject root = new GameObject("unit-vital-root", typeof(RectTransform));
            try
            {
                typeof(FormalBattlefieldView).GetMethod("Bar", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] { "生命", root.transform, FormalUiTheme.Health });
                Transform track = root.transform.Find("生命");
                Image trackImage = track.GetComponent<Image>();
                Image fill = track.Find("当前").GetComponent<Image>();

                Assert.That(trackImage.color, Is.EqualTo(FormalUiTheme.ResourceTrack));
                Assert.That(FormalUiKit.SkinOverlay(trackImage), Is.Not.Null);
                Assert.That(FormalUiKit.SkinOverlay(fill), Is.Null,
                    "small unit fills must not reuse the thick legacy segment skin");
                Assert.That(fill.rectTransform.offsetMin, Is.EqualTo(new Vector2(2f, 2f)));
                Assert.That(fill.rectTransform.offsetMax, Is.EqualTo(new Vector2(-2f, -2f)));
                Assert.That(track.GetComponentsInChildren<RectTransform>(true)
                    .Count(rect => rect.name.StartsWith("生命比例刻度_")), Is.EqualTo(3));
                Assert.That(track.Find("变化落点"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OverviewUnitTracks_RetainVisibleFillAtHalfResolution(bool health)
        {
            GameObject root = new GameObject("overview-vital", typeof(RectTransform));
            try
            {
                object bar = typeof(FormalBattlefieldView).GetMethod("Bar", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { "资源", root.transform, FormalUiTheme.Health });
                typeof(FormalBattlefieldView).GetMethod("RefreshVital", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { bar, new CombatUnitVitalPresentation(4, 4, 0, 4),
                        new BattlefieldRect(0f, 0f, 64f, 64f), health, FormalUiTheme.Health, FormalUiTheme.Danger });
                Canvas.ForceUpdateCanvases();
                RectTransform track = root.transform.Find("资源").GetComponent<RectTransform>();
                RectTransform fill = track.Find("当前").GetComponent<RectTransform>();
                float visibleHeight = track.rect.height + fill.offsetMax.y - fill.offsetMin.y;
                Assert.That(visibleHeight * .5f, Is.GreaterThanOrEqualTo(1f));
                Assert.That(visibleHeight * .5f % 1f, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void FusionPixelTypography_KeepsCompactAndInteractiveSizesSeparate()
        {
            Assert.That(FormalUiTheme.MinimumReadableFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.MinimumCompactFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.ButtonFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.ButtonDetailFontSize, Is.EqualTo(24));
        }

        [Test]
        public void TwoConsoleModules_StayAboveCommandDeckWithReadableFiveRowTimeline()
        {
            GameObject root = new GameObject("console-layout-test", typeof(RectTransform));
            GameObject hudObject = new GameObject("console-hud-test", typeof(FormalCombatHud));
            try
            {
                OccPixelUiLayoutEntry side = OccPixelUiConfig.Layout("combat.rightConsole");
                OccPixelUiLayoutEntry commands = OccPixelUiConfig.Layout("combat.commands");
                float commandsTop = UiLayoutContract.ReferenceHeight - commands.y - commands.height;
                Assert.That(-side.y + side.height, Is.LessThanOrEqualTo(commandsTop - 16f));
                MethodInfo create = typeof(FormalCombatHud).GetMethod("ConsoleModule", BindingFlags.Static | BindingFlags.NonPublic);
                float previousBottom = 0f;
                FormalCombatHud hud = hudObject.GetComponent<FormalCombatHud>();
                foreach (string key in new[] { "hero", "timeline" })
                {
                    GameObject module = (GameObject)create.Invoke(null, new object[] { key, root.transform, "combat." + key });
                    RectTransform rect = module.GetComponent<RectTransform>();
                    float top = -rect.anchoredPosition.y;
                    Assert.That(top, Is.GreaterThan(previousBottom));
                    Assert.That(top + rect.sizeDelta.y, Is.LessThanOrEqualTo(side.height - 16f));
                    Assert.That(rect.anchoredPosition.x, Is.GreaterThanOrEqualTo(16f));
                    Assert.That(rect.anchoredPosition.x + rect.sizeDelta.x, Is.LessThanOrEqualTo(side.width - 16f));
                    Assert.That(FormalUiKit.SkinOverlay(module.GetComponent<Image>()), Is.Null);
                    Assert.That(module.GetComponent<Image>().raycastTarget, Is.True, "module details must stay hoverable");
                    previousBottom = top + rect.sizeDelta.y;
                    if (key != "timeline") continue;
                    typeof(FormalCombatHud).GetField("timelineModule", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, module);
                    MethodInfo addRow = typeof(FormalCombatHud).GetMethod("CreateTimelineSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                    for (int i = 0; i < 5; i++)
                    {
                        addRow.Invoke(hud, new object[] { i });
                        RectTransform row = module.transform.Find("行动位" + (i + 1)).GetComponent<RectTransform>();
                        Assert.That(-row.anchoredPosition.y + row.sizeDelta.y, Is.LessThanOrEqualTo(rect.sizeDelta.y - 8f));
                        foreach (Text label in row.GetComponentsInChildren<Text>())
                            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(24));
                    }
                }
            }
            finally { Object.DestroyImmediate(hudObject); Object.DestroyImmediate(root); }
        }
    }
}
