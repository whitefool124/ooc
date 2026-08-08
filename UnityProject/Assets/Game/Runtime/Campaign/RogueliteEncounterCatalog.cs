using System;
using System.Collections.Generic;
using System.Linq;

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
        public static RogueliteEncounterDefinition For(string nodeId, string regionBossId = null)
        {
            if (!FirstRegionLevelCatalog.TryFor(nodeId, out FirstRegionLevelDefinition level))
                return new RogueliteEncounterDefinition(nodeId, false, false, "shieldguard", "pyromancer", "raider");
            return new RogueliteEncounterDefinition(level.Id, level.IsElite, level.IsBoss,
                level.ResolveEnemyArchetypeIds(regionBossId).ToArray());
        }
    }
}
