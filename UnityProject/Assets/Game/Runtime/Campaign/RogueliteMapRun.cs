using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OCC.Combat
{
    public enum RogueliteMapNodeType { Start, Combat, Elite, Event, Workshop, Medical, Shop, Rest, Treasure, Finale }
    public enum RogueliteMapNodeVisualState { Current, Available, Locked, Cleared, Visited, Known, Unknown }

    // Runtime tuning only: playtests can adjust these values without changing map topology or save data.
    public static class AcademyMapTuning
    {
        public const int ExpectedBossProgress = 20;
        // 固定段收束后交接到随机层：再完成 10 个随机阶段节点即可提前挑战首领。
        public const int BossMinimumProgress = 10;
        public const int ConsolidationProgress = 21;
        public const int TransitionWarningProgress = 25;
        public const int TransitionProgress = 28;
        public const bool EnforceBossGate = true;
        public const bool EnforceTransition = true;

        public static int TimeCost(RogueliteMapNodeType type)
        {
            if (type == RogueliteMapNodeType.Combat) return 2;
            if (type == RogueliteMapNodeType.Elite || type == RogueliteMapNodeType.Finale) return 3;
            if (type == RogueliteMapNodeType.Event) return 1;
            return 0;
        }

        public static int TimeCost(RogueliteMapNode node)
            => node != null && node.Id == "tower_lift" ? 0 : TimeCost(node?.Type ?? RogueliteMapNodeType.Start);
    }

    public enum AcademyMapPhase { NormalTerm, Consolidation, TransitionReady }

    public sealed class RogueliteMapNode
    {
        public string Id { get; }
        public RogueliteMapNodeType Type { get; }
        public IReadOnlyList<string> NextIds { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public int GridX { get; }
        public int GridY { get; }
        public bool IsCombat => Type == RogueliteMapNodeType.Combat || Type == RogueliteMapNodeType.Elite || Type == RogueliteMapNodeType.Finale;

        public RogueliteMapNode(string id, RogueliteMapNodeType type, string displayName, string summary, int gridX, int gridY, params string[] nextIds)
        {
            Id = id; Type = type; DisplayName = displayName; Summary = summary; GridX = gridX; GridY = gridY;
            NextIds = nextIds ?? Array.Empty<string>();
        }
    }

    public enum RogueliteRewardKind { Weapon, Spell, Item, Equipment, TacticalItem, Resource }

    public static class FireRogueliteStarterCatalog
    {
        public const string Melee = "fire_melee";
        public const string Universal = "fire_universal";
        public const string Ranged = "fire_ranged";
        public static readonly IReadOnlyList<string> All = new[] { Melee, Universal, Ranged };
        public static string DisplayName(string id) => id == Melee ? "近战训练" : id == Ranged ? "远程训练" : id == Universal ? "均衡训练" : "旧存档路线";
    }
    public enum RogueliteNodeContentEffect { Supplies = 0, ScoutingBeacon = 1, Reward = 3, Aether = 4, Recovery = 5, Economy = 6, Intelligence = 7 }
    public sealed class RogueliteNodeContentChoice
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Preview { get; }
        public RogueliteNodeContentEffect Effect { get; }
        public string RewardId { get; }
        public bool RequiresCombat { get; }
        public string CombatMissionId { get; }
        public int PartsCost { get; }
        public int AetherCost { get; }
        public int GoldCost { get; }
        public int ContributionCost { get; }
        public int GoldGain { get; }
        public int ContributionGain { get; }
        public int HealthGain { get; }
        public int ManaGain { get; }
        public RogueliteNodeContentChoice(string id, string displayName, string preview, RogueliteNodeContentEffect effect,
            string rewardId = null, bool requiresCombat = false, string combatMissionId = null,
            int partsCost = 0, int aetherCost = 0, int goldCost = 0, int contributionCost = 0,
            int goldGain = 0, int contributionGain = 0, int healthGain = 0, int manaGain = 0)
        {
            Id = id; DisplayName = displayName; Preview = preview; Effect = effect; RewardId = rewardId;
            RequiresCombat = requiresCombat; CombatMissionId = combatMissionId; PartsCost = partsCost; AetherCost = aetherCost;
            GoldCost = goldCost; ContributionCost = contributionCost; GoldGain = goldGain; ContributionGain = contributionGain;
            HealthGain = healthGain; ManaGain = manaGain;
        }
    }

    public static class RogueliteNodeContentCatalog
    {
        public static IReadOnlyList<RogueliteNodeContentChoice> ChoicesFor(RogueliteMapNode node)
            => ChoicesFor(node, null);

        public static IReadOnlyList<RogueliteNodeContentChoice> ChoicesFor(RogueliteMapNode node, string eventId)
        {
            if (node.Type == RogueliteMapNodeType.Event && (node.Id == "core_vault" || node.Id == "tower_lift"))
                return AcademyNodeContentCatalog.FunctionChoices(node);
            if (node.Type == RogueliteMapNodeType.Event && !string.IsNullOrEmpty(eventId))
                return AcademyNodeContentCatalog.Event(eventId).Choices;
            switch (node.Type)
            {
                case RogueliteMapNodeType.Event:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("survey", "低风险勘测", "收益：+1 侦测信标；无额外战斗。", RogueliteNodeContentEffect.ScoutingBeacon),
                        new RogueliteNodeContentChoice("scan_routes", "校准信标", "收益：+1 侦测信标；无额外战斗。", RogueliteNodeContentEffect.ScoutingBeacon),
                        new RogueliteNodeContentChoice("purify", "净化导管", "收益：+1 补给、+1 以太；无额外战斗。", RogueliteNodeContentEffect.Aether),
                        new RogueliteNodeContentChoice("recover_survey_lens", "回收显迹测镜", "收益：获得法宝“显迹测镜”；不触发额外战斗或时间压力。", RogueliteNodeContentEffect.Reward, "G-T04"),
                        new RogueliteNodeContentChoice("overload", "超载回收", "进入一场已公开的额外战斗，完成后结算节点。", RogueliteNodeContentEffect.Economy, requiresCombat: true, combatMissionId: "relay_event")
                    };
                case RogueliteMapNodeType.Medical:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("field_repair", "现场整备", "获得：1 补给，并恢复 6 生命、2 护盾和 4 个人魔力；不会触发额外战斗。", RogueliteNodeContentEffect.Recovery),
                        new RogueliteNodeContentChoice("scan_routes", "校准信标", "收益：+1 侦测信标；无额外战斗。", RogueliteNodeContentEffect.ScoutingBeacon)
                    };
                case RogueliteMapNodeType.Workshop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("wand_calibration", "以太聚焦校准", "获得：以太聚焦手杖，领取后放入背包。", RogueliteNodeContentEffect.Reward, "arcane_wand"),
                        new RogueliteNodeContentChoice("supply_strip", "拆解补给", "收益：+1 补给；无额外战斗。", RogueliteNodeContentEffect.Supplies)
                    };
                case RogueliteMapNodeType.Shop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("medical_cache", "医疗补给", "价格：2 零件；收益：+1 补给。", RogueliteNodeContentEffect.Supplies, partsCost: 2),
                        new RogueliteNodeContentChoice("signal_contract", "情报合约", "价格：1 零件 + 1 以太；收益：+1 侦测信标。", RogueliteNodeContentEffect.ScoutingBeacon, partsCost: 1, aetherCost: 1),
                        new RogueliteNodeContentChoice("buy_hazard_condenser", "购入险地冷凝器", "价格：3 零件 + 1 以太；收益：法宝“险地冷凝器”。", RogueliteNodeContentEffect.Reward, "G-T11", partsCost: 3, aetherCost: 1)
                    };
                default: return Array.Empty<RogueliteNodeContentChoice>();
            }
        }
    }

    public sealed class RogueliteReward
    {
        public string Id { get; }
        public string DisplayName { get; }
        public RogueliteRewardKind Kind { get; }
        public string BuildPath { get; }
        public WeaponDefinition Weapon { get; }
        public SkillDefinition Spell { get; }
        public OCC.Combat.Roguelite.SpellDefinition RogueSpell { get; }
        public ItemDefinition Item { get; }
        public OCC.Combat.Roguelite.EquipmentDefinition Equipment { get; }
        public OCC.Combat.Roguelite.TacticalItemDefinition TacticalItem { get; }
        public string ResourceId { get; }
        public int ResourceAmount { get; }
        public RogueliteReward(string id, string displayName, WeaponDefinition weapon, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Weapon; Weapon = weapon; BuildPath = buildPath; }
        public RogueliteReward(string id, string displayName, SkillDefinition spell, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Spell; Spell = spell; BuildPath = buildPath; }
        public RogueliteReward(OCC.Combat.Roguelite.SpellDefinition spell, string buildPath)
        { RogueSpell = spell ?? throw new ArgumentNullException(nameof(spell)); Id = spell.DefinitionId; DisplayName = spell.DisplayName; Kind = RogueliteRewardKind.Spell; BuildPath = buildPath; }
        public RogueliteReward(ItemDefinition item, string buildPath) { Item = item ?? throw new ArgumentNullException(nameof(item)); Id = item.Id; DisplayName = item.DisplayName; Kind = RogueliteRewardKind.Item; BuildPath = buildPath; }
        public RogueliteReward(OCC.Combat.Roguelite.EquipmentDefinition equipment, string buildPath)
        { Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment)); Id = equipment.DefinitionId; DisplayName = equipment.DisplayName; Kind = RogueliteRewardKind.Equipment; BuildPath = buildPath; }
        public RogueliteReward(OCC.Combat.Roguelite.TacticalItemDefinition tactical, string buildPath)
        { TacticalItem = tactical ?? throw new ArgumentNullException(nameof(tactical)); Id = tactical.DefinitionId; DisplayName = tactical.DisplayName; Kind = RogueliteRewardKind.TacticalItem; BuildPath = buildPath; }
        public RogueliteReward(string id, string displayName, string resourceId, int amount, string buildPath)
        { Id = id; DisplayName = displayName; ResourceId = resourceId; ResourceAmount = amount; Kind = RogueliteRewardKind.Resource; BuildPath = buildPath; }
    }

    public static class RogueliteMapCatalog
    {
        // This is an orthogonal room graph. Connections are interpreted as bidirectional at runtime,
        // so content authors only need to list an edge once.
        public static readonly IReadOnlyList<RogueliteMapNode> Nodes = new[]
        {
            new RogueliteMapNode("start", RogueliteMapNodeType.Start, "学院郊道", "首区入口。", 0, 2, "rail_patrol", "depot_wreck", "supply_checkpoint"),
            new RogueliteMapNode("rail_patrol", RogueliteMapNodeType.Combat, "普通战斗 04 · 石路巡哨", "清除巡哨队。", 1, 2, "start", "switchyard", "relay_raid", "supply_checkpoint"),
            new RogueliteMapNode("depot_wreck", RogueliteMapNodeType.Combat, "普通战斗 05 · 废弃驿站", "清除驿站守敌。", 1, 1, "start", "switchyard"),
            new RogueliteMapNode("supply_checkpoint", RogueliteMapNodeType.Shop, "行商补给点", "补给与零件交易。", 1, 3, "start", "rail_patrol", "field_workshop"),
            new RogueliteMapNode("switchyard", RogueliteMapNodeType.Event, "分岔石桥", "桥边贴着几份临时委托，也有人在等答复。", 2, 1, "depot_wreck", "rail_patrol", "signal_hub", "relay_event"),
            new RogueliteMapNode("relay_raid", RogueliteMapNodeType.Combat, "普通战斗 06 · 野外导能柱", "破坏被敌军占用的导能柱。", 2, 2, "rail_patrol", "relay_event", "med_bay", "field_workshop"),
            new RogueliteMapNode("field_workshop", RogueliteMapNodeType.Workshop, "随军工坊", "可以在这里修整装备，重新收拾行囊。", 2, 3, "supply_checkpoint", "relay_raid", "med_bay", "records_archive"),
            new RogueliteMapNode("signal_hub", RogueliteMapNodeType.Combat, "普通战斗 07 · 传讯石庭", "清除石庭守军。", 3, 1, "switchyard", "relay_event", "elite_foundry"),
            new RogueliteMapNode("relay_event", RogueliteMapNodeType.Event, "导能柱记录", "查阅现场记录。", 3, 2, "switchyard", "relay_raid", "signal_hub", "med_bay", "gatehouse"),
            new RogueliteMapNode("med_bay", RogueliteMapNodeType.Medical, "行军医帐", "治疗与餐食服务。", 3, 3, "relay_raid", "field_workshop", "relay_event", "records_archive", "sealed_market"),
            new RogueliteMapNode("elite_foundry", RogueliteMapNodeType.Elite, "精英战斗 02 · 刻阵工坊", "划线教官带着维护队守在里面，准备考验来访者。", 4, 1, "signal_hub", "gatehouse", "transmission_tower"),
            new RogueliteMapNode("gatehouse", RogueliteMapNodeType.Combat, "普通战斗 08 · 石闸关口", "打开通往古塔的道路。", 4, 2, "relay_event", "elite_foundry", "sealed_market", "aether_refinery"),
            new RogueliteMapNode("sealed_market", RogueliteMapNodeType.Shop, "封存商行", "商人既收金币，也愿意换取学院贡献。", 4, 3, "med_bay", "gatehouse", "records_archive", "aether_refinery", "safety_room"),
            new RogueliteMapNode("records_archive", RogueliteMapNodeType.Event, "档案整理", "帮助管理员整理积压的学院记录。", 4, 4, "field_workshop", "med_bay", "sealed_market", "safety_room"),
            new RogueliteMapNode("transmission_tower", RogueliteMapNodeType.Combat, "普通战斗 09 · 传讯塔楼", "沿相连道路前往塔楼，处理塔内的异常。", 5, 1, "elite_foundry", "aether_refinery", "core_approach"),
            new RogueliteMapNode("aether_refinery", RogueliteMapNodeType.Event, "以太校准室", "校准师愿意用报酬换取一双帮忙的手。", 5, 2, "gatehouse", "sealed_market", "transmission_tower", "safety_room", "core_vault"),
            new RogueliteMapNode("safety_room", RogueliteMapNodeType.Event, "守夜值班记录", "值班生准备了补给和情报，也可能请你出手帮忙。", 5, 3, "sealed_market", "records_archive", "aether_refinery", "core_vault"),
            new RogueliteMapNode("core_approach", RogueliteMapNodeType.Elite, "精英战斗 03 · 塔前石庭", "高年级守卫把住了古塔前庭。", 6, 1, "transmission_tower", "core_vault", "core_finale"),
            new RogueliteMapNode("core_vault", RogueliteMapNodeType.Event, "学院封存库", "管理员允许你从封存柜里带走一件东西。", 6, 2, "aether_refinery", "safety_room", "core_approach", "core_finale"),
            new RogueliteMapNode("core_finale", RogueliteMapNodeType.Finale, "Boss战斗 01 · 古塔核心", "击败拦在必经之路上的塔之守卫。", 7, 1, "core_approach", "core_vault", "seal_bridge", "tower_foyer"),
            new RogueliteMapNode("academy_gate", RogueliteMapNodeType.Event, "学院正门公告", "公告板上贴着新生委托和几张手绘地图。", 0, 0, "tutorial_hall", "dorm_drill"),
            new RogueliteMapNode("tutorial_hall", RogueliteMapNodeType.Combat, "新生演练厅", "处理公开演练中的失控傀儡。", 0, 1, "academy_gate", "start", "dorm_watch"),
            new RogueliteMapNode("dorm_watch", RogueliteMapNodeType.Combat, "宿舍夜间巡查", "清理夜间异常并保护宿舍区。", 0, 3, "tutorial_hall", "market_lane"),
            new RogueliteMapNode("market_lane", RogueliteMapNodeType.Combat, "学院市集护送", "护送器材通过市集外廊。", 0, 4, "dorm_watch", "field_infirmary"),
            new RogueliteMapNode("dorm_drill", RogueliteMapNodeType.Combat, "宿舍外实战演练", "近距离考核走位与护盾。", 1, 0, "academy_gate", "lecture_annex", "depot_wreck"),
            new RogueliteMapNode("field_infirmary", RogueliteMapNodeType.Event, "临时医务站", "医护生能帮你疗伤，也需要人手完成一趟救援。", 1, 4, "market_lane", "study_vault"),
            new RogueliteMapNode("lecture_annex", RogueliteMapNodeType.Combat, "讲坛公开考核", "在远程威胁下完成学院考核。", 2, 0, "dorm_drill", "archive_wing", "switchyard"),
            new RogueliteMapNode("study_vault", RogueliteMapNodeType.Combat, "阅览室封存柜异常", "清理封存柜周边的异常防卫。", 2, 4, "field_infirmary", "sparring_ring"),
            new RogueliteMapNode("archive_wing", RogueliteMapNodeType.Combat, "档案翼巡查", "处理档案翼中的显影误报。", 3, 0, "lecture_annex", "workshop_yard", "signal_hub"),
            new RogueliteMapNode("sparring_ring", RogueliteMapNodeType.Combat, "圆形实训场", "打赢训练阵列，就能从教员那里挑一件奖励。", 3, 4, "study_vault", "records_archive", "supply_depot"),
            new RogueliteMapNode("workshop_yard", RogueliteMapNodeType.Combat, "工坊庭院回路过载", "处理失控校准回路。", 4, 0, "archive_wing", "clinic_hall", "elite_foundry"),
            new RogueliteMapNode("clinic_hall", RogueliteMapNodeType.Combat, "诊疗厅导能泄漏", "在泄漏环境中保护治疗设备。", 5, 0, "workshop_yard", "wilds_path", "transmission_tower"),
            new RogueliteMapNode("supply_depot", RogueliteMapNodeType.Elite, "封存器材护送", "把封存器材安全送到另一边，教员会给出更好的奖励。", 5, 4, "sparring_ring", "records_archive", "wilds_camp"),
            new RogueliteMapNode("wilds_path", RogueliteMapNodeType.Combat, "郊野实训旧道", "开阔地中的学院实训巡查。", 6, 0, "clinic_hall", "seal_bridge", "core_approach"),
            new RogueliteMapNode("observatory_path", RogueliteMapNodeType.Elite, "观测塔求援", "处理封存区外环的高阶异常。", 6, 3, "wilds_camp", "tower_foyer", "core_vault"),
            new RogueliteMapNode("wilds_camp", RogueliteMapNodeType.Elite, "郊野导能柱考察", "完成这场艰难考察，领取精英战奖励。", 6, 4, "supply_depot", "observatory_path", "tower_records"),
            new RogueliteMapNode("seal_bridge", RogueliteMapNodeType.Combat, "封存区石桥", "清理通往高塔的学院警戒装置。", 7, 0, "wilds_path", "tower_foyer", "core_finale"),
            new RogueliteMapNode("tower_foyer", RogueliteMapNodeType.Elite, "封存塔门厅核验", "终考前的最后一队守卫正在门厅等候。", 7, 2, "seal_bridge", "observatory_path", "tower_lift", "core_finale"),
            new RogueliteMapNode("tower_records", RogueliteMapNodeType.Event, "高塔值守记录", "查阅维护记录，或免费恢复个人魔力；两项只能选一项。", 7, 3, "wilds_camp", "tower_lift"),
            new RogueliteMapNode("tower_lift", RogueliteMapNodeType.Event, "封存管理员匣", "管理员允许你领取一件封存的法宝。", 7, 4, "tower_records", "tower_foyer")
        };
        private static readonly IReadOnlyList<RogueliteReward> CoreRewards = new[]
        {
            new RogueliteReward("war_hammer", "破甲战锤", CombatCatalog.Hammer, "突击"),
            new RogueliteReward("aether_wand", "以太手杖", CombatCatalog.Wand, "控制"),
            new RogueliteReward("fire_bolt", "火矢术式", CombatCatalog.FireBolt, "突击"),
            new RogueliteReward("frost_bind", "冰缚术式", CombatCatalog.FrostBind, "控制"),
            new RogueliteReward("arcane_wand", "以太聚焦手杖", StageTwoBuilds.ArcaneWand, "以太"),
            new RogueliteReward(ItemCatalog.FirelineScroll, "火术封装")
        };
        public static readonly IReadOnlyList<RogueliteReward> Rewards = CoreRewards
            .Concat(ArtifactCatalog.All.Where(artifact => ArtifactCatalog.IsCurrentlyUsable(artifact.Id))
                .Select(artifact => new RogueliteReward(ItemCatalog.Get(artifact.Id), artifact.BuildUse)))
            .ToArray();
        /// <summary>
        /// 全图节点解析。学院层的 3 个服务节点是层专属节点（同一锚点上有别的全图节点），
        /// 因此这里先问学院层目录，再回到全图节点表。
        /// </summary>
        public static RogueliteMapNode Node(string id)
        {
            if (RogueliteAcademyLayerCatalog.TryResolveLayerNode(id, out RogueliteMapNode layerNode)) return layerNode;
            return Nodes.First(node => node.Id == id);
        }
        public static IReadOnlyList<RogueliteReward> RollRewards(int seed, int completedCombatCount)
        {
            var random = new Random(seed + completedCombatCount * 7919);
            return Rewards.OrderBy(_ => random.Next()).Take(3).ToArray();
        }
        public static IReadOnlyList<RogueliteReward> RollFireSupportRewards(RogueliteMapNodeType nodeType)
            => RollFireSupportRewards(0, 0, nodeType, Array.Empty<string>());

        public static IReadOnlyList<RogueliteReward> RollFireSupportRewards(int seed, int completedCombatCount,
            RogueliteMapNodeType nodeType, IEnumerable<string> ownedDefinitionIds)
        {
            bool rare = nodeType == RogueliteMapNodeType.Elite || nodeType == RogueliteMapNodeType.Treasure || nodeType == RogueliteMapNodeType.Finale;
            RogueliteReward scroll = Rewards.First(reward => reward.Id == ItemCatalog.FirelineScroll.Id);
            ArtifactDefinition artifact = ArtifactRewardPool.Roll(seed, completedCombatCount, nodeType, ownedDefinitionIds);
            RogueliteReward artifactReward = Rewards.First(reward => reward.Id == artifact.Id);
            if (!rare) return new[] { artifactReward };
            return new[] { scroll, artifactReward };
        }
    }

    public static class ArtifactRewardPool
    {
        public static ArtifactDefinition Roll(int seed, int progress, RogueliteMapNodeType nodeType, IEnumerable<string> ownedDefinitionIds = null)
        {
            HashSet<string> owned = new HashSet<string>(ownedDefinitionIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            ArtifactDefinition[] eligible = ArtifactCatalog.All.Where(artifact => ArtifactCatalog.IsCurrentlyUsable(artifact.Id))
                .Where(artifact => IsPoolEligible(artifact, nodeType) && IsTierEligible(artifact.Rarity, nodeType) && !owned.Contains(artifact.Id)).ToArray();
            if (eligible.Length == 0) eligible = ArtifactCatalog.All.Where(artifact => ArtifactCatalog.IsCurrentlyUsable(artifact.Id) && IsPoolEligible(artifact, nodeType) && !owned.Contains(artifact.Id)).ToArray();
            if (eligible.Length == 0) eligible = ArtifactCatalog.All.Where(artifact => ArtifactCatalog.IsCurrentlyUsable(artifact.Id) && !owned.Contains(artifact.Id)).ToArray();
            if (eligible.Length == 0) eligible = ArtifactCatalog.All.Where(artifact => ArtifactCatalog.IsCurrentlyUsable(artifact.Id)).ToArray();
            string key = seed + "|" + progress + "|" + nodeType;
            return eligible.OrderBy(artifact => StableKey(key, artifact.Id)).ThenBy(artifact => artifact.Id, StringComparer.Ordinal).First();
        }

        public static ArtifactDefinition RollLoot(int seed, string sourceId)
        {
            string key = seed + "|loot|" + (sourceId ?? string.Empty);
            ArtifactDefinition[] eligible = ArtifactCatalog.All.Where(artifact => ArtifactCatalog.IsCurrentlyUsable(artifact.Id) && (artifact.ContentSources & ArtifactContentSource.Loot) != 0).ToArray();
            if (eligible.Length == 0) throw new InvalidOperationException("Artifact catalog has no loot-reachable content.");
            return eligible.OrderBy(artifact => StableKey(key, artifact.Id)).ThenBy(artifact => artifact.Id, StringComparer.Ordinal).First();
        }

        private static bool IsTierEligible(ItemRarity rarity, RogueliteMapNodeType nodeType)
        {
            if (nodeType == RogueliteMapNodeType.Combat) return rarity == ItemRarity.Common || rarity == ItemRarity.Uncommon;
            if (nodeType == RogueliteMapNodeType.Elite) return rarity == ItemRarity.Uncommon || rarity == ItemRarity.Rare;
            if (nodeType == RogueliteMapNodeType.Treasure || nodeType == RogueliteMapNodeType.Finale) return rarity == ItemRarity.Rare || rarity == ItemRarity.Exceptional;
            return true;
        }

        private static bool IsPoolEligible(ArtifactDefinition artifact, RogueliteMapNodeType nodeType)
        {
            ArtifactContentSource source = nodeType == RogueliteMapNodeType.Combat ? ArtifactContentSource.NormalReward
                : nodeType == RogueliteMapNodeType.Elite ? ArtifactContentSource.EliteReward
                : nodeType == RogueliteMapNodeType.Treasure ? ArtifactContentSource.Treasure
                : nodeType == RogueliteMapNodeType.Finale ? ArtifactContentSource.BossReward
                : ArtifactContentSource.None;
            return source == ArtifactContentSource.None || (artifact.ContentSources & source) != 0;
        }

        private static int StableKey(string prefix, string id)
        {
            // Mix the run key through every id character. The old polynomial
            // prefix+id hash kept equal-length ids in the same order for every
            // seed, which made most artifacts unreachable.
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in prefix + "|" + id)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }
    }

    public sealed class AcademyResourceReceipt
    {
        public string NodeId { get; }
        public string ChoiceId { get; }
        public int GoldChange { get; }
        public int ContributionChange { get; }
        public int HealthChange { get; }
        public int ManaChange { get; }
        public int TimeChange { get; }

        public AcademyResourceReceipt(string nodeId, string choiceId, int goldChange, int contributionChange,
            int healthChange, int manaChange, int timeChange)
        {
            NodeId = nodeId ?? string.Empty; ChoiceId = choiceId ?? string.Empty;
            GoldChange = goldChange; ContributionChange = contributionChange;
            HealthChange = healthChange; ManaChange = manaChange; TimeChange = timeChange;
        }

        public string Encode() => string.Join(",", NodeId, ChoiceId,
            GoldChange.ToString(CultureInfo.InvariantCulture), ContributionChange.ToString(CultureInfo.InvariantCulture),
            HealthChange.ToString(CultureInfo.InvariantCulture), ManaChange.ToString(CultureInfo.InvariantCulture),
            TimeChange.ToString(CultureInfo.InvariantCulture));

        public static AcademyResourceReceipt Decode(string value)
        {
            string[] fields = (value ?? string.Empty).Split(',');
            if (fields.Length != 7 || string.IsNullOrEmpty(fields[0]) || string.IsNullOrEmpty(fields[1]))
                throw new InvalidOperationException("Invalid academy resource receipt.");
            return new AcademyResourceReceipt(fields[0], fields[1], int.Parse(fields[2], CultureInfo.InvariantCulture),
                int.Parse(fields[3], CultureInfo.InvariantCulture), int.Parse(fields[4], CultureInfo.InvariantCulture),
                int.Parse(fields[5], CultureInfo.InvariantCulture), int.Parse(fields[6], CultureInfo.InvariantCulture));
        }
    }

    public sealed class RogueliteMapRun
    {
        private readonly HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { "start" };
        private readonly List<string> routeHistory = new List<string> { "start" };
        private readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> claimedRewards = new List<string>();
        private readonly List<string> ownedFireSpells = new List<string>();
        private readonly string[] equippedFireSpells = new string[2];
        private readonly string[] rogueEquippedSpellIds = new string[8]
        { "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER", "", "", "", "" };
        private readonly List<FireSpellSaveMigrationClaim> pendingFireSpellReselections = new List<FireSpellSaveMigrationClaim>();
        private readonly List<FireSpellSaveMigrationClaim> fireSpellRetirementCompensations = new List<FireSpellSaveMigrationClaim>();
        private readonly List<string> fireSpellMigrationWarnings = new List<string>();
        private bool deferredNodeReward;
        private int nextItemSequence;
        private readonly Dictionary<string, string> lootProgress = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> encounterAssignments = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> nodeContentAssignments = new Dictionary<string, string>(StringComparer.Ordinal);
        private IReadOnlyList<RogueliteMapNode> generatedAcademyMapNodes = Array.Empty<RogueliteMapNode>();
        private string activeLayerServiceNodeId = string.Empty;
        private FirstRunWorkshopSnapshot layerWorkshopService;
        private FirstRunMedicalSnapshot layerMedicalService;
        private FirstRunShopSnapshot layerShopService;
        private OCC.Combat.Roguelite.RogueRunDto rogueRunDto;
        public int Seed { get; }
        public string CurrentNodeId { get; private set; } = "start";
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int Supplies { get; private set; }
        public int ScoutingBeacons { get; private set; }
        public int Parts { get; private set; } = 4;
        public int Aether { get; private set; } = 2;
        public string RegionBossId { get; private set; }
        public string EquippedWeaponId { get; private set; }
        public string EquippedSpellId { get; private set; }
        public bool IsAetherCalibrated { get; private set; }
        public string PendingContentChoiceId { get; private set; }
        public string PendingContentCombatMissionId { get; private set; }
        public string StarterId { get; private set; }
        public bool HasCombatSnapshot { get; private set; }
        public int CurrentHealth { get; private set; } = UnitState.HeroBaseHealth;
        public int CurrentShield { get; private set; } = 2;
        public int CurrentMana { get; private set; } = 12;
        public int RogueManaCapacity => 12 + (rogueRunDto == null ? 0 :
            OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto).ForgeManaCapacityBonus);
        public FirstRunExperienceState FirstRunExperience { get; private set; }
        public RogueliteRunProgram RunProgram => FirstRunExperience != null
            ? RogueliteRunProgram.FirstRunV1
            : !string.IsNullOrEmpty(rogueRunDto?.MigrationReportId)
                ? RogueliteRunProgram.LegacyGrandfathered
                : RogueliteRunProgram.EvergreenAcademy;
        public bool IsFirstRunExperience => FirstRunExperience != null;
        /// <summary>固定教学段阶段：节点集取 FirstRunExperienceCatalog，行为仍是教程语义。</summary>
        public bool IsTutorialPhase => IsFirstRunExperience && !IsInAcademyLayer;
        /// <summary>随机层阶段：固定段已收束，同一局继续走学院层 20 节点子图。</summary>
        public bool IsInAcademyLayer { get; private set; }
        public bool DeparturePending => rogueRunDto?.DeparturePending == true;
        public int AcademyFoodCount => (FirstRunExperience?.AcademyFoodCount ?? 0) + (rogueRunDto?.AcademyFoodCount ?? 0);
        public int ForgeMaterialCount => (FirstRunExperience?.ForgeMaterialCount ?? 0) + (rogueRunDto?.ForgeMaterialCount ?? 0);
        public int SpecializationMaterialCount => (FirstRunExperience?.SpecializationMaterialCount ?? 0) + (rogueRunDto?.SpecializationMaterialCount ?? 0);
        public int AcademyMaterialCount(string materialId)
        {
            if (!IsAcademyMaterialId(materialId)) return 0;
            string row = rogueRunDto?.MaterialStockRows.FirstOrDefault(value => value.StartsWith(materialId + "=", StringComparison.Ordinal));
            int saved = row == null ? 0 : int.Parse(row.Substring(materialId.Length + 1));
            int typed = rogueRunDto?.MaterialStockRows
                .Where(value => value.StartsWith(materialId.StartsWith("FORGE-", StringComparison.Ordinal) ? "FORGE-" : "SPEC-", StringComparison.Ordinal))
                .Sum(value => int.Parse(value.Substring(value.IndexOf('=') + 1))) ?? 0;
            int legacy = materialId == AcademyBattleRewardCatalog.ForgeLoad
                ? Math.Max(0, (rogueRunDto?.ForgeMaterialCount ?? 0) - typed) + (FirstRunExperience?.ForgeMaterialCount ?? 0)
                : materialId == AcademyBattleRewardCatalog.SpecAmplify
                    ? Math.Max(0, (rogueRunDto?.SpecializationMaterialCount ?? 0) - typed) + (FirstRunExperience?.SpecializationMaterialCount ?? 0)
                    : 0;
            return saved + legacy;
        }
        public bool CanAcceptAcademyResource(string resourceId) =>
            resourceId == AcademyBattleRewardCatalog.ScrollPack ? CanAcceptRogue11Content(ArtifactCatalog.FirelineScroll.Id) :
            IsAcademyMaterialId(resourceId) ? OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto).CanFitMaterials(1) :
            resourceId == AcademyBattleRewardCatalog.ForgePair || resourceId == AcademyBattleRewardCatalog.SpecPair ||
            resourceId == AcademyBattleRewardCatalog.MixedPair
                ? OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto).CanFitMaterials(2) : true;
        public bool CanAcceptAcademyReward(RogueliteReward reward, bool dismantle = false)
        {
            if (reward == null || rogueRunDto == null) return false;
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
            int materialCount = PendingRewardStepId == "main" && rogueRunDto.PendingRewardFollowupIds.Count > 0
                ? 0 : rogueRunDto.PendingFixedMaterialIds.Count;
            if (!string.IsNullOrEmpty(rogueRunDto.PendingMainRewardId))
            {
                bool mainDismantle = rogueRunDto.PendingMainRewardId.StartsWith("dismantle:", StringComparison.Ordinal);
                string mainId = mainDismantle ? rogueRunDto.PendingMainRewardId.Substring("dismantle:".Length) : rogueRunDto.PendingMainRewardId;
                if (!PreviewAcademyReward(runtime, ResolveAcademyChoice(mainId), mainDismantle,
                    "__main_preview__", ref materialCount)) return false;
            }
            return PreviewAcademyReward(runtime, reward, dismantle, "__reward_preview__", ref materialCount) &&
                runtime.CanFitMaterials(materialCount);
        }
        private static bool PreviewAcademyReward(OCC.Combat.Roguelite.RogueEquipmentRuntime runtime,
            RogueliteReward reward, bool dismantle, string previewId, ref int materialCount)
        {
            if (reward == null) return false;
            if (dismantle) { materialCount++; return true; }
            if (reward.Kind == RogueliteRewardKind.Resource)
            {
                if (IsAcademyMaterialId(reward.ResourceId)) materialCount++;
                else if (reward.ResourceId == AcademyBattleRewardCatalog.ForgePair ||
                    reward.ResourceId == AcademyBattleRewardCatalog.SpecPair ||
                    reward.ResourceId == AcademyBattleRewardCatalog.MixedPair) materialCount += 2;
                else if (reward.ResourceId == AcademyBattleRewardCatalog.ScrollPack)
                {
                    var scroll = runtime.CreateTacticalItem(previewId, ArtifactCatalog.FirelineScroll.Id, int.MaxValue, "preview");
                    return runtime.AddTacticalToBackpack(scroll);
                }
            }
            else if (reward.Equipment != null)
            {
                var item = runtime.CreateInstance(previewId, reward.Id,
                    reward.Equipment.AllowedRarities[0], int.MaxValue, "preview");
                return runtime.AddToBackpack(item);
            }
            else if (reward.TacticalItem != null)
            {
                var item = runtime.CreateTacticalItem(previewId, reward.Id, int.MaxValue, "preview");
                return runtime.AddTacticalToBackpack(item);
            }
            return true;
        }
        private static RogueliteReward ResolveAcademyChoice(string id)
        {
            RogueliteReward resource = AcademyBattleRewardCatalog.Resource(id);
            if (resource != null) return resource;
            var catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            var spell = catalog.Spells.FirstOrDefault(value => value.DefinitionId == id);
            if (spell != null) return new RogueliteReward(spell, "战斗奖励");
            var equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == id);
            if (equipment != null) return new RogueliteReward(equipment, "战斗奖励");
            var tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == id);
            return tactical == null ? null : new RogueliteReward(tactical, "战斗奖励");
        }
        public bool IsInAcademyLayerPhase => IsInAcademyLayer;
        public IReadOnlyList<RogueliteMapNode> AcademyLayerNodes => generatedAcademyMapNodes.Count > 0
            ? generatedAcademyMapNodes : RogueliteAcademyLayerCatalog.LayerNodes;
        public IReadOnlyList<RogueliteMapNode> MapNodes => IsTutorialPhase
            ? FirstRunExperienceCatalog.MapNodes
            : IsInAcademyLayer ? AcademyLayerNodes : RogueliteMapCatalog.Nodes;
        public RogueliteMapNode MapNode(string id)
        {
            if (IsInAcademyLayer && generatedAcademyMapNodes.Count > 0)
                return generatedAcademyMapNodes.FirstOrDefault(node => node.Id == id)
                    ?? throw new KeyNotFoundException("Node is not present in the saved academy map: " + id);
            if (IsTutorialPhase)
            {
                // 教学段以固定段节点集为准；查不到的 id 回落到完整节点表而不是抛异常。
                RogueliteMapNode fixedNode = FirstRunExperienceCatalog.MapNodes.FirstOrDefault(node => node.Id == id);
                if (fixedNode != null) return fixedNode;
            }
            // 学院层专属服务节点只存在于层目录。
            if (RogueliteAcademyLayerCatalog.TryResolveLayerNode(id, out RogueliteMapNode layerNode)) return layerNode;
            return RogueliteMapCatalog.Nodes.FirstOrDefault(node => node.Id == id)
                ?? throw new KeyNotFoundException("Unknown map node in the current stage: " + id);
        }
        public bool UsesRogue11 => rogueRunDto != null;
        public int Gold => rogueRunDto?.Gold ?? 0;
        public int StageContribution => rogueRunDto?.StageContribution ?? 0;
        public int StageTime => rogueRunDto?.StageTime ?? AcademyProgress;
        public OCC.Combat.Roguelite.RogueRunDto RogueRunState => rogueRunDto;
        public bool AwaitingReward { get; private set; }
        public string RunEndReason => rogueRunDto?.RunEndReason ?? string.Empty;
        public bool HasActiveCombat => !string.IsNullOrEmpty(rogueRunDto?.ActiveCombatNodeId);
        public AcademyResourceReceipt PendingResourceReceipt =>
            string.IsNullOrEmpty(rogueRunDto?.PendingResourceReceipt) ? null : AcademyResourceReceipt.Decode(rogueRunDto.PendingResourceReceipt);
        public IReadOnlyList<string> CombatJournalRows => rogueRunDto?.CombatJournalRows ?? (IReadOnlyList<string>)Array.Empty<string>();

        public void BeginCombatJournal()
        {
            if (rogueRunDto == null || IsComplete || AwaitingReward ||
                (!MapNode(CurrentNodeId).IsCombat && !HasPendingContentCombat))
                throw new InvalidOperationException("There is no active combat to record.");
            if (HasActiveCombat && rogueRunDto.ActiveCombatNodeId != CurrentNodeId)
                throw new InvalidOperationException("Another combat journal is still active.");
            if (HasActiveCombat) return;
            rogueRunDto.ActiveCombatNodeId = CurrentNodeId;
            rogueRunDto.CombatJournalRows.Clear();
        }

        public void AppendCombatJournal(CombatJournalEntry entry)
        {
            if (!HasActiveCombat || rogueRunDto.ActiveCombatNodeId != CurrentNodeId)
                throw new InvalidOperationException("Combat journal is not active for this node.");
            if (rogueRunDto.CombatJournalRows.Count >= 2000)
                throw new InvalidOperationException("Combat journal has reached its safety limit.");
            rogueRunDto.CombatJournalRows.Add(entry.Encode());
        }

        public void ClearCombatJournal()
        {
            if (rogueRunDto == null) return;
            rogueRunDto.ActiveCombatNodeId = string.Empty;
            rogueRunDto.CombatJournalRows.Clear();
        }
        public bool IsComplete => !string.IsNullOrEmpty(RunEndReason) || (FirstRunExperience != null
            ? FirstRunExperience.IsTerminal
            : completed.Contains("core_finale") && !AwaitingReward);

        public void CloseAsFailure(bool abandoned)
        {
            if (rogueRunDto == null || IsComplete || AwaitingReward)
                throw new InvalidOperationException("The active run cannot be closed as a failed combat.");
            RogueliteMapNode node = MapNode(CurrentNodeId);
            if (!node.IsCombat && !HasPendingContentCombat)
                throw new InvalidOperationException("There is no active combat to close.");
            rogueRunDto.RunEndReason = abandoned ? "abandon" : "death";
            ClearCombatJournal();
            if (!abandoned)
            {
                CurrentHealth = 0;
                rogueRunDto.CurrentHealth = 0;
            }
        }
        public int AcademyProgress => Math.Max(0, visited.Count - 1);
        public int CompletedAcademyNodeCount => completed.Count(id => TryMapNode(id)?.Type != RogueliteMapNodeType.Finale && TryMapNode(id) != null);
        public AcademyMapPhase AcademyPhase => StageTime >= AcademyMapTuning.TransitionProgress
            ? AcademyMapPhase.TransitionReady
            : StageTime >= AcademyMapTuning.ConsolidationProgress
                ? AcademyMapPhase.Consolidation
                : AcademyMapPhase.NormalTerm;
        public bool CanChallengeAcademyFinale => StageTime >= AcademyMapTuning.TransitionProgress ||
            CompletedAcademyNodeCount >= AcademyMapTuning.BossMinimumProgress;
        public bool IsTransitionPending => AcademyMapTuning.EnforceTransition
            && StageTime >= AcademyMapTuning.TransitionProgress;
        public IReadOnlyCollection<string> UnlockedNodes => IsTutorialPhase
            ? FirstRunExperience.Nodes.Where(node => (node.Flags & FirstRunNodeFlags.Locked) == 0).Select(node => node.Id).ToArray()
            : IsInAcademyLayer
                ? FirstRunExperience.Nodes.Where(node => (node.Flags & FirstRunNodeFlags.Locked) == 0).Select(node => node.Id)
                    .Concat(visited).Distinct(StringComparer.Ordinal).ToArray()
                : visited;
        public IReadOnlyCollection<string> VisitedNodes => visited;
        public IReadOnlyList<string> RouteHistoryNodeIds => routeHistory;
        public IReadOnlyCollection<string> CompletedNodes => IsTutorialPhase
            ? FirstRunExperience.Nodes.Where(node => (node.Flags & FirstRunNodeFlags.Completed) != 0).Select(node => node.Id).ToArray()
            : completed;
        public IReadOnlyList<string> ClaimedRewards => claimedRewards;
        public string PendingRewardStepId => rogueRunDto?.PendingRewardStepId ?? string.Empty;
        public IReadOnlyList<string> OwnedFireSpellIds => ownedFireSpells;
        public IReadOnlyList<string> EquippedFireSpellIds => equippedFireSpells;
        public IReadOnlyList<string> RogueEquippedSpellIds => rogueRunDto?.EquippedSpellIds ?? rogueEquippedSpellIds;
        public IReadOnlyList<FireSpellSaveMigrationClaim> PendingFireSpellReselections => pendingFireSpellReselections;
        public IReadOnlyList<FireSpellSaveMigrationClaim> FireSpellRetirementCompensations => fireSpellRetirementCompensations;
        public IReadOnlyList<string> FireSpellMigrationWarnings => fireSpellMigrationWarnings;
        public InventoryContainerState Inventory { get; private set; }
        public string[] ItemQuickbar { get; private set; } = new string[OCC.Combat.Roguelite.RogueRuntimeConstants.ItemQuickbarSize];
        public int NextItemSequence => nextItemSequence;
        public IReadOnlyDictionary<string, string> LootProgress => lootProgress;
        public IReadOnlyDictionary<string, string> EncounterAssignments => encounterAssignments;
        public IReadOnlyDictionary<string, string> NodeContentAssignments => nodeContentAssignments;
        public IReadOnlyCollection<string> SettledServiceNodeIds => rogueRunDto != null
            ? (IReadOnlyCollection<string>)rogueRunDto.SettledServiceNodeIds
            : Array.Empty<string>();
        public bool IsServiceNodeSettled(string nodeId) => !string.IsNullOrEmpty(nodeId) && SettledServiceNodeIds.Contains(nodeId);

        public void SettleServiceNode(string nodeId)
        {
            if (rogueRunDto == null) throw new InvalidOperationException("Service node settlement requires a rogue11 run.");
            if (!RogueliteAcademyLayerCatalog.IsServiceNode(nodeId))
                throw new InvalidOperationException("Unknown academy service node: " + nodeId);
            if (!rogueRunDto.SettledServiceNodeIds.Contains(nodeId)) rogueRunDto.SettledServiceNodeIds.Add(nodeId);
        }
        public FirstRunWorkshopSnapshot CurrentWorkshopService => IsTutorialPhase
            ? FirstRunExperience.Workshop
            : EnsureLayerServiceSession(RogueliteMapNodeType.Workshop).layerWorkshopService;
        public IReadOnlyList<FirstRunWorkshopSnapshot> CompletedWorkshopBuilds
        {
            get
            {
                var builds = new List<FirstRunWorkshopSnapshot>();
                if (FirstRunExperience?.Workshop != null) builds.Add(FirstRunExperience.Workshop);
                string row = rogueRunDto?.LayerServiceRows.FirstOrDefault(value =>
                    value.StartsWith("layer_workshop~W~", StringComparison.Ordinal));
                if (row != null)
                {
                    string[] fields = row.Split('~');
                    builds.Add(new FirstRunWorkshopSnapshot
                    {
                        ForgeCompleted = fields[2] == "1", SpecializationCompleted = fields[3] == "1",
                        ForgedTargetId = fields[4], SpecializedTargetId = fields[5],
                        ForgedMaterialId = fields.Length > 6 ? fields[6] : AcademyBattleRewardCatalog.ForgeLoad,
                        SpecializedMaterialId = fields.Length > 7 ? fields[7] : AcademyBattleRewardCatalog.SpecAmplify
                    });
                }
                return builds;
            }
        }
        public FirstRunMedicalSnapshot CurrentMedicalService => IsTutorialPhase
            ? FirstRunExperience.Medical
            : EnsureLayerServiceSession(RogueliteMapNodeType.Medical).layerMedicalService;
        public FirstRunShopSnapshot CurrentShopService => IsTutorialPhase
            ? FirstRunExperience.Shop
            : EnsureLayerServiceSession(RogueliteMapNodeType.Shop).layerShopService;
        public IReadOnlyList<string> CurrentFirstRunRewardIds
        {
            get
            {
                if (!IsFirstRunExperience || !AwaitingReward) return Array.Empty<string>();
                if (IsInAcademyLayer) return Array.Empty<string>();
                if (FirstRunExperience.Outcome == FirstRunOutcome.EliteVictory && !FirstRunExperience.EliteRewardClaimed && !FirstRunExperience.EliteRewardAbandoned)
                    return FirstRunExperienceCatalog.ElitePassiveRewardIds;
                FirstRunRewardGroupSnapshot group = FirstRunExperience.RewardGroups.SingleOrDefault(value => value.Id == FirstRunExperience.PendingRewardGroupId);
                return group?.CandidateIds.ToArray() ?? Array.Empty<string>();
            }
        }
        public bool HasDeferredNodeReward => deferredNodeReward;
        public WeaponDefinition EquippedWeapon => string.IsNullOrEmpty(EquippedWeaponId) ? CombatCatalog.Rifle : RogueliteMapCatalog.Rewards.First(reward => reward.Id == EquippedWeaponId).Weapon;
        public IReadOnlyList<RogueliteReward> CurrentRewards
        {
            get
            {
                if (!AwaitingReward || PendingResourceReceipt != null) return Array.Empty<RogueliteReward>();
                if (IsTutorialPhase)
                {
                    OCC.Combat.Roguelite.RogueContentCatalog firstRunCatalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
                    return CurrentFirstRunRewardIds.Select(id =>
                    {
                        OCC.Combat.Roguelite.SpellDefinition spell = firstRunCatalog.Spells.FirstOrDefault(value => value.DefinitionId == id);
                        if (spell != null) return new RogueliteReward(spell, "首次体验固定奖励");
                        OCC.Combat.Roguelite.EquipmentDefinition equipment = firstRunCatalog.Equipment.FirstOrDefault(value => value.DefinitionId == id);
                        return equipment == null ? null : new RogueliteReward(equipment, "首次体验固定奖励");
                    }).Where(value => value != null).ToArray();
                }
                if (rogueRunDto != null && rogueRunDto.PendingRewardIds != null && rogueRunDto.PendingRewardIds.Count > 0)
                {
                    OCC.Combat.Roguelite.RogueContentCatalog pendingCatalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
                    return rogueRunDto.PendingRewardIds.Select(id =>
                    {
                        OCC.Combat.Roguelite.SpellDefinition spell = pendingCatalog.Spells.FirstOrDefault(value => value.DefinitionId == id);
                        if (spell != null) return new RogueliteReward(spell, "事件结算");
                        OCC.Combat.Roguelite.EquipmentDefinition equipment = pendingCatalog.Equipment.FirstOrDefault(value => value.DefinitionId == id);
                        if (equipment != null) return new RogueliteReward(equipment, "事件结算");
                        OCC.Combat.Roguelite.TacticalItemDefinition tactical = pendingCatalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == id);
                        return tactical == null ? null : new RogueliteReward(tactical, "事件结算");
                    }).Where(value => value != null).ToArray();
                }
                if (rogueRunDto != null && rogueRunDto.RolledRewardChoiceIds.Count > 0)
                {
                    OCC.Combat.Roguelite.RogueContentCatalog fixedCatalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
                    return rogueRunDto.RolledRewardChoiceIds.Select(id =>
                    {
                        RogueliteReward resource = AcademyBattleRewardCatalog.Resource(id);
                        if (resource != null) return resource;
                        OCC.Combat.Roguelite.SpellDefinition spell = fixedCatalog.Spells.FirstOrDefault(value => value.DefinitionId == id);
                        if (spell != null) return new RogueliteReward(spell, "战斗奖励");
                        OCC.Combat.Roguelite.EquipmentDefinition equipment = fixedCatalog.Equipment.FirstOrDefault(value => value.DefinitionId == id);
                        if (equipment != null) return new RogueliteReward(equipment, "战斗奖励");
                        OCC.Combat.Roguelite.TacticalItemDefinition tactical = fixedCatalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == id);
                        return tactical == null ? null : new RogueliteReward(tactical, "战斗奖励");
                    }).Where(value => value != null).ToArray();
                }
                if (rogueRunDto == null) return RogueliteMapCatalog.RollFireSupportRewards(Seed, completed.Count, RogueliteMapCatalog.Node(CurrentNodeId).Type, Inventory.Items.Select(item => item.DefinitionId));
                OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
                string source = RogueliteMapCatalog.Node(CurrentNodeId).Type == RogueliteMapNodeType.Finale ? "boss" : RogueliteMapCatalog.Node(CurrentNodeId).Type == RogueliteMapNodeType.Elite ? "elite" : "combat";
                OCC.Combat.Roguelite.RogueAcademyContentService service = new OCC.Combat.Roguelite.RogueAcademyContentService();
                if (IsInAcademyLayer)
                    return AcademyBattleRewardCatalog.Roll(Seed, CurrentNodeId,
                        rogueRunDto.MasteredSpellIds.Concat(rogueRunDto.EquipmentInstances.Select(value => value.DefinitionId))
                            .Concat(rogueRunDto.TacticalItemInstances.Select(value => value.DefinitionId)))
                        .MainIds.Select(id => AcademyBattleRewardCatalog.Resource(id) ??
                            (catalog.Spells.FirstOrDefault(value => value.DefinitionId == id) is OCC.Combat.Roguelite.SpellDefinition selectedSpell
                                ? new RogueliteReward(selectedSpell, source) :
                                catalog.Equipment.FirstOrDefault(value => value.DefinitionId == id) is OCC.Combat.Roguelite.EquipmentDefinition selectedEquipment
                                    ? new RogueliteReward(selectedEquipment, source) :
                                    new RogueliteReward(catalog.TacticalItems.Single(value => value.DefinitionId == id), source))).ToArray();
                return service.Roll(Seed + completed.Count, source, OCC.Combat.Roguelite.SpellRarity.Common,
                    OCC.Combat.Roguelite.EquipmentRarity.Common, 2, 1,
                    rogueRunDto.MasteredSpellIds.Concat(rogueRunDto.EquipmentInstances.Select(value => value.DefinitionId)))
                    .Select(entry => entry.Kind == "spell"
                        ? new RogueliteReward(catalog.Spells.Single(value => value.DefinitionId == entry.DefinitionId), entry.Source)
                        : new RogueliteReward(catalog.Equipment.Single(value => value.DefinitionId == entry.DefinitionId), entry.Source)).ToArray();
            }
        }
        public IReadOnlyList<FireSpellDefinition> CurrentFireSpellChoices
        {
            get
            {
                if (IsTutorialPhase) return Array.Empty<FireSpellDefinition>();
                if (rogueRunDto != null) return Array.Empty<FireSpellDefinition>();
                if (pendingFireSpellReselections.Count > 0)
                {
                    FireSpellSaveMigrationClaim claim = pendingFireSpellReselections[0];
                    return FireSpellCatalog.All.Where(spell => spell.Rarity == claim.Rarity && !ownedFireSpells.Contains(spell.Id) && FireSpellCatalog.IsWeaponCompatible(spell, EquippedWeapon))
                        .OrderBy(spell => StableChoiceKey(claim.ClaimId, spell.Id)).ThenBy(spell => spell.Id, StringComparer.Ordinal).Take(3).ToArray();
                }
                return AwaitingReward
                    ? FireSpellRewardPool.RollPersonalChoices(Seed, completed.Count, RogueliteMapCatalog.Node(CurrentNodeId).Type, ownedFireSpells, EquippedWeapon)
                    : Array.Empty<FireSpellDefinition>();
            }
        }
        public string CurrentEventId => nodeContentAssignments.TryGetValue(CurrentNodeId, out string id) ? id : string.Empty;
        public AcademyEventDefinition CurrentEvent => string.IsNullOrEmpty(CurrentEventId) ? null : AcademyNodeContentCatalog.Event(CurrentEventId);
        public IReadOnlyList<RogueliteNodeContentChoice> CurrentContentChoices
        {
            get
            {
                RogueliteMapNode node = MapNode(CurrentNodeId);
                if (IsTutorialPhase)
                {
                    FirstRunEventSnapshot currentEvent = FirstRunExperience.EventForNode(CurrentNodeId);
                    if (currentEvent == null || !string.IsNullOrEmpty(currentEvent.SelectedOptionId)) return Array.Empty<RogueliteNodeContentChoice>();
                    if (CurrentNodeId == "EV1") return new[]
                    {
                        new RogueliteNodeContentChoice("FIRST-EV1-ACCEPT-DELIVERY", "接下温室传令", "获得轻装传令衣与承力合金×1；装备放入背包，可稍后比较与换装。", RogueliteNodeContentEffect.Reward, "ACA-EQ-CH04")
                    };
                    if (CurrentNodeId == "EV2") return new[]
                    {
                        new RogueliteNodeContentChoice("FIRST-EV2-CONTRIBUTION", "领取学院贡献", "获得导位罗盘、学院食材×1与学院贡献+1；罗盘自动放入空战术栏。", RogueliteNodeContentEffect.Intelligence, "G-T09", contributionGain: 1),
                        new RogueliteNodeContentChoice("FIRST-EV2-GOLD", "领取金币", "获得导位罗盘、学院食材×1与金币+3；罗盘自动放入空战术栏。", RogueliteNodeContentEffect.Economy, "G-T09", goldGain: 3)
                    };
                    if (CurrentNodeId == "EV3") return new[]
                    {
                        new RogueliteNodeContentChoice("FIRST-EV3-CONTRIBUTION", "领取学院贡献", "获得增幅刻墨×1与学院贡献+1。", RogueliteNodeContentEffect.Intelligence, contributionGain: 1),
                        new RogueliteNodeContentChoice("FIRST-EV3-REACTION-BELL", "领取截击铃", "获得增幅刻墨×1与完整2次截击铃；法宝放入背包。", RogueliteNodeContentEffect.Reward, "G-T10")
                    };
                    return currentEvent.OptionIds.Select(id => new RogueliteNodeContentChoice(id, "待定内容（占位）", "该选项尚未冻结，仅保留流程与存档接口。", RogueliteNodeContentEffect.Intelligence)).ToArray();
                }
                if (node.Type == RogueliteMapNodeType.Event)
                {
                    if (UsesRogue11 && string.IsNullOrEmpty(CurrentEventId)) return AcademyNodeContentCatalog.FunctionChoices(node);
                    return UsesRogue11
                        ? RogueliteNodeContentCatalog.ChoicesFor(node, CurrentEventId)
                        : RogueliteNodeContentCatalog.ChoicesFor(node);
                }
                return UsesRogue11 ? AcademyNodeContentCatalog.FunctionChoices(node) : RogueliteNodeContentCatalog.ChoicesFor(node);
            }
        }
        public bool HasPendingContentCombat => !string.IsNullOrEmpty(PendingContentCombatMissionId);
        public RogueliteMapRun(int seed)
        {
            Seed = seed; RegionBossId = "core_overseer"; Inventory = new InventoryContainerState();
            ReplaceEncounterAssignments(RogueliteEncounterCatalog.GenerateAssignments(seed));
            ReplaceNodeContentAssignments(AcademyNodeContentCatalog.GenerateAssignments(seed));
            GrantItem("medkit", 0); GrantItem("shield_cell", 1);
        }

        public bool TryGetEncounter(string nodeId, out RogueliteEncounterDefinition encounter)
        {
            if (encounterAssignments.TryGetValue(nodeId ?? string.Empty, out string variantKey))
            { encounter = RogueliteEncounterCatalog.Package(variantKey).BindToNode(nodeId); return true; }
            encounter = null; return false;
        }

        private void ReplaceEncounterAssignments(IEnumerable<RogueliteEncounterAssignment> assignments)
        {
            encounterAssignments.Clear();
            foreach (RogueliteEncounterAssignment assignment in assignments ?? Array.Empty<RogueliteEncounterAssignment>())
                encounterAssignments[assignment.NodeId] = assignment.VariantKey;
        }

        private void ReplaceNodeContentAssignments(IEnumerable<AcademyEventAssignment> assignments)
        {
            nodeContentAssignments.Clear();
            foreach (AcademyEventAssignment assignment in assignments ?? Array.Empty<AcademyEventAssignment>())
                nodeContentAssignments[assignment.NodeId] = assignment.EventId;
        }

        public RogueliteMapRun(int seed, string starterId) : this(seed)
        {
            if (!FireRogueliteStarterCatalog.All.Contains(starterId)) throw new ArgumentException("Unknown fire roguelite starter.", nameof(starterId));
            StarterId = starterId;
            if (starterId == FireRogueliteStarterCatalog.Melee) InitializeStarter("war_hammer", "F-P-M01", "F-P-M02");
            else if (starterId == FireRogueliteStarterCatalog.Ranged) InitializeStarter("arcane_wand", "F-P-R01", "F-P-R03");
            else InitializeStarter(null, "F-P-U01", "F-P-U02");
        }

        private void InitializeStarter(string weaponId, params string[] spells)
        {
            EquippedWeaponId = weaponId;
            if (!string.IsNullOrEmpty(weaponId)) claimedRewards.Add(weaponId);
            foreach (string spellId in spells) ownedFireSpells.Add(spellId);
            for (int i = 0; i < Math.Min(equippedFireSpells.Length, spells.Length); i++) equippedFireSpells[i] = spells[i];
            for (int i = 0; i < Math.Min(4, spells.Length); i++) rogueEquippedSpellIds[4 + i] = spells[i];
        }

        public ItemInstance GrantItem(string definitionId, int quickbarSlot = -1)
        {
            DeterministicItemIdAllocator allocator = new DeterministicItemIdAllocator(Seed, nextItemSequence);
            ItemInstance item = new ItemInstance(allocator.Next(definitionId), definitionId, nextItemSequence); nextItemSequence = allocator.NextValue;
            InventoryResult result = Inventory.AddFirstFit(item); if (!result.Success) throw new InvalidOperationException("Inventory cannot accept reward: " + result.Error);
            if (quickbarSlot >= 0 && quickbarSlot < ItemQuickbar.Length) ItemQuickbar[quickbarSlot] = item.InstanceId;
            return item;
        }

        public InventoryResult EquipInventoryItem(string instanceId, int quickbarSlot)
        {
            if (quickbarSlot < 0 || quickbarSlot >= ItemQuickbar.Length) return new InventoryResult(InventoryError.OutOfBounds, instanceId);
            ItemInstance item = Inventory.Get(instanceId); if (item == null) return new InventoryResult(InventoryError.MissingInstance, instanceId);
            ItemDefinition definition = ItemCatalog.Get(item.DefinitionId); if (!definition.CanQuickEquip) return new InventoryResult(InventoryError.Restricted, instanceId);
            string replacedId = ItemQuickbar[quickbarSlot];
            int specialCount = ItemQuickbar.Where(id => !string.IsNullOrEmpty(id) && id != replacedId && id != instanceId).Select(id => Inventory.Get(id)).Where(value => value != null).Count(value => { ItemCategory c = ItemCatalog.Get(value.DefinitionId).Category; return c == ItemCategory.Scroll || c == ItemCategory.Artifact; });
            if ((definition.Category == ItemCategory.Scroll || definition.Category == ItemCategory.Artifact) && specialCount >= 4) return new InventoryResult(InventoryError.QuickbarFull, instanceId);
            for (int i = 0; i < ItemQuickbar.Length; i++) if (ItemQuickbar[i] == instanceId) ItemQuickbar[i] = null;
            ItemQuickbar[quickbarSlot] = instanceId; return InventoryResult.Ok(instanceId, quickbarSlot, 0);
        }

        public void CaptureCombatInventory(CombatState combat)
        {
            if (combat == null) throw new ArgumentNullException(nameof(combat));
            if (rogueRunDto != null)
            {
                UnitState rogueHero = combat.GetUnit("hero");
                if (rogueHero != null) { CurrentHealth = rogueRunDto.CurrentHealth = rogueHero.Health; CurrentMana = rogueRunDto.CurrentMana = rogueHero.Mana; CurrentShield = 0; HasCombatSnapshot = true; }
                combat.RogueEquipment?.WriteToDto(rogueRunDto);
                if (combat.LootSource != null)
                {
                    lootProgress[combat.LootSource.Id] = combat.LootSource.ToProgressString();
                    if (IsTutorialPhase) FirstRunExperience.LootProgress[combat.LootSource.Id] = combat.LootSource.ToProgressString();
                }
                return;
            }
            Inventory = combat.ItemInventory.Clone(); ItemQuickbar = combat.ItemQuickbar.ToArray();
            if (combat.LootSource != null) lootProgress[combat.LootSource.Id] = combat.LootSource.ToProgressString();
            UnitState hero = combat.GetUnit("hero");
            if (hero != null)
            {
                HasCombatSnapshot = true; CurrentHealth = hero.Health; CurrentShield = hero.Shield; CurrentMana = hero.Mana;
            }
        }
        public void RestoreLootProgress(LootSourceState loot)
        {
            if (loot == null) return;
            if (IsTutorialPhase && FirstRunExperience.LootProgress.TryGetValue(loot.Id, out string firstRunProgress)) loot.RestoreProgress(firstRunProgress);
            else if (lootProgress.TryGetValue(loot.Id, out string progress)) loot.RestoreProgress(progress);
        }

        public bool IsAdjacentToCurrent(string nodeId)
        {
            RogueliteMapNode current = ResolveNode(CurrentNodeId);
            RogueliteMapNode target = ResolveNode(nodeId);
            if (current == null || target == null) return false;
            return current.NextIds.Contains(target.Id) || target.NextIds.Contains(current.Id);
        }

        /// <summary>
        /// 阶段权威的节点解析。固定段与学院层共用少量节点 id（academy_gate/dorm_drill 等）但连线不同，
        /// 所以学院层一律以完整节点表为准，固定段一律以固定段节点表为准。
        /// </summary>
        private RogueliteMapNode ResolveNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (IsInAcademyLayer && generatedAcademyMapNodes.Count > 0)
                return generatedAcademyMapNodes.FirstOrDefault(node => node.Id == id);
            if (RogueliteAcademyLayerCatalog.TryResolveLayerNode(id, out RogueliteMapNode layerNode)) return layerNode;
            if (IsTutorialPhase)
                return FirstRunExperienceCatalog.MapNodes.FirstOrDefault(node => node.Id == id);
            return RogueliteMapCatalog.Nodes.FirstOrDefault(node => node.Id == id);
        }

        private RogueliteMapNode TryMapNode(string id) => ResolveNode(id);

        public static RogueliteMapRun CreateFirstRunV1(int seed)
        {
            RogueliteMapRun run = new RogueliteMapRun(seed, FireRogueliteStarterCatalog.Universal)
            {
                FirstRunExperience = FirstRunExperienceCatalog.CreatePhaseA(),
                CurrentNodeId = FirstRunExperienceCatalog.OriginNodeId,
                RegionBossId = string.Empty,
                AwaitingReward = false
            };
            run.visited.Clear();
            run.visited.Add(FirstRunExperienceCatalog.OriginNodeId);
            run.routeHistory.Clear();
            run.routeHistory.Add(FirstRunExperienceCatalog.OriginNodeId);
            run.completed.Clear();
            run.claimedRewards.Clear();
            run.encounterAssignments.Clear();
            run.nodeContentAssignments.Clear();
            run.rogueRunDto = OCC.Combat.Roguelite.RogueRunDto.CreateNew("first-run-" + seed, seed);
            run.rogueRunDto.RunProgramId = RogueliteRunProgram.FirstRunV1.ToString();
            run.rogueRunDto.FirstRunExperience = run.FirstRunExperience;
            run.SyncFirstRunProjection();
            return run;
        }
        public static RogueliteMapRun CreateSubsequentAcademyRun(int seed)
        {
            RogueliteMapRun run = new RogueliteMapRun(seed, FireRogueliteStarterCatalog.Universal)
            {
                IsInAcademyLayer = true,
                CurrentNodeId = "academy_gate",
                RegionBossId = "core_overseer",
                generatedAcademyMapNodes = RogueliteAcademyMapGenerator.Generate(seed),
                HasCombatSnapshot = true,
                CurrentShield = 0
            };
            var selectedIds = new HashSet<string>(run.generatedAcademyMapNodes.Select(node => node.Id), StringComparer.Ordinal);
            run.ReplaceEncounterAssignments(RogueliteAcademyLayerCatalog.GenerateEncounterAssignments(seed)
                .Where(value => selectedIds.Contains(value.NodeId)));
            run.ReplaceNodeContentAssignments(RogueliteAcademyLayerCatalog.GenerateNodeContentAssignments()
                .Where(value => selectedIds.Contains(value.NodeId)));
            run.visited.Clear(); run.visited.Add("academy_gate"); run.visited.Add("dorm_drill");
            run.routeHistory.Clear(); run.routeHistory.Add("academy_gate");
            run.completed.Clear(); run.claimedRewards.Clear();
            run.rogueRunDto = OCC.Combat.Roguelite.RogueRunDto.CreateNew("academy-" + seed, seed);
            OCC.Combat.Roguelite.RogueEquipmentRuntime.CreateStarter(seed).WriteToDto(run.rogueRunDto);
            run.rogueRunDto.RunProgramId = RogueliteRunProgram.EvergreenAcademy.ToString();
            run.rogueRunDto.DeparturePending = true;
            run.SyncAcademyLayerProjection();
            return run;
        }
        public void ConfirmAcademyDeparture()
        {
            if (!IsInAcademyLayer || !DeparturePending || IsComplete)
                throw new InvalidOperationException("There is no academy departure to confirm.");
            OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
            rogueRunDto.DeparturePending = false;
        }
        public void RestoreAcademyDeparturePending()
        {
            if (IsInAcademyLayer && FirstRunExperience == null && !IsComplete) rogueRunDto.DeparturePending = true;
        }
        public bool IsNodeAvailable(string nodeId)
        {
            if (IsTutorialPhase) return FirstRunExperience.FindTravelPath(nodeId).Count > 0;
            if (IsInAcademyLayer) return IsAcademyLayerNodeAvailable(nodeId);
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            if (!IsAdjacentToCurrent(nodeId)) return false;
            if (AcademyMapTuning.EnforceTransition && !visited.Contains(nodeId) && IsTransitionPending && node.Type != RogueliteMapNodeType.Finale) return false;
            if (node.Type == RogueliteMapNodeType.Finale && AcademyMapTuning.EnforceBossGate && !CanChallengeAcademyFinale) return false;
            return true;
        }
        /// <summary>随机层普通节点相邻推进；终考是达到门槛后的阶段转换。</summary>
        public bool IsAcademyLayerNodeAvailable(string nodeId)
        {
            if (DeparturePending) return false;
            if (!AcademyLayerNodes.Any(value => value.Id == nodeId)) return false;
            RogueliteMapNode node = MapNode(nodeId);
            if (CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId && node.Id != CurrentNodeId) return false;
            if (node.Type == RogueliteMapNodeType.Finale)
                return !IsComplete && !AwaitingReward && !HasActiveCombat && !completed.Contains(node.Id) &&
                    (node.Id == CurrentNodeId || completed.Contains(CurrentNodeId) || MapNode(CurrentNodeId).Type == RogueliteMapNodeType.Start) &&
                    (!AcademyMapTuning.EnforceBossGate || CanChallengeAcademyFinale);
            if (IsTransitionPending && node.Id != CurrentNodeId) return false;
            if (node.Id != CurrentNodeId && !IsAdjacentToCurrent(nodeId)) return false;
            return true;
        }
        public bool IsAcademyFinaleGateLocked(RogueliteMapNode node)
        {
            return node != null && node.Type == RogueliteMapNodeType.Finale && AcademyMapTuning.EnforceBossGate && !CanChallengeAcademyFinale;
        }
        public bool IsNodeKnown(string nodeId) => IsTutorialPhase
            ? (FirstRunExperience.Node(nodeId).Flags & FirstRunNodeFlags.Locked) == 0 || CompletedNodes.Contains(nodeId)
            : visited.Contains(nodeId) || MapNode(CurrentNodeId).NextIds.Contains(nodeId);
        public RogueliteMapNodeVisualState VisualStateFor(string nodeId)
        {
            RogueliteMapNode node = MapNode(nodeId);
            if (node.Id == CurrentNodeId) return RogueliteMapNodeVisualState.Current;
            if (CompletedNodes.Contains(node.Id)) return RogueliteMapNodeVisualState.Cleared;
            if (IsNodeAvailable(node.Id)) return RogueliteMapNodeVisualState.Available;
            if (IsTutorialPhase && (FirstRunExperience.Node(node.Id).Flags & FirstRunNodeFlags.Locked) != 0) return RogueliteMapNodeVisualState.Locked;
            if (IsAdjacentToCurrent(node.Id) && IsAcademyFinaleGateLocked(node)) return RogueliteMapNodeVisualState.Locked;
            if (visited.Contains(node.Id) || IsNodeKnown(node.Id)) return RogueliteMapNodeVisualState.Known;
            return RogueliteMapNodeVisualState.Unknown;
        }
        /// <summary>
        /// 当前随机层里玩家能实际前往的节点。学院层与旧郊道在图上仍有少量跨层连接，
        /// 但一层的内容只包含学院节点，因此可前往列表不把旧郊道节点算进来。
        /// </summary>
        public IReadOnlyList<RogueliteMapNode> AvailableNodes => IsInAcademyLayer
            ? MapNodes.Where(node => RogueliteAcademyLayerCatalog.IsAcademyLayerNode(node.Id) && IsNodeAvailable(node.Id)).ToArray()
            : MapNodes.Where(node => IsNodeAvailable(node.Id)).ToArray();
        public void SelectNode(string nodeId)
        {
            if (IsTutorialPhase)
            {
                FirstRunExperience.TravelTo(nodeId);
                visited.Add(nodeId);
                routeHistory.Add(nodeId);
                SyncFirstRunProjection();
                return;
            }
            if (IsInAcademyLayer)
            {
                if (!IsAcademyLayerNodeAvailable(nodeId)) throw new InvalidOperationException(IsAcademyFinaleGateLocked(RogueliteMapCatalog.Node(nodeId))
                    ? "Academy finale requires " + AcademyMapTuning.BossMinimumProgress + " completed academy nodes."
                    : "Node is not adjacent or is unavailable in the current stage.");
                CurrentNodeId = nodeId; visited.Add(nodeId); routeHistory.Add(nodeId);
                if (rogueRunDto != null) rogueRunDto.CurrentNodeId = nodeId;
                RogueliteMapNode selected = MapNode(nodeId);
                if (IsServiceNodeType(selected.Type)) BeginLayerServiceSession(nodeId, selected.Type);
                return;
            }
            if (!IsNodeAvailable(nodeId)) throw new InvalidOperationException(IsAcademyFinaleGateLocked(RogueliteMapCatalog.Node(nodeId))
                ? "Academy finale requires " + AcademyMapTuning.BossMinimumProgress + " completed nodes."
                : "Node is not adjacent or is unavailable in the current stage.");
            CurrentNodeId = nodeId; visited.Add(nodeId); routeHistory.Add(nodeId);
        }

        /// <summary>
        /// 随机层里重复经过已经清理过的节点。玩家在同一层可以回走，因此这里跳过“未完成”限制，
        /// 但仍然要求相邻，并且把当前节点同步进存档。
        /// </summary>
        internal void TravelToVisitedAcademyNode(string nodeId)
        {
            if (!IsInAcademyLayer) throw new InvalidOperationException("Revisiting cleared nodes is only available inside the academy layer.");
            if (IsTransitionPending || CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId)
                throw new InvalidOperationException("The academy finale transition has closed ordinary routes.");
            if (!AcademyLayerNodes.Any(node => node.Id == nodeId)) throw new InvalidOperationException("Node is outside the academy layer: " + nodeId);
            if (!completed.Contains(nodeId)) throw new InvalidOperationException("Only cleared academy nodes can be revisited: " + nodeId);
            if (!IsAdjacentToCurrent(nodeId)) throw new InvalidOperationException("Revisit target is not adjacent to the current node: " + nodeId);
            CurrentNodeId = nodeId; visited.Add(nodeId); routeHistory.Add(nodeId);
            if (rogueRunDto != null) rogueRunDto.CurrentNodeId = nodeId;
            RogueliteMapNode revisited = MapNode(nodeId);
            if (IsServiceNodeType(revisited.Type)) BeginLayerServiceSession(nodeId, revisited.Type);
        }
        public void CompleteCurrentCombat()
        {
            if (IsTutorialPhase)
            {
                FirstRunExperience.CompleteCombat(CurrentNodeId);
                SyncFirstRunProjection();
                return;
            }
            RogueliteMapNode node = MapNode(CurrentNodeId);
            if (!node.IsCombat || completed.Contains(node.Id)) throw new InvalidOperationException("Current node is not an active combat.");            Complete(node, true);
        }
        public void CompleteCurrentNode()
        {
            if (IsTutorialPhase) throw new InvalidOperationException("Use the typed first-run event or service action.");
            RogueliteMapNode node = MapNode(CurrentNodeId);
            if (node.Type == RogueliteMapNodeType.Start || completed.Contains(node.Id)) throw new InvalidOperationException("Current node is not available.");
            Complete(node, node.IsCombat);
        }
        public void ChooseCurrentNodeContent(string choiceId)
        {
            if (IsTutorialPhase)
            {
                RogueliteNodeContentChoice firstRunChoice = CurrentContentChoices.FirstOrDefault(value => value.Id == choiceId)
                    ?? throw new InvalidOperationException("Event option is not available.");
                OCC.Combat.Roguelite.RogueEquipmentRuntime equipment = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
                if (CurrentNodeId == "EV1")
                {
                    string instanceId = "eq-first-event-" + Seed + "-" + rogueRunDto.DeterministicCounter;
                    OCC.Combat.Roguelite.RogueEquipmentInstance item = equipment.CreateInstance(instanceId, "ACA-EQ-CH04",
                        OCC.Combat.Roguelite.EquipmentRarity.Uncommon, equipment.AllInstances.Count + equipment.AllTacticalItems.Count, "first-run:EV1");
                    if (!equipment.AddToBackpack(item)) throw new InvalidOperationException("背包没有足够空间容纳轻装传令衣。");
                    rogueRunDto.DeterministicCounter++;
                }
                else if (CurrentNodeId == "EV2")
                {
                    string instanceId = "item-first-event-" + Seed + "-" + rogueRunDto.DeterministicCounter;
                    OCC.Combat.Roguelite.RogueTacticalItemInstance item = equipment.CreateTacticalItem(instanceId, "G-T09",
                        equipment.AllInstances.Count + equipment.AllTacticalItems.Count, "first-run:EV2");
                    if (!equipment.AddTacticalToBackpack(item)) throw new InvalidOperationException("背包没有足够空间容纳导位罗盘。");
                    int emptyQuickbar = Array.FindIndex(equipment.ItemQuickbarInstanceIds, string.IsNullOrEmpty);
                    if (emptyQuickbar < 0 || !equipment.AssignQuickbar(emptyQuickbar, item.InstanceId))
                        throw new InvalidOperationException("没有空余战术栏可放置导位罗盘。");
                    rogueRunDto.DeterministicCounter++;
                }
                else if (CurrentNodeId == "EV3")
                {
                    if (!string.IsNullOrEmpty(firstRunChoice.RewardId))
                    {
                        string instanceId = "item-first-event-" + Seed + "-" + rogueRunDto.DeterministicCounter;
                        OCC.Combat.Roguelite.RogueTacticalItemInstance item = equipment.CreateTacticalItem(instanceId, firstRunChoice.RewardId,
                            equipment.AllInstances.Count + equipment.AllTacticalItems.Count, "first-run:EV3");
                        if (!equipment.AddTacticalToBackpack(item)) throw new InvalidOperationException("背包没有足够空间容纳截击铃。");
                        rogueRunDto.DeterministicCounter++;
                    }
                }
                FirstRunExperience.ChooseEvent(CurrentNodeId, choiceId);
                equipment.WriteToDto(rogueRunDto);
                rogueRunDto.Gold += firstRunChoice.GoldGain;
                rogueRunDto.StageContribution += firstRunChoice.ContributionGain;
                if (!string.IsNullOrEmpty(firstRunChoice.RewardId) && !claimedRewards.Contains(firstRunChoice.RewardId)) claimedRewards.Add(firstRunChoice.RewardId);
                SyncFirstRunProjection();
                return;
            }
            RogueliteMapNode node = MapNode(CurrentNodeId);
            if (node.IsCombat || completed.Contains(node.Id) || HasPendingContentCombat) throw new InvalidOperationException("Current node content is not available.");
            RogueliteNodeContentChoice choice = ResolveContentChoice(node, choiceId);
            int goldBefore = rogueRunDto?.Gold ?? 0;
            int contributionBefore = rogueRunDto?.StageContribution ?? 0;
            int healthBefore = rogueRunDto?.CurrentHealth ?? 0;
            int manaBefore = rogueRunDto?.CurrentMana ?? 0;
            int timeBefore = rogueRunDto?.StageTime ?? 0;
            if (rogueRunDto == null)
            {
                if (Parts < choice.PartsCost || Aether < choice.AetherCost) throw new InvalidOperationException("Insufficient parts or aether.");
                Parts -= choice.PartsCost; Aether -= choice.AetherCost;
            }
            else
            {
                if (rogueRunDto.Gold < choice.GoldCost || rogueRunDto.StageContribution < choice.ContributionCost)
                    throw new InvalidOperationException("Insufficient gold or stage contribution.");
                if (choice.Id == "EV08_recover" && rogueRunDto.CurrentMana >= OCC.Combat.Roguelite.RogueRuntimeConstants.MaximumPersonalMana)
                    throw new InvalidOperationException("Personal mana is already full.");
                if (choice.HealthGain < 0 && rogueRunDto.CurrentHealth + choice.HealthGain <= 0)
                    throw new InvalidOperationException("This choice would reduce health to zero.");
                if (!string.IsNullOrEmpty(choice.RewardId) && claimedRewards.Contains(choice.RewardId))
                    throw new InvalidOperationException("Unique node content was already claimed.");
                if (!CanAcceptRogue11Content(choice.RewardId))
                    throw new InvalidOperationException("Backpack cannot accept node content reward.");
                rogueRunDto.Gold -= choice.GoldCost;
                rogueRunDto.StageContribution -= choice.ContributionCost;
            }
            PendingContentChoiceId = choice.Id;
            if (choice.RequiresCombat)
            {
                PendingContentCombatMissionId = choice.CombatMissionId;
                if (rogueRunDto != null && rogueRunDto.PendingRewardIds != null && !string.IsNullOrEmpty(choice.RewardId) && !rogueRunDto.PendingRewardIds.Contains(choice.RewardId))
                    rogueRunDto.PendingRewardIds.Add(choice.RewardId);
                return;
            }
            if (rogueRunDto != null && rogueRunDto.PendingRewardIds != null && !string.IsNullOrEmpty(choice.RewardId) && !rogueRunDto.PendingRewardIds.Contains(choice.RewardId))
            {
                rogueRunDto.PendingRewardIds.Add(choice.RewardId);
            }
            ApplyContentChoice(choice); Complete(node, !UsesRogue11 && node.Type == RogueliteMapNodeType.Treasure); PendingContentChoiceId = null;
            if (rogueRunDto != null && rogueRunDto.PendingRewardIds != null && !string.IsNullOrEmpty(choice.RewardId)) AwaitingReward = true;
            else if (IsInAcademyLayer && rogueRunDto != null && node.Type == RogueliteMapNodeType.Event)
            {
                // Resources are committed now; F40 acknowledges the saved result without
                // applying them again. The snapshot survives quit/reload and failed writes.
                rogueRunDto.PendingResourceReceipt = new AcademyResourceReceipt(node.Id, choice.Id,
                    rogueRunDto.Gold - goldBefore, rogueRunDto.StageContribution - contributionBefore,
                    rogueRunDto.CurrentHealth - healthBefore, rogueRunDto.CurrentMana - manaBefore,
                    rogueRunDto.StageTime - timeBefore).Encode();
                AwaitingReward = true;
            }
        }

        public void ConfirmResourceReceipt()
        {
            if (!AwaitingReward || PendingResourceReceipt == null)
                throw new InvalidOperationException("No resource receipt is awaiting confirmation.");
            rogueRunDto.PendingResourceReceipt = string.Empty;
            AwaitingReward = false;
        }
        public void CompletePendingContentCombat()
        {
            if (!HasPendingContentCombat) throw new InvalidOperationException("No event combat is active.");
            RogueliteMapNode current = MapNode(CurrentNodeId);
            RogueliteNodeContentChoice choice = ResolveContentChoice(current, PendingContentChoiceId);
            int goldBefore = rogueRunDto?.Gold ?? 0;
            int contributionBefore = rogueRunDto?.StageContribution ?? 0;
            int healthBefore = rogueRunDto?.CurrentHealth ?? 0;
            int manaBefore = rogueRunDto?.CurrentMana ?? 0;
            int timeBefore = rogueRunDto?.StageTime ?? 0;
            ApplyContentChoice(choice); Complete(current, false);
            PendingContentChoiceId = null; PendingContentCombatMissionId = null;
            if (rogueRunDto != null && rogueRunDto.PendingRewardIds != null && !string.IsNullOrEmpty(choice.RewardId)) AwaitingReward = true;
            else if (IsInAcademyLayer && rogueRunDto != null && current.Type == RogueliteMapNodeType.Event)
            {
                rogueRunDto.PendingResourceReceipt = new AcademyResourceReceipt(current.Id, choice.Id,
                    rogueRunDto.Gold - goldBefore, rogueRunDto.StageContribution - contributionBefore,
                    rogueRunDto.CurrentHealth - healthBefore, rogueRunDto.CurrentMana - manaBefore,
                    rogueRunDto.StageTime - timeBefore).Encode();
                AwaitingReward = true;
            }
        }
        private RogueliteNodeContentChoice ResolveContentChoice(RogueliteMapNode node, string choiceId)
        {
            RogueliteNodeContentChoice choice = CurrentContentChoices.FirstOrDefault(item => item.Id == choiceId);
            if (choice == null && !UsesRogue11)
                choice = RogueliteNodeContentCatalog.ChoicesFor(node).FirstOrDefault(item => item.Id == choiceId);
            return choice ?? throw new InvalidOperationException("Unknown node content choice.");
        }
        public void FailCurrentCombatSurvived()
        {
            RogueliteMapNode node = MapNode(CurrentNodeId);
            if ((!node.IsCombat && !HasPendingContentCombat) || completed.Contains(node.Id))
                throw new InvalidOperationException("Current node has no active combat to fail.");
            Complete(node, false, OCC.Combat.Roguelite.RogueEncounterOutcome.SurvivedFailure);
            if (rogueRunDto != null && rogueRunDto.PendingRewardIds != null && !string.IsNullOrEmpty(PendingContentChoiceId))
            {
                RogueliteNodeContentChoice failedChoice = ResolveContentChoice(node, PendingContentChoiceId);
                if (!string.IsNullOrEmpty(failedChoice.RewardId)) rogueRunDto.PendingRewardIds.Remove(failedChoice.RewardId);
            }
            PendingContentChoiceId = null; PendingContentCombatMissionId = null;
        }
        private void ApplyContentChoice(RogueliteNodeContentChoice choice)
        {
            if (rogueRunDto != null)
            {
                rogueRunDto.Gold += choice.GoldGain;
                rogueRunDto.StageContribution += choice.ContributionGain;
                rogueRunDto.CurrentHealth = Math.Max(1, Math.Min(UnitState.HeroBaseHealth, rogueRunDto.CurrentHealth + choice.HealthGain));
                rogueRunDto.CurrentMana = Math.Max(0, Math.Min(RogueManaCapacity, rogueRunDto.CurrentMana + choice.ManaGain));
                CurrentHealth = rogueRunDto.CurrentHealth; CurrentMana = rogueRunDto.CurrentMana;
                string contentSourceId = string.IsNullOrEmpty(CurrentEventId) ? CurrentNodeId : CurrentEventId;
                if (!string.IsNullOrEmpty(choice.RewardId) && (rogueRunDto.PendingRewardIds == null || !rogueRunDto.PendingRewardIds.Contains(choice.RewardId))) GrantRogue11Content(choice.RewardId, "event:" + contentSourceId);
                return;
            }
            if (choice.Effect == RogueliteNodeContentEffect.Supplies) Supplies++;
            else if (choice.Effect == RogueliteNodeContentEffect.ScoutingBeacon) ScoutingBeacons++;
            else if (choice.Effect == RogueliteNodeContentEffect.Aether) { Supplies++; Aether++; }
            else if (choice.Effect == RogueliteNodeContentEffect.Recovery)
            {
                Supplies++;
                if (HasCombatSnapshot)
                {
                    CurrentHealth = Math.Min(UnitState.HeroBaseHealth, CurrentHealth + 6);
                    if (CurrentShield < 6) CurrentShield = Math.Min(6, CurrentShield + 2);
                    CurrentMana = Math.Min(RogueManaCapacity, CurrentMana + 4);
                }
            }
            else if (choice.Effect == RogueliteNodeContentEffect.Reward && !string.IsNullOrEmpty(choice.RewardId) && !claimedRewards.Contains(choice.RewardId))
            {
                ItemDefinition item = ItemCatalog.All.FirstOrDefault(candidate => candidate.Id == choice.RewardId);
                if (item != null) GrantItem(item.Id);
                claimedRewards.Add(choice.RewardId);
            }
        }
        private void GrantRogue11Content(string rewardId, string source, bool allowRepeat = false)
        {
            if (!allowRepeat && claimedRewards.Contains(rewardId)) return;
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            OCC.Combat.Roguelite.SpellDefinition spell = catalog.Spells.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (spell != null)
            {
                if (!rogueRunDto.MasteredSpellIds.Contains(rewardId)) rogueRunDto.MasteredSpellIds.Add(rewardId);
                int empty = Array.FindIndex(rogueRunDto.EquippedSpellIds, string.IsNullOrEmpty);
                if (empty >= 0) rogueRunDto.EquippedSpellIds[empty] = rewardId;
                if (!claimedRewards.Contains(rewardId)) claimedRewards.Add(rewardId); return;
            }
            OCC.Combat.Roguelite.EquipmentDefinition equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (equipment != null)
            {
                OCC.Combat.Roguelite.RogueEquipmentRuntime equipmentRuntime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
                string instanceId = "eq-content-" + Seed + "-" + rogueRunDto.DeterministicCounter;
                OCC.Combat.Roguelite.RogueEquipmentInstance instance = equipmentRuntime.CreateInstance(instanceId, rewardId,
                    equipment.AllowedRarities[0], equipmentRuntime.AllInstances.Count + equipmentRuntime.AllTacticalItems.Count, source);
                if (!equipmentRuntime.AddToBackpack(instance)) throw new InvalidOperationException("Backpack cannot accept equipment reward: " + rewardId);
                rogueRunDto.DeterministicCounter++;
                equipmentRuntime.WriteToDto(rogueRunDto);
                if (!claimedRewards.Contains(rewardId)) claimedRewards.Add(rewardId); return;
            }
            OCC.Combat.Roguelite.TacticalItemDefinition tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (tactical == null) throw new InvalidOperationException("Unknown node content reward: " + rewardId);
            if (!ArtifactCatalog.IsCurrentlyUsable(rewardId)) throw new InvalidOperationException("Node content reward is retired pending rewrite: " + rewardId);
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
            string tacticalId = "item-content-" + Seed + "-" + rogueRunDto.DeterministicCounter++;
            OCC.Combat.Roguelite.RogueTacticalItemInstance item = runtime.CreateTacticalItem(tacticalId, rewardId,
                runtime.AllInstances.Count + runtime.AllTacticalItems.Count, source);
            if (!runtime.AddTacticalToBackpack(item)) throw new InvalidOperationException("Backpack cannot accept node content reward: " + rewardId);
            runtime.WriteToDto(rogueRunDto);
            if (!claimedRewards.Contains(rewardId)) claimedRewards.Add(rewardId);
        }
        private bool CanAcceptRogue11Content(string rewardId)
        {
            if (string.IsNullOrEmpty(rewardId)) return true;
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            if (catalog.Spells.Any(value => value.DefinitionId == rewardId)) return true;
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
            OCC.Combat.Roguelite.EquipmentDefinition equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (equipment != null)
            {
                OCC.Combat.Roguelite.RogueEquipmentInstance preview = runtime.CreateInstance("__content_preview__", rewardId,
                    equipment.AllowedRarities[0], int.MaxValue, "preview");
                return runtime.AddToBackpack(preview);
            }
            OCC.Combat.Roguelite.TacticalItemDefinition tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (tactical == null) return true;
            if (!ArtifactCatalog.IsCurrentlyUsable(rewardId)) return false;
            OCC.Combat.Roguelite.RogueTacticalItemInstance item = runtime.CreateTacticalItem("__content_preview__", rewardId, int.MaxValue, "preview");
            return runtime.AddTacticalToBackpack(item);
        }
        private void Complete(RogueliteMapNode node, bool offerReward,
            OCC.Combat.Roguelite.RogueEncounterOutcome outcome = OCC.Combat.Roguelite.RogueEncounterOutcome.Success)
        {
            completed.Add(node.Id); Experience++; if (Experience >= Level) Level++;
            int deferredGold = 0;
            int deferredContribution = 0;
            if (rogueRunDto != null)
            {
                bool failed = outcome == OCC.Combat.Roguelite.RogueEncounterOutcome.SurvivedFailure;
                bool combatSettlement = node.IsCombat || HasPendingContentCombat;
                int baseGold = combatSettlement ? 3 : node.Type == RogueliteMapNodeType.Event ? 1 : 0;
                int baseContribution = combatSettlement ? 2 : node.Type == RogueliteMapNodeType.Event ? 1 : 0;
                if (IsInAcademyLayer)
                {
                    // Stage-one event outcomes are defined by their individual choices;
                    // the regular battle's fixed payout is a separate reward package.
                    baseGold = node.Type == RogueliteMapNodeType.Finale ? 10 :
                        node.Type == RogueliteMapNodeType.Elite ? 6 :
                        node.Type == RogueliteMapNodeType.Combat ? 3 : 0;
                    baseContribution = node.Type == RogueliteMapNodeType.Finale ? 3 :
                        node.Type == RogueliteMapNodeType.Elite ? 2 :
                        node.Type == RogueliteMapNodeType.Combat ? 1 : 0;
                }
                if (IsInAcademyLayer && node.IsCombat && offerReward && !failed)
                {
                    deferredGold = baseGold;
                    deferredContribution = baseContribution;
                }
                else
                {
                    rogueRunDto.Gold += failed ? baseGold / 2 : baseGold;
                    rogueRunDto.StageContribution += failed ? baseContribution / 2 : baseContribution;
                }
                int timeCost = AcademyMapTuning.TimeCost(node);
                if (timeCost > 0) OCC.Combat.Roguelite.RogueRunProgression.ResolveEncounter(rogueRunDto, outcome, timeCost);
                else OCC.Combat.Roguelite.RogueRunProgression.ResolveZeroTimeFunction(rogueRunDto);
                CurrentHealth = rogueRunDto.CurrentHealth; CurrentMana = rogueRunDto.CurrentMana; CurrentShield = 0;
                if (failed) offerReward = false;
            }
            else if (node.IsCombat) { Parts += 2; Aether++; }
            if (rogueRunDto != null)
            {
                rogueRunDto.RolledRewardChoiceIds.Clear();
                rogueRunDto.PendingRewardFollowupIds.Clear();
                rogueRunDto.PendingFixedMaterialIds.Clear();
                rogueRunDto.PendingMainRewardId = string.Empty;
                rogueRunDto.PendingRewardGold = 0;
                rogueRunDto.PendingRewardContribution = 0;
                rogueRunDto.PendingRewardStepId = string.Empty;
            }
            AwaitingReward = offerReward;
            if (AwaitingReward && rogueRunDto != null && rogueRunDto.PendingRewardIds.Count == 0)
            {
                AcademyBattleRewardPackage package = IsInAcademyLayer && node.IsCombat
                    ? AcademyBattleRewardCatalog.Roll(Seed, node.Id,
                        rogueRunDto.MasteredSpellIds.Concat(rogueRunDto.EquipmentInstances.Select(value => value.DefinitionId))
                            .Concat(rogueRunDto.TacticalItemInstances.Select(value => value.DefinitionId)))
                    : null;
                string[] choices = package?.MainIds.ToArray() ?? CurrentRewards.Select(reward => reward.Id).ToArray();
                rogueRunDto.RolledRewardChoiceIds.AddRange(choices);
                if (package != null)
                {
                    rogueRunDto.PendingRewardFollowupIds.AddRange(package.FollowupIds);
                    rogueRunDto.PendingFixedMaterialIds.AddRange(package.FixedMaterialIds);
                }
                if (choices.Length == 0) AwaitingReward = false;
                else rogueRunDto.PendingRewardStepId = "main";
            }
            if (rogueRunDto != null && (deferredGold > 0 || deferredContribution > 0))
            {
                if (AwaitingReward)
                {
                    rogueRunDto.PendingRewardGold = deferredGold;
                    rogueRunDto.PendingRewardContribution = deferredContribution;
                }
                else
                {
                    rogueRunDto.Gold += deferredGold;
                    rogueRunDto.StageContribution += deferredContribution;
                }
            }
            if (IsInAcademyLayer && node.Type == RogueliteMapNodeType.Finale && !AwaitingReward) SettleAcademyRound();
        }

        /// <summary>
        /// 首领战结束且奖励已经落地时收束整轮：把 FirstRunExperience 推进到终态，
        /// 这是 IsComplete 唯一成立的地方。
        /// </summary>
        private void SettleAcademyRound()
        {
            if (FirstRunExperience == null)
            {
                if (rogueRunDto != null && string.IsNullOrEmpty(rogueRunDto.RunEndReason)) rogueRunDto.RunEndReason = "victory";
                return;
            }
            if (!FirstRunExperience.RoundSettled) FirstRunExperience.SettleRound();
        }
        internal void SettleAcademyRoundForDeveloper() => SettleAcademyRound();
        public void ClaimReward(string rewardId)
        {
            bool dismantle = rewardId != null && rewardId.StartsWith("dismantle:", StringComparison.Ordinal);
            if (dismantle) rewardId = rewardId.Substring("dismantle:".Length);
            if (IsTutorialPhase)
            {
                if (dismantle) throw new InvalidOperationException("Tutorial rewards cannot be dismantled here.");
                if (FirstRunExperience.Outcome == FirstRunOutcome.EliteVictory && !FirstRunExperience.EliteRewardClaimed)
                {
                    if (!FirstRunExperienceCatalog.ElitePassiveRewardIds.Contains(rewardId))
                        throw new InvalidOperationException("Elite reward is not available.");
                    GrantFirstRunEliteReward(rewardId);
                    FirstRunExperience.ClaimEliteReward(rewardId);
                }
                else
                {
                    if (CurrentRewards.All(value => value.Id != rewardId))
                        throw new InvalidOperationException("Reward is not available.");
                    GrantRogue11Content(rewardId, "first-run:combat-reward");
                    FirstRunExperience.ClaimReward(rewardId);
                }
                if (!claimedRewards.Contains(rewardId)) claimedRewards.Add(rewardId);
                SyncFirstRunProjection();
                return;
            }
            RogueliteReward reward = !AwaitingReward ? null : CurrentRewards.FirstOrDefault(value => value.Id == rewardId);
            if (reward == null) throw new InvalidOperationException("Reward is not available.");
            if (dismantle && !CanDismantleReward(reward))
                throw new InvalidOperationException("This reward cannot be dismantled.");
            if (rogueRunDto != null && !CanAcceptAcademyReward(reward, dismantle))
                throw new InvalidOperationException("Backpack cannot accept this reward and its fixed materials.");
            if (rogueRunDto != null)
            {
                if (rogueRunDto.PendingRewardIds != null && rogueRunDto.PendingRewardIds.Contains(rewardId))
                {
                    if (claimedRewards.Contains(rewardId))
                        throw new InvalidOperationException("Reward was already claimed.");
                    GrantRogue11Content(rewardId, "event:" + CurrentEventId);
                    if (reward.Kind == RogueliteRewardKind.TacticalItem)
                    {
                        OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
                        if (runtime.AllTacticalItems.All(value => value.DefinitionId != rewardId))
                        {
                            string instanceId = "item-content-" + Seed + "-" + rogueRunDto.DeterministicCounter++;
                            OCC.Combat.Roguelite.RogueTacticalItemInstance item = runtime.CreateTacticalItem(instanceId, rewardId, runtime.AllInstances.Count + runtime.AllTacticalItems.Count, "event:" + CurrentEventId);
                            if (!runtime.AddTacticalToBackpack(item)) throw new InvalidOperationException("Backpack cannot accept tactical reward: " + rewardId);
                            runtime.WriteToDto(rogueRunDto);
                        }
                    }
                    rogueRunDto.PendingRewardIds.Remove(rewardId);
                    claimedRewards.Add(rewardId);
                    AwaitingReward = rogueRunDto.PendingRewardIds.Count > 0;
                    if (!AwaitingReward) SettleAcademyRoundIfFinale();
                    return;
                }
                if (rogueRunDto.PendingRewardStepId == "main" && rogueRunDto.PendingRewardFollowupIds.Count > 0)
                {
                    rogueRunDto.PendingMainRewardId = (dismantle ? "dismantle:" : string.Empty) + rewardId;
                    rogueRunDto.RolledRewardChoiceIds.Clear();
                    rogueRunDto.RolledRewardChoiceIds.AddRange(rogueRunDto.PendingRewardFollowupIds);
                    rogueRunDto.PendingRewardFollowupIds.Clear();
                    rogueRunDto.PendingRewardStepId = "followup";
                    return;
                }
                rogueRunDto.AwaitingReward = AwaitingReward;
                string snapshot = OCC.Combat.Roguelite.Rogue11Serializer.Serialize(rogueRunDto);
                string[] priorClaims = claimedRewards.ToArray();
                try
                {
                    if (!string.IsNullOrEmpty(rogueRunDto.PendingMainRewardId))
                    {
                        bool mainDismantle = rogueRunDto.PendingMainRewardId.StartsWith("dismantle:", StringComparison.Ordinal);
                        string mainId = mainDismantle ? rogueRunDto.PendingMainRewardId.Substring("dismantle:".Length) : rogueRunDto.PendingMainRewardId;
                        RogueliteReward main = ResolveAcademyChoice(mainId) ?? throw new InvalidOperationException("Saved main reward is missing.");
                        GrantAcademyChosenReward(main, mainDismantle);
                        if (!claimedRewards.Contains(mainId)) claimedRewards.Add(mainId);
                    }
                    GrantAcademyChosenReward(reward, dismantle);
                    if (!claimedRewards.Contains(rewardId)) claimedRewards.Add(rewardId);
                    foreach (string materialId in rogueRunDto.PendingFixedMaterialIds)
                        GrantAcademyMaterial(materialId, 1);
                    rogueRunDto.Gold += rogueRunDto.PendingRewardGold;
                    rogueRunDto.StageContribution += rogueRunDto.PendingRewardContribution;
                    AwaitingReward = false;
                    rogueRunDto.PendingMainRewardId = string.Empty;
                    rogueRunDto.PendingFixedMaterialIds.Clear();
                    rogueRunDto.PendingRewardGold = 0;
                    rogueRunDto.PendingRewardContribution = 0;
                    rogueRunDto.RolledRewardChoiceIds.Clear();
                    rogueRunDto.PendingRewardFollowupIds.Clear();
                    rogueRunDto.PendingRewardStepId = string.Empty;
                    SettleAcademyRoundIfFinale();
                }
                catch
                {
                    rogueRunDto = OCC.Combat.Roguelite.Rogue11Serializer.Deserialize(snapshot);
                    claimedRewards.Clear(); claimedRewards.AddRange(priorClaims);
                    AwaitingReward = true;
                    throw;
                }
                return;
            }
            if (reward.Kind == RogueliteRewardKind.Item) GrantItem(reward.Item.Id);
            claimedRewards.Add(rewardId); AwaitingReward = false;
        }

        public void AbandonCurrentReward()
        {
            if (!AwaitingReward) throw new InvalidOperationException("No reward is awaiting resolution.");
            if (PendingResourceReceipt != null)
                throw new InvalidOperationException("A settled resource receipt cannot be abandoned.");
            if (IsTutorialPhase)
            {
                FirstRunExperience.AbandonReward();
                claimedRewards.Add("abandoned:" + CurrentNodeId);
                SyncFirstRunProjection();
                return;
            }
            if (pendingFireSpellReselections.Count > 0)
                throw new InvalidOperationException("Migration replacement rewards cannot be abandoned.");
            claimedRewards.Add("abandoned:" + CurrentNodeId + ":" + completed.Count);
            AwaitingReward = false;
            rogueRunDto?.RolledRewardChoiceIds.Clear();
            rogueRunDto?.PendingRewardFollowupIds.Clear();
            rogueRunDto?.PendingFixedMaterialIds.Clear();
            if (rogueRunDto != null) rogueRunDto.PendingMainRewardId = string.Empty;
            if (rogueRunDto != null) rogueRunDto.PendingRewardGold = 0;
            if (rogueRunDto != null) rogueRunDto.PendingRewardContribution = 0;
            if (rogueRunDto != null) rogueRunDto.PendingRewardStepId = string.Empty;
            SettleAcademyRoundIfFinale();
        }

        private void SettleAcademyRoundIfFinale()
        {
            if (!IsInAcademyLayer || AwaitingReward) return;
            if (CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId) SettleAcademyRound();
        }

        private void GrantFirstRunEliteReward(string passiveId)
        {
            if (claimedRewards.Contains(passiveId)) throw new InvalidOperationException("Elite reward was already claimed.");
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            if (catalog.Spells.All(value => value.DefinitionId != passiveId)) throw new InvalidOperationException("Unknown elite passive reward.");
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
            int order = runtime.AllInstances.Count + runtime.AllTacticalItems.Count;
            string headId = "eq-first-elite-" + Seed + "-" + rogueRunDto.DeterministicCounter;
            OCC.Combat.Roguelite.RogueEquipmentInstance head = runtime.CreateInstance(headId, "ACA-EQ-HD02",
                OCC.Combat.Roguelite.EquipmentRarity.Rare, order, "first-run:elite");
            if (!runtime.AddToBackpack(head)) throw new InvalidOperationException("背包没有足够空间容纳低压回路护额。");
            string braceId = "item-first-elite-" + Seed + "-" + (rogueRunDto.DeterministicCounter + 1);
            OCC.Combat.Roguelite.RogueTacticalItemInstance brace = runtime.CreateTacticalItem(braceId, "G-T13", order + 1, "first-run:elite");
            if (!runtime.AddTacticalToBackpack(brace)) throw new InvalidOperationException("背包没有足够空间容纳定锚支架。");
            runtime.WriteToDto(rogueRunDto);
            rogueRunDto.DeterministicCounter += 2;
            if (!rogueRunDto.MasteredSpellIds.Contains(passiveId)) rogueRunDto.MasteredSpellIds.Add(passiveId);
            int empty = Array.FindIndex(rogueRunDto.EquippedSpellIds, string.IsNullOrEmpty);
            if (empty >= 0) rogueRunDto.EquippedSpellIds[empty] = passiveId;
            rogueRunDto.Gold += 6;
            claimedRewards.Add(passiveId);
            claimedRewards.Add("ACA-EQ-HD02");
            claimedRewards.Add("G-T13");
        }
        public void ClaimFireSpell(string spellId)
        {
            if (!AwaitingReward || CurrentFireSpellChoices.All(spell => spell.Id != spellId)) throw new InvalidOperationException("Fire spell reward is not available.");
            if (ownedFireSpells.Contains(spellId)) throw new InvalidOperationException("Personal spells cannot be acquired twice in one run.");
            ownedFireSpells.Add(spellId);
            if (pendingFireSpellReselections.Count > 0)
            {
                FireSpellSaveMigrationClaim claim = pendingFireSpellReselections[0];
                foreach (int slot in claim.OriginalEquippedSlots)
                    if (slot >= 0 && slot < equippedFireSpells.Length && string.IsNullOrEmpty(equippedFireSpells[slot])) { equippedFireSpells[slot] = spellId; rogueEquippedSpellIds[4 + slot] = spellId; }
                pendingFireSpellReselections.RemoveAt(0);
                AwaitingReward = pendingFireSpellReselections.Count > 0 || deferredNodeReward;
                if (pendingFireSpellReselections.Count == 0) deferredNodeReward = false;
                return;
            }
            AwaitingReward = false;
        }
        public void EquipFireSpell(string spellId, int slot)
        {
            if (slot < 0 || slot >= equippedFireSpells.Length) throw new ArgumentOutOfRangeException(nameof(slot));
            if (!ownedFireSpells.Contains(spellId)) throw new InvalidOperationException("Fire spell is not owned.");
            if (!FireSpellCatalog.IsWeaponCompatible(FireSpellCatalog.Get(spellId), EquippedWeapon)) throw new InvalidOperationException("Fire spell is incompatible with the equipped weapon.");
            equippedFireSpells[slot] = FireSpellCatalog.Get(spellId).Id;
            rogueEquippedSpellIds[4 + slot] = equippedFireSpells[slot];
        }

        public bool AssignRogueSpell(string spellId, int slot)
        {
            if (!UsesRogue11 || slot < 0 || slot >= OCC.Combat.Roguelite.RogueRuntimeConstants.SpellSlotCount) return false;
            string normalized = spellId ?? string.Empty;
            if (!string.IsNullOrEmpty(normalized) && !rogueRunDto.MasteredSpellIds.Contains(normalized)) return false;
            if (!string.IsNullOrEmpty(normalized) && rogueRunDto.EquippedSpellIds.Where((value, index) => index != slot).Contains(normalized)) return false;
            rogueRunDto.EquippedSpellIds[slot] = normalized;
            return true;
        }
        public void EquipReward(string rewardId)
        {
            RogueliteReward reward = claimedRewards.Contains(rewardId) ? RogueliteMapCatalog.Rewards.First(item => item.Id == rewardId) : throw new InvalidOperationException("Reward is not owned.");
            if (reward.Kind == RogueliteRewardKind.Weapon)
            {
                if (equippedFireSpells.Where(id => !string.IsNullOrEmpty(id)).Select(FireSpellCatalog.Get).Any(spell => !FireSpellCatalog.IsWeaponCompatible(spell, reward.Weapon)))
                    throw new InvalidOperationException("Equipped fire spell is incompatible with the requested weapon.");
                EquippedWeaponId = rewardId;
            }
            else if (reward.Kind == RogueliteRewardKind.Spell) EquippedSpellId = rewardId;
            else throw new InvalidOperationException("Inventory rewards are equipped from the backpack.");
        }
        public void CalibrateAether()
        {
            throw new InvalidOperationException("Legacy global armor calibration is retired; use deterministic equipment forging.");
        }

        public void AcknowledgeFirstRunOrigin()
        {
            RequireFirstRun();
            FirstRunExperience.AcknowledgeOrigin();
            SyncFirstRunProjection();
        }

        public void CompleteFirstRunForge(string targetId)
            => CompleteFirstRunForge(targetId, AcademyBattleRewardCatalog.ForgeLoad);

        public void CompleteFirstRunForge(string targetId, string materialId)
        {
            if (IsTutorialPhase)
            {
                if (materialId != AcademyBattleRewardCatalog.ForgeLoad)
                    throw new InvalidOperationException("The tutorial forge uses the supplied load alloy.");
                RequireFirstRun();
                FirstRunExperience.CompleteForge(targetId);
                OCC.Combat.Roguelite.EquipmentInstanceDto tutorialEquipment = rogueRunDto?.EquipmentInstances
                    .FirstOrDefault(value => value.InstanceId == targetId);
                if (tutorialEquipment != null) tutorialEquipment.ForgeMaterialId = materialId;
                SyncFirstRunProjection();
                return;
            }
            FirstRunWorkshopSnapshot workshop = CurrentWorkshopService;
            if (workshop.ForgeCompleted || AcademyMaterialCount(materialId) <= 0 ||
                materialId != AcademyBattleRewardCatalog.ForgeLoad && materialId != AcademyBattleRewardCatalog.ForgeCircuit)
                throw new InvalidOperationException("Academy-layer forge is unavailable.");
            string selectedTarget = RequireServiceTarget(targetId);
            OCC.Combat.Roguelite.EquipmentDefinition selectedEquipment =
                OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto).DefinitionFor(selectedTarget);
            if (selectedEquipment == null || selectedEquipment.Slot != OCC.Combat.Roguelite.EquipmentSlot.Weapon &&
                selectedEquipment.Slot != OCC.Combat.Roguelite.EquipmentSlot.Head &&
                selectedEquipment.Slot != OCC.Combat.Roguelite.EquipmentSlot.Chest &&
                selectedEquipment.Slot != OCC.Combat.Roguelite.EquipmentSlot.Feet &&
                selectedEquipment.Slot != OCC.Combat.Roguelite.EquipmentSlot.Backpack &&
                selectedEquipment.Slot != OCC.Combat.Roguelite.EquipmentSlot.CastingUnit)
                throw new InvalidOperationException("Equipment has no academy forge slot.");
            if (rogueRunDto.EquipmentInstances.Any(value => value.InstanceId == selectedTarget && !string.IsNullOrEmpty(value.ForgeMaterialId)) ||
                CompletedWorkshopBuilds.Any(value => value.ForgeCompleted && value.ForgedTargetId == selectedTarget))
                throw new InvalidOperationException("Equipment has already been forged this round.");
            SpendAcademyMaterial(materialId);
            rogueRunDto.EquipmentInstances.Single(value => value.InstanceId == selectedTarget).ForgeMaterialId = materialId;
            workshop.ForgeCompleted = true;
            workshop.ForgedTargetId = selectedTarget;
            workshop.ForgedMaterialId = materialId;
        }

        private void GrantAcademyChosenReward(RogueliteReward reward, bool dismantle)
        {
            if (dismantle)
            {
                GrantAcademyMaterial(reward.Kind == RogueliteRewardKind.Equipment
                    ? AcademyBattleRewardCatalog.ForgeLoad : AcademyBattleRewardCatalog.SpecAmplify, 1);
            }
            else if (reward.RogueSpell != null)
            {
                if (!rogueRunDto.MasteredSpellIds.Contains(reward.Id)) rogueRunDto.MasteredSpellIds.Add(reward.Id);
                int empty = Array.FindIndex(rogueRunDto.EquippedSpellIds, string.IsNullOrEmpty);
                if (empty >= 0) rogueRunDto.EquippedSpellIds[empty] = reward.Id;
            }
            else if (reward.Equipment != null)
            {
                OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
                string instanceId = "eq-" + Seed + "-" + rogueRunDto.DeterministicCounter;
                OCC.Combat.Roguelite.RogueEquipmentInstance instance = runtime.CreateInstance(instanceId, reward.Id,
                    reward.Equipment.AllowedRarities[0], runtime.AllInstances.Count + runtime.AllTacticalItems.Count, reward.BuildPath);
                if (!runtime.AddToBackpack(instance)) throw new InvalidOperationException("Backpack cannot accept equipment reward: " + reward.Id);
                rogueRunDto.DeterministicCounter++;
                runtime.WriteToDto(rogueRunDto);
            }
            else if (reward.TacticalItem != null)
                GrantRogue11Content(reward.Id, "combat:" + CurrentNodeId);
            else if (reward.Kind == RogueliteRewardKind.Resource)
                GrantAcademyRewardResource(reward.ResourceId);
        }

        public bool CanDismantleReward(RogueliteReward reward) => IsInAcademyLayer && AwaitingReward &&
            reward != null && rogueRunDto != null && rogueRunDto.RolledRewardChoiceIds.Contains(reward.Id) &&
            (reward.Kind == RogueliteRewardKind.Equipment ||
                reward.RogueSpell != null && rogueRunDto.MasteredSpellIds.Contains(reward.Id));

        public void CompleteFirstRunSpecialization(string targetId)
            => CompleteFirstRunSpecialization(targetId, AcademyBattleRewardCatalog.SpecAmplify);

        public void CompleteFirstRunSpecialization(string targetId, string materialId)
        {
            if (IsTutorialPhase)
            {
                if (materialId != AcademyBattleRewardCatalog.SpecAmplify)
                    throw new InvalidOperationException("The tutorial specialization uses the supplied amplifying ink.");
                RequireFirstRun();
                FirstRunExperience.CompleteSpecialization(targetId);
                SyncFirstRunProjection();
                return;
            }
            FirstRunWorkshopSnapshot workshop = CurrentWorkshopService;
            if (workshop.SpecializationCompleted || AcademyMaterialCount(materialId) <= 0 ||
                materialId != AcademyBattleRewardCatalog.SpecAmplify && materialId != AcademyBattleRewardCatalog.SpecEfficient)
                throw new InvalidOperationException("Academy-layer specialization is unavailable.");
            string selectedTarget = RequireServiceTarget(targetId);
            if (!OCC.Combat.Roguelite.RogueSpellCombatRuntime.SupportsSpecialization(selectedTarget, materialId) ||
                CompletedWorkshopBuilds.Any(value => value.SpecializationCompleted && value.SpecializedTargetId == selectedTarget))
                throw new InvalidOperationException("Spell cannot receive this specialization.");
            SpendAcademyMaterial(materialId);
            workshop.SpecializationCompleted = true;
            workshop.SpecializedTargetId = selectedTarget;
            workshop.SpecializedMaterialId = materialId;
        }

        public void CompleteFirstRunHealthCheck()
        {
            if (IsTutorialPhase)
            {
                RequireFirstRun();
                FirstRunExperience.CompleteHealthCheck();
                SyncFirstRunProjection();
                return;
            }
            CurrentMedicalService.HealthCheckCompleted = true;
        }

        public void UseFirstRunHeal()
        {
            if (!IsFirstRunExperience && !IsInAcademyLayer) throw new InvalidOperationException("Academy treatment is unavailable.");
            FirstRunMedicalSnapshot layerMedical = null;
            if (!IsTutorialPhase)
            {
                layerMedical = CurrentMedicalService;
                if (!layerMedical.HealthCheckCompleted || layerMedical.HealUsed)
                    throw new InvalidOperationException("Academy-layer treatment is unavailable.");
            }
            if (rogueRunDto.StageContribution < 1) throw new InvalidOperationException("Insufficient stage contribution.");
            rogueRunDto.StageContribution--;
            rogueRunDto.CurrentHealth = Math.Min(UnitState.HeroBaseHealth, rogueRunDto.CurrentHealth + UnitState.HeroBaseHealth / 2);
            CurrentHealth = rogueRunDto.CurrentHealth;
            if (IsTutorialPhase)
            {
                FirstRunExperience.MarkHealUsed();
                SyncFirstRunProjection();
            }
            else
            {
                layerMedical.HealUsed = true;
            }
        }

        public void ChooseFirstRunMeal(string mealId)
        {
            if (!IsFirstRunExperience && !IsInAcademyLayer) throw new InvalidOperationException("Academy meal is unavailable.");
            FirstRunMedicalSnapshot layerMedical = null;
            if (!IsTutorialPhase)
            {
                layerMedical = CurrentMedicalService;
                if (!layerMedical.HealthCheckCompleted || layerMedical.MealUsed || !layerMedical.MealCandidateIds.Contains(mealId))
                    throw new InvalidOperationException("Academy-layer meal is unavailable.");
            }
            if (mealId == "MEAL-POWER")
            {
                if (rogueRunDto.Gold < 3) throw new InvalidOperationException("Insufficient gold.");
                rogueRunDto.Gold -= 3;
            }
            else if (mealId == "MEAL-AETHER")
            {
                if (rogueRunDto.StageContribution < 1) throw new InvalidOperationException("Insufficient stage contribution.");
                rogueRunDto.StageContribution--;
            }
            else if (mealId == "MEAL-GUARD")
            {
                if (AcademyFoodCount < 1) throw new InvalidOperationException("Insufficient academy food.");
                if (FirstRunExperience != null) FirstRunExperience.AcademyFoodCount--;
                else rogueRunDto.AcademyFoodCount--;
            }
            if (IsTutorialPhase)
            {
                FirstRunExperience.ChooseMeal(mealId);
                SyncFirstRunProjection();
            }
            else
            {
                layerMedical.MealUsed = true;
                layerMedical.SelectedMealId = mealId;
            }
        }

        public void PurchaseFirstRunOffer(string offerId)
        {
            if (!IsFirstRunExperience && !IsInAcademyLayer) throw new InvalidOperationException("Academy shop is unavailable.");
            FirstRunShopSnapshot shop = CurrentShopService;
            FirstRunShopOfferSnapshot offer = shop.Offers.SingleOrDefault(value => value.OfferId == offerId);
            if (offer == null || offer.Sold) throw new InvalidOperationException("Shop offer is unavailable.");
            if (offer.CurrencyId != "gold" || rogueRunDto.Gold < offer.Price) throw new InvalidOperationException("Insufficient gold.");
            if (!CanAcceptRogue11Content(offer.DefinitionId)) throw new InvalidOperationException("Backpack cannot accept shop item.");
            rogueRunDto.Gold -= offer.Price;
            GrantRogue11Content(offer.DefinitionId, "first-run-shop");
            if (IsTutorialPhase)
            {
                FirstRunExperience.Purchase(offerId);
                SyncFirstRunProjection();
            }
            else offer.Sold = true;
        }

        public bool CanAcceptFirstRunOffer(string offerId)
        {
            if (!IsFirstRunExperience && !IsInAcademyLayer) return false;
            FirstRunShopOfferSnapshot offer = CurrentShopService.Offers.SingleOrDefault(value => value.OfferId == offerId);
            return offer != null && !offer.Sold && CanAcceptRogue11Content(offer.DefinitionId);
        }

        public void SettleCurrentServiceNode()
        {
            if (!IsInAcademyLayer) throw new InvalidOperationException("Only academy-layer services use per-node settlement.");
            RogueliteMapNode node = MapNode(CurrentNodeId);
            if (!IsServiceNodeType(node.Type)) throw new InvalidOperationException("Current node is not a service node.");
            if (IsServiceNodeSettled(node.Id)) return;
            SettleServiceNode(node.Id);
            Complete(node, false);
            ClearLayerServiceSession();
        }

        private RogueliteMapRun EnsureLayerServiceSession(RogueliteMapNodeType expectedType)
        {
            if (!IsInAcademyLayer) throw new InvalidOperationException("Academy-layer service state is unavailable.");
            RogueliteMapNode node = MapNode(CurrentNodeId);
            if (node.Type != expectedType) throw new InvalidOperationException("Current service node type does not match the requested page.");
            if (!string.Equals(activeLayerServiceNodeId, node.Id, StringComparison.Ordinal)) BeginLayerServiceSession(node.Id, node.Type);
            return this;
        }

        private void BeginLayerServiceSession(string nodeId, RogueliteMapNodeType type)
        {
            ClearLayerServiceSession();
            activeLayerServiceNodeId = nodeId;
            if (type == RogueliteMapNodeType.Workshop) layerWorkshopService = new FirstRunWorkshopSnapshot();
            else if (type == RogueliteMapNodeType.Medical)
            {
                layerMedicalService = new FirstRunMedicalSnapshot();
                layerMedicalService.MealCandidateIds.AddRange(new[] { "MEAL-POWER", "MEAL-AETHER", "MEAL-GUARD" });
            }
            else if (type == RogueliteMapNodeType.Shop)
            {
                layerShopService = new FirstRunShopSnapshot { Opened = true };
                layerShopService.Offers.Add(new FirstRunShopOfferSnapshot("LAYER-SHOP-WEDGE", "G-T08", 3, "gold"));
                layerShopService.Offers.Add(new FirstRunShopOfferSnapshot("LAYER-SHOP-SPINDLE", "G-T02", 6, "gold"));
                layerShopService.Offers.Add(new FirstRunShopOfferSnapshot("LAYER-SHOP-GOGGLES", "ACA-EQ-HD01", 4, "gold"));
            }
            string row = rogueRunDto?.LayerServiceRows.FirstOrDefault(value => value.StartsWith(nodeId + "~", StringComparison.Ordinal));
            if (row == null) return;
            string[] fields = row.Split('~');
            if (type == RogueliteMapNodeType.Workshop)
            {
                layerWorkshopService.ForgeCompleted = fields[2] == "1";
                layerWorkshopService.SpecializationCompleted = fields[3] == "1";
                layerWorkshopService.ForgedTargetId = fields[4];
                layerWorkshopService.SpecializedTargetId = fields[5];
                layerWorkshopService.ForgedMaterialId = fields.Length > 6 ? fields[6] : AcademyBattleRewardCatalog.ForgeLoad;
                layerWorkshopService.SpecializedMaterialId = fields.Length > 7 ? fields[7] : AcademyBattleRewardCatalog.SpecAmplify;
            }
            else if (type == RogueliteMapNodeType.Medical)
            {
                layerMedicalService.HealthCheckCompleted = fields[2] == "1";
                layerMedicalService.HealUsed = fields[3] == "1";
                layerMedicalService.MealUsed = fields[4] == "1";
                layerMedicalService.SelectedMealId = fields[5];
            }
            else if (type == RogueliteMapNodeType.Shop)
                foreach (FirstRunShopOfferSnapshot offer in layerShopService.Offers)
                    offer.Sold = fields.Skip(2).Contains(offer.OfferId);
        }

        private void ClearLayerServiceSession()
        {
            CaptureLayerServiceSession();
            activeLayerServiceNodeId = string.Empty;
            layerWorkshopService = null;
            layerMedicalService = null;
            layerShopService = null;
        }

        private void CaptureLayerServiceSession()
        {
            if (rogueRunDto == null || string.IsNullOrEmpty(activeLayerServiceNodeId)) return;
            string row = layerWorkshopService != null
                ? string.Join("~", activeLayerServiceNodeId, "W", layerWorkshopService.ForgeCompleted ? "1" : "0",
                    layerWorkshopService.SpecializationCompleted ? "1" : "0", layerWorkshopService.ForgedTargetId,
                    layerWorkshopService.SpecializedTargetId, layerWorkshopService.ForgedMaterialId,
                    layerWorkshopService.SpecializedMaterialId)
                : layerMedicalService != null
                    ? string.Join("~", activeLayerServiceNodeId, "M", layerMedicalService.HealthCheckCompleted ? "1" : "0",
                        layerMedicalService.HealUsed ? "1" : "0", layerMedicalService.MealUsed ? "1" : "0", layerMedicalService.SelectedMealId)
                    : layerShopService != null
                        ? string.Join("~", new[] { activeLayerServiceNodeId, "S" }.Concat(layerShopService.Offers.Where(value => value.Sold).Select(value => value.OfferId)))
                        : string.Empty;
            if (row.Length == 0) return;
            rogueRunDto.LayerServiceRows.RemoveAll(value => value.StartsWith(activeLayerServiceNodeId + "~", StringComparison.Ordinal));
            rogueRunDto.LayerServiceRows.Add(row);
        }

        public static void ValidateLayerServiceRows(IEnumerable<string> rows)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string row in rows ?? Array.Empty<string>())
            {
                string[] fields = (row ?? string.Empty).Split('~');
                if (fields.Length < 2 || !seen.Add(fields[0])) throw new InvalidOperationException("Invalid academy service snapshot.");
                if (fields[0] == "layer_workshop" && fields[1] == "W" && (fields.Length == 6 || fields.Length == 8) ||
                    fields[0] == "layer_medical" && fields[1] == "M" && fields.Length == 6)
                {
                    int flagCount = fields[1] == "W" ? 2 : 3;
                    for (int i = 2; i < 2 + flagCount; i++)
                        if (fields[i] != "0" && fields[i] != "1") throw new InvalidOperationException("Invalid academy service flag.");
                    if (fields[1] == "W" && fields.Length == 8 &&
                        (fields[6] != AcademyBattleRewardCatalog.ForgeLoad && fields[6] != AcademyBattleRewardCatalog.ForgeCircuit ||
                         fields[7] != AcademyBattleRewardCatalog.SpecAmplify && fields[7] != AcademyBattleRewardCatalog.SpecEfficient))
                        throw new InvalidOperationException("Invalid academy workshop material selection.");
                    continue;
                }
                if (fields[0] == "layer_shop" && fields[1] == "S" && fields.Skip(2).Distinct(StringComparer.Ordinal).Count() == fields.Length - 2 &&
                    fields.Skip(2).All(value => value == "LAYER-SHOP-WEDGE" || value == "LAYER-SHOP-SPINDLE" || value == "LAYER-SHOP-GOGGLES")) continue;
                throw new InvalidOperationException("Invalid academy service snapshot.");
            }
        }

        public static void ValidateAcademyMaterialRows(IEnumerable<string> rows)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string row in rows ?? Array.Empty<string>())
            {
                string[] fields = (row ?? string.Empty).Split('=');
                if (fields.Length != 2 || !IsAcademyMaterialId(fields[0]) || !seen.Add(fields[0]) ||
                    !int.TryParse(fields[1], out int count) || count <= 0)
                    throw new InvalidOperationException("Invalid academy material stock.");
            }
        }

        private static bool IsAcademyMaterialId(string id) => id == AcademyBattleRewardCatalog.ForgeLoad ||
            id == AcademyBattleRewardCatalog.ForgeCircuit || id == AcademyBattleRewardCatalog.SpecAmplify ||
            id == AcademyBattleRewardCatalog.SpecEfficient;

        private void GrantAcademyMaterial(string materialId, int amount)
        {
            if (rogueRunDto == null || !IsAcademyMaterialId(materialId) || amount <= 0)
                throw new InvalidOperationException("Invalid academy material grant.");
            if (!OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto).CanFitMaterials(amount))
                throw new InvalidOperationException("Backpack cannot accept academy workshop material.");
            string existing = rogueRunDto.MaterialStockRows.FirstOrDefault(value => value.StartsWith(materialId + "=", StringComparison.Ordinal));
            int next = checked((existing == null ? 0 : int.Parse(existing.Substring(materialId.Length + 1))) + amount);
            rogueRunDto.MaterialStockRows.RemoveAll(value => value.StartsWith(materialId + "=", StringComparison.Ordinal));
            rogueRunDto.MaterialStockRows.Add(materialId + "=" + next);
            if (materialId == AcademyBattleRewardCatalog.ForgeLoad || materialId == AcademyBattleRewardCatalog.ForgeCircuit)
                rogueRunDto.ForgeMaterialCount = checked(rogueRunDto.ForgeMaterialCount + amount);
            else rogueRunDto.SpecializationMaterialCount = checked(rogueRunDto.SpecializationMaterialCount + amount);
        }

        private void SpendAcademyMaterial(string materialId)
        {
            bool forge = materialId == AcademyBattleRewardCatalog.ForgeLoad || materialId == AcademyBattleRewardCatalog.ForgeCircuit;
            if (!IsAcademyMaterialId(materialId)) throw new InvalidOperationException("Unknown academy workshop material.");
            if (materialId == AcademyBattleRewardCatalog.ForgeLoad && (FirstRunExperience?.ForgeMaterialCount ?? 0) > 0)
            { FirstRunExperience.ForgeMaterialCount--; return; }
            if (materialId == AcademyBattleRewardCatalog.SpecAmplify && (FirstRunExperience?.SpecializationMaterialCount ?? 0) > 0)
            { FirstRunExperience.SpecializationMaterialCount--; return; }
            string row = rogueRunDto.MaterialStockRows.FirstOrDefault(value => value.StartsWith(materialId + "=", StringComparison.Ordinal));
            if (row != null)
            {
                int count = int.Parse(row.Substring(materialId.Length + 1));
                if (count > 0)
                {
                    rogueRunDto.MaterialStockRows.Remove(row);
                    if (count > 1) rogueRunDto.MaterialStockRows.Add(materialId + "=" + (count - 1));
                    if (forge) rogueRunDto.ForgeMaterialCount--;
                    else rogueRunDto.SpecializationMaterialCount--;
                    return;
                }
            }
            if (materialId == AcademyBattleRewardCatalog.ForgeLoad && rogueRunDto.ForgeMaterialCount > 0)
            { rogueRunDto.ForgeMaterialCount--; return; }
            if (materialId == AcademyBattleRewardCatalog.SpecAmplify && rogueRunDto.SpecializationMaterialCount > 0)
            { rogueRunDto.SpecializationMaterialCount--; return; }
            throw new InvalidOperationException("Academy workshop material stock is empty.");
        }

        public bool DiscardRogueBackpackItem(string instanceId)
        {
            if (!UsesRogue11 || HasActiveCombat || string.IsNullOrEmpty(instanceId)) return false;
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
            string materialId = runtime.MaterialIdFor(instanceId);
            if (!string.IsNullOrEmpty(materialId))
            {
                SpendAcademyMaterial(materialId);
                return true;
            }
            if (!runtime.DiscardBackpackItem(instanceId)) return false;
            runtime.WriteToDto(rogueRunDto);
            return true;
        }

        private void GrantAcademyRewardResource(string resourceId)
        {
            if (!CanAcceptAcademyResource(resourceId))
                throw new InvalidOperationException("Backpack cannot accept academy reward resource.");
            switch (resourceId)
            {
                case AcademyBattleRewardCatalog.ForgeLoad:
                case AcademyBattleRewardCatalog.ForgeCircuit:
                case AcademyBattleRewardCatalog.SpecAmplify:
                case AcademyBattleRewardCatalog.SpecEfficient:
                    GrantAcademyMaterial(resourceId, 1); break;
                case AcademyBattleRewardCatalog.ForgePair:
                    GrantAcademyMaterial(AcademyBattleRewardCatalog.ForgeLoad, 1);
                    GrantAcademyMaterial(AcademyBattleRewardCatalog.ForgeCircuit, 1); break;
                case AcademyBattleRewardCatalog.SpecPair:
                    GrantAcademyMaterial(AcademyBattleRewardCatalog.SpecAmplify, 1);
                    GrantAcademyMaterial(AcademyBattleRewardCatalog.SpecEfficient, 1); break;
                case AcademyBattleRewardCatalog.MixedPair:
                    GrantAcademyMaterial(AcademyBattleRewardCatalog.ForgeLoad, 1);
                    GrantAcademyMaterial(AcademyBattleRewardCatalog.SpecAmplify, 1); break;
                case AcademyBattleRewardCatalog.ScrollPack:
                    GrantRogue11Content(ArtifactCatalog.FirelineScroll.Id, "combat-scroll:" + CurrentNodeId, true); break;
                default: throw new InvalidOperationException("Unknown academy resource reward: " + resourceId);
            }
        }

        private static bool IsServiceNodeType(RogueliteMapNodeType type) =>
            type == RogueliteMapNodeType.Workshop || type == RogueliteMapNodeType.Medical || type == RogueliteMapNodeType.Shop;

        private static string RequireServiceTarget(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) throw new InvalidOperationException("Service target is required.");
            return targetId;
        }

        public void CompleteFirstRunExperience()
        {
            RequireFirstRun();
            FirstRunExperience.CompleteExperience();
            EnterAcademyLayer();
        }

        /// <summary>
        /// 固定段 → 随机层的交接：同一局继续，把教程段成果原样带进学院层，
        /// 再把节点集、遭遇分配与事件分配切换到学院层 20 节点子图。
        /// </summary>
        private void EnterAcademyLayer()
        {
            if (IsInAcademyLayer) { SyncAcademyLayerProjection(); return; }
            CarryOverTutorialProgress();
            generatedAcademyMapNodes = RogueliteAcademyMapGenerator.Generate(Seed);
            var selectedIds = new HashSet<string>(generatedAcademyMapNodes.Select(node => node.Id), StringComparer.Ordinal);
            ReplaceEncounterAssignments(RogueliteAcademyLayerCatalog.GenerateEncounterAssignments(Seed)
                .Where(value => selectedIds.Contains(value.NodeId)));
            ReplaceNodeContentAssignments(RogueliteAcademyLayerCatalog.GenerateNodeContentAssignments()
                .Where(value => selectedIds.Contains(value.NodeId)));
            IsInAcademyLayer = true;
            completed.Clear();
            completed.Add(FirstRunExperienceCatalog.ShopNodeId);
            CurrentNodeId = "academy_gate";
            routeHistory.Add(CurrentNodeId);
            visited.Clear();
            visited.Add(FirstRunExperienceCatalog.ShopNodeId);
            visited.Add(CurrentNodeId);
            visited.Add("dorm_drill");
            AwaitingReward = false;
            PendingContentChoiceId = null;
            PendingContentCombatMissionId = null;
            deferredNodeReward = false;
            SyncAcademyLayerProjection();
        }

        /// <summary>
        /// 交接搬运：术式/精英被动、装备与背包、资源（金币/贡献/食材/补给/以太）、
        /// 战斗快照（生命/护盾/魔力）、日志与已领取奖励都已经在 rogueRunDto 与 claimedRewards 里，
        /// 这里只补齐教程段结束后需要收敛的部分。
        /// </summary>
        private void CarryOverTutorialProgress()
        {
            if (rogueRunDto == null) return;
            if (!string.IsNullOrEmpty(FirstRunExperience.EliteSelectedPassiveId) &&
                !rogueRunDto.MasteredSpellIds.Contains(FirstRunExperience.EliteSelectedPassiveId))
                rogueRunDto.MasteredSpellIds.Add(FirstRunExperience.EliteSelectedPassiveId);
            // 教程段获得的战场术式必须留在掌握列表里，才能带进随机层战斗。
            foreach (string spellId in ownedFireSpells)
                if (FireSpellCatalog.All.Any(spell => spell.Id == spellId) && !rogueRunDto.MasteredSpellIds.Contains(spellId))
                    rogueRunDto.MasteredSpellIds.Add(spellId);
            rogueRunDto.CurrentHealth = Math.Max(1, Math.Min(UnitState.HeroBaseHealth, rogueRunDto.CurrentHealth));
            rogueRunDto.CurrentMana = Math.Max(0, Math.Min(RogueManaCapacity, rogueRunDto.CurrentMana));
            CurrentHealth = rogueRunDto.CurrentHealth;
            CurrentMana = rogueRunDto.CurrentMana;
            CurrentShield = 0;
            HasCombatSnapshot = true;
            foreach (KeyValuePair<string, string> row in FirstRunExperience.LootProgress)
                if (!lootProgress.ContainsKey(row.Key)) lootProgress[row.Key] = row.Value;
        }

        private void SyncAcademyLayerProjection()
        {
            RegionBossId = "core_overseer";
            if (rogueRunDto == null) return;
            Replace(rogueRunDto.GeneratedAcademyMapRows, RogueliteAcademyMapGenerator.Encode(generatedAcademyMapNodes));
            rogueRunDto.CurrentNodeId = CurrentNodeId;
            rogueRunDto.RegionBossId = RegionBossId;
            rogueRunDto.AwaitingReward = AwaitingReward;
            rogueRunDto.RunProgramId = (FirstRunExperience == null ? RogueliteRunProgram.EvergreenAcademy : RogueliteRunProgram.FirstRunV1).ToString();
            rogueRunDto.FirstRunExperience = FirstRunExperience;
        }

        public void SealFirstRunEliteDefeat()
        {
            RequireFirstRun();
            FirstRunExperience.SealEliteDefeat();
            CurrentHealth = 0;
            rogueRunDto.CurrentHealth = 0;
            SyncFirstRunProjection();
        }

        private void RequireFirstRun()
        {
            if (!IsFirstRunExperience) throw new InvalidOperationException("The active run is not the fixed first-run experience.");
        }

        private void SyncFirstRunProjection()
        {
            if (!IsTutorialPhase) return;
            CurrentNodeId = FirstRunExperience.CurrentNodeId;
            completed.Clear();
            completed.UnionWith(FirstRunExperience.Nodes.Where(value => (value.Flags & FirstRunNodeFlags.Completed) != 0).Select(value => value.Id));
            AwaitingReward = !string.IsNullOrEmpty(FirstRunExperience.PendingRewardGroupId) ||
                FirstRunExperience.Outcome == FirstRunOutcome.EliteVictory && !FirstRunExperience.EliteRewardClaimed && !FirstRunExperience.EliteRewardAbandoned;
            if (rogueRunDto != null)
            {
                rogueRunDto.CurrentNodeId = CurrentNodeId;
                rogueRunDto.AwaitingReward = AwaitingReward;
                rogueRunDto.RunProgramId = RogueliteRunProgram.FirstRunV1.ToString();
                rogueRunDto.FirstRunExperience = FirstRunExperience;
            }
        }
        internal OCC.Combat.Roguelite.RogueRunDto ExportRogue11(OCC.Combat.Roguelite.RogueRunDto preserved = null, string migrationReportId = "")
        {
            OCC.Combat.Roguelite.RogueRunDto dto = preserved ?? rogueRunDto ?? OCC.Combat.Roguelite.RogueRunDto.CreateNew("run-" + Seed, Seed);
            rogueRunDto = dto;
            CaptureLayerServiceSession();
            dto.CurrentNodeId = CurrentNodeId; dto.RegionBossId = IsInAcademyLayer ? "core_overseer" : IsFirstRunExperience ? string.Empty : "core_overseer"; dto.StarterId = StarterId;
            dto.CurrentHealth = IsFirstRunExperience && FirstRunExperience.RunSealed || RunEndReason == "death"
                ? 0 : Math.Max(1, Math.Min(UnitState.HeroBaseHealth, CurrentHealth)); dto.CurrentMana = Math.Max(0, Math.Min(RogueManaCapacity, CurrentMana));
            dto.AwaitingReward = AwaitingReward; dto.PendingContentChoiceId = PendingContentChoiceId ?? string.Empty;
            dto.PendingContentCombatMissionId = PendingContentCombatMissionId ?? string.Empty;
            Replace(dto.VisitedNodeIds, visited.OrderBy(id => id, StringComparer.Ordinal));
            Replace(dto.RouteHistoryNodeIds, routeHistory);
            Replace(dto.CompletedNodeIds, completed.OrderBy(id => id, StringComparer.Ordinal));
            Replace(dto.ClaimedContentIds, claimedRewards);
            bool keepsAssignments = IsInAcademyLayer || !IsFirstRunExperience;
            Replace(dto.EncounterAssignments, keepsAssignments ? encounterAssignments.OrderBy(value => value.Key, StringComparer.Ordinal).Select(value => value.Key + "=" + value.Value) : Array.Empty<string>());
            Replace(dto.NodeContentAssignments, keepsAssignments ? nodeContentAssignments.OrderBy(value => value.Key, StringComparer.Ordinal).Select(value => value.Key + "=" + value.Value) : Array.Empty<string>());
            Replace(dto.GeneratedAcademyMapRows, RogueliteAcademyMapGenerator.Encode(generatedAcademyMapNodes));
            foreach (string id in ownedFireSpells.Where(id => FireSpellCatalog.All.Any(spell => spell.Id == id)))
                if (!dto.MasteredSpellIds.Contains(id)) dto.MasteredSpellIds.Add(id);
            for (int index = 0; index < equippedFireSpells.Length; index++)
                dto.EquippedSpellIds[4 + index] = dto.MasteredSpellIds.Contains(equippedFireSpells[index]) ? equippedFireSpells[index] : string.Empty;
            if (!string.IsNullOrEmpty(migrationReportId)) dto.MigrationReportId = migrationReportId;
            if (IsFirstRunExperience)
            {
                dto.RunProgramId = RogueliteRunProgram.FirstRunV1.ToString();
                dto.FirstRunExperience = FirstRunExperience;
            }
            return dto;
        }
        public static RogueliteMapRun FromRogue11(OCC.Combat.Roguelite.RogueRunDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            dto.MasteredSpellIds.RemoveAll(id => id == FirstRunExperienceCatalog.OriginSpellId);
            for (int index = 0; index < dto.EquippedSpellIds.Length; index++)
                if (dto.EquippedSpellIds[index] == FirstRunExperienceCatalog.OriginSpellId)
                    dto.EquippedSpellIds[index] = string.Empty;
            if (dto.FirstRunExperience?.Origin.SpellId == FirstRunExperienceCatalog.OriginSpellId)
                dto.FirstRunExperience.Origin.SpellId = "BASE-AETHER-SHIELD";
            RogueliteMapRun run = new RogueliteMapRun(dto.Seed)
            {
                CurrentNodeId = AcademyMapSaveMigration.NodeId(dto.CurrentNodeId), RegionBossId = "core_overseer", StarterId = dto.StarterId,
                EquippedWeaponId = StarterWeaponId(dto.StarterId),
                CurrentHealth = dto.CurrentHealth, CurrentShield = 0, CurrentMana = dto.CurrentMana,
                AwaitingReward = dto.AwaitingReward, PendingContentChoiceId = AcademyMapSaveMigration.ChoiceId(dto.PendingContentChoiceId),
                PendingContentCombatMissionId = dto.PendingContentCombatMissionId, HasCombatSnapshot = true
            };
            run.rogueRunDto = dto;
            if (dto.RunProgramId == RogueliteRunProgram.EvergreenAcademy.ToString() &&
                dto.EquipmentInstances.Count == 0 && dto.TacticalItemInstances.Count == 0)
                OCC.Combat.Roguelite.RogueEquipmentRuntime.CreateStarter(dto.Seed).WriteToDto(dto);
            run.generatedAcademyMapNodes = RogueliteAcademyMapGenerator.Decode(dto.GeneratedAcademyMapRows);
            run.routeHistory.Clear();
            if (dto.RouteHistoryNodeIds.Count > 0)
                run.routeHistory.AddRange(dto.RouteHistoryNodeIds.Select(AcademyMapSaveMigration.NodeId));
            else run.routeHistory.Add(run.CurrentNodeId);
            if (dto.FirstRunExperience != null)
            {
                run.FirstRunExperience = dto.FirstRunExperience;
                // 节点可达标记是规则的派生结果。读取旧存档时必须按当前规则重算，
                // 否则旧版写下的 Locked 会继续挡住已经满足新门槛的节点。
                run.FirstRunExperience.RecomputeNodeFlags();
                // RandomLayer 是本局已经交接到随机层的标记；Complete 是旧存档里的同一含义。
                run.IsInAcademyLayer = dto.FirstRunExperience.IsRandomLayer;
                if (run.IsInAcademyLayer)
                {
                    run.CurrentNodeId = AcademyMapSaveMigration.NodeId(dto.CurrentNodeId);
                    if (!run.AcademyLayerNodes.Any(node => node.Id == run.CurrentNodeId))
                        run.CurrentNodeId = run.AcademyLayerNodes[0].Id;
                    run.RegionBossId = "core_overseer";
                    run.visited.Clear();
                    foreach (string id in dto.VisitedNodeIds.Where(id => run.AcademyLayerNodes.Any(node => node.Id == id))) run.visited.Add(id);
                    if (run.visited.Count == 0)
                        foreach (string entry in RogueliteAcademyLayerCatalog.EntryNodeIds) run.visited.Add(entry);
                    run.AwaitingReward = dto.AwaitingReward;
                }
                else
                {
                    run.CurrentNodeId = dto.FirstRunExperience.CurrentNodeId;
                    run.RegionBossId = string.Empty;
                    run.encounterAssignments.Clear();
                    run.nodeContentAssignments.Clear();
                    run.visited.Clear();
                    foreach (string id in dto.VisitedNodeIds.Where(id => FirstRunExperienceCatalog.MapNodes.Any(node => node.Id == id))) run.visited.Add(id);
                    if (run.visited.Count == 0) run.visited.Add(FirstRunExperienceCatalog.OriginNodeId);
                    run.SyncFirstRunProjection();
                }
            }
            else if (dto.RunProgramId == RogueliteRunProgram.EvergreenAcademy.ToString() &&
                run.generatedAcademyMapNodes.Count > 0)
            {
                run.IsInAcademyLayer = true;
                run.visited.Clear();
                foreach (string id in dto.VisitedNodeIds.Where(id => run.generatedAcademyMapNodes.Any(node => node.Id == id)))
                    run.visited.Add(id);
                if (run.visited.Count == 0) run.visited.Add("academy_gate");
            }
            else
            {
                run.visited.Clear(); Restore(run.visited, string.Join(",", dto.VisitedNodeIds), true);
                run.completed.Clear(); Restore(run.completed, string.Join(",", dto.CompletedNodeIds), false);
            }
            run.claimedRewards.Clear(); run.claimedRewards.AddRange(dto.ClaimedContentIds.Where(id => !AcademyMapSaveMigration.IsRetiredProgressMarker(id)));
            if (run.IsFirstRunExperience && run.FirstRunExperience.RequiresRuntimeBackfill)
            {
                foreach (FirstRunRewardGroupSnapshot group in run.FirstRunExperience.RewardGroups.Where(value => !string.IsNullOrEmpty(value.SelectedId)))
                    if (!run.claimedRewards.Contains(group.SelectedId)) run.GrantRogue11Content(group.SelectedId, "first-run:migrated-reward");
                FirstRunEventSnapshot eventThree = run.FirstRunExperience.EventForNode("EV3");
                if (eventThree?.SelectedOptionId == "FIRST-EV3-REACTION-BELL" && !run.claimedRewards.Contains("G-T10"))
                    run.GrantRogue11Content("G-T10", "first-run:migrated-EV3");
                if (eventThree?.SelectedOptionId == "FIRST-EV3-CONTRIBUTION") dto.StageContribution++;
                run.FirstRunExperience.RequiresRuntimeBackfill = false;
            }
            if (dto.EncounterAssignments.Count > 0)
            {
                List<RogueliteEncounterAssignment> restoredAssignments = new List<RogueliteEncounterAssignment>();
                foreach (string row in dto.EncounterAssignments)
                {
                    int separator = row.IndexOf('=');
                    if (separator <= 0 || separator == row.Length - 1) throw new InvalidOperationException("Invalid encounter assignment row.");
                    restoredAssignments.Add(new RogueliteEncounterAssignment(AcademyMapSaveMigration.NodeId(row.Substring(0, separator)), row.Substring(separator + 1)));
                }
                run.ReplaceEncounterAssignments(restoredAssignments);
            }
            if (dto.NodeContentAssignments.Count > 0)
            {
                List<AcademyEventAssignment> restoredContent = new List<AcademyEventAssignment>();
                foreach (string row in dto.NodeContentAssignments)
                {
                    int separator = row.IndexOf('=');
                    if (separator <= 0 || separator == row.Length - 1) throw new InvalidOperationException("Invalid node content assignment row.");
                    restoredContent.Add(new AcademyEventAssignment(AcademyMapSaveMigration.NodeId(row.Substring(0, separator)), row.Substring(separator + 1)));
                }
                run.ReplaceNodeContentAssignments(restoredContent);
            }
            run.ownedFireSpells.Clear();
            run.ownedFireSpells.AddRange(dto.MasteredSpellIds.Where(id => FireSpellCatalog.All.Any(spell => spell.Id == id)).Distinct(StringComparer.Ordinal));
            for (int index = 0; index < run.equippedFireSpells.Length; index++)
            {
                string id = dto.EquippedSpellIds[4 + index]; run.equippedFireSpells[index] = run.ownedFireSpells.Contains(id) ? id : string.Empty;
                run.rogueEquippedSpellIds[4 + index] = run.equippedFireSpells[index];
            }
            for (int index = 0; index < run.rogueEquippedSpellIds.Length; index++)
                if (run.rogueEquippedSpellIds[index] == FirstRunExperienceCatalog.OriginSpellId)
                    run.rogueEquippedSpellIds[index] = string.Empty;
            if (run.IsInAcademyLayer)
            {
                // 随机层的节点绑定必须整体覆盖构造函数里的默认分配，避免残留旧郊道的绑定。
                run.completed.Clear();
                foreach (string id in dto.CompletedNodeIds.Where(id => run.AcademyLayerNodes.Any(node => node.Id == id))) run.completed.Add(id);
                run.visited.RemoveWhere(id => !run.AcademyLayerNodes.Any(node => node.Id == id));
                run.ReplaceEncounterAssignments(RestoreEncounterAssignments(dto.EncounterAssignments));
                run.ReplaceNodeContentAssignments(RestoreNodeContentAssignments(dto.NodeContentAssignments));
                run.SyncAcademyLayerProjection();
                // 旧版待领取战斗奖励未保存候选；读取时按原种子补记一次，后续整理背包不再重抽。
                if (run.AwaitingReward && run.MapNode(run.CurrentNodeId).IsCombat &&
                    dto.PendingRewardIds.Count == 0 && dto.RolledRewardChoiceIds.Count == 0)
                    dto.RolledRewardChoiceIds.AddRange(run.CurrentRewards.Select(reward => reward.Id));
            }
            foreach (FirstRunWorkshopSnapshot workshop in run.CompletedWorkshopBuilds.Where(value => value.ForgeCompleted))
            {
                OCC.Combat.Roguelite.EquipmentInstanceDto equipment = dto.EquipmentInstances.FirstOrDefault(value =>
                    value.InstanceId == workshop.ForgedTargetId);
                if (equipment != null && string.IsNullOrEmpty(equipment.ForgeMaterialId))
                    equipment.ForgeMaterialId = workshop.ForgedMaterialId;
            }
            return run;
        }
        private static IReadOnlyList<RogueliteEncounterAssignment> RestoreEncounterAssignments(IEnumerable<string> rows)
        {
            List<RogueliteEncounterAssignment> restored = new List<RogueliteEncounterAssignment>();
            foreach (string row in rows ?? Array.Empty<string>())
            {
                int separator = row.IndexOf('=');
                if (separator <= 0 || separator == row.Length - 1) throw new InvalidOperationException("Invalid encounter assignment row.");
                restored.Add(new RogueliteEncounterAssignment(AcademyMapSaveMigration.NodeId(row.Substring(0, separator)), row.Substring(separator + 1)));
            }
            return restored;
        }

        private static IReadOnlyList<AcademyEventAssignment> RestoreNodeContentAssignments(IEnumerable<string> rows)
        {
            List<AcademyEventAssignment> restored = new List<AcademyEventAssignment>();
            foreach (string row in rows ?? Array.Empty<string>())
            {
                int separator = row.IndexOf('=');
                if (separator <= 0 || separator == row.Length - 1) throw new InvalidOperationException("Invalid node content assignment row.");
                string nodeId = AcademyMapSaveMigration.NodeId(row.Substring(0, separator));
                string eventId = row.Substring(separator + 1);
                if (nodeId == "tower_lift" && eventId == "EV16") eventId = "T02";
                restored.Add(new AcademyEventAssignment(nodeId, eventId));
            }
            return restored;
        }

        private static string StarterWeaponId(string starterId)        {
            if (starterId == FireRogueliteStarterCatalog.Melee) return "war_hammer";
            if (starterId == FireRogueliteStarterCatalog.Ranged) return "arcane_wand";
            return null;
        }
        private static void Replace(List<string> target, IEnumerable<string> source)
        { target.Clear(); target.AddRange(source ?? Array.Empty<string>()); }
        // map10 is a read-only migration input. Its writer is compiled only for historical test fixtures.
