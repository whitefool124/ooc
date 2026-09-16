using System;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>
    /// Deterministic first-stage boss rules. The core's pressure is supplied by
    /// visible living maintenance units, so target order rewrites both defense
    /// and the boss action instead of merely shortening a large health bar.
    /// </summary>
    public sealed class AcademyCoreBossRuntime
    {
        private const int MaintenanceShieldPerUnit = 2;

        public int LivingMaintenanceCount(CombatState state) => state.Units.Values.Count(unit =>
            unit.IsAlive && !unit.IsHero && unit.EnemyArchetypeId != "core_overseer");

        public int PhaseFor(CombatState state, UnitState core)
        {
            if (core == null) return 1;
            return core.Health * 2 <= core.MaxHealth || LivingMaintenanceCount(state) <= 1 ? 2 : 1;
        }

        public CombatCommand ChooseEnemyCommand(CombatState state, UnitState enemy, UnitState hero)
        {
            if (enemy == null || hero == null) return CombatCommand.EndTurn(enemy?.Id ?? string.Empty);
            if (enemy.EnemyArchetypeId != "core_overseer") return EnemyTactics.Choose(state, enemy, hero);

            int phase = PhaseFor(state, enemy);
            int slot = phase == 1 ? 0 : 1;
            SkillDefinition skill = slot == 0 ? enemy.SkillOne : enemy.SkillTwo;
            int distance = enemy.Position.ManhattanDistance(hero.Position);
            if (skill != null && distance <= skill.Range && enemy.Mana >= skill.ManaCost && enemy.IsSkillReady(skill) &&
                (skill.Range <= 1 || skill.HasModifier(SkillModifierType.IgnoreLineOfSight) || state.HasLineOfSight(enemy.Position, hero.Position)))
                return CombatCommand.UseSkill(enemy.Id, slot, hero.Id);
            if (distance <= (enemy.MainHand?.Range ?? 1)) return CombatCommand.Attack(enemy.Id, hero.Id);

            GridPosition step = new GridPosition(enemy.Position.X + Math.Sign(hero.Position.X - enemy.Position.X), enemy.Position.Y);
            if (step == enemy.Position) step = new GridPosition(enemy.Position.X, enemy.Position.Y + Math.Sign(hero.Position.Y - enemy.Position.Y));
            return CombatCommand.Move(enemy.Id, step);
        }

        public EnemyIntentPresentation PresentIntent(CombatState state, UnitState enemy, CombatCommand command)
        {
            EnemyIntentPresentation basic = CombatInformationPresenter.BuildEnemyIntent(state, enemy, command);
            if (enemy?.EnemyArchetypeId != "core_overseer") return basic;
            int links = LivingMaintenanceCount(state);
            int phase = PhaseFor(state, enemy);
            string phaseRule = phase == 1
                ? "阶段一：使用四格定向束；维护单位减少至一名或核心半血后切换。"
                : "阶段二：改用两格破势脉冲，伤害更高但作用距离更短。";
            string maintenance = links == 0
                ? "维护链已全部切断，本回合开始不再获得维护护盾。"
                : "仍有 " + links + " 条维护链，本回合开始获得 " + (links * MaintenanceShieldPerUnit) + " 护盾。";
            return new EnemyIntentPresentation("academy-core:p" + phase + ":links" + links + ":" + basic.Signature,
                basic.ActionName, basic.TargetSummary, phaseRule + " " + maintenance + " " + basic.ResultSummary,
                basic.IconId, basic.HasDestination, basic.Destination, basic.ExpectedDamage);
        }

        internal void BeginTurn(CombatState state, UnitState unit)
        {
            if (unit?.EnemyArchetypeId != "core_overseer") return;
            int amount = LivingMaintenanceCount(state) * MaintenanceShieldPerUnit;
            if (amount > 0) state.TryGrantRogueliteShield(unit.Id, "academy-core-maintenance", amount);
        }

        public AcademyCoreBossRuntime Clone() => new AcademyCoreBossRuntime();
    }
}
