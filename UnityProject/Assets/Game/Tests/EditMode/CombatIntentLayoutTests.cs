using NUnit.Framework;
using UnityEngine;
using OCC.Combat.Presentation;

namespace OCC.Combat.Tests
{
    public sealed class CombatIntentLayoutTests
    {
        private static readonly Rect View = new Rect(0, 0, 640, 480);

        [Test]
        public void ClearBadge_RetainsItsAuthoredAttachment()
        {
            Rect desired = new Rect(200, 100, 68, 40);
            var placed = CombatIntentLayout.Place(desired, View, null, null);
            Assert.That(placed.Bounds, Is.EqualTo(desired));
            Assert.That(placed.ObscuredArea, Is.Zero);
            Assert.That(placed.FitsViewport, Is.True);
        }

        [Test]
        public void NeighborBodyOverlap_MovesBadgeToNearbyFreeSpace()
        {
            Rect desired = new Rect(200, 100, 68, 40), body = new Rect(190, 120, 100, 120);
            var placed = CombatIntentLayout.Place(desired, View, new[] { body }, null);
            Assert.That(placed.Bounds, Is.Not.EqualTo(desired));
            Assert.That(placed.Bounds.Overlaps(body), Is.False);
            Assert.That(placed.ObscuredArea, Is.Zero);
            Assert.That(Vector2.Distance(placed.Bounds.position, desired.position), Is.LessThanOrEqualTo(80));
        }

        [Test]
        public void AllocatedBadgeAndVitals_AreProtectedTogether()
        {
            Rect desired = new Rect(200, 100, 68, 40);
            Rect vital = new Rect(200, 48, 56, 12);
            var placed = CombatIntentLayout.Place(desired, View, new[] { vital }, new[] { desired });
            Assert.That(placed.ObscuredArea, Is.Zero);
            Assert.That(placed.Bounds.Overlaps(desired), Is.False);
            Assert.That(placed.Bounds.Overlaps(vital), Is.False);
        }

        [TestCase(-250f, -150f)]
        [TestCase(650f, 490f)]
        public void ViewportEdge_ClampsWithoutResizingTextOrLeavingFractionalPixels(float x, float y)
        {
            var placed = CombatIntentLayout.Place(new Rect(x, y, 68, 40), View, null, null);
            Assert.That(placed.FitsViewport, Is.True);
            Assert.That(placed.Bounds.size, Is.EqualTo(new Vector2(68, 40)));
            Assert.That(placed.Bounds.x % 2, Is.Zero);
            Assert.That(placed.Bounds.y % 2, Is.Zero);
        }

        [Test]
        public void FullyCrowdedViewport_ReturnsDeterministicVisibleFallbackAndReportsCrowding()
        {
            Rect desired = new Rect(200, 100, 68, 40);
            var obstacles = new[] { View };
            var first = CombatIntentLayout.Place(desired, View, obstacles, null);
            var again = CombatIntentLayout.Place(desired, View, obstacles, null);
            Assert.That(first.ObscuredArea, Is.GreaterThan(0));
            Assert.That(first.FitsViewport, Is.True);
            Assert.That(again.Bounds, Is.EqualTo(first.Bounds));
            Assert.That(obstacles[0], Is.EqualTo(View));
        }

        [TestCase(64f)]
        [TestCase(128f)]
        public void ActiveFootprint_FollowsVisualTravelAtBothWorldPixelScales(float size)
        {
            Vector2 foot = new Vector2(200, 240), travel = new Vector2(14, -20);
            Rect start = CombatIntentLayout.ActiveFootprint(foot, size);
            Rect moved = CombatIntentLayout.ActiveFootprint(foot + travel, size);
            Assert.That(moved.position - start.position, Is.EqualTo(travel));
            Assert.That(moved.size, Is.EqualTo(start.size));
            Assert.That(start.yMax, Is.LessThan(foot.y));
            Assert.That(start.width, Is.LessThan(size));
        }
    }
}
