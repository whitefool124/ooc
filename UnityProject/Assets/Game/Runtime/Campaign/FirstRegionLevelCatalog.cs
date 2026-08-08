using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum FirstRegionFloorTheme { StoneRoad, Courtyard, Ruins, AetherMarked }
    public enum LevelTerrainKind { LightCover, HeavyCover, AetherObjective }

    public sealed class LevelTerrainPlacement
    {
        public GridPosition Position { get; }
        public LevelTerrainKind Kind { get; }
        public LevelTerrainPlacement(int x, int y, LevelTerrainKind kind) { Position = new GridPosition(x, y); Kind = kind; }
    }

    public sealed class LevelEnemyPlacement
    {
        public string ArchetypeId { get; }
        public GridPosition Position { get; }
        public Facing Facing { get; }
        public LevelEnemyPlacement(string archetypeId, int x, int y, Facing facing = Facing.West)
        { ArchetypeId = archetypeId ?? throw new ArgumentNullException(nameof(archetypeId)); Position = new GridPosition(x, y); Facing = facing; }
    }

    public sealed class FirstRegionLevelDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string ObjectiveSummary { get; }
        public CombatObjectiveType ObjectiveType { get; }
        public int Tier { get; }
        public int Width { get; }
        public int Height { get; }
        public GridPosition HeroSpawn { get; }
        public FirstRegionFloorTheme FloorTheme { get; }
        public bool IsElite { get; }
        public bool IsBoss { get; }
        public IReadOnlyList<string> PrerequisiteLevelIds { get; }
        public IReadOnlyList<LevelEnemyPlacement> EnemyPlacements { get; }
        public IReadOnlyList<LevelTerrainPlacement> Terrain { get; }

        public FirstRegionLevelDefinition(string id, string displayName, string objectiveSummary, CombatObjectiveType objectiveType,
            int tier, GridPosition heroSpawn, FirstRegionFloorTheme floorTheme, bool isElite, bool isBoss,
            IEnumerable<string> prerequisiteLevelIds, IEnumerable<LevelEnemyPlacement> enemies, IEnumerable<LevelTerrainPlacement> terrain,
            int width = 12, int height = 9)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id)); DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ObjectiveSummary = objectiveSummary ?? string.Empty; ObjectiveType = objectiveType; Tier = tier; Width = width; Height = height;
            HeroSpawn = heroSpawn; FloorTheme = floorTheme; IsElite = isElite; IsBoss = isBoss;
            PrerequisiteLevelIds = (prerequisiteLevelIds ?? Array.Empty<string>()).ToArray();
            EnemyPlacements = (enemies ?? throw new ArgumentNullException(nameof(enemies))).ToArray();
            Terrain = (terrain ?? Array.Empty<LevelTerrainPlacement>()).ToArray();
        }

        public IReadOnlyList<string> ResolveEnemyArchetypeIds(string regionBossId = null) => EnemyPlacements
            .Select(enemy => enemy.ArchetypeId == FirstRegionLevelCatalog.RegionBossToken
                ? (string.IsNullOrEmpty(regionBossId) ? "core_overseer" : regionBossId)
                : enemy.ArchetypeId).ToArray();

        public string EnemySummary(string regionBossId = null) => string.Join("、", ResolveEnemyArchetypeIds(regionBossId)
            .Select(id => EnemyArchetypes.Get(id).DisplayName));
    }

    public sealed class FirstRegionLevelBuild
    {
        public FirstRegionLevelDefinition Definition { get; }
        public CombatState State { get; }
        public FirstRegionLevelBuild(FirstRegionLevelDefinition definition, CombatState state) { Definition = definition; State = state; }
    }

    public static class FirstRegionLevelCatalog
    {
        public const string RegionBossToken = "$region_boss";

        private static LevelTerrainPlacement L(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.LightCover);
        private static LevelTerrainPlacement H(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.HeavyCover);
        private static LevelTerrainPlacement O(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.AetherObjective);
        private static LevelEnemyPlacement E(string id, int x, int y) => new LevelEnemyPlacement(id, x, y);

        public static readonly IReadOnlyList<FirstRegionLevelDefinition> All = new[]
        {
            new FirstRegionLevelDefinition("rail_patrol", "石路巡哨", "清除石路巡哨队", CombatObjectiveType.Elimination, 1,
                new GridPosition(1, 4), FirstRegionFloorTheme.StoneRoad, false, false, Array.Empty<string>(),
                new[] { E("shieldguard", 8, 4), E("pyromancer", 10, 6), E("raider", 9, 2) },
                new[] { L(4, 2), L(4, 6), H(6, 4) }),
            new FirstRegionLevelDefinition("depot_wreck", "废弃驿站", "清除占据驿站的敌人", CombatObjectiveType.Elimination, 1,
                new GridPosition(1, 4), FirstRegionFloorTheme.Ruins, false, false, Array.Empty<string>(),
                new[] { E("tether_hound", 8, 4), E("sigil_mauler", 9, 6), E("stone_snare", 10, 2) },
                new[] { H(5, 2), H(5, 6), L(7, 4) }),
            new FirstRegionLevelDefinition("relay_raid", "野外导能柱", "破坏被敌军占用的导能柱", CombatObjectiveType.Destruction, 2,
                new GridPosition(1, 4), FirstRegionFloorTheme.AetherMarked, false, false, new[] { "rail_patrol" },
                new[] { E("raider", 7, 4), E("rune_arbalist", 10, 6), E("tether_hound", 9, 2) },
                new[] { H(6, 2), H(6, 6), L(4, 4), L(8, 5), O(10, 4) }),
            new FirstRegionLevelDefinition("signal_hub", "传讯石庭", "清除传讯石庭守军", CombatObjectiveType.Elimination, 2,
                new GridPosition(1, 4), FirstRegionFloorTheme.Courtyard, false, false, new[] { "depot_wreck" },
                new[] { E("barrier_mender", 9, 4), E("lantern_revealer", 10, 6), E("shieldguard", 8, 2) },
                new[] { H(6, 3), H(6, 5), L(4, 2), L(4, 6) }),
            new FirstRegionLevelDefinition("gatehouse", "石闸关口", "夺取石闸关口", CombatObjectiveType.Elimination, 3,
                new GridPosition(1, 4), FirstRegionFloorTheme.Courtyard, false, false, new[] { "signal_hub", "relay_raid" },
                new[] { E("shieldguard", 8, 4), E("sigil_mauler", 9, 2), E("rune_arbalist", 10, 6) },
                new[] { H(5, 3), H(5, 5), L(7, 2), L(7, 6) }),
            new FirstRegionLevelDefinition("transmission_tower", "传讯塔楼", "破坏塔楼内的敌方导能柱", CombatObjectiveType.Destruction, 3,
                new GridPosition(1, 4), FirstRegionFloorTheme.AetherMarked, false, false, new[] { "signal_hub" },
                new[] { E("pyromancer", 8, 6), E("stone_snare", 8, 2), E("lantern_revealer", 9, 4) },
                new[] { H(5, 2), H(5, 6), L(6, 4), L(9, 3), O(10, 4) }),
            new FirstRegionLevelDefinition("elite_foundry", "刻阵工坊", "摧毁工坊内的敌方导能柱", CombatObjectiveType.Destruction, 3,
                new GridPosition(1, 4), FirstRegionFloorTheme.Ruins, true, false, new[] { "signal_hub" },
                new[] { E("elite_vanguard", 7, 4), E("barrier_mender", 9, 6), E("sigil_mauler", 9, 2) },
                new[] { H(5, 3), H(5, 5), L(6, 2), L(6, 6), O(10, 4) }),
            new FirstRegionLevelDefinition("core_approach", "塔前石庭", "清除古塔前庭守军", CombatObjectiveType.Elimination, 4,
                new GridPosition(1, 4), FirstRegionFloorTheme.Courtyard, true, false, new[] { "transmission_tower", "elite_foundry" },
                new[] { E("elite_vanguard", 8, 4), E("rune_arbalist", 10, 6), E("stone_snare", 10, 2) },
                new[] { H(5, 2), H(5, 6), L(6, 4), L(8, 2) }),
            new FirstRegionLevelDefinition("core_finale", "古塔核心", "击败古塔核心守备", CombatObjectiveType.Elimination, 5,
                new GridPosition(1, 4), FirstRegionFloorTheme.AetherMarked, false, true, new[] { "core_approach" },
                new[] { E(RegionBossToken, 8, 4), E("elite_vanguard", 9, 6), E("barrier_mender", 10, 4), E("lantern_revealer", 9, 2) },
                new[] { H(5, 2), H(5, 6), L(6, 4), L(8, 2) })
        };

        private static readonly IReadOnlyDictionary<string, FirstRegionLevelDefinition> byId =
            All.ToDictionary(level => level.Id, StringComparer.Ordinal);

        public static FirstRegionLevelDefinition For(string id) => byId.TryGetValue(id, out FirstRegionLevelDefinition level)
            ? level : throw new KeyNotFoundException("Unknown first-region level: " + id);
        public static bool TryFor(string id, out FirstRegionLevelDefinition level)
        {
            if (id == null) { level = null; return false; }
            return byId.TryGetValue(id, out level);
        }

        public static IReadOnlyList<string> Validate()
        {
            List<string> errors = new List<string>();
            foreach (FirstRegionLevelDefinition level in All)
            {
                if (level.Width != 12 || level.Height != 9) errors.Add(level.Id + ": map must be 12x9");
                if (!Inside(level, level.HeroSpawn)) errors.Add(level.Id + ": hero spawn outside map");
                if (level.EnemyPlacements.Count < 3) errors.Add(level.Id + ": fewer than three enemies");
                HashSet<GridPosition> occupied = new HashSet<GridPosition> { level.HeroSpawn };
                foreach (LevelEnemyPlacement enemy in level.EnemyPlacements)
                {
                    if (!Inside(level, enemy.Position)) errors.Add(level.Id + ": enemy outside map");
                    if (!occupied.Add(enemy.Position)) errors.Add(level.Id + ": occupied cell repeated " + enemy.Position);
                    if (enemy.ArchetypeId != RegionBossToken)
                    {
                        try { EnemyArchetypes.Get(enemy.ArchetypeId); } catch (KeyNotFoundException) { errors.Add(level.Id + ": unknown enemy " + enemy.ArchetypeId); }
                    }
                }
                foreach (LevelTerrainPlacement terrain in level.Terrain)
                {
                    if (!Inside(level, terrain.Position)) errors.Add(level.Id + ": terrain outside map");
                    if (!occupied.Add(terrain.Position)) errors.Add(level.Id + ": occupied cell repeated " + terrain.Position);
                }
                int objectiveCount = level.Terrain.Count(tile => tile.Kind == LevelTerrainKind.AetherObjective);
                if (level.ObjectiveType == CombatObjectiveType.Destruction && objectiveCount == 0) errors.Add(level.Id + ": destruction objective has no target");
                if (level.ObjectiveType == CombatObjectiveType.Elimination && objectiveCount != 0) errors.Add(level.Id + ": elimination level has a destruction target");
                foreach (string prerequisite in level.PrerequisiteLevelIds)
                    if (!byId.TryGetValue(prerequisite, out FirstRegionLevelDefinition required)) errors.Add(level.Id + ": unknown prerequisite " + prerequisite);
                    else if (required.Tier >= level.Tier) errors.Add(level.Id + ": prerequisite is not earlier tier " + prerequisite);
            }
            return errors;
        }

        private static bool Inside(FirstRegionLevelDefinition level, GridPosition position) =>
            position.X >= 0 && position.X < level.Width && position.Y >= 0 && position.Y < level.Height;
    }

    public static class FirstRegionLevelBuilder
    {
        public static FirstRegionLevelBuild Build(string levelId, string regionBossId = null) => Build(FirstRegionLevelCatalog.For(levelId), regionBossId);

        public static FirstRegionLevelBuild Build(FirstRegionLevelDefinition level, string regionBossId = null)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            GridMap map = new GridMap(level.Width, level.Height);
            foreach (LevelTerrainPlacement placement in level.Terrain)
            {
                switch (placement.Kind)
                {
                    case LevelTerrainKind.LightCover: map.SetTile(placement.Position, new TileState { Cover = CoverType.Light, Durability = 4 }); break;
                    case LevelTerrainKind.HeavyCover: map.SetTile(placement.Position, new TileState { Cover = CoverType.Heavy, Durability = 7 }); break;
                    case LevelTerrainKind.AetherObjective: map.SetTile(placement.Position, new TileState { IsObjective = true, IsDevice = true, Durability = 6 }); break;
                }
            }

            List<UnitState> units = new List<UnitState>();
            UnitState hero = new UnitState("hero", true, level.HeroSpawn, Facing.East) { DisplayName = "阿斯特拉", Speed = 11 };
            hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            units.Add(hero);
            IReadOnlyList<string> resolvedIds = level.ResolveEnemyArchetypeIds(regionBossId);
            for (int index = 0; index < level.EnemyPlacements.Count; index++)
            {
                LevelEnemyPlacement placement = level.EnemyPlacements[index];
                UnitState enemy = new UnitState("enemy_" + index, false, placement.Position, placement.Facing);
                EnemyArchetypes.Get(resolvedIds[index]).Apply(enemy);
                units.Add(enemy);
            }

            CombatObjective objective = level.ObjectiveType == CombatObjectiveType.Destruction
                ? (CombatObjective)new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), level.Id + "_objective")
                : new EliminationObjective(level.Id + "_objective");
            return new FirstRegionLevelBuild(level, new CombatState(map, units, new[] { objective }));
        }
    }
}
