using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public sealed class RogueliteEncounterDefinition
    {
        public string NodeId { get; }
        public IReadOnlyList<string> EnemyArchetypeIds { get; }
        public bool IsElite { get; }
        public bool IsBoss { get; }

        public RogueliteEncounterDefinition(string nodeId, bool isElite, bool isBoss, params string[] enemyArchetypeIds)
        { NodeId = nodeId; IsElite = isElite; IsBoss = isBoss; EnemyArchetypeIds = enemyArchetypeIds ?? Array.Empty<string>(); }
    }

    public static class RogueliteEncounterCatalog
    {
        private static readonly Dictionary<string, RogueliteEncounterDefinition> encounters = new Dictionary<string, RogueliteEncounterDefinition>(StringComparer.Ordinal)
        {
            { "rail_patrol", new RogueliteEncounterDefinition("rail_patrol", false, false, "rifleman", "shieldguard", "pyromancer") },
            { "depot_wreck", new RogueliteEncounterDefinition("depot_wreck", false, false, "raider", "rifleman", "breaker") },
            { "relay_raid", new RogueliteEncounterDefinition("relay_raid", false, false, "rifleman", "raider", "sniper") },
            { "signal_hub", new RogueliteEncounterDefinition("signal_hub", false, false, "sniper", "binder", "warden") },
            { "gatehouse", new RogueliteEncounterDefinition("gatehouse", false, false, "shieldguard", "breaker", "rifleman") },
            { "transmission_tower", new RogueliteEncounterDefinition("transmission_tower", false, false, "pyromancer", "binder", "raider") },
            { "elite_foundry", new RogueliteEncounterDefinition("elite_foundry", true, false, "elite_vanguard", "warden", "breaker") },
            { "core_approach", new RogueliteEncounterDefinition("core_approach", true, false, "elite_vanguard", "binder", "shieldguard") },
            { "core_finale", new RogueliteEncounterDefinition("core_finale", false, true, "core_overseer", "warden", "binder", "shieldguard") }
        };

        public static RogueliteEncounterDefinition For(string nodeId, string regionBossId = null)
        {
            if (nodeId == "core_finale" && !string.IsNullOrEmpty(regionBossId))
                return new RogueliteEncounterDefinition("core_finale", false, true, regionBossId, "warden", "binder", "shieldguard");
            return encounters.TryGetValue(nodeId, out RogueliteEncounterDefinition encounter)
                ? encounter : new RogueliteEncounterDefinition(nodeId, false, false, "rifleman", "shieldguard", "pyromancer");
        }
    }
}
