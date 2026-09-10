using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>Board coordinates have a top-left origin; texture UVs have a bottom-left origin.</summary>
    public readonly struct CombatOcclusionLayer
    {
        public Rect Rect { get; }
        public Rect Uv { get; }
        public CombatTextureAlphaMask Mask { get; }
        public CombatOcclusionLayer(Rect rect, Rect uv, CombatTextureAlphaMask mask)
        { Rect = rect; Uv = uv; Mask = mask; }

        public bool Covers(Vector2 point)
        {
            if (Mask == null || Rect.width <= 0 || Rect.height <= 0 || !Rect.Contains(point)) return false;
            return Mask.Sample(Uv.x + (point.x - Rect.x) / Rect.width * Uv.width,
                Uv.y + (1f - (point.y - Rect.y) / Rect.height) * Uv.height);
        }
    }

    public static class CombatUnitOcclusion
    {
        public static void BuildBoundary(CombatTextureAlphaMask mask, Rect uv, List<Vector2Int> result)
        {
            result.Clear();
            if (mask == null) return;
            for (int y = 0; y < mask.Height; y++)
            for (int x = 0; x < mask.Width; x++)
            {
                if (!Opaque(mask, uv, x, y)) continue;
                if (!Opaque(mask, uv, x - 1, y) || !Opaque(mask, uv, x + 1, y) ||
                    !Opaque(mask, uv, x, y - 1) || !Opaque(mask, uv, x, y + 1))
                    result.Add(new Vector2Int(x, y));
            }
        }

        private static bool Opaque(CombatTextureAlphaMask mask, Rect uv, int x, int y)
        {
            if (x < 0 || y < 0 || x >= mask.Width || y >= mask.Height) return false;
            return mask.Sample(uv.x + (x + .5f) / mask.Width * uv.width,
                uv.y + (y + .5f) / mask.Height * uv.height);
        }

        public static void HiddenBoundary(Rect subject, int width, int height,
            IReadOnlyList<Vector2Int> boundary, IReadOnlyList<CombatOcclusionLayer> foreground,
            List<Vector2Int> result)
        {
            result.Clear();
            if (width <= 0 || height <= 0 || subject.width <= 0 || subject.height <= 0) return;
            foreach (Vector2Int pixel in boundary)
            {
                Vector2 point = new Vector2(subject.x + (pixel.x + .5f) / width * subject.width,
                    subject.yMax - (pixel.y + .5f) / height * subject.height);
                for (int i = 0; i < foreground.Count; i++)
                    if (foreground[i].Covers(point)) { result.Add(pixel); break; }
            }
        }
    }
}
