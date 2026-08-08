using System.Collections.Generic;

namespace OCC.Combat
{
    public static class CombatStatusLifecycle
    {
        public const int BurningDamagePerTurn = 2;

        private static readonly StatusType[] order =
        {
            StatusType.Burning,
            StatusType.Slow,
            StatusType.Bound,
            StatusType.ArmorBreak
        };

        public static CombatEffectExecution ResolveTurnStart(CombatState state, UnitState unit)
        {
            List<CombatEffect> effects = new List<CombatEffect>();
            foreach (StatusType status in order)
            {
                if (!unit.HasStatus(status)) continue;
                effects.Add(CombatEffect.TriggerStatus(unit.Id, status));
                if (status == StatusType.Burning)
                    effects.Add(CombatEffect.DamageHealth(unit.Id, BurningDamagePerTurn));
                effects.Add(CombatEffect.ReduceStatusDuration(unit.Id, status));
            }

            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, effects.ToArray());
            unit.TickCooldowns();
            return execution;
        }
    }
}
