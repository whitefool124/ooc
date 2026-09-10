using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public static class CombatActionTimeline
    {
        public const int ReadyThreshold = 100;
        public const int MaximumValue = 199;

        public static int Clamp(int value) => Math.Max(0, Math.Min(MaximumValue, value));

        public static int TicksUntilReady(int actionValue, int effectiveSpeed)
        {
            int remaining = Math.Max(0, ReadyThreshold - Clamp(actionValue));
            int speed = Math.Max(1, effectiveSpeed);
            return Math.Max(1, (remaining + speed - 1) / speed);
        }

        public static IOrderedEnumerable<UnitState> Order(CombatState state, IEnumerable<UnitState> units)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return (units ?? Enumerable.Empty<UnitState>())
                .Where(unit => unit != null && unit.IsAlive)
                .OrderByDescending(unit => unit.Id == state.ActiveUnitId)
                .ThenByDescending(unit => unit.ActionValue)
                .ThenByDescending(unit => unit.EffectiveSpeed)
                .ThenBy(unit => state.FixedTurnOrder(unit.Id));
        }

        public static int PreviewDelayedValue(UnitState unit, int amount) =>
            unit == null ? 0 : Clamp(unit.ActionValue - Math.Max(0, amount));
    }
}
