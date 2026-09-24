using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public static class CombatStatusLifecycle
    {
        /// <summary>燃烧每回合伤害。按数据表口径：4 点，在目标自身回合结束时结算。</summary>
        public const int BurningDamagePerTurn = 4;

        private static readonly StatusType[] order =
        {
            StatusType.Burning,
            StatusType.Bound,
            StatusType.FiregroundBoost,
            StatusType.FiregroundVulnerable
        };

        public static CombatEffectExecution ResolveTurnStart(CombatState state, UnitState unit)
        {
            List<CombatEffect> effects = new List<CombatEffect>();
            foreach (StatusType status in order)
            {
                if (!unit.HasStatus(status)) continue;
                if (status == StatusType.Burning) continue;
                effects.Add(CombatEffect.TriggerStatus(unit.Id, status));
                effects.Add(CombatEffect.ReduceStatusDuration(unit.Id, status));
            }

            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, effects.ToArray());
            unit.TickCooldowns();
            return execution;
        }

        /// <summary>自身回合结束：燃烧按实例强度结算火焰伤害并扣一次持续量。</summary>
        public static CombatEffectExecution ResolveTurnEnd(CombatState state, UnitState unit)
        {
            List<CombatEffect> effects = new List<CombatEffect>();
            if (unit.HasStatus(StatusType.Burning))
            {
                effects.Add(CombatEffect.TriggerStatus(unit.Id, StatusType.Burning));
                effects.Add(CombatEffect.DamageHealth(unit.Id, Math.Max(0,
                    unit.StatusStrength(StatusType.Burning, BurningDamagePerTurn) + unit.StatusStrength(StatusType.DamageTaken))));
                effects.Add(CombatEffect.ReduceStatusDuration(unit.Id, StatusType.Burning));
            }
            foreach (StatusType status in new[] { StatusType.Agility, StatusType.Strength, StatusType.SpellPower,
                StatusType.Speed, StatusType.Range, StatusType.DamageTaken, StatusType.ShieldEfficiency,
                StatusType.ShieldGrant, StatusType.Control, StatusType.Marked, StatusType.Prepared,
                StatusType.Invulnerable })
                if (unit.HasStatus(status))
                    effects.Add(CombatEffect.ReduceStatusDuration(unit.Id, status));
            return effects.Count == 0 ? CombatEffectExecution.Empty : CombatEffectExecutor.Execute(state, unit.Id, effects.ToArray());
        }
    }
}
