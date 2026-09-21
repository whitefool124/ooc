using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public sealed class RogueEnemyBaselineDefinition
    {
        public string ArchetypeId { get; }
        public int MaximumHealth { get; }
        public int StartingShield { get; }
        public string ShieldSourceId { get; }
        public RogueEnemyBaselineDefinition(string id, int health, int shield = 0, string shieldSourceId = "")
        { ArchetypeId = id; MaximumHealth = health; StartingShield = shield; ShieldSourceId = shieldSourceId ?? string.Empty; }
    }

    public sealed class RogueAcademyRewardEntry
    {
        public string DefinitionId { get; }
        public string Kind { get; }
        public string Source { get; }
        public string EquivalenceGroupId { get; }
        public RogueAcademyRewardEntry(string id, string kind, string source, string group)
        { DefinitionId = id; Kind = kind; Source = source; EquivalenceGroupId = string.IsNullOrWhiteSpace(group) ? id : group; }
    }

    public sealed class RogueAcademyContentService
    {
        private readonly RogueContentCatalog catalog;
        private readonly Dictionary<string, RogueEnemyBaselineDefinition> enemies;
        public IReadOnlyList<string> AllEligibleSpellIds => catalog.Spells.Where(value => value.RewardEligible).Select(value => value.DefinitionId).ToArray();
        public IReadOnlyList<EquipmentDefinition> Equipment => catalog.Equipment;
        public IReadOnlyList<AffixDefinition> Affixes => catalog.Affixes;
        public IReadOnlyCollection<RogueEnemyBaselineDefinition> EnemyBaselines => enemies.Values;

        public RogueAcademyContentService()
        {
            catalog = RogueContentCatalog.CreateAcademyV01();
            // 生命来自 EnemyArchetypes（其值即数据表口径），此处只补初始护盾的来源 Id，不再另设一套生命数值。
            enemies = OCC.Combat.EnemyArchetypes.All.ToDictionary(value => value.Id, value => Baseline(value), StringComparer.Ordinal);
        }

        public IReadOnlyList<RogueAcademyRewardEntry> Roll(int seed, string source, SpellRarity spellRarity, EquipmentRarity equipmentRarity,
            int spellCount, int equipmentCount, IEnumerable<string> ownedIds = null)
        {
            HashSet<string> owned = new HashSet<string>(ownedIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<RogueAcademyRewardEntry> result = RogueRewardPool.RollSpells(catalog, seed, spellCount, source, spellRarity, owned)
                .Select(value => new RogueAcademyRewardEntry(value.DefinitionId, "spell", source, value.EquivalenceGroupId)).ToList();
            IEnumerable<EquipmentDefinition> equipment = catalog.Equipment.Where(value => IsNextBuildRewardSlot(value.Slot) && value.AllowedRarities.Contains(equipmentRarity) && value.SourceTypes.Contains(source) && !owned.Contains(value.DefinitionId))
                .OrderBy(value => StableKey(seed, value.DefinitionId)).ThenBy(value => value.DefinitionId, StringComparer.Ordinal).Take(Math.Max(0, equipmentCount));
            result.AddRange(equipment.Select(value => new RogueAcademyRewardEntry(value.DefinitionId, "equipment", source, value.UniqueGroupId)));
            return result;
        }

        private static bool IsNextBuildRewardSlot(EquipmentSlot slot) => EquipmentSlotRules.IsActive(slot) &&
            slot != EquipmentSlot.Ring1 && slot != EquipmentSlot.Ring2 && slot != EquipmentSlot.Necklace;

        public void ApplyEnemyBaseline(CombatState combat, UnitState unit)
        {
            if (combat == null || combat.Ruleset != CombatRuleset.Roguelite) throw new ArgumentException("Roguelite combat required.", nameof(combat));
            if (unit == null || string.IsNullOrEmpty(unit.EnemyArchetypeId) || !enemies.TryGetValue(unit.EnemyArchetypeId, out RogueEnemyBaselineDefinition baseline)) return;
            unit.Armor = 0; unit.Block = 0; unit.ClearShield(); unit.ConfigureVitality(baseline.MaximumHealth);
            if (baseline.StartingShield > 0) combat.TryGrantRogueliteShield(unit.Id, baseline.ShieldSourceId, baseline.StartingShield);
        }

        /// <summary>初始生命唯一来源＝EnemyArchetypes（其值即 OCC_学院敌人数据表 的生命基线）；初始护盾数值同样取自数据表，这里只声明公开来源 Id。</summary>
        private static RogueEnemyBaselineDefinition Baseline(OCC.Combat.EnemyArchetype archetype)
        {
            switch (archetype.Id)
            {
                case "barrier_mender": return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth, 4, "enemy-mender-barrier");
                case "core_overseer": return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth, 6, "boss-core-barrier");
                case "breach_ram": return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth, 8, "breach-ram-initial-pressure");
                case "wind_librarian": return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth, 2, "baseline-wind_librarian-shield");
                case "prototype_hand": return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth, 2, "baseline-prototype_hand-shield");
                case "legacy_storekeeper": return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth, 2, "baseline-legacy_storekeeper-shield");
                case "signal_keeper": return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth, 1, "baseline-signal_keeper-shield");
                default: return new RogueEnemyBaselineDefinition(archetype.Id, archetype.MaxHealth);
            }
        }

        private static int StableKey(int seed, string id)
        { unchecked { int hash = seed * 397; foreach (char value in id ?? string.Empty) hash = hash * 31 + value; return hash; } }
    }
}
