using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class PixelChineseFontTests
    {
        private const string RepresentativeText = "OCC前线行动网络首区肉鸽战术中继节点简报目标敌情结构护盾以太资源行动序列现场记录移动攻击火矢冰缚搜刮互动结束回合奖励结算失败档案设置继续推进零件补给侦测权限铸造侵蚀0123456789：／（）+-";

        [Test]
        public void RuntimeBodyFont_RestoresValidatedFusionPixelAndContainsRepresentativeGlyphs()
        {
            Font font = FormalUiKit.Font;
            Assert.That(font, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(font), Is.EqualTo("Assets/Game/Resources/Fonts/FusionPixel12ProportionalZhHans.ttf"));
            Assert.That(font, Is.SameAs(FormalUiKit.DisplayFont));
            char[] missing = RepresentativeText.Where(character => !font.HasCharacter(character)).Distinct().ToArray();
            Assert.That(missing, Is.Empty, "Missing glyphs: " + new string(missing));
            Assert.That(FormalUiTheme.MinimumReadableFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.MinimumCompactFontSize, Is.EqualTo(24));
            Assert.That(FormalUiTheme.NativeFontGrid, Is.EqualTo(12));
        }

        [Test]
        public void DisplayFont_RemainsAvailableWithPixelRasterImportContract()
        {
            Assert.That(FormalUiKit.DisplayFont, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(FormalUiKit.DisplayFont), Is.EqualTo("Assets/Game/Resources/Fonts/FusionPixel12ProportionalZhHans.ttf"));
            TrueTypeFontImporter importer = AssetImporter.GetAtPath("Assets/Game/Resources/Fonts/FusionPixel12ProportionalZhHans.ttf") as TrueTypeFontImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.fontSize, Is.EqualTo(12));
            Assert.That(importer.fontRenderingMode, Is.EqualTo(FontRenderingMode.HintedRaster));
            Assert.That(importer.includeFontData, Is.True);
        }

        [TestCase(10, 24)]
        [TestCase(24, 24)]
        [TestCase(36, 48)]
        [TestCase(48, 48)]
        [TestCase(72, 72)]
        public void RuntimeTypography_QuantizesToApprovedNativeGridTiers(int requested, int expected)
        {
            Assert.That(FormalUiTheme.PixelAlignedFontSize(requested, false), Is.EqualTo(expected));
            Assert.That(expected % FormalUiTheme.NativeFontGrid, Is.Zero);
        }
    }
}
