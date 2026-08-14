using System;
using System.Linq;

namespace OCC.Combat.Presentation
{
    public readonly struct CombatTargetForecastResult
    {
        public CombatTargetDamageForecast Forecast { get; }
        public FireBattleState FireBattle { get; }

        public CombatTargetForecastResult(CombatTargetDamageForecast forecast, FireBattleState fireBattle)
        {
            Forecast = forecast;
            FireBattle = fireBattle;
        }
    }

    public sealed class CombatTargetForecastService
    {
        public CombatTargetForecastResult Evaluate(BattlefieldPresentationAdapter battlefield,
            CombatState state, FireBattleState fireBattle, string action, UnitState enemy,
            FireSpellDefinition fireSpell, bool artifactArmed)
        {
            if (enemy == null || enemy.IsHero || !enemy.IsAlive || state == null ||
                state.ActiveUnitId != "hero") return new CombatTargetForecastResult(null, fireBattle);
            try
            {
                if (action == "攻击")
                {
                    if (!string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state, action, enemy.Position)))
                        return new CombatTargetForecastResult(null, fireBattle);
                    fireBattle = EnsureFireBattle(state, fireBattle);
                    return new CombatTargetForecastResult(
                        CombatTargetDamageForecaster.WeaponAttack(fireBattle, "hero", enemy.Id), fireBattle);
                }

                int slot = action == "技能1" ? 0 : action == "技能2" ? 1 : -1;
                if (slot < 0) return new CombatTargetForecastResult(null, fireBattle);
                if (fireSpell != null)
                {
                    fireBattle = EnsureFireBattle(state, fireBattle);
                    Facing facing = BattlefieldPresentationAdapter.FacingToward(
                        state.GetUnit("hero").Position, enemy.Position);
                    FireSpellTarget target = FireSpellTarget.Unit(enemy.Id, facing);
                    FireSpellPreview preview = FireSpellEngine.Preview(fireBattle, "hero", fireSpell, target);
                    bool canDamage = fireSpell.Rules.Any(rule => rule.Kind == FireRuleKind.Damage ||
                        rule.Kind == FireRuleKind.WeaponDamage || rule.Kind == FireRuleKind.Push);
                    CombatTargetDamageForecast forecast = preview.CanCommit && canDamage
                        ? CombatTargetDamageForecaster.FireSpell(fireBattle, "hero", fireSpell, target, enemy.Id)
                        : null;
                    return new CombatTargetForecastResult(forecast, fireBattle);
                }

                if (slot == 0 && artifactArmed) return new CombatTargetForecastResult(null, fireBattle);
                UnitState hero = state.GetUnit("hero");
                SkillDefinition skill = slot == 0 ? hero.SkillOne : hero.SkillTwo;
                if (skill == null || skill.Damage <= 0 ||
                    !string.IsNullOrEmpty(battlefield.InvalidReasonForCell(state, action, enemy.Position)))
                    return new CombatTargetForecastResult(null, fireBattle);
                return new CombatTargetForecastResult(CombatTargetDamageForecaster.Skill(state,
                    CombatCommand.UseSkill("hero", slot, enemy.Id), enemy.Id), fireBattle);
            }
            catch (InvalidOperationException)
            {
                return new CombatTargetForecastResult(null, fireBattle);
            }
        }

        private static FireBattleState EnsureFireBattle(CombatState state, FireBattleState fireBattle) =>
            fireBattle == null || fireBattle.Combat != state ? new FireBattleState(state) : fireBattle;
    }
}
