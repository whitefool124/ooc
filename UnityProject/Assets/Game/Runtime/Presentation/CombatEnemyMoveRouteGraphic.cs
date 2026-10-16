using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Public move plans only: pixel lines and packets flowing toward each destination.</summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class CombatEnemyMoveRouteGraphic : MaskableGraphic
    {
        private readonly List<Vector2[]> routes = new List<Vector2[]>();
        private float pixel = 1f;
        private bool animated;
        private int lastFrame = -1;

        public void Refresh(IEnumerable<Vector2[]> paths, float cellSize, float animationIntensity)
        {
            raycastTarget = false;
            var incoming = new List<Vector2[]>();
            if (paths != null)
                foreach (var path in paths)
                    if (path != null && path.Length > 1) incoming.Add(path);
            bool same = routes.Count == incoming.Count && Mathf.Approximately(pixel, cellSize / 32f) &&
                animated == (animationIntensity > .01f);
            for (int i = 0; same && i < routes.Count; i++)
            {
                same = routes[i].Length == incoming[i].Length;
                for (int p = 0; same && p < routes[i].Length; p++) same = routes[i][p] == incoming[i][p];
            }
            if (same) return;
            routes.Clear(); routes.AddRange(incoming);
            pixel = cellSize / 32f;
            animated = animationIntensity > .01f;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!animated || routes.Count == 0) return;
            int frame = Mathf.FloorToInt(Time.unscaledTime * 15f);
            if (frame == lastFrame) return;
            lastFrame = frame; SetVerticesDirty();
        }

        public void Clear() { routes.Clear(); SetVerticesDirty(); }
        protected override void OnDisable() { Clear(); lastFrame = -1; base.OnDisable(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (pixel <= 0f) return;
            foreach (var path in routes)
            {
                for (int i = 1; i < path.Length; i++)
                {
                    Line(vh, path[i - 1], path[i], 3f * pixel, new Color32(85, 47, 24, 220));
                    Line(vh, path[i - 1], path[i], pixel, new Color32(208, 134, 55, 240));
                }
                float length = Length(path);
                if (animated)
                {
                    float phase = Mathf.Floor(Time.unscaledTime * 48f) % 20f * pixel;
                    for (float distance = phase; distance < length; distance += 20f * pixel)
                    {
                        // The packet uses cumulative distance, so it crosses a corner
                        // in the same direction instead of restarting on each segment.
                        Packet(vh, path, distance, 5f * pixel, new Color32(255, 189, 80, 255));
                        Packet(vh, path, distance, 2f * pixel, new Color32(255, 236, 172, 255));
                    }
                }
                // A stable head makes direction readable even with animation off.
                Vector2 end = path[path.Length - 1];
                Vector2 direction = (end - path[path.Length - 2]).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x);
                for (int row = 0; row < 4; row++)
                {
                    Vector2 center = end - direction * row * pixel;
                    Line(vh, center - side * row * pixel, center + side * row * pixel,
                        pixel, new Color32(255, 204, 111, 255));
                }
            }
        }

        private void Packet(VertexHelper vh, Vector2[] path, float end, float size, Color32 tint)
        {
            float offset = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                float segment = Vector2.Distance(path[i - 1], path[i]);
                float a = Mathf.Max(offset, end - size), b = Mathf.Min(offset + segment, end);
                if (b > a)
                {
                    Vector2 direction = (path[i] - path[i - 1]).normalized;
                    Line(vh, path[i - 1] + direction * (a - offset),
                        path[i - 1] + direction * (b - offset), 2f * pixel, tint);
                }
                offset += segment;
            }
        }

        public static float Length(IReadOnlyList<Vector2> path)
        {
            float length = 0f;
            for (int i = 1; i < path.Count; i++) length += Vector2.Distance(path[i - 1], path[i]);
            return length;
        }

        private void Line(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color32 tint)
        {
            tint.a = (byte)Mathf.RoundToInt(tint.a * .48f);
            a = new Vector2(Mathf.Round(a.x / pixel) * pixel, Mathf.Round(a.y / pixel) * pixel);
            b = new Vector2(Mathf.Round(b.x / pixel) * pixel, Mathf.Round(b.y / pixel) * pixel);
            float left = Mathf.Round((Mathf.Min(a.x, b.x) - thickness * .5f) / pixel) * pixel;
            float bottom = Mathf.Round((Mathf.Min(a.y, b.y) - thickness * .5f) / pixel) * pixel;
            float right = left + Mathf.Abs(a.x - b.x) + thickness;
            float top = bottom + Mathf.Abs(a.y - b.y) + thickness;
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), tint, Vector2.zero);
            vh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
