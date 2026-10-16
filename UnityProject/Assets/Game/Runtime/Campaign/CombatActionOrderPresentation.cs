using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public static class CombatActionOrderPresentation
    {
        // First future appearance of each living unit at current speed, with the active unit first.
        // Future status expiry and unsubmitted enemy decisions are deliberately not simulated.
        public static IReadOnlyList<UnitState> Build(CombatState state, CombatActionPreview preview = null)
        {
            if (state == null) return Array.Empty<UnitState>();
            bool projected = preview != null && preview.CanSubmit && preview.HasActionValuePreview;
            Func<UnitState, int> value = unit => projected && unit.Id == preview.ActionValueTargetId
                ? CombatActionTimeline.PreviewDelayedValue(unit, preview.ActionValueDelay) : unit.ActionValue;
            Func<UnitState, int> ticks = unit => value(unit) >= CombatActionTimeline.ReadyThreshold ? 0 :
                CombatActionTimeline.TicksUntilReady(value(unit), unit.EffectiveSpeed);
            return state.Units.Values.Where(unit => unit.IsAlive)
                .OrderByDescending(unit => unit.Id == state.ActiveUnitId)
                .ThenBy(ticks)
                .ThenByDescending(unit => CombatActionTimeline.Clamp(value(unit) + ticks(unit) * unit.EffectiveSpeed))
                .ThenByDescending(unit => unit.EffectiveSpeed)
                .ThenBy(unit => state.FixedTurnOrder(unit.Id)).ToArray();
        }
    }
}
