using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public static class EnemyAbilityCatalog
    {
        public static readonly WeaponDefinition TetherHoundBite = new WeaponDefinition("tether_hound_bite", "导能撕咬", DamageType.Physical, 4, 1);
        public static readonly WeaponDefinition HeavyCrossbow = new WeaponDefinition("heavy_crossbow", "绞盘重弩", DamageType.Physical, 3, 4);
        public static readonly SkillDefinition ShieldRam = new SkillDefinition(
            "enemy_shield_ram", "铭盾冲撞", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 2,
            CombatFeedbackKind.Slow,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Slow, 1) });
        public static readonly SkillDefinition HookingStrike = new SkillDefinition(
            "enemy_hooking_strike", "钩刃牵制", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 2,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(3, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 1) });
        public static readonly SkillDefinition VanguardCrush = new SkillDefinition(
            "enemy_vanguard_crush", "先锋压阵", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 2, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(4, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition SunderingSigil = new SkillDefinition(
            "enemy_sundering_sigil", "碎甲锤印", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition WardMend = new SkillDefinition(
            "enemy_ward_mend", "护障续接", SkillTargetRule.AllyUnit, SkillDeliveryMethod.Direct, 4, 2, 2,
            CombatFeedbackKind.ShieldRestore,
            new[] { SkillEffectDefinition.RestoreShield(4) });
        public static readonly SkillDefinition TetherPounce = new SkillDefinition(
            "enemy_tether_pounce", "缚环扑咬", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 1,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 1) });
        public static readonly SkillDefinition StoneSnare = new SkillDefinition(
            "enemy_stone_snare", "石索锁步", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 3, 2, 2,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(1, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 2) });
        public static readonly SkillDefinition RevealingLantern = new SkillDefinition(
            "enemy_revealing_lantern", "显影灯照", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 3, 1, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(1, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition WindlassBolt = new SkillDefinition(
            "enemy_windlass_bolt", "绞盘重矢", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 5, 1, 2,
            CombatFeedbackKind.Damage,
            new[] { SkillEffectDefinition.Damage(5, DamageType.Physical) });

        public static readonly IReadOnlyList<SkillDefinition> All = new[]
        {
            ShieldRam, CombatCatalog.FireBolt, HookingStrike, VanguardCrush,
            SunderingSigil, WardMend, TetherPounce, StoneSnare, RevealingLantern, WindlassBolt
        };
        public static SkillDefinition Get(string id) => All.Single(skill => skill.Id == id);
    }
}
