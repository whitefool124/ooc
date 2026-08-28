using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCC.Combat
{
    public enum RogueliteMapNodeType { Start, Combat, Elite, Event, Workshop, Shop, Rest, Treasure, Finale }
    public enum RogueliteMapNodeVisualState { Current, Available, Locked, Cleared, Visited, Known, Unknown }

    // Runtime tuning only: playtests can adjust these values without changing map topology or save data.
    public static class AcademyMapTuning
    {
        public const int ExpectedBossProgress = 20;
        public const int BossMinimumProgress = 12;
        public const int CorePermitRequirement = 2;
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
        public int RequiredAccessCards { get; }
        public int GrantedAccessCards { get; }
        public bool IsCombat => Type == RogueliteMapNodeType.Combat || Type == RogueliteMapNodeType.Elite || Type == RogueliteMapNodeType.Finale;

        public RogueliteMapNode(string id, RogueliteMapNodeType type, string displayName, string summary, int gridX, int gridY, int requiredAccessCards, int grantedAccessCards, params string[] nextIds)
        {
            Id = id; Type = type; DisplayName = displayName; Summary = summary; GridX = gridX; GridY = gridY;
            RequiredAccessCards = requiredAccessCards; GrantedAccessCards = grantedAccessCards; NextIds = nextIds ?? Array.Empty<string>();
        }
    }

    public enum RogueliteRewardKind { Weapon, Spell, Item, Equipment }

    public static class FireRogueliteStarterCatalog
    {
        public const string Melee = "fire_melee";
        public const string Universal = "fire_universal";
        public const string Ranged = "fire_ranged";
        public static readonly IReadOnlyList<string> All = new[] { Melee, Universal, Ranged };
        public static string DisplayName(string id) => id == Melee ? "近战热压" : id == Ranged ? "远程导能" : id == Universal ? "武器热载" : "旧版推进";
    }
    public enum RogueliteNodeContentEffect { Supplies, ScoutingBeacon, AccessCard, Reward, Aether, Recovery, Economy, Intelligence }
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
        public bool GrantsCorePermit { get; }
        public RogueliteNodeContentChoice(string id, string displayName, string preview, RogueliteNodeContentEffect effect,
            string rewardId = null, bool requiresCombat = false, string combatMissionId = null,
            int partsCost = 0, int aetherCost = 0, int goldCost = 0, int contributionCost = 0,
            int goldGain = 0, int contributionGain = 0, int healthGain = 0, int manaGain = 0,
            bool grantsCorePermit = false)
        {
            Id = id; DisplayName = displayName; Preview = preview; Effect = effect; RewardId = rewardId;
            RequiresCombat = requiresCombat; CombatMissionId = combatMissionId; PartsCost = partsCost; AetherCost = aetherCost;
            GoldCost = goldCost; ContributionCost = contributionCost; GoldGain = goldGain; ContributionGain = contributionGain;
            HealthGain = healthGain; ManaGain = manaGain; GrantsCorePermit = grantsCorePermit;
        }
    }

    public static class RogueliteNodeContentCatalog
    {
        public static IReadOnlyList<RogueliteNodeContentChoice> ChoicesFor(RogueliteMapNode node)
            => ChoicesFor(node, null);

        public static IReadOnlyList<RogueliteNodeContentChoice> ChoicesFor(RogueliteMapNode node, string eventId)
        {
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
                        new RogueliteNodeContentChoice("overload", "超载回收", "收益：+1 权限卡；后果：进入一场额外战斗。", RogueliteNodeContentEffect.AccessCard, requiresCombat: true, combatMissionId: "relay_event")
                    };
                case RogueliteMapNodeType.Rest:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("field_repair", "现场整备", "收益：+1 补给、恢复 6 生命/2 护盾/4 个人魔力；无敌情推进。", RogueliteNodeContentEffect.Recovery),
                        new RogueliteNodeContentChoice("scan_routes", "校准信标", "收益：+1 侦测信标；无额外战斗。", RogueliteNodeContentEffect.ScoutingBeacon)
                    };
                case RogueliteMapNodeType.Workshop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("wand_calibration", "以太聚焦校准", "收益：获得以太聚焦手杖；替换操作将在 R2-03 工坊系统开放。", RogueliteNodeContentEffect.Reward, "arcane_wand"),
                        new RogueliteNodeContentChoice("supply_strip", "拆解补给", "收益：+1 补给；无额外战斗。", RogueliteNodeContentEffect.Supplies)
                    };
                case RogueliteMapNodeType.Shop:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("medical_cache", "医疗补给", "价格：2 零件；收益：+1 补给。", RogueliteNodeContentEffect.Supplies, partsCost: 2),
                        new RogueliteNodeContentChoice("signal_contract", "情报合约", "价格：1 零件 + 1 以太；收益：+1 侦测信标。", RogueliteNodeContentEffect.ScoutingBeacon, partsCost: 1, aetherCost: 1),
                        new RogueliteNodeContentChoice("buy_hazard_condenser", "购入险地冷凝器", "价格：3 零件 + 1 以太；收益：法宝“险地冷凝器”。", RogueliteNodeContentEffect.Reward, "G-T11", partsCost: 3, aetherCost: 1)
                    };
                case RogueliteMapNodeType.Treasure:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("vault_fire_cache", "核心术式库", "收益：公开 1 稀有个人术式、1 卷轴与 1 法宝，三选一。", RogueliteNodeContentEffect.Reward)
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
        public RogueliteReward(string id, string displayName, WeaponDefinition weapon, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Weapon; Weapon = weapon; BuildPath = buildPath; }
        public RogueliteReward(string id, string displayName, SkillDefinition spell, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Spell; Spell = spell; BuildPath = buildPath; }
        public RogueliteReward(OCC.Combat.Roguelite.SpellDefinition spell, string buildPath)
        { RogueSpell = spell ?? throw new ArgumentNullException(nameof(spell)); Id = spell.DefinitionId; DisplayName = spell.DisplayName; Kind = RogueliteRewardKind.Spell; BuildPath = buildPath; }
        public RogueliteReward(ItemDefinition item, string buildPath) { Item = item ?? throw new ArgumentNullException(nameof(item)); Id = item.Id; DisplayName = item.DisplayName; Kind = RogueliteRewardKind.Item; BuildPath = buildPath; }
        public RogueliteReward(OCC.Combat.Roguelite.EquipmentDefinition equipment, string buildPath)
        { Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment)); Id = equipment.DefinitionId; DisplayName = equipment.DisplayName; Kind = RogueliteRewardKind.Equipment; BuildPath = buildPath; }
    }

    public static class RogueliteMapCatalog
    {
        // This is an orthogonal room graph. Connections are interpreted as bidirectional at runtime,
        // so content authors only need to list an edge once.
        public static readonly IReadOnlyList<RogueliteMapNode> Nodes = new[]
        {
            new RogueliteMapNode("start", RogueliteMapNodeType.Start, "学院郊道", "首区入口。", 0, 2, 0, 0, "rail_patrol", "depot_wreck", "supply_checkpoint"),
            new RogueliteMapNode("rail_patrol", RogueliteMapNodeType.Combat, "石路巡哨", "清除巡哨队。", 1, 2, 0, 0, "start", "switchyard", "relay_raid", "supply_checkpoint"),
            new RogueliteMapNode("depot_wreck", RogueliteMapNodeType.Combat, "废弃驿站", "清除驿站守敌。", 1, 1, 0, 0, "start", "switchyard"),
            new RogueliteMapNode("supply_checkpoint", RogueliteMapNodeType.Shop, "行商补给点", "补给与零件交易。", 1, 3, 0, 0, "start", "rail_patrol", "field_workshop"),
            new RogueliteMapNode("switchyard", RogueliteMapNodeType.Event, "分岔石桥", "风险与收益预览事件。", 2, 1, 0, 0, "depot_wreck", "rail_patrol", "signal_hub", "relay_event"),
            new RogueliteMapNode("relay_raid", RogueliteMapNodeType.Combat, "野外导能柱", "破坏被敌军占用的导能柱。", 2, 2, 0, 0, "rail_patrol", "relay_event", "med_bay", "field_workshop"),
            new RogueliteMapNode("field_workshop", RogueliteMapNodeType.Workshop, "随军工坊", "更换与维护构筑。", 2, 3, 0, 0, "supply_checkpoint", "relay_raid", "med_bay", "permit_archive"),
            new RogueliteMapNode("signal_hub", RogueliteMapNodeType.Combat, "传讯石庭", "清除石庭守军。", 3, 1, 0, 0, "switchyard", "relay_event", "elite_foundry"),
            new RogueliteMapNode("relay_event", RogueliteMapNodeType.Event, "导能柱记录", "查阅现场记录。", 3, 2, 0, 0, "switchyard", "relay_raid", "signal_hub", "med_bay", "gatehouse"),
            new RogueliteMapNode("med_bay", RogueliteMapNodeType.Rest, "行军医帐", "恢复与休整。", 3, 3, 0, 0, "relay_raid", "field_workshop", "relay_event", "permit_archive", "sealed_market"),
            new RogueliteMapNode("elite_foundry", RogueliteMapNodeType.Elite, "刻阵工坊", "高风险精英战斗。", 4, 1, 0, 0, "signal_hub", "gatehouse", "transmission_tower"),
            new RogueliteMapNode("gatehouse", RogueliteMapNodeType.Combat, "石闸关口", "打开通往古塔的道路。", 4, 2, 0, 0, "relay_event", "elite_foundry", "sealed_market", "aether_refinery"),
            new RogueliteMapNode("sealed_market", RogueliteMapNodeType.Shop, "封存商行", "双货币交易点。", 4, 3, 0, 0, "med_bay", "gatehouse", "permit_archive", "aether_refinery", "safety_room"),
            new RogueliteMapNode("permit_archive", RogueliteMapNodeType.Event, "许可档案", "可预览的档案提取；完成后获得权限卡。", 4, 4, 0, 1, "field_workshop", "med_bay", "sealed_market", "safety_room"),
            new RogueliteMapNode("transmission_tower", RogueliteMapNodeType.Combat, "传讯塔楼", "许可门后的战斗节点。", 5, 1, 1, 0, "elite_foundry", "aether_refinery", "core_approach"),
            new RogueliteMapNode("aether_refinery", RogueliteMapNodeType.Event, "以太校准室", "可预览的高收益事件。", 5, 2, 0, 0, "gatehouse", "sealed_market", "transmission_tower", "safety_room", "core_vault"),
            new RogueliteMapNode("safety_room", RogueliteMapNodeType.Event, "守夜值班记录", "公开选择补给、情报或追加战斗。", 5, 3, 0, 0, "sealed_market", "permit_archive", "aether_refinery", "core_vault"),
            new RogueliteMapNode("core_approach", RogueliteMapNodeType.Elite, "塔前石庭", "古塔前庭精英守备。", 6, 1, 1, 0, "transmission_tower", "core_vault", "core_finale"),
            new RogueliteMapNode("core_vault", RogueliteMapNodeType.Treasure, "学院封存库", "战利品节点。", 6, 2, 1, 0, "aether_refinery", "safety_room", "core_approach", "core_finale"),
            new RogueliteMapNode("core_finale", RogueliteMapNodeType.Finale, "古塔核心", "击败封存塔的核心守卫。", 7, 1, 1, 0, "core_approach", "core_vault", "seal_bridge", "tower_foyer"),
            new RogueliteMapNode("academy_gate", RogueliteMapNodeType.Event, "学院正门公告", "公开的入学期委托与路线情报。", 0, 0, 0, 0, "tutorial_hall", "dorm_drill"),
            new RogueliteMapNode("tutorial_hall", RogueliteMapNodeType.Combat, "新生演练厅", "处理公开演练中的失控傀儡。", 0, 1, 0, 0, "academy_gate", "start", "dorm_watch"),
            new RogueliteMapNode("dorm_watch", RogueliteMapNodeType.Combat, "宿舍夜间巡查", "清理夜间异常并保护宿舍区。", 0, 3, 0, 0, "tutorial_hall", "market_lane"),
            new RogueliteMapNode("market_lane", RogueliteMapNodeType.Combat, "学院市集护送", "护送器材通过市集外廊。", 0, 4, 0, 0, "dorm_watch", "field_infirmary"),
            new RogueliteMapNode("dorm_drill", RogueliteMapNodeType.Combat, "宿舍外实战演练", "近距离考核走位与护盾。", 1, 0, 0, 0, "academy_gate", "lecture_annex", "depot_wreck"),
            new RogueliteMapNode("field_infirmary", RogueliteMapNodeType.Event, "临时医务站", "公开选择补给、恢复或额外救援战。", 1, 4, 0, 0, "market_lane", "study_vault"),
            new RogueliteMapNode("lecture_annex", RogueliteMapNodeType.Combat, "讲坛公开考核", "在远程威胁下完成学院考核。", 2, 0, 0, 0, "dorm_drill", "archive_wing", "switchyard"),
            new RogueliteMapNode("study_vault", RogueliteMapNodeType.Combat, "阅览室封存柜异常", "清理封存柜周边的异常防卫。", 2, 4, 0, 0, "field_infirmary", "sparring_ring"),
            new RogueliteMapNode("archive_wing", RogueliteMapNodeType.Combat, "档案翼巡查", "处理档案翼中的显影误报。", 3, 0, 0, 0, "lecture_annex", "workshop_yard", "signal_hub"),
            new RogueliteMapNode("sparring_ring", RogueliteMapNodeType.Combat, "圆形实训场", "对抗训练阵列并获得构筑奖励。", 3, 4, 0, 0, "study_vault", "permit_archive", "supply_depot"),
            new RogueliteMapNode("workshop_yard", RogueliteMapNodeType.Combat, "工坊庭院回路过载", "处理失控校准回路。", 4, 0, 0, 0, "archive_wing", "clinic_hall", "elite_foundry"),
            new RogueliteMapNode("clinic_hall", RogueliteMapNodeType.Combat, "诊疗厅导能泄漏", "在泄漏环境中保护治疗设备。", 5, 0, 0, 0, "workshop_yard", "wilds_path", "transmission_tower"),
            new RogueliteMapNode("supply_depot", RogueliteMapNodeType.Elite, "封存器材护送", "精英护送考核，取得高风险奖励。", 5, 4, 0, 0, "sparring_ring", "permit_archive", "wilds_camp"),
            new RogueliteMapNode("wilds_path", RogueliteMapNodeType.Combat, "郊野实训旧道", "开阔地中的学院实训巡查。", 6, 0, 0, 0, "clinic_hall", "seal_bridge", "core_approach"),
            new RogueliteMapNode("observatory_path", RogueliteMapNodeType.Elite, "观测塔求援", "处理封存区外环的高阶异常。", 6, 3, 0, 0, "wilds_camp", "tower_foyer", "core_vault"),
            new RogueliteMapNode("wilds_camp", RogueliteMapNodeType.Elite, "郊野导能柱考察", "高风险实训，提供核心许可来源。", 6, 4, 0, 1, "supply_depot", "observatory_path", "tower_records"),
            new RogueliteMapNode("seal_bridge", RogueliteMapNodeType.Combat, "封存区石桥", "清理通往高塔的学院警戒装置。", 7, 0, 0, 0, "wilds_path", "tower_foyer", "core_finale"),
            new RogueliteMapNode("tower_foyer", RogueliteMapNodeType.Elite, "封存塔门厅核验", "精英守卫与维护链的最终考核。", 7, 2, 0, 1, "seal_bridge", "observatory_path", "tower_lift", "core_finale"),
            new RogueliteMapNode("tower_records", RogueliteMapNodeType.Event, "高塔值守记录", "公开选择首领情报或追加挑战。", 7, 3, 0, 0, "wilds_camp", "tower_lift"),
            new RogueliteMapNode("tower_lift", RogueliteMapNodeType.Treasure, "封存管理员匣", "稀有法宝与核心许可的公开取舍。", 7, 4, 0, 0, "tower_records", "tower_foyer")
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
            .Concat(ArtifactCatalog.All.Select(artifact => new RogueliteReward(ItemCatalog.Get(artifact.Id), artifact.BuildUse)))
            .ToArray();
        public static RogueliteMapNode Node(string id) => Nodes.First(node => node.Id == id);
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
            ArtifactDefinition[] eligible = ArtifactCatalog.All
                .Where(artifact => IsPoolEligible(artifact, nodeType) && IsTierEligible(artifact.Rarity, nodeType) && !owned.Contains(artifact.Id)).ToArray();
            if (eligible.Length == 0) eligible = ArtifactCatalog.All.Where(artifact => IsPoolEligible(artifact, nodeType) && !owned.Contains(artifact.Id)).ToArray();
            if (eligible.Length == 0) eligible = ArtifactCatalog.All.Where(artifact => !owned.Contains(artifact.Id)).ToArray();
            if (eligible.Length == 0) eligible = ArtifactCatalog.All.ToArray();
            string key = seed + "|" + progress + "|" + nodeType;
            return eligible.OrderBy(artifact => StableKey(key, artifact.Id)).ThenBy(artifact => artifact.Id, StringComparer.Ordinal).First();
        }

        public static ArtifactDefinition RollLoot(int seed, string sourceId)
        {
            string key = seed + "|loot|" + (sourceId ?? string.Empty);
            ArtifactDefinition[] eligible = ArtifactCatalog.All.Where(artifact => (artifact.ContentSources & ArtifactContentSource.Loot) != 0).ToArray();
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

    public sealed class RogueliteMapRun
    {
        private readonly HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { "start" };
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
        private OCC.Combat.Roguelite.RogueRunDto rogueRunDto;
        public int Seed { get; }
        public string CurrentNodeId { get; private set; } = "start";
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int AccessCards { get; private set; }
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
        public int CurrentHealth { get; private set; } = 18;
        public int CurrentShield { get; private set; } = 2;
        public int CurrentMana { get; private set; } = 12;
        public bool UsesRogue11 => rogueRunDto != null;
        public int Gold => rogueRunDto?.Gold ?? 0;
        public int StageContribution => rogueRunDto?.StageContribution ?? 0;
        public int StageTime => rogueRunDto?.StageTime ?? AcademyProgress;
        public OCC.Combat.Roguelite.RogueRunDto RogueRunState => rogueRunDto;
        public bool AwaitingReward { get; private set; }
        public bool IsComplete => completed.Contains("core_finale") && !AwaitingReward;
        public int AcademyProgress => Math.Max(0, visited.Count - 1);
        public int CorePermits => completed.Count(id => RogueliteMapCatalog.Node(id).GrantedAccessCards > 0) +
            claimedRewards.Count(id => id.StartsWith("permit:", StringComparison.Ordinal));
        public int ProgressPermits => UsesRogue11 ? CorePermits : AccessCards;
        public AcademyMapPhase AcademyPhase => StageTime >= AcademyMapTuning.TransitionProgress
            ? AcademyMapPhase.TransitionReady
            : StageTime >= AcademyMapTuning.ConsolidationProgress
                ? AcademyMapPhase.Consolidation
                : AcademyMapPhase.NormalTerm;
        public bool CanChallengeAcademyFinale => StageTime >= AcademyMapTuning.TransitionProgress ||
            (AcademyProgress >= AcademyMapTuning.BossMinimumProgress && CorePermits >= AcademyMapTuning.CorePermitRequirement);
        public bool IsTransitionPending => AcademyMapTuning.EnforceTransition
            && StageTime >= AcademyMapTuning.TransitionProgress;
        public IReadOnlyCollection<string> UnlockedNodes => visited;
        public IReadOnlyCollection<string> VisitedNodes => visited;
        public IReadOnlyCollection<string> CompletedNodes => completed;
        public IReadOnlyList<string> ClaimedRewards => claimedRewards;
        public IReadOnlyList<string> OwnedFireSpellIds => ownedFireSpells;
        public IReadOnlyList<string> EquippedFireSpellIds => equippedFireSpells;
        public IReadOnlyList<string> RogueEquippedSpellIds => rogueEquippedSpellIds;
        public IReadOnlyList<FireSpellSaveMigrationClaim> PendingFireSpellReselections => pendingFireSpellReselections;
        public IReadOnlyList<FireSpellSaveMigrationClaim> FireSpellRetirementCompensations => fireSpellRetirementCompensations;
        public IReadOnlyList<string> FireSpellMigrationWarnings => fireSpellMigrationWarnings;
        public InventoryContainerState Inventory { get; private set; }
        public string[] ItemQuickbar { get; private set; } = new string[8];
        public int NextItemSequence => nextItemSequence;
        public IReadOnlyDictionary<string, string> LootProgress => lootProgress;
        public IReadOnlyDictionary<string, string> EncounterAssignments => encounterAssignments;
        public IReadOnlyDictionary<string, string> NodeContentAssignments => nodeContentAssignments;
        public bool HasDeferredNodeReward => deferredNodeReward;
        public WeaponDefinition EquippedWeapon => string.IsNullOrEmpty(EquippedWeaponId) ? CombatCatalog.Rifle : RogueliteMapCatalog.Rewards.First(reward => reward.Id == EquippedWeaponId).Weapon;
        public IReadOnlyList<RogueliteReward> CurrentRewards
        {
            get
            {
                if (!AwaitingReward) return Array.Empty<RogueliteReward>();
                if (rogueRunDto == null) return RogueliteMapCatalog.RollFireSupportRewards(Seed, completed.Count, RogueliteMapCatalog.Node(CurrentNodeId).Type, Inventory.Items.Select(item => item.DefinitionId));
                OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
                string source = RogueliteMapCatalog.Node(CurrentNodeId).Type == RogueliteMapNodeType.Finale ? "boss" : RogueliteMapCatalog.Node(CurrentNodeId).Type == RogueliteMapNodeType.Elite ? "elite" : "combat";
                OCC.Combat.Roguelite.RogueAcademyContentService service = new OCC.Combat.Roguelite.RogueAcademyContentService();
                return service.Roll(Seed + completed.Count, source, OCC.Combat.Roguelite.SpellRarity.Common, OCC.Combat.Roguelite.EquipmentRarity.Common, 2, 1,
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
                RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
                if (node.Type == RogueliteMapNodeType.Event)
                    return UsesRogue11
                        ? RogueliteNodeContentCatalog.ChoicesFor(node, CurrentEventId)
                        : RogueliteNodeContentCatalog.ChoicesFor(node);
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
                combat.RogueEquipment?.WriteToDto(rogueRunDto); return;
            }
            Inventory = combat.ItemInventory.Clone(); ItemQuickbar = combat.ItemQuickbar.ToArray();
            if (combat.LootSource != null) lootProgress[combat.LootSource.Id] = combat.LootSource.ToProgressString();
            UnitState hero = combat.GetUnit("hero");
            if (hero != null)
            {
                HasCombatSnapshot = true; CurrentHealth = hero.Health; CurrentShield = hero.Shield; CurrentMana = hero.Mana;
            }
        }
        public void RestoreLootProgress(LootSourceState loot) { if (loot != null && lootProgress.TryGetValue(loot.Id, out string progress)) loot.RestoreProgress(progress); }

        public bool IsAdjacentToCurrent(string nodeId)
        {
            RogueliteMapNode current = RogueliteMapCatalog.Node(CurrentNodeId);
            RogueliteMapNode target = RogueliteMapCatalog.Node(nodeId);
            return current.NextIds.Contains(nodeId) || target.NextIds.Contains(CurrentNodeId);
        }
        public bool IsNodeAvailable(string nodeId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            if (!IsAdjacentToCurrent(nodeId) || ProgressPermits < node.RequiredAccessCards) return false;
            if (AcademyMapTuning.EnforceTransition && !visited.Contains(nodeId) && IsTransitionPending && node.Type != RogueliteMapNodeType.Finale) return false;
            if (node.Type == RogueliteMapNodeType.Finale && AcademyMapTuning.EnforceBossGate && !CanChallengeAcademyFinale) return false;
            return true;
        }
        public bool IsAcademyFinaleGateLocked(RogueliteMapNode node)
        {
            return node != null && node.Type == RogueliteMapNodeType.Finale && AcademyMapTuning.EnforceBossGate && !CanChallengeAcademyFinale;
        }
        public bool IsNodeKnown(string nodeId) => visited.Contains(nodeId) || RogueliteMapCatalog.Node(CurrentNodeId).NextIds.Contains(nodeId);
        public RogueliteMapNodeVisualState VisualStateFor(string nodeId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            if (node.Id == CurrentNodeId) return RogueliteMapNodeVisualState.Current;
            if (completed.Contains(node.Id)) return RogueliteMapNodeVisualState.Cleared;
            if (IsNodeAvailable(node.Id)) return RogueliteMapNodeVisualState.Available;
            if (IsAdjacentToCurrent(node.Id) && (ProgressPermits < node.RequiredAccessCards || IsAcademyFinaleGateLocked(node))) return RogueliteMapNodeVisualState.Locked;
            if (visited.Contains(node.Id) || IsNodeKnown(node.Id)) return RogueliteMapNodeVisualState.Known;
            return RogueliteMapNodeVisualState.Unknown;
        }
        public IReadOnlyList<RogueliteMapNode> AvailableNodes => RogueliteMapCatalog.Nodes.Where(node => IsNodeAvailable(node.Id)).ToArray();
        public void SelectNode(string nodeId)
        {
            if (!IsNodeAvailable(nodeId)) throw new InvalidOperationException(IsAcademyFinaleGateLocked(RogueliteMapCatalog.Node(nodeId))
                ? "Academy finale requires 12 explored nodes and 2 core permits."
                : "Node is not adjacent or its permission gate is locked.");
            CurrentNodeId = nodeId; visited.Add(nodeId);
        }
        public void CompleteCurrentCombat()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if (!node.IsCombat || completed.Contains(node.Id)) throw new InvalidOperationException("Current node is not an active combat.");
            Complete(node, true);
        }
        public void CompleteCurrentNode()
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if (node.Type == RogueliteMapNodeType.Start || completed.Contains(node.Id)) throw new InvalidOperationException("Current node is not available.");
            Complete(node, node.IsCombat);
        }
        public void ChooseCurrentNodeContent(string choiceId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if (node.IsCombat || completed.Contains(node.Id) || HasPendingContentCombat) throw new InvalidOperationException("Current node content is not available.");
            RogueliteNodeContentChoice choice = ResolveContentChoice(node, choiceId);
            if (rogueRunDto == null)
            {
                if (Parts < choice.PartsCost || Aether < choice.AetherCost) throw new InvalidOperationException("Insufficient parts or aether.");
                Parts -= choice.PartsCost; Aether -= choice.AetherCost;
            }
            else
            {
                if (rogueRunDto.Gold < choice.GoldCost || rogueRunDto.StageContribution < choice.ContributionCost)
                    throw new InvalidOperationException("Insufficient gold or stage contribution.");
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
            if (choice.RequiresCombat) { PendingContentCombatMissionId = choice.CombatMissionId; return; }
            ApplyContentChoice(choice); Complete(node, !UsesRogue11 && node.Type == RogueliteMapNodeType.Treasure); PendingContentChoiceId = null;
        }
        public void CompletePendingContentCombat()
        {
            if (!HasPendingContentCombat) throw new InvalidOperationException("No event combat is active.");
            RogueliteNodeContentChoice choice = ResolveContentChoice(RogueliteMapCatalog.Node(CurrentNodeId), PendingContentChoiceId);
            ApplyContentChoice(choice); Complete(RogueliteMapCatalog.Node(CurrentNodeId), false);
            PendingContentChoiceId = null; PendingContentCombatMissionId = null;
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
            RogueliteMapNode node = RogueliteMapCatalog.Node(CurrentNodeId);
            if ((!node.IsCombat && !HasPendingContentCombat) || completed.Contains(node.Id))
                throw new InvalidOperationException("Current node has no active combat to fail.");
            Complete(node, false, OCC.Combat.Roguelite.RogueEncounterOutcome.SurvivedFailure);
            PendingContentChoiceId = null; PendingContentCombatMissionId = null;
        }
        private void ApplyContentChoice(RogueliteNodeContentChoice choice)
        {
            if (rogueRunDto != null)
            {
                rogueRunDto.Gold += choice.GoldGain;
                rogueRunDto.StageContribution += choice.ContributionGain;
                rogueRunDto.CurrentHealth = Math.Max(1, Math.Min(18, rogueRunDto.CurrentHealth + choice.HealthGain));
                rogueRunDto.CurrentMana = Math.Max(0, Math.Min(12, rogueRunDto.CurrentMana + choice.ManaGain));
                CurrentHealth = rogueRunDto.CurrentHealth; CurrentMana = rogueRunDto.CurrentMana;
                if (!string.IsNullOrEmpty(choice.RewardId)) GrantRogue11Content(choice.RewardId, "event:" + CurrentEventId);
                if (choice.GrantsCorePermit)
                {
                    string permitId = "permit:" + CurrentEventId;
                    if (!claimedRewards.Contains(permitId)) claimedRewards.Add(permitId);
                }
                return;
            }
            if (choice.Effect == RogueliteNodeContentEffect.Supplies) Supplies++;
            else if (choice.Effect == RogueliteNodeContentEffect.ScoutingBeacon) ScoutingBeacons++;
            else if (choice.Effect == RogueliteNodeContentEffect.AccessCard) AccessCards++;
            else if (choice.Effect == RogueliteNodeContentEffect.Aether) { Supplies++; Aether++; }
            else if (choice.Effect == RogueliteNodeContentEffect.Recovery)
            {
                Supplies++;
                if (HasCombatSnapshot)
                {
                    CurrentHealth = Math.Min(18, CurrentHealth + 6);
                    if (CurrentShield < 6) CurrentShield = Math.Min(6, CurrentShield + 2);
                    CurrentMana = Math.Min(12, CurrentMana + 4);
                }
            }
            else if (choice.Effect == RogueliteNodeContentEffect.Reward && !string.IsNullOrEmpty(choice.RewardId) && !claimedRewards.Contains(choice.RewardId))
            {
                ItemDefinition item = ItemCatalog.All.FirstOrDefault(candidate => candidate.Id == choice.RewardId);
                if (item != null) GrantItem(item.Id);
                claimedRewards.Add(choice.RewardId);
            }
        }
        private void GrantRogue11Content(string rewardId, string source)
        {
            if (claimedRewards.Contains(rewardId)) return;
            OCC.Combat.Roguelite.RogueContentCatalog catalog = OCC.Combat.Roguelite.RogueContentCatalog.CreateAcademyV01();
            OCC.Combat.Roguelite.SpellDefinition spell = catalog.Spells.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (spell != null)
            {
                if (!rogueRunDto.MasteredSpellIds.Contains(rewardId)) rogueRunDto.MasteredSpellIds.Add(rewardId);
                claimedRewards.Add(rewardId); return;
            }
            OCC.Combat.Roguelite.EquipmentDefinition equipment = catalog.Equipment.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (equipment != null)
            {
                string instanceId = "eq-content-" + Seed + "-" + rogueRunDto.DeterministicCounter++;
                rogueRunDto.EquipmentInstances.Add(new OCC.Combat.Roguelite.EquipmentInstanceDto(instanceId, rewardId, equipment.Slot,
                    equipment.AllowedRarities[0], 0) { AcquiredOrder = rogueRunDto.EquipmentInstances.Count, SourceType = source });
                claimedRewards.Add(rewardId); return;
            }
            OCC.Combat.Roguelite.TacticalItemDefinition tactical = catalog.TacticalItems.FirstOrDefault(value => value.DefinitionId == rewardId);
            if (tactical == null) throw new InvalidOperationException("Unknown node content reward: " + rewardId);
            OCC.Combat.Roguelite.RogueEquipmentRuntime runtime = OCC.Combat.Roguelite.RogueEquipmentRuntime.FromDto(rogueRunDto);
            string tacticalId = "item-content-" + Seed + "-" + rogueRunDto.DeterministicCounter++;
            OCC.Combat.Roguelite.RogueTacticalItemInstance item = runtime.CreateTacticalItem(tacticalId, rewardId,
                runtime.AllInstances.Count + runtime.AllTacticalItems.Count, source);
            if (!runtime.AddTacticalToBackpack(item)) throw new InvalidOperationException("Backpack cannot accept node content reward: " + rewardId);
            runtime.WriteToDto(rogueRunDto); claimedRewards.Add(rewardId);
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
            OCC.Combat.Roguelite.RogueTacticalItemInstance item = runtime.CreateTacticalItem("__content_preview__", rewardId, int.MaxValue, "preview");
            return runtime.AddTacticalToBackpack(item);
        }
        private void Complete(RogueliteMapNode node, bool offerReward,
            OCC.Combat.Roguelite.RogueEncounterOutcome outcome = OCC.Combat.Roguelite.RogueEncounterOutcome.Success)
        {
            completed.Add(node.Id); Experience++; if (Experience >= Level) Level++;
            if (rogueRunDto != null)
            {
                bool failed = outcome == OCC.Combat.Roguelite.RogueEncounterOutcome.SurvivedFailure;
                bool combatSettlement = node.IsCombat || HasPendingContentCombat;
                int baseGold = combatSettlement ? 3 : node.Type == RogueliteMapNodeType.Event ? 1 : 0;
                int baseContribution = combatSettlement ? 2 : node.Type == RogueliteMapNodeType.Event ? 1 : 0;
                rogueRunDto.Gold += failed ? baseGold / 2 : baseGold;
                rogueRunDto.StageContribution += failed ? baseContribution / 2 : baseContribution;
                int timeCost = AcademyMapTuning.TimeCost(node.Type);
                if (timeCost > 0) OCC.Combat.Roguelite.RogueRunProgression.ResolveEncounter(rogueRunDto, outcome, timeCost);
                else OCC.Combat.Roguelite.RogueRunProgression.ResolveZeroTimeFunction(rogueRunDto);
                CurrentHealth = rogueRunDto.CurrentHealth; CurrentMana = rogueRunDto.CurrentMana; CurrentShield = 0;
                if (failed) offerReward = false;
            }
            else if (node.IsCombat) { Parts += 2; Aether++; }
            AccessCards += node.GrantedAccessCards;
            AwaitingReward = offerReward;
        }
        public void ClaimReward(string rewardId)
        {
            RogueliteReward reward = !AwaitingReward ? null : CurrentRewards.FirstOrDefault(value => value.Id == rewardId);
            if (reward == null) throw new InvalidOperationException("Reward is not available.");
            if (rogueRunDto != null)
            {
                if (reward.RogueSpell != null)
                {
                    if (!rogueRunDto.MasteredSpellIds.Contains(reward.Id)) rogueRunDto.MasteredSpellIds.Add(reward.Id);
                    int empty = Array.FindIndex(rogueRunDto.EquippedSpellIds, string.IsNullOrEmpty); if (empty >= 0) rogueRunDto.EquippedSpellIds[empty] = reward.Id;
                }
                else if (reward.Equipment != null)
                {
                    string instanceId = "eq-" + Seed + "-" + rogueRunDto.DeterministicCounter++;
                    rogueRunDto.EquipmentInstances.Add(new OCC.Combat.Roguelite.EquipmentInstanceDto(instanceId, reward.Id, reward.Equipment.Slot,
                        reward.Equipment.AllowedRarities[0], 0) { AcquiredOrder = rogueRunDto.EquipmentInstances.Count, SourceType = reward.BuildPath });
                }
                claimedRewards.Add(rewardId); AwaitingReward = false; return;
            }
            if (reward.Kind == RogueliteRewardKind.Item) GrantItem(reward.Item.Id);
            claimedRewards.Add(rewardId); AwaitingReward = false;
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
            if (Aether < 2) throw new InvalidOperationException("Insufficient aether for calibration.");
            Aether -= 2; IsAetherCalibrated = true;
        }
        internal OCC.Combat.Roguelite.RogueRunDto ExportRogue11(OCC.Combat.Roguelite.RogueRunDto preserved = null, string migrationReportId = "")
        {
            OCC.Combat.Roguelite.RogueRunDto dto = preserved ?? OCC.Combat.Roguelite.RogueRunDto.CreateNew("run-" + Seed, Seed);
            rogueRunDto = dto;
            dto.CurrentNodeId = CurrentNodeId; dto.RegionBossId = "core_overseer"; dto.StarterId = StarterId;
            dto.CurrentHealth = Math.Max(1, Math.Min(18, CurrentHealth)); dto.CurrentMana = Math.Max(0, Math.Min(12, CurrentMana));
            dto.AwaitingReward = AwaitingReward; dto.PendingContentChoiceId = PendingContentChoiceId ?? string.Empty;
            dto.PendingContentCombatMissionId = PendingContentCombatMissionId ?? string.Empty;
            Replace(dto.VisitedNodeIds, visited.OrderBy(id => id, StringComparer.Ordinal));
            Replace(dto.CompletedNodeIds, completed.OrderBy(id => id, StringComparer.Ordinal));
            Replace(dto.ClaimedContentIds, claimedRewards);
            Replace(dto.EncounterAssignments, encounterAssignments.OrderBy(value => value.Key, StringComparer.Ordinal).Select(value => value.Key + "=" + value.Value));
            Replace(dto.NodeContentAssignments, nodeContentAssignments.OrderBy(value => value.Key, StringComparer.Ordinal).Select(value => value.Key + "=" + value.Value));
            foreach (string id in ownedFireSpells.Where(id => FireSpellCatalog.All.Any(spell => spell.Id == id)))
                if (!dto.MasteredSpellIds.Contains(id)) dto.MasteredSpellIds.Add(id);
            for (int index = 0; index < equippedFireSpells.Length; index++)
                dto.EquippedSpellIds[4 + index] = dto.MasteredSpellIds.Contains(equippedFireSpells[index]) ? equippedFireSpells[index] : string.Empty;
            if (!string.IsNullOrEmpty(migrationReportId)) dto.MigrationReportId = migrationReportId;
            return dto;
        }
        public static RogueliteMapRun FromRogue11(OCC.Combat.Roguelite.RogueRunDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            RogueliteMapRun run = new RogueliteMapRun(dto.Seed)
            {
                CurrentNodeId = dto.CurrentNodeId, RegionBossId = "core_overseer", StarterId = dto.StarterId,
                CurrentHealth = dto.CurrentHealth, CurrentShield = 0, CurrentMana = dto.CurrentMana,
                AwaitingReward = dto.AwaitingReward, PendingContentChoiceId = dto.PendingContentChoiceId,
                PendingContentCombatMissionId = dto.PendingContentCombatMissionId, HasCombatSnapshot = true
            };
            run.rogueRunDto = dto;
            run.visited.Clear(); Restore(run.visited, string.Join(",", dto.VisitedNodeIds), true);
            run.completed.Clear(); Restore(run.completed, string.Join(",", dto.CompletedNodeIds), false);
            run.claimedRewards.Clear(); run.claimedRewards.AddRange(dto.ClaimedContentIds);
            if (dto.EncounterAssignments.Count > 0)
            {
                List<RogueliteEncounterAssignment> restoredAssignments = new List<RogueliteEncounterAssignment>();
                foreach (string row in dto.EncounterAssignments)
                {
                    int separator = row.IndexOf('=');
                    if (separator <= 0 || separator == row.Length - 1) throw new InvalidOperationException("Invalid encounter assignment row.");
                    restoredAssignments.Add(new RogueliteEncounterAssignment(row.Substring(0, separator), row.Substring(separator + 1)));
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
                    restoredContent.Add(new AcademyEventAssignment(row.Substring(0, separator), row.Substring(separator + 1)));
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
            return run;
        }
        private static void Replace(List<string> target, IEnumerable<string> source)
        { target.Clear(); target.AddRange(source ?? Array.Empty<string>()); }
        // map10 is a read-only migration input. Its writer is compiled only for historical test fixtures.
#if UNITY_INCLUDE_TESTS
        public string ToJson() => ToLegacyMap10TestFixture();
        public static RogueliteMapRun FromJson(string json) => FromLegacyMap10(json);
        internal string ToLegacyMap10TestFixture() => string.Join("|", "map10", Seed, RegionBossId, CurrentNodeId, Level, Experience, AccessCards, Supplies, ScoutingBeacons, Parts, Aether, EquippedWeaponId ?? string.Empty, EquippedSpellId ?? string.Empty, IsAetherCalibrated ? "1" : "0", PendingContentChoiceId ?? string.Empty, PendingContentCombatMissionId ?? string.Empty, string.Join(",", visited.OrderBy(id => id, StringComparer.Ordinal)), string.Join(",", completed.OrderBy(id => id, StringComparer.Ordinal)), string.Join(",", claimedRewards), AwaitingReward ? "1" : "0", string.Join(",", ownedFireSpells), string.Join(",", equippedFireSpells.Select(id => id ?? string.Empty)), Convert.ToBase64String(Encoding.UTF8.GetBytes(Inventory.ToDataString())), string.Join(",", ItemQuickbar.Select(id => id ?? string.Empty)), nextItemSequence, Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(";", lootProgress.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value)))), FireSpellCatalog.Version, EncodeMigrationClaims(pendingFireSpellReselections), EncodeMigrationClaims(fireSpellRetirementCompensations), Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(",", fireSpellMigrationWarnings.OrderBy(id => id, StringComparer.Ordinal)))), deferredNodeReward ? "1" : "0", StarterId ?? string.Empty, HasCombatSnapshot ? "1" : "0", CurrentHealth, CurrentShield, CurrentMana);
