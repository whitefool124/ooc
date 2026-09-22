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
        public void ActiveTurnMarker_FollowsVisualTravelAndFramesTheCellWithoutOccludingIt(float size)
        {
            Vector2 foot = new Vector2(200, 240), travel = new Vector2(14, -20);
            Rect start = CombatIntentLayout.ActiveTurnMarker(foot, size);
            Rect moved = CombatIntentLayout.ActiveTurnMarker(foot + travel, size);
            Assert.That(moved.position - start.position, Is.EqualTo(travel));
            Assert.That(moved.size, Is.EqualTo(start.size));
            Assert.That(start.yMax, Is.EqualTo(foot.y + size / 16f));
            Assert.That(start.width, Is.GreaterThan(size));
        }

        [Test]
        public void TimelineHoverArrow_SitsAboveAUnitWhenNoIntentBadgeExists()
        {
            Rect unit = new Rect(300f, 240f, 192f, 384f);
            Rect viewport = new Rect(0f, 0f, 1408f, 768f);

            Rect arrow = CombatIntentLayout.TimelineHoverArrow(unit, default, false, viewport, 192f);

            Assert.That(arrow.width, Is.EqualTo(48f));
            Assert.That(arrow.height, Is.EqualTo(48f));
            Assert.That(arrow.center.x, Is.EqualTo(unit.center.x));
            Assert.That(arrow.yMax, Is.EqualTo(unit.yMin - 6f));
        }

        [Test]
        public void TimelineHoverArrow_AvoidsEnemyIntentBadgeAndFallsBesideItAtViewportTop()
        {
            Rect unit = new Rect(980f, 32f, 192f, 384f);
            Rect intent = new Rect(1048f, 4f, 56f, 56f);
            Rect viewport = new Rect(0f, 0f, 1408f, 768f);

            Rect neighbouringIntent = new Rect(1048f, 66f, 56f, 56f);
            Rect arrow = CombatIntentLayout.TimelineHoverArrow(unit, intent, true, viewport, 192f,
                null, new[] { intent, neighbouringIntent });

            Assert.That(arrow.Overlaps(intent), Is.False);
            Assert.That(arrow.Overlaps(neighbouringIntent), Is.False);
            Assert.That(arrow.xMin, Is.GreaterThanOrEqualTo(viewport.xMin));
            Assert.That(arrow.yMin, Is.GreaterThanOrEqualTo(viewport.yMin));
            Assert.That(arrow.xMax, Is.LessThanOrEqualTo(viewport.xMax));
            Assert.That(arrow.yMax, Is.LessThanOrEqualTo(viewport.yMax));
        }

        [Test]
        public void TimelineHoverArrow_PrefersCenteredPositionAboveIntentWhenThereIsRoom()
        {
            Rect unit = new Rect(300f, 240f, 192f, 384f);
            Rect intent = new Rect(368f, 180f, 56f, 56f);
            Rect viewport = new Rect(0f, 0f, 1408f, 768f);

            Rect arrow = CombatIntentLayout.TimelineHoverArrow(unit, intent, true, viewport, 192f,
                null, new[] { intent });

            Assert.That(arrow.Overlaps(intent), Is.False);
            Assert.That(arrow.center.x, Is.EqualTo(intent.center.x));
            Assert.That(arrow.yMax, Is.EqualTo(intent.yMin - 6f));
        }
    }
}
