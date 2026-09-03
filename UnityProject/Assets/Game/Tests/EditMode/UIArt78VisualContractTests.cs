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

            Assert.That(health.width, Is.EqualTo(120f));
            Assert.That(health.height, Is.EqualTo(22f));
            Assert.That(shield.width, Is.EqualTo(120f));
            Assert.That(shield.height, Is.EqualTo(14f));
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

        [Test]
        public void FusionPixelTypography_KeepsCompactAndInteractiveSizesSeparate()
        {
            Assert.That(FormalUiTheme.MinimumReadableFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.MinimumCompactFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.ButtonFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.ButtonDetailFontSize, Is.EqualTo(24));
        }
    }
}
