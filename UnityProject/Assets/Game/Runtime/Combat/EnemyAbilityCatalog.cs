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
            "enemy_vanguard_crush", "拆架", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 2, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(4, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition SunderingSigil = new SkillDefinition(
            "enemy_sundering_sigil", "贴压", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition WardMend = new SkillDefinition(
            "enemy_ward_mend", "贴墙续盾", SkillTargetRule.AllyUnit, SkillDeliveryMethod.Direct, 4, 2, 1,
            CombatFeedbackKind.ShieldRestore,
            new[] { SkillEffectDefinition.RestoreShield(4) });
        public static readonly SkillDefinition TetherPounce = new SkillDefinition(
            "enemy_tether_pounce", "撕咬", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 1, 1,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(2, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 2) });
        public static readonly SkillDefinition StoneSnare = new SkillDefinition(
            "enemy_stone_snare", "石索", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 3, 2, 2,
            CombatFeedbackKind.Bound,
            new[] { SkillEffectDefinition.Damage(1, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 3) });
        public static readonly SkillDefinition RevealingLantern = new SkillDefinition(
            "enemy_revealing_lantern", "转灯", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 4, 1, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(1, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        public static readonly SkillDefinition WindlassBolt = new SkillDefinition(
            "enemy_windlass_bolt", "重矢", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 5, 1, 1,
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
            "enemy_tracker_maul", "扑咬（循味）", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 0, 0,
            CombatFeedbackKind.Bound,
            // 束缚在目标自身回合开始衰减；取 2 才能盖住一个完整回合，与"束缚 1 回合"的公开口径一致。
            new[] { SkillEffectDefinition.Damage(6, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 2) });
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
        /// <summary>拆架重锤：塔之守卫阶段一以后的近身招式，8 点物理并清除护盾、施加破势（破势在肉鸽下即清盾禁盾）。</summary>
        public static readonly SkillDefinition BossSunderMaul = new SkillDefinition(
            "enemy_sunder_maul", "拆架重锤", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 1, 0, 2,
            CombatFeedbackKind.ArmorBreak,
            new[] { SkillEffectDefinition.Damage(8, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) });
        /// <summary>塔压：直线长度与伤害随已放行机关成长（4→6 格、5→8 点），由 AcademyCoreBossRuntime 在核心回合开始结算；本条目只承载公开描述。</summary>
        public static readonly SkillDefinition BossTowerPress = new SkillDefinition(
            "enemy_tower_press", "塔压", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 0, 0,
            CombatFeedbackKind.Damage,
            new[] { SkillEffectDefinition.Damage(5, DamageType.Arcane) });

        public static readonly IReadOnlyList<SkillDefinition> All = new[]
        {
            ShieldRam, CombatCatalog.FireBolt, HookingStrike, VanguardCrush,
            SunderingSigil, WardMend, TetherPounce, StoneSnare, RevealingLantern, WindlassBolt,
            BossSunderMaul, BossTowerPress, TrackerSnap, TrackerMaul, SpotlightMirror, WindScrollEdge,
            LegacyPulse, PrototypeDeploy
        };
        public static SkillDefinition Get(string id) => All.Single(skill => skill.Id == id);
    }
}
