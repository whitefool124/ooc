using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public static class EnemyAbilityCatalog
    {
        public static readonly WeaponDefinition TetherHoundBite = new WeaponDefinition("tether_hound_bite", "导能撕咬", DamageType.Physical, 3, 1);
        public static readonly WeaponDefinition HeavyCrossbow = new WeaponDefinition("heavy_crossbow", "绞盘重弩", DamageType.Physical, 3, 4, minimumRange: 2);
        public static readonly WeaponDefinition BreachRam = new WeaponDefinition("breach_ram", "楔角撞击", DamageType.Physical, 4, 1);
        public static readonly SkillDefinition ShieldRam = new SkillDefinition(
            "enemy_shield_ram", "铭盾冲撞", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 2,
            CombatFeedbackKind.Slow,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Slow, 1) });
        public static readonly SkillDefinition HookingStrike = new SkillDefinition(
            "enemy_hooking_strike", "钩刃牵制", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 2,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(3, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 2) });
        public static readonly SkillDefinition VanguardCrush = new SkillDefinition(
            "enemy_vanguard_crush", "先锋压阵", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 2, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(4, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition SunderingSigil = new SkillDefinition(
            "enemy_sundering_sigil", "碎势锤印", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition WardMend = new SkillDefinition(
            "enemy_ward_mend", "护障续接", SkillTargetRule.AllyUnit, SkillDeliveryMethod.Direct, 4, 2, 2,
            CombatFeedbackKind.ShieldRestore,
            new[] { SkillEffectDefinition.RestoreShield(4) });
        public static readonly SkillDefinition TetherPounce = new SkillDefinition(
            "enemy_tether_pounce", "缚环扑咬", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 1,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 2) });
        public static readonly SkillDefinition StoneSnare = new SkillDefinition(
            "enemy_stone_snare", "石索锁步", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 3, 2, 2,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(1, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 3) });
        public static readonly SkillDefinition RevealingLantern = new SkillDefinition(
            "enemy_revealing_lantern", "显影灯照", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 3, 1, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(1, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition WindlassBolt = new SkillDefinition(
            "enemy_windlass_bolt", "绞盘重矢", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 5, 1, 2,
            CombatFeedbackKind.Damage,
            new[] { SkillEffectDefinition.Damage(5, DamageType.Physical) }, minimumRange: 2);
        public static readonly WeaponDefinition TrackerBite = new WeaponDefinition("tracker_bite", "近战撕咬", DamageType.Physical, 3, 1);
        public static readonly WeaponDefinition KeeperMirror = new WeaponDefinition("keeper_mirror", "塔上灯镜", DamageType.Arcane, 2, 3);
        public static readonly WeaponDefinition LibrarianStaff = new WeaponDefinition("librarian_staff", "引风短杖", DamageType.Arcane, 2, 3);
        /// <summary>卷页：消耗一格散页打出风刃。散页的消耗由 AcademyFieldEnemyRuntime 结算。</summary>
        public static readonly SkillDefinition WindScrollEdge = new SkillDefinition(
            "enemy_wind_scroll_edge", "卷页", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 0, 0,
            CombatFeedbackKind.Damage,
            new[] { SkillEffectDefinition.Damage(4, DamageType.Arcane) });
        public static readonly SkillDefinition TrackerSnap = new SkillDefinition(
            "enemy_tracker_snap", "扑咬", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 0, 0,
            CombatFeedbackKind.Damage,
            new[] { SkillEffectDefinition.Damage(3, DamageType.Physical) });
        public static readonly SkillDefinition TrackerMaul = new SkillDefinition(
            "enemy_tracker_maul", "循味直扑", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 0, 0,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(6, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 1) });
        public static readonly WeaponDefinition StorekeeperStand = new WeaponDefinition("storekeeper_stand", "旧检定台", DamageType.Arcane, 2, 3);
        public static readonly WeaponDefinition PrototypeTools = new WeaponDefinition("prototype_tools", "工具", DamageType.Physical, 3, 1);
        /// <summary>旧脉冲：直线长度、清盾与破势由 AcademyFieldEnemyRuntime 结算；本条目承载公开描述与单体伤害参数。</summary>
        public static readonly SkillDefinition LegacyPulse = new SkillDefinition(
            "enemy_legacy_pulse", "旧脉冲", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 0, 0,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(5, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        /// <summary>布放：在正交相邻空格放下试制件；具体装置由 AcademyFieldEnemyRuntime 结算。</summary>
        public static readonly SkillDefinition PrototypeDeploy = new SkillDefinition(
            "enemy_prototype_deploy", "布放", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 0, 0,
            CombatFeedbackKind.ShieldRestore,
            new[] { SkillEffectDefinition.RestoreShield(2) });
        /// <summary>转镜：光柱的方向、暗段与回合结束伤害由 AcademyFieldEnemyRuntime 结算；
        /// 本条目只承载公开描述，不参与技能管道的伤害结算。</summary>
        public static readonly SkillDefinition SpotlightMirror = new SkillDefinition(
            "enemy_spotlight_mirror", "转镜", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 0, 0,
            CombatFeedbackKind.Damage,
            new[] { SkillEffectDefinition.Damage(5, DamageType.Arcane) });
        public static readonly SkillDefinition CoreLance = new SkillDefinition(
            "enemy_core_lance", "核心定向束", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 1, 1,
            CombatFeedbackKind.Damage,
            new[] { SkillEffectDefinition.Damage(3, DamageType.Arcane) });
        public static readonly SkillDefinition CorePulse = new SkillDefinition(
            "enemy_core_pulse", "核心破势脉冲", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 2, 2, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(5, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });

        public static readonly IReadOnlyList<SkillDefinition> All = new[]
        {
            ShieldRam, CombatCatalog.FireBolt, HookingStrike, VanguardCrush,
            SunderingSigil, WardMend, TetherPounce, StoneSnare, RevealingLantern, WindlassBolt,
            CoreLance, CorePulse, TrackerSnap, TrackerMaul, SpotlightMirror, WindScrollEdge,
            LegacyPulse, PrototypeDeploy
        };
        public static SkillDefinition Get(string id) => All.Single(skill => skill.Id == id);
    }
}
