using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class RogueMapViewportControllerTests
    {
        [Test]
        public void ClampPan_PreventsExposingMapOutsideViewport()
        {
            Vector2 viewport = new Vector2(1400, 800);
            Vector2 content = new Vector2(3200, 1800);
            Assert.That(RogueMapViewportController.ClampPan(new Vector2(5000, -5000), viewport, content, 1), Is.EqualTo(new Vector2(900, -500)));
            Assert.That(RogueMapViewportController.ClampPan(new Vector2(-5000, 5000), viewport, content, 2), Is.EqualTo(new Vector2(-2500, 1400)));
        }

        [Test]
        public void ClampPan_CentersContentWhenViewportIsLarger()
        {
            Assert.That(RogueMapViewportController.ClampPan(new Vector2(200, -300), new Vector2(2000, 1200), new Vector2(1600, 900), 1), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ZoomAroundPoint_KeepsThePointUnderThePointer()
        {
            Vector2 contentPosition = new Vector2(120, -80);
            Vector2 pointer = new Vector2(300, 140);
            Vector2 zoomed = RogueMapViewportController.ZoomAroundPoint(contentPosition, pointer, 1, 2);
            Vector2 beforeLocal = (pointer - contentPosition) / 1;
            Vector2 afterLocal = (pointer - zoomed) / 2;
            Assert.That(afterLocal, Is.EqualTo(beforeLocal));
        }

        [Test]
        public void InteractionContract_UsesLaptopSafeThresholdAndDiscreteSharpZoom()
        {
            Assert.That(RogueMapViewportController.ReferenceDragThreshold, Is.EqualTo(10f));
            Assert.That(RogueMapViewportController.KeyboardPanSpeed, Is.GreaterThanOrEqualTo(600f));
            Assert.That(AcademyMapVisualLayout.UnityDisplayScale, Is.EqualTo(1f));
        }

        [Test]
        public void ClampPan_PreservesRestoredViewInsideCurrentZoomBounds()
        {
            Vector2 restored = RogueMapViewportController.ClampPan(new Vector2(880, -470),
                new Vector2(1872, 874), AcademyMapVisualLayout.LogicalCanvasSize, 2f);
            Assert.That(restored, Is.EqualTo(new Vector2(600, -427)));
        }
    }
}
