using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public readonly struct CombatIntentPlacement
    {
        public Rect Bounds { get; }
        public float ObscuredArea { get; }
        public bool FitsViewport { get; }
        public CombatIntentPlacement(Rect bounds, float obscuredArea, bool fitsViewport)
        { Bounds = bounds; ObscuredArea = obscuredArea; FitsViewport = fitsViewport; }
    }

    // A bounded, deterministic presentation query. Coordinates are reference pixels in board space.
    public static class CombatIntentLayout
    {
        public const int CandidateCount = 25;
        private static readonly Vector2[] Directions = {
            Vector2.up, Vector2.left, Vector2.right, new Vector2(-1, 1), new Vector2(1, 1),
            Vector2.down, new Vector2(-1, -1), new Vector2(1, -1)
        };

        public static CombatIntentPlacement Place(Rect desired, Rect viewport,
            IReadOnlyList<Rect> bodiesAndVitals, IReadOnlyList<Rect> placedBadges)
        {
            Rect best = Clamp(desired, viewport);
            float bestArea = Obscured(best, bodiesAndVitals, placedBadges);
            float bestDistance = (best.position - desired.position).sqrMagnitude;
            for (int radius = 1; radius <= 3; radius++)
            foreach (Vector2 direction in Directions)
            {
                var candidate = new Rect(desired.position + new Vector2(direction.x * (desired.width + 8) * radius,
                    -direction.y * (desired.height + 8) * radius), desired.size);
                candidate = Clamp(candidate, viewport);
                float area = Obscured(candidate, bodiesAndVitals, placedBadges);
                float distance = (candidate.position - desired.position).sqrMagnitude;
                if (area < bestArea || (Mathf.Approximately(area, bestArea) && distance < bestDistance))
                { best = candidate; bestArea = area; bestDistance = distance; }
            }
            return new CombatIntentPlacement(best, bestArea, best.xMin >= viewport.xMin && best.yMin >= viewport.yMin &&
                best.xMax <= viewport.xMax && best.yMax <= viewport.yMax);
        }

        public static Rect ActiveTurnMarker(Vector2 cellFoot, float cellSize)
        {
            float scale = cellSize / 64f;
            return new Rect(Snap(cellFoot.x - 4 * scale), Snap(cellFoot.y - cellSize - 18 * scale), 72 * scale, 86 * scale);
        }

        private static Rect Clamp(Rect rect, Rect viewport)
        {
            float left = Mathf.Ceil((viewport.xMin + 4) / 2) * 2;
            float top = Mathf.Ceil((viewport.yMin + 4) / 2) * 2;
            float right = Mathf.Max(left, Mathf.Floor((viewport.xMax - 4 - rect.width) / 2) * 2);
            float bottom = Mathf.Max(top, Mathf.Floor((viewport.yMax - 4 - rect.height) / 2) * 2);
            return new Rect(Mathf.Clamp(Snap(rect.x), left, right), Mathf.Clamp(Snap(rect.y), top, bottom), rect.width, rect.height);
        }

        private static float Obscured(Rect rect, IReadOnlyList<Rect> bodies, IReadOnlyList<Rect> badges)
        {
            float area = 0;
            if (bodies != null) foreach (Rect body in bodies) area += Intersection(rect, body, 2);
            if (badges != null) foreach (Rect badge in badges) area += Intersection(rect, badge, 4);
            return area;
        }

        private static float Intersection(Rect a, Rect b, float padding) =>
            Mathf.Max(0, Mathf.Min(a.xMax, b.xMax + padding) - Mathf.Max(a.xMin, b.xMin - padding)) *
            Mathf.Max(0, Mathf.Min(a.yMax, b.yMax + padding) - Mathf.Max(a.yMin, b.yMin - padding));

        private static float Snap(float value) => Mathf.Round(value / 2) * 2;
    }
}
