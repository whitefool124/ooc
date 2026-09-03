using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class AcademyMap3DLayoutTests
    {
        [Test]
        public void RenderContract_IsHighResolutionTopDownAndNonPixelated()
        {
            Assert.That(AcademyMap3DLayout.RenderWidth, Is.EqualTo(1536));
            Assert.That(AcademyMap3DLayout.RenderHeight, Is.EqualTo(864));
            Assert.That(AcademyMap3DLayout.RenderAntiAliasing, Is.GreaterThanOrEqualTo(2));
            Assert.That(AcademyMapVisualLayout.SourceSize, Is.EqualTo(new Vector2(1536, 864)));
            Assert.That(AcademyMap3DLayout.CameraPitch, Is.EqualTo(90f));
            Assert.That(AcademyMap3DLayout.CameraYaw, Is.EqualTo(0f));
        }

        [Test]
        public void DistrictLayout_HasNineStableApproximateZones()
        {
            string[] expectedIds =
            {
                "entrance", "teaching_archive", "central_public", "dormitory", "workshop",
                "market_medical", "harbour_logistics", "wilds", "sealed_tower"
            };
            Assert.That(AcademyMap3DLayout.Districts.Count, Is.EqualTo(expectedIds.Length));
            Assert.That(AcademyMap3DLayout.Districts.Select(district => district.Id), Is.EquivalentTo(expectedIds));
            Assert.That(AcademyMap3DLayout.Districts.Select(district => district.Id).Distinct().Count(),
                Is.EqualTo(AcademyMap3DLayout.Districts.Count));
            foreach (AcademyMapDistrictSpec district in AcademyMap3DLayout.Districts)
            foreach (Vector2 point in district.MapPolygon)
            {
                Assert.That(point.x, Is.InRange(0f, AcademyMapVisualLayout.SourceSize.x), district.Id);
                Assert.That(point.y, Is.InRange(0f, AcademyMapVisualLayout.SourceSize.y), district.Id);
            }
        }

        [Test]
        public void CentralPublicDistrict_ContainsTheMapCenter()
        {
            AcademyMapDistrictSpec central = AcademyMap3DLayout.Districts.Single(district => district.Id == "central_public");
            Vector2 mapCenter = AcademyMapVisualLayout.SourceSize * .5f;
            Assert.That(central.DisplayName, Does.Contain("图书馆"));
            Assert.That(AcademyMap3DLayout.PointInPolygon(mapCenter, central.MapPolygon), Is.True);
        }

        [Test]
        public void DistrictLayout_HasNoInteriorOverlap()
        {
            for (int left = 0; left < AcademyMap3DLayout.Districts.Count; left++)
            for (int right = left + 1; right < AcademyMap3DLayout.Districts.Count; right++)
            {
                AcademyMapDistrictSpec a = AcademyMap3DLayout.Districts[left];
                AcademyMapDistrictSpec b = AcademyMap3DLayout.Districts[right];
                Assert.That(AcademyMap3DLayout.PolygonsOverlap(a.MapPolygon, b.MapPolygon), Is.False,
                    a.Id + " overlaps " + b.Id);
            }
        }

        [Test]
        public void FixedDistrictMap_DoesNotChangeDynamicNodeAuthority()
        {
            Assert.That(AcademyMapVisualLayout.Anchors.Count, Is.EqualTo(40));
            Assert.That(RogueliteMapCatalog.Nodes.Count, Is.EqualTo(40));
            Assert.That(AcademyMap3DLayout.Districts.All(district => district.MapPolygon.Count >= 3), Is.True);
        }

        [Test]
        public void SimplifiedBackdrop_HasOnlyCoarseOrientationLayers()
        {
            Assert.That(AcademyMap3DLayout.OrientationRoadCount, Is.InRange(5, 12));
            Assert.That(AcademyMap3DLayout.DefenceSegmentCount, Is.InRange(6, 16));
            Assert.That(AcademyMap3DLayout.Districts.Count, Is.LessThanOrEqualTo(10));
            Assert.That(AcademyMap3DLayout.Roads.Count, Is.EqualTo(AcademyMap3DLayout.OrientationRoadCount));
            foreach (AcademyMapRoadSpec road in AcademyMap3DLayout.Roads)
            {
                Assert.That(road.ControlPoints.Count, Is.GreaterThanOrEqualTo(4), road.Id);
                bool hasBend = false;
                for (int i = 1; i < road.ControlPoints.Count - 1; i++)
                {
                    Vector2 incoming = road.ControlPoints[i] - road.ControlPoints[i - 1];
                    Vector2 outgoing = road.ControlPoints[i + 1] - road.ControlPoints[i];
                    if (Mathf.Abs(incoming.x * outgoing.y - incoming.y * outgoing.x) > 100f) hasBend = true;
                }
                Assert.That(hasBend, Is.True, road.Id + " must not be a straight segment");
            }
        }

        [Test]
        public void MapCoordinates_ProjectToStableWorldBounds()
        {
            Assert.That(AcademyMap3DLayout.MapToWorld(Vector2.zero), Is.EqualTo(new Vector3(-12f, 0f, 6.75f)));
            Assert.That(AcademyMap3DLayout.MapToWorld(AcademyMapVisualLayout.SourceSize), Is.EqualTo(new Vector3(12f, 0f, -6.75f)));
            Vector2 topLeft = AcademyMap3DLayout.ProjectMapToCanvas(Vector2.zero);
            Vector2 bottomRight = AcademyMap3DLayout.ProjectMapToCanvas(AcademyMapVisualLayout.SourceSize);
            Assert.That(topLeft.x, Is.EqualTo(-768f).Within(.01f));
            Assert.That(topLeft.y, Is.EqualTo(432f).Within(.01f));
            Assert.That(bottomRight.x, Is.EqualTo(768f).Within(.01f));
            Assert.That(bottomRight.y, Is.EqualTo(-432f).Within(.01f));
            Vector2 center = AcademyMap3DLayout.ProjectMapToCanvas(AcademyMapVisualLayout.SourceSize * .5f);
            Assert.That(center.x, Is.EqualTo(0f).Within(.01f));
            Assert.That(center.y, Is.EqualTo(0f).Within(.01f));
            foreach (AcademyMapVisualAnchor anchor in AcademyMapVisualLayout.Anchors)
            {
                Vector2 projected = AcademyMap3DLayout.ProjectMapToCanvas(anchor.SourcePosition);
                Assert.That(Mathf.Abs(projected.x), Is.LessThan(760f), anchor.GridX + "," + anchor.GridY);
                Assert.That(Mathf.Abs(projected.y), Is.LessThan(430f), anchor.GridX + "," + anchor.GridY);
            }
        }
    }
}
