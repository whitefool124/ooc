using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class AcademyMapVisualAnchor
    {
        public int GridX { get; }
        public int GridY { get; }
        public string RegionId { get; }
        public Vector2 SourcePosition { get; }

        public AcademyMapVisualAnchor(int gridX, int gridY, string regionId, Vector2 sourcePosition)
        {
            GridX = gridX;
            GridY = gridY;
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            SourcePosition = sourcePosition;
        }
    }

    // Fixed orthogonal anchor layer for the academy atlas. Runtime node type,
    // content and state remain dynamic and are never baked into this layout.
    public static class AcademyMapVisualLayout
    {
        public static readonly Vector2 SourceSize = new Vector2(1536, 864);
        public const float UnityDisplayScale = 1f;
        public static readonly Vector2 LogicalCanvasSize = SourceSize * UnityDisplayScale;
        public const float AnchorDiameter = 32f;

        private static readonly Vector2[,] positions =
        {
            { new Vector2(152, 144), new Vector2(328, 144), new Vector2(504, 144), new Vector2(680, 144), new Vector2(856, 144), new Vector2(1032, 144), new Vector2(1208, 144), new Vector2(1384, 144) },
            { new Vector2(152, 288), new Vector2(328, 288), new Vector2(504, 288), new Vector2(680, 288), new Vector2(856, 288), new Vector2(1032, 288), new Vector2(1208, 288), new Vector2(1384, 288) },
            { new Vector2(152, 432), new Vector2(328, 432), new Vector2(504, 432), new Vector2(680, 432), new Vector2(856, 432), new Vector2(1032, 432), new Vector2(1208, 432), new Vector2(1384, 432) },
            { new Vector2(152, 576), new Vector2(328, 576), new Vector2(504, 576), new Vector2(680, 576), new Vector2(856, 576), new Vector2(1032, 576), new Vector2(1208, 576), new Vector2(1384, 576) },
            { new Vector2(152, 720), new Vector2(328, 720), new Vector2(504, 720), new Vector2(680, 720), new Vector2(856, 720), new Vector2(1032, 720), new Vector2(1208, 720), new Vector2(1384, 720) }
        };

        private static readonly IReadOnlyList<AcademyMapVisualAnchor> anchors = BuildAnchors();
        public static IReadOnlyList<AcademyMapVisualAnchor> Anchors => anchors;

        public static AcademyMapVisualAnchor AnchorFor(RogueliteMapNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node.GridX < 0 || node.GridX >= 8 || node.GridY < 0 || node.GridY >= 5)
                throw new ArgumentOutOfRangeException(nameof(node), "Academy visual anchors require the frozen 8x5 topology coordinates.");
            return anchors[node.GridY * 8 + node.GridX];
        }

        public static Vector2 LogicalPositionFor(RogueliteMapNode node)
        {
            Vector2 source = AnchorFor(node).SourcePosition;
            return new Vector2(source.x * UnityDisplayScale, -source.y * UnityDisplayScale);
        }

        public static Vector2 CenteredLogicalPositionFor(RogueliteMapNode node)
        {
            Vector2 source = AnchorFor(node).SourcePosition;
            return new Vector2(source.x * UnityDisplayScale - LogicalCanvasSize.x * .5f,
                LogicalCanvasSize.y * .5f - source.y * UnityDisplayScale);
        }

        public static Vector2 SourceCenterForRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId)) throw new ArgumentNullException(nameof(regionId));
            AcademyMapVisualAnchor[] regionAnchors = anchors.Where(anchor => string.Equals(anchor.RegionId, regionId, StringComparison.Ordinal)).ToArray();
            if (regionAnchors.Length == 0) throw new KeyNotFoundException("Unknown academy map region: " + regionId);
            return new Vector2(regionAnchors.Average(anchor => anchor.SourcePosition.x), regionAnchors.Average(anchor => anchor.SourcePosition.y));
        }

        private static IReadOnlyList<AcademyMapVisualAnchor> BuildAnchors()
        {
            var result = new List<AcademyMapVisualAnchor>(40);
            for (int y = 0; y < 5; y++)
            for (int x = 0; x < 8; x++)
                result.Add(new AcademyMapVisualAnchor(x, y, RegionFor(x, y), positions[y, x]));
            return result;
        }

        private static string RegionFor(int x, int y)
        {
            if (x == 7 || x == 6 && y == 2) return "sealed_tower";
            if (y == 0 && x <= 3 || y == 1 && x <= 2) return "teaching_archive";
            if (y == 0 && x >= 4 && x <= 6 || y == 1 && x >= 3 && x <= 6) return "training_workshop";
            if (y >= 3 && x <= 2) return "market_infirmary";
            if (y == 3 && x >= 4 && x <= 6 || y == 4 && x >= 3 && x <= 6) return "campus_wilds";
            return "courtyard_dormitory";
        }
    }
}
