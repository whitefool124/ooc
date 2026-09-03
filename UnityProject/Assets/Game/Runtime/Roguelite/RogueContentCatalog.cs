using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public sealed class RogueContentCatalog
    {
        public IReadOnlyList<SpellDefinition> Spells { get; }
        public IReadOnlyList<EquipmentDefinition> Equipment { get; }
        public IReadOnlyList<AffixDefinition> Affixes { get; }
        public IReadOnlyList<TacticalItemDefinition> TacticalItems { get; }

        public RogueContentCatalog(IEnumerable<SpellDefinition> spells, IEnumerable<EquipmentDefinition> equipment,
            IEnumerable<AffixDefinition> affixes, IEnumerable<TacticalItemDefinition> tacticalItems)
        { Spells = spells.ToArray(); Equipment = equipment.ToArray(); Affixes = affixes.ToArray(); TacticalItems = tacticalItems.ToArray(); }

        public static RogueContentCatalog CreateAcademyV01()
            => new RogueContentCatalog(CreateSpells(), CreateEquipment(), CreateAffixes(), CreateTacticalItems());

        public static DamagePacket CreateDamagePacketSample()
            => new DamagePacket("sample-hit-1", "hero", "enemy", "BASE-FIRE-MELEE",
                new[] { new DamageComponent(DamageComponentKind.Physical, 8), new DamageComponent(DamageComponentKind.Fire, 4) },
                new[] { DamageTag.Melee });

        private static IEnumerable<SpellDefinition> CreateSpells()
        {
            yield return Basic("BASE-FIRE-MELEE", "灼触", "melee_attack", 1, 1, 0, "adjacent_visible_enemy", 1, "damage:physical:8");
            yield return Basic("BASE-FIRE-RANGED", "火花", "ranged_attack", 1, 2, 0, "visible_enemy", 4, "damage:fire:6");
            yield return Basic("BASE-AETHER-SHIELD", "以太护幕", "defense", 1, 2, 1, "self", 0, "grant_shield:6");
            yield return Basic("BASE-MANA-RECOVER", "回路调息", "resource", 1, 0, 1, "self", 0, "restore_mana:2:max12");

            foreach (FireSpellDefinition old in FireSpellCatalog.All)
            {
                string role = old.Group == FireSpellGroup.Melee ? "melee" : old.Group == FireSpellGroup.Universal ? "universal" : "ranged";
                SpellRarity rarity = old.Rarity == FireSpellRarity.Common ? SpellRarity.Common : old.Rarity == FireSpellRarity.Uncommon ? SpellRarity.Uncommon : SpellRarity.Rare;
                string[] rules = old.Rules.SelectMany(rule =>
                    (rule.Kind == FireRuleKind.ApplyArmorBreak || rule.Kind == FireRuleKind.ApplyBreakStance) ? new[] { "apply_break_stance" } :
                    (rule.Kind == FireRuleKind.ReduceIncomingDamage || rule.Kind == FireRuleKind.GrantShieldBeforeRanged) ? new[] { "grant_shield_before_ranged" } :
                    rule.Kind == FireRuleKind.ClearOneSelfStatus ? new[] { "clear_one_self_status" } :
                    rule.Kind == FireRuleKind.RepairWeapon ? Array.Empty<string>() :
                    new[] { "legacy_rule:" + rule.Kind }).ToArray();
                yield return new SpellDefinition(old.Id, old.DisplayName, "fire", role, rarity, old.ActionPointCost, old.ManaCost,
                    old.Cooldown, old.TargetKind.ToString(), old.Range, old.RequiresLineOfSight ? "required" : "not_required", rules,
                    Compatibility(old), new[] { "combat", "elite", "event", "treasure", "boss" }, true,
                    EquivalenceGroup(old.Id), "fire-reward-v0.1");
            }
        }

        private static SpellDefinition Basic(string id, string name, string role, int ap, int mana, int cooldown, string targeting, int range, string rule)
            => new SpellDefinition(id, name, "aether", role, SpellRarity.Basic, ap, mana, cooldown, targeting, range,
                "required_when_targeted", new[] { rule }, Array.Empty<string>(), new[] { "starter" }, false, string.Empty, "academy-basics-v0.1", true);

        private static IEnumerable<string> Compatibility(FireSpellDefinition spell)
        {
            if (spell.WeaponRequirement == FireWeaponRequirement.MeleeWeapon) return new[] { "melee_weapon" };
            if (spell.WeaponRequirement == FireWeaponRequirement.AnyWeapon) return new[] { "weapon" };
            return Array.Empty<string>();
        }

        private static string EquivalenceGroup(string id)
        {
            string[][] groups =
            {
                new[]{"FIRE-ENGAGE","M01","M02","M03"}, new[]{"FIRE-BREAK-APPLY","M06","U03"},
                new[]{"FIRE-BREAK-ATTACK","M07","R10"}, new[]{"FIRE-CONE-DAMAGE","M08","R07"},
                new[]{"FIRE-MELEE-CAPSTONE","M19","M20"}, new[]{"FIRE-RANGED-DEFENSE","M11","U10"},
                new[]{"FIRE-ACTIVE-SHIELD","M12","U07","U08"}, new[]{"FIRE-SELF-CLEANSE","M14","U09","U18"},
                new[]{"FIRE-BURN-DAMAGE","M16","U14"}, new[]{"FIRE-BURN-SHIELD","M17","U12"},
                new[]{"FIRE-BURN-MANA","U11","U13"}, new[]{"FIRE-WEAPON-DAMAGE","U01","U16"},
                new[]{"FIRE-BURN-ESTABLISH","U02","R03"}, new[]{"FIRE-BURN-MAINTAIN","U15","R04"},
                new[]{"FIRE-SINGLE-DIRECT","R01","R02","R05","R09"}, new[]{"FIRE-GROUND-LINE","R11","R12","R14"},
                new[]{"FIRE-GROUND-ZONE","R13","R15"}, new[]{"FIRE-BURN-DETONATE","R16","R18"},
                new[]{"FIRE-OBJECT-BREAK","U04","R19"}
            };
            string suffix = id.Replace("F-P-", string.Empty);
            return groups.Where(group => group.Skip(1).Contains(suffix)).Select(group => group[0]).FirstOrDefault() ?? id;
        }

        private static IEnumerable<EquipmentDefinition> CreateEquipment()
        {
            yield return E("ACA-EQ-MH01","学院练习剑",EquipmentSlot.MainHand,EquipmentHandedness.OneHanded,EquipmentRarity.Common,1,3,2,0,0,"attack:melee:8");
            yield return E("ACA-EQ-MH02","钩刃长枪",EquipmentSlot.MainHand,EquipmentHandedness.TwoHanded,EquipmentRarity.Uncommon,1,3,3,0,0,"attack:front2:8");
            yield return E("ACA-EQ-MH03","刻印战锤",EquipmentSlot.MainHand,EquipmentHandedness.TwoHanded,EquipmentRarity.Uncommon,2,3,4,0,0,"attack:heavy:16","object_damage:16");
            yield return E("ACA-EQ-MH04","猎团短弓",EquipmentSlot.MainHand,EquipmentHandedness.TwoHanded,EquipmentRarity.Common,2,3,2,0,0,"attack:ranged4:6");
            yield return E("ACA-EQ-MH05","绞盘重弩",EquipmentSlot.MainHand,EquipmentHandedness.TwoHanded,EquipmentRarity.Uncommon,2,3,4,0,0,"attack:ranged5:16","reload");
            yield return E("ACA-EQ-MH06","灰炉导杖",EquipmentSlot.MainHand,EquipmentHandedness.OneHanded,EquipmentRarity.Rare,1,3,2,0,0,"attack:aether3:6","mana_on_burning_hit:1");
            yield return E("ACA-EQ-OH01","学院圆盾",EquipmentSlot.OffHand,EquipmentHandedness.OffHand,EquipmentRarity.Common,2,2,2,0,0,"raise_shield:4");
            yield return E("ACA-EQ-OH02","石闸长盾",EquipmentSlot.OffHand,EquipmentHandedness.OffHand,EquipmentRarity.Uncommon,2,3,4,0,0,"raise_shield:4");
            yield return E("ACA-EQ-OH03","反握短刃",EquipmentSlot.OffHand,EquipmentHandedness.OffHand,EquipmentRarity.Uncommon,1,2,1,0,0,"attack:melee:6");
            yield return E("ACA-EQ-OH04","导流副环",EquipmentSlot.OffHand,EquipmentHandedness.OffHand,EquipmentRarity.Rare,1,1,1,1,0,"u_spell_first_mana:-1");
            yield return E("ACA-EQ-CH01","夹棉练习衣",EquipmentSlot.Chest,EquipmentHandedness.None,EquipmentRarity.Common,2,3,2,0,2,"turn_start_shield:2");
            yield return E("ACA-EQ-CH02","补强巡行衣",EquipmentSlot.Chest,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,3,3,0,4,"turn_start_shield:4");
            yield return E("ACA-EQ-CH03","塔卫承压带",EquipmentSlot.Chest,EquipmentHandedness.None,EquipmentRarity.Rare,2,3,4,0,6,"turn_start_shield:6");
            yield return E("ACA-EQ-CH04","轻装传令衣",EquipmentSlot.Chest,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,3,1,0,0,"first_move:+1");
            yield return E("ACA-EQ-CH05","封存巡检袍",EquipmentSlot.Chest,EquipmentHandedness.None,EquipmentRarity.Rare,2,3,2,0,0,"first_task_interact_free");
            yield return E("ACA-EQ-HD01","测距护目镜",EquipmentSlot.Head,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,1,1,0,0,"weapon_range:+1");
            yield return E("ACA-EQ-HD02","低压回路护额",EquipmentSlot.Head,EquipmentHandedness.None,EquipmentRarity.Rare,2,1,1,1,0,"low_mana_shield:2");
            yield return E("ACA-EQ-HN01","行进握带",EquipmentSlot.Hands,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,1,1,0,0,"move_attack_damage:+2");
            yield return E("ACA-EQ-HN02","回授护臂",EquipmentSlot.Hands,EquipmentHandedness.None,EquipmentRarity.Rare,2,1,2,1,0,"defensive_spell_shield:2");
            yield return E("ACA-EQ-LG01","石路行靴",EquipmentSlot.Legs,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,2,2,0,0,"first_move:+1");
            yield return E("ACA-EQ-LG02","定锚胫甲",EquipmentSlot.Legs,EquipmentHandedness.None,EquipmentRarity.Rare,2,2,3,0,0,"forced_move:-1");
            yield return E("ACA-EQ-BP01","勘验背架",EquipmentSlot.Backpack,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,3,2,0,0,"first_search_free");
            yield return E("ACA-EQ-BP02","快挂整备架",EquipmentSlot.Backpack,EquipmentHandedness.None,EquipmentRarity.Rare,2,3,2,0,0,"first_quickbar_swap_free");
            yield return E("ACA-EQ-CR01","学院储能芯",EquipmentSlot.AetherCore,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,2,1,1,0,"max_mana:+2");
            yield return E("ACA-EQ-CR02","余焰回收芯",EquipmentSlot.AetherCore,EquipmentHandedness.None,EquipmentRarity.Uncommon,2,2,1,2,0,"burn_apply_mana:1");
            yield return E("ACA-EQ-CR03","塔心并联芯",EquipmentSlot.AetherCore,EquipmentHandedness.None,EquipmentRarity.Legendary,2,2,2,3,0,"max_mana:+4");
            yield return E("ACA-EQ-DG01","远投定距杖",EquipmentSlot.Conduit,EquipmentHandedness.None,EquipmentRarity.Uncommon,1,3,1,2,0,"r_spell_range:+1","r_spell_mana:+1");
            yield return E("ACA-EQ-DG02","接触耦合环",EquipmentSlot.Conduit,EquipmentHandedness.None,EquipmentRarity.Uncommon,1,1,1,2,0,"m_spell_first_mana:-1");
            yield return E("ACA-EQ-AC01","余烬珠",EquipmentSlot.Accessory1,EquipmentHandedness.None,EquipmentRarity.Uncommon,1,1,1,0,0,"burning_direct_damage:+2");
            yield return E("ACA-EQ-AC02","空槽魔力计",EquipmentSlot.Accessory1,EquipmentHandedness.None,EquipmentRarity.Uncommon,1,1,1,0,0,"zero_mana_restore:1");
            yield return E("ACA-EQ-AC03","贴身守誓牌",EquipmentSlot.Accessory1,EquipmentHandedness.None,EquipmentRarity.Rare,1,1,1,0,0,"adjacent_enemy_turn_shield:2");
            yield return E("ACA-EQ-AC04","灰线行程扣",EquipmentSlot.Accessory1,EquipmentHandedness.None,EquipmentRarity.Rare,1,1,1,0,0,"first_fire_spell_free_facing");
        }

        private static EquipmentDefinition E(string id, string name, EquipmentSlot slot, EquipmentHandedness handedness,
            EquipmentRarity rarity, int width, int height, int weight, int load, int shield, params string[] effects)
            => new EquipmentDefinition(id, name, slot, handedness, rarity, width, height, weight, load, shield,
                effects.Where(value => value.StartsWith("attack:") || value == "reload" || value.StartsWith("raise_shield:")),
                effects.Where(value => !value.StartsWith("attack:") && value != "reload" && !value.StartsWith("raise_shield:")),
                upgradeNodes: UpgradeNodesFor(id), sourceTypes: new[] { "combat", "shop", "event", "treasure", "boss" });

        private static IEnumerable<UpgradeNodeDefinition> UpgradeNodesFor(string id)
        {
            if (id == "ACA-EQ-MH03") return new[] { new UpgradeNodeDefinition("node1","unit_damage:+4","object_damage:+12"), new UpgradeNodeDefinition("node2","remove_delay","push:1") };
            if (id == "ACA-EQ-MH05") return new[] { new UpgradeNodeDefinition("node1","shot_damage:+4","shot_range:+1"), new UpgradeNodeDefinition("node2","first_reload_free","remove_shot_delay") };
            if (id == "ACA-EQ-OH01") return new[] { new UpgradeNodeDefinition("node1","raise_shield:+1","raise_then_move:1") };
            if (id == "ACA-EQ-CH01") return new[] { new UpgradeNodeDefinition("node1","turn_shield:+1","first_move:+1") };
            if (id == "ACA-EQ-CH02") return new[] { new UpgradeNodeDefinition("node1","turn_shield:+1","weight:-1") };
            if (id == "ACA-EQ-CH03") return new[] { new UpgradeNodeDefinition("node1","turn_shield:+1","first_task_free"), new UpgradeNodeDefinition("node2","turn_shield:+1","weight:-1") };
            if (id == "ACA-EQ-CR02") return new[] { new UpgradeNodeDefinition("node1","burn_mana:2","burn_or_ground_mana"), new UpgradeNodeDefinition("node2","max_mana:+2","aether_load:-1") };
            if (id == "ACA-EQ-DG01") return new[] { new UpgradeNodeDefinition("node1","first_r_no_surcharge","r_range:+1") };
            return Array.Empty<UpgradeNodeDefinition>();
        }

        private static IEnumerable<AffixDefinition> CreateAffixes()
        {
            EquipmentSlot[] weapons = { EquipmentSlot.MainHand, EquipmentSlot.OffHand };
            EquipmentSlot[] allButAccessory = Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Where(value => value != EquipmentSlot.Accessory1 && value != EquipmentSlot.Accessory2).ToArray();
            yield return A("AFF-WEAPON-EDGE","平衡刃口",weapons,"weapon_damage:+2","weapon_damage");
            yield return A("AFF-WEAPON-REACH","延伸校尺",new[]{EquipmentSlot.MainHand},"weapon_range:+1","weapon_range");
            yield return A("AFF-OBJECT-BREACH","解构锤面",new[]{EquipmentSlot.MainHand},"object_damage:+8","object_damage");
            yield return A("AFF-FIRST-STEP","轻织",new[]{EquipmentSlot.Chest,EquipmentSlot.Legs},"first_move:+1","first_move");
            yield return A("AFF-FORCED-ANCHOR","定锚扣",new[]{EquipmentSlot.Legs,EquipmentSlot.Accessory1,EquipmentSlot.Accessory2},"forced_move:-1","forced_move");
            yield return A("AFF-MANA-RETURN","回流刻线",new[]{EquipmentSlot.AetherCore,EquipmentSlot.Conduit,EquipmentSlot.Accessory1,EquipmentSlot.Accessory2},"mana_return:1","mana_return");
            yield return A("AFF-BURN-EDGE","余焰刻线",new[]{EquipmentSlot.MainHand,EquipmentSlot.Hands,EquipmentSlot.Accessory1,EquipmentSlot.Accessory2},"burn_damage:+2","burn_payoff");
            yield return A("AFF-QUICK-SWAP","快挂",new[]{EquipmentSlot.Backpack,EquipmentSlot.Hands},"quick_swap_free","quick_swap");
            yield return A("AFF-SEARCH-MARK","勘验标尺",new[]{EquipmentSlot.Backpack,EquipmentSlot.Head},"search_free","search_action");
            yield return A("AFF-FACING-RESET","司向刻痕",new[]{EquipmentSlot.Head,EquipmentSlot.Legs,EquipmentSlot.OffHand,EquipmentSlot.Accessory1,EquipmentSlot.Accessory2},"free_facing","free_facing");
            yield return A("AFF-TASK-QUICK","巡检扣",new[]{EquipmentSlot.Chest,EquipmentSlot.Hands},"task_ap:-1","task_interaction");
            yield return A("AFF-LIGHT-FRAME","轻量骨架",allButAccessory,"weight:-1","item_weight");
            yield return new AffixDefinition("AFF-ROUND-SHIELD-P","紫色回合盾",new[]{EquipmentSlot.Head,EquipmentSlot.Hands,EquipmentSlot.Legs,EquipmentSlot.AetherCore,EquipmentSlot.Accessory1,EquipmentSlot.Accessory2},EquipmentRarity.Rare,"turn_start_shield:2","equipment_round_shield",EquipmentRarity.Rare);
            yield return new AffixDefinition("AFF-ROUND-SHIELD-G","金色回合盾",new[]{EquipmentSlot.Head,EquipmentSlot.Hands,EquipmentSlot.Legs,EquipmentSlot.AetherCore,EquipmentSlot.Accessory1,EquipmentSlot.Accessory2},EquipmentRarity.Legendary,"turn_start_shield:4","equipment_round_shield",EquipmentRarity.Legendary);
        }

        private static AffixDefinition A(string id, string name, IEnumerable<EquipmentSlot> slots, string effect, string group)
            => new AffixDefinition(id, name, slots, EquipmentRarity.Uncommon, effect, group);

        private static IEnumerable<TacticalItemDefinition> CreateTacticalItems()
        {
            foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
                yield return new TacticalItemDefinition(artifact.Id, artifact.DisplayName, artifact.Width,
                    artifact.Height, artifact.MaximumUses, artifact.Weight);
        }
    }

    public sealed class RogueValidationResult
    {
        private readonly List<string> errors = new List<string>();
        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;
        public void Add(string error) { errors.Add(error); }
    }

    public static class RogueContentValidator
    {
        private static readonly HashSet<string> RemovedFields = new HashSet<string>(new[]
        { "armor", "block", "block_chance", "durability", "max_durability", "armor_pierce", "maximum_total_shield", "shield_balance", "minimum_effective_damage" }, StringComparer.OrdinalIgnoreCase);

        public static RogueValidationResult Validate(RogueContentCatalog catalog)
        {
            RogueValidationResult result = new RogueValidationResult();
            Unique(catalog.Spells.Select(value => value.DefinitionId), "spell", result);
            Unique(catalog.Equipment.Select(value => value.DefinitionId), "equipment", result);
            Unique(catalog.Affixes.Select(value => value.AffixId), "affix", result);
            Unique(catalog.TacticalItems.Select(value => value.DefinitionId), "tactical item", result);
            if (catalog.Spells.Count(value => value.IsBasic) != 4) result.Add("Exactly four basic spells are required.");
            if (catalog.Spells.Count(value => value.RewardEligible) != 60) result.Add("Exactly sixty reward-eligible fire spells are required.");
            foreach (SpellDefinition spell in catalog.Spells)
            {
                if (string.IsNullOrWhiteSpace(spell.DefinitionId) || string.IsNullOrWhiteSpace(spell.DisplayName) || spell.Rules.Count == 0) result.Add("Invalid spell definition: " + spell.DefinitionId);
                if (spell.IsBasic && (spell.RewardEligible || spell.CompatibilityTags.Count > 0)) result.Add("Basic spell cannot enter rewards or require equipment: " + spell.DefinitionId);
            }
            foreach (EquipmentDefinition equipment in catalog.Equipment)
            {
                if (equipment.Width < 1 || equipment.Height < 1 || equipment.BaseWeight < 1) result.Add("Invalid equipment footprint: " + equipment.DefinitionId);
                if (equipment.HasDurability || equipment.Armor != 0 || equipment.BlockChance != 0) result.Add("Removed equipment field: " + equipment.DefinitionId);
                if (equipment.Handedness == EquipmentHandedness.TwoHanded && equipment.Slot != EquipmentSlot.MainHand) result.Add("Two-handed item must be main hand: " + equipment.DefinitionId);
                if (equipment.TurnStartShield > 0 && equipment.FixedEffectIds.Any(value => value.StartsWith("turn_start_shield:")) == false) result.Add("Shield source must be explicit: " + equipment.DefinitionId);
            }
            foreach (AffixDefinition affix in catalog.Affixes)
                if (string.IsNullOrWhiteSpace(affix.MutualExclusionGroup) || affix.LegalSlots.Count == 0) result.Add("Invalid affix: " + affix.AffixId);
            return result;
        }

        public static RogueValidationResult ValidateSerializedFieldNames(IEnumerable<string> fieldNames)
        {
            RogueValidationResult result = new RogueValidationResult();
            foreach (string field in fieldNames ?? Array.Empty<string>()) if (RemovedFields.Contains(field)) result.Add("Removed field is forbidden: " + field);
            return result;
        }

        private static void Unique(IEnumerable<string> ids, string kind, RogueValidationResult result)
        {
            foreach (IGrouping<string, string> group in ids.GroupBy(value => value, StringComparer.Ordinal))
                if (string.IsNullOrWhiteSpace(group.Key) || group.Count() != 1) result.Add("Invalid or duplicate " + kind + " id: " + group.Key);
        }
    }
}
