using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    /// <summary>学院敌人每场一次的公开成长意图。技能数值来自敌人技能数据表的 SK-GROW 行。</summary>
    public sealed class AcademyEnemyGrowthRuntime
    {
        public const int CommandSkillIndex = 2;

        private static readonly IReadOnlyDictionary<string, SkillDefinition> skills = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal)
        {
            ["raider"] = Growth("SK-GROW-RAIDER", "侧锋蓄势", StatusType.Strength, 1),
            ["shieldguard"] = Growth("SK-GROW-SHIELDGUARD", "整盾成长", StatusType.Strength, 1, StatusType.ShieldEfficiency, 1, 2),
            ["pyromancer"] = Growth("SK-GROW-PYROMANCER", "火矢蓄热", StatusType.SpellPower, 1),
            ["tether_hound"] = Growth("SK-GROW-HOUND", "猎意增长", StatusType.Strength, 1, StatusType.Agility, 1),
            ["sigil_mauler"] = Growth("SK-GROW-DUMMY", "重锤蓄势", StatusType.Strength, 1),
            ["barrier_mender"] = Growth("SK-GROW-MENDER", "护障蓄能", StatusType.ShieldGrant, 1),
            ["rune_arbalist"] = Growth("SK-GROW-ARBALEST", "绞盘上紧", StatusType.Strength, 2),
            ["stone_snare"] = Growth("SK-GROW-SNARE", "石索蓄能", StatusType.Control, 1),
            ["lantern_revealer"] = Growth("SK-GROW-REVEALER", "灯芯增亮", StatusType.SpellPower, 1),
            ["signal_keeper"] = Growth("SK-GROW-SIGNAL", "灯台升压", StatusType.SpellPower, 1),
            ["elite_vanguard"] = Growth("SK-GROW-VANGUARD", "阵列加固", StatusType.Strength, 2, StatusType.ShieldEfficiency, 1, 4),
            ["prototype_hand"] = Growth("SK-GROW-PROTOTYPE", "过载校准", StatusType.SpellPower, 2),
            ["elder_tracker_hound"] = Growth("SK-GROW-ELDER", "猎群本能", StatusType.Strength, 2, StatusType.Agility, 1),
            ["breach_ram"] = Growth("SK-GROW-RAM", "冲压蓄能", StatusType.Strength, 2),
            ["wind_librarian"] = Growth("SK-GROW-WIND", "风场增强", StatusType.SpellPower, 2)
        };

        private static readonly HashSet<string> ordinaryArchetypes = new HashSet<string>(StringComparer.Ordinal)
        { "raider", "shieldguard", "pyromancer", "tether_hound", "sigil_mauler", "barrier_mender", "rune_arbalist" };

        private readonly HashSet<string> usedUnitIds = new HashSet<string>(StringComparer.Ordinal);

        public static IReadOnlyCollection<SkillDefinition> All => skills.Values.ToArray();
        public static SkillDefinition For(string archetypeId) => archetypeId != null && skills.TryGetValue(archetypeId, out SkillDefinition skill) ? skill : null;
        public bool HasUsed(string unitId) => usedUnitIds.Contains(unitId);

        public CombatCommand ChooseOrdinaryFallback(UnitState enemy, CombatCommand fallback)
        {
            if (enemy == null || !ordinaryArchetypes.Contains(enemy.EnemyArchetypeId) ||
                fallback.Type != CombatCommandType.Move && fallback.Type != CombatCommandType.EndTurn ||
                usedUnitIds.Contains(enemy.Id) || For(enemy.EnemyArchetypeId) == null)
                return fallback;
            return CombatCommand.UseSkill(enemy.Id, CommandSkillIndex, enemy.Id);
        }

        /// <summary>专项敌人先展示其岗位动作；从第二个自身回合起，可把本场一次的成长作为下一条专属意图。</summary>
        public CombatCommand Choose(CombatState state, UnitState enemy, CombatCommand fallback)
        {
            if (state == null || enemy == null || enemy.IsHero || !enemy.IsAlive ||
                For(enemy.EnemyArchetypeId) == null || usedUnitIds.Contains(enemy.Id)) return fallback;
            if (enemy.EnemyArchetypeId == "rune_arbalist" && fallback.Type == CombatCommandType.Move)
                return fallback;
            if (ordinaryArchetypes.Contains(enemy.EnemyArchetypeId))
                return ChooseOrdinaryFallback(enemy, fallback);
            if (fallback.Type == CombatCommandType.UseSkill &&
                (fallback.SlotIndex == AcademyEnemyAreaRuntime.PrepareSkillIndex ||
                    fallback.SlotIndex == AcademyEnemyAreaRuntime.ResolveSkillIndex)) return fallback;
            if (enemy.EnemyArchetypeId == "barrier_mender" && fallback.Type == CombatCommandType.UseSkill &&
                fallback.SlotIndex == 0 && state.AcademyFieldEnemy?.IsPriorityMend(enemy.Id, fallback.TargetUnitId) == true)
                return fallback;
            if (enemy.EnemyArchetypeId == "elite_vanguard" && fallback.Type == CombatCommandType.UseSkill &&
                fallback.SlotIndex == AcademyFieldEnemyRuntime.VanguardDismantleSkillIndex)
                return fallback;
            if (fallback.Type != CombatCommandType.Move && fallback.Type != CombatCommandType.EndTurn &&
                state.OwnTurnCount(enemy.Id) < 2) return fallback;
            return CombatCommand.UseSkill(enemy.Id, CommandSkillIndex, enemy.Id);
        }

        public CombatEffectExecution Resolve(CombatState state, UnitState enemy, CombatCommand command)
        {
            SkillDefinition skill = For(enemy?.EnemyArchetypeId);
            if (state == null || enemy == null || !enemy.IsAlive || enemy.IsHero ||
                command.SlotIndex != CommandSkillIndex || command.UnitId != enemy.Id || skill == null ||
                usedUnitIds.Contains(enemy.Id))
                throw new InvalidOperationException("This academy growth intent is unavailable.");
            CombatEffectExecution result = CombatResolver.ResolveConfiguredEnemySkill(state, enemy, skill, command);
            usedUnitIds.Add(enemy.Id);
            return result;
        }

        public EnemyIntentPresentation PresentIntent(UnitState enemy)
        {
            SkillDefinition skill = For(enemy?.EnemyArchetypeId);
            if (skill == null) return null;
            string result = string.Join("、", skill.Effects.Where(effect => effect.Type == SkillEffectType.ApplyStatus)
                .Select(effect => AttributeLabel(effect.Status) + "+" + effect.Amount));
            SkillEffectDefinition? shield = skill.Effects.Where(effect => effect.Type == SkillEffectType.RestoreShield)
                .Select(effect => (SkillEffectDefinition?)effect).FirstOrDefault();
            if (shield.HasValue) result += "；立即获得 " + shield.Value.Amount + " 护盾";
            return new EnemyIntentPresentation(skill.Id + ":" + enemy.Id, skill.DisplayName, "自身",
                result + "；持续至本场战斗结束；每场一次，不造成伤害。", "defend", false, default, 0,
                affectedCells: new[] { enemy.Position });
        }

        public AcademyEnemyGrowthRuntime Clone()
        {
            AcademyEnemyGrowthRuntime copy = new AcademyEnemyGrowthRuntime();
            foreach (string unitId in usedUnitIds) copy.usedUnitIds.Add(unitId);
            return copy;
        }

        private static SkillDefinition Growth(string id, string name, StatusType first, int firstAmount,
            StatusType? second = null, int secondAmount = 0, int shield = 0)
        {
            List<SkillEffectDefinition> effects = new List<SkillEffectDefinition>();
            if (shield > 0) effects.Add(SkillEffectDefinition.RestoreShield(shield));
            effects.Add(SkillEffectDefinition.ApplyStatus(first, int.MaxValue, firstAmount));
            if (second.HasValue) effects.Add(SkillEffectDefinition.ApplyStatus(second.Value, int.MaxValue, secondAmount));
            return new SkillDefinition(id, name, SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 0, 0,
                CombatFeedbackKind.Attribute, effects);
        }

        private static string AttributeLabel(StatusType status)
        {
            switch (status)
            {
                case StatusType.Strength: return "力量";
                case StatusType.SpellPower: return "法强";
                case StatusType.Agility: return "敏捷";
                case StatusType.ShieldEfficiency: return "护盾效能";
                case StatusType.ShieldGrant: return "护盾授予";
                case StatusType.Control: return "控制";
                default: return status.ToString();
            }
        }
    }
}
