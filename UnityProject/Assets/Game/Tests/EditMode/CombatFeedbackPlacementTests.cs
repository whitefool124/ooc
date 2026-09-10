using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatFeedbackPlacementTests
    {
        private static readonly BattlefieldRect Viewport = new BattlefieldPresentationAdapter().ViewportRect;

        [Test]
        public void NarrowStatusObstacle_RequiresOnlySixteenPixelsOfDisplacement()
        {
            var occupied = new List<Rect> {new Rect(-370,20,24,20)};
            Vector2 center = CombatVisualFeedback.ResolveFeedbackCenter(new Vector2(-270,60),new Vector2(168,104),Viewport,52,occupied);
            Assert.That(center,Is.EqualTo(new Vector2(-254,60)));
        }

        [Test]
        public void StatusColumn_PrefersClearVerticalSpaceOverNearbySidewaysSpace()
        {
            var occupied = new List<Rect> {new Rect(-370,20,24,20)};
            Vector2 center = CombatVisualFeedback.ResolveFeedbackCenter(new Vector2(-270,60),new Vector2(168,104),Viewport,52,occupied,true);
            Assert.That(center.x,Is.EqualTo(-270));
            Assert.That(CombatVisualFeedback.FeedbackSweep(center,new Vector2(168,104),52).Overlaps(occupied[0]),Is.False);
        }

        [TestCase(0f)]
        [TestCase(28f)]
        public void CompositeStatusesAndDamage_HaveDisjointCompleteTrajectories(float rise)
        {
            var occupied = new List<Rect>();
            for (int i = 0; i < 4; i++)
            {
                Vector2 size = i == 3 ? new Vector2(168, 104) : new Vector2(240, 48);
                float travel = i == 3 && rise > 0 ? 52 : rise;
                Vector2 center = CombatVisualFeedback.ResolveFeedbackCenter(new Vector2(-270, 60), size, Viewport, travel, occupied);
                Rect sweep = CombatVisualFeedback.FeedbackSweep(center, size, travel);
                foreach (Rect other in occupied) Assert.That(sweep.Overlaps(other), Is.False);
                Assert.That(center.x % 2, Is.Zero);
                Assert.That(center.y % 2, Is.Zero);
                occupied.Add(sweep);
            }
        }

        [TestCase(-900f, 500f)]
        [TestCase(900f, -500f)]
        public void EdgeAndCrowdedFallback_StayInsideViewportIncludingRise(float x, float y)
        {
            var occupied = new List<Rect> { new Rect(-2000,-2000,4000,4000) };
            Vector2 desired = new Vector2(x,y), size = new Vector2(240,48);
            Vector2 center = CombatVisualFeedback.ResolveFeedbackCenter(desired,size,Viewport,28,occupied);
            Rect sweep = CombatVisualFeedback.FeedbackSweep(center,size,28);
            Assert.That(sweep.xMin, Is.GreaterThanOrEqualTo(Viewport.X-960+8));
            Assert.That(sweep.xMax, Is.LessThanOrEqualTo(Viewport.XMax-960-8));
            Assert.That(sweep.yMin, Is.GreaterThanOrEqualTo(540-Viewport.YMax+8));
            Assert.That(sweep.yMax, Is.LessThanOrEqualTo(540-Viewport.Y-8));
            Assert.That(CombatVisualFeedback.ResolveFeedbackCenter(desired,size,Viewport,28,occupied), Is.EqualTo(center));
        }

        [Test]
        public void ReleasedPopup_NoLongerReservesSpace_AndLeaderKeepsItsOriginWhileRising()
        {
            var host = new GameObject("feedback-test");
            var first = new GameObject("first",typeof(RectTransform));
            var second = new GameObject("second",typeof(RectTransform));
            try
            {
                var feedback = host.AddComponent<CombatVisualFeedback>();
                var method = typeof(CombatVisualFeedback).GetMethod("PlaceFeedback",BindingFlags.Instance|BindingFlags.NonPublic);
                var a = first.GetComponent<RectTransform>(); a.sizeDelta = new Vector2(240,48);
                var b = second.GetComponent<RectTransform>(); b.sizeDelta = a.sizeDelta;
                var target = new GridPosition(4,3);
                object[] Args(RectTransform rect) => new object[] {rect,target,new Vector2(-270,60),28f};
                method.Invoke(feedback,Args(a)); Vector2 initial = a.anchoredPosition;
                object placement = method.Invoke(feedback,Args(b));
                Assert.That(b.anchoredPosition,Is.Not.EqualTo(initial));
                FieldInfo originField = placement.GetType().GetField("Origin");
                Vector2 origin = (Vector2)originField.GetValue(placement);
                typeof(CombatVisualFeedback).GetMethod("MoveFeedback",BindingFlags.Static|BindingFlags.NonPublic)
                    .Invoke(null,new object[] {placement,b.anchoredPosition.y+28});
                var line = (RectTransform)placement.GetType().GetField("Horizontal").GetValue(placement);
                Vector2 start = b.anchoredPosition + line.anchoredPosition;
                // The rendered connector still reaches the original cell after the label rises.
                Assert.That(origin.x, Is.InRange(start.x-2, start.x+line.sizeDelta.x+2));
                Assert.That(origin.y, Is.InRange(start.y-2, start.y+line.sizeDelta.y+2));
                Assert.That(line.anchoredPosition.x % 2, Is.Zero);
                Assert.That(line.anchoredPosition.y % 2, Is.Zero);
                foreach (var image in second.GetComponentsInChildren<UnityEngine.UI.Image>()) Assert.That(image.raycastTarget,Is.False);
                Object.DestroyImmediate(first); Object.DestroyImmediate(second);
                first = new GameObject("replacement",typeof(RectTransform));
                a = first.GetComponent<RectTransform>(); a.sizeDelta = new Vector2(240,48);
                method.Invoke(feedback,Args(a));
                Assert.That(a.anchoredPosition,Is.EqualTo(initial));
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); Object.DestroyImmediate(host); }
        }
    }
}
