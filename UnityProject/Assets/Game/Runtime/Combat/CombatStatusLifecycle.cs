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
            StatusType.Slow,
            StatusType.Bound,
            StatusType.ArmorBreak,
            StatusType.FiregroundBoost,
            StatusType.FiregroundVulnerable
        };

        public static CombatEffectExecution ResolveTurnStart(CombatState state, UnitState unit)
        {
            List<CombatEffect> effects = new List<CombatEffect>();
            foreach (StatusType status in order)
            {
                if (!unit.HasStatus(status)) continue;
                effects.Add(CombatEffect.TriggerStatus(unit.Id, status));
                // 燃烧的伤害与持续量都在自身回合结束结算，回合开始只做公开触发标记。
                if (status != StatusType.Burning) effects.Add(CombatEffect.ReduceStatusDuration(unit.Id, status));
            }

            CombatEffectExecution execution = CombatEffectExecutor.Execute(state, unit.Id, effects.ToArray());
            unit.TickCooldowns();
            return execution;
        }

        /// <summary>自身回合结束：燃烧结算固定火焰伤害并扣一次持续量。</summary>
        public static CombatEffectExecution ResolveTurnEnd(CombatState state, UnitState unit)
        {
            List<CombatEffect> effects = new List<CombatEffect>();
            if (unit.HasStatus(StatusType.Burning))
            {
                effects.Add(CombatEffect.TriggerStatus(unit.Id, StatusType.Burning));
                effects.Add(CombatEffect.DamageHealth(unit.Id, BurningDamagePerTurn));
                effects.Add(CombatEffect.ReduceStatusDuration(unit.Id, StatusType.Burning));
            }
            return effects.Count == 0 ? CombatEffectExecution.Empty : CombatEffectExecutor.Execute(state, unit.Id, effects.ToArray());
        }
    }
}
