using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Tests
{
    public sealed class AcademyDeskLampTests
    {
        [Test] public void HoverRespondsLocally_ButZeroMotionRemainsSteady()
        {
            Assert.That(AcademyDeskLamp.EvaluateLightStep(1f, 1f, true), Is.EqualTo(3));
            Assert.That(AcademyDeskLamp.EvaluateLightStep(1f, 0f, true), Is.EqualTo(1));
            Assert.That(AcademyDeskLamp.EvaluateLightStep(1f, 0f, false), Is.EqualTo(1));
        }
        [Test] public void ZeroMotion_KeepsSteadyLight_AndDoesNotBlockInput()
        {
            var root = new GameObject("desk", typeof(RectTransform));
            try
            {
                var lamp = AcademyDeskLamp.Create(root.transform, () => 0f);
                Assert.That(lamp.LightStep(0), Is.EqualTo(lamp.LightStep(100)));
                Assert.That(lamp.LightStep(100), Is.GreaterThan(0));
                Assert.That(lamp.raycastTarget, Is.False);
                Assert.That(lamp.rectTransform.rect.Contains(new Vector2(-48, -38)), Is.True);
                Assert.That(lamp.GetComponentsInChildren<Graphic>().Length, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test] public void ReducedMotion_TakesEffectOnExistingLamp()
        {
            var root = new GameObject("desk", typeof(RectTransform));
            float intensity = 1;
            try
            {
                var lamp = AcademyDeskLamp.Create(root.transform, () => intensity);
                Assert.That(lamp.LightStep(1f), Is.EqualTo(2));
                intensity = 0;
                Assert.That(lamp.LightStep(1f), Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
