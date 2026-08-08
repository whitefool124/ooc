using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class SkillValidationIssue
    {
        public string SkillId { get; }
        public string Code { get; }
        public string Message { get; }
        public SkillValidationIssue(string skillId, string code, string message) { SkillId = skillId; Code = code; Message = message; }
        public override string ToString() => SkillId + ":" + Code + ":" + Message;
    }

    public static class SkillCatalogValidator
    {
        public static IReadOnlyList<SkillValidationIssue> Validate(IEnumerable<SkillDefinition> skills)
        {
            List<SkillValidationIssue> issues = new List<SkillValidationIssue>();
            SkillDefinition[] pool = (skills ?? Array.Empty<SkillDefinition>()).ToArray();
            foreach (IGrouping<string, SkillDefinition> duplicate in pool.Where(skill => skill != null).GroupBy(skill => skill.Id, StringComparer.Ordinal).Where(group => group.Count() > 1))
                issues.Add(new SkillValidationIssue(duplicate.Key, "duplicate_id", "Skill ids must be unique."));
            foreach (SkillDefinition skill in pool)
            {
                if (skill == null) { issues.Add(new SkillValidationIssue("<null>", "null_skill", "Skill entry is null.")); continue; }
                ValidateSkill(skill, issues);
            }
            return issues;
        }

        private static void ValidateSkill(SkillDefinition skill, List<SkillValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(skill.Id)) Add(skill, issues, "missing_id", "A stable id is required.");
            if (string.IsNullOrWhiteSpace(skill.DisplayName)) Add(skill, issues, "missing_name", "A readable display name is required.");
            if (skill.Range < 0 || skill.ManaCost < 0 || skill.Cooldown < 0) Add(skill, issues, "negative_cost", "Range, mana and cooldown cannot be negative.");
            if (skill.Effects == null || skill.Effects.Count == 0) { Add(skill, issues, "empty_effects", "At least one effect is required."); return; }
            if (skill.TargetRule == SkillTargetRule.Self && skill.Range != 0) Add(skill, issues, "self_range", "Self skills must use range 0.");
            if (skill.Delivery == SkillDeliveryMethod.Area && skill.ModifierValue(SkillModifierType.Radius) <= 0) Add(skill, issues, "area_radius", "Area delivery requires a positive radius.");
            if (skill.TargetRule == SkillTargetRule.Destructible && !skill.Effects.Any(effect => effect.Type == SkillEffectType.DamageObject)) Add(skill, issues, "destructible_effect", "Destructible skills require object damage.");
            if (skill.TargetRule == SkillTargetRule.GridCell && !skill.Effects.Any(effect => effect.Type == SkillEffectType.MoveSource)) Add(skill, issues, "grid_effect", "Grid skills require a movement effect in the current contract.");
            if (skill.Effects.Count(effect => effect.Type == SkillEffectType.Damage) > 1) Add(skill, issues, "multiple_damage", "The validation slice supports one deterministic damage packet per target.");
            foreach (SkillEffectDefinition effect in skill.Effects)
            {
                if ((effect.Type == SkillEffectType.Damage || effect.Type == SkillEffectType.RestoreHealth || effect.Type == SkillEffectType.RestoreShield || effect.Type == SkillEffectType.RestoreMana || effect.Type == SkillEffectType.MoveSource || effect.Type == SkillEffectType.DamageObject) && effect.Amount <= 0)
                    Add(skill, issues, "non_positive_amount", effect.Type + " requires a positive amount.");
                if (effect.Type == SkillEffectType.ApplyStatus && effect.Duration <= 0) Add(skill, issues, "status_duration", "Applied statuses require a positive duration.");
            }
            try { CombatFeedbackCatalog.For(skill.PresentationKind); }
            catch (ArgumentOutOfRangeException) { Add(skill, issues, "presentation", "Presentation semantic is not registered."); }
        }

        private static void Add(SkillDefinition skill, List<SkillValidationIssue> issues, string code, string message) => issues.Add(new SkillValidationIssue(skill.Id ?? "<missing>", code, message));
    }

    public sealed class RogueliteSkillBuild
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> SkillIds { get; }
        public string PrimarySkillId { get; }
        public string SecondarySkillId { get; }

        public RogueliteSkillBuild(string id, string displayName, IEnumerable<string> skillIds, string primarySkillId, string secondarySkillId)
        { Id = id; DisplayName = displayName; SkillIds = skillIds.ToArray(); PrimarySkillId = primarySkillId; SecondarySkillId = secondarySkillId; }

        public void Apply(UnitState hero) => hero.Equip(hero.MainHand, hero.OffHand, RogueliteSkillCatalog.Get(PrimarySkillId), RogueliteSkillCatalog.Get(SecondarySkillId));
    }

    public static class RogueliteSkillCatalog
    {
        private static SkillDefinition Skill(string id, string name, SkillTargetRule target, SkillDeliveryMethod delivery, int range, int mana, int cooldown, CombatFeedbackKind presentation, SkillEffectDefinition[] effects, params SkillModifierDefinition[] modifiers) =>
            new SkillDefinition(id, name, target, delivery, range, mana, cooldown, presentation, effects, modifiers);

        public static readonly IReadOnlyList<SkillDefinition> All = new[]
        {
            CombatCatalog.FireBolt,
            CombatCatalog.FrostBind,
            Skill("ember_lance", "灼流长矛", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 3, 2, CombatFeedbackKind.Burning, new[] { SkillEffectDefinition.Damage(6, DamageType.Fire), SkillEffectDefinition.ApplyStatus(StatusType.Burning, 1) }, new SkillModifierDefinition(SkillModifierType.InitiativeDelay, 2)),
            Skill("breach_shot", "破障射击", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 5, 2, 1, CombatFeedbackKind.Damage, new[] { SkillEffectDefinition.Damage(4, DamageType.Physical) }, new SkillModifierDefinition(SkillModifierType.ArmorPierce, 2)),
            Skill("hammer_pulse", "震锤脉冲", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Area, 1, 2, 2, CombatFeedbackKind.Damage, new[] { SkillEffectDefinition.Damage(5, DamageType.Physical) }, new SkillModifierDefinition(SkillModifierType.Radius, 1), new SkillModifierDefinition(SkillModifierType.InitiativeDelay, 3)),
            Skill("searing_mark", "灼蚀标记", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 2, 2, CombatFeedbackKind.ArmorBreak, new[] { SkillEffectDefinition.Damage(2, DamageType.Fire), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 2) }),
            Skill("rail_burst", "轨道点射", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 5, 1, 1, CombatFeedbackKind.Slow, new[] { SkillEffectDefinition.Damage(3, DamageType.Physical), SkillEffectDefinition.ApplyStatus(StatusType.Slow, 1) }),
            Skill("cinder_sweep", "余烬横扫", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Area, 3, 3, 3, CombatFeedbackKind.Burning, new[] { SkillEffectDefinition.Damage(3, DamageType.Fire), SkillEffectDefinition.ApplyStatus(StatusType.Burning, 2) }, new SkillModifierDefinition(SkillModifierType.Radius, 1)),
            Skill("tether_arc", "束缚电弧", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 2, 2, CombatFeedbackKind.Bound, new[] { SkillEffectDefinition.Damage(1, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 2) }),
            Skill("damping_field", "阻尼场", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Area, 4, 3, 3, CombatFeedbackKind.Slow, new[] { SkillEffectDefinition.Damage(1, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.Slow, 2) }, new SkillModifierDefinition(SkillModifierType.Radius, 1)),
            Skill("armor_solvent", "护甲溶剂", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 2, 2, CombatFeedbackKind.ArmorBreak, new[] { SkillEffectDefinition.Damage(1, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 3) }),
            Skill("cryo_pulse", "冷凝脉冲", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Area, 3, 3, 3, CombatFeedbackKind.Bound, new[] { SkillEffectDefinition.Damage(2, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.Bound, 1) }, new SkillModifierDefinition(SkillModifierType.Radius, 1)),
            Skill("anchor_seal", "锚定封印", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Direct, 3, 2, 2, CombatFeedbackKind.Bound, new[] { SkillEffectDefinition.ApplyStatus(StatusType.Bound, 2) }),
            Skill("arc_bolt", "以太弧矢", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 5, 2, 1, CombatFeedbackKind.Damage, new[] { SkillEffectDefinition.Damage(4, DamageType.Arcane) }),
            Skill("mana_siphon", "以太回收", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 4, 1, 2, CombatFeedbackKind.Damage, new[] { SkillEffectDefinition.Damage(2, DamageType.Arcane), SkillEffectDefinition.RestoreMana(1) }),
            Skill("shield_converter", "护盾换流", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 2, 2, CombatFeedbackKind.ShieldRestore, new[] { SkillEffectDefinition.RestoreShield(3) }),
            Skill("aether_surge", "以太增压", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 0, 3, CombatFeedbackKind.ManaRestore, new[] { SkillEffectDefinition.RestoreMana(2) }),
            Skill("prism_arc", "棱镜电弧", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Area, 4, 3, 3, CombatFeedbackKind.Damage, new[] { SkillEffectDefinition.Damage(3, DamageType.Arcane) }, new SkillModifierDefinition(SkillModifierType.Radius, 1)),
            Skill("phase_step", "相位步", SkillTargetRule.GridCell, SkillDeliveryMethod.Direct, 3, 2, 2, CombatFeedbackKind.Movement, new[] { SkillEffectDefinition.MoveSource(3) }),
            Skill("overload_needle", "过载针", SkillTargetRule.EnemyUnit, SkillDeliveryMethod.Projectile, 5, 3, 2, CombatFeedbackKind.ArmorBreak, new[] { SkillEffectDefinition.Damage(5, DamageType.Arcane), SkillEffectDefinition.ApplyStatus(StatusType.ArmorBreak, 1) }, new SkillModifierDefinition(SkillModifierType.InitiativeDelay, 2)),
            Skill("field_repair", "现场修复", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 2, 2, CombatFeedbackKind.Healing, new[] { SkillEffectDefinition.RestoreHealth(4) }),
            Skill("barrier_charge", "屏障充能", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 2, 2, CombatFeedbackKind.ShieldRestore, new[] { SkillEffectDefinition.RestoreShield(4) }),
            Skill("thermal_purge", "热流净化", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 1, 2, CombatFeedbackKind.StatusCleared, new[] { SkillEffectDefinition.ClearStatus(StatusType.Burning) }),
            Skill("regenerative_seal", "再生封条", SkillTargetRule.Self, SkillDeliveryMethod.Direct, 0, 3, 3, CombatFeedbackKind.Healing, new[] { SkillEffectDefinition.RestoreHealth(2), SkillEffectDefinition.RestoreShield(2) }),
            Skill("rescue_beam", "救援射束", SkillTargetRule.AllyUnit, SkillDeliveryMethod.Projectile, 4, 2, 2, CombatFeedbackKind.Healing, new[] { SkillEffectDefinition.RestoreHealth(3) }),
            Skill("bastion_pulse", "堡垒脉冲", SkillTargetRule.AllyUnit, SkillDeliveryMethod.Area, 3, 3, 3, CombatFeedbackKind.ShieldRestore, new[] { SkillEffectDefinition.RestoreShield(2) }, new SkillModifierDefinition(SkillModifierType.Radius, 1)),
            Skill("demolition_charge", "定向爆破", SkillTargetRule.Destructible, SkillDeliveryMethod.Direct, 2, 2, 1, CombatFeedbackKind.DestructibleDamaged, new[] { SkillEffectDefinition.DamageObject(4) })
        };

        public static readonly IReadOnlyList<RogueliteSkillBuild> Builds = new[]
        {
            new RogueliteSkillBuild("ember_assault", "余烬突击", new[] { "fire_bolt", "ember_lance", "breach_shot", "hammer_pulse", "searing_mark", "rail_burst", "cinder_sweep" }, "fire_bolt", "breach_shot"),
            new RogueliteSkillBuild("lockdown_control", "锚定控制", new[] { "frost_bind", "tether_arc", "damping_field", "armor_solvent", "cryo_pulse", "anchor_seal" }, "frost_bind", "tether_arc"),
            new RogueliteSkillBuild("aether_circuit", "以太回路", new[] { "arc_bolt", "mana_siphon", "shield_converter", "aether_surge", "prism_arc", "phase_step", "overload_needle" }, "arc_bolt", "mana_siphon"),
            new RogueliteSkillBuild("field_engineer", "战地工程", new[] { "field_repair", "barrier_charge", "thermal_purge", "regenerative_seal", "rescue_beam", "bastion_pulse", "demolition_charge" }, "demolition_charge", "field_repair")
        };

        public static SkillDefinition Get(string id) => All.FirstOrDefault(skill => string.Equals(skill.Id, id, StringComparison.Ordinal)) ?? throw new InvalidOperationException("Unknown skill: " + id);
    }
}
