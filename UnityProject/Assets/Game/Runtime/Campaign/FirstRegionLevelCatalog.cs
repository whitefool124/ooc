using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum FirstRegionFloorTheme { StoneRoad, Courtyard, Ruins, AetherMarked }
    public enum LevelTerrainKind { LightCover, HeavyCover, PermanentWall, AetherObjective, Water, LampVine, AetherCrystal, TowerMechanism,
        StakedStructure, OverloadDevice, CertifierStand, WardGenerator, LoosePaper, Trace, BindingMark }
    public enum LevelOpeningProfile { Melee, Ranged, Generalist }

    public sealed class LevelTerrainPlacement
    {
        public GridPosition Position { get; }
        public LevelTerrainKind Kind { get; }
        /// <summary>机关种类：1 护障维护 / 2 显影巡查 / 3 冲压隔离。非机关为 0。</summary>
        public int MechanismKind { get; }
        public LevelTerrainPlacement(int x, int y, LevelTerrainKind kind, int mechanismKind = 0)
        { Position = new GridPosition(x, y); Kind = kind; MechanismKind = mechanismKind; }
    }

    public sealed class LevelEnemyPlacement
    {
        public string ArchetypeId { get; }
        public GridPosition Position { get; }
        public LevelEnemyPlacement(string archetypeId, int x, int y)
        { ArchetypeId = archetypeId ?? throw new ArgumentNullException(nameof(archetypeId)); Position = new GridPosition(x, y); }
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
        /// <summary>塔内机关种类：护障维护。</summary>
        public const int TowerMechanismWard = 1;
        /// <summary>塔内机关种类：显影巡查。</summary>
        public const int TowerMechanismReveal = 2;
        /// <summary>塔内机关种类：冲压隔离。</summary>
        public const int TowerMechanismPress = 3;

        private static LevelTerrainPlacement L(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.LightCover);
        private static LevelTerrainPlacement H(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.HeavyCover);
        private static LevelTerrainPlacement O(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.AetherObjective);
        private static LevelTerrainPlacement W(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.Water);
        private static LevelTerrainPlacement V(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.LampVine);
        private static LevelTerrainPlacement C(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.AetherCrystal);
        private static LevelTerrainPlacement M(int mechanismKind, int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.TowerMechanism, mechanismKind);
        /// <summary>通用场地库条目：按元素种类直接落一格。</summary>
        private static LevelTerrainPlacement T(int x, int y, LevelTerrainKind kind) => new LevelTerrainPlacement(x, y, kind);
        private static LevelEnemyPlacement E(string id, int x, int y) => new LevelEnemyPlacement(id, x, y);
        private static LevelSpaceContract S(string grammar, GridPosition routeA, GridPosition routeB, string risk, string counterplay) =>
            new LevelSpaceContract(grammar, new[] { routeA, routeB }, risk, counterplay);

        public static readonly FirstRegionLevelDefinition RainLanternCourt =
            new FirstRegionLevelDefinition(RainLanternCourtRuntime.LevelId, "雨后灯庭", "让寻迹兽失去行动能力，并让高年级火矢生认输。", CombatObjectiveType.Elimination, 1,
                new GridPosition(1, 7), FirstRegionFloorTheme.Courtyard, false, false, Array.Empty<string>(),
                new[] { E("tether_hound", 7, 6), E("pyromancer", 8, 1) },
                new[]
                {
                    W(2, 6), W(3, 6), W(4, 6), W(5, 6), W(6, 6),
                    V(4, 0), V(5, 0), V(4, 1), V(5, 1), V(4, 2), V(5, 2), V(4, 3), V(5, 3), V(4, 4), V(5, 4),
                    L(2, 2), L(7, 3), L(2, 7),
                    H(0, 0), H(1, 0), H(11, 0), H(0, 1), H(11, 1),
                    H(0, 8), H(1, 8), H(2, 8), H(7, 8), H(8, 8), H(9, 8), H(10, 8), H(11, 8)
                },
                new LevelSpaceContract("雨后石庭、积水横带、双列灯藤",
                    new[] { new GridPosition(1, 6), new GridPosition(2, 5), new GridPosition(7, 7) },
                    "积水提高主角穿越成本；灯藤遮断远程视线，并会被火矢生依次烧开。",
                    "可沿西侧稳进、借积水灭火，或利用灯藤让寻迹兽进入公开的嗅探搜索。"));

        public static readonly FirstRegionLevelDefinition GreenhouseCollectionRoom =
            new FirstRegionLevelDefinition("first_battle_greenhouse_collection_room", "温室藏品间",
                "击倒替身偶与侧锋生；中央备件箱可搜刮苗床回流芯。", CombatObjectiveType.Elimination, 2,
                new GridPosition(1, 4), FirstRegionFloorTheme.Courtyard, false, false, new[] { RainLanternCourtRuntime.LevelId },
                new[] { E("raider", 9, 2), E("sigil_mauler", 9, 6) },
                new[]
                {
                    V(4, 3), V(5, 3), V(6, 3), V(4, 4), V(6, 4), V(4, 5), V(5, 5), V(6, 5),
                    C(8, 2), C(8, 6)
                },
                new LevelSpaceContract("中央藤圈宝箱、南北双晶簇、南侧普通长路",
                    new[] { new GridPosition(3, 4), new GridPosition(5, 6), new GridPosition(7, 8) },
                    "晶簇被摧毁时伤害正交邻格并生成五格碎晶；灯藤遮断攻击线。",
                    "可跃进抢箱、等待敌人贴晶引爆，或不依赖奖励沿南侧长路推进。"));

        public static readonly FirstRegionLevelDefinition RainPrismCourt =
            new FirstRegionLevelDefinition("first_b3_rain_prism_court", "雨痕晶庭",
                "击倒替身偶与火矢生；可破坏封门晶簇搜刮学院储能芯。", CombatObjectiveType.Elimination, 3,
                new GridPosition(1, 4), FirstRegionFloorTheme.Courtyard, false, false, new[] { GreenhouseCollectionRoom.Id },
                new[] { E("sigil_mauler", 5, 4), E("pyromancer", 6, 1) },
                new[]
                {
                    W(3, 1), W(3, 2), W(3, 3), W(3, 4), W(3, 5), W(3, 6), W(3, 7), W(4, 1), W(5, 1),
                    C(6, 4), H(7, 3), H(7, 5), H(8, 4), L(1, 2), L(1, 6), L(8, 2), L(9, 6)
                },
                new LevelSpaceContract("冷却沟、封门晶簇、南侧干路",
                    new[] { new GridPosition(4, 4), new GridPosition(3, 8), new GridPosition(5, 2) },
                    "火矢施加燃烧；封门晶簇阻挡器材匣唯一入口。",
                    "可破晶开匣、入水熄火反压，或从 D9 干路绕行。"));

        public static readonly FirstRegionLevelDefinition ThreeMaterialPressure =
            new FirstRegionLevelDefinition("first_elite_three_material_pressure", "三材承压场",
                "击倒楔角。", CombatObjectiveType.Elimination, 4,
                new GridPosition(1, 4), FirstRegionFloorTheme.Courtyard, true, false, new[] { RainPrismCourt.Id },
                new[] { E("breach_ram", 7, 4) },
                new[]
                {
                    W(3, 1), W(3, 2), W(3, 3), W(3, 4), W(3, 5), W(3, 6), W(3, 7),
                    V(4, 2), V(5, 2), V(6, 2), V(4, 5), V(5, 5), V(6, 5), C(6, 4)
                },
                new LevelSpaceContract("冷却沟、双灯藤带、中央稳压晶簇",
                    new[] { new GridPosition(3, 8), new GridPosition(2, 4), new GridPosition(8, 4) },
                    "楔角公开锁定冲压线，并会撞击晶簇、单位或灯藤。",
                    "可诱导撞晶、藏入灯藤、用浅水缩短冲压，或走 D9 干路等待卸压。"));

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
                S("三口收束", new GridPosition(5, 1), new GridPosition(5, 7), "三股追击从不同方向到达，中路停留会叠加约束。", "上下两条宽口均可拆开寻迹兽、替身偶和拴索助教的接触节奏。")),
            new FirstRegionLevelDefinition("relay_raid", "野外导能柱", "破坏被敌军占用的导能柱", CombatObjectiveType.Destruction, 2,
                new GridPosition(9, 0), FirstRegionFloorTheme.AetherMarked, false, false, new[] { "rail_patrol" },
                new[] { E("raider", 9, 2), E("rune_arbalist", 9, 4), E("tether_hound", 2, 6) },
                new[] { L(3, 2), L(7, 6), H(6, 1), H(5, 3), H(3, 5), H(6, 7), O(2, 4) },
                S("偏心目标争夺", new GridPosition(7, 1), new GridPosition(10, 3), "斜切目标会穿过公开背弩生线；右侧减员路线允许寻迹兽靠近目标。", "目标破坏立即完成；玩家可先压制背弩生再由下侧接近目标。")),
            new FirstRegionLevelDefinition("signal_hub", "传讯石庭", "清除传讯石庭守军", CombatObjectiveType.Elimination, 2,
                new GridPosition(1, 7), FirstRegionFloorTheme.Courtyard, false, false, new[] { "depot_wreck" },
                new[] { E("barrier_mender", 8, 1), E("lantern_revealer", 3, 2), E("shieldguard", 7, 6) },
                new[] { L(6, 1), L(3, 4), L(9, 7), H(5, 2), H(8, 4), H(4, 6) },
                S("三角维护网", new GridPosition(1, 4), new GridPosition(5, 8), "切补盾助教、提灯巡查或盾线会留下另外两条公开维护关系。", "三角外围保持连通，可在看见显影和护障意图后换边。"),
                blockedPositions: DaisSides()),
            new FirstRegionLevelDefinition("gatehouse", "石闸关口", "夺取石闸关口", CombatObjectiveType.Elimination, 3,
                new GridPosition(6, 4), FirstRegionFloorTheme.Courtyard, false, false, new[] { "signal_hub", "relay_raid" },
                new[] { E("shieldguard", 2, 4), E("sigil_mauler", 9, 2), E("rune_arbalist", 10, 7) },
                new[] { L(5, 3), L(7, 4), L(6, 7), H(4, 1), H(4, 2), H(4, 5), H(4, 6), H(8, 1), H(8, 2), H(8, 5), H(8, 6) },
                S("双向门厅", new GridPosition(6, 1), new GridPosition(6, 8), "北通道靠近替身偶，南通道暴露于背弩生；门厅内不能同时规避两者。", "两条通道均至少两格宽，并可从门厅中央改变方向。"),
                blockedPositions: DaisSides()),
            new FirstRegionLevelDefinition("transmission_tower", "传讯塔楼", "破坏塔楼内的敌方导能柱", CombatObjectiveType.Destruction, 3,
                new GridPosition(1, 1), FirstRegionFloorTheme.AetherMarked, false, false, new[] { "signal_hub" },
                new[] { E("pyromancer", 10, 1), E("stone_snare", 10, 7), E("lantern_revealer", 2, 7) },
                new[] { L(4, 2), L(4, 5), L(8, 4), H(5, 3), H(7, 3), H(7, 5), O(6, 4) },
                S("中央装置三扇区", new GridPosition(3, 3), new GridPosition(1, 6), "接近中心只能遮蔽部分远程压力，三个扇区的敌人意图均在首回合公开。", "目标破坏立即完成；可沿上扇区快拆或经左下扇区先处理提灯巡查。")),
            new FirstRegionLevelDefinition("elite_foundry", "刻阵工坊", "摧毁工坊内的敌方导能柱", CombatObjectiveType.Destruction, 3,
                new GridPosition(5, 8), FirstRegionFloorTheme.Ruins, true, false, new[] { "signal_hub" },
                new[] { E("elite_vanguard", 5, 3), E("barrier_mender", 2, 0), E("sigil_mauler", 2, 5) },
                new[] { L(8, 5), H(3, 1), H(6, 1), H(3, 2), H(6, 2), H(2, 4), H(3, 4), H(4, 4), H(7, 4), H(8, 4), H(9, 4), H(4, 6), H(7, 6), O(8, 0) },
                S("编织狭口", new GridPosition(3, 7), new GridPosition(8, 7), "左路承受替身偶破势，右路更快接近目标但会被划线教官横移拦截。", "两路在划线教官下方短暂连通；目标破坏立即完成且单一敌人不能封死双路。")),
            new FirstRegionLevelDefinition("core_approach", "塔前石庭", "清除古塔前庭守军", CombatObjectiveType.Elimination, 4,
                new GridPosition(1, 7), FirstRegionFloorTheme.Courtyard, true, false, new[] { "transmission_tower", "elite_foundry" },
                new[] { E("elite_vanguard", 5, 4), E("rune_arbalist", 10, 1), E("stone_snare", 9, 7) },
                new[] { L(1, 3), L(5, 7), L(9, 4), H(3, 1), H(4, 2), H(7, 5), H(8, 6), H(2, 6), H(3, 5), H(4, 4), H(6, 2), H(7, 1) },
                S("对角封线", new GridPosition(1, 4), new GridPosition(4, 8), "背弩生与约束控制相反对角，中心划线教官惩罚直穿。", "外围低暴露路线较慢，中心两侧缺口允许在束缚公开后换线。")),
            new FirstRegionLevelDefinition("core_finale", "古塔核心", "击败拦在必经之路上的塔之守卫", CombatObjectiveType.Elimination, 5,
                new GridPosition(0, 4), FirstRegionFloorTheme.AetherMarked, false, true, new[] { "core_approach" },
                new[] { E(RegionBossToken, 6, 4) },
                new[] { L(3, 3), L(3, 6), L(9, 4), L(9, 6), H(5, 3), H(7, 3), H(7, 5), H(5, 5),
                    M(TowerMechanismWard, 9, 2), M(TowerMechanismReveal, 3, 1), M(TowerMechanismPress, 6, 7) },
                S("中心核心与三道机关门槛", new GridPosition(2, 2), new GridPosition(2, 6), "塔之守卫居中；三组塔内机关（护障维护、显影巡查、冲压隔离）在三个阶段内逐组放行，放行前不提供任何效果。", "三组机关是塔之守卫的施术介质，耐久公开、可以先拆；拆掉已放行的机关即切断对应加成。"))
        };
        /// <summary>五张新精英底图中的三张：底图 id 与地图节点 id 解耦，由遭遇变体引用。</summary>
        public static readonly IReadOnlyList<FirstRegionLevelDefinition> NewEliteLevels = new[]
        {
            new FirstRegionLevelDefinition("calibration_lockdown", "失控校准室封锁",
                "击倒试制员、背弩生与拴索助教；扛过他的装置库存。", CombatObjectiveType.Elimination, 3,
                new GridPosition(1, 4), FirstRegionFloorTheme.Ruins, true, false, new[] { "rail_patrol", "depot_wreck" },
                new[] { E("prototype_hand", 8, 4), E("rune_arbalist", 10, 1), E("stone_snare", 10, 6) },
                new[]
                {
                    H(4, 1), H(4, 2), H(4, 5), H(4, 6), H(7, 1), H(7, 2), H(7, 5), H(7, 6),
                    T(5, 4, LevelTerrainKind.StakedStructure), T(6, 3, LevelTerrainKind.CertifierStand),
                    T(2, 2, LevelTerrainKind.WardGenerator), T(2, 6, LevelTerrainKind.OverloadDevice),
                    W(3, 3), W(3, 4)
                },
                S("设备间的校准台走廊、冷却沟、两处既有装置", new GridPosition(1, 2), new GridPosition(1, 7),
                    "走廊只有两格宽，试制员会一段段放上试制件堵住换线；背弩生的长线覆盖整条走廊，拴索助教刻住地面。",
                    "先拆试制件比追人便宜，也可以把过载区反用成他的坟；冷却沟能洗掉地面刻印，走廊外侧另有绕行通路。"),
                blockedPositions: CalibrationFrame()),
            new FirstRegionLevelDefinition("cliff_relay_survey", "断崖导能柱考察",
                "击倒老寻、灯台值守与补盾助教；痕迹暴露后强攻。", CombatObjectiveType.Elimination, 3,
                new GridPosition(1, 7), FirstRegionFloorTheme.Ruins, true, false, new[] { "rail_patrol", "depot_wreck" },
                new[] { E("elder_tracker_hound", 8, 7), E("signal_keeper", 10, 4), E("barrier_mender", 10, 1) },
                new[]
                {
                    W(2, 5), W(3, 5), W(4, 5),
                    V(7, 2), V(8, 2), V(7, 3), V(8, 3),
                    H(9, 3), H(9, 4), T(2, 3, LevelTerrainKind.StakedStructure), T(4, 1, LevelTerrainKind.StakedStructure),
                    T(6, 6, LevelTerrainKind.LoosePaper), T(7, 6, LevelTerrainKind.LoosePaper),
                    T(5, 7, LevelTerrainKind.CertifierStand)
                },
                S("断崖坡面、坡面积水带、双列灯藤、导能柱基座", new GridPosition(1, 4), new GridPosition(4, 8),
                    "灯台值守的光柱封住中廊，只有重掩体与基座之后才留暗段；老寻沿主角留下的气味痕提速，补盾助教贴着标定结构续盾。",
                    "走坡面积水冲掉气味痕，或借重掩体与基座的暗段绕开光柱，再先拆补盾助教依赖的标定结构。"),
                blockedPositions: CliffEdge()),
            new FirstRegionLevelDefinition("sealed_vault_certification", "封存库权限核验",
                "击倒老库管、提灯巡查与替身偶；他退你的场地吃你的行动点。", CombatObjectiveType.Elimination, 3,
                new GridPosition(1, 6), FirstRegionFloorTheme.AetherMarked, true, false, new[] { "relay_raid", "signal_hub" },
                new[] { E("legacy_storekeeper", 9, 4), E("lantern_revealer", 10, 1), E("sigil_mauler", 6, 6) },
                new[]
                {
                    H(5, 1), H(5, 2), H(5, 6), H(5, 7), H(7, 3), H(7, 4), H(7, 5),
                    T(4, 2, LevelTerrainKind.CertifierStand), T(8, 2, LevelTerrainKind.CertifierStand),
                    T(6, 4, LevelTerrainKind.WardGenerator),
                    T(3, 3, LevelTerrainKind.LoosePaper), T(3, 4, LevelTerrainKind.LoosePaper),
                    W(2, 2), W(2, 3)
                },
                S("旧检定台与读数桩之间的库房通道、货架与消防积水", new GridPosition(1, 4), new GridPosition(3, 7),
                    "老库管会退掉你刚放下的东西，旧脉冲沿通道一次扫到多人；提灯巡查沿直线显影并施加破势，替身偶贴身穿插。",
                    "先拆两座检定台再动手，或在登记生效前抢输出；积水能洗掉场地效果，货架把通道切成可以换线的两段。"),
                blockedPositions: VaultFrame()),
            new FirstRegionLevelDefinition("library_discipline", "图书馆纠察",
                "击倒小铃、补盾助教与替身偶；现场纠察逾期与喧哗。", CombatObjectiveType.Elimination, 3,
                new GridPosition(1, 3), FirstRegionFloorTheme.Courtyard, true, false, new[] { "rail_patrol", "relay_raid" },
                new[] { E("wind_librarian", 9, 2), E("barrier_mender", 9, 6), E("sigil_mauler", 5, 4) },
                new[]
                {
                    H(3, 1), H(3, 2), H(6, 1), H(6, 2), H(3, 6), H(3, 7), H(6, 6), H(6, 7),
                    T(4, 4, LevelTerrainKind.StakedStructure), T(6, 4, LevelTerrainKind.StakedStructure),
                    T(2, 2, LevelTerrainKind.LoosePaper), T(2, 6, LevelTerrainKind.LoosePaper),
                    T(5, 1, LevelTerrainKind.LoosePaper), T(5, 7, LevelTerrainKind.LoosePaper),
                    T(8, 4, LevelTerrainKind.BindingMark)
                },
                S("高书架切出的走廊、阅览长桌、散页堆", new GridPosition(1, 2), new GridPosition(1, 6),
                    "书架把场地切成走廊，穿过页幕与书架的攻击线都会失效；补盾助教贴着长桌续盾，小铃用散页立幕与打风刃。",
                    "先烧掉或打湿散页断掉她扬页与卷页的材料，再从另一条走廊压上；长桌是可拆的结构锚点。")),
            new FirstRegionLevelDefinition("outer_ring_clearance", "高塔外环清障",
                "击倒划线教官、提灯巡查与背弩生；在直线光照下处理物块与对手。", CombatObjectiveType.Elimination, 3,
                new GridPosition(1, 5), FirstRegionFloorTheme.AetherMarked, true, false, new[] { "relay_raid", "signal_hub" },
                new[] { E("elite_vanguard", 7, 4), E("lantern_revealer", 10, 6), E("rune_arbalist", 10, 2) },
                new[]
                {
                    H(4, 4), H(6, 4), H(5, 1), H(5, 7),
                    T(3, 4, LevelTerrainKind.StakedStructure), T(7, 3, LevelTerrainKind.StakedStructure),
                    T(6, 6, LevelTerrainKind.LoosePaper), T(6, 2, LevelTerrainKind.LoosePaper),
                    W(4, 7), T(5, 4, LevelTerrainKind.CertifierStand), T(3, 1, LevelTerrainKind.WardGenerator)
                },
                S("高塔外环直线廊道、中央物块堆、两处标定结构", new GridPosition(1, 6), new GridPosition(4, 8),
                    "外环是直线，背弩生重矢与提灯巡查的显影沿同一条廊道叠加；划线教官会现场改掩体，物块堆一旦被拆就没有换线空间。",
                    "借重掩体遮断显影线，或先拆标定结构与护罩发生器再压上；中央物块堆可以拆出一条新路。"),
                blockedPositions: RingWalls()),
        };

        private static readonly IReadOnlyDictionary<string, FirstRegionLevelDefinition> byId =
            All.Concat(new[] { RainLanternCourt, GreenhouseCollectionRoom, RainPrismCourt, ThreeMaterialPressure })
                .Concat(NewEliteLevels).ToDictionary(level => level.Id, StringComparer.Ordinal);

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
            foreach (FirstRegionLevelDefinition level in All.Concat(new[] { RainLanternCourt, GreenhouseCollectionRoom, RainPrismCourt, ThreeMaterialPressure }).Concat(NewEliteLevels))
            {
                if (level.Width != 12 || level.Height != 9) errors.Add(level.Id + ": map must be 12x9");
                if (!Inside(level, level.HeroSpawn)) errors.Add(level.Id + ": hero spawn outside map");
                int minimumEnemies = level.IsBoss || level.Id == ThreeMaterialPressure.Id ? 1 :
                    level.Id == RainLanternCourtRuntime.LevelId || level.Id == GreenhouseCollectionRoom.Id || level.Id == RainPrismCourt.Id ? 2 : 3;
                if (level.EnemyPlacements.Count < minimumEnemies) errors.Add(level.Id + ": too few enemies");
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
                    if (level.Terrain.Any(terrain => terrain.Position == routeAnchor &&
                        (terrain.Kind == LevelTerrainKind.HeavyCover || terrain.Kind == LevelTerrainKind.PermanentWall)))
                        errors.Add(level.Id + ": route anchor is blocked by terrain " + routeAnchor);
                }
                int objectiveCount = level.Terrain.Count(tile => tile.Kind == LevelTerrainKind.AetherObjective);
                if (level.ObjectiveType == CombatObjectiveType.Destruction && objectiveCount == 0) errors.Add(level.Id + ": destruction objective has no target");
                if (level.ObjectiveType == CombatObjectiveType.Elimination && objectiveCount != 0) errors.Add(level.Id + ": elimination level has a destruction target");
                int mechanismCount = level.Terrain.Count(tile => tile.Kind == LevelTerrainKind.TowerMechanism);
                if (level.IsBoss)
                {
                    if (mechanismCount != 3) errors.Add(level.Id + ": boss level must have exactly three tower mechanisms");
                    foreach (int kind in new[] { TowerMechanismWard, TowerMechanismReveal, TowerMechanismPress })
                        if (!level.Terrain.Any(tile => tile.Kind == LevelTerrainKind.TowerMechanism && tile.MechanismKind == kind))
                            errors.Add(level.Id + ": boss level missing tower mechanism kind " + kind);
                }
                else if (mechanismCount != 0) errors.Add(level.Id + ": tower mechanisms are boss-only");
                foreach (string prerequisite in level.PrerequisiteLevelIds)
                    if (!byId.TryGetValue(prerequisite, out FirstRegionLevelDefinition required)) errors.Add(level.Id + ": unknown prerequisite " + prerequisite);
                    else if (required.Tier >= level.Tier) errors.Add(level.Id + ": prerequisite is not earlier tier " + prerequisite);
            }
            return errors;
        }

        private static bool Inside(FirstRegionLevelDefinition level, GridPosition position) =>
            position.X >= 0 && position.X < level.Width && position.Y >= 0 && position.Y < level.Height;

        /// <summary>校准室两端的机台端墙：把走廊收成一条两格宽的通道。</summary>
        private static IReadOnlyList<GridPosition> CalibrationFrame() => new[]
        {
            new GridPosition(4, 0), new GridPosition(5, 0), new GridPosition(6, 0), new GridPosition(7, 0),
            new GridPosition(4, 8), new GridPosition(5, 8), new GridPosition(6, 8), new GridPosition(7, 8)
        };

        /// <summary>封存库的门框：库房通道只从两端进出。</summary>
        private static IReadOnlyList<GridPosition> VaultFrame() => new[]
        {
            new GridPosition(4, 0), new GridPosition(5, 0), new GridPosition(6, 0),
            new GridPosition(4, 8), new GridPosition(5, 8), new GridPosition(6, 8)
        };
        /// <summary>断崖考察的崖面与脊线：把坡面切成上下两段，只在两端留通道。</summary>
        private static IReadOnlyList<GridPosition> CliffEdge() => new[]
        {
            new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0), new GridPosition(3, 0), new GridPosition(4, 0),
            new GridPosition(5, 2), new GridPosition(5, 3), new GridPosition(5, 4), new GridPosition(5, 5)
        };

        /// <summary>高塔外环的两道外墙：压出直线廊道，只在两端留通道。</summary>
        private static IReadOnlyList<GridPosition> RingWalls() => new[]
        {
            new GridPosition(2, 2), new GridPosition(2, 3), new GridPosition(2, 4), new GridPosition(2, 5),
            new GridPosition(9, 2), new GridPosition(9, 3), new GridPosition(9, 4), new GridPosition(9, 5)
        };
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
                    case LevelTerrainKind.LightCover: map.SetTile(placement.Position, new TileState { Cover = CoverType.Light, Durability = TileState.LightDurability }); break;
                    case LevelTerrainKind.HeavyCover: map.SetTile(placement.Position, new TileState { Cover = CoverType.Heavy, Durability = TileState.HeavyDurability }); break;
                    case LevelTerrainKind.PermanentWall: map.SetTile(placement.Position, new TileState { IsPermanentWall = true }); break;
                    case LevelTerrainKind.AetherObjective: map.SetTile(placement.Position, new TileState { IsObjective = true, IsDevice = true, Durability = TileState.StandardDurability }); break;
                    case LevelTerrainKind.Water: map.SetTile(placement.Position, new TileState { IsWater = true }); break;
                    case LevelTerrainKind.LampVine: map.SetTile(placement.Position, new TileState { IsLampVine = true, Durability = TileState.FragileDurability }); break;
                    case LevelTerrainKind.AetherCrystal: map.SetTile(placement.Position, new TileState { IsDevice = true, IsAetherCrystal = true, Durability = TileState.StandardDurability }); break;
                    case LevelTerrainKind.TowerMechanism: map.SetTile(placement.Position, new TileState { IsDevice = true, IsTowerMechanism = true, MechanismKind = placement.MechanismKind, Durability = TileState.HeavyDurability }); break;
                    case LevelTerrainKind.StakedStructure: map.SetTile(placement.Position, new TileState { Cover = CoverType.Heavy, IsStakedStructure = true, Durability = TileState.StakedDurability }); break;
                    case LevelTerrainKind.OverloadDevice: map.SetTile(placement.Position, new TileState { IsDevice = true, IsOverloadDevice = true, Durability = TileState.StandardDurability }); break;
                    case LevelTerrainKind.CertifierStand: map.SetTile(placement.Position, new TileState { IsDevice = true, IsCertifierStand = true, Durability = TileState.StandardDurability }); break;
                    case LevelTerrainKind.WardGenerator: map.SetTile(placement.Position, new TileState { IsDevice = true, IsWardGenerator = true, Durability = TileState.StandardDurability }); break;
                    case LevelTerrainKind.LoosePaper: map.SetTile(placement.Position, new TileState { IsLoosePaper = true }); break;
                    case LevelTerrainKind.Trace: map.SetTile(placement.Position, new TileState { HasTrace = true }); break;
                    case LevelTerrainKind.BindingMark: map.SetTile(placement.Position, new TileState { IsBindingMark = true }); break;
                }
            }

            List<UnitState> units = new List<UnitState>();
            UnitState hero = new UnitState("hero", true, level.HeroSpawn) { DisplayName = "维克多·维恩", Speed = 11 };
            hero.Equip(CombatCatalog.Hammer, CombatCatalog.Shield, CombatCatalog.FireBolt, CombatCatalog.FrostBind);
            units.Add(hero);
            IReadOnlyList<string> resolvedIds = level.ResolveEnemyArchetypeIds(regionBossId);
            for (int index = 0; index < level.EnemyPlacements.Count; index++)
            {
                LevelEnemyPlacement placement = level.EnemyPlacements[index];
                UnitState enemy = new UnitState("enemy_" + index, false, placement.Position);
                EnemyArchetypes.Get(resolvedIds[index]).Apply(enemy);
                if (level.Id == RainLanternCourtRuntime.LevelId)
                {
                    if (enemy.EnemyArchetypeId == "tether_hound") enemy.ConfigureVitality(12);
                    if (enemy.EnemyArchetypeId == "pyromancer") enemy.ConfigureVitality(16);
                }
                if (level.Id == FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id)
                {
                    enemy.ConfigureVitality(16);
                    enemy.Speed = enemy.EnemyArchetypeId == "raider" ? 11 : 8;
                }
                if (level.Id == FirstRegionLevelCatalog.RainPrismCourt.Id)
                {
                    enemy.ConfigureVitality(16);
                    enemy.Speed = enemy.EnemyArchetypeId == "pyromancer" ? 9 : 8;
                }
                units.Add(enemy);
            }

            CombatObjective objective = level.ObjectiveType == CombatObjectiveType.Destruction
                ? (CombatObjective)new DestructionObjective(map.PositionsWith(tile => tile.IsObjective), level.Id + "_objective")
                : new EliminationObjective(level.Id + "_objective");
            CombatState state = new CombatState(map, units, new[] { objective });
            if (level.Id == RainLanternCourtRuntime.LevelId) state.AttachRainLanternCourt(new RainLanternCourtRuntime());
            if (level.Id == FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id)
            {
                state.AttachGreenhouseCollectionRoom(new GreenhouseCollectionRoomRuntime());
                state.SetLootSource(new LootSourceState("FIRST-B2-CENTRAL-CHEST", new GridPosition(5, 4),
                    new[] { new ItemInstance("FIRST-B2-ACA-EQ-CR04", "ACA-EQ-CR04", 0) }));
            }
            if (level.Id == FirstRegionLevelCatalog.RainPrismCourt.Id)
                state.SetLootSource(new LootSourceState("FIRST-B3-CHEST", new GridPosition(7, 4),
                    new[] { new ItemInstance("FIRST-B3-ACA-EQ-CR01", "ACA-EQ-CR01", 0) }));
            if (level.Id == FirstRegionLevelCatalog.ThreeMaterialPressure.Id)
            {
                state.Map.GetTile(new GridPosition(6, 4)).Durability = TileState.HeavyDurability;
                state.AttachThreeMaterialPressure(new ThreeMaterialPressureRuntime());
            }
            // 场上只要有通用场地敌人（老寻／灯台值守／小铃），就接上公开条件反应运行时。
            if (state.Units.Values.Any(unit => AcademyFieldEnemyRuntime.Handles(unit)))
                state.AttachAcademyFieldEnemy(new AcademyFieldEnemyRuntime());
            if (level.IsBoss && state.Units.Values.Any(unit => unit.EnemyArchetypeId == "core_overseer"))
                state.AttachAcademyCoreBoss(new AcademyCoreBossRuntime());
            return new FirstRegionLevelBuild(level, state);
        }
    }
}
