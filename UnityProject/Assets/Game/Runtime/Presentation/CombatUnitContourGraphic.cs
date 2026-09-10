using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Only hidden contour texels become UI quads; no source art or hit area is generated.</summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class CombatUnitContourGraphic : MaskableGraphic
    {
        private readonly List<Vector2Int> boundary = new List<Vector2Int>();
        private readonly List<Vector2Int> hidden = new List<Vector2Int>();
        private readonly List<Vector2Int> scratch = new List<Vector2Int>();
        private CombatTextureAlphaMask source;
        private Rect sourceUv;
        public int HiddenPixelCount => hidden.Count;
        public int BoundaryBuildCount { get; private set; }
        public int GeometryChangeCount { get; private set; }

        public void Refresh(CombatTextureAlphaMask mask, Rect uv, Rect boardRect,
            IReadOnlyList<CombatOcclusionLayer> foreground, Color tint)
        {
            raycastTarget = false;
            color = tint;
            bool changed = !ReferenceEquals(source, mask) || sourceUv != uv;
            if (changed)
            {
                source = mask; sourceUv = uv;
                CombatUnitOcclusion.BuildBoundary(mask, uv, boundary);
                BoundaryBuildCount++;
            }
            CombatUnitOcclusion.HiddenBoundary(boardRect, mask?.Width ?? 0, mask?.Height ?? 0,
                boundary, foreground, scratch);
            changed |= hidden.Count != scratch.Count;
            if (!changed)
                for (int i = 0; i < hidden.Count; i++)
                    if (hidden[i] != scratch[i]) { changed = true; break; }
            if (changed)
            {
                hidden.Clear(); hidden.AddRange(scratch);
                GeometryChangeCount++; SetVerticesDirty();
            }
            gameObject.SetActive(hidden.Count > 0);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (source == null) return;
            Rect rect = rectTransform.rect;
            float sx = rect.width / source.Width, sy = rect.height / source.Height;
            Color32 tint = color;
            foreach (Vector2Int pixel in hidden)
            {
                float x = rect.xMin + pixel.x * sx, y = rect.yMin + pixel.y * sy;
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(x, y), tint, Vector2.zero);
                vh.AddVert(new Vector3(x, y + sy), tint, Vector2.zero);
                vh.AddVert(new Vector3(x + sx, y + sy), tint, Vector2.zero);
                vh.AddVert(new Vector3(x + sx, y), tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
