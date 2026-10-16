using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class PeripheralPixelVfxTests
    {
        private GameObject root;
        [SetUp] public void SetUp()
        {
            root = new GameObject("VFX test canvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 600f);
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void DisabledAnimation_CreatesNoTransientObjects()
        {
            Assert.That(PeripheralPixelVfx.Spawn(root.transform, "forge", 0f, Vector2.one * 64f, Vector2.zero, Vector2.one * .5f), Is.Null);
            Assert.That(root.transform.childCount, Is.Zero);
        }

        [Test]
        public void ClosedPage_DoesNotCreateFeedback()
        {
            root.SetActive(false);
            Assert.That(PeripheralPixelVfx.Spawn(root.transform, "receipt", 1f, Vector2.one * 64f, Vector2.zero, Vector2.one * .5f), Is.Null);
        }

        [TestCase("forge")]
        [TestCase("specialize")]
        [TestCase("heal")]
        [TestCase("meal")]
        [TestCase("purchase")]
        [TestCase("receipt")]
        public void ServiceFeedback_NeverInterceptsInput_AndRespectsMask(string motif)
        {
            var effect = PeripheralPixelVfx.Spawn(root.transform, motif, 1f, Vector2.one * 64f, Vector2.zero, Vector2.one * .5f);
            Assert.That(effect.raycastTarget, Is.False);
            Assert.That(effect.maskable, Is.True);
            Assert.That(effect.GetComponent<Button>(), Is.Null);
        }

        [Test]
        public void MapTrace_HasViewportBounds_ForRectMaskClipping()
        {
            var effect = PeripheralPixelVfx.SpawnRoute(root.transform, new[] { Vector2.zero, new Vector2(120f, 0f) }, 1f);
            Assert.That(effect.rectTransform.rect.size, Is.EqualTo(root.GetComponent<RectTransform>().rect.size));
            Assert.That(effect.raycastTarget, Is.False);
        }
    }
}
