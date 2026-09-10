using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OCC.Combat.Tests
{
    public sealed class CombatUnitPixelRaycastTests
    {
        private static Color32[] Diagonal() => new[] {
            new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 0),
            new Color32(255, 255, 255, 0), new Color32(255, 255, 255, 255) };

        [Test]
        public void PackedMaskPreservesRowsByteBoundariesAndAlphaThreshold()
        {
            var pixels = new Color32[9]; pixels[0].a = 127; pixels[1].a = 128; pixels[8].a = 255;
            var mask = new CombatTextureAlphaMask(3, 3, pixels);
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                Assert.That(mask.Sample((x + .5f) / 3, (y + .5f) / 3), Is.EqualTo(x + y * 3 == 1 || x + y * 3 == 8));
            Assert.That(mask.Sample(1, 1), Is.True);
            Assert.That(mask.Sample(float.NaN, .5f), Is.False);
        }

        [TestCase(128f, 1f)]
        [TestCase(128f, .5f)]
        [TestCase(256f, 1f)]
        [TestCase(256f, .5f)]
        public void GraphicSpaceHitsOpaquePixelsAndLetsTransparentPixelsThrough(float size, float scale)
        {
            GameObject root = null;
            try
            {
                root = new GameObject("UnitHitTest", typeof(RectTransform), typeof(RawImage), typeof(CombatUnitPixelRaycast));
                var image = root.GetComponent<RawImage>(); var filter = root.GetComponent<CombatUnitPixelRaycast>();
                image.rectTransform.sizeDelta = Vector2.one * size;
                image.rectTransform.position = new Vector3(200, 150, 0);
                image.rectTransform.localScale = new Vector3(scale, scale, 1);
                filter.Bind(image, new CombatTextureAlphaMask(2, 2, Diagonal()));
                Assert.That(image.raycastTarget, Is.True);
                Assert.That(Hit(image, filter, .25f, .25f), Is.True);
                Assert.That(Hit(image, filter, .75f, .75f), Is.True);
                Assert.That(Hit(image, filter, .25f, .75f), Is.False);
                Assert.That(Hit(image, filter, .75f, .25f), Is.False);
                Assert.That(Hit(image, filter, -1f, .25f), Is.False);
                Assert.That(Hit(image, filter, 1f, .25f), Is.False);
            }
            finally { if (root != null) Object.DestroyImmediate(root); }
        }

        [Test]
        public void UvFlipMovementAndHiddenTintFollowCurrentGraphic()
        {
            var root = new GameObject("UnitTransformHitTest", typeof(RectTransform), typeof(RawImage), typeof(CombatUnitPixelRaycast));
            try
            {
                var image = root.GetComponent<RawImage>(); var filter = root.GetComponent<CombatUnitPixelRaycast>();
                image.rectTransform.sizeDelta = new Vector2(128, 128);
                filter.Bind(image, new CombatTextureAlphaMask(2, 2, Diagonal()));
                image.uvRect = new Rect(1, 0, -1, 1);
                Assert.That(Hit(image, filter, .25f, .25f), Is.False);
                Assert.That(Hit(image, filter, .75f, .25f), Is.True);
                Vector3 oldPoint = Point(image, .75f, .25f);
                image.rectTransform.position += new Vector3(300, 200, 0);
                Assert.That(filter.IsRaycastLocationValid(oldPoint, null), Is.False);
                Assert.That(Hit(image, filter, .75f, .25f), Is.True);
                image.color = new Color(1, 1, 1, 0);
                Assert.That(Hit(image, filter, .75f, .25f), Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingMaskKeepsGridFallbackInsteadOfAFullRectangleHit()
        {
            var root = new GameObject("MissingMaskTest", typeof(RectTransform), typeof(RawImage), typeof(CombatUnitPixelRaycast));
            try
            {
                var image = root.GetComponent<RawImage>(); var filter = root.GetComponent<CombatUnitPixelRaycast>();
                image.rectTransform.sizeDelta = Vector2.one * 128;
                filter.Bind(image, null);
                Assert.That(image.raycastTarget, Is.False);
                Assert.That(Hit(image, filter, .5f, .5f), Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NonReadableTextureIsCapturedOnceWithCorrectAlphaAndRenderTargetRestored()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            RenderTexture original = RenderTexture.active;
            RenderTexture sentinel = RenderTexture.GetTemporary(4, 4, 0);
            try
            {
                texture.SetPixels32(Diagonal()); texture.Apply(false, true);
                Assert.That(texture.isReadable, Is.False);
                RenderTexture.active = sentinel;
                var cache = new CombatTextureAlphaMaskCache();
                var first = cache.Get(texture);
                Assert.That(first, Is.Not.Null);
                Assert.That(first.Sample(.25f, .25f), Is.True);
                Assert.That(first.Sample(.25f, .75f), Is.False);
                Assert.That(first.Sample(.75f, .25f), Is.False);
                Assert.That(first.Sample(.75f, .75f), Is.True);
                for (int i = 0; i < 20; i++) Assert.That(cache.Get(texture), Is.SameAs(first));
                Assert.That(cache.CaptureCount, Is.EqualTo(1));
                Assert.That(cache.FailedCaptureCount, Is.Zero);
                Assert.That(RenderTexture.active, Is.SameAs(sentinel));
                Assert.That(texture.isReadable, Is.False);
            }
            finally { RenderTexture.active = original; RenderTexture.ReleaseTemporary(sentinel); Object.DestroyImmediate(texture); }
        }

        private static Vector3 Point(RawImage image, float u, float v) => image.rectTransform.TransformPoint(
            new Vector3(image.rectTransform.rect.xMin + u * image.rectTransform.rect.width,
                image.rectTransform.rect.yMin + v * image.rectTransform.rect.height, 0));
        private static bool Hit(RawImage image, CombatUnitPixelRaycast filter, float u, float v) =>
            filter.IsRaycastLocationValid(Point(image, u, v), null);
    }
}
