using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public enum CombatEffectKind
    {
        SpendActionPoints,
        SpendMana,
        AbsorbShield,
        DamageHealth,
        RestoreHealth,
        RestoreShield,
        RestoreMana,
        ApplyStatus,
        ClearStatus,
        TriggerStatus,
        ReduceStatusDuration,
        Move,
        DamageObject,
        DelayInitiative
    }

    public enum CombatStatusLifecyclePhase
    {
        None,
        Applied,
        Refreshed,
        Preserved,
        Triggered,
        DurationReduced,
        Expired,
        Cleared
    }

    public readonly struct CombatEffect
    {
        public CombatEffectKind Kind { get; }
        public string TargetUnitId { get; }
        public int Amount { get; }
        public StatusType Status { get; }
        public int Duration { get; }
        public GridPosition Destination { get; }
        public Facing Facing { get; }

        private CombatEffect(CombatEffectKind kind, string targetUnitId, int amount, StatusType status, int duration, GridPosition destination, Facing facing)
        {
            Kind = kind;
            TargetUnitId = targetUnitId;
            Amount = amount;
            Status = status;
            Duration = duration;
            Destination = destination;
            Facing = facing;
        }

        public static CombatEffect SpendActionPoints(int amount) => new CombatEffect(CombatEffectKind.SpendActionPoints, null, amount, default, 0, default, default);
        public static CombatEffect SpendMana(int amount) => new CombatEffect(CombatEffectKind.SpendMana, null, amount, default, 0, default, default);
        public static CombatEffect AbsorbShield(string targetUnitId, int amount) => new CombatEffect(CombatEffectKind.AbsorbShield, targetUnitId, amount, default, 0, default, default);
        public static CombatEffect DamageHealth(string targetUnitId, int amount) => new CombatEffect(CombatEffectKind.DamageHealth, targetUnitId, amount, default, 0, default, default);
        public static CombatEffect RestoreHealth(string targetUnitId, int amount) => new CombatEffect(CombatEffectKind.RestoreHealth, targetUnitId, amount, default, 0, default, default);
        public static CombatEffect RestoreShield(string targetUnitId, int amount) => new CombatEffect(CombatEffectKind.RestoreShield, targetUnitId, amount, default, 0, default, default);
        public static CombatEffect RestoreMana(string targetUnitId, int amount) => new CombatEffect(CombatEffectKind.RestoreMana, targetUnitId, amount, default, 0, default, default);
        public static CombatEffect ApplyStatus(string targetUnitId, StatusType status, int duration) => new CombatEffect(CombatEffectKind.ApplyStatus, targetUnitId, 0, status, duration, default, default);
        public static CombatEffect ClearStatus(string targetUnitId, StatusType status) => new CombatEffect(CombatEffectKind.ClearStatus, targetUnitId, 0, status, 0, default, default);
        public static CombatEffect TriggerStatus(string targetUnitId, StatusType status) => new CombatEffect(CombatEffectKind.TriggerStatus, targetUnitId, 0, status, 0, default, default);
        public static CombatEffect ReduceStatusDuration(string targetUnitId, StatusType status, int amount = 1) => new CombatEffect(CombatEffectKind.ReduceStatusDuration, targetUnitId, amount, status, 0, default, default);
        public static CombatEffect Move(GridPosition destination, Facing facing) => new CombatEffect(CombatEffectKind.Move, null, 0, default, 0, destination, facing);
        public static CombatEffect DamageObject(GridPosition destination, int amount) => new CombatEffect(CombatEffectKind.DamageObject, null, amount, default, 0, destination, default);
        public static CombatEffect DelayInitiative(int amount) => new CombatEffect(CombatEffectKind.DelayInitiative, null, amount, default, 0, default, default);
    }

    public readonly struct CombatEffectResult
    {
        public int Sequence { get; }
        public CombatEffectKind Kind { get; }
        public string SourceUnitId { get; }
        public string TargetUnitId { get; }
        public int RequestedAmount { get; }
        public int AppliedAmount { get; }
        public int ValueBefore { get; }
        public int ValueAfter { get; }
        public StatusType Status { get; }
        public CombatStatusLifecyclePhase StatusPhase { get; }
        public int Duration { get; }
        public GridPosition PositionBefore { get; }
        public GridPosition PositionAfter { get; }
        public bool Changed => ValueBefore != ValueAfter || PositionBefore != PositionAfter;

        internal CombatEffectResult(int sequence, CombatEffect effect, string sourceUnitId, string targetUnitId, int appliedAmount, int valueBefore, int valueAfter, GridPosition positionBefore, GridPosition positionAfter)
        {
            Sequence = sequence;
            Kind = effect.Kind;
            SourceUnitId = sourceUnitId;
            TargetUnitId = targetUnitId;
            RequestedAmount = effect.Amount;
            AppliedAmount = appliedAmount;
            ValueBefore = valueBefore;
            ValueAfter = valueAfter;
            Status = effect.Status;
            StatusPhase = ResolveStatusPhase(effect.Kind, valueBefore, valueAfter);
            Duration = effect.Duration;
            PositionBefore = positionBefore;
            PositionAfter = positionAfter;
        }

        private static CombatStatusLifecyclePhase ResolveStatusPhase(CombatEffectKind kind, int before, int after)
        {
            if (kind == CombatEffectKind.ApplyStatus)
            {
                if (before <= 0 && after > 0) return CombatStatusLifecyclePhase.Applied;
                if (after > before) return CombatStatusLifecyclePhase.Refreshed;
                return CombatStatusLifecyclePhase.Preserved;
            }
            if (kind == CombatEffectKind.ClearStatus) return CombatStatusLifecyclePhase.Cleared;
            if (kind == CombatEffectKind.TriggerStatus) return CombatStatusLifecyclePhase.Triggered;
            if (kind == CombatEffectKind.ReduceStatusDuration) return after <= 0 ? CombatStatusLifecyclePhase.Expired : CombatStatusLifecyclePhase.DurationReduced;
            return CombatStatusLifecyclePhase.None;
        }
    }

    public sealed class CombatEffectExecution
    {
        private static readonly CombatEffectExecution empty = new CombatEffectExecution(Array.Empty<CombatEffectResult>());
        private readonly CombatEffectResult[] results;

        public static CombatEffectExecution Empty => empty;
        public IReadOnlyList<CombatEffectResult> Results => results;

        internal CombatEffectExecution(CombatEffectResult[] results) => this.results = results ?? Array.Empty<CombatEffectResult>();
    }

    public static class CombatEffectExecutor
    {
        public static CombatEffectExecution Execute(CombatState state, string sourceUnitId, params CombatEffect[] effects)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            UnitState source = state.GetUnit(sourceUnitId) ?? throw new InvalidOperationException("Unit does not exist.");
            if (effects == null || effects.Length == 0) return CombatEffectExecution.Empty;

            ValidateBatch(state, source, effects);

            CombatEffectResult[] results = new CombatEffectResult[effects.Length];
            for (int index = 0; index < effects.Length; index++)
                results[index] = Apply(state, source, effects[index], index);
            return new CombatEffectExecution(results);
        }

        private static void ValidateBatch(CombatState state, UnitState source, IReadOnlyList<CombatEffect> effects)
        {
            int remainingActionPoints = source.ActionPoints;
            int remainingMana = source.Mana;
            for (int index = 0; index < effects.Count; index++)
            {
                CombatEffect effect = effects[index];
                if (!Enum.IsDefined(typeof(CombatEffectKind), effect.Kind))
                    throw new ArgumentOutOfRangeException(nameof(effects), effect.Kind, "Unsupported combat effect.");
                if (effect.Amount < 0 || effect.Duration < 0)
                    throw new InvalidOperationException("Combat effect amounts cannot be negative.");
                if (!string.IsNullOrEmpty(effect.TargetUnitId) && state.GetUnit(effect.TargetUnitId) == null)
                    throw new InvalidOperationException("Effect target does not exist.");
                if (effect.Kind == CombatEffectKind.SpendActionPoints)
                {
                    remainingActionPoints -= effect.Amount;
                    if (remainingActionPoints < 0) throw new InvalidOperationException("Not enough action points.");
                }
                else if (effect.Kind == CombatEffectKind.SpendMana)
                {
                    remainingMana -= effect.Amount;
                    if (remainingMana < 0) throw new InvalidOperationException("Not enough mana.");
                }
                else if (effect.Kind == CombatEffectKind.Move)
                {
                    if (!state.Map.IsInside(effect.Destination) || state.Map.IsBlocked(effect.Destination) ||
                        state.IsOccupied(effect.Destination, source.Id))
                        throw new InvalidOperationException("Effect movement destination is not legal.");
                }
                else if (effect.Kind == CombatEffectKind.DamageObject && !state.Map.IsInside(effect.Destination))
                    throw new InvalidOperationException("Effect object target is outside the map.");
            }
        }

        private static CombatEffectResult Apply(CombatState state, UnitState source, CombatEffect effect, int sequence)
        {
            UnitState target = string.IsNullOrEmpty(effect.TargetUnitId) ? source : state.GetUnit(effect.TargetUnitId);
            if (target == null) throw new InvalidOperationException("Effect target does not exist.");

            int before;
            int after;
            int applied;
            GridPosition positionBefore = target.Position;
            GridPosition positionAfter = positionBefore;

            switch (effect.Kind)
            {
                case CombatEffectKind.SpendActionPoints:
                    before = source.ActionPoints;
                    source.SpendActionPoint(effect.Amount);
                    after = source.ActionPoints;
                    applied = before - after;
                    break;
                case CombatEffectKind.SpendMana:
                    before = source.Mana;
                    source.SpendMana(effect.Amount);
                    after = source.Mana;
                    applied = before - after;
                    break;
                case CombatEffectKind.AbsorbShield:
                    before = target.Shield;
                    applied = target.AbsorbShield(effect.Amount);
                    after = target.Shield;
                    break;
                case CombatEffectKind.DamageHealth:
                    before = target.Health;
                    target.TakeDamage(effect.Amount);
                    after = target.Health;
                    applied = before - after;
                    break;
                case CombatEffectKind.RestoreHealth:
                    before = target.Health;
                    target.Heal(effect.Amount);
                    after = target.Health;
                    applied = after - before;
                    break;
                case CombatEffectKind.RestoreShield:
                    before = target.Shield;
                    target.RestoreShield(effect.Amount);
                    after = target.Shield;
                    applied = after - before;
                    break;
                case CombatEffectKind.RestoreMana:
                    before = target.Mana;
                    target.RestoreMana(effect.Amount);
                    after = target.Mana;
                    applied = after - before;
                    break;
                case CombatEffectKind.ApplyStatus:
                    before = target.StatusDuration(effect.Status);
                    if (target.IsAlive) target.ApplyStatus(effect.Status, effect.Duration);
                    after = target.StatusDuration(effect.Status);
                    applied = after - before;
                    break;
                case CombatEffectKind.ClearStatus:
                    before = target.StatusDuration(effect.Status);
                    target.ClearStatus(effect.Status);
                    after = target.StatusDuration(effect.Status);
                    applied = before - after;
                    break;
                case CombatEffectKind.TriggerStatus:
                    before = target.StatusDuration(effect.Status);
                    after = before;
                    applied = 0;
                    break;
                case CombatEffectKind.ReduceStatusDuration:
                    before = target.StatusDuration(effect.Status);
                    target.ReduceStatusDuration(effect.Status, effect.Amount);
                    after = target.StatusDuration(effect.Status);
                    applied = before - after;
                    break;
                case CombatEffectKind.Move:
                    before = 0;
                    source.MoveTo(effect.Destination, effect.Facing);
                    after = 0;
                    applied = 0;
                    positionAfter = source.Position;
                    break;
                case CombatEffectKind.DamageObject:
                    TileState tile = state.Map.GetTile(effect.Destination);
                    before = tile.Durability;
                    tile.Durability = Math.Max(0, tile.Durability - effect.Amount);
                    after = tile.Durability;
                    applied = before - after;
                    positionBefore = effect.Destination;
                    positionAfter = effect.Destination;
                    break;
                case CombatEffectKind.DelayInitiative:
                    before = source.InitiativeTime;
                    source.SetInitiativeTime(source.InitiativeTime + effect.Amount);
                    after = source.InitiativeTime;
                    applied = after - before;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "Unsupported combat effect.");
            }

            return new CombatEffectResult(sequence, effect, source.Id, target.Id, applied, before, after, positionBefore, positionAfter);
        }
    }
}
