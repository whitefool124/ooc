using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCC.Combat
{
    public enum RogueliteMapNodeType { Start, Combat, Elite, Event, Workshop, Shop, Rest, Treasure, Finale }
    public enum RogueliteMapNodeVisualState { Current, Available, Locked, Cleared, Visited, Known, Unknown }

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

    public enum RogueliteRewardKind { Weapon, Spell, Item }

    public static class FireRogueliteStarterCatalog
    {
        public const string Melee = "fire_melee";
        public const string Universal = "fire_universal";
        public const string Ranged = "fire_ranged";
        public static readonly IReadOnlyList<string> All = new[] { Melee, Universal, Ranged };
        public static string DisplayName(string id) => id == Melee ? "近战热压" : id == Ranged ? "远程导能" : id == Universal ? "武器热载" : "旧版推进";
    }
    public enum RogueliteNodeContentEffect { Supplies, ScoutingBeacon, AccessCard, Reward, Aether, Recovery }
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
        public RogueliteNodeContentChoice(string id, string displayName, string preview, RogueliteNodeContentEffect effect, string rewardId = null, bool requiresCombat = false, string combatMissionId = null, int partsCost = 0, int aetherCost = 0)
        { Id = id; DisplayName = displayName; Preview = preview; Effect = effect; RewardId = rewardId; RequiresCombat = requiresCombat; CombatMissionId = combatMissionId; PartsCost = partsCost; AetherCost = aetherCost; }
    }

    public static class RogueliteNodeContentCatalog
    {
        public static IReadOnlyList<RogueliteNodeContentChoice> ChoicesFor(RogueliteMapNode node)
        {
            switch (node.Type)
            {
                case RogueliteMapNodeType.Event:
                    return new[]
                    {
                        new RogueliteNodeContentChoice("survey", "低风险勘测", "收益：+1 侦测信标；无额外战斗。", RogueliteNodeContentEffect.ScoutingBeacon),
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
        public ItemDefinition Item { get; }
        public RogueliteReward(string id, string displayName, WeaponDefinition weapon, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Weapon; Weapon = weapon; BuildPath = buildPath; }
        public RogueliteReward(string id, string displayName, SkillDefinition spell, string buildPath) { Id = id; DisplayName = displayName; Kind = RogueliteRewardKind.Spell; Spell = spell; BuildPath = buildPath; }
        public RogueliteReward(ItemDefinition item, string buildPath) { Item = item ?? throw new ArgumentNullException(nameof(item)); Id = item.Id; DisplayName = item.DisplayName; Kind = RogueliteRewardKind.Item; BuildPath = buildPath; }
    }

    public static class RogueliteMapCatalog
    {
        // This is an orthogonal room graph: every listed connection is reciprocated by its neighbor.
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
            new RogueliteMapNode("safety_room", RogueliteMapNodeType.Rest, "守夜营帐", "无敌情推进的休整点。", 5, 3, 0, 0, "sealed_market", "permit_archive", "aether_refinery", "core_vault"),
            new RogueliteMapNode("core_approach", RogueliteMapNodeType.Elite, "塔前石庭", "古塔前庭精英守备。", 6, 1, 1, 0, "transmission_tower", "core_vault", "core_finale"),
            new RogueliteMapNode("core_vault", RogueliteMapNodeType.Treasure, "学院封存库", "战利品节点。", 6, 2, 1, 0, "aether_refinery", "safety_room", "core_approach", "core_finale"),
            new RogueliteMapNode("core_finale", RogueliteMapNodeType.Finale, "古塔核心", "击败古塔核心首领。", 7, 1, 1, 0, "core_approach", "core_vault")
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
        private readonly List<FireSpellSaveMigrationClaim> pendingFireSpellReselections = new List<FireSpellSaveMigrationClaim>();
        private readonly List<FireSpellSaveMigrationClaim> fireSpellRetirementCompensations = new List<FireSpellSaveMigrationClaim>();
        private readonly List<string> fireSpellMigrationWarnings = new List<string>();
        private bool deferredNodeReward;
        private int nextItemSequence;
        private readonly Dictionary<string, string> lootProgress = new Dictionary<string, string>(StringComparer.Ordinal);
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
        public bool AwaitingReward { get; private set; }
        public bool IsComplete => completed.Contains("core_finale") && !AwaitingReward;
        public IReadOnlyCollection<string> UnlockedNodes => visited;
        public IReadOnlyCollection<string> VisitedNodes => visited;
        public IReadOnlyCollection<string> CompletedNodes => completed;
        public IReadOnlyList<string> ClaimedRewards => claimedRewards;
        public IReadOnlyList<string> OwnedFireSpellIds => ownedFireSpells;
        public IReadOnlyList<string> EquippedFireSpellIds => equippedFireSpells;
        public IReadOnlyList<FireSpellSaveMigrationClaim> PendingFireSpellReselections => pendingFireSpellReselections;
        public IReadOnlyList<FireSpellSaveMigrationClaim> FireSpellRetirementCompensations => fireSpellRetirementCompensations;
        public IReadOnlyList<string> FireSpellMigrationWarnings => fireSpellMigrationWarnings;
        public InventoryContainerState Inventory { get; private set; }
        public string[] ItemQuickbar { get; private set; } = new string[8];
        public int NextItemSequence => nextItemSequence;
        public IReadOnlyDictionary<string, string> LootProgress => lootProgress;
        public bool HasDeferredNodeReward => deferredNodeReward;
        public WeaponDefinition EquippedWeapon => string.IsNullOrEmpty(EquippedWeaponId) ? CombatCatalog.Rifle : RogueliteMapCatalog.Rewards.First(reward => reward.Id == EquippedWeaponId).Weapon;
        public IReadOnlyList<RogueliteReward> CurrentRewards => AwaitingReward
            ? RogueliteMapCatalog.RollFireSupportRewards(Seed, completed.Count, RogueliteMapCatalog.Node(CurrentNodeId).Type, Inventory.Items.Select(item => item.DefinitionId))
            : Array.Empty<RogueliteReward>();
        public IReadOnlyList<FireSpellDefinition> CurrentFireSpellChoices
        {
            get
            {
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
        public IReadOnlyList<RogueliteNodeContentChoice> CurrentContentChoices => RogueliteNodeContentCatalog.ChoicesFor(RogueliteMapCatalog.Node(CurrentNodeId));
        public bool HasPendingContentCombat => !string.IsNullOrEmpty(PendingContentCombatMissionId);
        public RogueliteMapRun(int seed)
        {
            Seed = seed; RegionBossId = seed % 2 == 0 ? "core_overseer" : "purifier_overseer"; Inventory = new InventoryContainerState();
            GrantItem("medkit", 0); GrantItem("shield_cell", 1);
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
            if (combat == null) throw new ArgumentNullException(nameof(combat)); Inventory = combat.ItemInventory.Clone(); ItemQuickbar = combat.ItemQuickbar.ToArray();
            if (combat.LootSource != null) lootProgress[combat.LootSource.Id] = combat.LootSource.ToProgressString();
            UnitState hero = combat.GetUnit("hero");
            if (hero != null)
            {
                HasCombatSnapshot = true; CurrentHealth = hero.Health; CurrentShield = hero.Shield; CurrentMana = hero.Mana;
            }
        }
        public void RestoreLootProgress(LootSourceState loot) { if (loot != null && lootProgress.TryGetValue(loot.Id, out string progress)) loot.RestoreProgress(progress); }

        public bool IsAdjacentToCurrent(string nodeId) => RogueliteMapCatalog.Node(CurrentNodeId).NextIds.Contains(nodeId);
        public bool IsNodeAvailable(string nodeId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            return IsAdjacentToCurrent(nodeId) && AccessCards >= node.RequiredAccessCards;
        }
        public bool IsNodeKnown(string nodeId) => visited.Contains(nodeId) || RogueliteMapCatalog.Node(CurrentNodeId).NextIds.Contains(nodeId);
        public RogueliteMapNodeVisualState VisualStateFor(string nodeId)
        {
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
            if (node.Id == CurrentNodeId) return RogueliteMapNodeVisualState.Current;
            if (completed.Contains(node.Id)) return RogueliteMapNodeVisualState.Cleared;
            if (IsNodeAvailable(node.Id)) return RogueliteMapNodeVisualState.Available;
            if (IsAdjacentToCurrent(node.Id) && AccessCards < node.RequiredAccessCards) return RogueliteMapNodeVisualState.Locked;
            if (visited.Contains(node.Id) || IsNodeKnown(node.Id)) return RogueliteMapNodeVisualState.Known;
            return RogueliteMapNodeVisualState.Unknown;
        }
        public IReadOnlyList<RogueliteMapNode> AvailableNodes => RogueliteMapCatalog.Nodes.Where(node => IsNodeAvailable(node.Id)).ToArray();
        public void SelectNode(string nodeId)
        {
            if (!IsNodeAvailable(nodeId)) throw new InvalidOperationException("Node is not adjacent or its permission gate is locked.");
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
            RogueliteNodeContentChoice choice = CurrentContentChoices.FirstOrDefault(item => item.Id == choiceId) ?? throw new InvalidOperationException("Unknown node content choice.");
            if (Parts < choice.PartsCost || Aether < choice.AetherCost) throw new InvalidOperationException("Insufficient parts or aether.");
            Parts -= choice.PartsCost; Aether -= choice.AetherCost;
            PendingContentChoiceId = choice.Id;
            if (choice.RequiresCombat) { PendingContentCombatMissionId = choice.CombatMissionId; return; }
            ApplyContentChoice(choice); Complete(node, node.Type == RogueliteMapNodeType.Treasure); PendingContentChoiceId = null;
        }
        public void CompletePendingContentCombat()
        {
            if (!HasPendingContentCombat) throw new InvalidOperationException("No event combat is active.");
            RogueliteNodeContentChoice choice = CurrentContentChoices.First(item => item.Id == PendingContentChoiceId);
            ApplyContentChoice(choice); Complete(RogueliteMapCatalog.Node(CurrentNodeId), false);
            PendingContentChoiceId = null; PendingContentCombatMissionId = null;
        }
        private void ApplyContentChoice(RogueliteNodeContentChoice choice)
        {
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
        private void Complete(RogueliteMapNode node, bool offerReward)
        {
            completed.Add(node.Id); Experience++; if (Experience >= Level) Level++;
            if (node.IsCombat) { Parts += 2; Aether++; }
            AccessCards += node.GrantedAccessCards;
            AwaitingReward = offerReward;
        }
        public void ClaimReward(string rewardId)
        {
            RogueliteReward reward = !AwaitingReward ? null : CurrentRewards.FirstOrDefault(value => value.Id == rewardId);
            if (reward == null) throw new InvalidOperationException("Reward is not available.");
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
                    if (slot >= 0 && slot < equippedFireSpells.Length && string.IsNullOrEmpty(equippedFireSpells[slot])) equippedFireSpells[slot] = spellId;
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
        public string ToJson() => string.Join("|", "map9", Seed, RegionBossId, CurrentNodeId, Level, Experience, AccessCards, Supplies, ScoutingBeacons, Parts, Aether, EquippedWeaponId ?? string.Empty, EquippedSpellId ?? string.Empty, IsAetherCalibrated ? "1" : "0", PendingContentChoiceId ?? string.Empty, PendingContentCombatMissionId ?? string.Empty, string.Join(",", visited.OrderBy(id => id, StringComparer.Ordinal)), string.Join(",", completed.OrderBy(id => id, StringComparer.Ordinal)), string.Join(",", claimedRewards), AwaitingReward ? "1" : "0", string.Join(",", ownedFireSpells), string.Join(",", equippedFireSpells.Select(id => id ?? string.Empty)), Convert.ToBase64String(Encoding.UTF8.GetBytes(Inventory.ToDataString())), string.Join(",", ItemQuickbar.Select(id => id ?? string.Empty)), nextItemSequence, Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(";", lootProgress.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value)))), FireSpellCatalog.Version, EncodeMigrationClaims(pendingFireSpellReselections), EncodeMigrationClaims(fireSpellRetirementCompensations), Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(",", fireSpellMigrationWarnings.OrderBy(id => id, StringComparer.Ordinal)))), deferredNodeReward ? "1" : "0", StarterId ?? string.Empty, HasCombatSnapshot ? "1" : "0", CurrentHealth, CurrentShield, CurrentMana);
        public static RogueliteMapRun FromJson(string json)
        {
            string[] parts = (json ?? throw new ArgumentNullException(nameof(json))).Split('|');
            if (parts.Length == 36 && parts[0] == "map9")
            {
                if (!string.Equals(parts[26], FireSpellCatalog.Version, StringComparison.Ordinal)) throw new InvalidOperationException("Unsupported fire spell catalog version.");
                RogueliteMapRun map9Run = RestoreMap6Fields(parts, false); RestoreInventoryAndLoot(map9Run, parts);
                map9Run.pendingFireSpellReselections.AddRange(DecodeMigrationClaims(parts[27], FireSpellSaveMigrationKind.ReselectSameRarity));
                map9Run.fireSpellRetirementCompensations.AddRange(DecodeMigrationClaims(parts[28], FireSpellSaveMigrationKind.Compensation));
                map9Run.fireSpellMigrationWarnings.AddRange(Encoding.UTF8.GetString(Convert.FromBase64String(parts[29])).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                map9Run.deferredNodeReward = parts[30] == "1"; map9Run.StarterId = parts[31]; map9Run.HasCombatSnapshot = parts[32] == "1";
                map9Run.CurrentHealth = int.Parse(parts[33]); map9Run.CurrentShield = int.Parse(parts[34]); map9Run.CurrentMana = int.Parse(parts[35]);
                return map9Run;
            }
            if (parts.Length == 31 && parts[0] == "map8")
            {
                if (!string.Equals(parts[26], FireSpellCatalog.Version, StringComparison.Ordinal)) throw new InvalidOperationException("Unsupported fire spell catalog version.");
                RogueliteMapRun map8Run = RestoreMap6Fields(parts, false);
                RestoreInventoryAndLoot(map8Run, parts);
                map8Run.pendingFireSpellReselections.AddRange(DecodeMigrationClaims(parts[27], FireSpellSaveMigrationKind.ReselectSameRarity));
                map8Run.fireSpellRetirementCompensations.AddRange(DecodeMigrationClaims(parts[28], FireSpellSaveMigrationKind.Compensation));
                map8Run.fireSpellMigrationWarnings.AddRange(Encoding.UTF8.GetString(Convert.FromBase64String(parts[29])).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                map8Run.deferredNodeReward = parts[30] == "1";
                return map8Run;
            }
            if ((parts.Length == 25 || parts.Length == 26) && parts[0] == "map7")
            {
                RogueliteMapRun map7Run = RestoreMap6Fields(parts, true);
                RestoreInventoryAndLoot(map7Run, parts);
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
            var run = new RogueliteMapRun(int.Parse(parts[1])) { RegionBossId = parts[2], CurrentNodeId = parts[3], Level = int.Parse(parts[4]), Experience = int.Parse(parts[5]), AccessCards = int.Parse(parts[6]), Supplies = int.Parse(parts[7]), ScoutingBeacons = int.Parse(parts[8]), Parts = int.Parse(parts[9]), Aether = int.Parse(parts[10]), EquippedWeaponId = parts[11], EquippedSpellId = parts[12], IsAetherCalibrated = parts[13] == "1", PendingContentChoiceId = parts[14], PendingContentCombatMissionId = parts[15], AwaitingReward = parts[19] == "1" };
            Restore(run.visited, parts[16], true); Restore(run.completed, parts[17], false); run.claimedRewards.AddRange(parts[18].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); return run;
        }
        private static RogueliteMapRun RestoreMap6Fields(string[] parts, bool migrateLegacy)
        {
            var run = new RogueliteMapRun(int.Parse(parts[1])) { RegionBossId = parts[2], CurrentNodeId = parts[3], Level = int.Parse(parts[4]), Experience = int.Parse(parts[5]), AccessCards = int.Parse(parts[6]), Supplies = int.Parse(parts[7]), ScoutingBeacons = int.Parse(parts[8]), Parts = int.Parse(parts[9]), Aether = int.Parse(parts[10]), EquippedWeaponId = parts[11], EquippedSpellId = parts[12], IsAetherCalibrated = parts[13] == "1", PendingContentChoiceId = parts[14], PendingContentCombatMissionId = parts[15], AwaitingReward = parts[19] == "1" };
            Restore(run.visited, parts[16], true); Restore(run.completed, parts[17], false); run.claimedRewards.AddRange(parts[18].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            string[] rawOwned = parts[20].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] rawEquipped = parts[21].Split(',');
            if (migrateLegacy)
            {
                bool nodeReward = run.AwaitingReward;
                FireSpellSaveMigrationResult migration = FireSpellSaveMigration.Migrate(rawOwned, rawEquipped);
                run.ownedFireSpells.AddRange(migration.DirectOwnedIds.Distinct(StringComparer.Ordinal));
                for (int i = 0; i < Math.Min(run.equippedFireSpells.Length, migration.EquippedNewIds.Count); i++)
                    if (run.ownedFireSpells.Contains(migration.EquippedNewIds[i])) run.equippedFireSpells[i] = migration.EquippedNewIds[i];
                run.pendingFireSpellReselections.AddRange(migration.ReselectClaims);
                run.fireSpellRetirementCompensations.AddRange(migration.CompensationClaims);
                run.fireSpellMigrationWarnings.AddRange(migration.UnknownLegacyIds.Select(id => "unknown_legacy_fire_spell:" + id));
                run.deferredNodeReward = nodeReward && run.pendingFireSpellReselections.Count > 0;
                run.AwaitingReward = nodeReward || run.pendingFireSpellReselections.Count > 0;
            }
            else
            {
                run.ownedFireSpells.AddRange(rawOwned.Where(id => FireSpellCatalog.Get(id) != null).Distinct(StringComparer.Ordinal));
                for (int i = 0; i < Math.Min(run.equippedFireSpells.Length, rawEquipped.Length); i++) if (run.ownedFireSpells.Contains(rawEquipped[i])) run.equippedFireSpells[i] = rawEquipped[i];
            }
            return run;
        }
        private static void RestoreInventoryAndLoot(RogueliteMapRun run, string[] parts)
        {
            run.Inventory = InventoryContainerState.FromDataString(Encoding.UTF8.GetString(Convert.FromBase64String(parts[22])));
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
