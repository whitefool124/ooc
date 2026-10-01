using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class UiLayoutContractTests
    {
        [Test]
        public void ReferenceAndSortingLayers_AreStable()
        {
            Assert.That(UiLayoutContract.ReferenceWidth, Is.EqualTo(1920));
            Assert.That(UiLayoutContract.ReferenceHeight, Is.EqualTo(1080));
            Assert.That(UiLayoutContract.MatchWidthOrHeight, Is.EqualTo(.5f));
            Assert.That(UiLayoutContract.SafeAreaPadding, Is.EqualTo(24));
            Assert.That(UiLayoutContract.CompactHeightThreshold, Is.EqualTo(600));
            Assert.That(UiLayoutContract.HasValidLayerOrder, Is.True);
        }

        [Test]
        public void EveryRegisteredLayoutStaysInsideBothSupportedSixteenByNineResolutions()
        {
            foreach (OccPixelUiLayoutEntry layout in OccPixelUiConfig.Data.layouts)
            {
                Rect reference = Resolve(layout, UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight, 1f);
                Rect compact = Resolve(layout, 960, 540, .5f);
                AssertInside(reference, UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight, layout.id + "@1920x1080");
                AssertInside(compact, 960, 540, layout.id + "@960x540");
            }
        }

        [TestCase(960, 540, 3)]
        [TestCase(1280, 720, 4)]
        [TestCase(1366, 768, 4)]
        [TestCase(1600, 900, 5)]
        [TestCase(1920, 1080, 6)]
        [TestCase(1920, 1200, 6)]
        [TestCase(2560, 1440, 8)]
        [TestCase(2560, 1600, 8)]
        [TestCase(3440, 1440, 8)]
        [TestCase(3840, 2160, 12)]
        public void MonitorProfilesKeepLayoutsInsideOnePixelAlignedFrame(int width, int height, int nativeScale)
        {
            float scale = UiResolutionLayout.ScreenScale(width, height);
            Assert.That(scale * UiLayoutContract.NativePixelsAtReference, Is.EqualTo(nativeScale).Within(.0001f));
            Rect content = UiResolutionLayout.ScreenContentRect(width, height);
            AssertInside(content, width, height, "content");
            Assert.That(content.xMin, Is.EqualTo(Mathf.Round(content.xMin)));
            Assert.That(content.yMin, Is.EqualTo(Mathf.Round(content.yMin)));
            Assert.That(content.width / content.height, Is.EqualTo(16f / 9f).Within(.0001f));
            foreach (OccPixelUiLayoutEntry layout in OccPixelUiConfig.Data.layouts)
            {
                Rect region = Resolve(layout, content.width, content.height, scale);
                region.position += content.position;
                AssertInside(region, width, height, layout.id + "@" + width + "x" + height);
            }
        }

        [TestCase(1366, 768)]
        [TestCase(1920, 1200)]
        [TestCase(2560, 1440)]
        [TestCase(3440, 1440)]
        [TestCase(3840, 2160)]
        public void GuiAndBattlefieldInputAgreeWithTheRenderedReferenceFrame(int width, int height)
        {
            Rect content = UiResolutionLayout.ScreenContentRect(width, height);
            float scale = UiResolutionLayout.ScreenScale(width, height);
            Vector2 reference = new Vector2(1888f, 1032f);
            Vector2 guiPoint = UiResolutionLayout.GuiMatrix(width, height).MultiplyPoint3x4(reference);
            Vector2 screenPoint = new Vector2(content.x + reference.x * scale,
                content.yMax - reference.y * scale);
            Assert.That(guiPoint.x, Is.EqualTo(screenPoint.x).Within(.001f));
            Assert.That(guiPoint.y, Is.EqualTo(height - screenPoint.y).Within(.001f));
            Vector2 recovered = BattlefieldViewportInputController.ScreenToReferenceUi(screenPoint,
                width, height, UiLayoutContract.ReferenceWidth, UiLayoutContract.ReferenceHeight);
            Assert.That(recovered.x, Is.EqualTo(reference.x).Within(.001f));
            Assert.That(recovered.y, Is.EqualTo(reference.y).Within(.001f));
        }

        private static Rect Resolve(OccPixelUiLayoutEntry layout, float width, float height, float scale)
        {
            Vector2 anchor = FormalUiKit.ResolveAnchor(layout.anchor);
            float rectWidth = layout.width * scale;
            float rectHeight = layout.height * scale;
            float left = anchor.x * width + layout.x * scale - rectWidth * anchor.x;
            float bottom = anchor.y * height + layout.y * scale - rectHeight * anchor.y;
            return new Rect(left, bottom, rectWidth, rectHeight);
        }

        private static void AssertInside(Rect rect, float width, float height, string id)
        {
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f), id);
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f), id);
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(width), id);
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(height), id);
        }
    }
}
