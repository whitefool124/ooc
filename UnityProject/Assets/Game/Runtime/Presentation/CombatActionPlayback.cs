using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Presentation
{
    public interface ICombatActionPresentationHost
    {
        bool IsCombatActionPlaying { get; }
        int CombatActionPresentationVersion { get; }
        UnitState PresentCombatUnit(UnitState unit);
    }

    public sealed class CombatActionCapture : IDisposable
    {
        internal readonly Dictionary<string, CombatUnitDisplaySnapshot> Units;
        internal readonly Dictionary<GridPosition, TileState> Tiles = new Dictionary<GridPosition, TileState>();
        internal readonly Dictionary<GridPosition, bool> Fireground = new Dictionary<GridPosition, bool>();
        internal readonly List<(Action Show, float Start)> Deliveries = new List<(Action, float)>();
        internal readonly List<CombatFeedbackEvent> Feedback = new List<CombatFeedbackEvent>();
        internal readonly Dictionary<GridPosition, float> Impacts = new Dictionary<GridPosition, float>();
        internal readonly GridPosition Source;
        internal readonly float Delay;
        internal float Duration = .34f;
        internal float TriggerDelay;
        internal Action CompleteAction, AbortAction;

        internal CombatActionCapture(CombatState state, FireBattleState fire, string actorId, float delay)
        {
            Delay = delay; Source = state.GetUnit(actorId)?.Position ?? default;
            Units = state.Units.Values.ToDictionary(unit => unit.Id, unit => new CombatUnitDisplaySnapshot(unit));
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++)
            {
                var p = new GridPosition(x, y); Tiles[p] = state.Map.GetTile(p).Clone();
                Fireground[p] = fire?.HasFireground(p) == true;
            }
        }

        public void Complete() { var action = CompleteAction; CompleteAction = null; AbortAction = null; action?.Invoke(); }
        public void Dispose() { CompleteAction = null; var action = AbortAction; AbortAction = null; action?.Invoke(); }

        public void AddFeedback(CombatFeedbackEvent feedback) => Feedback.Add(feedback);
        public void AddDelivery(Action delivery, IEnumerable<CombatVfxCue> cues = null, float startOffset = 0f)
        {
            Deliveries.Add((delivery, startOffset));
            if (cues == null) return;
            foreach (CombatVfxCue cue in cues)
            {
                Duration = Math.Max(Duration, cue.End + startOffset);
                if (cue.Effect == "fire_cast" || cue.Effect == "fire_projectile" || cue.Effect == "fire_spray" ||
                    cue.Effect == "fire_line" || cue.Effect == "path") continue;
                if (!Impacts.TryGetValue(cue.Position, out float time) || cue.Start + startOffset < time)
                    Impacts[cue.Position] = cue.Start + startOffset;
            }
        }
        public void SetImpact(GridPosition position, float time) => Impacts[position] = time;
        internal float Impact(GridPosition position) => Impacts.TryGetValue(position, out float time) ? time : .16f;

        // Some result routes (notably the standalone fire engine) have no CombatEffectExecution.
        // Fill only feedback absent from explicit results; retain explicit per-hit amounts when present.
        internal void AddUnpublishedChanges(CombatState state)
        {
            foreach (UnitState after in state.Units.Values)
            {
                if (!Units.TryGetValue(after.Id, out CombatUnitDisplaySnapshot snapshot)) continue;
                UnitState before = snapshot.Unit;
                AddMissing(after, CombatFeedbackKind.Damage, before.Health - after.Health);
                AddMissing(after, CombatFeedbackKind.Healing, after.Health - before.Health);
                if (!Feedback.Any(f => Matches(f, after) && (f.Kind == CombatFeedbackKind.ShieldConsumed || f.Kind == CombatFeedbackKind.ShieldTransferredOut)))
                    AddMissing(after, CombatFeedbackKind.ShieldAbsorb, before.Shield - after.Shield);
                if (!Feedback.Any(f => Matches(f, after) && f.Kind == CombatFeedbackKind.ShieldTransferredIn))
                    AddMissing(after, CombatFeedbackKind.ShieldRestore, after.Shield - before.Shield);
                AddMissing(after, CombatFeedbackKind.ManaRestore, after.Mana - before.Mana);
                if (before.IsAlive && !after.IsAlive) AddMissing(after, CombatFeedbackKind.UnitDefeated, 1);
                foreach (var status in after.Statuses)
                    if (status.Value > before.StatusDuration(status.Key))
                        AddMissing(after, CombatFeedbackCatalog.ForStatus(status.Key), 1, status.Value);
                if (CombatStatusFeedback.HasRemoval(before.Statuses, after.Statuses)) AddMissing(after, CombatFeedbackKind.StatusCleared, 1);
                // Position and action costs are already settled. Only visible results wait for contact.
                snapshot.AlignResolvedAction(after);
            }
            foreach (var entry in Tiles)
            {
                TileState after = state.Map.GetTile(entry.Key);
                if (after.Durability >= entry.Value.Durability) continue;
                CombatFeedbackKind kind = after.IsDestroyed ? CombatFeedbackKind.DestructibleDestroyed : CombatFeedbackKind.DestructibleDamaged;
                if (!Feedback.Any(value => value.Kind == kind && value.Target == entry.Key))
                    Feedback.Add(new CombatFeedbackEvent(kind, Source, entry.Key));
            }
        }
        private void AddMissing(UnitState unit, CombatFeedbackKind kind, int amount, int duration = 0)
        {
            if (amount > 0 && !Feedback.Any(value => value.Kind == kind && Matches(value, unit)))
                Feedback.Add(new CombatFeedbackEvent(kind, Source, unit.Position, amount, duration));
        }
        private static bool Matches(CombatFeedbackEvent feedback, UnitState unit) =>
            feedback.TargetUnitId != null ? feedback.TargetUnitId == unit.Id : feedback.Target == unit.Position;
    }

    /// <summary>Short-lived display snapshots and callbacks. Combat has already resolved when this clock starts.</summary>
    public sealed class CombatActionPlayback
    {
        private sealed class Pending { public float At; public Action Show; public bool Shown; public int Order; }
        private readonly List<Pending> pending = new List<Pending>();
        private readonly Dictionary<GridPosition, float> contactTimes = new Dictionary<GridPosition, float>();
        private CombatActionCapture active;
        private float startedAt, finishesAt;
        public int Version { get; private set; }
        public float? DispatchTime { get; private set; }
        public bool IsPlaying(float now) => active != null && now < finishesAt;
        public CombatActionCapture Capture(CombatState state, FireBattleState fire, string actorId, float movementDelay) =>
            new CombatActionCapture(state, fire, actorId, Math.Max(0f, movementDelay));

        public void Start(CombatActionCapture capture, CombatState resolvedState, float now, Action<CombatFeedbackEvent> publish)
        {
            if (active != null) throw new InvalidOperationException("Finish the previous action presentation first.");
            capture.AddUnpublishedChanges(resolvedState);
            active = capture; startedAt = now + capture.Delay;
            // Persist the float once: callbacks and display snapshots must use the identical boundary,
            // including on Mono backends that retain extra precision in intermediate expressions.
            foreach (GridPosition position in capture.Tiles.Keys) contactTimes[position] = startedAt + capture.Impact(position);
            finishesAt = startedAt + Math.Max(capture.Duration, capture.Feedback.Count == 0 ? 0f : capture.Feedback.Max(f => capture.Impact(f.Target)) + .42f);
            foreach (var delivery in capture.Deliveries) pending.Add(new Pending { At = startedAt + delivery.Start, Show = delivery.Show, Order = pending.Count });
            foreach (CombatFeedbackEvent value in capture.Feedback)
            { var feedback = value; pending.Add(new Pending { At = ContactAt(value.Target), Show = () => publish(feedback), Order = pending.Count }); }
            pending.Sort((a, b) => a.At != b.At ? a.At.CompareTo(b.At) : a.Order.CompareTo(b.Order)); Version++;
        }

        public void Advance(float now, bool animationsEnabled = true)
        {
            if (active == null) return;
            foreach (Pending item in pending)
                if (!item.Shown && (!animationsEnabled || now >= item.At))
                {
                    item.Shown = true; DispatchTime = animationsEnabled ? item.At : (float?)null;
                    try { item.Show(); } finally { DispatchTime = null; }
                    Version++;
                }
            if (!animationsEnabled || now >= finishesAt) Clear();
        }

        public UnitState Unit(UnitState resolved, float now)
        {
            if (resolved != null && active != null && now < ContactAt(resolved.Position) &&
                active.Units.TryGetValue(resolved.Id, out CombatUnitDisplaySnapshot before)) return before.Unit;
            return resolved;
        }
        public TileState Tile(GridPosition position, TileState resolved, float now) =>
            active != null && now < ContactAt(position) && active.Tiles.TryGetValue(position, out TileState before) ? before : resolved;
        public bool Fireground(GridPosition position, bool resolved, float now) =>
            active != null && now < ContactAt(position) && active.Fireground.TryGetValue(position, out bool before) ? before : resolved;
        private float ContactAt(GridPosition position) => contactTimes.TryGetValue(position, out float time) ? time : startedAt;
        public void Clear() { active = null; pending.Clear(); contactTimes.Clear(); Version++; }
    }
}
