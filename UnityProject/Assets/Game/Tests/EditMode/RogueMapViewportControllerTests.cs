using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class RogueMapViewportControllerTests
    {
        [TestCase(0f,0f)]
        [TestCase(1f,6f)]
        public void EdgeDrag_RespectsAnimationPreferenceAndClearsSpringOnZoom(float animation,float overshoot)
        {
            var root=new GameObject("map-test",typeof(RectTransform),typeof(Canvas));
            try
            {
                var viewObject=new GameObject("viewport",typeof(RectTransform));viewObject.transform.SetParent(root.transform,false);
                var viewport=viewObject.GetComponent<RectTransform>();viewport.sizeDelta=new Vector2(400,300);
                var contentObject=new GameObject("map",typeof(RectTransform));contentObject.transform.SetParent(viewport,false);
                var content=contentObject.GetComponent<RectTransform>();content.sizeDelta=new Vector2(800,600);
                var controller=viewObject.AddComponent<RogueMapViewportController>();controller.Initialize(viewport,content,root.GetComponent<Canvas>());
                controller.SetAnimationIntensity(()=>animation);controller.SetView(new Vector2(200,0),0);
                var pointer=new UnityEngine.EventSystems.PointerEventData(null){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,position=Vector2.zero};
                controller.OnBeginDrag(pointer);pointer.position=new Vector2(200,0);pointer.delta=new Vector2(200,0);controller.OnDrag(pointer);
                Assert.That(content.anchoredPosition.x,Is.EqualTo(200+overshoot).Within(.01));
                controller.OnEndDrag(pointer);controller.ZoomIn();Vector2 zoomed=content.anchoredPosition;
                typeof(RogueMapViewportController).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(controller,null);
                Assert.That(content.anchoredPosition,Is.EqualTo(zoomed),"Old spring must not override a newly zoomed view");
            }
            finally {Object.DestroyImmediate(root);}
        }

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
