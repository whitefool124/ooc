using System;

namespace OCC.Combat
{
    /// <summary>Owns a detached copy for display. Alignment never writes into the resolved actor.</summary>
    public sealed class CombatUnitDisplaySnapshot
    {
        public UnitState Unit { get; }
        public CombatUnitDisplaySnapshot(UnitState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Unit = source.Clone();
        }

        public void AlignResolvedAction(UnitState resolved)
        {
            if (resolved == null || resolved.Id != Unit.Id) throw new ArgumentException("Snapshot actor mismatch.", nameof(resolved));
            Unit.MoveTo(resolved.Position);
            Unit.BeginTurn(resolved.ActionPoints);
            Unit.SetMovementRangeForTurn(resolved.MovementRangeThisTurn);
            Unit.LimitMovementRangeForTurn(resolved.MovementRangeThisTurn);
            if (resolved.Mana < Unit.Mana) Unit.SpendMana(Unit.Mana - resolved.Mana);
        }
    }
}