#if UNITY_INCLUDE_TESTS
        public string ToJson() => ToLegacyMap10TestFixture();
        public static RogueliteMapRun FromJson(string json) => FromLegacyMap10(json);
        internal string ToLegacyMap10TestFixture() => string.Join("|", "map10", Seed, RegionBossId, CurrentNodeId, Level, Experience, 0, Supplies, ScoutingBeacons, Parts, Aether, EquippedWeaponId ?? string.Empty, EquippedSpellId ?? string.Empty, IsAetherCalibrated ? "1" : "0", PendingContentChoiceId ?? string.Empty, PendingContentCombatMissionId ?? string.Empty, string.Join(",", visited.OrderBy(id => id, StringComparer.Ordinal)), string.Join(",", completed.OrderBy(id => id, StringComparer.Ordinal)), string.Join(",", claimedRewards), AwaitingReward ? "1" : "0", string.Join(",", ownedFireSpells), string.Join(",", equippedFireSpells.Select(id => id ?? string.Empty)), Convert.ToBase64String(Encoding.UTF8.GetBytes(Inventory.ToDataString())), string.Join(",", ItemQuickbar.Select(id => id ?? string.Empty)), nextItemSequence, Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(";", lootProgress.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value)))), FireSpellCatalog.Version, EncodeMigrationClaims(pendingFireSpellReselections), EncodeMigrationClaims(fireSpellRetirementCompensations), Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(",", fireSpellMigrationWarnings.OrderBy(id => id, StringComparer.Ordinal)))), deferredNodeReward ? "1" : "0", StarterId ?? string.Empty, HasCombatSnapshot ? "1" : "0", CurrentHealth, CurrentShield, CurrentMana);
