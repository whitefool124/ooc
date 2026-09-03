using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum RogueliteEncounterTier { Weak, Strong, Elite, Boss }

    public sealed class RogueliteEncounterLayout
    {
        public int Width { get; }
        public int Height { get; }
        public GridPosition HeroSpawn { get; }
        public IReadOnlyList<GridPosition> EnemySpawns { get; }
        public IReadOnlyList<LevelTerrainPlacement> Terrain { get; }
        public IReadOnlyList<GridPosition> BlockedPositions { get; }
        public string Signature { get; }
        public RogueliteEncounterLayout(string signature, GridPosition heroSpawn,
            IEnumerable<GridPosition> enemySpawns, IEnumerable<LevelTerrainPlacement> terrain,
            int width = 12, int height = 9, IEnumerable<GridPosition> blockedPositions = null)
        {
            Signature = signature;
            HeroSpawn = heroSpawn;
            EnemySpawns = enemySpawns.ToArray();
            Terrain = terrain.ToArray();
            Width = width;
            Height = height;
            BlockedPositions = (blockedPositions ?? Array.Empty<GridPosition>()).Distinct().ToArray();
        }
    }

    public sealed class RogueliteEncounterDefinition
    {
        public string NodeId { get; }
        public string VariantKey { get; }
        public string LevelId { get; }
        public RogueliteEncounterTier Tier { get; }
        public string SpatialGrammar { get; }
        public string SpawnRelationship { get; }
        public string PublicRisk { get; }
        public string RewardTier { get; }
        public int MaximumOpeningThreatOverlap { get; }
        public RogueliteEncounterLayout Layout { get; }
        public string ObjectiveSummary => Tier == RogueliteEncounterTier.Weak
            ? "让所有对手认输。教员会在有人重伤前叫停演练。" : string.Empty;
        public IReadOnlyList<string> EnemyArchetypeIds { get; }
        public bool IsElite => Tier == RogueliteEncounterTier.Elite;
        public bool IsBoss => Tier == RogueliteEncounterTier.Boss;

        public RogueliteEncounterDefinition(string variantKey, string levelId, RogueliteEncounterTier tier,
            string spatialGrammar, string spawnRelationship, string publicRisk, string rewardTier,
            int maximumOpeningThreatOverlap, params string[] enemyArchetypeIds)
            : this(levelId, variantKey, levelId, tier, spatialGrammar, spawnRelationship, publicRisk,
                rewardTier, maximumOpeningThreatOverlap, null, enemyArchetypeIds) { }

        public RogueliteEncounterDefinition(string variantKey, string levelId, RogueliteEncounterTier tier,
            string spatialGrammar, string spawnRelationship, string publicRisk, string rewardTier,
            int maximumOpeningThreatOverlap, RogueliteEncounterLayout layout, params string[] enemyArchetypeIds)
            : this(levelId, variantKey, levelId, tier, spatialGrammar, spawnRelationship, publicRisk,
                rewardTier, maximumOpeningThreatOverlap, layout, enemyArchetypeIds) { }

        private RogueliteEncounterDefinition(string nodeId, string variantKey, string levelId,
            RogueliteEncounterTier tier, string spatialGrammar, string spawnRelationship,
            string publicRisk, string rewardTier, int maximumOpeningThreatOverlap, RogueliteEncounterLayout layout,
            IReadOnlyList<string> enemyArchetypeIds)
        {
            NodeId = nodeId; VariantKey = variantKey; LevelId = levelId; Tier = tier;
            SpatialGrammar = spatialGrammar; SpawnRelationship = spawnRelationship;
            PublicRisk = publicRisk; RewardTier = rewardTier;
            MaximumOpeningThreatOverlap = maximumOpeningThreatOverlap;
            Layout = layout;
            EnemyArchetypeIds = enemyArchetypeIds ?? Array.Empty<string>();
        }

        internal RogueliteEncounterDefinition BindToNode(string nodeId) =>
            new RogueliteEncounterDefinition(nodeId, VariantKey, LevelId, Tier, SpatialGrammar,
                SpawnRelationship, PublicRisk, RewardTier, MaximumOpeningThreatOverlap, Layout, EnemyArchetypeIds);
    }

    public sealed class RogueliteEncounterAssignment
    {
        public string NodeId { get; }
        public string VariantKey { get; }
        public RogueliteEncounterAssignment(string nodeId, string variantKey)
        { NodeId = nodeId ?? string.Empty; VariantKey = variantKey ?? string.Empty; }
        public override string ToString() => NodeId + "=" + VariantKey;
    }

    public static class RogueliteEncounterCatalog
    {
        private static RogueliteEncounterDefinition Weak(string key, string map, string grammar,
            string relation, string risk, RogueliteEncounterLayout layout, params string[] enemies) =>
            new RogueliteEncounterDefinition(key, map, RogueliteEncounterTier.Weak, grammar, relation,
                "轻松", "基础奖励", 1, layout, enemies);
        private static RogueliteEncounterDefinition Strong(string key, string map, string grammar,
            string relation, string risk, params string[] enemies) =>
            new RogueliteEncounterDefinition(key, map, RogueliteEncounterTier.Strong, grammar, relation,
                "棘手", "更好的奖励", 2, enemies);
        private static RogueliteEncounterDefinition Elite(string key, string map, string grammar,
            string relation, params string[] enemies) =>
            new RogueliteEncounterDefinition(key, map, RogueliteEncounterTier.Elite, grammar, relation,
                "危险", "稀有奖励", 2, enemies);

        private static LevelTerrainPlacement L(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.LightCover);
        private static LevelTerrainPlacement H(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.HeavyCover);
        private static RogueliteEncounterLayout W1() => new RogueliteEncounterLayout("W1",
            new GridPosition(5, 7), new[] { new GridPosition(3, 1), new GridPosition(8, 1) },
            new[] { L(4, 2), L(7, 2), H(5, 4), L(2, 5), L(9, 5) });
        private static RogueliteEncounterLayout W2() => new RogueliteEncounterLayout("W2",
            new GridPosition(4, 7), new[] { new GridPosition(10, 0), new GridPosition(10, 6) },
            new[] { H(6, 1), H(6, 2), L(9, 2), H(2, 3), H(3, 3), H(4, 3), H(5, 3), H(6, 3), L(3, 5), H(6, 5), H(6, 6) });
        private static RogueliteEncounterLayout W3() => new RogueliteEncounterLayout("W3",
            new GridPosition(5, 7), new[] { new GridPosition(3, 1), new GridPosition(8, 1) },
            new[] { H(4, 2), H(5, 2), H(6, 2), H(7, 2), H(3, 3), H(4, 3), H(5, 3), H(6, 3), H(7, 3), H(8, 3), H(3, 4), H(4, 4), H(5, 4), H(6, 4), H(7, 4), H(8, 4), H(3, 5), H(4, 5), H(5, 5), H(6, 5), H(7, 5), H(8, 5), L(2, 6), L(9, 6) });
        private static RogueliteEncounterLayout W4() => new RogueliteEncounterLayout("W4",
            new GridPosition(2, 7), new[] { new GridPosition(9, 0), new GridPosition(10, 6) },
            new[] { L(7, 1), H(3, 2), H(4, 2), H(3, 3), H(4, 3), L(7, 3), L(4, 5), H(7, 5), H(8, 5), H(7, 6), H(8, 6), L(4, 7) });

        public static readonly IReadOnlyList<RogueliteEncounterDefinition> Packages = new[]
        {
            Weak("weak_flank_drill", "rail_patrol", "半开放演练场", "盾术生守在中间，侧锋从一侧接近；身后有退路", "轻松", W1(), "raider", "shieldguard"),
            Weak("weak_fire_drill", "relay_raid", "双台校准场", "火矢生留在远处，侧锋从旁接近；起步位置很安全", "轻松", W4(), "pyromancer", "raider"),
            Weak("weak_tracker_test", "depot_wreck", "环形练习场", "寻迹兽与盾术生分守两侧", "轻松", W3(), "tether_hound", "shieldguard"),
            Weak("weak_barrier_demo", "signal_hub", "环形练习场", "护障助教和盾术生隔着设施相互照应", "轻松", W3(), "barrier_mender", "shieldguard"),
            Weak("weak_arbalist_calibration", "gatehouse", "回廊折角", "重弩守在远端，侧锋守着下方通道", "轻松", W2(), "rune_arbalist", "raider"),
            Weak("weak_restraint_exam", "transmission_tower", "回廊折角", "约束助教守在远端，侧锋守着另一处出口", "轻松", W2(), "stone_snare", "raider"),

            Strong("strong_rail_patrol_a", "rail_patrol", "开阔交叉线", "盾术居中，火矢与侧锋分列两翼", "中高：主动进入的近远交叉", "shieldguard", "pyromancer", "raider"),
            Strong("strong_rail_patrol_b", "rail_patrol", "开阔交叉线·反向翼位", "盾术前压，两翼远近职责反置", "中高：两翼压力与中央暴露", "shieldguard", "pyromancer", "raider"),
            Strong("strong_depot_wreck_a", "depot_wreck", "三口收束", "双束缚源错层，检验偶守中口", "高：束缚后贴身破势", "tether_hound", "sigil_mauler", "stone_snare"),
            Strong("strong_depot_wreck_b", "depot_wreck", "三口收束·偏置中口", "寻迹兽近翼、助教远翼、检验偶偏心", "高：换路时的双束缚压力", "tether_hound", "sigil_mauler", "stone_snare"),
            Strong("strong_relay_raid_a", "relay_raid", "偏心目标", "目标两侧为侧锋与寻迹兽，重弩远守", "高：双接近与公开重弩线", "raider", "rune_arbalist", "tether_hound"),
            Strong("strong_relay_raid_b", "relay_raid", "偏心目标·对角争夺", "重弩与目标对角，双近战分守两路", "高：目标争夺与远程重击", "raider", "rune_arbalist", "tether_hound"),
            Strong("strong_signal_hub_a", "signal_hub", "三角维护网", "护障助教、巡查员、盾术形成三角", "高：护障、破势与回合盾循环", "barrier_mender", "lantern_revealer", "shieldguard"),
            Strong("strong_signal_hub_b", "signal_hub", "三角维护网·断链", "巡查员前置，助教与盾术分居后翼", "高：先破显影或先断维护", "barrier_mender", "lantern_revealer", "shieldguard"),
            Strong("strong_gatehouse_a", "gatehouse", "双向门厅", "盾术守一门、检验偶游走、重弩守另一门", "高：门厅封线与远程重击", "shieldguard", "sigil_mauler", "rune_arbalist"),
            Strong("strong_gatehouse_b", "gatehouse", "双向门厅·错位门线", "两门压力错位且中央可换路", "高：正面阻挡与贴身破势", "shieldguard", "sigil_mauler", "rune_arbalist"),
            Strong("strong_transmission_tower_a", "transmission_tower", "三扇区", "火矢、约束、显影各守一扇区", "高：三种公开远程状态压力", "pyromancer", "stone_snare", "lantern_revealer"),
            Strong("strong_transmission_tower_b", "transmission_tower", "三扇区·对角封线", "远程三角错位且存在两条切入线", "高：燃烧、长束缚与破势", "pyromancer", "stone_snare", "lantern_revealer"),

            Elite("elite_foundry_a", "elite_foundry", "编织狭口", "教官居中、助教与检验偶分守两路", "elite_vanguard", "barrier_mender", "sigil_mauler"),
            Elite("elite_foundry_b", "elite_foundry", "编织狭口·维护侧", "护障维护链偏左，右路可直取目标", "elite_vanguard", "barrier_mender", "sigil_mauler"),
            Elite("elite_foundry_c", "elite_foundry", "编织狭口·检验侧", "检验偶前置，教官与助教后置", "elite_vanguard", "barrier_mender", "sigil_mauler"),
            Elite("core_approach_a", "core_approach", "对角封线", "教官与重弩对角，约束助教守换路线", "elite_vanguard", "rune_arbalist", "stone_snare"),
            Elite("core_approach_b", "core_approach", "对角封线·双入口", "约束线不覆盖出生，左右均可切入", "elite_vanguard", "rune_arbalist", "stone_snare"),
            Elite("core_approach_c", "core_approach", "对角封线·远近换位", "教官前置，远程二人分守外翼", "elite_vanguard", "rune_arbalist", "stone_snare"),

            new RogueliteEncounterDefinition("boss_academy_sealed_core", "core_finale", RogueliteEncounterTier.Boss,
                "中心核心／外围维护", "固定核心守卫居中；教官、助教与巡查员构成可拆维护链",
                "终考", "终考奖励", 2,
                "core_overseer", "elite_vanguard", "barrier_mender", "lantern_revealer")
        };

        private static readonly IReadOnlyDictionary<string, RogueliteEncounterDefinition> ByVariant =
            Packages.ToDictionary(value => value.VariantKey, StringComparer.Ordinal);

        public static IReadOnlyList<RogueliteEncounterDefinition> WeakPool => Packages.Where(value => value.Tier == RogueliteEncounterTier.Weak).ToArray();
        public static IReadOnlyList<RogueliteEncounterDefinition> StrongPool => Packages.Where(value => value.Tier == RogueliteEncounterTier.Strong).ToArray();
        public static IReadOnlyList<RogueliteEncounterDefinition> ElitePool => Packages.Where(value => value.Tier == RogueliteEncounterTier.Elite).ToArray();
        public static RogueliteEncounterDefinition FixedBoss => Packages.Single(value => value.Tier == RogueliteEncounterTier.Boss);

        public static RogueliteEncounterDefinition Package(string variantKey) =>
            ByVariant.TryGetValue(variantKey ?? string.Empty, out RogueliteEncounterDefinition value)
                ? value : throw new InvalidOperationException("Unknown encounter variant: " + variantKey);

        public static RogueliteEncounterDefinition For(string nodeId, string regionBossId = null)
        {
            RogueliteEncounterDefinition package = Packages.FirstOrDefault(value => value.LevelId == nodeId &&
                (value.VariantKey == "strong_" + nodeId + "_a" || value.Tier >= RogueliteEncounterTier.Elite));
            if (package != null) return package.BindToNode(nodeId);
            if (!FirstRegionLevelCatalog.TryFor(nodeId, out FirstRegionLevelDefinition level))
                return new RogueliteEncounterDefinition(nodeId, nodeId, RogueliteEncounterTier.Strong,
                    "学院演练场", "对手已经在场地另一侧等候", "棘手", "基础奖励", 2,
                    "shieldguard", "pyromancer", "raider").BindToNode(nodeId);
            return new RogueliteEncounterDefinition("legacy_" + nodeId, level.Id,
                level.IsBoss ? RogueliteEncounterTier.Boss : level.IsElite ? RogueliteEncounterTier.Elite : RogueliteEncounterTier.Strong,
                "学院演练场", "对手已经在场地另一侧等候", level.IsBoss ? "终考" : level.IsElite ? "危险" : "棘手",
                level.IsBoss ? "终考奖励" : level.IsElite ? "稀有奖励" : "基础奖励", 2,
                level.ResolveEnemyArchetypeIds(regionBossId).ToArray()).BindToNode(nodeId);
        }

        public static RogueliteEncounterDefinition For(RogueliteMapRun run, string nodeId)
        {
            if (run != null && run.TryGetEncounter(nodeId, out RogueliteEncounterDefinition encounter)) return encounter;
            if (run != null && run.HasPendingContentCombat && nodeId == run.PendingContentCombatMissionId && nodeId == "relay_event")
            {
                string eventId = run.CurrentEventId;
                if (eventId == "EV08" || eventId == "EV13" || eventId == "EV16")
                    return new RogueliteEncounterDefinition("event_maintenance_elite", "elite_foundry", RogueliteEncounterTier.Elite,
                        "狭窄的维护通道", "两条路分别通向教官和护障助教", "危险",
                        "稀有奖励", 3, "elite_vanguard", "barrier_mender", "sigil_mauler").BindToNode(nodeId);
                if (eventId == "EV03" || eventId == "EV06")
                    return new RogueliteEncounterDefinition("event_archive_rescue", "signal_hub", RogueliteEncounterTier.Strong,
                        "三角维护场", "可以从护障、显影或盾线中的任意一处切入；赢了才能拿到许可", "危险",
                        "核心许可", 2, "barrier_mender", "lantern_revealer", "shieldguard").BindToNode(nodeId);
                if (eventId == "EV15")
                    return new RogueliteEncounterDefinition("event_relay_objective", "relay_raid", RogueliteEncounterTier.Strong,
                        "偏心校准场", "近路会暴露在重弩前；绕远一些可以先处理守卫。破坏目标就算完成", "危险",
                        "稀有法宝", 2, "raider", "rune_arbalist", "tether_hound").BindToNode(nodeId);
                return new RogueliteEncounterDefinition("event_field_drill", "rail_patrol", RogueliteEncounterTier.Weak,
                    "半开放演练场", "两名陪练分守前方两侧，起步位置很安全", "轻松",
                    "基础奖励", 1, "shieldguard", "raider").BindToNode(nodeId);
            }
            if (run != null) throw new InvalidOperationException("No registered encounter package for map node: " + nodeId);
            return For(nodeId);
        }

        public static IReadOnlyList<RogueliteEncounterAssignment> GenerateAssignments(int seed)
        {
            RogueliteMapNode[] normal = RogueliteMapCatalog.Nodes.Where(value => value.Type == RogueliteMapNodeType.Combat).ToArray();
            RogueliteMapNode[] startCombat = normal.Where(value => IsAdjacent(value.Id, "start")).OrderBy(value => StableKey(seed, value.Id)).ToArray();
            HashSet<string> forcedWeak = new HashSet<string>(startCombat.Take(Math.Max(2, Math.Min(3, startCombat.Length))).Select(value => value.Id), StringComparer.Ordinal);
            List<RogueliteEncounterAssignment> result = new List<RogueliteEncounterAssignment>();
            RogueliteMapNode[] normalOrder = normal.OrderBy(value => forcedWeak.Contains(value.Id) ? 0 : 1)
                .ThenByDescending(value => RogueliteMapCatalog.Nodes.Count(other => IsAdjacent(value.Id, other.Id)))
                .ThenBy(value => StableKey(seed, "node|" + value.Id)).ToArray();
            List<RogueliteEncounterDefinition> normalPackages = WeakPool.Concat(StrongPool)
                .OrderBy(value => StableKey(seed, "normal|" + value.VariantKey)).ToList();
            if (!TryAssign(normalOrder, 0, normalPackages, result, forcedWeak, seed))
                throw new InvalidOperationException("Unable to generate a valid fixed encounter assignment.");

            RogueliteMapNode[] eliteOrder = RogueliteMapCatalog.Nodes.Where(value => value.Type == RogueliteMapNodeType.Elite)
                .OrderByDescending(value => RogueliteMapCatalog.Nodes.Count(other => IsAdjacent(value.Id, other.Id)))
                .ThenBy(value => StableKey(seed, "elite-node|" + value.Id)).ToArray();
            if (!TryAssign(eliteOrder, 0, ElitePool.OrderBy(value => StableKey(seed, "elite|" + value.VariantKey)).ToList(), result, null, seed))
                throw new InvalidOperationException("Unable to generate a valid fixed elite assignment.");
            result.Add(new RogueliteEncounterAssignment("core_finale", FixedBoss.VariantKey));
            return result.OrderBy(value => value.NodeId, StringComparer.Ordinal).ToArray();
        }

        private static bool TryAssign(IReadOnlyList<RogueliteMapNode> nodes, int index,
            List<RogueliteEncounterDefinition> remaining, List<RogueliteEncounterAssignment> result,
            HashSet<string> forcedWeak, int seed)
        {
            if (index >= nodes.Count) return remaining.Count == 0;
            RogueliteMapNode node = nodes[index];
            RogueliteEncounterDefinition[] candidates = remaining
                .Where(value => forcedWeak == null || !forcedWeak.Contains(node.Id) || value.Tier == RogueliteEncounterTier.Weak)
                .Where(value => Compatible(node.Id, value, result))
                .OrderBy(value => StableKey(seed, node.Id + "|" + value.VariantKey)).ToArray();
            foreach (RogueliteEncounterDefinition candidate in candidates)
            {
                remaining.Remove(candidate); result.Add(new RogueliteEncounterAssignment(node.Id, candidate.VariantKey));
                if (TryAssign(nodes, index + 1, remaining, result, forcedWeak, seed)) return true;
                result.RemoveAt(result.Count - 1); remaining.Add(candidate);
            }
            return false;
        }

        private static bool Compatible(string nodeId, RogueliteEncounterDefinition candidate, IEnumerable<RogueliteEncounterAssignment> assigned)
        {
            foreach (RogueliteEncounterAssignment neighbor in assigned.Where(value => IsAdjacent(nodeId, value.NodeId)))
            {
                RogueliteEncounterDefinition other = Package(neighbor.VariantKey);
                if (other.VariantKey == candidate.VariantKey || other.LevelId == candidate.LevelId || other.SpatialGrammar == candidate.SpatialGrammar) return false;
            }
            return true;
        }

        public static bool IsAdjacent(string a, string b)
        {
            RogueliteMapNode left = RogueliteMapCatalog.Node(a); RogueliteMapNode right = RogueliteMapCatalog.Node(b);
            return left.NextIds.Contains(b) || right.NextIds.Contains(a);
        }

        private static int StableKey(int seed, string value)
        {
            unchecked { uint hash = (uint)(2166136261 ^ seed); foreach (char c in value) { hash ^= c; hash *= 16777619; } return (int)(hash & 0x7fffffff); }
        }
    }
}
