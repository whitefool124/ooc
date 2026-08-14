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
