using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatUnitOcclusionTests
    {
        private static readonly Rect FullUv = new Rect(0, 0, 1, 1);
        private static CombatTextureAlphaMask Mask(int width, int height, params int[] opaque)
        {
            var pixels = new Color32[width * height];
            foreach (int i in opaque) pixels[i] = new Color32(255, 255, 255, 255);
            return new CombatTextureAlphaMask(width, height, pixels);
        }
        private static CombatTextureAlphaMask Solid3() => Mask(3, 3, 0, 1, 2, 3, 4, 5, 6, 7, 8);
        private static List<Vector2Int> Boundary(CombatTextureAlphaMask mask, Rect uv)
        { var result = new List<Vector2Int>(); CombatUnitOcclusion.BuildBoundary(mask, uv, result); return result; }

        [Test]
        public void InteriorPixelsNeverBecomeFilledGhostBody()
        {
            var boundary = Boundary(Solid3(), FullUv);
            Assert.That(boundary.Count, Is.EqualTo(8));
            Assert.That(boundary.Contains(new Vector2Int(1, 1)), Is.False);
            var hidden = new List<Vector2Int>();
            CombatUnitOcclusion.HiddenBoundary(new Rect(0, 0, 6, 6), 3, 3, boundary,
                new[] { new CombatOcclusionLayer(new Rect(0, 0, 6, 6), FullUv, Solid3()) }, hidden);
            CollectionAssert.AreEquivalent(boundary, hidden);
        }

        [Test]
        public void TransparentOccluderAndEmptyForegroundLeaveOriginalArtAlone()
        {
            var boundary = Boundary(Solid3(), FullUv); var result = new List<Vector2Int>();
            CombatUnitOcclusion.HiddenBoundary(new Rect(0, 0, 6, 6), 3, 3, boundary,
                new[] { new CombatOcclusionLayer(new Rect(0, 0, 6, 6), FullUv, Mask(1, 1)) }, result);
            Assert.That(result, Is.Empty);
            CombatUnitOcclusion.HiddenBoundary(new Rect(0, 0, 6, 6), 3, 3, boundary,
                new CombatOcclusionLayer[0], result);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void PartialCoverageMarksOnlyTheHiddenSide()
        {
            var result = new List<Vector2Int>();
            CombatUnitOcclusion.HiddenBoundary(new Rect(0, 0, 6, 6), 3, 3, Boundary(Solid3(), FullUv),
                new[] { new CombatOcclusionLayer(new Rect(0, 0, 2, 6), FullUv, Mask(1, 1, 0)) }, result);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) }, result);
        }

        [TestCase(1f)]
        [TestCase(2f)]
        [TestCase(4f)]
        public void IntegerScaleAndTranslatedBoardKeepTheSameNativePixels(float scale)
        {
            var result = new List<Vector2Int>();
            CombatUnitOcclusion.HiddenBoundary(new Rect(17, 31, 3*scale, 3*scale), 3, 3, Boundary(Solid3(), FullUv),
                new[] { new CombatOcclusionLayer(new Rect(17,31,scale,3*scale),FullUv,Mask(1,1,0)) },result);
            Assert.That(result.Count, Is.EqualTo(3));
            foreach (var pixel in result) Assert.That(pixel.x, Is.Zero);
        }

        [Test]
        public void CroppedTerrainUvSamplesItsActualForegroundRows()
        {
            var source = Mask(1, 4, 0); // only bottom row of the original terrain is opaque
            var front = new CombatOcclusionLayer(new Rect(4, 20, 8, 4), new Rect(0,0,1,.5f), source);
            Assert.That(front.Covers(new Vector2(8,23)), Is.True);
            Assert.That(front.Covers(new Vector2(8,21)), Is.False);
            Assert.That(front.Covers(new Vector2(8,19)), Is.False);
        }

        [Test]
        public void SourceAndOccluderUvFlipsPreserveCorrectSide()
        {
            var source = Mask(2,1,0);
            CollectionAssert.AreEqual(new[] {new Vector2Int(1,0)}, Boundary(source,new Rect(1,0,-1,1)));
            var flipped = new CombatOcclusionLayer(new Rect(0,0,4,2),new Rect(1,0,-1,1),source);
            Assert.That(flipped.Covers(new Vector2(1,1)), Is.False);
            Assert.That(flipped.Covers(new Vector2(3,1)), Is.True);
        }

        [Test]
        public void ContourCanRenderInsideAClippedCanvas()
        {
            var root = new GameObject("ContourCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var clip = new GameObject("Clip",typeof(RectTransform),typeof(RectMask2D));
                clip.transform.SetParent(root.transform,false);
                clip.GetComponent<RectTransform>().sizeDelta = new Vector2(100,100);
                var go = new GameObject("Contour",typeof(CombatUnitContourGraphic));
                go.transform.SetParent(clip.transform,false);
                var graphic = go.GetComponent<CombatUnitContourGraphic>();
                graphic.rectTransform.sizeDelta = new Vector2(6,6);
                Assert.That(go.GetComponent<CanvasRenderer>(), Is.Not.Null);
                var rect = new Rect(0,0,6,6); var mask = Solid3();
                graphic.Refresh(mask,FullUv,rect,new[] {new CombatOcclusionLayer(rect,FullUv,mask)},Color.cyan);
                Assert.DoesNotThrow(Canvas.ForceUpdateCanvases);
                graphic.Rebuild(CanvasUpdate.PreRender);
                Assert.That(graphic.canvasRenderer.GetMesh().vertexCount, Is.EqualTo(32));
                Assert.That(graphic.raycastTarget, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void GraphicReusesBoundaryAndGeometryAndRemovesStaleContour()
        {
            var go = new GameObject("ContourTest",typeof(RectTransform),typeof(CombatUnitContourGraphic));
            try
            {
                var graphic = go.GetComponent<CombatUnitContourGraphic>();
                var mask = Solid3(); var rect = new Rect(0,0,6,6);
                var front = new[] {new CombatOcclusionLayer(rect,FullUv,mask)};
                graphic.Refresh(mask,FullUv,rect,front,Color.cyan);
                int builds=graphic.BoundaryBuildCount, changes=graphic.GeometryChangeCount;
                for(int i=0;i<10;i++) graphic.Refresh(mask,FullUv,rect,front,Color.cyan);
                Assert.That(graphic.BoundaryBuildCount, Is.EqualTo(builds));
                Assert.That(graphic.GeometryChangeCount, Is.EqualTo(changes));
                Assert.That(graphic.raycastTarget, Is.False);
                Assert.That(graphic.HiddenPixelCount, Is.EqualTo(8));
                graphic.Refresh(mask,FullUv,rect,new CombatOcclusionLayer[0],Color.cyan);
                Assert.That(graphic.HiddenPixelCount, Is.Zero);
                Assert.That(go.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
