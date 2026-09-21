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
        // 固定教学段在第 4 列的商店收束；展开区整体向右接 5 列，避免重新铺回旧节点上。
        public const float AcademyLayerSourceOffsetX = 1000f;
        public static readonly Vector2 LogicalCanvasSize = SourceSize * UnityDisplayScale;
        public const float AnchorDiameter = 32f;

        private static readonly Vector2[,] positions =
        {
            { new Vector2(68, 64), new Vector2(268, 64), new Vector2(468, 64), new Vector2(668, 64), new Vector2(868, 64), new Vector2(1068, 64), new Vector2(1268, 64), new Vector2(1468, 64) },
            { new Vector2(68, 248), new Vector2(268, 248), new Vector2(468, 248), new Vector2(668, 248), new Vector2(868, 248), new Vector2(1068, 248), new Vector2(1268, 248), new Vector2(1468, 248) },
            { new Vector2(68, 432), new Vector2(268, 432), new Vector2(468, 432), new Vector2(668, 432), new Vector2(868, 432), new Vector2(1068, 432), new Vector2(1268, 432), new Vector2(1468, 432) },
            { new Vector2(68, 616), new Vector2(268, 616), new Vector2(468, 616), new Vector2(668, 616), new Vector2(868, 616), new Vector2(1068, 616), new Vector2(1268, 616), new Vector2(1468, 616) },
            { new Vector2(68, 800), new Vector2(268, 800), new Vector2(468, 800), new Vector2(668, 800), new Vector2(868, 800), new Vector2(1068, 800), new Vector2(1268, 800), new Vector2(1468, 800) }
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

        public static Vector2 SourcePositionFor(RogueliteMapNode node, bool continueAfterTutorial)
        {
            Vector2 source = AnchorFor(node).SourcePosition;
            if (continueAfterTutorial && RogueliteAcademyLayerCatalog.IsAcademyLayerNode(node.Id))
                source.x += AcademyLayerSourceOffsetX;
            return source;
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
