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
            enemies = OCC.Combat.EnemyArchetypes.All.ToDictionary(value => value.Id, value => Baseline(value.Id, value.IsElite), StringComparer.Ordinal);
        }

        public IReadOnlyList<RogueAcademyRewardEntry> Roll(int seed, string source, SpellRarity spellRarity, EquipmentRarity equipmentRarity,
            int spellCount, int equipmentCount, IEnumerable<string> ownedIds = null)
        {
            HashSet<string> owned = new HashSet<string>(ownedIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<RogueAcademyRewardEntry> result = RogueRewardPool.RollSpells(catalog, seed, spellCount, source, spellRarity, owned)
                .Select(value => new RogueAcademyRewardEntry(value.DefinitionId, "spell", source, value.EquivalenceGroupId)).ToList();
            IEnumerable<EquipmentDefinition> equipment = catalog.Equipment.Where(value => value.AllowedRarities.Contains(equipmentRarity) && value.SourceTypes.Contains(source) && !owned.Contains(value.DefinitionId))
                .OrderBy(value => StableKey(seed, value.DefinitionId)).ThenBy(value => value.DefinitionId, StringComparer.Ordinal).Take(Math.Max(0, equipmentCount));
            result.AddRange(equipment.Select(value => new RogueAcademyRewardEntry(value.DefinitionId, "equipment", source, value.UniqueGroupId)));
            return result;
        }

        public void ApplyEnemyBaseline(CombatState combat, UnitState unit)
        {
            if (combat == null || combat.Ruleset != CombatRuleset.Roguelite) throw new ArgumentException("Roguelite combat required.", nameof(combat));
            if (unit == null || string.IsNullOrEmpty(unit.EnemyArchetypeId) || !enemies.TryGetValue(unit.EnemyArchetypeId, out RogueEnemyBaselineDefinition baseline)) return;
            unit.Armor = 0; unit.Block = 0; unit.ClearShield(); unit.ConfigureVitality(baseline.MaximumHealth);
            if (baseline.StartingShield > 0) combat.TryGrantRogueliteShield(unit.Id, baseline.ShieldSourceId, baseline.StartingShield);
        }

        private static RogueEnemyBaselineDefinition Baseline(string id, bool elite)
        {
            if (id == "warden") return new RogueEnemyBaselineDefinition(id, 18, 4, "enemy-warden-barrier");
            if (id == "barrier_mender") return new RogueEnemyBaselineDefinition(id, 16, 4, "enemy-mender-barrier");
            if (id == "core_overseer") return new RogueEnemyBaselineDefinition(id, 36, 6, "boss-core-barrier");
            if (id == "purifier_overseer") return new RogueEnemyBaselineDefinition(id, 32, 6, "boss-purifier-barrier");
            return new RogueEnemyBaselineDefinition(id, elite ? 24 : id == "tether_hound" ? 12 : 16);
        }

        private static int StableKey(int seed, string id)
        { unchecked { int hash = seed * 397; foreach (char value in id ?? string.Empty) hash = hash * 31 + value; return hash; } }
    }
}