#endif
        public static RogueliteMapRun FromLegacyMap10(string json)
        {
            string[] parts = (json ?? throw new ArgumentNullException(nameof(json))).Split('|');
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
            var run = new RogueliteMapRun(int.Parse(parts[1])) { RegionBossId = "core_overseer", CurrentNodeId = parts[3], Level = int.Parse(parts[4]), Experience = int.Parse(parts[5]), AccessCards = int.Parse(parts[6]), Supplies = int.Parse(parts[7]), ScoutingBeacons = int.Parse(parts[8]), Parts = int.Parse(parts[9]), Aether = int.Parse(parts[10]), EquippedWeaponId = parts[11], EquippedSpellId = parts[12], IsAetherCalibrated = parts[13] == "1", PendingContentChoiceId = parts[14], PendingContentCombatMissionId = parts[15], AwaitingReward = parts[19] == "1" };
            Restore(run.visited, parts[16], true); Restore(run.completed, parts[17], false); run.claimedRewards.AddRange(parts[18].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun RestoreMap6Fields(string[] parts, bool migrateLegacy)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { RegionBossId = "core_overseer", CurrentNodeId = parts[3], Level = int.Parse(parts[4]), Experience = int.Parse(parts[5]), AccessCards = int.Parse(parts[6]), Supplies = int.Parse(parts[7]), ScoutingBeacons = int.Parse(parts[8]), Parts = int.Parse(parts[9]), Aether = int.Parse(parts[10]), EquippedWeaponId = parts[11], EquippedSpellId = parts[12], IsAetherCalibrated = parts[13] == "1", PendingContentChoiceId = parts[14], PendingContentCombatMissionId = parts[15], AwaitingReward = parts[19] == "1" };
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
            run.ItemQuickbar = new string[8]; string[] itemSlots = parts[23].Split(',');
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
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AccessCards = int.Parse(parts[5]), Supplies = int.Parse(parts[6]), ScoutingBeacons = int.Parse(parts[7]), Parts = int.Parse(parts[8]), Aether = int.Parse(parts[9]), EquippedWeaponId = parts[10], EquippedSpellId = parts[11], IsAetherCalibrated = parts[12] == "1", PendingContentChoiceId = parts[13], PendingContentCombatMissionId = parts[14], AwaitingReward = parts[18] == "1" };
            Restore(run.visited, parts[15], true); Restore(run.completed, parts[16], false); run.claimedRewards.AddRange(parts[17].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap3(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AccessCards = int.Parse(parts[5]), Supplies = int.Parse(parts[6]), ScoutingBeacons = int.Parse(parts[7]), PendingContentChoiceId = parts[8], PendingContentCombatMissionId = parts[9], AwaitingReward = parts[13] == "1" };
            Restore(run.visited, parts[10], true); Restore(run.completed, parts[11], false); run.claimedRewards.AddRange(parts[12].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap2(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AccessCards = int.Parse(parts[5]), AwaitingReward = parts[9] == "1" };
            Restore(run.visited, parts[6], true); Restore(run.completed, parts[7], false); run.claimedRewards.AddRange(parts[8].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun FromMap1(string[] parts)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { CurrentNodeId = parts[2], Level = int.Parse(parts[3]), Experience = int.Parse(parts[4]), AwaitingReward = parts[8] == "1" };
            Restore(run.visited, parts[5], true); Restore(run.completed, parts[6], false); run.claimedRewards.AddRange(parts[7].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            if (run.visited.Contains("core_finale")) run.AccessCards = 1;
            return run;
        }
        private static void Restore(HashSet<string> destination, string source, bool includeStart)
        {
            destination.Clear(); foreach (string id in source.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) if (RogueliteMapCatalog.Nodes.Any(node => node.Id == id)) destination.Add(id);
            if (includeStart) destination.Add("start");
        }
        public void ApplyBuild(UnitState hero)
        {
            if (rogueRunDto != null)
            {
                hero.ConfigureMana(12, rogueRunDto.CurrentMana);
                if (hero.Health > rogueRunDto.CurrentHealth) hero.TakeDamage(hero.Health - rogueRunDto.CurrentHealth);
                hero.ClearShield(); return;
            }
            hero.ConfigureMana(12, HasCombatSnapshot ? CurrentMana : 12);
            if (!string.IsNullOrEmpty(EquippedWeaponId)) hero.Equip(RogueliteMapCatalog.Rewards.First(item => item.Id == EquippedWeaponId).Weapon, CombatCatalog.Shield, hero.SkillOne, hero.SkillTwo);
            if (!string.IsNullOrEmpty(EquippedSpellId)) hero.Equip(hero.MainHand, CombatCatalog.Shield, RogueliteMapCatalog.Rewards.First(item => item.Id == EquippedSpellId).Spell, hero.SkillTwo);
            if (IsAetherCalibrated) hero.Armor += 1;
            if (HasCombatSnapshot)
            {
                if (hero.Health > CurrentHealth) hero.TakeDamage(hero.Health - CurrentHealth);
                if (hero.Shield > CurrentShield) hero.AbsorbShield(hero.Shield - CurrentShield);
                else if (hero.Shield < CurrentShield) hero.GrantShield(CurrentShield - hero.Shield);
            }
        }
    }
}
