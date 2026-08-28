using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum FirstRegionFloorTheme { StoneRoad, Courtyard, Ruins, AetherMarked }
    public enum LevelTerrainKind { LightCover, HeavyCover, AetherObjective }
    public enum LevelOpeningProfile { Melee, Ranged, Generalist }

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

    public sealed class LevelSpaceContract
    {
        public string Grammar { get; }
        public IReadOnlyList<GridPosition> RouteAnchors { get; }
        public string PublicRisk { get; }
        public string CounterplayWindow { get; }
        public IReadOnlyList<LevelOpeningProfile> SupportedOpenings { get; }

        public LevelSpaceContract(string grammar, IEnumerable<GridPosition> routeAnchors, string publicRisk, string counterplayWindow)
        {
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            RouteAnchors = (routeAnchors ?? throw new ArgumentNullException(nameof(routeAnchors))).ToArray();
            PublicRisk = publicRisk ?? string.Empty;
            CounterplayWindow = counterplayWindow ?? string.Empty;
            SupportedOpenings = new[] { LevelOpeningProfile.Melee, LevelOpeningProfile.Ranged, LevelOpeningProfile.Generalist };
        }
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
        public IReadOnlyList<GridPosition> BlockedPositions { get; }
        public LevelSpaceContract SpaceContract { get; }

        public FirstRegionLevelDefinition(string id, string displayName, string objectiveSummary, CombatObjectiveType objectiveType,
            int tier, GridPosition heroSpawn, FirstRegionFloorTheme floorTheme, bool isElite, bool isBoss,
            IEnumerable<string> prerequisiteLevelIds, IEnumerable<LevelEnemyPlacement> enemies, IEnumerable<LevelTerrainPlacement> terrain,
            LevelSpaceContract spaceContract, int width = 12, int height = 9, IEnumerable<GridPosition> blockedPositions = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id)); DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ObjectiveSummary = objectiveSummary ?? string.Empty; ObjectiveType = objectiveType; Tier = tier; Width = width; Height = height;
            HeroSpawn = heroSpawn; FloorTheme = floorTheme; IsElite = isElite; IsBoss = isBoss;
            PrerequisiteLevelIds = (prerequisiteLevelIds ?? Array.Empty<string>()).ToArray();
            EnemyPlacements = (enemies ?? throw new ArgumentNullException(nameof(enemies))).ToArray();
            Terrain = (terrain ?? Array.Empty<LevelTerrainPlacement>()).ToArray();
            BlockedPositions = (blockedPositions ?? Array.Empty<GridPosition>()).Distinct().ToArray();
            SpaceContract = spaceContract ?? throw new ArgumentNullException(nameof(spaceContract));
        }

        public IReadOnlyList<string> ResolveEnemyArchetypeIds(string regionBossId = null) => EnemyPlacements
            .Select(enemy => enemy.ArchetypeId == FirstRegionLevelCatalog.RegionBossToken
                ? "core_overseer"
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
        private static LevelSpaceContract S(string grammar, GridPosition routeA, GridPosition routeB, string risk, string counterplay) =>
            new LevelSpaceContract(grammar, new[] { routeA, routeB }, risk, counterplay);

        public static readonly IReadOnlyList<FirstRegionLevelDefinition> All = new[]
        {
            new FirstRegionLevelDefinition("rail_patrol", "石路巡哨", "清除石路巡哨队", CombatObjectiveType.Elimination, 1,
                new GridPosition(5, 8), FirstRegionFloorTheme.StoneRoad, false, false, Array.Empty<string>(),
                new[] { E("shieldguard", 5, 4), E("pyromancer", 2, 1), E("raider", 9, 1) },
                new[] { L(3, 2), L(8, 2), L(2, 5), L(9, 5), H(4, 3), H(7, 3), H(4, 6), H(7, 6) },
                S("开阔交叉线", new GridPosition(3, 7), new GridPosition(8, 7), "压中会进入两翼交叉影响区；切翼路线更长。", "左右翼均可撤回底边换线，中央盾位不封路。")),
            new FirstRegionLevelDefinition("depot_wreck", "废弃驿站", "清除占据驿站的敌人", CombatObjectiveType.Elimination, 1,
                new GridPosition(5, 4), FirstRegionFloorTheme.Ruins, false, false, Array.Empty<string>(),
                new[] { E("tether_hound", 1, 1), E("sigil_mauler", 10, 7), E("stone_snare", 10, 1) },
                new[] { H(3, 2), H(4, 2), H(7, 2), H(8, 2), H(3, 6), H(4, 6), H(7, 6), H(8, 6), L(2, 4), L(9, 4) },
                S("三口收束", new GridPosition(5, 1), new GridPosition(5, 7), "三股追击从不同方向到达，中路停留会叠加约束。", "上下两条宽口均可拆开寻迹兽、检验偶和助教的接触节奏。")),
            new FirstRegionLevelDefinition("relay_raid", "野外导能柱", "破坏被敌军占用的导能柱", CombatObjectiveType.Destruction, 2,
                new GridPosition(9, 0), FirstRegionFloorTheme.AetherMarked, false, false, new[] { "rail_patrol" },
                new[] { E("raider", 9, 2), E("rune_arbalist", 9, 4), E("tether_hound", 2, 6) },
                new[] { L(3, 2), L(7, 6), H(6, 1), H(5, 3), H(3, 5), H(6, 7), O(2, 4) },
                S("偏心目标争夺", new GridPosition(7, 1), new GridPosition(10, 3), "斜切目标会穿过公开重弩线；右侧减员路线允许寻迹兽靠近目标。", "目标破坏立即完成；玩家可先压制重弩再由下侧接近目标。")),
            new FirstRegionLevelDefinition("signal_hub", "传讯石庭", "清除传讯石庭守军", CombatObjectiveType.Elimination, 2,
                new GridPosition(1, 7), FirstRegionFloorTheme.Courtyard, false, false, new[] { "depot_wreck" },
                new[] { E("barrier_mender", 8, 1), E("lantern_revealer", 3, 2), E("shieldguard", 7, 6) },
                new[] { L(6, 1), L(3, 4), L(9, 7), H(5, 2), H(8, 4), H(4, 6) },
                S("三角维护网", new GridPosition(1, 4), new GridPosition(5, 8), "切助教、巡查员或盾线会留下另外两条公开维护关系。", "三角外围保持连通，可在看见显影和护障意图后换边。"),
                blockedPositions: DaisSides()),
            new FirstRegionLevelDefinition("gatehouse", "石闸关口", "夺取石闸关口", CombatObjectiveType.Elimination, 3,
                new GridPosition(6, 4), FirstRegionFloorTheme.Courtyard, false, false, new[] { "signal_hub", "relay_raid" },
                new[] { E("shieldguard", 2, 4), E("sigil_mauler", 9, 2), E("rune_arbalist", 10, 7) },
                new[] { L(5, 3), L(7, 4), L(6, 7), H(4, 1), H(4, 2), H(4, 5), H(4, 6), H(8, 1), H(8, 2), H(8, 5), H(8, 6) },
                S("双向门厅", new GridPosition(6, 1), new GridPosition(6, 8), "北通道靠近检验偶，南通道暴露于重弩；门厅内不能同时规避两者。", "两条通道均至少两格宽，并可从门厅中央改变方向。"),
                blockedPositions: DaisSides()),
            new FirstRegionLevelDefinition("transmission_tower", "传讯塔楼", "破坏塔楼内的敌方导能柱", CombatObjectiveType.Destruction, 3,
                new GridPosition(1, 1), FirstRegionFloorTheme.AetherMarked, false, false, new[] { "signal_hub" },
                new[] { E("pyromancer", 10, 1), E("stone_snare", 10, 7), E("lantern_revealer", 2, 7) },
                new[] { L(4, 2), L(4, 5), L(8, 4), H(5, 3), H(7, 3), H(7, 5), O(6, 4) },
                S("中央装置三扇区", new GridPosition(3, 3), new GridPosition(1, 6), "接近中心只能遮蔽部分远程压力，三个扇区的敌人意图均在首回合公开。", "目标破坏立即完成；可沿上扇区快拆或经左下扇区先处理巡查员。")),
            new FirstRegionLevelDefinition("elite_foundry", "刻阵工坊", "摧毁工坊内的敌方导能柱", CombatObjectiveType.Destruction, 3,
                new GridPosition(5, 8), FirstRegionFloorTheme.Ruins, true, false, new[] { "signal_hub" },
                new[] { E("elite_vanguard", 5, 3), E("barrier_mender", 2, 0), E("sigil_mauler", 2, 5) },
                new[] { L(8, 5), H(3, 1), H(6, 1), H(3, 2), H(6, 2), H(2, 4), H(3, 4), H(4, 4), H(7, 4), H(8, 4), H(9, 4), H(4, 6), H(7, 6), O(8, 0) },
                S("编织狭口", new GridPosition(3, 7), new GridPosition(8, 7), "左路承受检验偶破势，右路更快接近目标但会被教官转向拦截。", "两路在教官下方短暂连通；目标破坏立即完成且单一敌人不能封死双路。")),
            new FirstRegionLevelDefinition("core_approach", "塔前石庭", "清除古塔前庭守军", CombatObjectiveType.Elimination, 4,
                new GridPosition(1, 7), FirstRegionFloorTheme.Courtyard, true, false, new[] { "transmission_tower", "elite_foundry" },
                new[] { E("elite_vanguard", 5, 4), E("rune_arbalist", 10, 1), E("stone_snare", 9, 7) },
                new[] { L(1, 3), L(5, 7), L(9, 4), H(3, 1), H(4, 2), H(7, 5), H(8, 6), H(2, 6), H(3, 5), H(4, 4), H(6, 2), H(7, 1) },
                S("对角封线", new GridPosition(1, 4), new GridPosition(4, 8), "重弩与约束控制相反对角，中心教官惩罚直穿。", "外围低暴露路线较慢，中心两侧缺口允许在束缚公开后换线。")),
            new FirstRegionLevelDefinition("core_finale", "古塔核心", "击败古塔核心守备", CombatObjectiveType.Elimination, 5,
                new GridPosition(0, 4), FirstRegionFloorTheme.AetherMarked, false, true, new[] { "core_approach" },
                new[] { E(RegionBossToken, 6, 4), E("elite_vanguard", 6, 7), E("barrier_mender", 9, 2), E("lantern_revealer", 3, 1) },
                new[] { L(3, 3), L(3, 6), L(9, 4), L(9, 6), H(5, 3), H(7, 3), H(7, 5), H(5, 5) },
                S("中心核心与外围维护", new GridPosition(2, 2), new GridPosition(2, 6), "首领居中，三条外围维护链的作用与敌人意图在首回合可见。", "上下缺口分别通向巡查与教官侧，先拆哪条维护链决定后续环绕方向。"))
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
                if (string.IsNullOrWhiteSpace(level.SpaceContract.Grammar)) errors.Add(level.Id + ": missing space grammar");
                if (level.SpaceContract.RouteAnchors.Count < 2 || level.SpaceContract.RouteAnchors.Distinct().Count() < 2)
                    errors.Add(level.Id + ": fewer than two route anchors");
                if (string.IsNullOrWhiteSpace(level.SpaceContract.PublicRisk) || string.IsNullOrWhiteSpace(level.SpaceContract.CounterplayWindow))
                    errors.Add(level.Id + ": incomplete public space contract");
                if (level.SpaceContract.SupportedOpenings.Distinct().Count() != Enum.GetValues(typeof(LevelOpeningProfile)).Length)
                    errors.Add(level.Id + ": incomplete opening profile coverage");
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
                foreach (GridPosition blocked in level.BlockedPositions)
                {
                    if (!Inside(level, blocked)) errors.Add(level.Id + ": permanent blocker outside map");
                    if (!occupied.Add(blocked)) errors.Add(level.Id + ": permanent blocker overlaps occupied cell " + blocked);
                }
                foreach (GridPosition routeAnchor in level.SpaceContract.RouteAnchors)
                {
                    if (!Inside(level, routeAnchor)) errors.Add(level.Id + ": route anchor outside map");
                    if (occupied.Contains(routeAnchor)) errors.Add(level.Id + ": route anchor is occupied " + routeAnchor);
                    if (level.Terrain.Any(terrain => terrain.Position == routeAnchor && terrain.Kind == LevelTerrainKind.HeavyCover))
                        errors.Add(level.Id + ": route anchor is blocked by heavy cover " + routeAnchor);
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

        private static IReadOnlyList<GridPosition> DaisSides() => new[]
        {
            new GridPosition(3, 7), new GridPosition(4, 7), new GridPosition(7, 7), new GridPosition(8, 7),
            new GridPosition(3, 8), new GridPosition(4, 8), new GridPosition(7, 8), new GridPosition(8, 8)
        };
    }

    public static class FirstRegionLevelBuilder
    {
        public static FirstRegionLevelBuild Build(string levelId, string regionBossId = null) => Build(FirstRegionLevelCatalog.For(levelId), regionBossId);

        public static FirstRegionLevelBuild Build(FirstRegionLevelDefinition level, string regionBossId = null)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            GridMap map = new GridMap(level.Width, level.Height, level.BlockedPositions);
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
