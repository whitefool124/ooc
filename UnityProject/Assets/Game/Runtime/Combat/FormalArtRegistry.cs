using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class FormalArtEntry
    {
        public string AssetId { get; }
        public string RuntimeId { get; }
        public string ResourcePath { get; }

        public FormalArtEntry(string assetId, string runtimeId, string resourcePath)
        {
            AssetId = assetId ?? throw new ArgumentNullException(nameof(assetId));
            RuntimeId = runtimeId ?? throw new ArgumentNullException(nameof(runtimeId));
            ResourcePath = resourcePath ?? throw new ArgumentNullException(nameof(resourcePath));
        }
    }

    // Single strict lookup surface for all formal-art bindings. Unknown ids throw;
    // production code must never silently substitute another unit or semantic asset.
    public static class FormalArtRegistry
    {
        public static readonly IReadOnlyList<FormalArtEntry> Units = new[]
        {
            new FormalArtEntry("unit.hero", "hero", "Art/FormalUnits64/hero"),
            new FormalArtEntry("unit.shieldguard", "shieldguard", "Art/FormalUnits64/shieldguard"),
            new FormalArtEntry("unit.pyromancer", "pyromancer", "Art/FormalUnits64/pyromancer"),
            new FormalArtEntry("unit.raider", "raider", "Art/FormalUnits64/raider"),
            new FormalArtEntry("unit.breaker", "breaker", "Art/FormalUnits64/breaker"),
            new FormalArtEntry("unit.warden", "warden", "Art/FormalUnits64/warden"),
            new FormalArtEntry("unit.binder", "binder", "Art/FormalUnits64/binder"),
            new FormalArtEntry("unit.elite_vanguard", "elite_vanguard", "Art/FormalUnits64/elite"),
            new FormalArtEntry("unit.core_overseer", "core_overseer", "Art/FormalUnits64/core_overseer"),
            new FormalArtEntry("unit.purifier_overseer", "purifier_overseer", "Art/FormalUnits64/purifier_overseer"),
            new FormalArtEntry("unit.sigil_mauler", "sigil_mauler", "Art/FormalUnits64/sigil_mauler"),
            new FormalArtEntry("unit.barrier_mender", "barrier_mender", "Art/FormalUnits64/barrier_mender"),
            new FormalArtEntry("unit.tether_hound", "tether_hound", "Art/FormalUnits64/tether_hound"),
            new FormalArtEntry("unit.stone_snare", "stone_snare", "Art/FormalUnits64/stone_snare"),
            new FormalArtEntry("unit.lantern_revealer", "lantern_revealer", "Art/FormalUnits64/lantern_revealer"),
            new FormalArtEntry("unit.rune_arbalist", "rune_arbalist", "Art/FormalUnits64/rune_arbalist")
        };

        public static readonly IReadOnlyList<FormalArtEntry> Commands = new[]
        {
            new FormalArtEntry("command.move", "move", "Art/FormalIcons32/move"),
            new FormalArtEntry("command.attack", "attack", "Art/FormalIcons32/attack"),
            new FormalArtEntry("command.skill", "skill", "Art/FormalIcons32/skill"),
            new FormalArtEntry("command.skill_two", "skill_two", "Art/FormalIcons32/skill_two"),
            new FormalArtEntry("command.loot", "loot", "Art/FormalIcons32/loot"),
            new FormalArtEntry("command.interact", "interact", "Art/FormalIcons32/interact")
        };

        public static readonly IReadOnlyList<FormalArtEntry> Feedback = new[]
        {
            new FormalArtEntry("feedback.damage", "damage", "Art/FormalFeedbackIcons32/damage"),
            new FormalArtEntry("feedback.shield_absorb", "shield_absorb", "Art/FormalFeedbackIcons32/shield_absorb"),
            new FormalArtEntry("feedback.armor_break", "armor_break", "Art/FormalFeedbackIcons32/armor_break"),
            new FormalArtEntry("feedback.burning", "burning", "Art/FormalFeedbackIcons32/burning"),
            new FormalArtEntry("feedback.bound", "bound", "Art/FormalFeedbackIcons32/bound"),
            new FormalArtEntry("feedback.slow", "slow", "Art/FormalFeedbackIcons32/slow"),
            new FormalArtEntry("feedback.healing", "healing", "Art/FormalFeedbackIcons32/healing"),
            new FormalArtEntry("feedback.shield_restore", "shield_restore", "Art/FormalFeedbackIcons32/shield_restore"),
            new FormalArtEntry("feedback.mana_restore", "mana_restore", "Art/FormalFeedbackIcons32/mana_restore"),
            new FormalArtEntry("feedback.status_cleared", "status_cleared", "Art/FormalFeedbackIcons32/status_cleared"),
            new FormalArtEntry("feedback.movement", "movement", "Art/FormalFeedbackIcons32/movement"),
            new FormalArtEntry("feedback.object_damaged", "object_damaged", "Art/FormalFeedbackIcons32/object_damaged"),
            new FormalArtEntry("feedback.object_destroyed", "object_destroyed", "Art/FormalFeedbackIcons32/object_destroyed"),
            new FormalArtEntry("feedback.unit_defeated", "unit_defeated", "Art/FormalFeedbackIcons32/unit_defeated")
        };

        public static readonly IReadOnlyList<FormalArtEntry> Statuses = new[]
        {
            new FormalArtEntry("status.burning", "burning", "Art/FormalStatusIcons32/burning"),
            new FormalArtEntry("status.slow", "slow", "Art/FormalStatusIcons32/slow"),
            new FormalArtEntry("status.bound", "bound", "Art/FormalStatusIcons32/bound"),
            new FormalArtEntry("status.armor_break", "armor_break", "Art/FormalStatusIcons32/armor_break"),
            new FormalArtEntry("status.dazzled", "dazzled", "Art/FormalStatusIcons32/dazzled"),
            new FormalArtEntry("status.revealed", "revealed", "Art/FormalStatusIcons32/revealed")
        };

        public static readonly IReadOnlyList<FormalArtEntry> Environments = new[]
        {
            new FormalArtEntry("environment.burning_ground", "burning_ground", "Art/FormalEnvironment32/burning_ground"),
            new FormalArtEntry("environment.water", "water", "Art/FormalEnvironment32/water"),
            new FormalArtEntry("environment.ice", "ice", "Art/FormalEnvironment32/ice"),
            new FormalArtEntry("environment.smoke", "smoke", "Art/FormalEnvironment32/smoke"),
            new FormalArtEntry("environment.bright_zone", "bright_zone", "Art/FormalEnvironment32/bright_zone"),
            new FormalArtEntry("environment.dark_zone", "dark_zone", "Art/FormalEnvironment32/dark_zone"),
            new FormalArtEntry("environment.conductive_path", "conductive_path", "Art/FormalEnvironment32/conductive_path"),
            new FormalArtEntry("environment.obstacle_cover", "obstacle_cover", "Art/FormalEnvironment32/obstacle_cover")
        };

        public static readonly IReadOnlyList<FormalArtEntry> NodeTypes = new[]
        {
            new FormalArtEntry("node_type.start", "start", "Art/FormalNodeIcons32/types/start"),
            new FormalArtEntry("node_type.combat", "combat", "Art/FormalNodeIcons32/types/combat"),
            new FormalArtEntry("node_type.elite", "elite", "Art/FormalNodeIcons32/types/elite"),
            new FormalArtEntry("node_type.event", "event", "Art/FormalNodeIcons32/types/event"),
            new FormalArtEntry("node_type.workshop", "workshop", "Art/FormalNodeIcons32/types/workshop"),
            new FormalArtEntry("node_type.shop", "shop", "Art/FormalNodeIcons32/types/shop"),
            new FormalArtEntry("node_type.rest", "rest", "Art/FormalNodeIcons32/types/rest"),
            new FormalArtEntry("node_type.treasure", "treasure", "Art/FormalNodeIcons32/types/treasure"),
            new FormalArtEntry("node_type.finale", "finale", "Art/FormalNodeIcons32/types/finale")
        };

        public static readonly IReadOnlyList<FormalArtEntry> RuntimeSkills = new[]
        {
            new FormalArtEntry("skill.runtime.fire_bolt", "fire_bolt", "Art/FormalSkillIcons32/Runtime/fire_bolt"),
            new FormalArtEntry("skill.runtime.frost_bind", "frost_bind", "Art/FormalSkillIcons32/Runtime/frost_bind"),
            new FormalArtEntry("skill.runtime.ember_lance", "ember_lance", "Art/FormalSkillIcons32/Runtime/ember_lance"),
            new FormalArtEntry("skill.runtime.breach_shot", "breach_shot", "Art/FormalSkillIcons32/Runtime/breach_shot"),
            new FormalArtEntry("skill.runtime.hammer_pulse", "hammer_pulse", "Art/FormalSkillIcons32/Runtime/hammer_pulse"),
            new FormalArtEntry("skill.runtime.searing_mark", "searing_mark", "Art/FormalSkillIcons32/Runtime/searing_mark"),
            new FormalArtEntry("skill.runtime.rail_burst", "rail_burst", "Art/FormalSkillIcons32/Runtime/rail_burst"),
            new FormalArtEntry("skill.runtime.cinder_sweep", "cinder_sweep", "Art/FormalSkillIcons32/Runtime/cinder_sweep"),
            new FormalArtEntry("skill.runtime.tether_arc", "tether_arc", "Art/FormalSkillIcons32/Runtime/tether_arc"),
            new FormalArtEntry("skill.runtime.damping_field", "damping_field", "Art/FormalSkillIcons32/Runtime/damping_field"),
            new FormalArtEntry("skill.runtime.armor_solvent", "armor_solvent", "Art/FormalSkillIcons32/Runtime/armor_solvent"),
            new FormalArtEntry("skill.runtime.cryo_pulse", "cryo_pulse", "Art/FormalSkillIcons32/Runtime/cryo_pulse"),
            new FormalArtEntry("skill.runtime.anchor_seal", "anchor_seal", "Art/FormalSkillIcons32/Runtime/anchor_seal"),
            new FormalArtEntry("skill.runtime.arc_bolt", "arc_bolt", "Art/FormalSkillIcons32/Runtime/arc_bolt"),
            new FormalArtEntry("skill.runtime.mana_siphon", "mana_siphon", "Art/FormalSkillIcons32/Runtime/mana_siphon"),
            new FormalArtEntry("skill.runtime.shield_converter", "shield_converter", "Art/FormalSkillIcons32/Runtime/shield_converter"),
            new FormalArtEntry("skill.runtime.aether_surge", "aether_surge", "Art/FormalSkillIcons32/Runtime/aether_surge"),
            new FormalArtEntry("skill.runtime.prism_arc", "prism_arc", "Art/FormalSkillIcons32/Runtime/prism_arc"),
            new FormalArtEntry("skill.runtime.phase_step", "phase_step", "Art/FormalSkillIcons32/Runtime/phase_step"),
            new FormalArtEntry("skill.runtime.overload_needle", "overload_needle", "Art/FormalSkillIcons32/Runtime/overload_needle"),
            new FormalArtEntry("skill.runtime.field_repair", "field_repair", "Art/FormalSkillIcons32/Runtime/field_repair"),
            new FormalArtEntry("skill.runtime.barrier_charge", "barrier_charge", "Art/FormalSkillIcons32/Runtime/barrier_charge"),
            new FormalArtEntry("skill.runtime.thermal_purge", "thermal_purge", "Art/FormalSkillIcons32/Runtime/thermal_purge"),
            new FormalArtEntry("skill.runtime.regenerative_seal", "regenerative_seal", "Art/FormalSkillIcons32/Runtime/regenerative_seal"),
            new FormalArtEntry("skill.runtime.rescue_beam", "rescue_beam", "Art/FormalSkillIcons32/Runtime/rescue_beam"),
            new FormalArtEntry("skill.runtime.bastion_pulse", "bastion_pulse", "Art/FormalSkillIcons32/Runtime/bastion_pulse"),
            new FormalArtEntry("skill.runtime.demolition_charge", "demolition_charge", "Art/FormalSkillIcons32/Runtime/demolition_charge")
        };

        public static readonly IReadOnlyList<FormalArtEntry> FireSpells = FireSpellCatalog.All
            .Select(spell => new FormalArtEntry("skill.fire." + spell.Id.ToLowerInvariant(), spell.Id, spell.IconPath))
            .ToArray();

        private static readonly IReadOnlyList<FormalArtEntry> CoreItems = new[]
        {
            new FormalArtEntry("item.rifle", "rifle", "Art/FormalItemIcons32/rifle"),
            new FormalArtEntry("item.hammer", "hammer", "Art/FormalItemIcons32/hammer"),
            new FormalArtEntry("item.wand", "wand", "Art/FormalItemIcons32/wand"),
            new FormalArtEntry("item.shield", "shield", "Art/FormalItemIcons32/shield"),
            new FormalArtEntry("item.medkit", "medkit", "Art/FormalItemIcons32/medkit"),
            new FormalArtEntry("item.shield_cell", "shield_cell", "Art/FormalItemIcons32/shield_cell"),
            new FormalArtEntry("item.war_hammer", "war_hammer", "Art/FormalItemIcons32/war_hammer"),
            new FormalArtEntry("item.aether_wand", "aether_wand", "Art/FormalItemIcons32/aether_wand"),
            new FormalArtEntry("item.arcane_wand", "arcane_wand", "Art/FormalItemIcons32/arcane_wand"),
            new FormalArtEntry("item.fire_bolt_reward", "fire_bolt_reward", "Art/FormalItemIcons32/fire_bolt_reward"),
            new FormalArtEntry("item.frost_bind_reward", "frost_bind_reward", "Art/FormalItemIcons32/frost_bind_reward"),
            new FormalArtEntry("item.fire_scroll", "F-S01", "Art/FormalItemIcons32/fire_scroll"),
            new FormalArtEntry("item.category_weapon", "category_weapon", "Art/FormalItemIcons32/category_weapon"),
            new FormalArtEntry("item.category_armor", "category_armor", "Art/FormalItemIcons32/category_armor"),
            new FormalArtEntry("item.category_consumable", "category_consumable", "Art/FormalItemIcons32/category_consumable"),
            new FormalArtEntry("item.category_scroll", "category_scroll", "Art/FormalItemIcons32/category_scroll"),
            new FormalArtEntry("item.category_artifact", "category_artifact", "Art/FormalItemIcons32/category_artifact"),
            new FormalArtEntry("item.category_material", "category_material", "Art/FormalItemIcons32/category_material"),
            new FormalArtEntry("item.category_quest", "category_quest", "Art/FormalItemIcons32/category_quest"),
            new FormalArtEntry("item.category_container", "category_container", "Art/FormalItemIcons32/category_container"),
            new FormalArtEntry("item.inventory_search", "inventory_search", "Art/FormalItemIcons32/inventory_search"),
            new FormalArtEntry("item.inventory_filter", "inventory_filter", "Art/FormalItemIcons32/inventory_filter"),
            new FormalArtEntry("item.inventory_sort", "inventory_sort", "Art/FormalItemIcons32/inventory_sort"),
            new FormalArtEntry("item.inventory_autoplace", "inventory_autoplace", "Art/FormalItemIcons32/inventory_autoplace"),
            new FormalArtEntry("item.inventory_quickbar", "inventory_quickbar", "Art/FormalItemIcons32/inventory_quickbar"),
            new FormalArtEntry("item.inventory_use", "inventory_use", "Art/FormalItemIcons32/inventory_use"),
            new FormalArtEntry("item.inventory_salvage", "inventory_salvage", "Art/FormalItemIcons32/inventory_salvage"),
            new FormalArtEntry("item.inventory_discard", "inventory_discard", "Art/FormalItemIcons32/inventory_discard"),
            new FormalArtEntry("item.inventory_rotate", "inventory_rotate", "Art/FormalItemIcons32/inventory_rotate"),
            new FormalArtEntry("item.inventory_clear", "inventory_clear", "Art/FormalItemIcons32/inventory_clear"),
            new FormalArtEntry("item.inventory_weight", "inventory_weight", "Art/FormalItemIcons32/inventory_weight"),
            new FormalArtEntry("item.loot_unknown", "loot_unknown", "Art/FormalItemIcons32/loot_unknown"),
            new FormalArtEntry("item.loot_searching", "loot_searching", "Art/FormalItemIcons32/loot_searching"),
            new FormalArtEntry("item.loot_empty", "loot_empty", "Art/FormalItemIcons32/loot_empty")
        };
        public static readonly IReadOnlyList<FormalArtEntry> Items = CoreItems
            .Concat(ArtifactCatalog.All.Select(artifact => new FormalArtEntry("item.artifact." + artifact.Id.ToLowerInvariant(), artifact.Id, artifact.IconPath)))
            .ToArray();

        public static readonly IReadOnlyList<FormalArtEntry> Vfx = new[]
        {
            new FormalArtEntry("vfx.selection", "selection", "Art/FormalVfx32/selection"),
            new FormalArtEntry("vfx.lock", "lock", "Art/FormalVfx32/lock"),
            new FormalArtEntry("vfx.path", "path", "Art/FormalVfx32/path"),
            new FormalArtEntry("vfx.landing", "landing", "Art/FormalVfx32/landing"),
            new FormalArtEntry("vfx.shoot", "shoot", "Art/FormalVfx32/shoot"),
            new FormalArtEntry("vfx.melee", "melee", "Art/FormalVfx32/melee"),
            new FormalArtEntry("vfx.hit", "hit", "Art/FormalVfx32/hit"),
            new FormalArtEntry("vfx.heavy_hit", "heavy_hit", "Art/FormalVfx32/heavy_hit"),
            new FormalArtEntry("vfx.shield_hit", "shield_hit", "Art/FormalVfx32/shield_hit"),
            new FormalArtEntry("vfx.shield_absorb", "shield_absorb", "Art/FormalVfx32/shield_absorb"),
            new FormalArtEntry("vfx.shield_break", "shield_break", "Art/FormalVfx32/shield_break"),
            new FormalArtEntry("vfx.shield_restore", "shield_restore", "Art/FormalVfx32/shield_restore"),
            new FormalArtEntry("vfx.health_repair", "health_repair", "Art/FormalVfx32/health_repair"),
            new FormalArtEntry("vfx.mana_restore", "mana_restore", "Art/FormalVfx32/mana_restore"),
            new FormalArtEntry("vfx.cleanse", "cleanse", "Art/FormalVfx32/cleanse"),
            new FormalArtEntry("vfx.burning", "burning", "Art/FormalVfx32/burning"),
            new FormalArtEntry("vfx.slow", "slow", "Art/FormalVfx32/slow"),
            new FormalArtEntry("vfx.bound", "bound", "Art/FormalVfx32/bound"),
            new FormalArtEntry("vfx.armor_break", "armor_break", "Art/FormalVfx32/armor_break"),
            new FormalArtEntry("vfx.dazzled", "dazzled", "Art/FormalVfx32/dazzled"),
            new FormalArtEntry("vfx.revealed", "revealed", "Art/FormalVfx32/revealed"),
            new FormalArtEntry("vfx.object_damage", "object_damage", "Art/FormalVfx32/object_damage"),
            new FormalArtEntry("vfx.object_break", "object_break", "Art/FormalVfx32/object_break"),
            new FormalArtEntry("vfx.debris", "debris", "Art/FormalVfx32/debris"),
            new FormalArtEntry("vfx.fire_projectile", "fire_projectile", "Art/FormalVfx32/fire_projectile"),
            new FormalArtEntry("vfx.fire_spray", "fire_spray", "Art/FormalVfx32/fire_spray"),
            new FormalArtEntry("vfx.fire_cross_blast", "fire_cross_blast", "Art/FormalVfx32/fire_cross_blast"),
            new FormalArtEntry("vfx.fire_burning_ground", "fire_burning_ground", "Art/FormalVfx32/fire_burning_ground"),
            new FormalArtEntry("vfx.fire_detonate", "fire_detonate", "Art/FormalVfx32/fire_detonate"),
            new FormalArtEntry("vfx.fire_smoke", "fire_smoke", "Art/FormalVfx32/fire_smoke")
        };

        public static readonly IReadOnlyList<FormalArtEntry> All = Units
            .Concat(Commands).Concat(Feedback).Concat(Statuses).Concat(Environments)
            .Concat(NodeTypes).Concat(RuntimeSkills).Concat(FireSpells).Concat(Items).Concat(Vfx).ToArray();

        public static FormalArtEntry Required(IReadOnlyList<FormalArtEntry> entries, string runtimeId)
        {
            FormalArtEntry entry = entries.FirstOrDefault(candidate => string.Equals(candidate.RuntimeId, runtimeId, StringComparison.OrdinalIgnoreCase));
            if (entry == null) throw new KeyNotFoundException("Missing formal art mapping for runtime id: " + runtimeId);
            return entry;
        }

        public static string UnitPath(string runtimeId) => Required(Units, runtimeId).ResourcePath;
        public static string CommandPath(string runtimeId) => Required(Commands, runtimeId).ResourcePath;
        public static string FeedbackPath(string runtimeId) => Required(Feedback, runtimeId).ResourcePath;
        public static string StatusPath(string runtimeId) => Required(Statuses, runtimeId).ResourcePath;
        public static string EnvironmentPath(string runtimeId) => Required(Environments, runtimeId).ResourcePath;
        public static string NodeTypePath(string runtimeId) => Required(NodeTypes, runtimeId).ResourcePath;
        public static string RuntimeSkillPath(string runtimeId) => Required(RuntimeSkills, runtimeId).ResourcePath;
        public static string FireSpellPath(string runtimeId) => Required(FireSpells, runtimeId).ResourcePath;
        public static string ItemPath(string runtimeId) => Required(Items, runtimeId).ResourcePath;
        public static string VfxPath(string runtimeId) => Required(Vfx, runtimeId).ResourcePath;
    }
}
