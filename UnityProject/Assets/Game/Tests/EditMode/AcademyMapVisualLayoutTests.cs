using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class AcademyMapVisualLayoutTests
    {
        [Test]
        public void Atlas_HasFortyUniqueAnchorsAndFrozenRegionCounts()
        {
            IReadOnlyList<AcademyMapVisualAnchor> anchors = AcademyMapVisualLayout.Anchors;
            Assert.That(anchors, Has.Count.EqualTo(40));
            Assert.That(anchors.Select(anchor => anchor.SourcePosition).Distinct().Count(), Is.EqualTo(40));
            Dictionary<string, int> counts = anchors.GroupBy(anchor => anchor.RegionId).ToDictionary(group => group.Key, group => group.Count());
            Assert.That(counts, Is.EquivalentTo(new Dictionary<string, int>
            {
                ["courtyard_dormitory"] = 7,
                ["teaching_archive"] = 7,
                ["training_workshop"] = 7,
                ["market_infirmary"] = 6,
                ["campus_wilds"] = 7,
                ["sealed_tower"] = 6
            }));
        }

        [Test]
        public void Atlas_LeavesSafeEdgesAndReadableAnchorSpacing()
        {
            IReadOnlyList<AcademyMapVisualAnchor> anchors = AcademyMapVisualLayout.Anchors;
            foreach (AcademyMapVisualAnchor anchor in anchors)
            {
                Assert.That(anchor.SourcePosition.x, Is.InRange(64f, AcademyMapVisualLayout.SourceSize.x - 64f), anchor.GridX + "," + anchor.GridY);
                Assert.That(anchor.SourcePosition.y, Is.InRange(64f, AcademyMapVisualLayout.SourceSize.y - 64f), anchor.GridX + "," + anchor.GridY);
            }
            float minimum = anchors.SelectMany((left, index) => anchors.Skip(index + 1).Select(right => Vector2.Distance(left.SourcePosition, right.SourcePosition))).Min();
            Assert.That(minimum, Is.GreaterThanOrEqualTo(112f));
        }

        [Test]
        public void Atlas_PreservesShortGeographicRoutesForTheAuthorityGraph()
        {
            var failures = new List<string>();
            foreach (RogueliteMapNode from in RogueliteMapCatalog.Nodes)
            foreach (string nextId in from.NextIds)
            {
                RogueliteMapNode to = RogueliteMapCatalog.Node(nextId);
                float distance = Vector2.Distance(AcademyMapVisualLayout.AnchorFor(from).SourcePosition, AcademyMapVisualLayout.AnchorFor(to).SourcePosition);
                if (distance > 430f) failures.Add(from.Id + " -> " + to.Id + " = " + distance.ToString("0"));
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void Atlas_UsesOneAndTwoTimesPixelDisplayContracts()
        {
            Assert.That(AcademyMapVisualLayout.SourceSize, Is.EqualTo(new Vector2(1536, 864)));
            Assert.That(AcademyMapVisualLayout.LogicalCanvasSize, Is.EqualTo(new Vector2(1536, 864)));
            RogueliteMapNode start = RogueliteMapCatalog.Node("start");
            Vector2 source = AcademyMapVisualLayout.AnchorFor(start).SourcePosition;
            Assert.That(AcademyMapVisualLayout.LogicalPositionFor(start), Is.EqualTo(new Vector2(source.x, -source.y)));
            Assert.That(AcademyMapVisualLayout.CenteredLogicalPositionFor(start), Is.EqualTo(new Vector2(
                source.x - 768, 432 - source.y)));
        }

        [Test]
        public void Atlas_ProvidesAValidCenterForEveryRegionShortcut()
        {
            foreach (string region in AcademyMapVisualLayout.Anchors.Select(anchor => anchor.RegionId).Distinct())
            {
                Vector2 center = AcademyMapVisualLayout.SourceCenterForRegion(region);
                Assert.That(center.x, Is.InRange(0f, AcademyMapVisualLayout.SourceSize.x));
                Assert.That(center.y, Is.InRange(0f, AcademyMapVisualLayout.SourceSize.y));
            }
        }
    }
}