#endif
        public static RogueliteMapRun FromLegacyMap10(string json)
        {
            RogueliteMapRun run = ReadLegacyMapData(json);
            run.CurrentNodeId = AcademyMapSaveMigration.NodeId(run.CurrentNodeId);
            run.PendingContentChoiceId = AcademyMapSaveMigration.ChoiceId(run.PendingContentChoiceId);
            run.claimedRewards.RemoveAll(AcademyMapSaveMigration.IsRetiredProgressMarker);
            return run;
        }
        private static RogueliteMapRun ReadLegacyMapData(string json)
        {
            string[] parts = (json ?? throw new ArgumentNullException(nameof(json))).Split('|');
            AcademyMapSaveMigration.ValidateHistoricalCounter(parts);
            if (parts.Length == 36 && (parts[0] == "map10" || parts[0] == "map9"))
            {
                if (!string.Equals(parts[26], FireSpellCatalog.Version, StringComparison.Ordinal)) throw new InvalidOperationException("Unsupported fire spell catalog version.");
                bool legacyLayout = parts[0] == "map9";
                RogueliteMapRun currentRun = RestoreMap6Fields(parts, false); RestoreInventoryAndLoot(currentRun, parts, legacyLayout);
                currentRun.pendingFireSpellReselections.AddRange(DecodeMigrationClaims(parts[27], FireSpellSaveMigrationKind.ReselectSameRarity));
                currentRun.fireSpellRetirementCompensations.AddRange(DecodeMigrationClaims(parts[28], FireSpellSaveMigrationKind.Compensation));
                currentRun.fireSpellMigrationWarnings.AddRange(Encoding.UTF8.GetString(Convert.FromBase64String(parts[29])).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                currentRun.deferredNodeReward = parts[30] == "1"; currentRun.StarterId = parts[31]; currentRun.HasCombatSnapshot = parts[32] == "1";
                currentRun.CurrentHealth = int.Parse(parts[33]); currentRun.CurrentShield = int.Parse(parts[34]); currentRun.CurrentMana = int.Parse(parts[35]);
                return currentRun;
            }
            if (parts.Length == 31 && parts[0] == "map8")
            {
                if (!string.Equals(parts[26], FireSpellCatalog.Version, StringComparison.Ordinal)) throw new InvalidOperationException("Unsupported fire spell catalog version.");
                RogueliteMapRun map8Run = RestoreMap6Fields(parts, false);
                RestoreInventoryAndLoot(map8Run, parts, true);
                map8Run.pendingFireSpellReselections.AddRange(DecodeMigrationClaims(parts[27], FireSpellSaveMigrationKind.ReselectSameRarity));
                map8Run.fireSpellRetirementCompensations.AddRange(DecodeMigrationClaims(parts[28], FireSpellSaveMigrationKind.Compensation));
                map8Run.fireSpellMigrationWarnings.AddRange(Encoding.UTF8.GetString(Convert.FromBase64String(parts[29])).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                map8Run.deferredNodeReward = parts[30] == "1";
                return map8Run;
            }
            if ((parts.Length == 25 || parts.Length == 26) && parts[0] == "map7")
            {
                RogueliteMapRun map7Run = RestoreMap6Fields(parts, true);
                RestoreInventoryAndLoot(map7Run, parts, true);
                return map7Run;
            }
            if (parts.Length == 9 && parts[0] == "map1") return FromMap1(parts);
            if (parts.Length == 10 && parts[0] == "map2") return FromMap2(parts);
            if (parts.Length == 14 && parts[0] == "map3") return FromMap3(parts);
            if (parts.Length == 19 && parts[0] == "map4") return FromMap4(parts);
            if (parts.Length == 22 && parts[0] == "map6")
            {
                return RestoreMap6Fields(parts, true);
            }
            if (parts.Length != 20 || parts[0] != "map5") throw new InvalidOperationException("Unsupported map run save version.");
            var run = new RogueliteMapRun(int.Parse(parts[1])) { RegionBossId = "core_overseer", CurrentNodeId = parts[3], Level = int.Parse(parts[4]), Experience = int.Parse(parts[5]), Supplies = int.Parse(parts[7]), ScoutingBeacons = int.Parse(parts[8]), Parts = int.Parse(parts[9]), Aether = int.Parse(parts[10]), EquippedWeaponId = parts[11], EquippedSpellId = parts[12], IsAetherCalibrated = parts[13] == "1", PendingContentChoiceId = parts[14], PendingContentCombatMissionId = parts[15], AwaitingReward = parts[19] == "1" };
            Restore(run.visited, parts[16], true); Restore(run.completed, parts[17], false); run.claimedRewards.AddRange(parts[18].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun RestoreMap6Fields(string[] parts, bool migrateLegacy)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { RegionBossId = "core_overseer", CurrentNodeId = parts[3], Level = int.Parse(parts[4]), Experience = int.Parse(parts[5]), Supplies = int.Parse(parts[7]), ScoutingBeacons = int.Parse(parts[8]), Parts = int.Parse(parts[9]), Aether = int.Parse(parts[10]), EquippedWeaponId = parts[11], EquippedSpellId = parts[12], IsAetherCalibrated = parts[13] == "1", PendingContentChoiceId = parts[14], PendingContentCombatMissionId = parts[15], AwaitingReward = parts[19] == "1" };
            Restore(run.visited, parts[16], true); Restore(run.completed, parts[17], false); run.claimedRewards.AddRange(parts[18].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            string[] rawOwned = parts[20].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] rawEquipped = parts[21].Split(',');
            if (migrateLegacy)
            {
                bool nodeReward = run.AwaitingReward;
                FireSpellSaveMigrationResult migration = FireSpellSaveMigration.Migrate(rawOwned, rawEquipped);
                run.ownedFireSpells.AddRange(migration.DirectOwnedIds.Distinct(StringComparer.Ordinal));
                for (int i = 0; i < Math.Min(run.equippedFireSpells.Length, migration.EquippedNewIds.Count); i++)
                    if (run.ownedFireSpells.Contains(migration.EquippedNewIds[i])) { run.equippedFireSpells[i] = migration.EquippedNewIds[i]; run.rogueEquippedSpellIds[4 + i] = migration.EquippedNewIds[i]; }
                run.pendingFireSpellReselections.AddRange(migration.ReselectClaims);
                run.fireSpellRetirementCompensations.AddRange(migration.CompensationClaims);
                run.fireSpellMigrationWarnings.AddRange(migration.UnknownLegacyIds.Select(id => "unknown_legacy_fire_spell:" + id));
                run.deferredNodeReward = nodeReward && run.pendingFireSpellReselections.Count > 0;
                run.AwaitingReward = nodeReward || run.pendingFireSpellReselections.Count > 0;
            }
            else
            {
                run.ownedFireSpells.AddRange(rawOwned.Where(id => FireSpellCatalog.Get(id) != null).Distinct(StringComparer.Ordinal));
                for (int i = 0; i < Math.Min(run.equippedFireSpells.Length, rawEquipped.Length); i++) if (run.ownedFireSpells.Contains(rawEquipped[i])) { run.equippedFireSpells[i] = rawEquipped[i]; run.rogueEquippedSpellIds[4 + i] = rawEquipped[i]; }
            }
            return run;
        }
        private static void RestoreInventoryAndLoot(RogueliteMapRun run, string[] parts, bool legacyLayout)
        {
            string inventoryData = Encoding.UTF8.GetString(Convert.FromBase64String(parts[22]));
            run.Inventory = legacyLayout ? InventoryContainerState.FromLegacyMap9DataString(inventoryData) : InventoryContainerState.FromDataString(inventoryData);
            run.ItemQuickbar = new string[OCC.Combat.Roguelite.RogueRuntimeConstants.ItemQuickbarSize]; string[] itemSlots = parts[23].Split(',');
            for (int i = 0; i < Math.Min(run.ItemQuickbar.Length, itemSlots.Length); i++) if (run.Inventory.Get(itemSlots[i]) != null) run.ItemQuickbar[i] = itemSlots[i];
            run.nextItemSequence = int.Parse(parts[24]);
            if (parts.Length <= 25) return;
            string progressData = Encoding.UTF8.GetString(Convert.FromBase64String(parts[25]));
            foreach (string row in progressData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { int separator = row.IndexOf('='); if (separator > 0) run.lootProgress[row.Substring(0, separator)] = row.Substring(separator + 1); }
        }
        private static string EncodeMigrationClaims(IEnumerable<FireSpellSaveMigrationClaim> claims)
        {
            string raw = string.Join(";", (claims ?? Array.Empty<FireSpellSaveMigrationClaim>()).Select(claim => claim.LegacyId + "@" + string.Join(".", claim.OriginalEquippedSlots)));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }
        private static IReadOnlyList<FireSpellSaveMigrationClaim> DecodeMigrationClaims(string encoded, FireSpellSaveMigrationKind kind)
        {
            string raw = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)); List<FireSpellSaveMigrationClaim> result = new List<FireSpellSaveMigrationClaim>();
            foreach (string row in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] values = row.Split('@'); FireSpellSaveMigrationEntry entry = FireSpellSaveMigration.Get(values[0]);
                if (entry.Kind != kind) throw new InvalidOperationException("Fire spell migration claim kind mismatch.");
                int[] slots = values.Length < 2 ? Array.Empty<int>() : values[1].Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                result.Add(new FireSpellSaveMigrationClaim(entry.LegacyId, entry.LegacyRarity, kind, slots));
            }
            return result;
        }
        private static int StableChoiceKey(string claimId, string spellId)
        {
            unchecked { int hash = 17; foreach (char c in claimId + "|" + spellId) hash = hash * 31 + c; return hash; }
        }
        private static RogueliteMapRun FromMap4(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), Supplies = int.Parse(parts[6]), ScoutingBeacons = int.Parse(parts[7]), Parts = int.Parse(parts[8]), Aether = int.Parse(parts[9]), EquippedWeaponId = parts[10], EquippedSpellId = parts[11], IsAetherCalibrated = parts[12] == "1", PendingContentChoiceId = parts[13], PendingContentCombatMissionId = parts[14], AwaitingReward = parts[18] == "1" };
            Restore(run.visited, parts[15], true); Restore(run.completed, parts[16], false); run.claimedRewards.AddRange(parts[17].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap3(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), Supplies = int.Parse(parts[6]), ScoutingBeacons = int.Parse(parts[7]), PendingContentChoiceId = parts[8], PendingContentCombatMissionId = parts[9], AwaitingReward = parts[13] == "1" };
            Restore(run.visited, parts[10], true); Restore(run.completed, parts[11], false); run.claimedRewards.AddRange(parts[12].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap2(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AwaitingReward = parts[9] == "1" };
            Restore(run.visited, parts[6], true); Restore(run.completed, parts[7], false); run.claimedRewards.AddRange(parts[8].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap1(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AwaitingReward = parts[8] == "1" };
            Restore(run.visited, parts[5], true); Restore(run.completed, parts[6], false); run.claimedRewards.AddRange(parts[7].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            return run;
        }
        private static void Restore(HashSet<string> destination, string source, bool includeStart)
        {
            destination.Clear(); foreach (string id in source.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(AcademyMapSaveMigration.NodeId)) if (RogueliteMapCatalog.Nodes.Any(node => node.Id == id)) destination.Add(id);
            if (includeStart) destination.Add("start");
        }
        public void ApplyBuild(UnitState hero)
        {
            if (rogueRunDto != null)
            {
                int forgeManaBonus = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto).ForgeManaCapacityBonus;
                hero.ConfigureMana(12 + forgeManaBonus, rogueRunDto.CurrentMana);
                if (hero.Health > rogueRunDto.CurrentHealth) hero.TakeDamage(hero.Health - rogueRunDto.CurrentHealth);
                hero.ClearShield(); return;
            }
            hero.ConfigureMana(12, HasCombatSnapshot ? CurrentMana : 12);
            if (!string.IsNullOrEmpty(EquippedWeaponId)) hero.Equip(RogueliteMapCatalog.Rewards.First(item => item.Id == EquippedWeaponId).Weapon, CombatCatalog.Shield, hero.SkillOne, hero.SkillTwo);
            if (!string.IsNullOrEmpty(EquippedSpellId)) hero.Equip(hero.MainHand, CombatCatalog.Shield, RogueliteMapCatalog.Rewards.First(item => item.Id == EquippedSpellId).Spell, hero.SkillTwo);
            if (HasCombatSnapshot)
            {
                if (hero.Health > CurrentHealth) hero.TakeDamage(hero.Health - CurrentHealth);
                if (hero.Shield > CurrentShield) hero.AbsorbShield(hero.Shield - CurrentShield);
                else if (hero.Shield < CurrentShield) hero.GrantShield(CurrentShield - hero.Shield);
            }
        }
    }
}
