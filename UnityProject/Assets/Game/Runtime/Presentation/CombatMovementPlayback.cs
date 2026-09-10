using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public readonly struct CombatMovementPose
    {
        public Vector2 Position { get; }
        public CombatMovementPose(Vector2 position) { Position = position; }

        // Grid Y points up; top-left UI Y points down. Quantize both body and HUD together.
        public Vector2 OffsetFrom(GridPosition destination, float cellSize) => new Vector2(
            Mathf.Round((Position.x - destination.X) * cellSize / 2f) * 2f,
            Mathf.Round((destination.Y - Position.y) * cellSize / 2f) * 2f);
    }

    /// <summary>Samples only executed routes. Never seeks a path or changes a UnitState.</summary>
    public sealed class CombatMovementPlayback
    {
        public const float MaximumDuration = .35f;
        private readonly Dictionary<string, Track> tracks = new Dictionary<string, Track>();
        public int Count => tracks.Count;
        public float Remaining(string unitId, float now) => unitId != null && tracks.TryGetValue(unitId, out Track track)
            ? Mathf.Max(0f, track.StartedAt + track.Duration - now) : 0f;

        private sealed class Track
        {
            public Vector2[] Points;
            public float Length, StartedAt, Duration;
            public GridPosition Destination;
            public CombatMovementPose Sample(float now, out int next)
            {
                float distance = Mathf.Clamp01((now - StartedAt) / Duration) * Length;
                for (next = 1; next < Points.Length; next++)
                {
                    Vector2 delta = Points[next] - Points[next - 1];
                    float segment = delta.magnitude;
                    if (distance < segment)
                    {
                        return new CombatMovementPose(Points[next - 1] + delta * (distance / segment));
                    }
                    distance -= segment;
                }
                return new CombatMovementPose(Points[Points.Length - 1]);
            }
        }

        public bool Play(string unitId, GridPosition source, GridPosition destination,
            IReadOnlyList<GridPosition> path, float now)
        {
            if (string.IsNullOrEmpty(unitId)) return false;
            if (path == null || path.Count < 2 || path[0] != source || path[path.Count - 1] != destination)
            { Cancel(unitId); return false; }
            for (int i = 1; i < path.Count; i++)
                if (System.Math.Abs(path[i].X - path[i - 1].X) + System.Math.Abs(path[i].Y - path[i - 1].Y) != 1)
                { Cancel(unitId); return false; }

            var points = new List<Vector2>();
            if (tracks.TryGetValue(unitId, out Track previous) && previous.Destination == source &&
                now < previous.StartedAt + previous.Duration)
            {
                CombatMovementPose current = previous.Sample(now, out int next);
                points.Add(current.Position);
                for (int i = next; i < previous.Points.Length; i++) points.Add(previous.Points[i]);
            }
            else points.Add(new Vector2(source.X, source.Y));
            for (int i = 1; i < path.Count; i++) points.Add(new Vector2(path[i].X, path[i].Y));
            float length = 0f;
            for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
            tracks[unitId] = new Track { Points = points.ToArray(), Length = length, StartedAt = now,
                Duration = Mathf.Clamp(length * .08f, .12f, MaximumDuration),
                Destination = destination };
            return true;
        }

        public CombatMovementPose Sample(string unitId, GridPosition resolvedPosition, float now)
        {
            if (tracks.TryGetValue(unitId, out Track track))
            {
                if (track.Destination == resolvedPosition && now < track.StartedAt + track.Duration)
                    return track.Sample(now, out _);
                Cancel(unitId);
            }
            return new CombatMovementPose(new Vector2(resolvedPosition.X, resolvedPosition.Y));
        }

        public void Cancel(string unitId) { if (unitId != null) tracks.Remove(unitId); }
        public void Clear() => tracks.Clear();
    }
}
