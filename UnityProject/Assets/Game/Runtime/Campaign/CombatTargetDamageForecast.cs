using System;
using System.Linq;

namespace OCC.Combat
{
    public sealed class CombatTargetDamageForecast
    {
        public int ShieldLoss { get; }
        public int HealthLoss { get; }
        public int RemainingShield { get; }
        public int RemainingHealth { get; }
        public int EnvironmentDamage { get; }
        public bool WillDefeat { get; }
        public string DefeatSummary { get; }
        public int TotalDamage => ShieldLoss + HealthLoss;

        public string PlayerSummary
        {
            get
            {
                if (TotalDamage <= 0) return "不会造成伤害 · 生命剩余 " + RemainingHealth;
                string result = WillDefeat
                    ? DefeatSummary + " · 生命归零"
                    : "生命 -" + HealthLoss + "（剩 " + RemainingHealth + "）";
                if (ShieldLoss > 0) result += " · 护盾 -" + ShieldLoss;
                if (EnvironmentDamage > 0) result += " · 含环境伤害 " + EnvironmentDamage;
                return result;
            }
        }

        internal CombatTargetDamageForecast(int shieldLoss, int healthLoss, int remainingShield, int remainingHealth,
            int environmentDamage, string defeatSummary)
        {
            ShieldLoss = Math.Max(0, shieldLoss);
            HealthLoss = Math.Max(0, healthLoss);
            RemainingShield = Math.Max(0, remainingShield);
            RemainingHealth = Math.Max(0, remainingHealth);
            EnvironmentDamage = Math.Max(0, Math.Min(TotalDamage, environmentDamage));
            WillDefeat = RemainingHealth <= 0 && HealthLoss > 0;
            DefeatSummary = string.IsNullOrWhiteSpace(defeatSummary) ? "可使目标失去行动能力" : defeatSummary;
        }
    }

    // Read-only authoritative forecast: execute the real resolver on cloned state, then compare target vitals.
    public static class CombatTargetDamageForecaster
    {
        public static CombatTargetDamageForecast WeaponAttack(FireBattleState liveBattle, string attackerId, string targetId)
        {
            if (liveBattle == null) throw new ArgumentNullException(nameof(liveBattle));
            UnitState before = RequiredTarget(liveBattle.Combat, targetId);

            FireBattleState full = liveBattle.Clone();
            FireSpellEngine.ResolveWeaponAttack(full, attackerId, targetId);
            UnitState after = RequiredTarget(full.Combat, targetId);

            FireBattleState neutral = WithoutFiregrounds(liveBattle);
            FireSpellEngine.ResolveWeaponAttack(neutral, attackerId, targetId);
            UnitState neutralAfter = RequiredTarget(neutral.Combat, targetId);
            return Build(before, after, neutralAfter);
        }

        public static CombatTargetDamageForecast Skill(CombatState liveState, CombatCommand command, string targetId)
        {
            if (liveState == null) throw new ArgumentNullException(nameof(liveState));
            UnitState before = RequiredTarget(liveState, targetId);
            CombatState clone = liveState.Clone();
            CombatResolver.Resolve(clone, command);
            UnitState after = RequiredTarget(clone, targetId);
            return Build(before, after, after);
        }

        public static CombatTargetDamageForecast FireSpell(FireBattleState liveBattle, string sourceId,
            FireSpellDefinition spell, FireSpellTarget target, string targetId)
        {
            if (liveBattle == null) throw new ArgumentNullException(nameof(liveBattle));
            if (spell == null) throw new ArgumentNullException(nameof(spell));
            UnitState before = RequiredTarget(liveBattle.Combat, targetId);

            FireBattleState full = liveBattle.Clone();
            FireSpellEngine.Execute(full, sourceId, spell, target);
            UnitState after = RequiredTarget(full.Combat, targetId);

            UnitState neutralAfter = before;
            try
            {
                FireBattleState neutral = WithoutFiregrounds(liveBattle);
                FireSpellEngine.Execute(neutral, sourceId, spell, target);
                neutralAfter = RequiredTarget(neutral.Combat, targetId);
            }
            catch (InvalidOperationException)
            {
                // If removing the fireground makes the cast illegal, all applied damage depends on the environment.
            }
            return Build(before, after, neutralAfter);
        }

        private static CombatTargetDamageForecast Build(UnitState before, UnitState after, UnitState neutralAfter)
        {
            int shieldLoss = Math.Max(0, before.Shield - after.Shield);
            int healthLoss = Math.Max(0, before.Health - after.Health);
            int neutralTotal = Math.Max(0, before.Shield - neutralAfter.Shield) +
                               Math.Max(0, before.Health - neutralAfter.Health);
            int environment = Math.Max(0, shieldLoss + healthLoss - neutralTotal);
            return new CombatTargetDamageForecast(shieldLoss, healthLoss, after.Shield, after.Health, environment,
                EnemyResolutionSemantics.Forecast(before));
        }

        private static FireBattleState WithoutFiregrounds(FireBattleState liveBattle)
        {
            FireBattleState clone = liveBattle.Clone();
            foreach (GridPosition position in clone.Firegrounds.Keys.ToArray()) clone.RemoveFireground(position);
            return clone;
        }

        private static UnitState RequiredTarget(CombatState state, string targetId) =>
            state.GetUnit(targetId) ?? throw new InvalidOperationException("Damage preview target does not exist.");
    }
}
