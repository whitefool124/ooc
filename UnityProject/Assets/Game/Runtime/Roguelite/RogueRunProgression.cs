using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public enum RogueEncounterOutcome { Success, SurvivedFailure, Defeat }

    public sealed class RogueStageResolution
    {
        public bool RunSealed { get; }
        public bool TimeAdvanced { get; }
        public int HealthRecovered { get; }
        public int ManaRecovered { get; }
        public int TimeCost { get; }
        public RogueStageResolution(bool sealedRun, bool advanced, int health, int mana, int timeCost = 0)
        { RunSealed = sealedRun; TimeAdvanced = advanced; HealthRecovered = health; ManaRecovered = mana; TimeCost = timeCost; }
    }

    public static class RogueRunProgression
    {
        public static RogueStageResolution ResolveEncounter(RogueRunDto run, RogueEncounterOutcome outcome, int timeCost = 1, int healthRecoveryPerTime = 4, int manaRecoveryPerTime = 1)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (outcome == RogueEncounterOutcome.Defeat || run.CurrentHealth <= 0)
            {
                run.CurrentHealth = 0;
                return new RogueStageResolution(true, false, 0, 0);
            }
            timeCost = Math.Max(0, timeCost);
            if (timeCost == 0) return new RogueStageResolution(false, false, 0, 0);
            int healthBefore = run.CurrentHealth, manaBefore = run.CurrentMana;
            run.StageTime += timeCost;
            run.CurrentHealth = Math.Min(18, run.CurrentHealth + Math.Max(0, healthRecoveryPerTime) * timeCost);
            run.CurrentMana = Math.Min(RogueRuntimeConstants.MaximumPersonalMana, run.CurrentMana + Math.Max(0, manaRecoveryPerTime) * timeCost);
            return new RogueStageResolution(false, true, run.CurrentHealth - healthBefore, run.CurrentMana - manaBefore, timeCost);
        }

        public static void ResolveZeroTimeFunction(RogueRunDto run)
        { if (run == null) throw new ArgumentNullException(nameof(run)); }
    }

    public static class RogueRewardPool
    {
        public static IReadOnlyList<SpellDefinition> RollSpells(RogueContentCatalog catalog, int seed, int count,
            string source, SpellRarity rarity, IEnumerable<string> ownedIds, IEnumerable<string> allowedRoles = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            HashSet<string> owned = new HashSet<string>(ownedIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            HashSet<string> roles = allowedRoles == null ? null : new HashSet<string>(allowedRoles, StringComparer.Ordinal);
            List<SpellDefinition> candidates = catalog.Spells.Where(value => value.RewardEligible && value.Rarity == rarity &&
                value.RewardSources.Contains(source) && !owned.Contains(value.DefinitionId) && (roles == null || roles.Contains(value.Role))).ToList();
            return candidates.OrderBy(value => StableKey(seed, value.DefinitionId)).ThenBy(value => value.DefinitionId, StringComparer.Ordinal)
                .GroupBy(value => value.EquivalenceGroupId, StringComparer.Ordinal).Select(group => group.First()).Take(Math.Max(0, count)).ToArray();
        }

        private static int StableKey(int seed, string id)
        {
            unchecked { int hash = seed * 397; foreach (char value in id ?? string.Empty) hash = hash * 31 + value; return hash; }
        }
    }

    public sealed class RogueMigrationReport
    {
        public string ReportId { get; }
        public IReadOnlyList<string> Entries { get; }
        public RogueMigrationReport(string reportId, IEnumerable<string> entries)
        { ReportId = reportId; Entries = (entries ?? Array.Empty<string>()).ToArray(); }
        public string Serialize() => string.Join("\n", new[] { "migration-report-v1", ReportId }.Concat(Entries));
    }

    public static class LegacyMap10Migrator
    {
        public static RogueRunDto Migrate(OCC.Combat.RogueliteMapRun legacy, string reportId, out RogueMigrationReport report)
        {
            if (legacy == null) throw new ArgumentNullException(nameof(legacy));
            List<string> entries = new List<string>
            {
                "preserved:seed,map_nodes,current_health,current_mana,legal_fire_spells",
                "discarded:parts,aether,supplies,scouting_beacons,access_cards",
                "resources:gold=8,stage_contribution=0",
                "shield:reset_to_zero",
                "quickbar:compressed_8_to_4"
            };
            RogueRunDto dto = legacy.ExportRogue11(null, reportId);
            dto.Gold = 8; dto.StageContribution = 0;
            if (!string.IsNullOrEmpty(legacy.EquippedWeaponId))
            {
                dto.ReselectionClaimIds.Add("equipment:" + legacy.EquippedWeaponId);
                entries.Add("reselection:equipment:" + legacy.EquippedWeaponId);
            }
            foreach (OCC.Combat.ItemInstance item in legacy.Inventory.Items.OrderBy(value => value.AcquiredOrder))
            {
                dto.ReselectionClaimIds.Add("tactical:" + item.DefinitionId);
                entries.Add("reselection:tactical:" + item.DefinitionId);
            }
            entries.Add("validation:rogue11_shape_ok");
            report = new RogueMigrationReport(reportId, entries);
            return dto;
        }
    }
}
